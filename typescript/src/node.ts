import { asError, CancellationError, ExecutionException, isCancellation, throwIfCancelled } from "./exceptions.js";
import type { ILab } from "./lab.js";
import { Model } from "./model.js";
import { captureException, TestReport } from "./report.js";
import type { NodeReport, ReportException } from "./report.js";

export const NodeStatus = Object.freeze({
    None: "None",
    Root: "Root",
    Ready: "Ready",
    Executing: "Executing",
    Success: "Success",
    Failure: "Failure",
    Cancelled: "Cancelled",
} as const);
export type NodeStatus = typeof NodeStatus[keyof typeof NodeStatus];

interface TraceEntry<TModel extends Model> {
    readonly lab: ILab<TModel> | null;
    readonly report: NodeReport;
}

export class Node<TModel extends Model> {
    // Front
    public readonly model: TModel;
    public readonly lab: ILab<TModel> | null;

    public get id(): string { return this.lab?.id ?? "root"; }
    public get status(): NodeStatus { return this._status; }
    public get depth(): number { return this._depth; }
    public get before(): Node<TModel> | null { return this._before; }
    public get afters(): readonly Node<TModel>[] { return this._afters; }
    public get exception(): Error | null { return this._exception; }
    public get continuable(): boolean {
        return this.model.continuable && (this._status === NodeStatus.Root || this._status === NodeStatus.Success);
    }

    // Internal
    private readonly _createModel: () => TModel;
    private readonly _afters: Node<TModel>[] = [];
    private _before: Node<TModel> | null = null;
    private _status: NodeStatus;
    private _depth = 0;
    private _executed = false;
    private _exception: Error | null = null;
    private _modelDescription: string | null = null;
    private _history = "";
    private _error: ReportException | null = null;
    private _reportError: ReportException | null = null;
    private _restoredAncestors: readonly TraceEntry<TModel>[] = [];


    // Content --------------------
    // Construction and replay
    public constructor(createModel: () => TModel, lab?: ILab<TModel>) {
        if (lab !== undefined) validateLabId(lab.id);

        this._createModel = createModel;
        this.model = createModel();
        this.lab = lab ?? null;
        this._status = lab === undefined ? NodeStatus.Root : NodeStatus.Ready;
    }

    public async append(lab: ILab<TModel>, signal = new AbortController().signal): Promise<Node<TModel>> {
        if (!this.continuable) throw new Error(`Cannot append to a node in state '${this.status}'.`);

        throwIfCancelled(signal);
        const node = new Node(this._createModel, lab);
        const trace = this.getTrace();
        const replay = trace[0]._restoredAncestors.map((entry) => entry.lab).concat(trace.map((entry) => entry.lab));

        await node.rerun(replay, signal);
        node._depth = this._depth + 1;
        node._before = this;
        this._afters.push(node);
        return node;
    }

    public async detachAndRestore(signal = new AbortController().signal): Promise<Node<TModel>> {
        if (!this.lab) throw new Error("This node cannot be restored because it has no Lab.");

        throwIfCancelled(signal);
        const node = new Node(this._createModel, this.lab);
        node._depth = this._depth;
        node._restoredAncestors = this.getAncestors();
        await node.rerun(node._restoredAncestors.map((entry) => entry.lab), signal);
        return node;
    }

    private async rerun(labs: Iterable<ILab<TModel> | null>, signal: AbortSignal): Promise<void> {
        for (const lab of labs) {
            if (!lab) continue;

            throwIfCancelled(signal);
            validateLabId(lab.id);
            const failure = await lab.execute(this.model, signal);

            if (failure) throw new ExecutionException("An error occurred while re-running Lab during Append.", failure);
        }

        throwIfCancelled(signal);
        this._history = this.model.getExecutionHistory();
    }

    private getTrace(): Node<TModel>[] {
        const trace: Node<TModel>[] = [];

        for (let current: Node<TModel> | null = this; current; current = current._before) {
            trace.push(current);
        }

        return trace.reverse();
    }

    private getAncestors(): TraceEntry<TModel>[] {
        const trace = this.getTrace();
        const ancestors = [...trace[0]._restoredAncestors];

        for (const ancestor of trace.slice(0, -1)) {
            ancestors.push({ lab: ancestor.lab, report: ancestor.captureReport(0, null) });
        }

        return ancestors;
    }

