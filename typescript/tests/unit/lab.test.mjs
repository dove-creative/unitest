import assert from "node:assert/strict";
import test from "node:test";

import {
    CompositeLab, ExecutionException, extend, Lab, merge, MissingExpectedException, Model, Node, NodeStatus,
} from "../../dist/index.js";

async function execute(lab, model = new Model(), signal) {
    const node = new Node(() => model, lab);
    await node.execute(signal);
    return node;
}

test("awaits every AAA stage in order", async () => {
    // TU03
    const controller = new AbortController();
    const lab = new Lab("async", {
        arranger: async (model, _metadata, signal) => {
            assert.equal(signal, controller.signal);
            await Promise.resolve();
            model.subject = 2;
        },
        actor: async (model, _metadata, signal) => {
            assert.equal(signal, controller.signal);
            assert.equal(model.subject, 2);
            await Promise.resolve();
            model.subject *= 3;
        },
        asserter: async (model, _metadata, signal) => {
            assert.equal(signal, controller.signal);
            await Promise.resolve();
            assert.equal(model.subject, 6);
        },
    });
    const result = await execute(lab, new Model(0), controller.signal);
    assert.equal(result.status, NodeStatus.Success);
    assert.equal(result.model.subject, 6);
});

test("preserves rejection from each AAA stage", async t => {
    // TU04
    const stages = ["arranger", "actor", "asserter"];

    for (const [index, stage] of stages.entries()) {
        await t.test(stage, async () => {
            const rejection = new Error(stage + " rejected");
            const observed = [];
            const options = Object.fromEntries(stages.map(current => [current, async () => {
                await Promise.resolve();
                observed.push(current);

                if (current === stage) throw rejection;
            }]));
            const result = await execute(new Lab(stage, options));
            assert.equal(result.status, NodeStatus.Failure);
            assert.ok(result.exception instanceof ExecutionException);
            assert.equal(result.exception.cause, rejection);
            assert.deepEqual(observed, stages.slice(0, index + 1));
        });
    }
});

test("checks matching, mismatching, and missing expected errors", async t => {
    // TU05
    class ExpectedError extends Error {}
    class DerivedError extends ExpectedError {}

    const cases = [
        { name: "matching subtype", thrown: new DerivedError(), expectedStatus: NodeStatus.Success },
        { name: "unexpected error", thrown: new TypeError("other"), expectedStatus: NodeStatus.Failure },
        { name: "missing error", thrown: null, expectedStatus: NodeStatus.Failure },
    ];

    for (const { name, thrown, expectedStatus } of cases) {
        await t.test(name, async () => {
            let assertions = 0;
            const lab = new Lab("expected", {
                expectedExceptionType: ExpectedError,
                actor: async () => {
                    await Promise.resolve();

                    if (thrown) throw thrown;
                },
                asserter: () => { assertions++; },
            });
            const result = await execute(lab);
            assert.equal(result.status, expectedStatus);
            assert.equal(assertions, expectedStatus === NodeStatus.Success ? 1 : 0);

            if (!thrown) assert.ok(result.exception.cause instanceof MissingExpectedException);
        });
    }
});

test("preserves Merge and Extend callback order", async () => {
    // TU06
    const observed = [];
    const namedLab = name => new Lab(name, {
        setMetadata: () => { observed.push("metadata-" + name); },
        arranger: () => { observed.push("arrange-" + name); },
        actor: async () => {
            await Promise.resolve();
            observed.push("act-" + name);
        },
        asserter: () => { observed.push("assert-" + name); },
    });
    const original = namedLab("original");
    const first = namedLab("first");
    const second = namedLab("second");
    const merged = await execute(merge(original, first));
    assert.equal(merged.status, NodeStatus.Success);
    assert.deepEqual(observed.splice(0), [
        "metadata-first", "metadata-original", "arrange-first", "arrange-original",
        "act-first", "act-original", "assert-first", "assert-original",
    ]);

    const extended = await execute(extend(second, new CompositeLab(original, first)));
    assert.equal(extended.status, NodeStatus.Success);
    assert.deepEqual(observed, [
        "metadata-original", "arrange-original", "metadata-first", "arrange-first", "metadata-second", "arrange-second",
        "act-original", "assert-second", "assert-first", "assert-original",
    ]);
});

test("requires all composite error expectations to match", async () => {
    // TU07
    const thrown = new TypeError("expected type");
    const original = new Lab("original", {
        expectedExceptionType: TypeError,
        actor: () => { throw thrown; },
    });
    let assertions = 0;
    const matches = await execute(new CompositeLab(original, new Lab("compatible", {
        expectedExceptionType: Error,
        asserter: () => { assertions++; },
    })));
    assert.equal(matches.status, NodeStatus.Success);
    assert.equal(assertions, 1);

    const mismatches = await execute(new CompositeLab(original, new Lab("incompatible", {
        expectedExceptionType: RangeError,
        asserter: () => { assertions++; },
    })));
    assert.equal(mismatches.status, NodeStatus.Failure);
    assert.ok(mismatches.exception.cause instanceof MissingExpectedException);
    assert.equal(mismatches.exception.cause.cause, thrown);
    assert.equal(assertions, 1);
});
