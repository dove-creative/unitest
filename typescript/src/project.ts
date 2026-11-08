import { asError, ExecutionException, isCancellation, throwIfCancelled } from "./exceptions.js";
import type { CompactLab, ILab } from "./lab.js";
import { Model } from "./model.js";
import { Node, NodeStatus } from "./node.js";
import { compareOrdinal } from "./seed.js";

export interface ExecuteOptions {
    readonly stopOnFailure?: boolean;
    readonly signal?: AbortSignal;
}

export interface ContinuousExecuteOptions extends ExecuteOptions {
    readonly seed?: number;
}

export abstract class Project<TModel extends Model> {
    // Front
    public get processCount(): number { return this._processCount; }
    public get executed(): boolean { return this._executed; }

    // Internal
    private readonly _createModel: () => TModel;
    private _controller: AbortController | undefined;
    private _executed = false;
    private _processCount = 0;


    // Content --------------------
    // Scenario definition
    protected constructor(createModel: () => TModel) {
        this._createModel = createModel;
    }

    public abstract createLabs(model: TModel): Iterable<ILab<TModel>> | null | undefined;

    public createLab(model: TModel, id: string): ILab<TModel> {
        for (const lab of this.createLabs(model) ?? []) {
            if (lab.id === id) return lab;
        }

        throw new RangeError(`No test found with id '${id}'.`);
    }

    // Execution
    public async execute(depthOrPath: number | string, options: ExecuteOptions = {}): Promise<Node<TModel>> {
        if (typeof depthOrPath === "string") {
            return this.run(options, (root, signal, stopOnFailure) =>
                this.runPath(root, depthOrPath, signal, stopOnFailure));
        }

        requireInt32(depthOrPath, "Depth");
        return this.run(options, (root, signal, stopOnFailure) =>
            this.runDepth(root, depthOrPath, signal, stopOnFailure));
    }

    public async executeContinuously(depth: number, options: ContinuousExecuteOptions = {}): Promise<Node<TModel>> {
        requireInt32(depth, "Depth");
        const seed = options.seed ?? 0;
        requireInt32(seed, "Seed");
        return this.run(options, (root, signal, stopOnFailure) =>
            this.runContinuously(root, depth, seed, signal, stopOnFailure));
    }

    public cancel(): void {
        this._executed = true;
        this._controller?.abort();
    }

    private async run(
        options: ExecuteOptions,
        operation: (root: Node<TModel>, signal: AbortSignal, stopOnFailure: boolean) => Promise<void>,
    ): Promise<Node<TModel>> {
        if (this._executed) throw new Error("A project cannot be executed multiple times.");

        this._executed = true;
        const controller = new AbortController();
        this._controller = controller;
        const externalSignal = options.signal;
        const cancel = () => controller.abort(externalSignal?.reason);
        externalSignal?.addEventListener("abort", cancel, { once: true });

        try {
            if (externalSignal?.aborted) cancel();

            const root = new Node(this._createModel);

            if (controller.signal.aborted) root.setCancellationException(controller.signal.reason);
            else await operation(root, controller.signal, options.stopOnFailure ?? true);

            return root;
        } finally {
            externalSignal?.removeEventListener("abort", cancel);
            this._controller = undefined;
        }
    }

    private async runDepth(
        root: Node<TModel>,
        depth: number,
        signal: AbortSignal,
        stopOnFailure: boolean,
    ): Promise<void> {
        const pending = [root];

        // Like the C# depth mode, even depth zero executes the root's first-level candidates.
        for (let index = 0; index < pending.length; index++) {
            const parent = pending[index];
            await this.expand(parent, signal, stopOnFailure);

            if (this.shouldStop(parent, signal, stopOnFailure)) return;

            for (const child of parent.afters) {
                if (child.continuable && child.depth < depth) pending.push(child);
            }
        }
    }

    private async runPath(
        root: Node<TModel>,
        path: string,
        signal: AbortSignal,
        stopOnFailure: boolean,
    ): Promise<void> {
        let node = root;

        for (const id of path.split(Model.Separator)) {
            try {
                await yieldToHost();
                throwIfCancelled(signal);
                node = await node.append(this.createLab(node.model, id), signal);
                await this.runNode(node, signal);
            } catch (error) {
                if (!isCancellation(error, signal)) throw error;

                node.setCancellationException(error);
                return;
            }

            if (node.status === NodeStatus.Cancelled) return;
            if (stopOnFailure && node.status === NodeStatus.Failure) return;
        }
    }

    private async runContinuously(
        root: Node<TModel>,
        depth: number,
        seed: number,
        signal: AbortSignal,
        stopOnFailure: boolean,
    ): Promise<void> {
        let node = root;

        for (let index = 0; index < depth; index++) {
            await this.expand(node, signal, stopOnFailure);

            if (node.status === NodeStatus.Failure || this.shouldStop(node, signal, stopOnFailure)) return;

            // Array sorting is stable, and afters are in candidate generation order.
            const candidates = node.afters.filter((child) => child.continuable && child.model.sustainable)
                .sort((left, right) => compareOrdinal(left.id, right.id));

            if (candidates.length === 0) return;

            node = candidates[node.model.getDeterministicRandom(candidates.length, seed)];
        }
    }

    private async expand(parent: Node<TModel>, signal: AbortSignal, stopOnFailure: boolean): Promise<void> {
        try {
            await yieldToHost();
            throwIfCancelled(signal);

            for (const lab of this.createLabs(parent.model) ?? []) {
                const child = await parent.append(lab, signal);
                await this.runNode(child, signal);

                if (child.status === NodeStatus.Cancelled) {
                    this.cancel();
                    return;
                }

                if (stopOnFailure && child.status === NodeStatus.Failure) return;

                await yieldToHost();
                throwIfCancelled(signal);
            }
        } catch (error) {
            if (isCancellation(error, signal)) {
                parent.setCancellationException(error);
                this.cancel();
                return;
            }

            const failure = new ExecutionException(`An exception occurred while designing ${parent}.`, asError(error));
            parent.setExternalException(failure, NodeStatus.Failure);
        }
    }

    private shouldStop(parent: Node<TModel>, signal: AbortSignal, stopOnFailure: boolean): boolean {
        return signal.aborted || (stopOnFailure && (parent.status === NodeStatus.Failure ||
            parent.afters.some((child) => child.status === NodeStatus.Failure)));
    }

    private async runNode(node: Node<TModel>, signal: AbortSignal): Promise<void> {
        try {
            await node.execute(signal);
        } finally {
            this._processCount++;
        }
    }
}

export abstract class CompactProject<TModel extends Model> extends Project<TModel> {
    public abstract createCompactLabs(model: TModel): Iterable<CompactLab<TModel>> | null | undefined;

    public override *createLabs(model: TModel): Iterable<ILab<TModel>> {
        for (const lab of this.createCompactLabs(model) ?? []) yield lab.build();
    }
}

function yieldToHost(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, 0));
}

function requireInt32(value: number, name: string): void {
    if (!Number.isInteger(value) || value < -2147483648 || value > 2147483647) {
        throw new RangeError(`${name} must be a signed 32-bit integer.`);
    }
}
