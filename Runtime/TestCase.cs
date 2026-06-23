using System;
using System.Linq;
using System.Collections.Generic;

namespace UniTest
{
    public readonly struct TestCase
    {
        // Internal
        readonly (bool include, object[] definitions)[] testCases => _testCases ?? Array.Empty<(bool, object[])>();
        readonly (bool, object[])[] _testCases;


        // Control
        public readonly object this[int i]
        {
            get
            {
                if (testCases.Length <= i)
                    throw new ArgumentOutOfRangeException(nameof(i), $"Index must be in range [0..{testCases.Length - 1}] but was {i}");

                var (include, defs) = testCases[i];

                if (!include || defs.Length != 1)
                    throw new InvalidOperationException("Test case at index must be a single confined definition");

                return defs[0];
            }
        }
        public int Count => testCases.Length;


        // Content
        public TestCase(params object[] testCases) : this(testCases.Select(tc => (true, new object[] { tc })).ToArray()) { }
        public TestCase(IEnumerable<(bool include, object[] definitions)> testCases)
        {
            _testCases = testCases.ToArray();
        }

        public readonly TestCase Append(object definition, bool include = true)
        {
            return new TestCase(testCases.Append((include, new object[] { definition })));
        }
        public readonly TestCase Confine(int index, object definition)
        {
            if (index < 0 || index > testCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{testCases.Length}] but was {index}");
            if (definition == null)
                throw new ArgumentNullException(nameof(definition), "The definition must not be null.");

            if (index == testCases.Length)
                return Append(definition, true);

            var tc = new TestCase(testCases.Select(tc => (tc.include, tc.definitions.ToArray())));
            tc.testCases[index] = (true, new object[] { definition });

            return tc;
        }

        public readonly TestCase Include(int index, params object[] definitions) => Set(index, definitions, true);
        public readonly TestCase Exclude(int index, params object[] definitions) => Set(index, definitions, false);
        TestCase Set(int index, object[] definitions, bool include)
        {
            if (index < 0 || index > testCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{testCases.Length}] but was {index}");
            if (definitions.Length == 0 || definitions.Any(d => d == null))
                throw new ArgumentNullException(nameof(definitions), "Definitions must not be empty or contain null elements.");

            var updatedTestCases = testCases.Select(tc => (tc.include, tc.definitions.ToArray()));

            if (index == testCases.Length)
                return new TestCase(updatedTestCases.Append((include, definitions)));

            var tc = new TestCase(updatedTestCases);
            var (currentInclude, currentDefs) = tc.testCases[index];

            var updatedDefs = currentInclude switch
            {
                true when include => currentDefs.Union(definitions),
                true when !include => currentDefs.Except(definitions),
                false when include => currentDefs.Except(definitions),
                false when !include => currentDefs.Union(definitions),
                _ => throw new Exception()
            };

            tc.testCases[index] = (currentInclude, updatedDefs.ToArray());
            return tc;
        }


        public readonly bool Confineable(int index, out TestCase confied, object definition)
        {
            if (!Confineable(index, definition))
            {
                confied = default;
                return false;
            }

            confied = Confine(index, definition);
            return true;
        }

        public readonly bool Confineable(int index, params object[] definitions)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be greater than or equal to 0 but was {index}");
            if (definitions.Length == 0 || definitions.Any(d => d == null))
                throw new ArgumentNullException(nameof(definitions), "Definitions must not be empty or contain null elements.");

            if (Count <= index)
                return true;

            var (include, defs) = testCases[index];

            if (include)
                return definitions.All(d => defs.Contains(d));
            else
                return definitions.All(d => !defs.Contains(d));
        }

        public readonly bool ConfineableExcept(int index, out TestCase confined, params object[] definitions)
        {
            if (index < 0 || index > testCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{testCases.Length}] but was {index}");
            if (definitions.Length == 0 || definitions.Any(d => d == null))
                throw new ArgumentException("Definitions must not be empty or contain null elements.", nameof(definitions));

            if (index == testCases.Length)
            {
                confined = new TestCase(testCases
                    .Select(tc => (tc.include, tc.definitions.ToArray()))
                    .Append((false, definitions)));

                return true;
            }

            var (include, defs) = testCases[index];

            if (include)
            {
                if (defs.Any(d => !definitions.Contains(d)))
                {
                    confined = new TestCase(testCases.Select(tc => (tc.include, tc.definitions.ToArray())));
                    confined.testCases[index] = (true, confined.testCases[index].definitions.Except(definitions).ToArray());

                    return true;
                }

                confined = default;
                return false;
            }
            else
            {
                confined = new TestCase(testCases.Select(tc => (tc.include, tc.definitions.ToArray())));
                confined.testCases[index] = (false, confined.testCases[index].definitions.Union(definitions).ToArray());

                return true;
            }    
        }
    }
}
