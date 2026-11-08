import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

import { verifySelectionPaths } from "../helpers/selection.mjs";

test("runs every shared seeded path", async t => {
    // TR03
    const vectors = JSON.parse(await readFile(new URL("../../../contracts/selection.json", import.meta.url), "utf8"));
    const expectedPaths = vectors.paths.reduce((count, path) => count + path.depth * path.ids.length, 0);
    assert.ok(expectedPaths > 0);
    const processed = await verifySelectionPaths(vectors);
    assert.equal(processed, expectedPaths);
    t.diagnostic("Processed " + processed + " candidates across " + vectors.paths.length + " fixed seeded paths.");
});
