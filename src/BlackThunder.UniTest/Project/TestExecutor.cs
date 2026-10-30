using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlackThunder.UniTest
{
    public abstract partial class Project<TModel>
    {
        private class TestExecutor
        {
            // Internal
            private Project<TModel> _parent;
            private Node<TModel> _node;

            private volatile bool _executed = false;


            // Content
            public TestExecutor(Project<TModel> parent, Node<TModel> node)
            {
                this._parent = parent;
                this._node = node;
            }

            public async Task Execute(CancellationToken ct)
            {
                if (_executed)
                {
                    throw new InvalidOperationException(
                        "Test Executor cannot be executed multiple times.");
                }

                _executed = true;

                if (_parent.ExecutionMode == BlackThunder.UniTest.ExecutionMode.Inline)
                {
                    await Task.Yield();
                    ExecuteCore(ct);
                }
                else
                {
                    if (ct.IsCancellationRequested)
                    {
                        ExecuteCore(ct);
                        return;
                    }

                    await Task.Run(() => ExecuteCore(ct), ct);
                }
            }

            private void ExecuteCore(CancellationToken ct)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();

                    _node.Execute();

                    ct.ThrowIfCancellationRequested();

                    if (_node.Continuable && _node.Depth < _parent._targetDepth)
                    {
                        lock (_parent._idleLock)
                            _parent._idleNodes.Enqueue(_node);

                        _parent.SignalStateChange();
                    }
                    else if (_node.Status == Node<TModel>.NodeStatus.Failure)
                    {
                        _parent._failureOccurred = true;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    _node.SetCancellationException(ex);
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"Test Executor failed at node '{_node}'.", ex);

                    _node.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);
                    throw ex;
                }
                finally
                {
                    Interlocked.Increment(ref _parent._processCount);
                }
            }
        }
    }
}
