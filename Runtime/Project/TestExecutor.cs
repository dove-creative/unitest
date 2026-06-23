using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniTest
{
    public abstract partial class Project<TModel>
    {
        class TestExecutor
        {
            // Internal
            Project<TModel> parent;
            Node<TModel> node;

            volatile bool executed = false;


            // Content
            public TestExecutor(Project<TModel> parent, Node<TModel> node)
            {
                this.parent = parent;
                this.node = node;
            }

            public async Task Execute(CancellationToken ct)
            {
                if (executed)
                {
                    throw new InvalidOperationException(
                        "Test Designer cannot be executed multiple times.");
                }

                executed = true;

#if UNITEST_SINGLETHREAD
                await Task.Yield();
                ExecuteCore(ct);
#else
                if (ct.IsCancellationRequested)
                {
                    ExecuteCore(ct);
                    return;
                }

                await Task.Run(() => ExecuteCore(ct), ct);
#endif
            }

            void ExecuteCore(CancellationToken ct)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    node.Execute();

                    ct.ThrowIfCancellationRequested();

                    if (node.Continuable && node.Depth < parent.targetDepth)
                    {
                        lock (parent.idleLock)
                            parent.idleNodes.Enqueue(node);
                    }
                    else if (node.Status == Node<TModel>.NodeStatus.Failure)
                    {
                        parent.failureOccurred = true;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    node.SetCancellationException(ex);
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"Test Executor failed at node '{node}'.", ex);

                    node.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);
                    throw ex;
                }
                finally
                {
                    Interlocked.Increment(ref parent._processCount);
                    parent.completedExecutors.Enqueue(this);
                }
            }
        }
    }
}
