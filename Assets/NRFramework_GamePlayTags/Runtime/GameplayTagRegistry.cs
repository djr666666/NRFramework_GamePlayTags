using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 第一步：把点号名字收成一棵树，并给出父子判断。
    /// 只登记最末尾的名字，中间的父节点会自动补上。
    /// 建表之后按深度优先、同一层按名字排序，摊成连续编号。
    /// 父标签的全部子孙占一段连续编号，所以父子判断不用再拆字符串。
    /// </summary>
    public sealed class GameplayTagRegistry
    {
        static int s_NextTreeId = 2;

        readonly Dictionary<string, MutableNode> _open = new Dictionary<string, MutableNode>(StringComparer.Ordinal);
        Dictionary<string, GameplayTag> _byName = new Dictionary<string, GameplayTag>(StringComparer.Ordinal);
        GameplayTag[] _byIndex = Array.Empty<GameplayTag>();
        int _treeId;
        bool _built;

        public bool IsBuilt
        {
            get { return _built; }
        }

        /// <summary>不含编号 0 的 None。</summary>
        public int Count
        {
            get { return _built ? _byIndex.Length - 1 : _open.Count; }
        }

        public void Register(string tagName)
        {
            if (_built)
                throw new InvalidOperationException("登记表已经建好，不能再加标签。");

            string[] parts = SplitAndValidate(tagName);
            string path = null;
            MutableNode parent = null;
            for (int i = 0; i < parts.Length; i++)
            {
                path = path == null ? parts[i] : path + "." + parts[i];
                MutableNode node;
                if (!_open.TryGetValue(path, out node))
                {
                    node = new MutableNode(path, parts[i], parent);
                    _open.Add(path, node);
                    if (parent != null)
                        parent.Children.Add(node);
                }
                parent = node;
            }
        }

        public void Build()
        {
            if (_built)
                throw new InvalidOperationException("登记表已经建好。");

            var roots = new List<MutableNode>();
            foreach (MutableNode node in _open.Values)
            {
                if (node.Parent == null)
                    roots.Add(node);
            }
            roots.Sort(CompareSegment);

            _treeId = s_NextTreeId++;
            var built = new List<GameplayTag> { GameplayTag.None };
            for (int i = 0; i < roots.Count; i++)
                WriteDepthFirst(roots[i], built);

            _byIndex = built.ToArray();
            _byName = new Dictionary<string, GameplayTag>(_byIndex.Length, StringComparer.Ordinal);
            for (int i = 1; i < _byIndex.Length; i++)
                _byName.Add(_byIndex[i].Name, _byIndex[i]);

            _open.Clear();
            _built = true;
        }

        public GameplayTag Get(string tagName)
        {
            GameplayTag tag;
            if (!TryGet(tagName, out tag))
                throw new KeyNotFoundException("没有这个标签：" + tagName);
            return tag;
        }

        public bool TryGet(string tagName, out GameplayTag tag)
        {
            if (!_built)
                throw new InvalidOperationException("要先调用 Build，登记表还没建好。");
            if (string.IsNullOrEmpty(tagName))
            {
                tag = GameplayTag.None;
                return false;
            }
            return _byName.TryGetValue(tagName, out tag);
        }

        public bool Contains(string tagName)
        {
            GameplayTag tag;
            return TryGet(tagName, out tag);
        }

        public bool IsParentOf(GameplayTag parent, GameplayTag child)
        {
            return parent.IsParentOf(child);
        }

        /// <summary>
        /// 按深度优先的编号顺序抄出全部标签，不含 None。
        /// </summary>
        public void CopyTags(List<GameplayTag> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!_built)
                throw new InvalidOperationException("要先调用 Build，登记表还没建好。");

            destination.Clear();
            for (int i = 1; i < _byIndex.Length; i++)
                destination.Add(_byIndex[i]);
        }

        void WriteDepthFirst(MutableNode node, List<GameplayTag> built)
        {
            int index = built.Count;
            built.Add(default(GameplayTag));
            node.Children.Sort(CompareSegment);
            for (int i = 0; i < node.Children.Count; i++)
                WriteDepthFirst(node.Children[i], built);

            int subtreeEnd = built.Count;
            built[index] = new GameplayTag(node.FullName, index, subtreeEnd, _treeId);
        }

        static int CompareSegment(MutableNode left, MutableNode right)
        {
            return string.CompareOrdinal(left.Segment, right.Segment);
        }

        static string[] SplitAndValidate(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
                throw new ArgumentException("标签名是空的。", nameof(tagName));

            string[] parts = tagName.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                    throw new ArgumentException("标签名不能有空段：" + tagName, nameof(tagName));
                if (!IsNameStart(part[0]))
                    throw new ArgumentException("标签段要以字母或下划线开头：" + part, nameof(tagName));
                for (int c = 1; c < part.Length; c++)
                {
                    char ch = part[c];
                    if (!IsNameStart(ch) && !char.IsDigit(ch))
                        throw new ArgumentException("标签段只能用字母、数字和下划线：" + part, nameof(tagName));
                }
            }
            return parts;
        }

        static bool IsNameStart(char ch)
        {
            return char.IsLetter(ch) || ch == '_';
        }

        sealed class MutableNode
        {
            public readonly string FullName;
            public readonly string Segment;
            public readonly MutableNode Parent;
            public readonly List<MutableNode> Children = new List<MutableNode>();

            public MutableNode(string fullName, string segment, MutableNode parent)
            {
                FullName = fullName;
                Segment = segment;
                Parent = parent;
            }
        }
    }
}
