using System;
using System.Linq;
using System.Collections.Generic;

namespace UniTest
{
    public readonly struct TestCase
    {
        // Internal
        readonly (bool include, object[] definitions)[] TestCases => _testCases ?? Array.Empty<(bool, object[])>();
        readonly (bool, object[])[] _testCases;


        // Control
        public readonly object this[int i]
        {
            get
            {
                if (TestCases.Length <= i)
                    throw new ArgumentOutOfRangeException(nameof(i), $"Index must be in range [0..{TestCases.Length - 1}] but was {i}");

                var (include, defs) = TestCases[i];

                if (!include || defs.Length != 1)
                    throw new InvalidOperationException("Test case at index must be a single confined definition");

                return defs[0];
            }
        }
        public int Count => TestCases.Length;


        // Content
        public TestCase(params object[] testCases) : this(testCases.Select(tc => (true, new object[] { tc })).ToArray()) { }
        public TestCase(IEnumerable<(bool include, object[] definitions)> testCases)
        {
            _testCases = testCases.ToArray();
        }

        public readonly TestCase Append(object definition, bool include = true)
        {
            return new TestCase(TestCases.Append((include, new object[] { definition })));
        }
        public readonly TestCase Confine(int index, object definition)
        {
            if (index < 0 || index > TestCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{TestCases.Length}] but was {index}");
            if (definition == null)
                throw new ArgumentNullException(nameof(definition), "The definition must not be null.");

            if (index == TestCases.Length)
                return Append(definition, true);

            var tc = new TestCase(TestCases.Select(tc => (tc.include, tc.definitions.ToArray())));
            tc.TestCases[index] = (true, new object[] { definition });

            return tc;
        }

        public readonly TestCase Include(int index, params object[] definitions) => Set(index, definitions, true);
        public readonly TestCase Exclude(int index, params object[] definitions) => Set(index, definitions, false);
        TestCase Set(int index, object[] definitions, bool include)
        {
            if (index < 0 || index > TestCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{TestCases.Length}] but was {index}");
            if (definitions.Length == 0 || definitions.Any(d => d == null))
                throw new ArgumentNullException(nameof(definitions), "Definitions must not be empty or contain null elements.");

            var updatedTestCases = TestCases.Select(tc => (tc.include, tc.definitions.ToArray()));

            if (index == TestCases.Length)
                return new TestCase(updatedTestCases.Append((include, definitions)));

            var tc = new TestCase(updatedTestCases);
            var (currentInclude, currentDefs) = tc.TestCases[index];

            var updatedDefs = currentInclude switch
            {
                true when include => currentDefs.Union(definitions),
                true when !include => currentDefs.Except(definitions),
                false when include => currentDefs.Except(definitions),
                false when !include => currentDefs.Union(definitions),
                _ => throw new Exception()
            };

            tc.TestCases[index] = (currentInclude, updatedDefs.ToArray());
            return tc;
        }


        public readonly bool Confineable(int index, out TestCase confined, object definition)
        {
            if (!Confineable(index, definition))
            {
                confined = default;
                return false;
            }

            confined = Confine(index, definition);
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

            var (include, defs) = TestCases[index];

            if (include)
                return definitions.All(d => defs.Contains(d));
            else
                return definitions.All(d => !defs.Contains(d));
        }

        public readonly bool ConfineableExcept(int index, out TestCase confined, params object[] definitions)
        {
            if (index < 0 || index > TestCases.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be in range [0..{TestCases.Length}] but was {index}");
            if (definitions.Length == 0 || definitions.Any(d => d == null))
                throw new ArgumentException("Definitions must not be empty or contain null elements.", nameof(definitions));

            if (index == TestCases.Length)
            {
                confined = new TestCase(TestCases
                    .Select(tc => (tc.include, tc.definitions.ToArray()))
                    .Append((false, definitions)));

                return true;
            }

            var (include, defs) = TestCases[index];

            if (include)
            {
                if (defs.Any(d => !definitions.Contains(d)))
                {
                    confined = new TestCase(TestCases.Select(tc => (tc.include, tc.definitions.ToArray())));
                    confined.TestCases[index] = (true, confined.TestCases[index].definitions.Except(definitions).ToArray());

                    return true;
                }

                confined = default;
                return false;
            }
            else
            {
                confined = new TestCase(TestCases.Select(tc => (tc.include, tc.definitions.ToArray())));
                confined.TestCases[index] = (false, confined.TestCases[index].definitions.Union(definitions).ToArray());

                return true;
            }    
        }
    }
}
