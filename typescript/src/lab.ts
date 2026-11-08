import {
    asError,
    ExecutionException,
    isCancellation,
    MissingExpectedException,
    throwIfCancelled,
} from "./exceptions.js";
import { SubjectMetadata } from "./model.js";
import type { ErrorConstructor, Model, ReadonlySubjectMetadata } from "./model.js";

type Awaitable<T> = T | Promise<T>;
type SetMetadata<TModel> = (model: TModel, signal: AbortSignal) => Awaitable<unknown>;

export type LabCallback<TModel> = (model: TModel, metadata: SubjectMetadata, signal: AbortSignal) => Awaitable<void>;
export type CompactLabCallback<TModel> = (model: TModel, signal: AbortSignal) => Awaitable<void>;

export interface ILab<TModel extends Model> {
    readonly id: string;
    execute(model: TModel, signal?: AbortSignal): Promise<ExecutionException | null>;
}

interface MetadataOptions {
    readonly expectedExceptionType?: ErrorConstructor;
    readonly toUnsustainable?: boolean;
    readonly toUncontinuable?: boolean;
    readonly remainingExecutionCount?: number;
}

export interface LabOptions<TModel> extends MetadataOptions {
    readonly setMetadata?: SetMetadata<TModel>;
    readonly arranger?: LabCallback<TModel>;
    readonly actor?: LabCallback<TModel>;
    readonly asserter?: LabCallback<TModel>;
}

export interface CompactLabOptions<TModel> extends MetadataOptions {
    readonly arranger?: CompactLabCallback<TModel>;
    readonly actor?: CompactLabCallback<TModel>;
    readonly asserter?: CompactLabCallback<TModel>;
}

export interface MergeOptions {
    readonly useSetMetadata?: boolean;
    readonly useArranger?: boolean;
    readonly useActor?: boolean;
    readonly useAsserter?: boolean;
}

export class Lab<TModel extends Model> implements ILab<TModel> {
    // Front
    public static readonly Extender = "_";
    public static readonly Separator = "__";
    public static readonly DefaultId = "Lab";

    public readonly id: string;
    public readonly setMetadata?: SetMetadata<TModel>;
    public readonly arranger?: LabCallback<TModel>;
    public readonly actor?: LabCallback<TModel>;
    public readonly asserter?: LabCallback<TModel>;
    public readonly metadataTemplate: ReadonlySubjectMetadata;


    // Content
    public constructor(id: string = Lab.DefaultId, options: LabOptions<TModel> = {}) {
        this.id = id;
        this.setMetadata = options.setMetadata;
        this.arranger = options.arranger;
        this.actor = options.actor;
        this.asserter = options.asserter;
        this.metadataTemplate = Object.freeze({
            expectedExceptionType: options.expectedExceptionType,
            toUnsustainable: options.toUnsustainable ?? false,
            toUncontinuable: options.toUncontinuable ?? false,
            remainingExecutionCount: options.remainingExecutionCount ?? -1,
        });
    }

    public copy(id: string): Lab<TModel> {
        return new Lab(id, {
            ...this.metadataTemplate,
            setMetadata: this.setMetadata,
            arranger: this.arranger,
            actor: this.actor,
            asserter: this.asserter,
        });
    }

    public execute(model: TModel, signal = new AbortController().signal): Promise<ExecutionException | null> {
        return executeLayers(this.id, model, [this], signal, false);
    }

    public toString(): string {
        return `Lab '${this.id}'`;
    }
}

export class CompactLab<TModel extends Model> {
    // Front
    public id: string;
    public arranger?: CompactLabCallback<TModel>;
    public actor?: CompactLabCallback<TModel>;
    public asserter?: CompactLabCallback<TModel>;
    public expectedExceptionType?: ErrorConstructor;
    public toUnsustainable: boolean;
    public toUncontinuable: boolean;
    public remainingExecutionCount: number;


