using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;
using TargetCase = BlackThunder.UniTest.TestCase;

namespace BlackThunder.UniTest.Tests.Recursion.UniTest
{
    public class TestCaseTest
    {
        // Internal
        private const int DomainSize = 5;
        private const int AllValues = (1 << DomainSize) - 1;
        private const int ContinuousDepth = 16;

        internal enum ConditionState
        {
            Unbound,
            Single,
            Inclusive,
            Exclusive
        }

        internal readonly struct ExpectedAxis
        {
            public bool Inclusive { get; }
            public int Allowed { get; }

            public ExpectedAxis(bool inclusive, int allowed) => (Inclusive, Allowed) = (inclusive, allowed);
        }

        internal class Snapshot
        {
            public TargetCase Value { get; }
            public ExpectedAxis[] Axes { get; }

            public Snapshot(TargetCase value, IEnumerable<ExpectedAxis> axes) =>
                (Value, Axes) = (value, axes.ToArray());
        }

        public class CaseModel : Model
        {
            internal bool Initialized;
            internal TargetCase Current;
            internal ConditionState PreviousState;
            internal List<ExpectedAxis> Expected { get; } = new();
            internal List<Snapshot> Earlier { get; } = new();
        }

        private class SequenceProject : Project<CaseModel>
        {
            private readonly bool _wrongOracle;
            private readonly ConcurrentDictionary<string, byte> _covered = new();

            public IEnumerable<string> Covered => _covered.Keys;


            public SequenceProject(
                ExecutionMode mode = BlackThunder.UniTest.ExecutionMode.Inline,
                bool wrongOracle = false) : base(mode) => _wrongOracle = wrongOracle;

            // TC-state-action: every initial state offers each of the six action groups.
            public override IEnumerable<ILab<CaseModel>> CreateLabs(CaseModel model)
            {
                if (!model.Initialized)
                {
                    foreach (var state in Enum.GetValues(typeof(ConditionState)).Cast<ConditionState>())
                    {
                        yield return new Lab<CaseModel>(state.ToString())
                        {
                            Actor = (m, _) => Initialize(m, state, _wrongOracle),
                            Asserter = (m, _) => CheckModel(m)
                        };
                    }

                    yield break;
                }

                if (model.Expected.Count < 3)
                {
                    yield return Operation("AppendIncluded", "Append", m => Append(m, 0, true));
                    yield return Operation("AppendExcluded", "Append", m => Append(m, 1, false));
                }

                yield return Operation("Include02", "Set", m => Set(m, true, 0, 2));
                yield return Operation("Exclude01", "Set", m => Set(m, false, 0, 1));
                yield return Operation("Confine0", "Confine", m => Confine(m, 0));
                yield return Operation("Confine3", "Confine", m => Confine(m, 3));
                yield return Operation("Query", "Query", m => CheckCase(m.Current, m.Expected));
                yield return Operation("TryConfine0", "TryConfine", m => TryConfine(m, 0));
                yield return Operation("TryConfine2", "TryConfine", m => TryConfine(m, 2));
                yield return Operation("TryExcept0", "TryExcept", m => TryExcept(m, 0));
                yield return Operation("TryExceptAll", "TryExcept", m => TryExcept(m, 0, 1, 2, 3));
            }

            private Lab<CaseModel> Operation(string id, string group, Action<CaseModel> action)
            {
                return new Lab<CaseModel>(id)
                {
                    Arranger = (m, _) =>
                    {
                        m.PreviousState = GetState(m);
                        m.Earlier.Add(new Snapshot(m.Current, m.Expected));
                    },
                    Actor = (m, _) => action(m),
                    Asserter = (m, _) =>
                    {
                        CheckModel(m);
                        _covered.TryAdd($"TC-{m.PreviousState}-{group}", 0);
                    }
                };
            }
        }

        private class BoundaryProject : Project<CaseModel>
        {
            public BoundaryProject() : base(BlackThunder.UniTest.ExecutionMode.Inline) { }

