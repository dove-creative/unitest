import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { chromium, firefox, webkit } from "playwright";

import { withStaticServer } from "./helpers/static-server.mjs";

const vectorsFile = new URL("../../contracts/selection.json", import.meta.url);

test("runs deterministic selection and async execution in the browser", { timeout: 60000 }, async t => {
    // TE01
    const vectors = JSON.parse(await readFile(vectorsFile, "utf8"));
    const expectedChecks = vectors.hashes.length + vectors.ranges.length + vectors.ordering.length;
    const expectedProcessed = vectors.paths.reduce((count, path) => count + path.depth * path.ids.length, 0);
    const resources = {
        pages: { "/index.html": "<!doctype html><html lang='en'><title>UniTest browser test</title></html>" },
        files: {
            "/contracts/selection.json": vectorsFile,
            "/typescript/tests/helpers/selection.mjs": new URL("./helpers/selection.mjs", import.meta.url),
        },
        mounts: { "/typescript/dist/": new URL("../dist/", import.meta.url) },
    };

    await withStaticServer(resources, async origin => {
        for (const [name, browserType] of Object.entries({ chromium, firefox, webkit })) {
            await t.test(name, { timeout: 15000 }, async t => {
                const browser = await browserType.launch({ timeout: 10000 });

                try {
                    const page = await browser.newPage();
                    await page.goto(origin + "/index.html");
                    const result = await page.evaluate(async () => {
                        const { verifySelectionVectors, verifySelectionPaths } =
                            await import("/typescript/tests/helpers/selection.mjs");
                        const { Lab, Model, Project } = await import("/typescript/dist/index.js");
                        const input = await fetch("/contracts/selection.json").then(response => response.json());

                        class AsyncProject extends Project {
                            constructor() {
                                super(() => new Model(0));
                            }

                            createLabs() {
                                return [new Lab("check", {
                                    arranger: async model => {
                                        await Promise.resolve();
                                        model.subject = 6;
                                    },
                                    actor: async model => {
                                        await Promise.resolve();
                                        model.subject += 4;
                                    },
                                    asserter: async model => {
                                        await Promise.resolve();

                                        if (model.subject !== 10) throw new Error("Wrong final value");
                                    },
                                })];
                            }
                        }

                        const root = await new AsyncProject().execute("check");
                        return {
                            checks: verifySelectionVectors(input),
                            processed: await verifySelectionPaths(input),
                            json: root.getReport().toJson(),
                            subject: root.afters[0].model.subject,
                        };
                    });
                    assert.equal(result.checks, expectedChecks);
                    assert.equal(result.processed, expectedProcessed);
                    assert.equal(result.subject, 10);
                    assert.equal(JSON.parse(result.json).nodes[1].status, "Success");
                    t.diagnostic("Verified " + result.checks + " fixed checks and " + result.processed + " candidates.");
                } finally {
                    await browser.close();
                }
            });
        }
    });
});
