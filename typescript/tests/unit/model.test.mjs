import assert from "node:assert/strict";
import test from "node:test";

import { Lab, Model } from "../../dist/index.js";

test("tracks model history and tightens execution limits", async () => {
    // TU01
    const model = new Model(0);
    assert.equal(model.getExecutionHistory(), "");
    assert.equal(model.remainingExecutionCount, null);
    assert.equal(model.sustainable, true);
    assert.equal(model.continuable, true);

    await new Lab("prepare", { remainingExecutionCount: 2 }).execute(model);
    assert.equal(model.remainingExecutionCount, 2);
    assert.equal(model.sustainable, false);
    assert.equal(model.continuable, true);

    await new Lab("execute", { remainingExecutionCount: 9 }).execute(model);
    assert.equal(model.remainingExecutionCount, 1);

    await new Lab("finish").execute(model);
    assert.equal(model.remainingExecutionCount, 0);
    assert.equal(model.continuable, false);
    assert.equal(model.executionCount, 3);
    assert.equal(model.getExecutionHistory(), "prepare/execute/finish");
    assert.throws(() => { model.continuable = true; });
});