            public override IEnumerable<ILab<CaseModel>> CreateLabs(CaseModel model)
            {
                if (model.ExecutionCount > 0)
                    yield break;

                foreach (var boundary in Boundaries())
                {
                    yield return new Lab<CaseModel>(boundary.Id)
                    {
                        Actor = (m, _) =>
                        {
                            m.Current = new TargetCase(0, 1);
                            m.Expected.Add(new ExpectedAxis(true, 1 << 0));
                            m.Expected.Add(new ExpectedAxis(true, 1 << 1));
                            boundary.Check(m.Current);
                        },
                        Asserter = (m, _) => CheckModel(m)
                    };
                }
            }
        }


        // Content --------------------
        // Execution
        // TC-state-action: all 24 cells and both execution modes must actually execute.
        [TestCase(ExecutionMode.Inline), TestCase(ExecutionMode.ThreadPool)]
        public async Task BoundedExplorationCoversEveryStateOperation(ExecutionMode mode)
        {
            var project = new SequenceProject(mode);
            var result = await project.Execute(3, stopOnFailure: false);

            CheckSuccessfulExecution(result, 3);
            Assert.That(result.GetCount(), Is.EqualTo(533));

            var groups = new[] { "Append", "Set", "Confine", "Query", "TryConfine", "TryExcept" };
            var expected = Enum.GetNames(typeof(ConditionState))
                .SelectMany(state => groups.Select(group => $"TC-{state}-{group}"));

            Assert.That(project.Covered, Is.EquivalentTo(expected));
        }

        // TC-state-action: preserve every earlier value across seeded, repeated operations.
        [TestCase(0), TestCase(7), TestCase(23)]
        public async Task ContinuousSequencesPreserveEarlierValues(int seed)
        {
            var project = new SequenceProject();
            var result = await project.ExecuteContinuously(ContinuousDepth, seed, stopOnFailure: false);

            CheckSuccessfulExecution(result, ContinuousDepth);
            Assert.That(result.GetCount(), Is.GreaterThan(ContinuousDepth));
            Assert.That(project.Covered.Count(), Is.GreaterThanOrEqualTo(6));
        }

        // TC-state-action: replay mixed changes, rejected Try calls, and an empty inclusion set.
        [TestCase(
            "Unbound/AppendIncluded/Include02/TryExcept0/TryConfine2/AppendExcluded/Exclude01/Confine3/Query",
            3,
            2)]
        [TestCase("Exclusive/Include02/Exclude01/TryConfine2/TryExceptAll/Confine0/AppendIncluded/Query", 0, 2)]
        [TestCase("Single/TryConfine2/TryExceptAll/Exclude01/Include02/TryExcept0/TryConfine2/Query", 2, 1)]
        public async Task MixedPathsKeepIndependentExpectations(string path, int firstValue, int axisCount)
        {
            var result = await new SequenceProject().Execute(path, stopOnFailure: false);
            var depth = path.Split('/').Length;

            CheckSuccessfulExecution(result, depth);
            Assert.That(result.GetCount(), Is.EqualTo(depth + 1));

            var last = result.GetLastNode();
            Assert.That(last.Model.Current[0], Is.EqualTo(firstValue));
            Assert.That(last.Model.Current.Count, Is.EqualTo(axisCount));
            Assert.That(last.Model.Earlier.Count, Is.EqualTo(depth - 1));
        }

        // TC-B01..TC-B11: every invalid-input Lab must execute and preserve the input value.
        [Test]
        public async Task InvalidInputsPreserveTheOriginal()
        {
            var result = await new BoundaryProject().Execute(1, stopOnFailure: false);

            CheckSuccessfulExecution(result, 1);
            Assert.That(result.Afters.Select(n => n.Id), Is.EquivalentTo(Boundaries().Select(b => b.Id)));
            Assert.That(result.GetCount(), Is.EqualTo(12));
        }

        // TC-F01: a deliberately wrong independent expectation must not pass silently.
        [Test]
        public async Task WrongOracleIsReportedAsFailure()
        {
            Node<CaseModel> result;

            using (new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext())
            {
                result = await new SequenceProject(wrongOracle: true).Execute("Single", stopOnFailure: false);
            }

            Assert.That(result.AllSucceed(out var cancelled), Is.False);
            Assert.That(cancelled, Is.False);

            var failed = result.Afters.Single();
            Assert.That(failed.Status, Is.EqualTo(Node<CaseModel>.NodeStatus.Failure));
            Assert.That(failed.Exception.GetBaseException(), Is.TypeOf<AssertionException>());
        }

