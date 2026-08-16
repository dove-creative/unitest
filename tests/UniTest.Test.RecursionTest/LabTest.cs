using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class LabTest
{
    static void AssertSucceed(Node<Model> result)
    {
        Assert.That(result.AllSucceed(out var cancelled), Is.True, result.GetFailedReports().OuterXml);
        Assert.That(cancelled, Is.False);
    }

    public class Model : UniTest.Model
    {
        public Lab<Model> Lab
        {
            get => (Lab<Model>)Subject;
            set => Subject = value;
        }


        // Mock
        public Model model;
        public object metadata;

        public Func<Model, object> setMetadata;
        public Action<Model, SubjectMetadata> arranger;
        public Action<Model, SubjectMetadata> actor;
        public Action<Model, SubjectMetadata> asserter;
        public SubjectMetadata subjectMetadata;
    }

    public enum Condition
    {
        None,
        Success,
        Failure
    }

    class Project : Project<Model>
    {
        public override IEnumerable<ILab<Model>> CreateLabs(Model model)
        {
            if (model.ExecutionCount > 0)
                yield break;

            yield return Scenario("Execute_NullModel", ExecuteNullModel);

            foreach (var testCase in GetExecuteModelCases())
            {
                var captured = testCase;
                yield return Scenario(
                    $"Execute_Model_{(captured.SetMetadataByConstructor ? "Constructor" : "Delegate")}_{captured.SetMetadata}_{captured.Arranger}_{captured.Actor}_{captured.Asserter}",
                    () => ExecuteModel(captured));
            }

            yield return Scenario("SetMetadata_ByConstructor", SetMetadataByConstructor);
            yield return Scenario("SetMetadata_ByDelegate", SetMetadataByDelegate);

            foreach (var testCase in GetMergeMetadataCases())
            {
                var captured = testCase;
                yield return Scenario(
                    $"MergeMetadata_RemainingExecutionCount_{CountName(captured.Current)}_{CountName(captured.Template)}",
                    () => MergeMetadataChooseRemainingExecutionCount(captured));
            }

            foreach (var testCase in GetExecuteModelCases())
            {
                var captured = testCase;
                yield return Scenario(
                    $"Copy_{(captured.SetMetadataByConstructor ? "Constructor" : "Delegate")}_{captured.SetMetadata}_{captured.Arranger}_{captured.Actor}_{captured.Asserter}",
                    () => Copy(captured));
            }
        }

        static Lab<Model> Scenario(string id, Action test)
        {
            return new Lab<Model>(id, actor: (_, _) => test());
        }

        static string CountName(int count)
        {
            return count < 0 ? $"Minus{-count}" : count.ToString();
        }

        static IEnumerable<ExecuteModelCase> GetExecuteModelCases()
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

        static IEnumerable<MergeMetadataCase> GetMergeMetadataCases()
        {
            yield return new MergeMetadataCase(-1, -1, -1);
            yield return new MergeMetadataCase(-1, 3, 3);
            yield return new MergeMetadataCase(3, -1, 3);
            yield return new MergeMetadataCase(1, 10, 1);
            yield return new MergeMetadataCase(10, 1, 1);
            yield return new MergeMetadataCase(0, 5, 0);
        }

        static Lab<Model> CreateLab(
            ExecuteModelCase testCase,
            out Model targetModel,
            out object metadata)
        {
            var lab = testCase.SetMetadataByConstructor
                ? new Lab<Model>(
                    expectedExceptionType: typeof(ProbeException),
                    toUnsustainable: true,
                    toUncontinuable: true,
                    remainingExecutionCount: 10)
                : new Lab<Model>();

            targetModel = new Model();
            metadata = null;

            if (testCase.SetMetadata != Condition.None)
            {
                metadata = new object();
                var expectedModel = targetModel;
                var expectedMetadata = metadata;

                lab.SetMetadata = m =>
                {
                    Assert.That(m, Is.SameAs(expectedModel));

                    if (testCase.SetMetadata == Condition.Failure)
                        throw new ProbeException("Set Metadata Failure");

                    return expectedMetadata;
                };
            }

            if (testCase.Arranger != Condition.None)
            {
                var expectedModel = targetModel;
                var expectedMetadata = metadata;

                lab.Arranger = (m, md) =>
                {
                    Assert.That(m, Is.SameAs(expectedModel));
                    Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                    if (testCase.Arranger == Condition.Failure)
                        throw new ProbeException("Arranger Failure");
                };
            }

            if (testCase.Actor != Condition.None)
            {
                var expectedModel = targetModel;
                var expectedMetadata = metadata;

                lab.Actor = (m, md) =>
                {
                    Assert.That(m, Is.SameAs(expectedModel));
                    Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                    if (testCase.Actor == Condition.Failure)
                        throw new ProbeException("Actor Failure");
                };
            }

            if (testCase.Asserter != Condition.None)
            {
                var expectedModel = targetModel;
                var expectedMetadata = metadata;

                lab.Asserter = (m, md) =>
                {
                    Assert.That(m, Is.SameAs(expectedModel));
                    Assert.That(md.Metadata, Is.SameAs(expectedMetadata));

                    if (testCase.Asserter == Condition.Failure)
                        throw new ProbeException("Asserter Failure");
                };
            }

            return lab;
        }

        static void ExecuteNullModel()
        {
            var lab = new Lab<Model>();

            lab.Execute(null, out var ex);

            Assert.That(ex, Is.AssignableTo<ExecutionException>());
            Assert.That(ex.Message, Is.EqualTo("Model setting failed"));
        }

        static void ExecuteModel(ExecuteModelCase testCase)
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
            Assert.That(targetModel.RemainingExecutionCount, Is.EqualTo(testCase.SetMetadataByConstructor ? 10 : null));
        }

        static void AssertExecutionException(
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

        static void SetMetadataByConstructor()
        {
            SubjectMetadata template = new()
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

        static void SetMetadataByDelegate()
        {
            SubjectMetadata template = new()
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

        static void MergeMetadataChooseRemainingExecutionCount(MergeMetadataCase testCase)
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

        static void Copy(ExecuteModelCase testCase)
        {
            var copiedID = "Copied";
            var lab = CreateLab(testCase, out var _, out var _);

            var copied = lab.Copy(copiedID);

            Assert.That(copied.ID, Is.EqualTo(copiedID));
            Assert.That(copied.SetMetadata, Is.SameAs(lab.SetMetadata));
            Assert.That(copied.Arranger, Is.SameAs(lab.Arranger));
            Assert.That(copied.Actor, Is.SameAs(lab.Actor));
            Assert.That(copied.Asserter, Is.SameAs(lab.Asserter));

            var metadataTemplateOriginal = lab.MetadataTemplate;
            var metadataTemplateCopied = copied.MetadataTemplate;

            Assert.That(metadataTemplateCopied.ExpectedExceptionType, Is.EqualTo(metadataTemplateOriginal.ExpectedExceptionType));
            Assert.That(metadataTemplateCopied.ToUnsustainable, Is.EqualTo(metadataTemplateOriginal.ToUnsustainable));
            Assert.That(metadataTemplateCopied.ToUncontinuable, Is.EqualTo(metadataTemplateOriginal.ToUncontinuable));
            Assert.That(metadataTemplateCopied.RemainingExecutionCount, Is.EqualTo(metadataTemplateOriginal.RemainingExecutionCount));
        }

        struct ExecuteModelCase
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

        struct MergeMetadataCase
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
    }



    [Test]
    public async Task Test()
    {
        // Arrange
        var project = new Project();

        // Act
        var result = await project.Execute(6);

        // Assert
        AssertSucceed(result);
    }

    [TestCase(0), TestCase(1), TestCase(2)]
    public async Task ContinuousTest(int seed)
    {
        // Arrange
        var project = new Project();

        // Act
        var result = await project.ExecuteContinuously(100, seed);

        // Assert
        AssertSucceed(result);
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
