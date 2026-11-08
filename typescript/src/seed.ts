const Int32Min = -2147483648;
const Int32Max = 2147483647;
const Uint32Max = 0xffffffff;
const FnvOffset = 2166136261;
const FnvPrime = 16777619;
const NonzeroState = 2463534242;

function requireInt32(value: number, name: string): void {
    if (!Number.isInteger(value) || value < Int32Min || value > Int32Max) {
        throw new RangeError(`${name} must be a signed 32-bit integer.`);
    }
}

/** Hashes the original UTF-16 code units using the same arithmetic as UniTest for C#. */
export function deterministicHash(history: string | null, seed = 0): number {
    requireInt32(seed, "seed");

    let mix = seed >>> 0;
    mix = Math.imul(mix ^ (mix >>> 16), 0x7feb352d);
    mix = Math.imul(mix ^ (mix >>> 15), 0x846ca68b);
    mix ^= mix >>> 16;

    const input = history ?? "";
    let hash = FnvOffset ^ mix;
    for (let index = 0; index < input.length; index++) {
        hash = Math.imul(hash ^ input.charCodeAt(index), FnvPrime);
    }

    return Math.imul(hash ^ mix, FnvPrime);
}

/** Returns a deterministic integer in [minInclusive, maxExclusive). */
export function deterministicRandom(
    history: string | null,
    minInclusive: number,
    maxExclusive: number,
    seed = 0,
): number {
    requireInt32(minInclusive, "minInclusive");
    requireInt32(maxExclusive, "maxExclusive");
    if (minInclusive >= maxExclusive) {
        throw new RangeError("minInclusive must be less than maxExclusive.");
    }

    let state = (deterministicHash(history, seed) >>> 0) || NonzeroState;
    const range = maxExclusive - minInclusive;
    const threshold = Uint32Max - (Uint32Max % range);
    let sample: number;
    do {
        state ^= state << 13;
        state ^= state >>> 17;
        state ^= state << 5;
        sample = state >>> 0;
    } while (sample >= threshold);

    return minInclusive + (sample % range);
}

/** Compares strings by UTF-16 code unit, independently of the current locale. */
export function compareOrdinal(left: string, right: string): number {
    if (left < right) {
        return -1;
    }

    return left === right ? 0 : 1;
}
