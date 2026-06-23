using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class ProjectTest
{
    SampleProject project;
    int MaxDepth = 100;


    [SetUp]
    public void SetUp()
    {
        project = new SampleProject();
    }

    [TearDown]
    public void TearDown()
    {
        project.Cancel();
        project = null;
    }



    [TestCase(0), TestCase(1), TestCase(10)]
    public async Task Execute(int depth)
    {
        // Act
        var result = await project.Execute(depth, true);


        // Assert
        var last = result;

        while (last.Afters.Count > 0)
        {
            Assert.IsNull(last.Exception);
            last = last.Afters[0];
        }

        Assert.AreEqual(Math.Max(depth, 1), last.Model.ExecutionCount);
    }
    [Test]
    public async Task Execute_Targeted()
    {
        // Act
        var result = await project.Execute(string.Join(Model.Separator, "pass", "pass", "pass"));


        // Assert
        var last = result;

        while (last.Afters.Count > 0)
        {
            Assert.IsNull(last.Exception);
            last = last.Afters[0];
        }

        Assert.AreEqual(3, last.Model.ExecutionCount); 
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
        Assert.IsFalse(LabCreatedAfterFailure);
        Assert.AreEqual(1, result.Afters.Count);

        var failed = result.Afters[0];
        Assert.AreEqual(Node<Model>.NodeStatus.Failure, failed.Status);
        Assert.IsAssignableFrom<ExecutionException>(failed.Exception);
        Assert.IsAssignableFrom<ProbeException>(failed.Exception?.InnerException);
        Assert.IsEmpty(failed.Afters);
    }
    [Test]
    public async Task Execute_Continuous()
    {
        // Act
        var result = await project.ExecuteContinuously(100);


        // Assert
        Assert.IsTrue(result.AllSucceed(out var cancelled));
        Assert.IsFalse(cancelled);

        var last = result;

        while (last.Afters.Count > 0)
            last = last.Afters[0];

        Assert.AreEqual(100, last.Model.ExecutionCount);
    }


    [TestCase(0), TestCase(1), TestCase(10)]
    public async Task FailureAt(int failureAt)
    {
        // Arrange
        project.FailureAt = failureAt;

        // Act
        var result = await project.Execute(MaxDepth, true);


        // Assert
        var last = result;

        while (last.Afters.Count > 0)
            last = last.Afters[0];

        Assert.AreEqual(Math.Max(failureAt, 1), last.Model.ExecutionCount);

        Assert.IsAssignableFrom<ExecutionException>(last.Exception, "Act failed");
        Assert.IsAssignableFrom<ProbeException>(last.Exception?.InnerException);
    }



    [Test]
    public async Task Rerun_Idle()
    {
        // Arrange
        project.FailureAt = 3;

        // Act
        var result = await project.Execute(MaxDepth, true);


        // Assert
        var last = result;

        while (last.Afters.Count > 0)
            last = last.Afters[0];

        var restored = last.DetachAndRestore();
        restored.Execute();

        Assert.IsAssignableFrom<ExecutionException>(restored.Exception);
    }
    [Test]
    public async Task Rerun_Root()
    {
        // Arrange
        project.FailureAt = 0;

        // Act
        var result = await project.Execute(MaxDepth, true);

        // Assert
        Assert.Throws<InvalidOperationException>(() => result.DetachAndRestore());
    }
}
