using System;
using UnityEngine;

namespace NRFramework.GameplayTags
{
    /// <summary>
    /// 播放后按 Q，给玩家挂上或去掉冰冻。正式项目里请直接点 Tags.State.Control.Frozen。
    /// 这个演示放在包里，所以按名字取，避免包去引用项目自己导出的代码。
    /// </summary>
    public sealed class GameplayTagPlayDemo : MonoBehaviour
    {
        [SerializeField] GameplayTagCatalog _catalog;

        readonly GameplayTagContainer _player = new GameplayTagContainer();
        readonly GameplayTagContainer _boss = new GameplayTagContainer();
        readonly GameplayTagCountContainer _playerBurns = new GameplayTagCountContainer();
        readonly GameplayTagRequirements _cast = new GameplayTagRequirements();
        readonly GameplayTagCountContainer _hero = new GameplayTagCountContainer();
        readonly GameplayTagCountContainer _sword = new GameplayTagCountContainer();
        readonly GameplayTagCountContainer _aura = new GameplayTagCountContainer();
        GameplayTag _frozen;
        GameplayTag _burning;
        GameplayTag _control;
        GameplayTag _silenced;
        bool _swordWorn = true;
        string _screen = "还没开始。";
        bool _ready;
        GUIStyle _style;

        void Start()
        {
            try
            {
                _frozen = GameplayTagLibrary.Get("State.Control.Frozen");
                _burning = GameplayTagLibrary.Get("State.Burning");
                _control = GameplayTagLibrary.Get("State.Control");
                _silenced = GameplayTagLibrary.Get("State.Control.Silenced");
                if (_catalog != null)
                    GameplayTagLibrary.Use(_catalog);
                _boss.AddTag(_burning);
                _cast.Forbid(_control);
                _sword.SetParent(_hero);
                _aura.SetParent(_hero);
                _sword.AddTag(_frozen);
                _sword.AddTag(_burning);
                _aura.AddTag(_silenced);
                _aura.AddTag(_burning);
                _ready = true;
                Refresh();
            }
            catch (Exception exception)
            {
                _screen = exception.Message;
            }
        }

        void Update()
        {
            if (!_ready)
                return;

            bool changed = false;
            if (Input.GetKeyDown(KeyCode.Q))
            {
                bool had = _player.HasTagExact(_frozen);
                if (had)
                    _player.RemoveTag(_frozen);
                else
                    _player.AddTag(_frozen);
                Debug.Log((had ? "去掉 " : "加上 ") + _frozen.Name);
                changed = true;
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                int count = _playerBurns.AddTag(_burning);
                Debug.Log("加上 " + _burning.Name + "，份数 " + count);
                changed = true;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                int count = _playerBurns.RemoveTag(_burning);
                Debug.Log("去掉一份 " + _burning.Name + "，份数 " + count);
                changed = true;
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                _swordWorn = !_swordWorn;
                _sword.SetParent(_swordWorn ? _hero : null);
                Debug.Log((_swordWorn ? "戴上剑，" : "卸下剑，") +
                    "角色燃烧份数 " + _hero.GetCount(_burning));
                changed = true;
            }
            if (changed)
                Refresh();
        }

        void Refresh()
        {
            _screen =
                "按 Q：挂上或去掉 " + _frozen.Name + "\n" +
                "按 E：给 " + _burning.Name + " 加 1 份    按 R：减 1 份\n\n" +
                "玩家亲手挂着：" + NamesOf(_player) + "\n" +
                "问有没有 " + _control.Name + "：" + YesNo(_player.HasTag(_control)) + "\n" +
                "问是不是亲手加了它：" + YesNo(_player.HasTagExact(_control)) + "\n\n" +
                _burning.Name + " 份数：" + _playerBurns.GetCount(_burning) + "\n" +
                "问有没有它：" + YesNo(_playerBurns.HasTag(_burning)) + "\n\n" +
                "Boss 亲手挂着：" + NamesOf(_boss) + "\n" +
                "问有没有 " + _control.Name + "：" + YesNo(_boss.HasTag(_control)) + "\n\n" +
                "放技能的条件：不能有 " + _control.Name + "\n" +
                "这次放技能：" + CastResult() + "\n\n" +
                "按 T：卸下或戴上剑\n" +
                "剑：" + (_swordWorn ? "戴着" : "已卸下") + "\n" +
                "角色冰冻：" + YesNo(_hero.HasTag(_frozen)) + "\n" +
                "角色沉默：" + YesNo(_hero.HasTag(_silenced)) + "\n" +
                "角色问控制：" + YesNo(_hero.HasTag(_control)) + "\n" +
                "角色燃烧份数：" + _hero.GetCount(_burning);
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box);
                _style.fontSize = 18;
                _style.alignment = TextAnchor.UpperLeft;
                _style.normal.textColor = Color.white;
                _style.wordWrap = true;
                _style.padding = new RectOffset(12, 12, 12, 12);
            }

            GUI.Box(new Rect(16f, 16f, 560f, 520f), _screen, _style);
        }

        static string NamesOf(GameplayTagContainer container)
        {
            var names = new System.Collections.Generic.List<GameplayTag>();
            container.CopyExplicitTags(names);
            if (names.Count == 0)
                return "无";

            string text = names[0].Name;
            for (int i = 1; i < names.Count; i++)
                text += "、" + names[i].Name;
            return text;
        }

        static string YesNo(bool value)
        {
            return value ? "有" : "没有";
        }

        string CastResult()
        {
            string blockedBy;
            if (_cast.IsMet(_player, out blockedBy))
                return "可以放";
            return blockedBy;
        }
    }
}
