using System.Collections.Generic;
using System.Text;

namespace NRFramework.GameplayTags
{
    static class GameplayTagScriptText
    {
        public static string Build(GameplayTagNameTable table)
        {
            GameplayTagRegistry registry = table.CreateRegistry();
            var tags = new List<GameplayTag>();
            registry.CopyTags(tags);
            Node root = BuildTree(tags, table);
            var mapLines = new List<KeyValuePair<string, string>>();

            var builder = new StringBuilder();
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine("namespace NRFramework.GameplayTags");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// 保存配置导出的标签。叶子写成 Tags.State.Control.Frozen。");
            builder.AppendLine("    /// 下面还有子标签的，自身写成 Tags.State.Control.Tag。");
            builder.AppendLine("    /// 字段在导出时已经带好编号，调用时不再查表。");
            builder.AppendLine("    /// 这个文件在用包人自己的 Assets/GameplayTags 里。再点保存配置会重写。");
            builder.AppendLine("    /// 自己加的方法写到同目录的 Tags.User.cs。");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static partial class Tags");
            builder.AppendLine("    {");
            var rootTaken = new HashSet<string> { "TryGet", "CreateMap", "Map", "InstallRuntime", "InstallEditor" };
            for (int i = 0; i < root.Children.Count; i++)
                WriteNode(builder, root.Children[i], 2, null, rootTaken, mapLines);

            builder.AppendLine("        /// <summary>按完整名字取出导出的标签。组件上存的名字走这里。</summary>");
            builder.AppendLine("        public static bool TryGet(string tagName, out GameplayTag tag)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (string.IsNullOrEmpty(tagName))");
            builder.AppendLine("            {");
            builder.AppendLine("                tag = GameplayTag.None;");
            builder.AppendLine("                return false;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            Dictionary<string, GameplayTag> map = Map;");
            builder.AppendLine("            if (map == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                map = CreateMap();");
            builder.AppendLine("                Map = map;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return map.TryGetValue(tagName, out tag);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        static Dictionary<string, GameplayTag> Map;");
            builder.AppendLine();
            builder.AppendLine("        static Dictionary<string, GameplayTag> CreateMap()");
            builder.AppendLine("        {");
            builder.AppendLine("            var map = new Dictionary<string, GameplayTag>(System.StringComparer.Ordinal);");
            for (int i = 0; i < mapLines.Count; i++)
            {
                builder.Append("            map.Add(\"");
                builder.Append(EscapeString(mapLines[i].Key));
                builder.Append("\", ");
                builder.Append(mapLines[i].Value);
                builder.AppendLine(");");
            }
            builder.AppendLine("            return map;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]");
            builder.AppendLine("        static void InstallRuntime()");
            builder.AppendLine("        {");
            builder.AppendLine("            GameplayTagLibrary.Install(CreateMap());");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("#if UNITY_EDITOR");
            builder.AppendLine("        [UnityEditor.InitializeOnLoadMethod]");
            builder.AppendLine("        static void InstallEditor()");
            builder.AppendLine("        {");
            builder.AppendLine("            GameplayTagLibrary.Install(CreateMap());");
            builder.AppendLine("        }");
            builder.AppendLine("#endif");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        static Node BuildTree(List<GameplayTag> tags, GameplayTagNameTable table)
        {
            var root = new Node();
            for (int i = 0; i < tags.Count; i++)
            {
                string[] parts = tags[i].Name.Split('.');
                Node current = root;
                string path = null;
                for (int p = 0; p < parts.Length; p++)
                {
                    path = path == null ? parts[p] : path + "." + parts[p];
                    Node child = current.Find(parts[p]);
                    if (child == null)
                    {
                        child = new Node { Segment = parts[p], FullName = path };
                        current.Children.Add(child);
                    }
                    current = child;
                }

                current.Index = tags[i].RuntimeIndex;
                current.SubtreeEnd = tags[i].SubtreeEnd;
                current.Note = table.GetNote(tags[i].Name);
            }
            return root;
        }

        static void WriteNode(StringBuilder builder, Node node, int depth, string parentAccess, HashSet<string> taken, List<KeyValuePair<string, string>> mapLines)
        {
            string indent = new string(' ', depth * 4);
            string inner = new string(' ', (depth + 1) * 4);
            string member = MemberName(node.Segment, taken);
            string access = parentAccess == null ? member : parentAccess + "." + member;
            if (node.Children.Count == 0)
            {
                WriteDoc(builder, indent, node.Note);
                builder.Append(indent);
                builder.Append("public static readonly GameplayTag ");
                builder.Append(member);
                builder.Append(" = GameplayTag.Baked(\"");
                builder.Append(EscapeString(node.FullName));
                builder.Append("\", ");
                builder.Append(node.Index);
                builder.Append(", ");
                builder.Append(node.SubtreeEnd);
                builder.AppendLine(");");
                builder.AppendLine();
                mapLines.Add(new KeyValuePair<string, string>(node.FullName, access));
                return;
            }

            builder.Append(indent);
            builder.Append("public static class ");
            builder.AppendLine(member);
            builder.Append(indent);
            builder.AppendLine("{");
            WriteDoc(builder, inner, node.Note);
            builder.Append(inner);
            builder.Append("public static readonly GameplayTag Tag = GameplayTag.Baked(\"");
            builder.Append(EscapeString(node.FullName));
            builder.Append("\", ");
            builder.Append(node.Index);
            builder.Append(", ");
            builder.Append(node.SubtreeEnd);
            builder.AppendLine(");");
            builder.AppendLine();
            mapLines.Add(new KeyValuePair<string, string>(node.FullName, access + ".Tag"));

            var childTaken = new HashSet<string> { "Tag" };
            for (int i = 0; i < node.Children.Count; i++)
                WriteNode(builder, node.Children[i], depth + 1, access, childTaken, mapLines);

            builder.Append(indent);
            builder.AppendLine("}");
            builder.AppendLine();
        }

        static void WriteDoc(StringBuilder builder, string indent, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return;

            string[] lines = note.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            builder.Append(indent);
            builder.AppendLine("/// <summary>");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                    continue;
                builder.Append(indent);
                builder.Append("/// ");
                builder.AppendLine(EscapeDoc(line));
            }
            builder.Append(indent);
            builder.AppendLine("/// </summary>");
        }

        static string MemberName(string segment, HashSet<string> taken)
        {
            string name = Identifier(segment);
            string plain = name.StartsWith("@") ? name.Substring(1) : name;
            if (taken != null && taken.Contains(plain))
                return plain + "_";
            return name;
        }

        static string Identifier(string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return "_";
            if (IsKeyword(segment))
                return "@" + segment;
            return segment;
        }

        static bool IsKeyword(string segment)
        {
            switch (segment)
            {
                case "abstract":
                case "as":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "false":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "internal":
                case "is":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "null":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "ref":
                case "return":
                case "sbyte":
                case "sealed":
                case "short":
                case "sizeof":
                case "stackalloc":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "true":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unchecked":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "void":
                case "volatile":
                case "while":
                    return true;
                default:
                    return false;
            }
        }

        static string EscapeString(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        static string EscapeDoc(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("*/", "* /");
        }

        sealed class Node
        {
            public string Segment;
            public string FullName;
            public string Note;
            public int Index;
            public int SubtreeEnd;
            public readonly List<Node> Children = new List<Node>();

            public Node Find(string segment)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Segment == segment)
                        return Children[i];
                }
                return null;
            }
        }
    }
}
