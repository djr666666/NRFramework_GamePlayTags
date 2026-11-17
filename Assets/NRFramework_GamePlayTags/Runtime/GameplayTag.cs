using System;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 一个标签的运行时身份。
    /// 存档和资源里只保存 <see cref="Name"/>。
    /// <see cref="RuntimeIndex"/> 每次建表都会重排，不能写进存档。
    /// </summary>
    public readonly struct GameplayTag : IEquatable<GameplayTag>
    {
        public string Name { get; }
        public int RuntimeIndex { get; }

        /// <summary>这棵子树的结束编号，不含自身以外的下一个标签。编号 0 的 None 没有子树。</summary>
        internal int SubtreeEnd { get; }

        /// <summary>发出这张标签的那次 Build。不同登记表的编号不能拿来互比。</summary>
        internal int TreeId { get; }

        public bool IsNone
        {
            get { return RuntimeIndex == 0; }
        }

        public static GameplayTag None
        {
            get { return default(GameplayTag); }
        }

        /// <summary>导出的 Tags 固定用这个树号。现场建的登记表从 2 起，不会和它对上。</summary>
        internal const int LibraryTreeId = 1;

        internal GameplayTag(string name, int runtimeIndex, int subtreeEnd, int treeId)
        {
            Name = name ?? string.Empty;
            RuntimeIndex = runtimeIndex;
            SubtreeEnd = subtreeEnd;
            TreeId = treeId;
        }

        /// <summary>保存配置写进代码的标签。编号在导出时定好，调用时不再查表。</summary>
        public static GameplayTag Baked(string name, int runtimeIndex, int subtreeEnd)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("标签名是空的。", nameof(name));
            if (runtimeIndex <= 0 || subtreeEnd <= runtimeIndex)
                throw new ArgumentOutOfRangeException(nameof(runtimeIndex));
            return new GameplayTag(name, runtimeIndex, subtreeEnd, LibraryTreeId);
        }

        /// <summary>
        /// 自己是不是 other 的严格祖先。自己不算自己的父标签。
        /// </summary>
        public bool IsParentOf(GameplayTag other)
        {
            if (IsNone || TreeId == 0 || TreeId != other.TreeId)
                return false;
            return other.RuntimeIndex > RuntimeIndex && other.RuntimeIndex < SubtreeEnd;
        }

        /// <summary>
        /// 自己就是 other，或者自己是 other 的祖先。后面的 HasTag 用这一条。
        /// </summary>
        public bool IsSameOrParentOf(GameplayTag other)
        {
            if (TreeId != other.TreeId)
                return false;
            if (RuntimeIndex == other.RuntimeIndex)
                return true;
            return IsParentOf(other);
        }

        public bool Equals(GameplayTag other)
        {
            return TreeId == other.TreeId && RuntimeIndex == other.RuntimeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TreeId * 397) ^ RuntimeIndex;
            }
        }

        public override string ToString()
        {
            return IsNone ? "None" : Name;
        }

        public static bool operator ==(GameplayTag left, GameplayTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayTag left, GameplayTag right)
        {
            return !left.Equals(right);
        }
    }
}
