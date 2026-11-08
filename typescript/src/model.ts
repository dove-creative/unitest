import { deterministicRandom } from "./seed.js";

export type ErrorConstructor = (abstract new (...args: never[]) => Error) & { readonly name: string };

export interface ReadonlySubjectMetadata {
    readonly expectedExceptionType?: ErrorConstructor;
    readonly toUnsustainable: boolean;
    readonly toUncontinuable: boolean;
    readonly remainingExecutionCount: number;
}

export class SubjectMetadata implements ReadonlySubjectMetadata {
    // Front
    public expectedExceptionType?: ErrorConstructor;
    public toUnsustainable = false;
    public toUncontinuable = false;
    public remainingExecutionCount = -1;


    // Content
    public constructor(public metadata?: unknown) {}

    public getMetadata<T>(): T | undefined {
        return this.metadata as T | undefined;
    }

    public copy(): SubjectMetadata {
        const result = new SubjectMetadata();
        result.merge(this);
        return result;
    }

    public merge(template: ReadonlySubjectMetadata): void {
        this.expectedExceptionType ??= template.expectedExceptionType;
        this.toUnsustainable ||= template.toUnsustainable;
        this.toUncontinuable ||= template.toUncontinuable;

        if (this.remainingExecutionCount < 0) {
            this.remainingExecutionCount = template.remainingExecutionCount;
        } else if (template.remainingExecutionCount >= 0) {
            this.remainingExecutionCount = Math.min(this.remainingExecutionCount, template.remainingExecutionCount);
        }
    }
}

export class Model<TSubject = unknown> {
    // Front
    public static readonly Separator = "/";
    public subject: TSubject;
    public readonly metadataGroup = new Map<object, SubjectMetadata>();

    public get sustainable(): boolean { return this._sustainable; }
    public set sustainable(value: boolean) {
        if (value) throw new Error("Setting the model to sustainable is not allowed.");

        this._sustainable = false;
    }

    public get continuable(): boolean { return this._continuable; }
    public set continuable(value: boolean) {
        if (value) throw new Error("Setting the model to continuable is not allowed.");

        this._continuable = false;
        this.sustainable = false;
    }

    public get remainingExecutionCount(): number | null { return this._remainingExecutionCount; }
    public set remainingExecutionCount(value: number) {
        if (!Number.isInteger(value) || value < -2147483648 || value > 2147483647) {
            throw new RangeError("The remaining execution count must be a signed 32-bit integer.");
        }

        this._remainingExecutionCount = Math.min(value, this._remainingExecutionCount ?? 2147483647);
        this.sustainable = false;

        if (this._remainingExecutionCount <= 0) this.continuable = false;
    }

    public get executionCount(): number { return this._executedLabs.length; }

    // Internal
    private _sustainable = true;
    private _continuable = true;
    private _remainingExecutionCount: number | null = null;
    private readonly _executedLabs: string[] = [];


    // Content
    public constructor(subject?: TSubject) {
        this.subject = subject as TSubject;
    }

    /** @internal */
    public recordExecution(labId: string): void {
        if (!this._continuable) {
            throw new Error(`The model can no longer be tested. Lab ID: ${labId}. History: ${this.getExecutionHistory()}`);
        }

        if (this._remainingExecutionCount !== null) {
            this.remainingExecutionCount = this._remainingExecutionCount - 1;
        }

        this._executedLabs.push(labId);
    }

    public getExecutionHistory(): string {
        return this._executedLabs.join(Model.Separator);
    }

    public getDeterministicRandom(maxExclusive: number, seed?: number): number;
    public getDeterministicRandom(minInclusive: number, maxExclusive: number, seed: number): number;
    public getDeterministicRandom(minOrMax: number, maxOrSeed = 0, seed?: number): number {
        const history = this.getExecutionHistory();
        return seed === undefined ? deterministicRandom(history, 0, minOrMax, maxOrSeed) :
            deterministicRandom(history, minOrMax, maxOrSeed, seed);
    }

    public toString(): string {
        return `Subject : ${String(this.subject ?? "Null")}, ExecutionCount : ${this.executionCount}, ` +
            `Continuable : ${this.continuable}, Sustainable : ${this.sustainable}, ` +
            `RemainingExecutionCount : ${this.remainingExecutionCount ?? "Unlimited"}`;
    }
}
