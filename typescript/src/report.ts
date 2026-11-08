import { asError } from "./exceptions.js";

export interface ReportException {
    readonly type: string;
    readonly message: string;
    readonly details: string;
}

export interface NodeReport {
    readonly nodeId: number;
    readonly parentId: number | null;
    readonly labId: string | null;
    readonly depth: number;
    readonly status: string;
    readonly model: string | null;
    readonly history: string;
    readonly continuable: boolean;
    readonly error: ReportException | null;
    readonly reportError: ReportException | null;
}

export class TestReport {
    // Front
    public readonly nodes: readonly NodeReport[];


    // Content
    public constructor(nodes: Iterable<NodeReport>) {
        this.nodes = Object.freeze(Array.from(nodes, (node) => Object.freeze({
            ...node,
            error: node.error && Object.freeze({ ...node.error }),
            reportError: node.reportError && Object.freeze({ ...node.reportError }),
        })));
        Object.freeze(this);
    }

    public toJson(): string {
        return JSON.stringify(this, null, 2).replace(/\u2028/g, "\\u2028").replace(/\u2029/g, "\\u2029");
    }
}

export function captureException(error: unknown): ReportException {
    const failure = asError(error);
    const type = readText(() => failure.constructor.name, "Error");
    const message = readText(() => failure.message, "<unavailable message>");
    const seen = new Set<Error>();
    const details: string[] = [];
    let current: Error | undefined = failure;

    while (current && !seen.has(current)) {
        const entry: Error = current;
        seen.add(entry);
        details.push(readText(() => entry.stack ?? entry.toString(), "<unavailable exception details>"));

        try {
            current = entry.cause instanceof Error ? entry.cause : undefined;
        } catch {
            current = undefined;
        }
    }

    return { type, message, details: details.join("\nCaused by: ") };
}

function readText(read: () => unknown, fallback: string): string {
    try {
        const text = read();
        return typeof text === "string" ? text : fallback;
    } catch {
        return fallback;
    }
}
