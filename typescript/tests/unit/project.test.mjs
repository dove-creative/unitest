import assert from "node:assert/strict";
import test from "node:test";
import { setTimeout as delay } from "node:timers/promises";

import { ExecutionException, Lab, Model, NodeStatus, Project } from "../../dist/index.js";

class TestProject extends Project {
    constructor(createLabs, createModel = () => new Model(0)) {
        super(createModel);
        this.generate = createLabs;
    }

    createLabs(model) {
        return this.generate(model);
    }
}

test("honors stop-on-failure and the single-use lifetime", async () => {
    // TU08
    let successfulActions = 0;
    const createLabs = () => [
        new Lab("fail", { actor: () => { throw new Error("expected test failure"); } }),
        new Lab("pass", { actor: () => { successfulActions++; } }),
    ];
    const stoppingProject = new TestProject(createLabs);
    const stopped = await stoppingProject.execute(1);
    assert.equal(stopped.afters[0].status, NodeStatus.Failure);
    assert.equal(successfulActions, 0);
    assert.equal(stoppingProject.executed, true);
    await assert.rejects(stoppingProject.execute(1));

    const continued = await new TestProject(createLabs).execute(1, { stopOnFailure: false });
    assert.deepEqual(continued.afters.map(node => node.status), [NodeStatus.Failure, NodeStatus.Success]);
    assert.equal(successfulActions, 1);

    const targeted = await new TestProject(createLabs).execute("fail/pass");
    assert.equal(targeted.afters[0].status, NodeStatus.Failure);
    assert.equal(targeted.afters[0].afters.length, 0);
    assert.equal(successfulActions, 1);
    await assert.rejects(new TestProject(createLabs).execute("absent"));
});

test("observes pre-cancellation and waits for an in-flight callback", { timeout: 5000 }, async () => {
    // TU09
    const preCancelled = new AbortController();
    preCancelled.abort();
    let generated = 0;
    const skipped = await new TestProject(() => { generated++; return []; })
        .execute(2, { signal: preCancelled.signal });
    assert.equal(skipped.status, NodeStatus.Cancelled);
    assert.equal(generated, 0);

    const controller = new AbortController();
    const started = Promise.withResolvers();
    const release = Promise.withResolvers();
    const calls = [];
    const project = new TestProject(() => [
        new Lab("wait", {
            actor: async (_model, _metadata, signal) => {
                calls.push("started");
                started.resolve();
                await release.promise;
                assert.equal(signal.aborted, true);
                calls.push("finished");
            },
            asserter: () => { calls.push("assert"); },
        }),
        new Lab("later", { actor: () => { calls.push("later"); } }),
    ]);
    const running = project.execute(3, { signal: controller.signal });
    let finished = false;
    void running.then(() => { finished = true; }, () => { finished = true; });

    try {
        await started.promise;
        controller.abort(new Error("requested cancellation"));
        await delay(0);
        assert.equal(finished, false);
        assert.deepEqual(calls, ["started"]);
    } finally {
        release.resolve();
    }

    const result = await running;
    assert.deepEqual(calls, ["started", "finished"]);
    assert.equal(result.afters[0].status, NodeStatus.Cancelled);
    assert.equal(result.afters[0].afters.length, 0);
    const snapshot = result.getReport().toJson();
    const processed = project.processCount;
    await delay(0);
    assert.equal(result.getReport().toJson(), snapshot);
    assert.equal(project.processCount, processed);
    assert.deepEqual(calls, ["started", "finished"]);
});

test("distinguishes recorded generator failures from construction errors", async () => {
    // TU10
    const generatorFailure = new Error("generator broke after yielding");
    const project = new TestProject(function* () {
        yield new Lab("generated", { actor: model => { model.subject++; } });
        throw generatorFailure;
    });
    const root = await project.execute(1);
    assert.equal(root.status, NodeStatus.Failure);
    assert.ok(root.exception instanceof ExecutionException);
    assert.equal(root.exception.cause, generatorFailure);
    assert.equal(root.afters.length, 1);
    assert.equal(root.afters[0].status, NodeStatus.Success);
    assert.equal(root.afters[0].model.subject, 1);

    const factoryFailure = new Error("model construction failed");
    const failingFactory = () => { throw factoryFailure; };
    await assert.rejects(new TestProject(() => [], failingFactory).execute(1), error => error === factoryFailure);
    await assert.rejects(new TestProject(() => [], failingFactory).execute("generated"), error => error === factoryFailure);
    await assert.rejects(new TestProject(() => [], failingFactory).executeContinuously(1),
        error => error === factoryFailure);
});
