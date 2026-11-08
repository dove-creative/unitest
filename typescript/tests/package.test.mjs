import assert from "node:assert/strict";
import { execFile } from "node:child_process";
import { mkdir, mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join, resolve, sep } from "node:path";
import test from "node:test";
import { fileURLToPath, pathToFileURL } from "node:url";
import { promisify } from "node:util";
import { chromium } from "playwright";

import { withStaticServer } from "./helpers/static-server.mjs";

const run = promisify(execFile);
const packageDirectory = fileURLToPath(new URL("../", import.meta.url));
const tsc = fileURLToPath(new URL("../node_modules/typescript/bin/tsc", import.meta.url));

const consumerSource = `
import { Lab, Model, Project, type Node } from "@blackthunder/unitest";
import { saveReport } from "@blackthunder/unitest/node";
import { downloadReport } from "@blackthunder/unitest/browser";

class DemoProject extends Project<Model<string>> {
    public constructor() {
        super(() => new Model("한🙂"));
    }

    public override createLabs(): Iterable<Lab<Model<string>>> {
        return [new Lab<Model<string>>("smoke", {
            actor: async model => { model.subject += "\\nreport"; },
        })];
    }
}

const result: Node<Model<string>> = await new DemoProject().execute(1);
if (typeof downloadReport !== "function") throw new Error("Missing browser export.");
await saveReport(result.getReport(), new URL("report.json", import.meta.url));
`;

const browserPage = `
<!doctype html>
<html lang="en">
<title>Installed UniTest package</title>
<button id="download" disabled>Download report</button>
<script type="importmap">
{
    "imports": {
        "@blackthunder/unitest": "/package/index.js",
        "@blackthunder/unitest/browser": "/package/browser.js"
    }
}
</script>
<script type="module">
import { Lab, Model, Project } from "@blackthunder/unitest";
import { downloadReport } from "@blackthunder/unitest/browser";

class DemoProject extends Project {
    constructor() {
        super(() => new Model("browser"));
    }

    createLabs() {
        return [new Lab("browser-smoke")];
    }
}

const root = await new DemoProject().execute(1);
const download = document.getElementById("download");
download.addEventListener("click", () => downloadReport(root.getReport(), "consumer-report.json"));
download.disabled = false;
</script>
</html>
`;

function npm(args, cwd, cache) {
    const cli = process.env.npm_execpath ??
        (process.platform === "win32" ? join(dirname(process.execPath), "node_modules/npm/bin/npm-cli.js") : null);
    const executable = cli ? process.execPath : "npm";
    const npmArgs = cli ? [cli, "--cache", cache, ...args] : ["--cache", cache, ...args];
    return run(executable, npmArgs, { cwd, timeout: 15000 });
}

test("installs and verifies the packed package in a separate consumer", { timeout: 60000 }, async () => {
    // TE02, TE03
    const tempRoot = resolve(tmpdir());
    const directory = await mkdtemp(join(tempRoot, "unitest-package-"));
    assert.equal(dirname(directory), tempRoot);
    const cache = join(directory, "npm-cache");
    const consumer = join(directory, "consumer");

    try {
        const packed = await npm(["pack", "--ignore-scripts", "--json", "--pack-destination", directory],
            packageDirectory, cache);
        const [archive] = JSON.parse(packed.stdout);
        assert.equal(archive.name, "@blackthunder/unitest");
        assert.equal(archive.version, "0.1.0");
        const packageFiles = new Set(archive.files.map(file => file.path));
        assert.ok(packageFiles.has("dist/index.d.ts"));
        assert.ok(packageFiles.has("dist/node-output.d.ts"));
        assert.ok(packageFiles.has("dist/browser.d.ts"));
        assert.ok(packageFiles.has("dist/LICENSE.md") || packageFiles.has("LICENSE.md"));

        await mkdir(consumer);
        await writeFile(join(consumer, "package.json"), JSON.stringify({
            name: "unitest-verification",
            private: true,
            type: "module",
        }));
        await npm(["install", "--offline", "--ignore-scripts", "--no-audit", "--no-fund",
            join(directory, archive.filename)], consumer, cache);
        await writeFile(join(consumer, "consumer.ts"), consumerSource);

        await run(process.execPath, [tsc, "--strict", "--target", "ES2022", "--module", "NodeNext", "--lib", "ES2022,DOM",
            "--outDir", "build", "consumer.ts"], { cwd: consumer, timeout: 10000 });
        await run(process.execPath, [join("build", "consumer.js")], { cwd: consumer, timeout: 10000 });

        const bytes = await readFile(join(consumer, "build", "report.json"));
        assert.equal(bytes[0], "{".charCodeAt(0));
        const { nodes } = JSON.parse(bytes.toString("utf8"));
        assert.equal(nodes[1].status, "Success");
        assert.ok(nodes[1].model.includes("한🙂\nreport"));

        const installed = join(consumer, "node_modules", "@blackthunder", "unitest", "dist");
        await withStaticServer({
            pages: { "/index.html": browserPage },
            mounts: { "/package/": pathToFileURL(installed + sep) },
        }, async origin => {
            const browser = await chromium.launch({ timeout: 10000 });

            try {
                const page = await browser.newPage({ acceptDownloads: true });
                await page.goto(origin + "/index.html");
                const [download] = await Promise.all([
                    page.waitForEvent("download", { timeout: 10000 }),
                    page.locator("#download").click({ timeout: 10000 }),
                ]);
                assert.equal(download.suggestedFilename(), "consumer-report.json");
                const report = JSON.parse(await readFile(await download.path(), "utf8"));
                assert.equal(report.nodes[1].labId, "browser-smoke");
                assert.equal(report.nodes[1].status, "Success");
            } finally {
                await browser.close();
            }
        });
    } finally {
        await rm(directory, { recursive: true, force: true });
    }
});
