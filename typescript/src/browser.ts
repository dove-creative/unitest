import type { TestReport } from "./report.js";

export * from "./index.js";

export function downloadReport(report: TestReport, fileName = "unitest-report.json"): void {
    const blob = new Blob([report.toJson()], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.hidden = true;

    try {
        (document.body ?? document.documentElement).append(anchor);
        anchor.click();
    } finally {
        anchor.remove();
        // Browsers may consume the object URL after the click handler returns.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
}
