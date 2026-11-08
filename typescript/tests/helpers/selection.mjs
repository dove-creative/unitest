import { compareOrdinal, deterministicHash, deterministicRandom, Lab, Model, Project } from "../../dist/index.js";

function check(actual, expected, name) {
    if (JSON.stringify(actual) !== JSON.stringify(expected)) {
        throw new Error(name + ": expected " + JSON.stringify(expected) + ", received " + JSON.stringify(actual));
    }
}

// TU13: Fixed vectors are shared with C# and run unchanged in Node.js and browsers.
export function verifySelectionVectors(vectors) {
    for (const [index, vector] of vectors.hashes.entries()) {
        const history = vector.utf16 ? String.fromCharCode(...vector.utf16) : vector.history;
        check(deterministicHash(history, vector.seed), vector.expected, "hash vector " + index);
    }

    for (const [index, vector] of vectors.ranges.entries()) {
        const { history, minInclusive, maxExclusive, seed, expected } = vector;
        check(deterministicRandom(history, minInclusive, maxExclusive, seed), expected, "range vector " + index);
    }

    for (const [index, vector] of vectors.ordering.entries()) {
        const indices = vector.ids.map((_, candidateIndex) => candidateIndex);
        indices.sort((left, right) => compareOrdinal(vector.ids[left], vector.ids[right]));
        check(indices, vector.expectedIndices, "ordering vector " + index);
    }

    return vectors.hashes.length + vectors.ranges.length + vectors.ordering.length;
}

class CandidateProject extends Project {
    constructor(ids) {
        super(() => new Model([]));
        this.ids = ids;
    }

    createLabs() {
        return this.ids.map((id, index) => new Lab(id, {
            actor: model => { model.subject.push(index); },
        }));
    }
}

// TR03: The last explored batch tests every candidate and does not select a child.
export async function verifySelectionPaths(vectors) {
    let processed = 0;

    for (const [index, vector] of vectors.paths.entries()) {
        const project = new CandidateProject(vector.ids);
        const root = await project.executeContinuously(vector.depth, { seed: vector.seed });
        const pending = [root];
        let deepest = root;

        while (pending.length > 0) {
            const current = pending.pop();

            if (current.depth > deepest.depth) deepest = current;

            pending.push(...current.afters);
        }

        const selected = deepest.before?.model.subject ?? [];
        const expectedProcessed = vector.depth * vector.ids.length;
        check(deepest.depth, vector.depth, "deepest node for path vector " + index);
        check(selected, vector.expectedIndices, "chosen path vector " + index);
        check(project.processCount, expectedProcessed, "processed candidates for path vector " + index);
        check(root.allSucceed(), { succeeded: true, cancelled: false }, "execution status for path vector " + index);
        processed += project.processCount;
    }

    return processed;
}
