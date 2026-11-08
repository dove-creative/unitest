import { writeFile } from "node:fs/promises";
import type { TestReport } from "./report.js";

export * from "./index.js";

export async function saveReport(report: TestReport, path: string | URL): Promise<void> {
    await writeFile(path, report.toJson(), "utf8");
}
