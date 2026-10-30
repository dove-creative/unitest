using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class JsonReportTest
    {

        public sealed class BrokenDescriptionModel : Model
        {
            public override string ToString() => throw new InvalidOperationException("description failed");
        }

        private sealed class CustomLab : ILab<Model>
        {
            public string Id { get; }
            public CustomLab(string id) => Id = id;
            public void Execute(Model model, out ExecutionException exception) => exception = null;
        }

        private sealed class ReportProject : Project<Model>
        {
            private readonly Func<Model, IEnumerable<ILab<Model>>> _createLabs;
            public ReportProject(ExecutionMode mode, Func<Model, IEnumerable<ILab<Model>>> createLabs)
                : base(mode) => _createLabs = createLabs;
            public override IEnumerable<ILab<Model>> CreateLabs(Model model) => _createLabs(model);
        }
        [TestCase("Charge 50%")]
        [TestCase("충전 \"50%\" & <준비>")]
        [TestCase("123:action\nnext\t\\path")]
        public async Task SpecialIdsCanExecuteReplayAndRoundTrip(string id)
        {
            var project = new ReportProject(ExecutionMode.ThreadPool, _ => new[] { new Lab<Model>(id) });
            var root = await project.Execute(id);
            Assert.That(root.AllSucceed(out _), Is.True);
            var report = root.GetReport();
            using var json = JsonDocument.Parse(report.ToJson());
            var nodes = json.RootElement.GetProperty("nodes");
            Assert.That(nodes.GetArrayLength(), Is.EqualTo(2));
            Assert.That(nodes[0].GetProperty("labId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(nodes[0].GetProperty("parentId").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(nodes[1].GetProperty("labId").GetString(), Is.EqualTo(id));
            Assert.That(nodes[1].GetProperty("history").GetString(), Is.EqualTo(id));
            Assert.That(nodes[1].GetProperty("status").GetString(), Is.EqualTo("Success"));
        }

        [Test]
        public void SlashIsRejectedBeforeAttachingOrReplaying()
        {
            int executions = 0;
            var root = new Node<Model>(null);
            var first = root.Append(new Lab<Model>("first", actor: (_, _) => executions++), default);
            first.Execute();
            var invalid = new Lab<Model>("a/b");
            Assert.Throws<ArgumentException>(() => new Node<Model>(invalid));
            var error = Assert.Throws<ArgumentException>(() => first.Append(invalid, default));
            Assert.That(error.Message, Does.Contain("execution path separator"));
            Assert.That(first.Afters, Is.Empty);
            Assert.That(executions, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => root.Append(new CustomLab("a/b"), default));
        }

        [TestCase(ExecutionMode.ThreadPool)]
        [TestCase(ExecutionMode.Inline)]
        public async Task IteratorFailureRetainsCompletedChildren(ExecutionMode mode)
        {
            var project = new ReportProject(mode, _ => Labs());
            var root = await project.Execute(1, stopOnFailure: false).WaitAsync(TimeSpan.FromSeconds(10));
            var report = root.GetReport();
            Assert.That(root.Afters.Count, Is.EqualTo(1));
            Assert.That(report.Nodes.Select(n => n.Status), Is.EqualTo(new[] { "Failure", "Success" }));
            Assert.That(report.Nodes[0].Error.Details, Does.Contain("iterator failed"));
            using var json = JsonDocument.Parse(report.ToJson());
            Assert.That(json.RootElement.GetProperty("nodes").GetArrayLength(), Is.EqualTo(2));
            Assert.That(root.GetFailedReports().Nodes.Count, Is.EqualTo(1));

            static IEnumerable<ILab<Model>> Labs()
            {
                yield return new Lab<Model>("existing child");
                throw new InvalidOperationException("iterator failed");
            }
        }

        [Test]
        public void SelectedSubtreeIncludesAncestorsWithoutSiblings()
        {
            var root = new Node<Model>(null);
            var branch = root.Append(new Lab<Model>("branch"), default);
            branch.Execute();
            var leaf = branch.Append(new Lab<Model>("leaf"), default);
            root.Append(new Lab<Model>("unrelated sibling"), default);
            var report = branch.GetReport();
            Assert.That(report.Nodes.Select(n => n.LabId), Is.EqualTo(new[] { null, "branch", "leaf" }));
            Assert.That(report.Nodes.Select(n => n.ParentId), Is.EqualTo(new int?[] { null, 0, 1 }));
            leaf.SetExternalException(new Exception("failure"), Node<Model>.NodeStatus.Failure);
            Assert.That(branch.GetFailedReports().Nodes.Select(n => n.Status),
                Is.EqualTo(new[] { "Root", "Success", "Failure" }));
        }

        [Test]
        public void DeepPathsSerializeWithoutNestedJsonDepthGrowth()
        {
            var root = new Node<Model>(null);
            var current = root;
            for (int i = 0; i < 120; i++)
            {
                current = current.Append(new Lab<Model>("same ID"), default);
                current.Execute();
            }
            var report = root.GetReport();
            using var json = JsonDocument.Parse(report.ToJson());
            Assert.That(json.RootElement.GetProperty("nodes").GetArrayLength(), Is.EqualTo(121));
            Assert.That(report.Nodes.Last().Depth, Is.EqualTo(120));
            Assert.That(report.Nodes.Last().ParentId, Is.EqualTo(119));
            Assert.That(report.Nodes.Select(n => n.NodeId).Distinct().Count(), Is.EqualTo(121));
        }

        [Test]
        public void ModelDescriptionFailureDoesNotLoseExecutionFailureOrChildren()
        {
            var root = new Node<BrokenDescriptionModel>(null);
            var child = root.Append(new Lab<BrokenDescriptionModel>("child"), default);
            child.Execute();
            Exception failure;
            try { throw new InvalidOperationException("inner detail"); }
            catch (Exception inner) { failure = new ExecutionException("outer detail", inner); }
            root.SetExternalException(failure, Node<BrokenDescriptionModel>.NodeStatus.Failure);
            var report = root.GetReport();
            Assert.That(report.Nodes.Count, Is.EqualTo(2));
            var result = report.Nodes[0];
            Assert.That(result.Status, Is.EqualTo("Failure"));
            Assert.That(result.Error.Type, Is.EqualTo(typeof(ExecutionException).FullName));
            Assert.That(result.Error.Message, Is.EqualTo("outer detail"));
            Assert.That(result.Error.Details, Does.Contain("inner detail").And.Contain(nameof(ModelDescriptionFailureDoesNotLoseExecutionFailureOrChildren)));
            Assert.That(result.Model, Is.Null);
            Assert.That(result.ReportError.Message, Is.EqualTo("description failed"));
            Assert.That(result.History, Is.EqualTo(string.Empty));
            Assert.That(report.Nodes[1].Status, Is.EqualTo("Success"));
            Assert.That(report.Nodes[1].History, Is.EqualTo("child"));
            Assert.DoesNotThrow(() => JsonDocument.Parse(report.ToJson()).Dispose());
        }

        [Test]
        public void ReportsAndExecutionDiagnosticsAreIndependentSnapshots()
        {
            var node = new Node<Model>(new Lab<Model>("original", actor: (m, _) => m.Subject = "at execution"));
            node.Execute();
            node.Model.Subject = "changed later";
            var report = node.GetReport();
            var json = report.ToJson();
            Assert.That(report.Nodes[0].Model, Does.Contain("at execution").And.Not.Contain("changed later"));
            node.SetExternalException(new Exception("later error"), Node<Model>.NodeStatus.Failure);
            Assert.That(report.ToJson(), Is.EqualTo(json));
            Assert.That(node.GetReport().Nodes[0].Status, Is.EqualTo("Failure"));
            Assert.That(((ICollection<NodeReport>)report.Nodes).IsReadOnly, Is.True);
        }

        [Test]
        public void RepeatedRestoreAndAppendReplayAncestorsAndPreserveOriginal()
        {
            var root = new Node<Model>(null);
            var first = root.Append(new Lab<Model>("first", actor: (m, _) => m.Subject = 10), default);
            first.Execute();
            var second = first.Append(new Lab<Model>("second", actor: (m, _) => m.Subject = (int)m.Subject + 2), default);
            second.Execute();
            var original = root.GetReport().ToJson();
            var restored = second.DetachAndRestore();
            Assert.That(restored.Model.Subject, Is.EqualTo(10));
            restored.Execute();
            var again = restored.DetachAndRestore();
            Assert.That(again.Before, Is.Null);
            Assert.That(again.Model.Subject, Is.EqualTo(10));
            again.Execute();
            var third = again.Append(new Lab<Model>("third", actor: (m, _) => m.Subject = (int)m.Subject + 3), default);
            third.Execute();
            Assert.That(third.Model.Subject, Is.EqualTo(15));
            Assert.That(third.Model.GetExecutionHistory(), Is.EqualTo("first/second/third"));
            Assert.That(third.GetReport().Nodes.Select(n => n.LabId), Is.EqualTo(new[] { null, "first", "second", "third" }));
            Assert.That(third.GetReport().Nodes.Select(n => n.Depth), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(root.GetReport().ToJson(), Is.EqualTo(original));

            var captured = again.GetReport().ToJson();
            first.SetExternalException(new Exception("original ancestor changed"), Node<Model>.NodeStatus.Failure);
            Assert.That(again.GetReport().ToJson(), Is.EqualTo(captured));
        }

        [TestCase(ExecutionMode.ThreadPool)]
        [TestCase(ExecutionMode.Inline)]
        public async Task BranchingReportsAreCompleteAndStableAfterExecution(ExecutionMode mode)
        {
            var project = new ReportProject(mode, _ => Enumerable.Range(0, 3).Select(i => new Lab<Model>($"branch {i}")));
            var root = await project.Execute(5).WaitAsync(TimeSpan.FromSeconds(20));
            var report = root.GetReport();
            Assert.That(report.Nodes.Count, Is.EqualTo(364));
            Assert.That(report.Nodes.Count, Is.EqualTo(root.GetCount()));
            Assert.That(report.Nodes.Skip(1).All(n => n.Status == "Success"), Is.True);
            Assert.That(report.Nodes.Skip(1).All(n => n.ParentId < n.NodeId), Is.True);
            Assert.That(report.Nodes.Select(n => n.NodeId).Distinct().Count(), Is.EqualTo(364));
            var json = report.ToJson();
            await Task.Yield();
            Assert.That(root.GetReport().ToJson(), Is.EqualTo(json));
            using var parsed = JsonDocument.Parse(json);
            Assert.That(parsed.RootElement.GetProperty("nodes").GetArrayLength(), Is.EqualTo(364));
        }

        [Test]
        public void ExportWritesIndentedUtf8JsonWithoutBom()
        {
            var node = new Node<Model>(new Lab<Model>("충전 50%", actor: (m, _) => m.Subject = "한글 상태"));
            node.Execute();
            var report = node.GetReport();
            var directory = Path.Combine(Path.GetTempPath(), "unitest-json-" + Guid.NewGuid().ToString("N"));
            try
            {
                Tools.ExportJson(report, "result", directory);
                var bytes = File.ReadAllBytes(Path.Combine(directory, "result.json"));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
                var text = new UTF8Encoding(false, true).GetString(bytes);
                Assert.That(text, Is.EqualTo(report.ToJson()));
                Assert.That(text, Does.Contain("\n").And.Contain("충전 50%"));
                using var parsed = JsonDocument.Parse(bytes);
                Assert.That(parsed.RootElement.GetProperty("nodes")[0].GetProperty("model").GetString(), Does.Contain("한글 상태"));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void BuiltInWriterMatchesSerializerOutput()
        {
            var tricky = "quote \" backslash \\ slash / tab \t line \r\n nul \0 bell \u0007 sep " + (char)0x2028 + (char)0x2029 + " 한글 emoji 😀 <&'+>";
            var root = new Node<Model>(null);
            var child = root.Append(new Lab<Model>("lab " + tricky.Replace("/", "|"), actor: (m, _) => m.Subject = tricky), default);
            child.Execute();
            child.Append(new Lab<Model>("waiting"), default);
            Exception failure;
            try { throw new InvalidOperationException("inner " + tricky); }
            catch (Exception inner) { failure = new ExecutionException("outer " + tricky, inner); }
            root.SetExternalException(failure, Node<Model>.NodeStatus.Failure);
            var report = root.GetReport();

            var expected = JsonSerializer.SerializeToNode(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var actual = JsonNode.Parse(report.ToJson());

            Assert.That(JsonNode.DeepEquals(actual, expected), Is.True, report.ToJson());
            Assert.That(report.ToJson(), Does.Contain("한글 emoji 😀").And.Contain("\\u2028"));
        }

        [Test]
        public void LoneSurrogatesAreEscapedSoReportsStayValidUtf8()
        {
            var node = new Node<Model>(new Lab<Model>("broken \uD83D pair"));
            node.Execute();

            var json = node.GetReport().ToJson();

            Assert.That(json, Does.Contain("broken \\uD83D pair"));
            Assert.DoesNotThrow(() => new UTF8Encoding(false, true).GetBytes(json));
        }
    }
}