    // Execution
    public async execute(signal = new AbortController().signal): Promise<void> {
        if (!this.lab) throw new Error("Cannot execute a node without a Lab.");
        if (this._executed) throw new Error("Cannot re-execute a node that has already been executed.");

        this._executed = true;
        this._status = NodeStatus.Executing;

        try {
            throwIfCancelled(signal);
            const failure = await this.lab.execute(this.model, signal);

            if (failure) {
                this.setExternalException(failure, NodeStatus.Failure);
                return;
            }

            throwIfCancelled(signal);
            this._status = NodeStatus.Success;
            this.captureDiagnostics();
        } catch (error) {
            if (isCancellation(error, signal)) this.setCancellationException(error);
            else this.setExternalException(error, NodeStatus.Failure);
        }
    }

    public setCancellationException(cause?: unknown): void {
        this.setExternalException(cause instanceof CancellationError ? cause : new CancellationError(cause),
            NodeStatus.Cancelled);
    }

    public setExternalException(error: unknown, status: NodeStatus): void {
        this._executed = true;
        this._status = status;
        this._exception = asError(error);

        if (status === NodeStatus.Failure) this.model.continuable = false;

        this.captureDiagnostics();
    }

    // Inspection and reporting
    private captureDiagnostics(): void {
        this._history = this.model.getExecutionHistory();
        this._error = this._exception && captureException(this._exception);
        this._modelDescription = null;
        this._reportError = null;

        try {
            this._modelDescription = String(this.model);
        } catch (error) {
            this._reportError = captureException(error);
        }
    }

    private captureReport(nodeId: number, parentId: number | null): NodeReport {
        return {
            nodeId,
            parentId,
            labId: this.lab?.id ?? null,
            depth: this._depth,
            status: this._status,
            model: this._modelDescription,
            history: this._history,
            continuable: this.continuable,
            error: this._error,
            reportError: this._reportError,
        };
    }

    public getReport(): TestReport {
        const nodes: NodeReport[] = [];
        let parentId: number | null = null;

        for (const ancestor of this.getAncestors()) {
            const nodeId = nodes.length;
            nodes.push({ ...ancestor.report, nodeId, parentId });
            parentId = nodeId;
        }

        const pending: { node: Node<TModel>; parentId: number | null }[] = [{ node: this, parentId }];

        while (pending.length > 0) {
            const current = pending.pop()!;
            const nodeId = nodes.length;
            nodes.push(current.node.captureReport(nodeId, current.parentId));

            for (let index = current.node.afters.length - 1; index >= 0; index--) {
                pending.push({ node: current.node.afters[index], parentId: nodeId });
            }
        }

        return new TestReport(nodes);
    }

    public getFailedReports(): TestReport {
        const report = this.getReport();
        const keep = new Set<number>([0]);

        for (let index = report.nodes.length - 1; index >= 0; index--) {
            const node = report.nodes[index];

            if (node.status === NodeStatus.Failure) keep.add(node.nodeId);
            if (node.parentId !== null && keep.has(node.nodeId)) keep.add(node.parentId);
        }

        return new TestReport(report.nodes.filter((node) => keep.has(node.nodeId)));
    }

    public getLastNode(): Node<TModel> {
        let node: Node<TModel> = this;

        while (node.afters.length > 0) node = node.afters[0];

        return node;
    }

    public getCount(): number {
        let count = 0;
        const pending: Node<TModel>[] = [this];

        while (pending.length > 0) {
            const node = pending.pop()!;
            count++;

            for (const child of node.afters) pending.push(child);
        }

        return count;
    }

    public allSucceed(): { succeeded: boolean; cancelled: boolean } {
        let cancelled = false;
        const pending: Node<TModel>[] = [this];

        while (pending.length > 0) {
            const node = pending.pop()!;

            if (node.status === NodeStatus.Failure) return { succeeded: false, cancelled: false };

            if (node.status === NodeStatus.Cancelled) cancelled = true;
            else for (const child of node.afters) pending.push(child);
        }

        return { succeeded: true, cancelled };
    }

    public toString(): string {
        return `Node '${this.id}' (Depth=${this.depth}, Status=${this.status})`;
    }
}

function validateLabId(id: string): void {
    if (typeof id !== "string") throw new TypeError("Lab ID must be a string.");

    if (id.includes(Model.Separator)) {
        throw new RangeError(`Lab ID '${id}' cannot contain '${Model.Separator}', the execution path separator.`);
    }
}

export function getReport<TModel extends Model>(node: Node<TModel>): TestReport { return node.getReport(); }

export function getFailedReports<TModel extends Model>(node: Node<TModel>): TestReport {
    return node.getFailedReports();
}

export function getLastNode<TModel extends Model>(node: Node<TModel>): Node<TModel> { return node.getLastNode(); }

export function getCount<TModel extends Model>(node: Node<TModel> | null): number { return node?.getCount() ?? 0; }

export function allSucceed<TModel extends Model>(node: Node<TModel>): { succeeded: boolean; cancelled: boolean } {
    return node.allSucceed();
}
