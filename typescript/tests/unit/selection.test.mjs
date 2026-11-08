import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";

import { verifySelectionVectors } from "../helpers/selection.mjs";

test("checks fixed deterministic-selection vectors", async () => {
    // TU13
    const vectors = JSON.parse(await readFile(new URL("../../../contracts/selection.json", import.meta.url), "utf8"));
    assert.ok(vectors.hashes.length > 0 && vectors.ranges.length > 0 && vectors.ordering.length > 0);
    assert.equal(verifySelectionVectors(vectors), vectors.hashes.length + vectors.ranges.length + vectors.ordering.length);
});
