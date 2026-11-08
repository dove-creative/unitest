using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Recursion
{
    public class LabTest
    {

        public class Model : BlackThunder.UniTest.Model
        {
            // Front --------------------
            // Subject
            public Lab<Model> Lab
            {
                get => (Lab<Model>)Subject;
                set => Subject = value;
            }

            // Mock
            public Model ExpectedModel;
            public object Metadata;

            public Func<Model, object> SetMetadata;
            public Action<Model, SubjectMetadata> Arranger;
            public Action<Model, SubjectMetadata> Actor;
            public Action<Model, SubjectMetadata> Asserter;
            public SubjectMetadata SubjectMetadata;
        }

        public enum Condition
        {
            None,
            Success,
            Failure
        }

        private class Project : Project<Model>
        {
            public Project(ExecutionMode executionMode) : base(executionMode) { }

            private struct ExecuteModelCase
            {
                public bool SetMetadataByConstructor;
                public Condition SetMetadata;
                public Condition Arranger;
                public Condition Actor;
                public Condition Asserter;

                public ExecuteModelCase(
                    bool setMetadataByConstructor,
                    Condition setMetadata,
                    Condition arranger,
                    Condition actor,
                    Condition asserter)
                {
                    SetMetadataByConstructor = setMetadataByConstructor;
                    SetMetadata = setMetadata;
                    Arranger = arranger;
                    Actor = actor;
                    Asserter = asserter;
                }
            }

            private struct MergeMetadataCase
            {
                public int Current;
                public int Template;
                public int Expected;

                public MergeMetadataCase(int current, int template, int expected)
                {
                    Current = current;
                    Template = template;
                    Expected = expected;
                }
            }
            public override IEnumerable<ILab<Model>> CreateLabs(Model model)
            {
                if (model.ExecutionCount > 0)
                    yield break;

                yield return Scenario("Execute_NullModel", ExecuteNullModel);

                foreach (var testCase in GetExecuteModelCases())
                {
                    var captured = testCase;
                    yield return Scenario(
                        $"Execute_Model_{(captured.SetMetadataByConstructor ? "Constructor" : "Delegate")}_"
                        + $"{captured.SetMetadata}_{captured.Arranger}_{captured.Actor}_{captured.Asserter}",
                        () => ExecuteModel(captured));
                }

                yield return Scenario("SetMetadata_ByConstructor", SetMetadataByConstructor);
                yield return Scenario("SetMetadata_ByDelegate", SetMetadataByDelegate);

                foreach (var testCase in GetMergeMetadataCases())
                {
                    var captured = testCase;
                    yield return Scenario(
                        $"MergeMetadata_RemainingExecutionCount_{CountName(captured.Current)}_"
                        + CountName(captured.Template),
                        () => MergeMetadataChooseRemainingExecutionCount(captured));
                }

                foreach (var testCase in GetExecuteModelCases())
                {
                    var captured = testCase;
                    yield return Scenario(
                        $"Copy_{(captured.SetMetadataByConstructor ? "Constructor" : "Delegate")}_"
                        + $"{captured.SetMetadata}_{captured.Arranger}_{captured.Actor}_{captured.Asserter}",
                        () => Copy(captured));
                }
            }

            private static Lab<Model> Scenario(string id, Action test) => new Lab<Model>(id, actor: (_, _) => test());

            private static string CountName(int count) => count < 0 ? $"Minus{-count}" : count.ToString();

            private static IEnumerable<ExecuteModelCase> GetExecuteModelCases()
            {
                var cond = new[] { Condition.None, Condition.Success, Condition.Failure };

                IEnumerable<Condition[]> GetSettings()
                {
                    yield return new[] { Condition.Failure, Condition.None, Condition.None, Condition.None };

                    foreach (var c1 in cond.Where(c => c != Condition.Failure))
                        yield return new[] { c1, Condition.Failure, Condition.None, Condition.None };

                    foreach (var c1 in cond.Where(c => c != Condition.Failure))
                        foreach (var c2 in cond.Where(c => c != Condition.Failure))
                            yield return new[] { c1, c2, Condition.Failure, Condition.None };

                    foreach (var c1 in cond.Where(c => c != Condition.Failure))
                        foreach (var c2 in cond.Where(c => c != Condition.Failure))
                            foreach (var c3 in cond.Where(c => c != Condition.Failure))
                                foreach (var c4 in cond)
                                    yield return new[] { c1, c2, c3, c4 };
                }

                foreach (var setting in GetSettings())
                {
                    yield return new ExecuteModelCase(false, setting[0], setting[1], setting[2], setting[3]);
                    yield return new ExecuteModelCase(true, setting[0], setting[1], setting[2], setting[3]);
                }
            }

            private static IEnumerable<MergeMetadataCase> GetMergeMetadataCases()
            {
                yield return new MergeMetadataCase(-1, -1, -1);
                yield return new MergeMetadataCase(-1, 3, 3);
                yield return new MergeMetadataCase(3, -1, 3);
                yield return new MergeMetadataCase(1, 10, 1);
                yield return new MergeMetadataCase(10, 1, 1);
                yield return new MergeMetadataCase(0, 5, 0);
            }

            private static Lab<Model> CreateLab(ExecuteModelCase testCase, out Model targetModel, out object metadata)
            {
                targetModel = new Model();
                metadata = testCase.SetMetadata != Condition.None ? new object() : null;

                var expectedModel = targetModel;
                var expectedMetadata = metadata;

                Func<Model, object> setMetadata = null;
                Action<Model, SubjectMetadata> arranger = null;
                Action<Model, SubjectMetadata> actor = null;
                Action<Model, SubjectMetadata> asserter = null;

                if (testCase.SetMetadata != Condition.None)
                {
                    setMetadata = m =>
                    {
                        Assert.That(m, Is.SameAs(expectedModel));

                        if (testCase.SetMetadata == Condition.Failure)
                            throw new ProbeException("Set Metadata Failure");

                        return expectedMetadata;
                    };
                }

                if (testCase.Arranger != Condition.None)
                {
                    arranger = (m, md) =>
                    {
                        Assert.That(m, Is.SameAs(expectedModel));
                        Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                        if (testCase.Arranger == Condition.Failure)
                            throw new ProbeException("Arranger Failure");
                    };
                }

                if (testCase.Actor != Condition.None)
                {
                    actor = (m, md) =>
                    {
                        Assert.That(m, Is.SameAs(expectedModel));
                        Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                        if (testCase.Actor == Condition.Failure)
                            throw new ProbeException("Actor Failure");
                    };
                }

                if (testCase.Asserter != Condition.None)
                {
                    asserter = (m, md) =>
                    {
                        Assert.That(m, Is.SameAs(expectedModel));
                        Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                        if (testCase.Asserter == Condition.Failure)
                            throw new ProbeException("Asserter Failure");
                    };
                }

                return testCase.SetMetadataByConstructor
                    ? new Lab<Model>(
                        arranger: arranger,
                        actor: actor,
                        asserter: asserter,
                        expectedExceptionType: typeof(ProbeException),
                        toUnsustainable: true,
                        toUncontinuable: true,
                        remainingExecutionCount: 10)
                    {
                        SetMetadata = setMetadata
                    }
                    : new Lab<Model>(arranger: arranger, actor: actor, asserter: asserter)
                    {
                        SetMetadata = setMetadata
                    };
            }

            private static void ExecuteNullModel()
            {
                var lab = new Lab<Model>();

                lab.Execute(null, out var ex);

                Assert.That(ex, Is.AssignableTo<ExecutionException>());
                Assert.That(ex.Message, Is.EqualTo("Model setting failed"));
            }

            private static void ExecuteModel(ExecuteModelCase testCase)
            {
                var lab = CreateLab(testCase, out var targetModel, out var _);

                lab.Execute(targetModel, out var ex);

                if (testCase.SetMetadata == Condition.Failure)
                {
                    AssertExecutionException(ex, "Arrange failed", typeof(ProbeException), "Set Metadata Failure");
                    Assert.That(targetModel.Continuable, Is.False);
                    return;
                }

                if (testCase.Arranger == Condition.Failure)
                {
                    AssertExecutionException(ex, "Arrange failed", typeof(ProbeException), "Arranger Failure");
                    Assert.That(targetModel.Continuable, Is.False);
                    return;
                }

                if (testCase.Actor == Condition.Failure && !testCase.SetMetadataByConstructor)
                {
                    AssertExecutionException(ex, "Act failed", typeof(ProbeException), "Actor Failure");
                    Assert.That(targetModel.Continuable, Is.False);
                    return;
                }

                if (testCase.Actor != Condition.Failure && testCase.SetMetadataByConstructor)
                {
                    Assert.That(ex, Is.AssignableTo<ExecutionException>());
                    Assert.That(ex.Message, Is.EqualTo("Act failed"));

                    Assert.That(targetModel.Continuable, Is.False);
                    return;
                }

                if (testCase.Asserter == Condition.Failure)
                {
                    AssertExecutionException(ex, "Assert failed", typeof(ProbeException), "Asserter Failure");
                    Assert.That(targetModel.Continuable, Is.False);
                    return;
                }

                Assert.That(ex, Is.Null);
                Assert.That(!targetModel.Continuable, Is.EqualTo(testCase.SetMetadataByConstructor));
                Assert.That(
                    targetModel.RemainingExecutionCount,
                    Is.EqualTo(testCase.SetMetadataByConstructor ? 10 : null));
            }

            private static void AssertExecutionException(
                Exception ex,
                string expectedMessage,
                Type expectedInnerType,
                string expectedInnerMessage)
            {
                Assert.That(ex, Is.AssignableTo<ExecutionException>());
                Assert.That(ex.Message, Is.EqualTo(expectedMessage));

                ex = ex.InnerException;
                Assert.That(ex, Is.AssignableTo(expectedInnerType), ex?.Message ?? "-");
                Assert.That(ex.Message, Is.EqualTo(expectedInnerMessage));
            }

            private static void SetMetadataByConstructor()
            {
                var template = new SubjectMetadata()
                {
                    ExpectedExceptionType = typeof(ProbeException),
                    ToUncontinuable = true,
                    RemainingExecutionCount = 10
                };

                SubjectMetadata metadata = null;

                var lab = new Lab<Model>(
                    expectedExceptionType: template.ExpectedExceptionType,
                    toUncontinuable: template.ToUncontinuable,
                    remainingExecutionCount: template.RemainingExecutionCount)
                {
                    Arranger = (_, md) => metadata = md
                };

                lab.Execute(new Model(), out var ex);

                Assert.That(ex, Is.AssignableTo<ExecutionException>());
                Assert.That(metadata, Is.Not.SameAs(template));

                Assert.That(metadata.ExpectedExceptionType, Is.EqualTo(template.ExpectedExceptionType));
                Assert.That(metadata.ToUncontinuable, Is.EqualTo(template.ToUncontinuable));
                Assert.That(metadata.RemainingExecutionCount, Is.EqualTo(template.RemainingExecutionCount));
            }

            private static void SetMetadataByDelegate()
            {
                var template = new SubjectMetadata()
                {
                    Metadata = new object(),
                    ExpectedExceptionType = typeof(ProbeException),
                    ToUncontinuable = true,
                    RemainingExecutionCount = 10
                };

                SubjectMetadata metadata = null;

                var lab = new Lab<Model>
                {
                    Arranger = (_, md) =>
                    {
                        md.Metadata = template.Metadata;
                        md.ExpectedExceptionType = template.ExpectedExceptionType;
                        md.ToUncontinuable = template.ToUncontinuable;
                        md.RemainingExecutionCount = template.RemainingExecutionCount;
                    },
                    Actor = (_, md) => metadata = md
                };

                lab.Execute(new Model(), out var ex);

                Assert.That(ex, Is.AssignableTo<ExecutionException>());
                Assert.That(metadata, Is.Not.SameAs(template));

                Assert.That(metadata.Metadata, Is.SameAs(template.Metadata));
                Assert.That(metadata.ExpectedExceptionType, Is.EqualTo(template.ExpectedExceptionType));
                Assert.That(metadata.ToUncontinuable, Is.EqualTo(template.ToUncontinuable));
                Assert.That(metadata.RemainingExecutionCount, Is.EqualTo(template.RemainingExecutionCount));
            }

            private static void MergeMetadataChooseRemainingExecutionCount(MergeMetadataCase testCase)
            {
                var current = new SubjectMetadata
                {
                    RemainingExecutionCount = testCase.Current
                };

                var template = new SubjectMetadata
                {
                    RemainingExecutionCount = testCase.Template
                };

                current.Merge(template);

                Assert.That(current.RemainingExecutionCount, Is.EqualTo(testCase.Expected));
            }

            private static void Copy(ExecuteModelCase testCase)
            {
                var copiedID = "Copied";
                var lab = CreateLab(testCase, out var _, out var _);

                var copied = lab.Copy(copiedID);

                Assert.That(copied.Id, Is.EqualTo(copiedID));
                Assert.That(copied.SetMetadata, Is.SameAs(lab.SetMetadata));
                Assert.That(copied.Arranger, Is.SameAs(lab.Arranger));
                Assert.That(copied.Actor, Is.SameAs(lab.Actor));
                Assert.That(copied.Asserter, Is.SameAs(lab.Asserter));

                var metadataTemplateOriginal = lab.MetadataTemplate;
                var metadataTemplateCopied = copied.MetadataTemplate;

                Assert.That(
                    metadataTemplateCopied.ExpectedExceptionType,
                    Is.EqualTo(metadataTemplateOriginal.ExpectedExceptionType));
                Assert.That(
                    metadataTemplateCopied.ToUnsustainable,
                    Is.EqualTo(metadataTemplateOriginal.ToUnsustainable));
                Assert.That(
                    metadataTemplateCopied.ToUncontinuable,
                    Is.EqualTo(metadataTemplateOriginal.ToUncontinuable));
                Assert.That(
                    metadataTemplateCopied.RemainingExecutionCount,
                    Is.EqualTo(metadataTemplateOriginal.RemainingExecutionCount));
            }
        }
        public class SequenceModel : BlackThunder.UniTest.Model
        {
            public int Value;
            public object Payload;
            public SubjectMetadata PreviousMetadata;
            public List<string> Operations { get; } = new();
            public List<string> Phases { get; } = new();
        }

        private class SequenceProject : Project<SequenceModel>
        {
            private readonly Lab<SequenceModel>[] _labs;

            public SequenceProject(ExecutionMode executionMode, bool wrongExpectation = false) : base(executionMode)
            {
                _labs = new[]
                {
                    CreateSequenceLab("Add2", 2, wrongExpectation),
                    CreateSequenceLab("Subtract1", -1, wrongExpectation)
                };
            }

            public override IEnumerable<ILab<SequenceModel>> CreateLabs(SequenceModel model) => _labs;

            // LB-R01..LB-R04: reuse two Labs while each replay owns its model and metadata.
            private static Lab<SequenceModel> CreateSequenceLab(string id, int delta, bool wrongExpectation)
            {
                Lab<SequenceModel> lab = null;

                lab = new Lab<SequenceModel>(id)
                {
                    SetMetadata = model =>
                    {
                        Assert.That(model.MetadataGroup, Is.Empty);
                        Assert.That(model.ExecutionCount, Is.EqualTo(model.Operations.Count + 1));
                        Assert.That(model.Phases, Is.EqualTo(ExpectedPhases(model.Operations)));
                        model.Phases.Add(id + ":SetMetadata");
                        model.Payload = new object();
                        return model.Payload;
                    },
                    Arranger = (model, metadata) =>
                    {
                        Assert.That(model.Phases.Last(), Is.EqualTo(id + ":SetMetadata"));
                        Assert.That(metadata, Is.Not.SameAs(model.PreviousMetadata));
                        Assert.That(metadata, Is.Not.SameAs(lab.MetadataTemplate));
                        Assert.That(metadata.Metadata, Is.SameAs(model.Payload));
                        Assert.That(metadata.ExpectedExceptionType, Is.Null);
                        Assert.That(metadata.ToUnsustainable, Is.False);
                        Assert.That(metadata.ToUncontinuable, Is.False);
                        Assert.That(metadata.RemainingExecutionCount, Is.EqualTo(-1));
                        Assert.That(model.MetadataGroup.Count, Is.EqualTo(1));
                        Assert.That(model.MetadataGroup[lab], Is.SameAs(metadata));
                        model.Phases.Add(id + ":Arrange");

                        if (model.ExecutionCount % 2 == 1)
                            metadata.ExpectedExceptionType = typeof(ProbeException);
                    },
                    Actor = (model, metadata) =>
                    {
                        Assert.That(model.Phases.Last(), Is.EqualTo(id + ":Arrange"));
                        Assert.That(metadata.Metadata, Is.SameAs(model.Payload));
                        model.Phases.Add(id + ":Act");
                        model.Value += delta;

                        if (model.ExecutionCount % 2 == 1)
                            throw new ProbeException("Expected sequence exception");
                    },
                    Asserter = (model, metadata) =>
                    {
                        Assert.That(model.Phases.Last(), Is.EqualTo(id + ":Act"));
                        Assert.That(metadata.Metadata, Is.SameAs(model.Payload));
                        Assert.That(lab.MetadataTemplate.ExpectedExceptionType, Is.Null);
                        model.Phases.Add(id + ":Assert");
                        model.Operations.Add(id);
                        model.PreviousMetadata = metadata;
                        AssertSequenceModel(model, model.Operations.ToArray(), wrongExpectation ? 1 : 0);
                    }
                };

                return lab;
            }
        }

        private static void AssertSucceed<TModel>(Node<TModel> result) where TModel : BlackThunder.UniTest.Model, new()
        {
            Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().ToJson());
            Assert.That(cancelled, Is.False);
            Assert.That(result.GetReport().Nodes.Skip(1).Select(node => node.Status), Is.All.EqualTo("Success"));
        }

        // LB-G01: this fixed catalog is independent of Project.CreateLabs and its case generator.
        private static string[] ExpectedScenarioIds()
        {
            var signatures = new[]
            {
                "FNNN", "NFNN", "SFNN", "NNFN", "NSFN", "SNFN", "SSFN",
                "NNNN", "NNNS", "NNNF", "NNSN", "NNSS", "NNSF",
                "NSNN", "NSNS", "NSNF", "NSSN", "NSSS", "NSSF",
                "SNNN", "SNNS", "SNNF", "SNSN", "SNSS", "SNSF",
                "SSNN", "SSNS", "SSNF", "SSSN", "SSSS", "SSSF"
            };

            var expected = new List<string>
            {
                "Execute_NullModel", "SetMetadata_ByConstructor", "SetMetadata_ByDelegate",
                "MergeMetadata_RemainingExecutionCount_Minus1_Minus1",
                "MergeMetadata_RemainingExecutionCount_Minus1_3",
                "MergeMetadata_RemainingExecutionCount_3_Minus1",
                "MergeMetadata_RemainingExecutionCount_1_10",
                "MergeMetadata_RemainingExecutionCount_10_1",
                "MergeMetadata_RemainingExecutionCount_0_5"
            };

            foreach (var signature in signatures)
            {
                var suffix = string.Join(
                    "_",
                    signature.Select(c => c == 'N' ? "None" : c == 'S' ? "Success" : "Failure"));

                foreach (var source in new[] { "Constructor", "Delegate" })
                {
                    expected.Add($"Execute_Model_{source}_{suffix}");
                    expected.Add($"Copy_{source}_{suffix}");
                }
            }

            return expected.ToArray();
        }

        private static IEnumerable<string> ExpectedPhases(IEnumerable<string> path) =>
            path.SelectMany(id => new[] { ":SetMetadata", ":Arrange", ":Act", ":Assert" }.Select(phase => id + phase));

        private static void AssertSequenceModel(SequenceModel model, string[] path, int wrongOffset = 0)
        {
            var expectedValue = 2 * path.Count(id => id == "Add2") - path.Count(id => id == "Subtract1");
            Assert.That(model.Value, Is.EqualTo(expectedValue + wrongOffset));
            Assert.That(model.Operations, Is.EqualTo(path));
            Assert.That(model.Phases, Is.EqualTo(ExpectedPhases(path)));
            Assert.That(model.ExecutionCount, Is.EqualTo(path.Length));
            Assert.That(model.GetExecutionHistory(), Is.EqualTo(string.Join("/", path)));
            Assert.That(model.MetadataGroup.Count, Is.EqualTo(1));
            Assert.That(model.MetadataGroup.Values.Single(), Is.SameAs(model.PreviousMetadata));
            Assert.That(model.Continuable, Is.True);
            Assert.That(model.Sustainable, Is.True);
            Assert.That(model.RemainingExecutionCount, Is.Null);
        }

        private static IEnumerable<Node<SequenceModel>> SequenceNodes(Node<SequenceModel> root)
        {
            foreach (var child in root.Afters)
            {
                yield return child;

                foreach (var descendant in SequenceNodes(child))
                    yield return descendant;
            }
        }

        private static void AssertSequenceTree(Node<SequenceModel> root, int depth, bool exhaustive)
        {
            AssertSucceed(root);
            var nodes = SequenceNodes(root).ToArray();
            Assert.That(nodes.Length, Is.EqualTo(exhaustive ? (1 << (depth + 1)) - 2 : 2 * depth));
            Assert.That(nodes.Max(node => node.Depth), Is.EqualTo(depth));
            Assert.That(nodes.Select(node => node.Model).Distinct().Count(), Is.EqualTo(nodes.Length));

            for (var level = 1; level <= depth; level++)
                Assert.That(nodes.Count(node => node.Depth == level), Is.EqualTo(exhaustive ? 1 << level : 2));

            foreach (var parent in new[] { root }.Concat(nodes).Where(node => node.Afters.Count > 0))
                Assert.That(parent.Afters.Select(node => node.Id), Is.EquivalentTo(new[] { "Add2", "Subtract1" }));

            foreach (var node in nodes)
            {
                var path = new Stack<string>();

                for (var current = node; current.Before != null; current = current.Before)
                    path.Push(current.Id);

                AssertSequenceModel(node.Model, path.ToArray());
            }

            Assert.That(root.Model.Operations, Is.Empty);
            Assert.That(root.Model.Phases, Is.Empty);
            Assert.That(root.Model.MetadataGroup, Is.Empty);
        }

        // LB-M01..LB-M05, LB-G01: every existing scenario must execute exactly once in both modes.
        [TestCase(ExecutionMode.Inline), TestCase(ExecutionMode.ThreadPool)]
        public async Task ScenarioMatrixExecutesEveryExpectedCase(ExecutionMode mode)
        {
            var result = await new Project(mode).Execute(1, stopOnFailure: false);
            var expected = ExpectedScenarioIds();

            AssertSucceed(result);
            Assert.That(expected.Length, Is.EqualTo(133));
            Assert.That(expected.Distinct().Count(), Is.EqualTo(133));
            Assert.That(result.GetCount(), Is.EqualTo(134));
            Assert.That(result.Afters.Select(node => node.Id), Is.EquivalentTo(expected));

            foreach (var node in result.Afters)
            {
                Assert.That(node.Depth, Is.EqualTo(1));
                Assert.That(node.Model.ExecutionCount, Is.EqualTo(1));
                Assert.That(node.Model.GetExecutionHistory(), Is.EqualTo(node.Id));
            }
        }

        // LB-R01..LB-R04, LB-G02: bounded branching covers all repeated and mixed paths of length three.
        [TestCase(ExecutionMode.Inline), TestCase(ExecutionMode.ThreadPool)]
        public async Task RepeatedLabsExploreEveryShortPath(ExecutionMode mode)
        {
            var result = await new SequenceProject(mode).Execute(3, stopOnFailure: false);

            AssertSequenceTree(result, 3, true);
        }

        // LB-R01..LB-R04, LB-G02: each seed must reach eight real executions, with two candidates per step.
        [TestCase(ExecutionMode.Inline, 0), TestCase(ExecutionMode.ThreadPool, 0)]
        [TestCase(ExecutionMode.Inline, 1), TestCase(ExecutionMode.ThreadPool, 1)]
        [TestCase(ExecutionMode.Inline, 2), TestCase(ExecutionMode.ThreadPool, 2)]
        public async Task ContinuousLabsReachRequestedDepth(ExecutionMode mode, int seed)
        {
            var result = await new SequenceProject(mode).ExecuteContinuously(8, seed, stopOnFailure: false);

            AssertSequenceTree(result, 8, false);
        }

        // LB-G03: a wrong independent expectation must produce a real failed execution.
        [TestCase(ExecutionMode.Inline), TestCase(ExecutionMode.ThreadPool)]
        public async Task WrongSequenceExpectationIsReportedAsFailure(ExecutionMode mode)
        {
            Node<SequenceModel> result;

            using (new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext())
            {
                result = await new SequenceProject(mode, true).Execute("Add2", stopOnFailure: false);
            }

            Assert.That(result.AllSucceed(out var cancelled), Is.False);
            Assert.That(cancelled, Is.False);
            Assert.That(result.GetCount(), Is.EqualTo(2));
            var failed = result.Afters.Single();
            Assert.That(failed.Id, Is.EqualTo("Add2"));
            Assert.That(failed.Model.ExecutionCount, Is.EqualTo(1));
            Assert.That(failed.Status, Is.EqualTo(Node<SequenceModel>.NodeStatus.Failure));
            Assert.That(failed.Exception.GetBaseException(), Is.TypeOf<AssertionException>());
        }

        //[Test]
        //public async Task Debug()
        //{
        //    var project = new Project();
        //    var result = await project.Execute("");

        //    result = result.GetLastNode().DetachAndRestore();
        //    result.Execute();
        //}
    }
}
