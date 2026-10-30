using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlackThunder.UniTest;
using NUnit.Framework;

namespace BlackThunder.UniTest.Tests.Unit
{
    public class ProjectTest
    {
        private SampleProject _project;
        private int _maxDepth = 100;


        public sealed class ThrowingModel : Model
        {
            public ThrowingModel() => throw new ProbeException("Model construction failed.");
        }

        private sealed class ThrowingModelProject : Project<ThrowingModel>
        {
            public override IEnumerable<ILab<ThrowingModel>> CreateLabs(ThrowingModel model) => Array.Empty<ILab<ThrowingModel>>();
        }


        [SetUp]
        public void SetUp() => _project = new SampleProject();

        [TearDown]
        public void TearDown()
        {
            _project.Cancel();
            _project = null;
        }


        [TestCase(0), TestCase(1), TestCase(10)]
        public async Task Execute(int depth)
        {
            // Act
            var result = await _project.Execute(depth, true);


            // Assert
            var last = result;

            while (last.Afters.Count > 0)
            {
                Assert.That(last.Exception, Is.Null);
                last = last.Afters[0];
            }

            Assert.That(last.Model.ExecutionCount, Is.EqualTo(Math.Max(depth, 1)));
        }
        [Test]
        public async Task Execute_Targeted()
        {
            // Act
            var result = await _project.Execute(string.Join(Model.Separator, "pass", "pass", "pass"));


            // Assert
            var last = result;

            while (last.Afters.Count > 0)
            {
                Assert.That(last.Exception, Is.Null);
                last = last.Afters[0];
            }

            Assert.That(last.Model.ExecutionCount, Is.EqualTo(3));
        }
        [TestCase("missing", true, 0)]
        [TestCase("missing", false, 0)]
        [TestCase("pass/missing/pass", true, 1)]
        [TestCase("pass/missing/pass", false, 1)]
        public void Execute_Targeted_UnknownIdThrows(string ids, bool stopOnFailure, int expectedActorCalls)
        {
            int actorCalls = 0;
            var targetedProject = new ProjectMock(_ => new[]
            {
            new Lab<Model>("pass", actor: (_, _) => actorCalls++)
        });

            var exception = Assert.ThrowsAsync<ArgumentException>(() => targetedProject.Execute(ids, stopOnFailure));

            Assert.That(exception.ParamName, Is.EqualTo("id"));
            Assert.That(exception.Message, Does.Contain("missing"));
            Assert.That(actorCalls, Is.EqualTo(expectedActorCalls));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void Execute_Targeted_LabCreationExceptionPropagates(bool deferred)
        {
            var expected = new ProbeException("Cannot create replay labs.");
            var targetedProject = new ProjectMock(_ => deferred ? CreateLabs() : throw expected);

            var exception = Assert.ThrowsAsync<ProbeException>(() => targetedProject.Execute("missing"));

            Assert.That(exception, Is.SameAs(expected));

            IEnumerable<ILab<Model>> CreateLabs()
            {
                yield return new Lab<Model>("pass");
                throw expected;
            }
        }
        [TestCase("missing")]
        [TestCase("pass/missing/pass")]
        public async Task Run_Targeted_UnknownIdReturnsFalse(string ids)
        {
            var targetedProject = new ProjectMock(_ => new[] { new Lab<Model>("pass") });
            var reportPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ReplayRegression", Guid.NewGuid().ToString("N"));

            var result = await targetedProject.Run(reportPath, "replay", ids, printResult: false);

            Assert.That(result, Is.False);
        }
        [Test]
        public async Task Execute_Targeted_StopOnFailure()
        {
            // Arrange
            bool LabCreatedAfterFailure = false;
            var targetedProject = new ProjectMock(model =>
            {
                if (model.ExecutionCount > 0)
                    LabCreatedAfterFailure = true;

                return new[]
                {
                new Lab<Model>("fail", actor: (_, _) => throw new ProbeException()),
                new Lab<Model>("after")
                };
            });

            // Act
            var result = await targetedProject.Execute(string.Join(Model.Separator, "fail", "after"), true);


            // Assert
            Assert.That(LabCreatedAfterFailure, Is.False);
            Assert.That(result.Afters.Count, Is.EqualTo(1));

            var failed = result.Afters[0];
            Assert.That(failed.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
            Assert.That(failed.Exception, Is.AssignableTo<ExecutionException>());
            Assert.That(failed.Exception?.InnerException, Is.AssignableTo<ProbeException>());
            Assert.That(failed.Afters, Is.Empty);
        }
        [Test]
        public async Task Execute_Continuous()
        {
            // Act
            var result = await _project.ExecuteContinuously(100);


            // Assert
            Assert.That(result.AllSucceed(out var cancelled), Is.True);
            Assert.That(cancelled, Is.False);

            var last = result;

            while (last.Afters.Count > 0)
                last = last.Afters[0];

            Assert.That(last.Model.ExecutionCount, Is.EqualTo(100));
        }
        [TestCase(false)]
        [TestCase(true)]
        public async Task Execute_Continuous_LabCreationFailureIsRecorded(bool deferred)
        {
            var expected = new ProbeException("Cannot create continuous labs.");
            var project = new ProjectMock(model => model.ExecutionCount == 0
                ? new[] { new Lab<Model>("pass") }
                : deferred ? CreateLabs() : throw expected);

            var result = await project.ExecuteContinuously(10);

            Assert.That(result.AllSucceed(out var cancelled), Is.False);
            Assert.That(cancelled, Is.False);

            var failed = result.Afters[0];
            Assert.That(failed.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
            Assert.That(failed.Exception, Is.AssignableTo<ExecutionException>());
            Assert.That(failed.Exception?.InnerException, Is.SameAs(expected));

            IEnumerable<ILab<Model>> CreateLabs()
            {
                yield return new Lab<Model>("pass");
                throw expected;
            }
        }
        [TestCase("depth")]
        [TestCase("ids")]
        [TestCase("continuous")]
        public void Execute_ModelConstructionFailurePropagates(string entryPoint)
        {
            var project = new ThrowingModelProject();

            var exception = Assert.CatchAsync(() => entryPoint switch
            {
                "depth" => project.Execute(1),
                "ids" => project.Execute("any"),
                _ => project.ExecuteContinuously(1)
            });

            Assert.That(exception.GetBaseException(), Is.InstanceOf<ProbeException>());
            Assert.That(project.Executed, Is.True);
        }


        [TestCase(0), TestCase(1), TestCase(10)]
        public async Task FailureAt(int failureAt)
        {
            // Arrange
            _project.FailureAt = failureAt;

            // Act
            var result = await _project.Execute(_maxDepth, true);


            // Assert
            var last = result;

            while (last.Afters.Count > 0)
                last = last.Afters[0];

            Assert.That(last.Model.ExecutionCount, Is.EqualTo(Math.Max(failureAt, 1)));

            Assert.That(last.Exception, Is.AssignableTo<ExecutionException>(), "Act failed");
            Assert.That(last.Exception?.InnerException, Is.AssignableTo<ProbeException>());
        }


        [Test]
        public async Task Rerun_Idle()
        {
            // Arrange
            _project.FailureAt = 3;

            // Act
            var result = await _project.Execute(_maxDepth, true);


            // Assert
            var last = result;

            while (last.Afters.Count > 0)
                last = last.Afters[0];

            var restored = last.DetachAndRestore();
            restored.Execute();

            Assert.That(restored.Exception, Is.AssignableTo<ExecutionException>());
        }
        [Test]
        public async Task Rerun_Root()
        {
            // Arrange
            _project.FailureAt = 0;

            // Act
            var result = await _project.Execute(_maxDepth, true);

            // Assert
            Assert.Throws<InvalidOperationException>(() => result.DetachAndRestore());
        }
    }
}
