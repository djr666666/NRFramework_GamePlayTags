using System;
using System.Collections.Generic;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 按名字取出已经导出的标签。组件上存的是名字，播放时走这里。
    /// 代码里请直接点 Tags，例如 Tags.State.Control.Frozen。那是字段，不再查表。
    /// </summary>
    public static class GameplayTagLibrary
    {
        static Dictionary<string, GameplayTag> _exported;

        public static bool IsReady
        {
            get { return _exported != null; }
        }

        /// <summary>保存配置生成的代码在加载时把整张表交过来。业务代码不用自己调。</summary>
        public static void Install(Dictionary<string, GameplayTag> tags)
        {
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            _exported = tags;
        }

        /// <summary>
        /// 开局可以对一下资源。资源里的名字必须已经点过保存配置，否则这里会说明哪一条还没导出。
        /// </summary>
        public static void Use(GameplayTagCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (_exported == null)
                throw new InvalidOperationException("还没导出代码。在标签图里点保存配置。");

            IReadOnlyList<string> names = catalog.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
            {
                GameplayTag tag;
                if (TryGet(names[i], out tag))
                    continue;
                throw new InvalidOperationException("「" + names[i] + "」还没导出。在标签图里点保存配置。");
            }
        }

        public static bool TryGet(string tagName, out GameplayTag tag)
        {
            if (_exported == null || string.IsNullOrEmpty(tagName))
            {
                tag = GameplayTag.None;
                return false;
            }
            return _exported.TryGetValue(tagName, out tag);
        }

        public static GameplayTag Get(string tagName)
        {
            GameplayTag tag;
            if (!TryGet(tagName, out tag) || tag.IsNone)
                throw new KeyNotFoundException("没有这个标签：" + tagName + "。在标签图里登记后点保存配置。");
            return tag;
        }
    }
}
