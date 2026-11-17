using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 一个人身上现在挂着哪些标签。玩家一个，Boss 一个，问的是同一张登记表。
    /// 这里只记有没有。同一标签要算几份，用 GameplayTagCountContainer。
    /// </summary>
    public sealed class GameplayTagContainer : IGameplayTagSet
    {
        readonly List<GameplayTag> _explicit = new List<GameplayTag>();

        public int Count
        {
            get { return _explicit.Count; }
        }

        public bool AddTag(GameplayTag tag)
        {
            if (tag.IsNone)
                return false;
            if (HasTagExact(tag))
                return false;
            if (_explicit.Count > 0 && _explicit[0].TreeId != tag.TreeId)
                throw new InvalidOperationException("这个容器里的标签必须来自同一张登记表。");

            _explicit.Add(tag);
            return true;
        }

        /// <summary>
        /// 只去掉亲手加过的那一个。没加过的父标签去掉时，子标签还在。
        /// </summary>
        public bool RemoveTag(GameplayTag tag)
        {
            for (int i = 0; i < _explicit.Count; i++)
            {
                if (!_explicit[i].Equals(tag))
                    continue;
                _explicit.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool HasTagExact(GameplayTag tag)
        {
            if (tag.IsNone)
                return false;
            for (int i = 0; i < _explicit.Count; i++)
            {
                if (_explicit[i].Equals(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 身上有这个标签，或者有它下面的标签。有冰冻时，问控制也算有。
        /// 有控制时，问冰冻不算有。
        /// </summary>
        public bool HasTag(GameplayTag tag)
        {
            if (tag.IsNone)
                return false;
            for (int i = 0; i < _explicit.Count; i++)
            {
                if (tag.IsSameOrParentOf(_explicit[i]))
                    return true;
            }
            return false;
        }

        public void Clear()
        {
            _explicit.Clear();
        }

        public void CopyExplicitTags(List<GameplayTag> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < _explicit.Count; i++)
                destination.Add(_explicit[i]);
        }
    }
}
