import assert from "node:assert/strict";
import test from "node:test";

import { Lab, Model, Node, NodeStatus, Project } from "../../dist/index.js";

class FailingProject extends Project {
    constructor() {
        super(() => new Model(0));
    }

    createLabs(model) {
        if (model.executionCount === 0) {
            return [new Lab("setup", { actor: current => { current.subject++; } })];
        }

        return [new Lab("fail", {
            asserter: current => {
                assert.equal(current.subject, 1);
                throw new Error("recorded failure");
            },
        })];
    }
}

test("restores a detached failure without altering the captured tree", async () => {
    // TU11
    const root = await new FailingProject().execute("setup/fail");
    const leaf = root.afters[0].afters[0];
    assert.equal(leaf.status, NodeStatus.Failure);
    const original = root.getReport();
    const originalJson = original.toJson();

    const detached = await leaf.detachAndRestore();
    assert.equal(detached.before, null);
    assert.equal(detached.depth, leaf.depth);
    assert.equal(detached.status, NodeStatus.Ready);
    assert.equal(detached.model.getExecutionHistory(), "setup");
    assert.equal(detached.model.subject, 1);
    assert.notEqual(detached.model, leaf.model);

    await detached.execute();
    assert.equal(detached.status, NodeStatus.Failure);
    const report = detached.getReport();
    assert.deepEqual(report.nodes.map(({ parentId, labId, status }) => [parentId, labId, status]), [
        [null, null, NodeStatus.Root],
        [0, "setup", NodeStatus.Success],
        [1, "fail", NodeStatus.Failure],
    ]);
    assert.equal(report.nodes[2].history, "setup/fail");

    detached.model.subject = 100;
    assert.equal(leaf.model.subject, 1);
    assert.equal(original.toJson(), originalJson);
    assert.equal(root.getReport().toJson(), originalJson);
});

test("captures independent failure diagnostics and JSON strings", async () => {
    // TU12
    const source = "line\nquote\"slash\\\u0000한\uD800🙂";
    const root = new Node(() => new Model(source));
    const success = await root.append(new Lab("pass"));
    await success.execute();
    const failed = await root.append(new Lab("fail", {
        actor: model => {
            model.subject = { toString() { throw new Error("description failed"); } };
            throw new Error("execution failed");
        },
    }));
    await failed.execute();

    const full = root.getReport();
    const fullJson = full.toJson();
    const parsed = JSON.parse(fullJson);
    assert.deepEqual(parsed.nodes.map(node => node.nodeId), [0, 1, 2]);
    assert.ok(parsed.nodes[1].model.includes(source));
    assert.equal(parsed.nodes[2].model, null);
    assert.equal(parsed.nodes[2].reportError.message, "description failed");
    assert.match(parsed.nodes[2].error.details, /execution failed/);
    assert.equal(parsed.nodes[2].status, NodeStatus.Failure);
    assert.equal(parsed.nodes[2].continuable, false);

    const failures = root.getFailedReports();
    assert.deepEqual(failures.nodes.map(({ nodeId, parentId }) => [nodeId, parentId]), [[0, null], [2, 0]]);
    failed.model.subject = "new value";
    const later = await root.append(new Lab("later"));
    await later.execute();
    assert.equal(full.toJson(), fullJson);
    assert.equal(root.getReport().nodes.length, 4);
});
