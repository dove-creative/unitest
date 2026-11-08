import { mkdir } from "node:fs/promises";
import { createInterface } from "node:readline/promises";
import { fileURLToPath } from "node:url";
import { NodeStatus, saveReport } from "../dist/node-output.js";
import { scenarios } from "./scenarios.mjs";

const outputDirectory = new URL("../../artifacts/typescript-samples/", import.meta.url);
const commandHelp = new Map([
    ["explore", "Explore all counter branches to depth 3."],
    ["replay", "Run increment/increment/decrement."],
    ["compose", "Combine the counter and lock state rules."],
    ["continuous", "Run a continuous path with seed 42."],
    ["async", "Await asynchronous Arrange, Act, and Assert."],
    ["cancel", "Request cancellation from a running callback."],
    ["all", "Run all six scenarios."],
    ["help", "List commands."],
    ["exit", "Close the prompt."],
]);

function printHelp() {
    for (const [name, description] of commandHelp) console.log(`${name.padEnd(12)}${description}`);
}

async function runScenario(name) {
    const root = await scenarios.get(name)();
    const report = root.getReport();
    const file = new URL(`${name}.json`, outputDirectory);
    await mkdir(outputDirectory, { recursive: true });
    await saveReport(report, file);
    console.log(`${name}: ${report.nodes.length} report nodes → ${fileURLToPath(file)}`);

    const failed = report.nodes.some(node => node.status === NodeStatus.Failure);
    const cancelled = report.nodes.some(node => node.status === NodeStatus.Cancelled);
    const expectedCancellation = name === "cancel";

    if (failed || cancelled !== expectedCancellation) {
        console.error(`${name}: unexpected outcome. See the JSON report.`);
        return 1;
    }

    if (cancelled) console.log(`${name}: cancellation recorded in the report.`);
    return 0;
}

async function runCommand(command) {
    if (command === "help") {
        printHelp();
        return 0;
    }

    if (command === "all") {
        let exitCode = 0;
        for (const name of scenarios.keys()) exitCode = Math.max(exitCode, await runScenario(name));
        return exitCode;
    }

    if (!scenarios.has(command)) {
        console.error(`Unknown command: ${command}`);
        printHelp();
        return 1;
    }

    return runScenario(command);
}

async function main() {
    const args = process.argv.slice(2);
    if (args.length > 1) throw new Error("Specify one command, or use help to list commands.");
    if (args[0] === "exit") return 0;
    if (args.length) return runCommand(args[0]);

    const input = createInterface({ input: process.stdin, output: process.stdout, prompt: "sample> " });

    try {
        printHelp();
        input.prompt();
        for await (const line of input) {
            const command = line.trim();
            if (command === "exit") break;

            if (command) {
                try {
                    await runCommand(command);
                } catch (error) {
                    console.error(error);
                }
            }

            input.prompt();
        }
    } finally {
        input.close();
    }

    return 0;
}

try {
    process.exitCode = await main();
} catch (error) {
    console.error(error);
    process.exitCode = 1;
}
