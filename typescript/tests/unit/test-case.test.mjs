import assert from "node:assert/strict";
import test from "node:test";

import { TestCase } from "../../dist/index.js";

test("refines a test case without changing earlier values", () => {
    // TU02
    const initial = new TestCase("idle", "cold");
    const included = initial.include(0, "ready");
    const excluded = included.exclude(1, "warm", "hot");
    const confined = excluded.tryConfine(0, "ready");

    assert.equal(initial.count, 2);
    assert.equal(initial.at(0), "idle");
    assert.equal(initial.at(1), "cold");
    assert.equal(initial.confineable(0, "ready"), false);
    assert.equal(included.confineable(0, "ready", "idle"), true);
    assert.equal(excluded.confineable(1, "warm"), false);
    assert.equal(confined?.at(0), "ready");
    assert.equal(confined?.at(1), "cold");

    const appended = confined.exclude(2, "disabled");
    const narrowed = appended.confineableExcept(2, "archived");
    assert.equal(narrowed?.confineable(2, "active"), true);
    assert.equal(narrowed?.confineable(2, "archived"), false);
    assert.equal(narrowed?.confineable(2, "disabled"), false);
    assert.equal(appended.confineable(2, "archived"), true);
});
