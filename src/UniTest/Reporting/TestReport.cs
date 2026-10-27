using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniTest
{
    /// <summary>An independent snapshot of an execution tree and its diagnostic values.</summary>
    public sealed class TestReport
    {
        public IReadOnlyList<NodeReport> Nodes { get; }

        internal TestReport(IEnumerable<NodeReport> nodes)
        {
            Nodes = Array.AsReadOnly(nodes.ToArray());
        }

        public string ToJson() => ReportJsonWriter.Write(this);

        // The writer escapes lone surrogates, so UTF-8 encoding is lossless and never emits a BOM.
        internal byte[] ToUtf8Json() => Encoding.UTF8.GetBytes(ToJson());
    }

    /// <summary>A node's values, without references to executable Labs, Models, or other nodes.</summary>
    public sealed class NodeReport
    {
        public int NodeId { get; }
        public int? ParentId { get; }
        public string LabId { get; }
        public int Depth { get; }
        public string Status { get; }
        public string Model { get; }
        public string History { get; }
        public bool Continuable { get; }
        public ReportException Error { get; }
        public ReportException ReportError { get; }

        internal NodeReport(int nodeId, int? parentId, string labId, int depth, string status,
            string model, string history, bool continuable, ReportException error, ReportException reportError)
        {
            NodeId = nodeId;
            ParentId = parentId;
            LabId = labId;
            Depth = depth;
            Status = status;
            Model = model;
            History = history;
            Continuable = continuable;
            Error = error;
            ReportError = reportError;
        }

        internal NodeReport WithIdentity(int nodeId, int? parentId) => new(nodeId, parentId,
            LabId, Depth, Status, Model, History, Continuable, Error, ReportError);
    }

    public sealed class ReportException
    {
        public string Type { get; }
        public string Message { get; }
        /// <summary>The full exception text, including inner exceptions and stack traces.</summary>
        public string Details { get; }

        ReportException(Exception exception)
        {
            Type = exception.GetType().FullName;
            Message = exception.Message;
            Details = exception.ToString();
        }

        internal static ReportException Capture(Exception exception) =>
            exception == null ? null : new ReportException(exception);
    }
}