    // Content
    public constructor(id: string = Lab.DefaultId, options: CompactLabOptions<TModel> = {}) {
        this.id = id;
        this.arranger = options.arranger;
        this.actor = options.actor;
        this.asserter = options.asserter;
        this.expectedExceptionType = options.expectedExceptionType;
        this.toUnsustainable = options.toUnsustainable ?? false;
        this.toUncontinuable = options.toUncontinuable ?? false;
        this.remainingExecutionCount = options.remainingExecutionCount ?? -1;
    }

    public build(): Lab<TModel> {
        return new Lab(this.id, {
            arranger: this.arranger && ((model, _, signal) => this.arranger?.(model, signal)),
            actor: this.actor && ((model, _, signal) => this.actor?.(model, signal)),
            asserter: this.asserter && ((model, _, signal) => this.asserter?.(model, signal)),
            expectedExceptionType: this.expectedExceptionType,
            toUnsustainable: this.toUnsustainable,
            toUncontinuable: this.toUncontinuable,
            remainingExecutionCount: this.remainingExecutionCount,
        });
    }
}

export class CompositeLab<TModel extends Model> implements ILab<TModel> {
    // Front
    public get id(): string { return this._id; }

    // Internal
    private _id: string;
    private _labs: readonly Lab<TModel>[];


    // Content
    public constructor(original: Lab<TModel>, extension: Lab<TModel>) {
        this._labs = [original, extension];
        this._id = [extension.id, original.id].join(Lab.Separator);
    }

    public extend(extension: Lab<TModel>): CompositeLab<TModel> {
        const result = new CompositeLab(this._labs[0], extension);
        result._labs = [...this._labs, extension];
        result._id = [extension.id, this.id].join(Lab.Separator);
        return result;
    }

    public execute(model: TModel, signal = new AbortController().signal): Promise<ExecutionException | null> {
        return executeLayers(this.id, model, this._labs, signal, true);
    }

    public toString(): string {
        return `Composite Lab '${this.id}'`;
    }
}

export function extend<TModel extends Model>(extension: Lab<TModel>, original: ILab<TModel>): CompositeLab<TModel>;
export function extend<TModel extends Model>(
    extension: Lab<TModel>,
    originals: Iterable<ILab<TModel>>,
): Iterable<CompositeLab<TModel>>;
export function extend<TModel extends Model>(
    extension: Lab<TModel>,
    original: ILab<TModel> | Iterable<ILab<TModel>>,
): CompositeLab<TModel> | Iterable<CompositeLab<TModel>> {
    if (original instanceof Lab) return new CompositeLab(original, extension);
    if (original instanceof CompositeLab) return original.extend(extension);

    if (Symbol.iterator in original) {
        return (function* () {
            for (const lab of original) yield extend(extension, lab);
        })();
    }

    throw new TypeError("Cannot extend a lab of unknown type.");
}

export function merge<TModel extends Model>(
    lab: Lab<TModel>,
    template: Lab<TModel>,
    options?: MergeOptions,
): Lab<TModel>;
export function merge<TModel extends Model>(
    lab: Lab<TModel>,
    templates: Iterable<Lab<TModel>>,
    options?: MergeOptions,
): Iterable<Lab<TModel>>;
export function merge<TModel extends Model>(
    lab: Lab<TModel>,
    template: Lab<TModel> | Iterable<Lab<TModel>>,
    options: MergeOptions = {},
): Lab<TModel> | Iterable<Lab<TModel>> {
    if (!(template instanceof Lab)) {
        return (function* () {
            for (const item of template) yield merge(lab, item, options);
        })();
    }

    const id = template.id === Lab.DefaultId ? lab.id : [lab.id, template.id].join(Lab.Extender);
    const metadata = new SubjectMetadata();
    metadata.merge(lab.metadataTemplate);
    metadata.merge(template.metadataTemplate);

    return new Lab(id, {
        ...metadata,
        setMetadata: combine(options.useSetMetadata === false ? undefined : template.setMetadata, lab.setMetadata),
        arranger: combine(options.useArranger === false ? undefined : template.arranger, lab.arranger),
        actor: combine(options.useActor === false ? undefined : template.actor, lab.actor),
        asserter: combine(options.useAsserter === false ? undefined : template.asserter, lab.asserter),
    });
}

