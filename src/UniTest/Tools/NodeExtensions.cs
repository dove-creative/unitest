using System;
using System.Collections.Generic;
using System.Linq;

namespace UniTest
{
    public static partial class Tools
    {
        public static Node<TModel> GetLastNode<TModel>(this Node<TModel> node) where TModel : Model, new()
        {
            var current = node;

            while (current.Afters.Count > 0)
                current = current.Afters[0];

            return current;
        }

        public static int GetCount<TModel>(this Node<TModel> node) where TModel : Model, new()
        {
            if (node == null)
                return 0;

            int count = 1;

            foreach (var child in node.Afters)
                count += child.GetCount();

            return count;
        }

        public static bool AllSucceed<TModel>(this Node<TModel> node, out bool cancelled) where TModel : Model, new()
        {
            var status = node.Status;

            if (status == Node<TModel>.NodeStatus.Failure)
            {
                cancelled = false;
                return false;
            }
            if (status == Node<TModel>.NodeStatus.Cancelled)
            {
                cancelled = true;
                return true;
            }

            cancelled = false;

            foreach (var _node in node.Afters)
            {
                if (!_node.AllSucceed(out var _cancelled))
                {
                    cancelled = false;
                    return false;
                }

                if (_cancelled)
                    cancelled = true;
            }

            return true;
        }

        /// <summary>
        /// Captures this subtree and its ancestor path after execution has finished.
        /// Concurrent mutation of the execution tree during capture is not supported.
        /// </summary>
        public static TestReport GetReport<TModel>(this Node<TModel> node) where TModel : Model, new()
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var nodes = new List<NodeReport>();
            int? parentId = null;
            foreach (var ancestor in node.GetAncestors())
            {
                var id = nodes.Count;
                nodes.Add(ancestor.Report.WithIdentity(id, parentId));
                parentId = id;
            }

            var pending = new Stack<(Node<TModel> Node, int? ParentId)>();
            pending.Push((node, parentId));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                var id = nodes.Count;
                nodes.Add(current.Node.CaptureReport(id, current.ParentId));
                for (var i = current.Node.Afters.Count - 1; i >= 0; i--)
                    pending.Push((current.Node.Afters[i], id));
            }
            return new TestReport(nodes);
        }

        /// <summary>Keeps failures and their ancestors, retaining the full report's node IDs.</summary>
        public static TestReport GetFailedReports<TModel>(this Node<TModel> node) where TModel : Model, new()
        {
            var report = node.GetReport();
            var keep = new bool[report.Nodes.Count];
            keep[0] = true;
            for (var i = report.Nodes.Count - 1; i >= 0; i--)
            {
                var current = report.Nodes[i];
                if (current.Status == Node<TModel>.NodeStatus.Failure.ToString())
                    keep[i] = true;
                if (keep[i] && current.ParentId.HasValue)
                    keep[current.ParentId.Value] = true;
            }
            return new TestReport(report.Nodes.Where(n => keep[n.NodeId]));
        }
    }
}
