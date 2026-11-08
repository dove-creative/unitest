import type { ErrorConstructor } from "./model.js";

export class ExecutionException extends Error {
    public constructor(message = "", cause?: unknown) {
        super(message, { cause });
        this.name = "ExecutionException";
    }
}

export class MissingExpectedException extends Error {
    public constructor(public readonly expectedExceptionType: ErrorConstructor, cause?: unknown) {
        super(`Expected exception '${expectedExceptionType.name}' was not thrown.`, { cause });
        this.name = "MissingExpectedException";
    }
}

export class TestCaseAbsentException extends Error {
    public constructor(public readonly parameterName?: string, cause?: unknown) {
        super("Cannot proceed: the required test state is absent.", { cause });
        this.name = "TestCaseAbsentException";
    }
}

export class UndefinedTestCaseException extends Error {
    public constructor(testCase?: unknown, cause?: unknown) {
        super(testCase === undefined ? "Cannot execute undefined test case." :
            `Cannot execute undefined test case '${String(testCase)}'.`, { cause });
        this.name = "UndefinedTestCaseException";
    }
}

export class InvalidTestException extends Error {
    public constructor(currentCase?: unknown, currentModelState?: unknown, cause?: unknown) {
        super(currentCase === undefined ? "Cannot execute test due to invalid case or state." :
            `Cannot execute test case '${String(currentCase)}' in state '${String(currentModelState)}'.`, { cause });
        this.name = "InvalidTestException";
    }
}

export class CancellationError extends Error {
    public constructor(cause?: unknown) {
        super("The operation cannot be performed because it has been cancelled.", { cause });
        this.name = "CancellationError";
    }
}

export function throwIfCancelled(signal: AbortSignal): void {
    if (signal.aborted) throw new CancellationError(signal.reason);
}

export function isCancellation(error: unknown, signal: AbortSignal): boolean {
    return error instanceof CancellationError || (signal.aborted &&
        (error === signal.reason || (error instanceof Error && error.name === "AbortError")));
}

export function asError(value: unknown): Error {
    if (value instanceof Error) return value;

    let description: string;

    try {
        description = String(value);
    } catch {
        description = "<unprintable value>";
    }

    return new Error(`A non-Error value was thrown: ${description}`, { cause: value });
}
