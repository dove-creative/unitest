using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UniTest;

public class ProjectSchedulingTest
{
    [Test]
    public void CoordinatorWaitsWhileActorIsBlocked()
    {
        using var context = new ControlledContext();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var project = new ProjectMock(_ => new[]
        {
            new Lab<Model>("wait", actor: (_, _) =>
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("The actor was not released.");
            })
        });
        var execution = context.Start(() => project.Execute(1));
        Task[] workers = Array.Empty<Task>();

        try
        {
            context.PumpUntil(() => entered.IsSet);
            workers = GetWorkerTasks(project);

            // A blocked actor produces no new work. Only a finite number of
            // startup/completion notifications should remain on the context.
            Assert.That(context.Drain(32), Is.True, "The coordinator keeps scheduling itself without any state change.");
            Assert.That(execution.IsCompleted, Is.False);
        }
        finally
        {
            release.Set();
            context.Complete(Task.WhenAll(workers.Append(execution)));
        }

        Assert.That(execution.Result.AllSucceed(out _), Is.True);
    }

    [Test]
    public void CoordinatorWaitsForActualDesignerTaskCompletion()
    {
        using var context = new ControlledContext();
        using var release = new ManualResetEventSlim();
        var project = new ProjectMock(_ =>
        {
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("The designer was not released.");
            return Array.Empty<ILab<Model>>();
        });
        var execution = context.Start(() => project.Execute(1));
        var worker = GetWorkerTasks(project).Single();

        try
        {
            release.Set();
            Assert.That(context.BackgroundPost.Wait(TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(worker.IsCompleted, Is.False, "The designer's async continuation has not run yet.");

            context.RunNext();

            Assert.That(execution.IsCompleted && !worker.IsCompleted, Is.False,
                "A completion queue entry must not let the coordinator discard an unfinished Task.");
        }
        finally
        {
            release.Set();
            context.Complete(Task.WhenAll(execution, worker));
        }
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task PreparedLabRunsBeforeDesignerFinishes(ExecutionMode mode)
    {
        using var executed = new ManualResetEventSlim();
        var project = new ProjectMock(mode, _ => CreateLabs());

        var result = await Task.Run(() => project.Execute(1)).WaitAsync(TimeSpan.FromSeconds(15));

        Assert.That(result.AllSucceed(out _), Is.True);
        Assert.That(project.ProcessCount, Is.EqualTo(1));

        IEnumerable<ILab<Model>> CreateLabs()
        {
            yield return new Lab<Model>("first", actor: (_, _) => executed.Set());
            if (!executed.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Prepared work did not wake the coordinator while the designer was still running.");
        }
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task RepeatedWorkNotificationsCompleteAllBranches(ExecutionMode mode)
    {
        var project = new ProjectMock(mode, _ => new[] { new Lab<Model>("A"), new Lab<Model>("B") });

        var result = await project.Execute(4).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(result.AllSucceed(out _), Is.True);
        Assert.That(result.GetCount(), Is.EqualTo(31));
        Assert.That(project.ProcessCount, Is.EqualTo(30));
        Assert.That(GetWorkerTasks(project), Is.Empty);
    }

    [TestCase(ExecutionMode.ThreadPool)]
    [TestCase(ExecutionMode.Inline)]
    public async Task EmptyProjectCompletesOnDesignerNotification(ExecutionMode mode)
    {
        var project = new ProjectMock(mode, _ => Array.Empty<ILab<Model>>());

        var result = await project.Execute(1).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(result.Afters, Is.Empty);
        Assert.That(GetWorkerTasks(project), Is.Empty);
    }

    [Test]
    public void CancellationWakesWaitingCoordinator()
    {
        using var context = new ControlledContext();
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var project = new ProjectMock(_ => new[]
        {
            new Lab<Model>("wait", actor: (_, _) =>
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("The actor was not released.");
            })
        });
        var execution = context.Start(() => project.Execute(1, cancellation.Token));
        Task[] workers = Array.Empty<Task>();

        try
        {
            context.PumpUntil(() => entered.IsSet);
            workers = GetWorkerTasks(project);
            Assert.That(context.Drain(32), Is.True);

            cancellation.Cancel();
            // With the actor still blocked, cancellation alone must resume
            // the coordinator. It may then wait for workers as part of cleanup.
            context.RunNext();
        }
        finally
        {
            release.Set();
            context.Complete(Task.WhenAll(workers.Append(execution)));
        }

        Assert.That(project.ProcessCount, Is.EqualTo(1));
    }

    private static Task[] GetWorkerTasks(Project<Model> project)
    {
        return new[] { "_designerTasks", "_executorTasks" }
            .SelectMany(name => ((IDictionary)typeof(Project<Model>)
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(project)).Values.Cast<Task>())
            .ToArray();
    }

    private sealed class ControlledContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<Action> _callbacks = new();
        private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;
        public readonly ManualResetEventSlim BackgroundPost = new();

        public override void Post(SendOrPostCallback callback, object state)
        {
            if (Environment.CurrentManagedThreadId != _ownerThreadId)
                BackgroundPost.Set();
            _callbacks.Add(() => callback(state));
        }

        public Task<T> Start<T>(Func<Task<T>> action)
        {
            var previous = Current;
            SetSynchronizationContext(this);
            try { return action(); }
            finally { SetSynchronizationContext(previous); }
        }

        public void RunNext()
        {
            if (!_callbacks.TryTake(out var callback, TimeSpan.FromSeconds(10)))
                throw new TimeoutException("No coordinator or worker continuation arrived.");
            Invoke(callback);
        }

        public bool Drain(int maximumCallbacks)
        {
            for (int i = 0; i < maximumCallbacks; i++)
            {
                if (!_callbacks.TryTake(out var callback))
                    return true;
                Invoke(callback);
            }
            return _callbacks.Count == 0;
        }

        public void PumpUntil(Func<bool> completed)
        {
            var timeout = Stopwatch.StartNew();
            while (!completed())
            {
                if (timeout.Elapsed > TimeSpan.FromSeconds(10))
                    throw new TimeoutException("The test did not complete.");
                if (_callbacks.TryTake(out var callback, 10))
                    Invoke(callback);
            }
        }

        public void Complete(Task task)
        {
            PumpUntil(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }

        private void Invoke(Action callback)
        {
            var previous = Current;
            SetSynchronizationContext(this);
            try { callback(); }
            finally { SetSynchronizationContext(previous); }
        }

        public void Dispose()
        {
            BackgroundPost.Dispose();
            _callbacks.Dispose();
        }
    }
}
