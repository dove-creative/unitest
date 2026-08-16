using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UniTest
{
    public interface IProject
    {
        int ProcessCount { get; }
        bool Executed { get; }

        void Cancel();
    }

    public abstract partial class Project<TModel> : IProject where TModel : Model, new()
    {
        // Front
        public int ProcessCount => (int)Interlocked.Read(ref _processCount);
        long _processCount = 0;

        public bool Executed => Interlocked.Read(ref _executed) != 0;
        long _executed = 0;
        

        // Internal
        int _targetDepth = int.MaxValue;
        volatile bool _failureOccurred = false;

        readonly Queue<Node<TModel>> _idleNodes = new();
        readonly Queue<Node<TModel>> _preparedNodes = new();

        readonly object _idleLock = new();
        readonly object _preparedLock = new();

        readonly HashSet<TestDesigner> _designers = new();
        readonly Dictionary<TestDesigner, Task> _designerTasks = new();
        readonly ConcurrentQueue<TestDesigner> _completedDesigners = new();

        readonly HashSet<TestExecutor> _executors = new();
        readonly Dictionary<TestExecutor, Task> _executorTasks = new();
        readonly ConcurrentQueue<TestExecutor> _completedExecutors = new();

        readonly CancellationTokenSource _cts = new();
        CancellationTokenRegistration _externalCancellationRegistration;
        bool _externalCancellationRegistered = false;

        public const string DividingLine = "==============================";


        // Content
        public Task<Node<TModel>> Execute(int depth, CancellationToken cancellationToken)
        {
            return Execute(depth, true, cancellationToken);
        }

        public async Task<Node<TModel>> Execute(int depth, bool stopOnFailure = true, CancellationToken cancellationToken = default)
        {
            DoExecute(cancellationToken);
            _targetDepth = depth;

            var ct = _cts.Token;
            var rootNode = new Node<TModel>(null);
            _idleNodes.Enqueue(rootNode);

            try
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();


                    lock (_idleLock)
                    {
                        while (_idleNodes.TryDequeue(out var node))
                        {
                            var designer = new TestDesigner(this, node);
                            _designers.Add(designer);

                            _designerTasks[designer] = designer.Execute(ct);
                        }
                    }

                    while (_completedDesigners.TryDequeue(out var designer))
                    {
                        _designers.Remove(designer);
                        ObserveTask(_designerTasks, designer);
                    }

                    lock (_preparedLock)
                    {
                        while (_preparedNodes.TryDequeue(out var node))
                        {
                            var executor = new TestExecutor(this, node);

                            _executors.Add(executor);
                            _executorTasks[executor] = executor.Execute(ct);
                        }
                    }

                    while (_completedExecutors.TryDequeue(out var executor))
                    {
                        _executors.Remove(executor);
                        ObserveTask(_executorTasks, executor);
                    }


                    if (stopOnFailure && _failureOccurred)
                    {
                        Logger.Log(DividingLine);
                        Logger.Log("Cancelling operation: a failure has occurred");

                        Cancel();
                        return rootNode;
                    }

                    if (_designers.Count == 0 && _executors.Count == 0)
                    {
                        bool isEmpty = true;

                        lock (_idleLock)
                            if (_idleNodes.Count > 0) isEmpty = false;

                        lock (_preparedLock)
                            if (_preparedNodes.Count > 0) isEmpty = false;

                        if (isEmpty)
                            break;
                    }

                    await Task.Yield();
                }

                return rootNode;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("The operation has been cancelled.");
                return rootNode;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                Cancel();
                return rootNode;
            }
            finally
            {
                CompleteExecution();
            }
        }

        public Task<Node<TModel>> Execute(string ids, CancellationToken cancellationToken)
        {
            return Execute(ids, true, cancellationToken);
        }

        public async Task<Node<TModel>> Execute(string ids, bool stopOnFailure = true, CancellationToken cancellationToken = default)
        {
            DoExecute(cancellationToken);

            var ct = _cts.Token;
            var rootNode = new Node<TModel>(null);
            var node = rootNode;

            try
            {
                foreach (var id in ids.Split(Model.Separator))
                {
                    await Task.Yield();

                    ct.ThrowIfCancellationRequested();

                    var lab = CreateLab(node.Model, id);
                    node = node.Append(lab, ct);

                    node.Execute();

                    if (node.Status == Node<TModel>.NodeStatus.Failure || node.Exception != null)
                        _failureOccurred = true;

                    if (stopOnFailure && _failureOccurred)
                    {
                        Logger.Log(DividingLine);
                        Logger.Log("Operation cancelled due to failure.");

                        Cancel();
                        return rootNode;
                    }
                }

                return rootNode;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("The operation has been cancelled.");
                return rootNode;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                Cancel();
                return rootNode;
            }
            finally
            {
                CompleteExecution();
            }
        }

        public Task<Node<TModel>> ExecuteContinuously(int depth, CancellationToken cancellationToken)
        {
            return ExecuteContinuously(depth, 0, true, cancellationToken);
        }

        public Task<Node<TModel>> ExecuteContinuously(int depth, int seed, CancellationToken cancellationToken)
        {
            return ExecuteContinuously(depth, seed, true, cancellationToken);
        }

        public async Task<Node<TModel>> ExecuteContinuously(int depth, int seed = 0, bool stopOnFailure = true, CancellationToken cancellationToken = default)
        {
            DoExecute(cancellationToken);

            var ct = _cts.Token;
            var rootNode = new Node<TModel>(null);
            var node = rootNode;

            try
            {
                for (int i = 0; i < depth; i++)
                {
                    await Task.Yield();
                    
                    ct.ThrowIfCancellationRequested();

                    await new TestDesigner(this, node).Execute(ct);
                    await Task.WhenAll(_preparedNodes.Select(n => new TestExecutor(this, n).Execute(ct)));

                    _completedDesigners.Clear();
                    _completedExecutors.Clear();

                    if (stopOnFailure && _failureOccurred)
                    {
                        Logger.Log(DividingLine);
                        Logger.Log("Operation cancelled due to failure.");

                        Cancel();
                        return rootNode;
                    }


                    var candidates = _idleNodes.Where(n => n.Model.Sustainable);
                    if (!candidates.Any()) break;

                    node = candidates
                        .OrderBy(c => c.ID)
                        .ElementAt(node.Model.GetDeterministicRandom(candidates.Count(), seed));

                    _preparedNodes.Clear();
                    _idleNodes.Clear();
                }

                return rootNode;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("The operation has been cancelled.");
                return rootNode;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                Cancel();
                return rootNode;
            }
            finally
            {
                CompleteExecution();
            }
        }

        void DoExecute(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _executed, 1, 0) != 0)
                throw new InvalidOperationException($"Project '{typeof(TModel)}' cannot be executed multiple times.");

            RegisterExternalCancellation(cancellationToken);
        }

        void ObserveTask<TWorker>(Dictionary<TWorker, Task> tasks, TWorker worker)
        {
            if (!tasks.TryGetValue(worker, out var task))
                return;

            tasks.Remove(worker);

            if (task == null || !task.IsCompleted)
                return;

            if (task.IsFaulted)
            {
                _failureOccurred = true;
                Logger.LogException(task.Exception.GetBaseException());
            }
        }

        void RegisterExternalCancellation(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
                return;

            _externalCancellationRegistration = cancellationToken.Register(Cancel);
            _externalCancellationRegistered = true;
        }

        void CompleteExecution()
        {
            if (_externalCancellationRegistered)
            {
                _externalCancellationRegistration.Dispose();
                _externalCancellationRegistered = false;
            }

            _cts.Dispose();
        }

        public void Cancel()
        {
            Interlocked.Exchange(ref _executed, 1);

            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException) { }
        }

        public abstract IEnumerable<ILab<TModel>> CreateLabs(TModel model);
        public virtual ILab<TModel> CreateLab(TModel model, string id)
        {
            return CreateLabs(model).FirstOrDefault(t => t.ID == id)
                ?? throw new ArgumentException($"No test found with id '{id}'.", nameof(id));
        }
    }
}
