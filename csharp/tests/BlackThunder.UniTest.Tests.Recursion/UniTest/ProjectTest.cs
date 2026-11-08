using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Recursion.UniTest
{
    public class ProjectTest
    {
        private enum ScenarioKind
        {
            Arithmetic,
            Empty,
            Terminal,
            Failure,
            MixedFailure,
            GeneratorFailure
        }

        private enum EntryPoint
        {
            Depth,
            Replay,
            Continuous
        }

        public class CounterModel : Model
        {
            public int Value = 1;
        }

        private class CounterProject : Project<CounterModel>
        {
            public int GeneratorCalls => Volatile.Read(ref _generatorCalls);
            public int ActorCalls => Volatile.Read(ref _actorCalls);

            private readonly ScenarioKind _kind;
            private int _generatorCalls;
            private int _actorCalls;


            public CounterProject(ExecutionMode mode, ScenarioKind kind = ScenarioKind.Arithmetic) : base(mode) =>
                _kind = kind;

            public override IEnumerable<ILab<CounterModel>> CreateLabs(CounterModel model)
            {
                Interlocked.Increment(ref _generatorCalls);

                switch (_kind)
                {
                    case ScenarioKind.Empty:
                        yield break;
                    case ScenarioKind.Terminal:
                        yield return Operation("Stop", m => m.Value = -m.Value, terminal: true);
                        yield break;
                    case ScenarioKind.Failure:
                        yield return Operation("Fail", _ => throw new ProbeException("Deliberate project failure"));
                        yield break;
                    case ScenarioKind.MixedFailure:
                        if (model.ExecutionCount == 0)
                        {
                            yield return Operation("Good", m => m.Value += 3);
                            yield return Operation("Fail", _ => throw new ProbeException("Deliberate sibling failure"));
                        }
                        else
                        {
                            yield return Operation("AfterGood", m => m.Value *= 3, terminal: true);
                        }

                        yield break;
                    case ScenarioKind.GeneratorFailure:
                        if (model.ExecutionCount == 0)
                        {
                            yield return Operation("Good", m => m.Value += 3);
                            throw new ProbeException("Deliberate iterator failure");
                        }

                        yield break;
                    default:
                        yield return Operation("Add", m => m.Value++);
                        yield return Operation("Double", m => m.Value *= 2);
                        yield return Operation("Stop", m => m.Value = -m.Value, terminal: true);
                        yield return Operation("Transient", m => m.Value += 10, unsustainable: true);
                        yield break;
                }
            }

            private Lab<CounterModel> Operation(
                string id,
                Action<CounterModel> action,
                bool terminal = false,
                bool unsustainable = false) =>
                new(
                    id,
                    actor: (model, _) =>
                    {
                        Interlocked.Increment(ref _actorCalls);
                        action(model);
                    },
                    toUnsustainable: unsustainable,
                    toUncontinuable: terminal);
        }

        private class ScenarioProject : Project<Model>
        {
            private readonly ExecutionMode _subjectMode;


            public ScenarioProject(ExecutionMode subjectMode) => _subjectMode = subjectMode;

            public override IEnumerable<ILab<Model>> CreateLabs(Model model)
            {
                if (model.ExecutionCount > 0)
                    yield break;

                foreach (var scenario in Scenarios(_subjectMode))
                    yield return new Lab<Model>(scenario.Id, actor: (_, _) => scenario.Verify());
            }
        }


        // P01-P10: every planned scenario must actually execute through the outer UniTest project.
        [TestCase(ExecutionMode.ThreadPool)]
        [TestCase(ExecutionMode.Inline)]
        public async Task ProjectScenarios(ExecutionMode mode)
        {
            var expectedIds = Scenarios(mode).Select(s => s.Id).ToArray();
            var project = new ScenarioProject(mode);
            var result = await project.Execute(1, stopOnFailure: false).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.That(expectedIds, Has.Length.EqualTo(26));
            Assert.That(expectedIds.Distinct().Count(), Is.EqualTo(expectedIds.Length));
            Assert.That(result.Afters.Select(n => n.Id), Is.EquivalentTo(expectedIds));
            Assert.That(project.ProcessCount, Is.EqualTo(expectedIds.Length));
            Assert.That(
                result.Afters.All(n => n.Status == Node<Model>.NodeStatus.Success),
                Is.True,
                result.GetFailedReports().ToJson());
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
        }

        private static IEnumerable<(string Id, Action Verify)> Scenarios(ExecutionMode mode)
        {
            foreach (var depth in new[] { 0, 1, 3 })
                yield return ($"Depth_{depth}", () => VerifyDepth(mode, depth));

            yield return ("Replay_Mixed", () => VerifyReplay(mode, "Add/Double/Transient/Add"));
            yield return ("Replay_Stop", () => VerifyReplay(mode, "Double/Stop"));
            yield return ("Continuous_Zero", () => VerifyContinuous(mode, 0, 0));

            foreach (var seed in new[] { 0, 1, 2 })
                yield return ($"Continuous_{seed}", () => VerifyContinuous(mode, 12, seed));

            foreach (var kind in new[] { ScenarioKind.Empty, ScenarioKind.Terminal })
            {
                yield return ($"{kind}_Depth", () => VerifyNoContinuation(mode, kind, EntryPoint.Depth));
                yield return ($"{kind}_Continuous", () => VerifyNoContinuation(mode, kind, EntryPoint.Continuous));
            }

            foreach (var entry in (EntryPoint[])Enum.GetValues(typeof(EntryPoint)))
            {
                yield return ($"Failure_{entry}", () => VerifyFailure(mode, entry));
                yield return ($"PreCancelled_{entry}", () => VerifyExternalCancellation(mode, entry));
                yield return ($"SingleUse_{entry}", () => VerifySingleUse(mode, entry));
            }

            yield return ("ContinueAfterFailure", () => VerifyContinueAfterFailure(mode));
            yield return ("GeneratorFailure", () => VerifyGeneratorFailure(mode));
            yield return ("UnknownPath", () => VerifyUnknownPath(mode));
            yield return ("CancelledProject", () => VerifyCancelledProject(mode));
        }

        // P01: independent bounded path enumeration also detects omitted or unexpected branches.
        private static void VerifyDepth(ExecutionMode mode, int depth)
        {
            var project = new CounterProject(mode);
            var result = project.Execute(depth).GetAwaiter().GetResult();
            var expected = ExpectedPaths(Math.Max(1, depth));
            var actual = Flatten(result);

            Assert.That(actual.Keys, Is.EquivalentTo(expected));
            Assert.That(project.ProcessCount, Is.EqualTo(expected.Count - 1));
            Assert.That(actual.Values.Select(n => n.Model).Distinct().Count(), Is.EqualTo(actual.Count));
            VerifyArithmeticTree(actual);
        }

        // P02: compare explicit replay against both arithmetic and exhaustive traversal.
        private static void VerifyReplay(ExecutionMode mode, string path)
        {
            var project = new CounterProject(mode);
            var result = project.Execute(path).GetAwaiter().GetResult();
            var actual = Flatten(result);
            var ids = path.Split('/');
            var prefixes = Enumerable.Range(0, ids.Length + 1).Select(n => string.Join("/", ids.Take(n))).ToArray();

            Assert.That(actual.Keys, Is.EquivalentTo(prefixes));
            VerifyArithmeticTree(actual);

            var exhaustive = Flatten(new CounterProject(mode).Execute(ids.Length).GetAwaiter().GetResult());

            foreach (var prefix in prefixes)
                Assert.That(actual[prefix].Model.Value, Is.EqualTo(exhaustive[prefix].Model.Value));
        }

        // P03: assert candidate coverage without duplicating the production random-selection algorithm.
        private static void VerifyContinuous(ExecutionMode mode, int depth, int seed)
        {
            var project = new CounterProject(mode);
            var result = project.ExecuteContinuously(depth, seed).GetAwaiter().GetResult();
            var actual = Flatten(result);

            Assert.That(actual.Count, Is.EqualTo(1 + 4 * depth));
            Assert.That(project.ProcessCount, Is.EqualTo(4 * depth));
            VerifyArithmeticTree(actual);

            foreach (var parent in actual.Values.Where(n => n.Afters.Count > 0))
            {
                Assert.That(parent.Afters.Select(n => n.Id), Is.EquivalentTo(new[]
                {
                    "Add", "Double", "Stop", "Transient"
                }));
                Assert.That(parent.Afters.Count(n => n.Afters.Count > 0), Is.EqualTo(parent.Depth < depth - 1 ? 1 : 0));

                foreach (var expanded in parent.Afters.Where(n => n.Afters.Count > 0))
                    Assert.That(new[] { "Add", "Double" }, Does.Contain(expanded.Id));
            }

            var repeated = new CounterProject(mode).ExecuteContinuously(depth, seed).GetAwaiter().GetResult();
            var otherMode = mode == ExecutionMode.Inline ? ExecutionMode.ThreadPool : ExecutionMode.Inline;
            var other = new CounterProject(otherMode).ExecuteContinuously(depth, seed).GetAwaiter().GetResult();

            Assert.That(Fingerprint(repeated), Is.EqualTo(Fingerprint(result)));
            Assert.That(Fingerprint(other), Is.EqualTo(Fingerprint(result)));

            if (depth == 0)
                return;

            var longest = actual.OrderByDescending(p => p.Value.Depth)
                .ThenBy(p => p.Key, StringComparer.Ordinal).First();
            var replay = Flatten(new CounterProject(mode).Execute(longest.Key).GetAwaiter().GetResult());
            Assert.That(replay[longest.Key].Model.Value, Is.EqualTo(longest.Value.Model.Value));
            Assert.That(replay[longest.Key].Model.GetExecutionHistory(), Is.EqualTo(longest.Key));
        }

        // P04: no candidates and terminal candidates both stop without adding another level.
        private static void VerifyNoContinuation(ExecutionMode mode, ScenarioKind kind, EntryPoint entry)
        {
            var project = new CounterProject(mode, kind);
            var result = Execute(project, entry).GetAwaiter().GetResult();
            var expected = kind == ScenarioKind.Empty ? new[] { "" } : new[] { "", "Stop" };

            Assert.That(Flatten(result).Keys, Is.EquivalentTo(expected));
            Assert.That(project.ProcessCount, Is.EqualTo(expected.Length - 1));
            Assert.That(project.GeneratorCalls, Is.EqualTo(1));
            VerifyArithmeticTree(Flatten(result));
        }

        // P05: deliberate subject failures must remain visible through the self-test runner.
        private static void VerifyFailure(ExecutionMode mode, EntryPoint entry)
        {
            var project = new CounterProject(mode, ScenarioKind.Failure);
            var result = Execute(project, entry, "Fail").GetAwaiter().GetResult();
            var actual = Flatten(result);

            Assert.That(actual.Keys, Is.EquivalentTo(new[] { "", "Fail" }));
            Assert.That(actual["Fail"].Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Failure));
            Assert.That(actual["Fail"].Exception.GetBaseException(), Is.TypeOf<ProbeException>());
            Assert.That(actual["Fail"].Continuable, Is.False);
            Assert.That(result.AllSucceed(out _), Is.False);
            Assert.That(result.GetFailedReports().Nodes.Select(n => n.History), Is.EquivalentTo(new[] { "", "Fail" }));
        }

        // P06: a successful sibling keeps expanding when stop-on-failure is disabled.
        private static void VerifyContinueAfterFailure(ExecutionMode mode)
        {
            var project = new CounterProject(mode, ScenarioKind.MixedFailure);
            var result = project.Execute(3, stopOnFailure: false).GetAwaiter().GetResult();
            var actual = Flatten(result);

            Assert.That(actual.Keys, Is.EquivalentTo(new[] { "", "Good", "Fail", "Good/AfterGood" }));
            Assert.That(actual["Fail"].Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Failure));
            Assert.That(actual["Fail"].Exception.GetBaseException(), Is.TypeOf<ProbeException>());
            Assert.That(actual["Good/AfterGood"].Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Success));
            Assert.That(actual["Good/AfterGood"].Model.Value, Is.EqualTo(12));
            Assert.That(actual["Good/AfterGood"].Continuable, Is.False);
            Assert.That(project.ProcessCount, Is.EqualTo(3));
            Assert.That(result.GetFailedReports().Nodes.Select(n => n.History), Is.EquivalentTo(new[] { "", "Fail" }));
        }

        // P07: a deferred generator error preserves both its root diagnostic and yielded child.
        private static void VerifyGeneratorFailure(ExecutionMode mode)
        {
            var project = new CounterProject(mode, ScenarioKind.GeneratorFailure);
            var result = project.Execute(2, stopOnFailure: false).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Failure));
            Assert.That(result.Exception.GetBaseException(), Is.TypeOf<ProbeException>());
            Assert.That(Flatten(result).Keys, Is.EquivalentTo(new[] { "", "Good" }));
            Assert.That(result.Afters.Single().Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Success));
            Assert.That(result.Afters.Single().Model.Value, Is.EqualTo(4));
            Assert.That(project.ProcessCount, Is.EqualTo(1));
            Assert.That(result.GetReport().Nodes, Has.Count.EqualTo(2));
        }

        // P07: a missing ID is a caller-visible error, even after a valid prefix.
        private static void VerifyUnknownPath(ExecutionMode mode)
        {
            var project = new CounterProject(mode);
            var error = Assert.Throws<ArgumentException>(() => project.Execute("Add/missing").GetAwaiter().GetResult());

            Assert.That(error.ParamName, Is.EqualTo("id"));
            Assert.That(project.ActorCalls, Is.EqualTo(1));
            Assert.That(project.Executed, Is.True);
            AssertRejectsReuse(project);
        }

        // P08: pre-cancellation prevents generator and actor callbacks at all entry points.
        private static void VerifyExternalCancellation(ExecutionMode mode, EntryPoint entry)
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var project = new CounterProject(mode);
            var result = Execute(project, entry, cancellationToken: cancellation.Token).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(Node<CounterModel>.NodeStatus.Root));
            Assert.That(result.Afters, Is.Empty);
            Assert.That(project.GeneratorCalls, Is.Zero);
            Assert.That(project.ActorCalls, Is.Zero);
            Assert.That(project.ProcessCount, Is.Zero);
            Assert.That(project.Executed, Is.True);
            AssertRejectsReuse(project);
        }

        // P09: consuming any entry point forbids every later entry point and preserves earlier snapshots.
        private static void VerifySingleUse(ExecutionMode mode, EntryPoint entry)
        {
            var project = new CounterProject(mode);
            Assert.That(project.Executed, Is.False);
            var result = Execute(project, entry).GetAwaiter().GetResult();
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
            var report = result.GetReport();
            var snapshot = report.ToJson();
            var processes = project.ProcessCount;
            var calls = project.ActorCalls;

            Assert.That(project.Executed, Is.True);
            AssertRejectsReuse(project);
            project.Cancel();
            project.Cancel();
            Assert.That(project.ProcessCount, Is.EqualTo(processes));
            Assert.That(project.ActorCalls, Is.EqualTo(calls));
            Assert.That(report.ToJson(), Is.EqualTo(snapshot));
            Assert.That(result.GetReport().ToJson(), Is.EqualTo(snapshot));
        }

        // P10: cancelling the project itself consumes its lifetime before any execution.
        private static void VerifyCancelledProject(ExecutionMode mode)
        {
            var project = new CounterProject(mode);
            project.Cancel();
            project.Cancel();
            Assert.That(project.Executed, Is.True);
            AssertRejectsReuse(project);
            Assert.That(project.GeneratorCalls, Is.Zero);
            Assert.That(project.ActorCalls, Is.Zero);
        }

        private static void AssertRejectsReuse(CounterProject project)
        {
            foreach (var entry in (EntryPoint[])Enum.GetValues(typeof(EntryPoint)))
                Assert.Throws<InvalidOperationException>(() => Execute(project, entry).GetAwaiter().GetResult());
        }

        private static Task<Node<CounterModel>> Execute(
            CounterProject project,
            EntryPoint entry,
            string path = "Add/Double",
            CancellationToken cancellationToken = default) =>
            entry switch
            {
                EntryPoint.Depth => project.Execute(3, cancellationToken),
                EntryPoint.Replay => project.Execute(path, cancellationToken),
                _ => project.ExecuteContinuously(3, 0, cancellationToken)
            };

        private static HashSet<string> ExpectedPaths(int depth)
        {
            var result = new HashSet<string> { "" };
            AddChildren("", depth);
            return result;

            void AddChildren(string parent, int remaining)
            {
                if (remaining == 0)
                    return;

                foreach (var id in new[] { "Add", "Double", "Stop", "Transient" })
                {
                    var path = parent.Length == 0 ? id : parent + "/" + id;
                    result.Add(path);

                    if (id != "Stop")
                        AddChildren(path, remaining - 1);
                }
            }
        }

        private static Dictionary<string, Node<CounterModel>> Flatten(Node<CounterModel> root)
        {
            var result = new Dictionary<string, Node<CounterModel>>();
            AddNode(root, "");
            return result;

            void AddNode(Node<CounterModel> node, string path)
            {
                result.Add(path, node);

                foreach (var child in node.Afters)
                    AddNode(child, path.Length == 0 ? child.Id : path + "/" + child.Id);
            }
        }

        private static void VerifyArithmeticTree(Dictionary<string, Node<CounterModel>> actual)
        {
            foreach (var pair in actual)
            {
                var ids = pair.Key.Length == 0 ? Array.Empty<string>() : pair.Key.Split('/');
                var expectedValue = 1;

                foreach (var id in ids)
                {
                    expectedValue = id switch
                    {
                        "Add" => expectedValue + 1,
                        "Double" => expectedValue * 2,
                        "Stop" => -expectedValue,
                        "Transient" => expectedValue + 10,
                        _ => throw new ArgumentException("Unknown reference operation", nameof(actual))
                    };
                }

                var node = pair.Value;
                Assert.That(node.Model.Value, Is.EqualTo(expectedValue), pair.Key);
                Assert.That(node.Model.ExecutionCount, Is.EqualTo(ids.Length), pair.Key);
                Assert.That(node.Model.GetExecutionHistory(), Is.EqualTo(pair.Key));
                Assert.That(node.Depth, Is.EqualTo(ids.Length), pair.Key);
                Assert.That(node.Exception, Is.Null, pair.Key);
                Assert.That(
                    node.Status,
                    Is.EqualTo(ids.Length == 0
                        ? Node<CounterModel>.NodeStatus.Root : Node<CounterModel>.NodeStatus.Success));
                Assert.That(node.Model.Continuable, Is.EqualTo(!ids.Contains("Stop")), pair.Key);
                Assert.That(node.Model.Sustainable, Is.EqualTo(!ids.Contains("Stop") && !ids.Contains("Transient")));

                if (ids.Contains("Stop"))
                    Assert.That(node.Afters, Is.Empty, pair.Key);
            }
        }

        private static string[] Fingerprint(Node<CounterModel> root) =>
            Flatten(root).OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}:{p.Value.Model.Value}:{p.Value.Status}").ToArray();
    }
}
