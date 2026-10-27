using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace UniTest
{
    public class Node<TModel> where TModel : Model, new()
    {
        // Front
        public string ID => Lab?.ID ?? "root";

        public enum NodeStatus
        {
            None,
            Root,
            Ready,
            Executing,
            Success,
            Failure,
            Cancelled,
        }
        public NodeStatus Status { get; private set; } = NodeStatus.None;

        public int Depth { get; private set; } 

        public Node<TModel> Before { get; }
        public List<Node<TModel>> Afters { get; } = new();

        public TModel Model { get; }
        public ILab<TModel> Lab { get; }

        public Exception Exception { get; private set; }

        // Control
        public bool Continuable =>
            Model.Continuable && (Status == NodeStatus.Root || Status == NodeStatus.Success);

        // Internal
        volatile bool _executed = false;
        string _modelDescription;
        string _history = string.Empty;
        ReportException _error;
        ReportException _reportError;
        IReadOnlyList<TraceEntry> _restoredAncestors = Array.Empty<TraceEntry>();

        internal sealed class TraceEntry
        {
            internal ILab<TModel> Lab { get; }
            internal NodeReport Report { get; }

            internal TraceEntry(ILab<TModel> lab, NodeReport report)
            {
                Lab = lab;
                Report = report;
            }
        }

        
        // Content
        public Node(ILab<TModel> lab)
        {
            ValidateLabId(lab);
            Depth = 0;

            Model = new();
            Lab = lab;

            Status = lab != null ? NodeStatus.Ready : NodeStatus.Root;
        }
        public Node(Node<TModel> before, ILab<TModel> lab, CancellationToken ct)
        {
            if (lab == null)
                throw new InvalidOperationException("Lab of a non-root Node cannot be null.");
            if (before == null)
                throw new ArgumentNullException(nameof(before), "Before Node cannot be null.");
            if (!before.Continuable)
                throw new InvalidOperationException($"Before Node must be in 'Continuable' state(Root or Succeded) but was '{before.Status}'.");

            ct.ThrowIfCancellationRequested();
            ValidateLabId(lab);

            Depth = before.Depth + 1;

            Model = new();
            Lab = lab;

            Rerun(before.GetReplayLabs(), ct);
            _history = Model.GetExecutionHistory();

            Status = NodeStatus.Ready;

            Before = before;
            before.Afters.Add(this);
        }
        static void ValidateLabId(ILab<TModel> lab)
        {
            if (lab?.ID?.Contains(UniTest.Model.Separator) == true)
                throw new ArgumentException($"Lab ID '{lab.ID}' cannot contain '{UniTest.Model.Separator}', the execution path separator.", nameof(lab));
        }

        public Node<TModel> Append(ILab<TModel> lab, CancellationToken ct) => new(this, lab, ct);

        /// <summary>
        /// Creates a detached debug node with its replay path and diagnostic ancestry,
        /// without mutating the original graph or previously captured reports.
        /// </summary>
        public Node<TModel> DetachAndRestore(CancellationToken ct = default)
        {
            if (Lab == null)
                throw new InvalidOperationException("This node cannot be restored because Lab is null.");

            var node = new Node<TModel>(Lab)
            {
                Depth = Depth,
                Status = NodeStatus.Ready,
                _restoredAncestors = GetAncestors()
            };

            node.Rerun(node._restoredAncestors.Select(entry => entry.Lab), ct);
            node._history = node.Model.GetExecutionHistory();
            return node;
        }
        internal IReadOnlyList<TraceEntry> GetAncestors()
        {
            var trace = GetTrace();
            var lineage = new List<TraceEntry>(trace[0]._restoredAncestors);
            foreach (var ancestor in trace.Take(trace.Count - 1))
                lineage.Add(new TraceEntry(ancestor.Lab, ancestor.CaptureReport(0, null)));
            return lineage.AsReadOnly();
        }
        IEnumerable<ILab<TModel>> GetReplayLabs()
        {
            var trace = GetTrace();
            return trace[0]._restoredAncestors.Select(entry => entry.Lab)
                .Concat(trace.Select(node => node.Lab));
        }
        void Rerun(IEnumerable<ILab<TModel>> labs, CancellationToken ct)
        {
            foreach (var lab in labs)
            {
                if (lab == null)
                    continue;

                ValidateLabId(lab);
                lab.Execute(Model, out var ex);

                if (ex != null)
                    throw new ExecutionException("An error occurred while re-running Lab during the Append process.", ex);

                ct.ThrowIfCancellationRequested();
            }
        }
        List<Node<TModel>> GetTrace()
        {
            var current = this;
            var trace = new List<Node<TModel>> { current };

            while (current.Before != null)
            {
                trace.Add(current.Before);
                current = current.Before;
            }

            trace.Reverse();
            return trace;
        }


        public void Execute()
        {
            if (Lab == null)
                throw new InvalidOperationException("Cannot execute Node without Lab.");
            if (_executed)
                throw new InvalidOperationException("Cannot re-execute a node that has already been executed.");

            _executed = true;
            Status = NodeStatus.Executing;

            try
            {
                Lab.Execute(Model, out var exception);
                Exception = exception;
            }
            catch (Exception ex)
            {
                Model.Continuable = false;
                Exception = ex;
            }
            finally
            {
                if (Exception == null)
                    Status = NodeStatus.Success;
                else
                    Status = NodeStatus.Failure;

                CaptureDiagnostics();
            }
        }

        public void SetCancellationException(Exception ex = null)
        {
            SetExternalException(
                new OperationCanceledException("The operation cannot be performed because the Project has been cancelled.", ex),
                NodeStatus.Cancelled);
        }
        public void SetExternalException(Exception ex, NodeStatus status)
        {
            if (ex == null)
                throw new ArgumentNullException(nameof(ex), "The exception must not be null.");

            _executed = true;
            Status = status;
            Exception = ex;

            CaptureDiagnostics();
        }
        void CaptureDiagnostics()
        {
            _history = Model.GetExecutionHistory();
            _error = ReportException.Capture(Exception);
            _modelDescription = null;
            _reportError = null;
            try
            {
                _modelDescription = Model.ToString();
            }
            catch (Exception ex)
            {
                _reportError = ReportException.Capture(ex);
            }
        }

        internal NodeReport CaptureReport(int nodeId, int? parentId) => new(nodeId, parentId,
            Lab?.ID, Depth, Status.ToString(), _modelDescription, _history, Continuable, _error, _reportError);

        public override string ToString() => $"Node '{ID}' (Depth={Depth}, Status={Status})";
    }
}
