using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniTest
{
    public abstract partial class Project<TModel>
    {
        class TestDesigner
        {   
            // Internal
            Project<TModel> parent;
            Node<TModel> node;

            volatile bool executed = false;


            // Content
            public TestDesigner(Project<TModel> parent, Node<TModel> node)
            {
                this.parent = parent;
                this.node = node;
            }

            public async Task Execute(CancellationToken ct)
            {
                if (executed)
                    throw new InvalidOperationException("Test Designer cannot be executed multiple times.");

                executed = true;

#if UNITEST_SINGLETHREAD
                await Task.Yield();
                ExecuteCore(ct);
#else
                if (ct.IsCancellationRequested)
                {
                    parent.completedDesigners.Enqueue(this);
                    return;
                }

                await Task.Run(() => ExecuteCore(ct), ct);
#endif
            }

            void ExecuteCore(CancellationToken ct)
            {
                if (ct.IsCancellationRequested)
                {
                    parent.completedDesigners.Enqueue(this);
                    return;
                }

                Node<TModel> currentNode = node;
                IEnumerable<ILab<TModel>> labs;

                try
                {
                    labs = parent.CreateLabs(node.Model) ?? Array.Empty<Lab<TModel>>();
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"An exception occurred while creating labs for node '{node}'.", ex);

                    currentNode.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);
                    parent.completedDesigners.Enqueue(this);

                    throw ex;
                }


                try
                {
                    foreach (var lab in labs)
                    {
                        ct.ThrowIfCancellationRequested();

                        currentNode = node.Append(lab, ct);

                        lock (parent.preparedLock)
                            parent.preparedNodes.Enqueue(currentNode);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    currentNode.SetExternalException(ex, Node<TModel>.NodeStatus.Cancelled);
                }
                catch (Exception ex)
                {
                    ex = new ExecutionException(
                        $"Test Designer failed at node '{node}'.", ex);

                    node.SetExternalException(ex, Node<TModel>.NodeStatus.Failure);
                    throw ex;
                }
                finally
                {
                    parent.completedDesigners.Enqueue(this);
                }
            }
        }
    }
}
