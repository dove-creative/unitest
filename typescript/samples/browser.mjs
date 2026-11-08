import { downloadReport } from "../dist/browser.js";
import { scenarios } from "./scenarios.mjs";

const scenario = document.querySelector("#scenario");
const run = document.querySelector("#run");
const download = document.querySelector("#download");
const status = document.querySelector("#status");
const output = document.querySelector("#report");
let latestReport;
let latestName;

for (const name of scenarios.keys()) scenario.add(new Option(name, name));

run.addEventListener("click", async () => {
    run.disabled = true;
    download.disabled = true;
    output.textContent = "";
    status.textContent = "Running…";
    const name = scenario.value;

    try {
        const root = await scenarios.get(name)();
        latestReport = root.getReport();
        latestName = name;
        output.textContent = latestReport.toJson();
        status.textContent = `${name}: ${latestReport.nodes.length} report nodes`;
        download.disabled = false;
    } catch (error) {
        status.textContent = String(error);
    } finally {
        run.disabled = false;
    }
});

download.addEventListener("click", () => {
    downloadReport(latestReport, `${latestName}.json`);
});
