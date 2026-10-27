using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class ExecutionModeTest
{
    [Test]
    public void DefaultConstructorsPreserveThreadPoolMode()
    {
        Assert.That(new ModeProject().ExecutionMode, Is.EqualTo(ExecutionMode.ThreadPool));
        Assert.That(new ModeCompactProject().ExecutionMode, Is.EqualTo(ExecutionMode.ThreadPool));
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public void ConstructorsPreserveSelectedMode(ExecutionMode executionMode)
    {
        Assert.That(new ModeProject(executionMode).ExecutionMode, Is.EqualTo(executionMode));
        Assert.That(new ModeCompactProject(executionMode).ExecutionMode, Is.EqualTo(executionMode));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void ConstructorsRejectUnknownMode(int value)
    {
        var executionMode = (ExecutionMode)value;
        var projectException = Assert.Throws<ArgumentOutOfRangeException>(() => new ModeProject(executionMode));
        var compactException = Assert.Throws<ArgumentOutOfRangeException>(() => new ModeCompactProject(executionMode));

        Assert.That(projectException.ParamName, Is.EqualTo("executionMode"));
        Assert.That(compactException.ParamName, Is.EqualTo("executionMode"));
    }

    [TestCase(ExecutionMode.ThreadPool, false)]
    [TestCase(ExecutionMode.ThreadPool, true)]
    [TestCase(ExecutionMode.Inline, false)]
    [TestCase(ExecutionMode.Inline, true)]
    public void ExecutionPreservesResultsAndUsesSelectedScheduling(ExecutionMode executionMode, bool continuous)
    {
        using var context = new QueuedSynchronizationContext();
        var designerContexts = new ConcurrentQueue<SynchronizationContext>();
        var executorContexts = new ConcurrentQueue<SynchronizationContext>();
        var project = new ModeProject(executionMode, _ =>
        {
            designerContexts.Enqueue(SynchronizationContext.Current);
            return new[]
            {
                new Lab<Model>("advance", actor: (model, _) =>
                {
                    executorContexts.Enqueue(SynchronizationContext.Current);
                    model.Subject = model.Subject is int value ? value + 1 : 1;
                })
            };
        });
        Node<Model> result = null;

        context.Run(async () =>
        {
            result = continuous ? await project.ExecuteContinuously(2) : await project.Execute(2);
        });

        Assert.That(result.AllSucceed(out bool cancelled), Is.True);
        Assert.That(cancelled, Is.False);
        Assert.That(result.GetLastNode().Model.ExecutionCount, Is.EqualTo(2));
        Assert.That(result.GetLastNode().Model.Subject, Is.EqualTo(2));
        Assert.That(designerContexts, Is.Not.Empty);
        Assert.That(executorContexts, Is.Not.Empty);

        foreach (var observedContext in designerContexts.Concat(executorContexts))
        {
            if (executionMode == ExecutionMode.Inline)
                Assert.That(observedContext, Is.SameAs(context));
            else
                Assert.That(observedContext, Is.Null);
        }
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task DesignerFailureRemainsInResult(ExecutionMode executionMode)
    {
        var project = new ModeProject(executionMode, _ => throw new ProbeException());

        var result = await project.Execute(1);

        Assert.That(result.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
        Assert.That(result.Exception, Is.TypeOf<ExecutionException>());
        Assert.That(result.Exception.InnerException, Is.TypeOf<ProbeException>());
        Assert.That(result.Afters, Is.Empty);
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task ExecutorFailureRemainsInResult(ExecutionMode executionMode)
    {
        var project = new ModeProject(executionMode, _ => new[]
        {
            new Lab<Model>("fail", actor: (_, _) => throw new ProbeException())
        });

        var result = await project.Execute(1);
        var failedNode = result.Afters.Single();

        Assert.That(failedNode.Status, Is.EqualTo(Node<Model>.NodeStatus.Failure));
        Assert.That(failedNode.Exception, Is.TypeOf<ExecutionException>());
        Assert.That(failedNode.Exception.InnerException, Is.TypeOf<ProbeException>());
        Assert.That(failedNode.Afters, Is.Empty);
    }

    [TestCase(ExecutionMode.ThreadPool, "TestDesigner")]
    [TestCase(ExecutionMode.ThreadPool, "TestExecutor")]
    [TestCase(ExecutionMode.Inline, "TestDesigner")]
    [TestCase(ExecutionMode.Inline, "TestExecutor")]
    public async Task PreCancelledWorkerCompletesWithoutRunningUserCode(ExecutionMode executionMode, string workerName)
    {
        bool userCodeRan = false;
        var project = new ModeProject(executionMode, _ =>
        {
            userCodeRan = true;
            return Array.Empty<ILab<Model>>();
        });
        var node = workerName == "TestDesigner"
            ? new Node<Model>(null)
            : new Node<Model>(new Lab<Model>(actor: (_, _) => userCodeRan = true));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await ExecuteWorker(workerName, project, node, cancellation.Token);

        Assert.That(userCodeRan, Is.False);
        Assert.That(node.Status, Is.EqualTo(workerName == "TestDesigner"
            ? Node<Model>.NodeStatus.Root : Node<Model>.NodeStatus.Cancelled));
        Assert.That(project.ProcessCount, Is.EqualTo(workerName == "TestDesigner" ? 0 : 1));
    }

    [TestCase(ExecutionMode.ThreadPool, "TestDesigner")]
    [TestCase(ExecutionMode.ThreadPool, "TestExecutor")]
    [TestCase(ExecutionMode.Inline, "TestDesigner")]
    [TestCase(ExecutionMode.Inline, "TestExecutor")]
    public async Task CancellationDuringWorkerPreservesCancelledState(ExecutionMode executionMode, string workerName)
    {
        using var cancellation = new CancellationTokenSource();
        var project = new ModeProject(executionMode, _ =>
        {
            cancellation.Cancel();
            return new[] { new Lab<Model>() };
        });
        var node = workerName == "TestDesigner"
            ? new Node<Model>(null)
            : new Node<Model>(new Lab<Model>(actor: (_, _) => cancellation.Cancel()));

        await ExecuteWorker(workerName, project, node, cancellation.Token);

        Assert.That(node.Status, Is.EqualTo(Node<Model>.NodeStatus.Cancelled));
        Assert.That(node.Exception, Is.InstanceOf<OperationCanceledException>());
        Assert.That(project.ProcessCount, Is.EqualTo(workerName == "TestDesigner" ? 0 : 1));
    }

    private static Task ExecuteWorker(string workerName, Project<Model> project, Node<Model> node, CancellationToken cancellationToken)
    {
        var workerType = typeof(Project<Model>).GetNestedType(workerName, BindingFlags.NonPublic)
            .MakeGenericType(typeof(Model));
        var constructor = workerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(Project<Model>), typeof(Node<Model>) }, null);
        var worker = constructor.Invoke(new object[] { project, node });
        var execute = workerType.GetMethod("Execute", new[] { typeof(CancellationToken) });
        return (Task)execute.Invoke(worker, new object[] { cancellationToken });
    }

    private sealed class ModeProject : Project<Model>
    {
        private readonly Func<Model, IEnumerable<ILab<Model>>> _createLabs;

        public ModeProject() { }

        public ModeProject(ExecutionMode executionMode, Func<Model, IEnumerable<ILab<Model>>> createLabs = null)
            : base(executionMode)
        {
            _createLabs = createLabs;
        }

        public override IEnumerable<ILab<Model>> CreateLabs(Model model) => _createLabs?.Invoke(model);
    }

    private sealed class ModeCompactProject : CompactProject<Model>
    {
        public ModeCompactProject() { }

        public ModeCompactProject(ExecutionMode executionMode)
            : base(executionMode) { }

        public override IEnumerable<CompactLab<Model>> CreateCompactLabs(Model model) => Array.Empty<CompactLab<Model>>();
    }

}
