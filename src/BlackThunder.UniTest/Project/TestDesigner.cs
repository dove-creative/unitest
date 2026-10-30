using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackThunder.UniTest
{
    public abstract partial class Project<TModel>
    {
        private class TestDesigner
        {
            // Internal
            private Project<TModel> _parent;
            private Node<TModel> _node;

            private volatile bool _executed = false;


            // Content
            public TestDesigner(Project<TModel> parent, Node<TModel> node)
            {
                this._parent = parent;
                this._node = node;
            }

            public async Task Execute(CancellationToken ct)
            {
                if (_executed)
                    throw new InvalidOperationException("Test Designer cannot be executed multiple times.");

                _executed = true;

                if (_parent.ExecutionMode == BlackThunder.UniTest.ExecutionMode.Inline)
                {
                    await Task.Yield();
                    ExecuteCore(ct);
                }
                else
                {
                    if (ct.IsCancellationRequested)
                        return;

                    await Task.Run(() => ExecuteCore(ct), ct);
                }
            }

            private void ExecuteCore(CancellationToken ct)
            {
                if (ct.IsCancellationRequested)
                    return;

                Node<TModel> currentNode = _node;
                IEnumerable<ILab<TModel>> labs;

                try
                {
                    labs = _parent.CreateLabs(_node.Model) ?? Array.Empty<Lab<TModel>>();
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"An exception occurred while creating labs for node '{_node}'.", ex);

                    currentNode.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);

                    throw ex;
                }


                try
                {
                    foreach (var lab in labs)
                    {
                        ct.ThrowIfCancellationRequested();

                        currentNode = _node.Append(lab, ct);

                        lock (_parent._preparedLock)
                            _parent._preparedNodes.Enqueue(currentNode);

                        _parent.SignalStateChange();
                    }
                }
                catch (OperationCanceledException ex)
                {
                    currentNode.SetExternalException(ex, Node<TModel>.NodeStatus.Cancelled);
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"Test Designer failed at node '{_node}'.", ex);

                    _node.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);
                    throw ex;
                }
            }
        }
    }
}
