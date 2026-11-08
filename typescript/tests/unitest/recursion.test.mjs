import assert from "node:assert/strict";
import test from "node:test";

import { CompactLab, CompactProject, extend, Lab, Model, NodeStatus, Project } from "../../dist/index.js";

class DigitsProject extends Project {
    constructor() {
        super(() => new Model({ actual: 0, expected: 0 }));
    }

    createLabs() {
        return [1, 2].map(digit => new Lab(String(digit), {
            arranger: model => { model.subject.expected = 10 * model.subject.expected + digit; },
            actor: async model => {
                await Promise.resolve();
                model.subject.actual = Number(String(model.subject.actual) + digit);
            },
            asserter: model => { assert.equal(model.subject.actual, model.subject.expected); },
        }));
    }
}

class LimitedProject extends CompactProject {
    constructor() {
        super(() => new Model(0));
    }

    createCompactLabs(model) {
        if (model.executionCount === 0) {
            return [new CompactLab("limit", {
                remainingExecutionCount: 1,
                actor: current => { current.subject++; },
            })];
        }

        return [new CompactLab("add", {
            actor: current => { current.subject += 2; },
            asserter: current => { assert.equal(current.subject, 3); },
        })];
    }
}

class AxisProject extends CompactProject {
    constructor() {
        super(() => new Model({ enabled: false, level: 0, expectedLevel: 0 }));
    }

    createCompactLabs(model) {
        if (!model.subject.enabled) {
            return [new CompactLab("enable", {
                actor: current => { current.subject.enabled = true; },
            })];
        }

        return [new CompactLab("add", {
            actor: current => { current.subject.level += 2; },
        }), new CompactLab("faulty", {
            actor: current => { current.subject.level++; },
        })];
    }

    *createLabs(model) {
        for (const lab of super.createLabs(model)) {
            yield model.subject.enabled ? extend(new Lab("level", {
                arranger: current => { current.subject.expectedLevel += 2; },
                asserter: current => { assert.equal(current.subject.level, current.subject.expectedLevel); },
            }), lab) : lab;
        }
    }
}

test("explores every generated path with independent models", async t => {
    // TR01
    const project = new DigitsProject();
    const root = await project.execute(2);
    const firstLevel = root.afters;
    const secondLevel = firstLevel.flatMap(node => node.afters);
    const nodes = [root, ...firstLevel, ...secondLevel];
    assert.equal(root.getCount(), 7);
    assert.equal(project.processCount, 6);
    assert.equal(new Set(nodes.map(node => node.model)).size, 7);
    assert.equal(new Set(nodes.map(node => node.model.subject)).size, 7);
    assert.deepEqual(nodes.map(node => node.model.subject.actual), [0, 1, 2, 11, 12, 21, 22]);
    assert.deepEqual(root.allSucceed(), { succeeded: true, cancelled: false });
    t.diagnostic("Executed 6 generated paths.");
});

test("replays a path and observes an execution limit", async t => {
    // TR02
    const project = new LimitedProject();
    const explored = await project.execute(9);
    const replayed = await new LimitedProject().execute("limit/add");
    const branch = explored.getLastNode();
    const replica = replayed.getLastNode();
    assert.equal(project.processCount, 2);
    assert.equal(explored.getCount(), 3);
    assert.equal(replica.model.getExecutionHistory(), "limit/add");
    assert.equal(branch.model.subject, 3);
    assert.equal(replica.model.subject, 3);
    assert.equal(replica.model.remainingExecutionCount, 0);
    assert.equal(replica.continuable, false);
    assert.notEqual(branch.model, replica.model);
    assert.deepEqual(replayed.allSucceed(), { succeeded: true, cancelled: false });
    t.diagnostic("Executed 2 generated paths and replayed 2 Labs.");
});

test("detects a faulty transition in composed state exploration", async t => {
    // TR04
    const project = new AxisProject();
    const root = await project.execute(2, { stopOnFailure: false });
    const [valid, invalid] = root.afters[0].afters;
    assert.equal(project.processCount, 3);
    assert.equal(root.getCount(), 4);
    assert.equal(valid.status, NodeStatus.Success);
    assert.deepEqual(valid.model.subject, { enabled: true, level: 2, expectedLevel: 2 });
    assert.equal(invalid.status, NodeStatus.Failure);
    assert.equal(invalid.model.subject.level, 1);
    assert.equal(invalid.model.subject.expectedLevel, 2);
    assert.deepEqual(root.getFailedReports().nodes.map(node => node.status),
        [NodeStatus.Root, NodeStatus.Success, NodeStatus.Failure]);
    t.diagnostic("Executed 3 generated paths, including the intentionally faulty transition.");
});