function combine<TArgs extends [...unknown[], AbortSignal], TResult>(
    first: ((...args: TArgs) => Awaitable<TResult>) | undefined,
    second: ((...args: TArgs) => Awaitable<TResult>) | undefined,
): ((...args: TArgs) => Awaitable<TResult>) | undefined {
    if (!first || !second) return first ?? second;

    return async (...args: TArgs) => {
        await first(...args);
        throwIfCancelled(args[args.length - 1] as AbortSignal);
        return second(...args);
    };
}

async function executeLayers<TModel extends Model>(
    id: string,
    model: TModel,
    layers: readonly Lab<TModel>[],
    signal: AbortSignal,
    composite: boolean,
): Promise<ExecutionException | null> {
    function fail(stage: string, cause: unknown, layer?: Lab<TModel>): ExecutionException {
        if (isCancellation(cause, signal)) throw cause;

        model.continuable = false;
        const layerSuffix = composite && layer ? ` at '${layer.id}'` : "";
        return new ExecutionException(`${stage} failed${layerSuffix}`, asError(cause));
    }

    throwIfCancelled(signal);

    try {
        model.recordExecution(id);
        model.metadataGroup.clear();
    } catch (cause) {
        return fail("Model setting", cause);
    }

    const arranged: { lab: Lab<TModel>; metadata: SubjectMetadata }[] = [];

    for (const lab of layers) {
        try {
            throwIfCancelled(signal);
            const metadata = new SubjectMetadata(await lab.setMetadata?.(model, signal));
            throwIfCancelled(signal);
            metadata.merge(lab.metadataTemplate);
            model.metadataGroup.set(lab, metadata);
            arranged.push({ lab, metadata });
            await lab.arranger?.(model, metadata, signal);
        } catch (cause) {
            return fail("Arrange", cause, lab);
        }
    }

    throwIfCancelled(signal);
    const original = arranged[0];
    let actFailure: { cause: unknown } | null = null;

    try {
        await original.lab.actor?.(model, original.metadata, signal);
    } catch (cause) {
        if (isCancellation(cause, signal)) throw cause;

        actFailure = { cause };
    }

    throwIfCancelled(signal);
    const expectedTypes = arranged.flatMap(({ metadata }) =>
        metadata.expectedExceptionType === undefined ? [] : [metadata.expectedExceptionType]);

    if (actFailure && !expectedTypes.some((type) => actFailure.cause instanceof type)) {
        return fail("Act", actFailure.cause, original.lab);
    }

    const missingType = expectedTypes.find((type) => !(actFailure?.cause instanceof type));

    if (missingType) {
        return fail("Act", new MissingExpectedException(missingType, actFailure?.cause), original.lab);
    }

    for (let index = arranged.length - 1; index >= 0; index--) {
        const { lab, metadata } = arranged[index];

        try {
            throwIfCancelled(signal);
            await lab.asserter?.(model, metadata, signal);
        } catch (cause) {
            return fail("Assert", cause, lab);
        }
    }

    throwIfCancelled(signal);

    try {
        for (const { metadata } of arranged) {
            if (metadata.toUncontinuable) model.continuable = false;
            else if (metadata.toUnsustainable) model.sustainable = false;

            if (metadata.remainingExecutionCount >= 0 &&
                (model.remainingExecutionCount === null || metadata.remainingExecutionCount < model.remainingExecutionCount)) {
                model.remainingExecutionCount = metadata.remainingExecutionCount;
            }
        }
    } catch (cause) {
        return fail("Execution limit", cause);
    }

    return null;
}
