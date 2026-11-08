import { CompactLab, CompactProject, Lab, Model, Project, extend, merge } from "../dist/index.js";

class CounterModel extends Model {
    expectedCount = 0;
    expectedLocked = false;

    constructor() {
        super({ count: 0, locked: false });
    }

    toString() {
        return `count=${this.subject.count}, locked=${this.subject.locked}`;
    }
}

function checkCount(model) {
    if (model.subject.count !== model.expectedCount) {
        throw new Error(`Expected count ${model.expectedCount}, got ${model.subject.count}.`);
    }
}

function checkLock(model) {
    if (model.subject.locked !== model.expectedLocked) {
        throw new Error(`Expected locked=${model.expectedLocked}, got ${model.subject.locked}.`);
    }
}

class CounterProject extends CompactProject {
    constructor() {
        super(() => new CounterModel());
    }

    *createCompactLabs(model) {
        if (model.subject.count < 3) {
            yield new CompactLab("increment", {
                arranger: current => { current.expectedCount++; },
                actor: current => { current.subject.count++; },
                asserter: checkCount,
            });
        }

        if (model.subject.count > 0) {
            yield new CompactLab("decrement", {
                arranger: current => { current.expectedCount--; },
                actor: current => { current.subject.count--; },
                asserter: checkCount,
            });
        }
    }
}

class LockingCounterProject extends Project {
    constructor() {
        super(() => new CounterModel());
    }

    *createLabs(model) {
        const nextLocked = !model.subject.locked;
        yield new Lab(nextLocked ? "lock" : "unlock", {
            arranger: current => { current.expectedLocked = nextLocked; },
            actor: current => { current.subject.locked = nextLocked; },
            asserter: current => {
                checkCount(current);
                checkLock(current);
            },
        });

        if (!model.subject.locked && model.subject.count < 3) {
            const count = new Lab("increment", {
                arranger: current => { current.expectedCount++; },
                asserter: checkCount,
            });
            const action = new Lab("increment-actor", {
                actor: current => { current.subject.count++; },
            });
            const unlocked = new Lab("unlocked", { asserter: checkLock });
            yield extend(unlocked, merge(count, action));
        }
    }
}

class AsyncCounterProject extends CompactProject {
    constructor(cancel = false) {
        super(() => new CounterModel());
        this.cancelAfterAct = cancel;
    }

    *createCompactLabs() {
        yield new CompactLab("async-increment", {
            arranger: async current => {
                await Promise.resolve();
                current.expectedCount++;
            },
            actor: async (current, signal) => {
                await new Promise(resolve => setTimeout(resolve, 0));
                signal.throwIfAborted();
                current.subject.count++;

                if (this.cancelAfterAct) this.cancel();
            },
            asserter: async current => {
                await Promise.resolve();
                checkCount(current);
            },
        });
    }
}

export const scenarios = new Map([
    ["explore", () => new CounterProject().execute(3)],
    ["replay", () => new CounterProject().execute("increment/increment/decrement")],
    ["compose", () => new LockingCounterProject().execute(3)],
    ["continuous", () => new CounterProject().executeContinuously(8, { seed: 42 })],
    ["async", () => new AsyncCounterProject().execute(2)],
    ["cancel", () => new AsyncCounterProject(true).execute(3)],
]);
