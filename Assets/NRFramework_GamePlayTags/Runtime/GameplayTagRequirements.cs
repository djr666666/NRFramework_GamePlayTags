using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 一次施放要满足的条件。必须有的标签都要有，不能有的标签一个都不能有。
    /// 不能有「控制」时，冰冻和沉默都会拦住。
    /// </summary>
    public sealed class GameplayTagRequirements
    {
        readonly List<GameplayTag> _required = new List<GameplayTag>();
        readonly List<GameplayTag> _forbidden = new List<GameplayTag>();

        public void Require(GameplayTag tag)
        {
            Add(_required, tag);
        }

        public void Forbid(GameplayTag tag)
        {
            Add(_forbidden, tag);
        }

        public bool IsMet(IGameplayTagSet tags)
        {
            string blockedBy;
            return IsMet(tags, out blockedBy);
        }

        public bool IsMet(IGameplayTagSet tags, out string blockedBy)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));

            for (int i = 0; i < _required.Count; i++)
            {
                if (tags.HasTag(_required[i]))
                    continue;
                blockedBy = "缺少 " + _required[i].Name;
                return false;
            }

            for (int i = 0; i < _forbidden.Count; i++)
            {
                if (!tags.HasTag(_forbidden[i]))
                    continue;
                blockedBy = "不能有 " + _forbidden[i].Name;
                return false;
            }

            blockedBy = null;
            return true;
        }

        void Add(List<GameplayTag> into, GameplayTag tag)
        {
            if (tag.IsNone)
                throw new ArgumentException("空标签不能写进施放条件。");

            List<GameplayTag> other = into == _required ? _forbidden : _required;
            if (Contains(other, tag))
                throw new InvalidOperationException("同一个标签不能又必须有又不能有：" + tag.Name);

            GameplayTag known = FirstTag();
            if (!known.IsNone && known.TreeId != tag.TreeId)
                throw new InvalidOperationException("施放条件里的标签必须来自同一张登记表。");

            if (Contains(into, tag))
                return;
            into.Add(tag);
        }

        GameplayTag FirstTag()
        {
            if (_required.Count > 0)
                return _required[0];
            if (_forbidden.Count > 0)
                return _forbidden[0];
            return GameplayTag.None;
        }

        static bool Contains(List<GameplayTag> tags, GameplayTag tag)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Equals(tag))
                    return true;
            }
            return false;
        }
    }
}