        private static void CheckSuccessfulExecution(Node<CaseModel> result, int expectedDepth)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);

            var nodes = result.GetReport().Nodes;
            Assert.That(nodes.Skip(1).Select(n => n.Status), Is.All.EqualTo("Success"));
            Assert.That(nodes.Max(n => n.Depth), Is.EqualTo(expectedDepth));
        }

        // Independent expectations
        private static void Initialize(CaseModel model, ConditionState state, bool wrongOracle)
        {
            model.Initialized = true;

            switch (state)
            {
                case ConditionState.Unbound:
                    model.Current = default;
                    break;

                case ConditionState.Single:
                    model.Current = new TargetCase(0);
                    model.Expected.Add(new ExpectedAxis(true, wrongOracle ? 1 << 1 : 1 << 0));
                    break;

                case ConditionState.Inclusive:
                    model.Current = new TargetCase(new[] { (true, new object[] { 0, 1 }) });
                    model.Expected.Add(new ExpectedAxis(true, (1 << 0) | (1 << 1)));
                    break;

                case ConditionState.Exclusive:
                    model.Current = new TargetCase(new[] { (false, new object[] { 1, 2 }) });
                    model.Expected.Add(new ExpectedAxis(false, AllValues & ~((1 << 1) | (1 << 2))));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static ConditionState GetState(CaseModel model)
        {
            if (model.Expected.Count == 0)
                return ConditionState.Unbound;

            var axis = model.Expected[0];

            if (!axis.Inclusive)
                return ConditionState.Exclusive;

            return IsSingle(axis.Allowed) ? ConditionState.Single : ConditionState.Inclusive;
        }

        private static bool IsSingle(int mask) => mask != 0 && (mask & (mask - 1)) == 0;

        private static int Mask(IEnumerable<int> values)
        {
            var mask = 0;

            foreach (var value in values)
                mask |= 1 << value;

            return mask;
        }

        private static void ReplaceFirst(CaseModel model, ExpectedAxis axis)
        {
            if (model.Expected.Count == 0)
                model.Expected.Add(axis);
            else
                model.Expected[0] = axis;
        }

        // TC-*-Append: append independently of all existing axes.
        private static void Append(CaseModel model, int value, bool include)
        {
            var bit = 1 << value;
            model.Current = model.Current.Append(value, include);
            model.Expected.Add(new ExpectedAxis(include, include ? bit : AllValues & ~bit));
        }

        // TC-*-Set: update allowed bits without using target results to calculate expectations.
        private static void Set(CaseModel model, bool include, params int[] values)
        {
            var mask = Mask(values);
            var axis = model.Expected.Count == 0
                ? new ExpectedAxis(include, include ? 0 : AllValues)
                : model.Expected[0];
            var allowed = include ? axis.Allowed | mask : axis.Allowed & ~mask;
            var definitions = values.Cast<object>().ToArray();

            model.Current = include
                ? model.Current.Include(0, definitions)
                : model.Current.Exclude(0, definitions);
            ReplaceFirst(model, new ExpectedAxis(axis.Inclusive, allowed));
        }

        // TC-*-Confine: confinement replaces the condition even when the old one excluded the value.
        private static void Confine(CaseModel model, int value)
        {
            model.Current = model.Current.Confine(0, value);
            ReplaceFirst(model, new ExpectedAxis(true, 1 << value));
        }

        // TC-*-TryConfine: rejection returns default and leaves the current case unchanged.
        private static void TryConfine(CaseModel model, int value)
        {
            var expected = model.Expected.Count == 0 || (model.Expected[0].Allowed & (1 << value)) != 0;
            var actual = model.Current.Confineable(0, out var confined, value);
            Assert.That(actual, Is.EqualTo(expected));

            if (!expected)
            {
                CheckCase(confined, Array.Empty<ExpectedAxis>());
                return;
            }

            model.Current = confined;
            ReplaceFirst(model, new ExpectedAxis(true, 1 << value));
        }

        // TC-*-TryExcept: inclusion can become unsatisfiable; exclusion keeps an open domain.
        private static void TryExcept(CaseModel model, params int[] values)
        {
            var axis = model.Expected.Count == 0 ? new ExpectedAxis(false, AllValues) : model.Expected[0];
            var remaining = axis.Allowed & ~Mask(values);
            var expected = !axis.Inclusive || remaining != 0;
            var actual = model.Current.ConfineableExcept(0, out var confined, values.Cast<object>().ToArray());
            Assert.That(actual, Is.EqualTo(expected));

            if (!expected)
            {
                CheckCase(confined, Array.Empty<ExpectedAxis>());
                return;
            }

            model.Current = confined;
            ReplaceFirst(model, new ExpectedAxis(axis.Inclusive, remaining));
        }

        // TC-*-Query and preservation checks use only public observations of every saved value.
        private static void CheckModel(CaseModel model)
        {
            CheckCase(model.Current, model.Expected);

            foreach (var earlier in model.Earlier)
                CheckCase(earlier.Value, earlier.Axes);
        }

        private static void CheckCase(TargetCase value, IReadOnlyList<ExpectedAxis> expected)
        {
            Assert.That(value.Count, Is.EqualTo(expected.Count));

            for (var index = 0; index < expected.Count; index++)
            {
                var axis = expected[index];

                for (var candidate = 0; candidate < DomainSize; candidate++)
                {
                    var allowed = (axis.Allowed & (1 << candidate)) != 0;
                    Assert.That(value.Confineable(index, candidate), Is.EqualTo(allowed));
                }

                Assert.That(value.Confineable(index, 0, 2), Is.EqualTo((axis.Allowed & 5) == 5));
                Assert.That(value.Confineable(index, 3, 4), Is.EqualTo((axis.Allowed & 24) == 24));

                if (axis.Inclusive && IsSingle(axis.Allowed))
                {
                    var single = Enumerable.Range(0, DomainSize).Single(v => (axis.Allowed & (1 << v)) != 0);
                    Assert.That(value[index], Is.EqualTo(single));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => { var ignored = value[index]; });
                }
            }

            Assert.That(value.Confineable(expected.Count, 0, 4), Is.True);
            Assert.That(value.Confineable(expected.Count + 1, 0, 4), Is.True);
        }

        // Boundary cases
        private static IEnumerable<(string Id, Action<TargetCase> Check)> Boundaries()
        {
            // TC-B01
            yield return ("TC-B01", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Confine(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Confine(value.Count + 1, 0));
            });

            // TC-B02
            yield return ("TC-B02", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Include(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Include(value.Count + 1, 0));
            });

            // TC-B03
            yield return ("TC-B03", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Exclude(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Exclude(value.Count + 1, 0));
            });

            // TC-B04
            yield return ("TC-B04", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Confineable(-1, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => value.Confineable(-1, out _, 0));
            });

            // TC-B05
            yield return ("TC-B05", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => value.ConfineableExcept(-1, out _, 0));
                Assert.Throws<ArgumentOutOfRangeException>(() => value.ConfineableExcept(value.Count + 1, out _, 0));
            });

            // TC-B06
            yield return ("TC-B06", value =>
            {
                Assert.Throws<ArgumentNullException>(() => value.Confine(0, null));
            });

            // TC-B07
            yield return ("TC-B07", value =>
            {
                Assert.Throws<ArgumentNullException>(() => value.Include(0, Array.Empty<object>()));
                Assert.Throws<ArgumentNullException>(() => value.Include(0, new object[] { null }));
            });

            // TC-B08
            yield return ("TC-B08", value =>
            {
                Assert.Throws<ArgumentNullException>(() => value.Exclude(0, Array.Empty<object>()));
                Assert.Throws<ArgumentNullException>(() => value.Exclude(0, new object[] { null }));
            });

            // TC-B09
            yield return ("TC-B09", value =>
            {
                Assert.Throws<ArgumentNullException>(() => value.Confineable(0, Array.Empty<object>()));
                Assert.Throws<ArgumentNullException>(() => value.Confineable(0, new object[] { null }));
                Assert.Throws<ArgumentNullException>(() => value.Confineable(0, out _, null));
            });

            // TC-B10
            yield return ("TC-B10", value =>
            {
                Assert.Throws<ArgumentException>(() => value.ConfineableExcept(0, out _, Array.Empty<object>()));
                Assert.Throws<ArgumentException>(() => value.ConfineableExcept(0, out _, new object[] { null }));
            });

            // TC-B11
            yield return ("TC-B11", value =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { var ignored = value[value.Count]; });
            });
        }
    }
}
