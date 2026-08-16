using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class ProjectTest
{
    SampleProject _project;
    int _maxDepth = 100;


    [SetUp]
    public void SetUp()
    {
        _project = new SampleProject();
    }

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
