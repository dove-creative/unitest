using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BlackThunder.UniTest
{
    /// <summary>
    /// Writes reports as indented JSON without package dependencies, so the netstandard2.1
    /// assembly can be copied into hosts that do not restore NuGet dependencies, such as Unity.
    /// </summary>
    internal static class ReportJsonWriter
    {
        private const string NewLine = "\n";
        private const string Indent = "  ";

        internal static string Write(TestReport report)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            AppendProperty(sb, 1, "nodes", first: true);
            AppendArray(sb, 1, report.Nodes);
            sb.Append(NewLine).Append('}');
            return sb.ToString();
        }

        private static void AppendArray(StringBuilder sb, int depth, IReadOnlyList<NodeReport> nodes)
        {
            if (nodes.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            sb.Append('[');
            for (int i = 0; i < nodes.Count; i++)
            {
                sb.Append(i == 0 ? NewLine : "," + NewLine);
                AppendIndent(sb, depth + 1);
                AppendNode(sb, depth + 1, nodes[i]);
            }
            sb.Append(NewLine);
            AppendIndent(sb, depth);
            sb.Append(']');
        }

        private static void AppendNode(StringBuilder sb, int depth, NodeReport node)
        {
            sb.Append('{');
            AppendProperty(sb, depth + 1, "nodeId", first: true);
            AppendNumber(sb, node.NodeId);
            AppendProperty(sb, depth + 1, "parentId");
            AppendNumber(sb, node.ParentId);
            AppendProperty(sb, depth + 1, "labId");
            AppendString(sb, node.LabId);
            AppendProperty(sb, depth + 1, "depth");
            AppendNumber(sb, node.Depth);
            AppendProperty(sb, depth + 1, "status");
            AppendString(sb, node.Status);
            AppendProperty(sb, depth + 1, "model");
            AppendString(sb, node.Model);
            AppendProperty(sb, depth + 1, "history");
            AppendString(sb, node.History);
            AppendProperty(sb, depth + 1, "continuable");
            sb.Append(node.Continuable ? "true" : "false");
            AppendProperty(sb, depth + 1, "error");
            AppendException(sb, depth + 1, node.Error);
            AppendProperty(sb, depth + 1, "reportError");
            AppendException(sb, depth + 1, node.ReportError);
            sb.Append(NewLine);
            AppendIndent(sb, depth);
            sb.Append('}');
        }

        private static void AppendException(StringBuilder sb, int depth, ReportException exception)
        {
            if (exception == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('{');
            AppendProperty(sb, depth + 1, "type", first: true);
            AppendString(sb, exception.Type);
            AppendProperty(sb, depth + 1, "message");
            AppendString(sb, exception.Message);
            AppendProperty(sb, depth + 1, "details");
            AppendString(sb, exception.Details);
            sb.Append(NewLine);
            AppendIndent(sb, depth);
            sb.Append('}');
        }

        private static void AppendProperty(StringBuilder sb, int depth, string name, bool first = false)
        {
            sb.Append(first ? NewLine : "," + NewLine);
            AppendIndent(sb, depth);
            AppendString(sb, name);
            sb.Append(": ");
        }

        private static void AppendIndent(StringBuilder sb, int depth)
        {
            for (int i = 0; i < depth; i++)
                sb.Append(Indent);
        }

        private static void AppendNumber(StringBuilder sb, int? value)
        {
            if (value.HasValue)
                sb.Append(value.Value.ToString(CultureInfo.InvariantCulture));
            else
                sb.Append("null");
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        // Lone surrogates have no UTF-8 encoding; control and JavaScript line
                        // separators are escaped so the text stays valid wherever it is embedded.
                        if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                            sb.Append(c).Append(value[++i]);
                        else if (c < ' ' || char.IsSurrogate(c) || c == (char)0x2028 || c == (char)0x2029)
                            sb.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
