using System;

namespace UniTest
{
    public static class SeedGenerator
    {
        /// <summary>
        /// Computes a deterministic 32-bit integer from a string and an extra integer seed.<br/>
        /// Same inputs -> same output.
        /// </summary>
        public static int GetDeterministicRandom(this string s, int seed = 0)
        {
            s ??= string.Empty;
            s = s.ToUpperInvariant();

            unchecked
            {
                const uint offset = 2166136261u;  // FNV offset basis
                const uint prime = 16777619u;     // FNV prime

                // Scramble the external seed to improve distribution.
                uint mix = (uint)seed;
                mix ^= mix >> 16; mix *= 0x7feb352d;
                mix ^= mix >> 15; mix *= 0x846ca68b;
                mix ^= mix >> 16;

                // Start from FNV offset mixed with the scrambled seed.
                uint hash = offset ^ mix;

                // FNV-1a over UTF-16 code units.
                foreach (char ch in s)
                {
                    hash ^= ch;
                    hash *= prime;
                }

                // Finalize by folding the seed in once more.
                hash ^= mix;
                hash *= prime;

                return (int)hash; // Negative values are fine for Random(seed), etc.
            }
        }


        /// <summary>
        /// Returns a deterministic random integer in [minInclusive, maxExclusive).<br/>
        /// Same inputs -> same output.
        /// </summary>
        public static int GetDeterministicRandom(this string s, int minInclusive, int maxExclusive, int seed = 0)
        {
            if (minInclusive >= maxExclusive)
                throw new ArgumentException("minInclusive must be less than maxExclusive.");

            // Combine string + seed into a deterministic 32-bit value.
            uint combinedSeed = (uint)GetDeterministicRandom(s, seed);

            // Local XorShift32 PRNG (in-method, no external type).
            unchecked
            {
                uint state = combinedSeed == 0 ? 2463534242u : combinedSeed;

                // Local function capturing 'state'; performs 32-bit modular arithmetic.
                uint NextUInt()
                {
                    state ^= state << 13;
                    state ^= state >> 17;
                    state ^= state << 5;
                    return state;
                }

                // Draw an unbiased integer in [min, max) via rejection sampling (no modulo bias).
                uint range = (uint)(maxExclusive - minInclusive);
                uint threshold = uint.MaxValue - (uint.MaxValue % range);

                uint r;
                do
                {
                    r = NextUInt();
                } while (r >= threshold);

                return minInclusive + (int)(r % range);
            }
        }
    }
}
