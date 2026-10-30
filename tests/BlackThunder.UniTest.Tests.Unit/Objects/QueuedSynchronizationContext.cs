using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlackThunder.UniTest.Tests.Unit
{
    internal sealed class QueuedSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<Action> _callbacks = new BlockingCollection<Action>();

        public override void Post(SendOrPostCallback callback, object state) => _callbacks.Add(() => callback(state));

        public void Run(Func<Task> action)
        {
            var previousContext = Current;
            SetSynchronizationContext(this);
            try
            {
                var execution = action();
                var completion = execution.ContinueWith(_ => _callbacks.Add(() => { }),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                while (!execution.IsCompleted)
                {
                    // This timeout only bounds a broken test; scheduling assertions use the observed context.
                    if (!_callbacks.TryTake(out var callback, TimeSpan.FromSeconds(10)))
                        throw new TimeoutException("The execution did not make progress on the synchronization context.");

                    callback();
                }

                completion.GetAwaiter().GetResult();
                execution.GetAwaiter().GetResult();
            }
            finally
            {
                SetSynchronizationContext(previousContext);
            }
        }

        public void Dispose() => _callbacks.Dispose();
    }
}
