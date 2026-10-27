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
        public ExecutionMode ExecutionMode { get; }

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

        readonly Dictionary<TestDesigner, Task> _designerTasks = new();
        readonly ConcurrentQueue<TestDesigner> _completedDesigners = new();

        readonly Dictionary<TestExecutor, Task> _executorTasks = new();
        readonly ConcurrentQueue<TestExecutor> _completedExecutors = new();

        readonly object _signalLock = new();
        SemaphoreSlim _stateChanged;

        readonly CancellationTokenSource _cts = new();
        CancellationTokenRegistration _externalCancellationRegistration;
        bool _externalCancellationRegistered = false;

        public const string DividingLine = "==============================";


        // Content
        protected Project()
            : this(UniTest.ExecutionMode.ThreadPool) { }

        protected Project(ExecutionMode executionMode)
        {
            if (executionMode != UniTest.ExecutionMode.ThreadPool && executionMode != UniTest.ExecutionMode.Inline)
                throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, "Unknown execution mode.");

            ExecutionMode = executionMode;
        }

        public Task<Node<TModel>> Execute(int depth, CancellationToken cancellationToken)
        {
            return Execute(depth, true, cancellationToken);
        }

        public async Task<Node<TModel>> Execute(int depth, bool stopOnFailure = true, CancellationToken cancellationToken = default)
        {
            DoExecute(cancellationToken);
            _targetDepth = depth;

            var ct = _cts.Token;
            var stateChanged = _stateChanged = new SemaphoreSlim(0, 1);
            Node<TModel> rootNode = null;

            try
            {
                // Model construction runs user code, so it shares the error and cleanup paths.
                rootNode = new Node<TModel>(null);
                _idleNodes.Enqueue(rootNode);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    // Consume the previous notification before inspecting state.
                    // Changes during this pass retain a notification for the next wait.
                    stateChanged.Wait(0);

                    lock (_idleLock)
                    {
                        while (_idleNodes.Count > 0)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (stopOnFailure && _failureOccurred)
                                break;

                            var node = _idleNodes.Dequeue();
                            var designer = new TestDesigner(this, node);
                            TrackTask(_designerTasks, _completedDesigners, designer, designer.Execute(ct));
                        }
                    }

                    while (_completedDesigners.TryDequeue(out var designer))
                    {
                        ObserveTask(_designerTasks, designer);
                    }

                    lock (_preparedLock)
                    {
                        while (_preparedNodes.Count > 0)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (stopOnFailure && _failureOccurred)
                                break;

                            var node = _preparedNodes.Dequeue();
                            var executor = new TestExecutor(this, node);

                            TrackTask(_executorTasks, _completedExecutors, executor, executor.Execute(ct));
                        }
                    }

                    while (_completedExecutors.TryDequeue(out var executor))
                    {
                        ObserveTask(_executorTasks, executor);
                    }


                    if (stopOnFailure && _failureOccurred)
                    {
                        Logger.Log(DividingLine);
                        Logger.Log("Cancelling operation: a failure has occurred");

                        Cancel();
                        return rootNode;
                    }

                    if (_designerTasks.Count == 0 && _executorTasks.Count == 0)
                    {
                        bool isEmpty = true;

                        lock (_idleLock)
                            if (_idleNodes.Count > 0) isEmpty = false;

                        lock (_preparedLock)
                            if (_preparedNodes.Count > 0) isEmpty = false;

                        if (isEmpty)
                            break;
                    }

                    await stateChanged.WaitAsync(ct);
                }

                return rootNode;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("The operation has been cancelled.");
                return rootNode;
            }
            catch
            {
                // Worker failures are recorded on Nodes and observed above;
                // anything reaching here was not recorded, so it must not look like a result.
                Cancel();
                throw;
            }
            finally
            {
                try
                {
                    await WaitForWorkers();
                }
                finally
                {
                    CompleteExecution();
                }
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
            Node<TModel> rootNode = null;

            try
            {
                rootNode = new Node<TModel>(null);
                var node = rootNode;

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
            catch
            {
                Cancel();
                throw;
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
            Node<TModel> rootNode = null;

            try
            {
                rootNode = new Node<TModel>(null);
                var node = rootNode;

                for (int i = 0; i < depth; i++)
                {
                    await Task.Yield();
                    
                    ct.ThrowIfCancellationRequested();

                    await new TestDesigner(this, node).Execute(ct);
                    await Task.WhenAll(_preparedNodes.Select(n => new TestExecutor(this, n).Execute(ct)));

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
            catch (ExecutionException ex)
            {
                // Designers and executors record their failure on a Node before throwing.
                Logger.LogException(ex);

                Cancel();
                return rootNode;
            }
            catch
            {
                Cancel();
                throw;
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

        void SignalStateChange()
        {
            lock (_signalLock)
            {
                // Coalesce notifications; the coordinator inspects all queues on wake-up.
                if (_stateChanged != null && _stateChanged.CurrentCount == 0)
                    _stateChanged.Release();
            }
        }

        void TrackTask<TWorker>(Dictionary<TWorker, Task> tasks, ConcurrentQueue<TWorker> completed, TWorker worker, Task task)
        {
            tasks.Add(worker, task);

            // Publish completion only after the entire async worker Task has ended,
            // including faults and cancellation before its core starts running.
            _ = task.ContinueWith(_ =>
            {
                completed.Enqueue(worker);
                SignalStateChange();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        void ObserveTask<TWorker>(Dictionary<TWorker, Task> tasks, TWorker worker)
        {
            if (!tasks.TryGetValue(worker, out var task))
                return;

            tasks.Remove(worker);

            if (task.IsFaulted)
            {
                _failureOccurred = true;
                Logger.LogException(task.Exception.GetBaseException());
            }
        }

        async Task WaitForWorkers()
        {
            // Only the coordinator starts workers and changes these dictionaries.
            // Once scheduling stops, this snapshot covers every remaining worker.
            var tasks = _designerTasks.Values.Concat(_executorTasks.Values).ToArray();
            try
            {
                // Do not cancel this wait: callbacks must finish before the result
                // is returned or cancellation resources are disposed.
                await Task.WhenAll(tasks);
            }
            catch
            {
                // WhenAll waits for every worker, even if one faults or cancels.
                // Inspect each worker below so late failures are also observed.
            }

            foreach (var designer in _designerTasks.Keys.ToArray())
                ObserveTask(_designerTasks, designer);
            foreach (var executor in _executorTasks.Keys.ToArray())
                ObserveTask(_executorTasks, executor);

            _idleNodes.Clear();
            _preparedNodes.Clear();
            _completedDesigners.Clear();
            _completedExecutors.Clear();
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
            lock (_signalLock)
            {
                // Completion notifications can still be finishing after worker
                // Tasks end. They must not release a disposed semaphore.
                _stateChanged?.Dispose();
                _stateChanged = null;
            }

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
