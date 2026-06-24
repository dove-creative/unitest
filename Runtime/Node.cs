using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml;

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
        public XmlElement Report { get; private set; }

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

        
        // Content
        public Node(ILab<TModel> lab)
        {
            Depth = 0;

            Model = new();
            Lab = lab;

            Status = lab != null ? NodeStatus.Ready : NodeStatus.Root;
            SetXmlNode(null);
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

            Depth = before.Depth + 1;

            Model = new();
            Lab = lab;

            Rerun(before.GetTrace().Select(n => n.Lab), ct);

            Status = NodeStatus.Ready;
            SetXmlNode(before.Report);

            Before = before;
            before.Afters.Add(this);
        }
        void SetXmlNode(XmlNode parent)
        {
            if (parent == null)
            {
                var doc = new XmlDocument();
                Report = doc.CreateElement(ID);
            }
            else
            {
                Report = parent.OwnerDocument.CreateElement(ID);
                parent.AppendChild(Report);
            }

            if (Lab != null)
                Report.InnerText = "Waiting for execution";
            else
                Report.InnerText = "Root Node";
        }

        public Node<TModel> Append(ILab<TModel> lab, CancellationToken ct) => new(this, lab, ct);

        /// <summary>
        /// Restore creates a detached debug node with a copied report lineage, without mutating the original graph or report tree.
        /// </summary>
        public Node<TModel> DetachAndRestore(CancellationToken ct = default)
        {
            if (Lab == null)
                throw new InvalidOperationException("This node cannot be restored because Lab is null.");

            var node = new Node<TModel>(Lab)
            {
                Depth = Depth,
                Status = NodeStatus.Ready
            };

            var trace = GetTrace();
            node.Rerun(trace.Take(trace.Count - 1).Select(n => n.Lab), ct);

            node.SetXmlNode(CopyReportLineage());
            return node;
        }
        XmlNode CopyReportLineage()
        {
            var doc = new XmlDocument();
            XmlElement parent = null;
            var lineage = new Stack<XmlElement>();

            var current = Report.ParentNode;
            while (current is XmlElement element)
            {
                lineage.Push(element);
                current = element.ParentNode;
            }

            foreach (var element in lineage)
            {
                var copy = (XmlElement)doc.ImportNode(element, false);

                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child is XmlElement)
                        continue;

                    copy.AppendChild(doc.ImportNode(child, true));
                }

                if (parent == null)
                    doc.AppendChild(copy);
                else
                    parent.AppendChild(copy);

                parent = copy;
            }

            return parent;
        }
        void Rerun(IEnumerable<ILab<TModel>> labs, CancellationToken ct)
        {
            foreach (var lab in labs)
            {
                if (lab == null)
                    continue;

                var id = lab.ID;
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

                UpdateXml();
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

            UpdateXml();
        }
        void UpdateXml()
        {
            XmlAttribute attr;

            Report.InnerText = Status.ToString();

            if (Exception != null)
            {
                attr = Report.OwnerDocument.CreateAttribute("Report");
                attr.Value = Exception.ToString();
                Report.Attributes.Append(attr);
            }
            
            try
            {
                attr = Report.OwnerDocument.CreateAttribute("Model");
                attr.Value = Model.ToString();
                Report.Attributes.Append(attr);

                attr = Report.OwnerDocument.CreateAttribute("History");
                attr.Value = Model.GetExecutionHistory();
                Report.Attributes.Append(attr);

                attr = Report.OwnerDocument.CreateAttribute("Continuable");
                attr.Value = Continuable.ToString();
                Report.Attributes.Append(attr);
            }
            catch (Exception ex)
            {
                attr = Report.OwnerDocument.CreateAttribute("Error");
                attr.Value = ex.ToString();
                Report.Attributes.Append(attr);
            }
        }
    }
}
