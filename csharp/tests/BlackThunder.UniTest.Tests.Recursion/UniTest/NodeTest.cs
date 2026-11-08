using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Recursion.UniTest
{
    public class NodeTest
    {
        // Front
        public enum ExpectedState
        {
            Root,
            Ready,
            Success,
            Terminal,
            Failure,
            Cancelled
        }

        public class SubjectModel : Model
        {
            public int Value;
        }

        public class StateModel : Model
        {
            // N01: The oracle starts with an independently specified empty root.
            public Node<SubjectModel> Current = new(null);
            public Node<SubjectModel> ExpectedBefore;
            public ExpectedState State = ExpectedState.Root;
            public bool CurrentWasExecuted;
            public List<string> Path { get; } = new();
            public List<(Node<SubjectModel> Node, string Id)> Children { get; } = new();
            public List<(Node<SubjectModel> Root, TestReport Report, string Json)> PreservedGraphs { get; } = new();

            public override string ToString() => $"Expected {State}: {string.Join("/", Path)}";
        }

        // Content --------------------
        // Generated operations
        private class StateProject : Project<StateModel>
        {
            public StateProject(ExecutionMode mode) : base(mode) { }

            public override IEnumerable<ILab<StateModel>> CreateLabs(StateModel model)
            {
                AssertCurrent(model);

                if (model.State == ExpectedState.Root || model.State == ExpectedState.Success)
                {
                    yield return Operation("AppendAdd", m => Append(m, "Add"));
                    yield return Operation("AppendFail", m => Append(m, "Fail"));
                    yield return Operation("AppendStop", m => Append(m, "Stop"));
                    yield return Operation("Branch", Branch);
                    yield return Operation("RejectNull", m => Reject(m, "Null"));
                    yield return Operation("RejectId", m => Reject(m, "Id"));
                    yield return Operation("CancelledAppend", m => Reject(m, "CancelledAppend"));
                }
                else
                {
                    yield return Operation("RejectAppend", m => Reject(m, "Append"));
                }

                if (model.State == ExpectedState.Ready)
                {
                    yield return Operation("Execute", NodeTest.Execute);
                    yield return Operation("Cancel", NodeTest.Cancel);
                }
                else
                {
                    yield return Operation("RejectExecute", m => Reject(m, "Execute"));
                }

                if (model.State == ExpectedState.Root)
                    yield return Operation("RejectRestore", m => Reject(m, "Restore"));
                else
                    yield return Operation("Restore", Restore);
            }
        }

        private class IncorrectExpectationProject : Project<StateModel>
        {
            public IncorrectExpectationProject() : base(BlackThunder.UniTest.ExecutionMode.Inline) { }

            public override IEnumerable<ILab<StateModel>> CreateLabs(StateModel model)
            {
                yield return Operation("IncorrectExpectation", m =>
                {
                    Assert.That(m.Current.Model.Value, Is.EqualTo(-1), "Intentional oracle mismatch.");
                });
            }
        }

        private class SubjectFailure : Exception { }

        private static Lab<StateModel> Operation(string id, Action<StateModel> action) =>
            new(id, actor: (model, _) => action(model), asserter: (model, _) => AssertCurrent(model));

        private static int Delta(string id) => id == "Stop" ? 10 : id == "Branch7" ? 7 : id == "Fail" ? 0 : 1;

        private static Lab<SubjectModel> SubjectLab(string id) => new(
            id,
            actor: (model, _) =>
            {
                if (id == "Fail")
                    throw new SubjectFailure();

                model.Value += Delta(id);
            },
            toUncontinuable: id == "Stop");

        // N02: Append replays the specified prefix without executing the new Lab.
        private static void Append(StateModel model, string id)
        {
            var parent = model.Current;
            var siblings = parent.Afters.ToArray();
            var parentValue = ExpectedValue(model);
            var current = parent.Append(SubjectLab(id), CancellationToken.None);
            Assert.That(parent.Afters, Is.EqualTo(siblings.Concat(new[] { current })));
            Assert.That(parent.Model.Value, Is.EqualTo(parentValue));
            Assert.That(current.Model, Is.Not.SameAs(parent.Model));

            model.Current = current;
            model.ExpectedBefore = parent;
            model.Path.Add(id);
            model.State = ExpectedState.Ready;
            model.CurrentWasExecuted = false;
            model.Children.Clear();
        }

        // N03: The input Lab determines success, failure, or terminal success.
        private static void Execute(StateModel model)
        {
            model.Current.Execute();
            model.CurrentWasExecuted = true;
            model.State = model.Path.Last() switch
            {
                "Fail" => ExpectedState.Failure,
                "Stop" => ExpectedState.Terminal,
                _ => ExpectedState.Success
            };
        }

        // N04: Both siblings have independent models and the parent remains unchanged.
        private static void Branch(StateModel model)
        {
            var first = model.Current.Append(SubjectLab("Branch1"), CancellationToken.None);
            var second = model.Current.Append(SubjectLab("Branch7"), CancellationToken.None);
            first.Execute();
            second.Execute();
            Assert.That(first.Model, Is.Not.SameAs(second.Model));
            Assert.That(first.Model, Is.Not.SameAs(model.Current.Model));
            Assert.That(second.Model, Is.Not.SameAs(model.Current.Model));
            model.Children.Add((first, "Branch1"));
            model.Children.Add((second, "Branch7"));
        }

        // N05: Preserve every original graph and snapshot across all later operations.
        private static void Restore(StateModel model)
        {
            var graph = model.Current;

            while (graph.Before != null)
                graph = graph.Before;

            var report = graph.GetReport();
            model.PreservedGraphs.Add((graph, report, report.ToJson()));
            var original = model.Current;
            model.Current = original.DetachAndRestore();
            Assert.That(model.Current, Is.Not.SameAs(original));
            Assert.That(model.Current.Model, Is.Not.SameAs(original.Model));
            model.ExpectedBefore = null;
            model.State = ExpectedState.Ready;
            model.CurrentWasExecuted = false;
            model.Children.Clear();
        }

        // N06: Cancellation before execution does not apply the pending Lab.
        private static void Cancel(StateModel model)
        {
            model.Current.SetCancellationException();
            model.State = ExpectedState.Cancelled;
        }

        // N07-N12: Rejections must preserve the graph as well as the oracle observations.
        private static void Reject(StateModel model, string operation)
        {
            var report = model.Current.GetReport().ToJson();

            switch (operation)
            {
                case "Execute":
                    Assert.Throws<InvalidOperationException>(() => model.Current.Execute());
                    break;
                case "Append":
                    Assert.Throws<InvalidOperationException>(() => model.Current.Append(SubjectLab("Add"), default));
                    break;
                case "Restore":
                    Assert.Throws<InvalidOperationException>(() => model.Current.DetachAndRestore());
                    break;
                case "Null":
                    Assert.Throws<InvalidOperationException>(() => model.Current.Append(null, default));
                    break;
                case "Id":
                    Assert.Throws<ArgumentException>(() => model.Current.Append(SubjectLab("bad/id"), default));
                    break;
                case "CancelledAppend":
                    using (var cancellation = new CancellationTokenSource())
                    {
                        cancellation.Cancel();
                        Assert.Throws<OperationCanceledException>(() =>
                            model.Current.Append(SubjectLab("Add"), cancellation.Token));
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }

            Assert.That(model.Current.GetReport().ToJson(), Is.EqualTo(report));
        }

        // Independent observations
        private static int ExecutedCount(StateModel model) =>
            model.Path.Count - (model.Path.Count > 0 && !model.CurrentWasExecuted ? 1 : 0);

        private static int ExpectedValue(StateModel model) => model.Path.Take(ExecutedCount(model)).Sum(Delta);

        // N01-N12: Expected values come from inputs; no target state feeds this oracle.
        private static void AssertCurrent(StateModel model)
        {
            var current = model.Current;
            var count = ExecutedCount(model);
            var value = ExpectedValue(model);
            var history = string.Join("/", model.Path.Take(count));
            var status = model.State == ExpectedState.Terminal ? "Success" : model.State.ToString();
            var continuable = model.State == ExpectedState.Root || model.State == ExpectedState.Success;
            Assert.That(current.Status.ToString(), Is.EqualTo(status));
            Assert.That(current.Depth, Is.EqualTo(model.Path.Count));
            Assert.That(current.Model.Value, Is.EqualTo(value));
            Assert.That(current.Model.ExecutionCount, Is.EqualTo(count));
            Assert.That(current.Model.GetExecutionHistory(), Is.EqualTo(history));
            Assert.That(
                current.Model.Continuable,
                Is.EqualTo(model.State != ExpectedState.Terminal && model.State != ExpectedState.Failure));
            Assert.That(current.Continuable, Is.EqualTo(continuable));
            Assert.That(current.Before, Is.SameAs(model.ExpectedBefore));
            Assert.That(current.Afters, Is.EqualTo(model.Children.Select(child => child.Node)));

            if (model.State == ExpectedState.Failure)
            {
                Assert.That(current.Exception, Is.TypeOf<ExecutionException>());
                Assert.That(current.Exception.InnerException, Is.TypeOf<SubjectFailure>());
            }
            else if (model.State == ExpectedState.Cancelled)
            {
                Assert.That(current.Exception, Is.TypeOf<OperationCanceledException>());
            }
            else
            {
                Assert.That(current.Exception, Is.Null);
            }

            foreach (var child in model.Children)
            {
                Assert.That(child.Node.Before, Is.SameAs(current));
                Assert.That(child.Node.Depth, Is.EqualTo(model.Path.Count + 1));
                Assert.That(child.Node.Status, Is.EqualTo(Node<SubjectModel>.NodeStatus.Success));
                Assert.That(child.Node.Exception, Is.Null);
                Assert.That(child.Node.Afters, Is.Empty);
                Assert.That(child.Node.Model.Value, Is.EqualTo(value + Delta(child.Id)));
                Assert.That(child.Node.Model.ExecutionCount, Is.EqualTo(count + 1));
                Assert.That(
                    child.Node.Model.GetExecutionHistory(),
                    Is.EqualTo(string.Join("/", model.Path.Concat(new[] { child.Id }))));
            }

            var report = current.GetReport();
            Assert.That(report.Nodes.Count, Is.EqualTo(model.Path.Count + 1 + model.Children.Count));

            for (var i = 0; i <= model.Path.Count; i++)
            {
                var entry = report.Nodes[i];
                Assert.That(entry.NodeId, Is.EqualTo(i));
                Assert.That(entry.ParentId, Is.EqualTo(i == 0 ? (int?)null : i - 1));
                Assert.That(entry.Depth, Is.EqualTo(i));
                Assert.That(entry.LabId, Is.EqualTo(i == 0 ? null : model.Path[i - 1]));
                Assert.That(entry.Status, Is.EqualTo(i == model.Path.Count ? status : i == 0 ? "Root" : "Success"));
                Assert.That(
                    entry.History,
                    Is.EqualTo(string.Join("/", model.Path.Take(i == model.Path.Count ? count : i))));
                Assert.That(entry.ReportError, Is.Null);
            }

            Assert.That(report.Nodes[model.Path.Count].Continuable, Is.EqualTo(continuable));

            foreach (var preserved in model.PreservedGraphs)
            {
                Assert.That(preserved.Root.GetReport().ToJson(), Is.EqualTo(preserved.Json));
                Assert.That(preserved.Report.ToJson(), Is.EqualTo(preserved.Json));
            }
        }

        private static List<Node<StateModel>> CompletedNodes(Node<StateModel> root)
        {
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Status, Is.EqualTo(Node<StateModel>.NodeStatus.Root));
            var nodes = new List<Node<StateModel>>();
            var pending = new Stack<Node<StateModel>>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                nodes.Add(node);
                AssertCurrent(node.Model);

                if (!ReferenceEquals(node, root))
                    Assert.That(
                        node.Status,
                        Is.EqualTo(Node<StateModel>.NodeStatus.Success),
                        node.Exception?.ToString());

                foreach (var child in node.Afters)
                    pending.Push(child);
            }

            Assert.That(nodes.Count, Is.GreaterThan(1));
            return nodes;
        }

        // NUnit entry points
        [TestCase(ExecutionMode.ThreadPool), TestCase(ExecutionMode.Inline)]
        public async Task BoundedExplorationCoversEveryOperation(ExecutionMode mode)
        {
            // N01-N12: Bound exhaustive generation and require every operation and state.
            var result = await new StateProject(mode).Execute(4);
            var nodes = CompletedNodes(result);
            Assert.That(nodes.Max(node => node.Depth), Is.EqualTo(4));
            Assert.That(
                nodes.Select(node => node.Model.State).Distinct(),
                Is.EquivalentTo(Enum.GetValues(typeof(ExpectedState))));

            Assert.That(
                nodes.Skip(1).Select(node => node.Id).Distinct(),
                Is.EquivalentTo(new[]
                {
                    "AppendAdd", "AppendFail", "AppendStop", "Branch", "Execute", "Restore", "Cancel",
                    "RejectAppend", "RejectExecute", "RejectRestore", "RejectNull", "RejectId", "CancelledAppend"
                }));
        }

        [TestCase(0), TestCase(1), TestCase(2)]
        public async Task ContinuousExplorationPreservesState(int seed)
        {
            // N01-N12: Fixed seeds exercise longer histories and require actual candidate execution.
            var result = await new StateProject(ExecutionMode.Inline).ExecuteContinuously(48, seed);
            var nodes = CompletedNodes(result);
            Assert.That(nodes.Max(node => node.Depth), Is.EqualTo(48));
            Assert.That(nodes.Count, Is.GreaterThan(49));
        }

        [TestCase(
            "AppendAdd/Execute/AppendAdd/Execute/Restore/Execute/Restore/Execute/AppendAdd/Execute",
            3,
            3,
            ExpectedState.Success)]
        [TestCase("AppendAdd/Execute/AppendFail/Execute/Restore/Execute", 2, 1, ExpectedState.Failure)]
        [TestCase("AppendStop/Execute/Restore/Execute", 1, 10, ExpectedState.Terminal)]
        [TestCase("AppendAdd/Cancel/Restore/Execute/Branch", 1, 1, ExpectedState.Success)]
        [TestCase("Branch/Branch/AppendAdd/Execute/Branch", 1, 1, ExpectedState.Success)]
        public async Task RegressionPathsPreserveState(string path, int depth, int value, ExpectedState state)
        {
            // N02-N06: Fixed paths prove repeated restoration, failure, cancellation, and branching.
            var result = await new StateProject(ExecutionMode.Inline).Execute(path);
            var nodes = CompletedNodes(result);
            Assert.That(nodes.Count, Is.EqualTo(path.Split('/').Length + 1));
            var last = nodes.Single(node => node.Depth == path.Split('/').Length).Model;
            Assert.That(last.State, Is.EqualTo(state));
            Assert.That(last.Current.Depth, Is.EqualTo(depth));
            Assert.That(last.Current.Model.Value, Is.EqualTo(value));
        }

        [Test]
        public async Task DetectsIncorrectExpectation()
        {
            // N13: An intentionally wrong oracle must be observable as a real failure.
            Node<StateModel> result;

            using (new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext())
            {
                result = await new IncorrectExpectationProject().Execute(1, stopOnFailure: false);
            }

            Assert.That(result.Afters.Count, Is.EqualTo(1));
            Assert.That(result.Afters[0].Id, Is.EqualTo("IncorrectExpectation"));
            Assert.That(result.Afters[0].Status, Is.EqualTo(Node<StateModel>.NodeStatus.Failure));
            Assert.That(result.Afters[0].Exception, Is.Not.Null);
            Assert.That(result.AllSucceed(out var cancelled), Is.False);
            Assert.That(cancelled, Is.False);
        }
    }
}
