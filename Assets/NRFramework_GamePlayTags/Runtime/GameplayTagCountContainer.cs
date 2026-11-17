using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 同一个标签可以挂好几份。减到 0 才算没有。
    /// 问父标签时，只看下面还有没有份数，父标签自己的份数不会把子标签算进去。
    /// SetParent 之后，这个容器里的标签会算进角色身上；卸下时只收回自己的份数。
    /// </summary>
    public sealed class GameplayTagCountContainer : IGameplayTagSet
    {
        readonly List<GameplayTag> _tags = new List<GameplayTag>();
        readonly List<int> _counts = new List<int>();
        GameplayTagCountContainer _parent;

        public int TagCount
        {
            get { return _tags.Count; }
        }

        public void SetParent(GameplayTagCountContainer parent)
        {
            if (_parent == parent)
                return;
            if (parent != null && Reaches(parent, this))
                throw new InvalidOperationException("不能把容器挂到自己下面。");

            if (_parent != null)
                CopyCountsTo(_parent, false);
            _parent = parent;
            if (_parent != null)
                CopyCountsTo(_parent, true);
        }

        public int AddTag(GameplayTag tag)
        {
            int count = AddLocal(tag);
            if (count == 0)
                return 0;
            if (_parent != null)
                _parent.AddTag(tag);
            return count;
        }

        public int RemoveTag(GameplayTag tag)
        {
            int index = Find(tag);
            if (index < 0)
                return 0;

            int next = DecreaseAt(index);
            if (_parent != null)
                _parent.RemoveTag(tag);
            return next;
        }

        public int GetCount(GameplayTag tag)
        {
            int index = Find(tag);
            if (index < 0)
                return 0;
            return _counts[index];
        }

        public bool HasTagExact(GameplayTag tag)
        {
            return GetCount(tag) > 0;
        }

        public bool HasTag(GameplayTag tag)
        {
            if (tag.IsNone)
                return false;
            for (int i = 0; i < _tags.Count; i++)
            {
                if (tag.IsSameOrParentOf(_tags[i]))
                    return true;
            }
            return false;
        }

        int Find(GameplayTag tag)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Equals(tag))
                    return i;
            }
            return -1;
        }

        int AddLocal(GameplayTag tag)
        {
            if (tag.IsNone)
                return 0;

            int index = Find(tag);
            if (index >= 0)
            {
                _counts[index] = _counts[index] + 1;
                return _counts[index];
            }

            if (_tags.Count > 0 && _tags[0].TreeId != tag.TreeId)
                throw new InvalidOperationException("这个容器里的标签必须来自同一张登记表。");

            _tags.Add(tag);
            _counts.Add(1);
            return 1;
        }

        int DecreaseAt(int index)
        {
            int next = _counts[index] - 1;
            if (next <= 0)
            {
                _tags.RemoveAt(index);
                _counts.RemoveAt(index);
                return 0;
            }

            _counts[index] = next;
            return next;
        }

        void CopyCountsTo(GameplayTagCountContainer target, bool add)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                int count = _counts[i];
                for (int n = 0; n < count; n++)
                {
                    if (add)
                        target.AddTag(_tags[i]);
                    else
                        target.RemoveTag(_tags[i]);
                }
            }
        }

        static bool Reaches(GameplayTagCountContainer start, GameplayTagCountContainer self)
        {
            GameplayTagCountContainer cursor = start;
            while (cursor != null)
            {
                if (cursor == self)
                    return true;
                cursor = cursor._parent;
            }
            return false;
        }
    }
}
