using System;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    /// <summary>
        /// 组件上点选的一个标签。资源里只存名字，播放时按导出的接口换成编号。
    /// </summary>
    [Serializable]
    public struct GameplayTagLink
    {
        [SerializeField] string _name;

        public GameplayTagLink(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name ?? string.Empty; }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(_name); }
        }

        public GameplayTag Resolve()
        {
            return GameplayTagLibrary.Get(_name);
        }
    }
}
