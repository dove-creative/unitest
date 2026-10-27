using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

[NonParallelizable]
public class ProjectShutdownTest
{
    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task CancellationWaitsForRunningExecutor(ExecutionMode mode)
    {
        using var gate = new WorkerGate();
        using var cancellation = new CancellationTokenSource();
        var project = new ProjectMock(mode, _ => new[]
        {
            new Lab<Model>("slow", actor: (_, _) => gate.Wait())
        });
        var execution = Task.Run(() => project.Execute(2, cancellation.Token));

        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await AssertStillWaiting(execution);
        }
        finally
        {
            gate.Release.Set();
            await FinishExecution(project, execution);
        }

        var root = await execution;
        Assert.That(root.Afters.Single().Status, Is.Not.EqualTo(Node<Model>.NodeStatus.Executing));
        Assert.That(root.Afters.Single().Afters, Is.Empty);
        Assert.That(project.ProcessCount, Is.EqualTo(1));
        Assert.That(GetWorkerTasks(project), Is.Empty);
    }

    [TestCase(ExecutionMode.ThreadPool, false)]
    [TestCase(ExecutionMode.Inline, false)]
    [TestCase(ExecutionMode.ThreadPool, true)]
    [TestCase(ExecutionMode.Inline, true)]
    public async Task CancellationWaitsForDesignerAndObservesLateFault(ExecutionMode mode, bool fail)
    {
        using var gate = new WorkerGate();
        using var cancellation = new CancellationTokenSource();
        var expected = new ProbeException("Designer failed during shutdown.");
        int actorCalls = 0;
        int observedFaults = 0;
        var previousLogger = Logger.ExceptionLogger;
        Logger.ExceptionLogger = exception =>
        {
            if (ReferenceEquals(exception.InnerException, expected))
                Interlocked.Increment(ref observedFaults);
        };
        var project = new ProjectMock(mode, _ =>
        {
            gate.Wait();
            if (fail)
                throw expected;
            return new[] { new Lab<Model>("never", actor: (_, _) => actorCalls++) };
        });
        var execution = Task.Run(() => project.Execute(1, cancellation.Token));

        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            await AssertStillWaiting(execution);
        }
        finally
        {
            gate.Release.Set();
            try { await FinishExecution(project, execution); }
            finally { Logger.ExceptionLogger = previousLogger; }
        }

        var root = await execution;
        Assert.That(actorCalls, Is.Zero);
        Assert.That(root.Afters, Is.Empty);
        Assert.That(GetWorkerTasks(project), Is.Empty);
        Assert.That(root.Status, Is.EqualTo(fail ? Node<Model>.NodeStatus.Failure : Node<Model>.NodeStatus.Cancelled));
        Assert.That(observedFaults, Is.EqualTo(fail ? 1 : 0));
        if (fail)
            Assert.That(root.Exception.InnerException, Is.SameAs(expected));
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task StopOnFailureWaitsForOtherExecutor(ExecutionMode mode)
    {
        using var gate = new WorkerGate();
        var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var project = new ProjectMock(mode, _ => new[]
        {
            new Lab<Model>("slow", actor: (_, _) => gate.Wait()),
            new Lab<Model>("fail", actor: (_, _) =>
            {
                if (!gate.Entered.Task.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("The slow actor did not start.");
                throw new ProbeException();
            })
        });
        using var registration = GetInternalCancellation(project).Register(() => stopped.TrySetResult(true));
        var execution = Task.Run(() => project.Execute(1));

        try
        {
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await AssertStillWaiting(execution);
        }
        finally
        {
            gate.Release.Set();
            await FinishExecution(project, execution);
        }

        var root = await execution;
        Assert.That(root.AllSucceed(out _), Is.False);
        Assert.That(root.Afters.All(node => node.Status != Node<Model>.NodeStatus.Executing), Is.True);
        Assert.That(project.ProcessCount, Is.EqualTo(2));
        Assert.That(GetWorkerTasks(project), Is.Empty);

        var reportAtReturn = root.GetReport().ToJson();
        var countAtReturn = project.ProcessCount;
        await Task.Yield();
        Assert.That(root.GetReport().ToJson(), Is.EqualTo(reportAtReturn));
        Assert.That(project.ProcessCount, Is.EqualTo(countAtReturn));
    }

    [Test]
    public async Task RunnerWaitsAfterTimeLimitAndRequestsCancellationOnce()
    {
        using var gate = new WorkerGate();
        var limitReached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int cancellationMessages = 0;
        var previousLogger = Logger.NormalLogger;
        Logger.NormalLogger = message =>
        {
            if (message.StartsWith("Cancelling test: time limit exceeded", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref cancellationMessages);
                limitReached.TrySetResult(true);
            }
        };
        var project = new ProjectMock(_ => new[] { new Lab<Model>("slow", actor: (_, _) => gate.Wait()) });
        var reportPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ShutdownRegression", Guid.NewGuid().ToString("N"));
        var execution = project.Run(reportPath, "shutdown", 1, timeLimit: 1, printResult: false);

        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await limitReached.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await AssertStillWaiting(execution);
            Assert.That(Volatile.Read(ref cancellationMessages), Is.EqualTo(1));
        }
        finally
        {
            gate.Release.Set();
            try { await FinishExecution(project, execution); }
            finally { Logger.NormalLogger = previousLogger; }
        }

        Assert.That(project.ProcessCount, Is.EqualTo(1));
        Assert.That(GetWorkerTasks(project), Is.Empty);
    }

    private static async Task AssertStillWaiting(Task execution)
    {
        // The worker remains blocked by a gate for the entire observation window.
        // The delay gives a broken coordinator an opportunity to return early.
        await Task.WhenAny(execution, Task.Delay(100));
        Assert.That(execution.IsCompleted, Is.False, "Execution returned while a worker was still running.");
    }

    private static async Task FinishExecution(Project<Model> project, Task execution)
    {
        await execution.WaitAsync(TimeSpan.FromSeconds(10));
        // Also clean up workers when exercising the old, early-return behavior.
        var workers = Task.WhenAll(GetWorkerTasks(project));
        try { await workers.WaitAsync(TimeSpan.FromSeconds(10)); }
        catch when (workers.IsCompleted) { }
    }

    private static Task[] GetWorkerTasks(Project<Model> project) =>
        new[] { "_designerTasks", "_executorTasks" }
            .SelectMany(name => ((IDictionary)typeof(Project<Model>)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(project)).Values.Cast<Task>())
            .ToArray();

    private static CancellationToken GetInternalCancellation(Project<Model> project) =>
        ((CancellationTokenSource)typeof(Project<Model>)
            .GetField("_cts", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(project)).Token;

    private sealed class WorkerGate : IDisposable
    {
        public readonly TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly ManualResetEventSlim Release = new();

        public void Wait()
        {
            Entered.TrySetResult(true);
            if (!Release.Wait(TimeSpan.FromSeconds(15)))
                throw new TimeoutException("The test did not release the worker.");
        }

        public void Dispose() => Release.Dispose();
    }
}
