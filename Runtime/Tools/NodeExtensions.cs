using System.Linq;
using System.Xml;

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

        public static XmlElement GetFailedReports<TModel>( this Node<TModel> node) where TModel : Model, new()
        {
            var report = (XmlElement)node.Report.CloneNode(true);

            PruneNonFailure(report, Node<TModel>.NodeStatus.Failure.ToString());
            return report;


            bool PruneNonFailure(XmlElement node, string failureText)
            {
                var children = node.ChildNodes.OfType<XmlElement>().ToList();
                foreach (var child in children)
                {
                    if (PruneNonFailure(child, failureText))
                        node.RemoveChild(child);
                }

                if (!node.ChildNodes.OfType<XmlElement>().Any())
                    return node.InnerText != failureText;

                return false;
            }
        }
    }
}
