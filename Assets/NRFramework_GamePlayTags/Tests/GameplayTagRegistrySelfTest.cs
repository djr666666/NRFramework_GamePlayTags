using System;

namespace NRFramework.GameplayTags
{
    public static class GameplayTagRegistrySelfTest
    {
        public static void Run()
        {
            ImplicitParentsExist();
            IntervalsMatchDepthFirstOrder();
            ParentChecks();
            AddingARootShiftsIndexesButKeepsParenthood();
            TagsFromAnotherTreeDoNotMatch();
            RegisteringTwiceDoesNotDuplicate();
            BadNamesAreRejected();
            EmptyRegistryBuilds();
            NameTableAddsParentsAndRejectsDuplicates();
            NameTableRemoveAndRenameRewriteChildren();
            NotesFollowTheTagWhenRenamedOrDeleted();
            PlayerAndBossKeepTheirOwnTags();
            TwoBurnsStayUntilBothAreRemoved();
            ControlBlocksTheCast();
            UnequipTakesOnlyThatSource();
        }

        static void ImplicitParentsExist()
        {
            GameplayTagRegistry registry = Build("State.Control.Frozen");
            Check(registry.Count == 3, "只登记冰冻，应该连父节点一共 3 个。实际 " + registry.Count);
            Check(registry.Contains("State"), "缺少自动补上的 State。");
            Check(registry.Contains("State.Control"), "缺少自动补上的 State.Control。");
            Check(registry.Contains("State.Control.Frozen"), "缺少登记的 State.Control.Frozen。");
            Check(!registry.Contains("State.Burning"), "没有登记的标签不应该出现。");
        }

        static void IntervalsMatchDepthFirstOrder()
        {
            GameplayTagRegistry registry = Build(
                "State.Control.Frozen",
                "State.Control.Silenced",
                "State.Burning");

            GameplayTag state = registry.Get("State");
            GameplayTag burning = registry.Get("State.Burning");
            GameplayTag control = registry.Get("State.Control");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag silenced = registry.Get("State.Control.Silenced");

            CheckIndex(state, 1, 6);
            CheckIndex(burning, 2, 3);
            CheckIndex(control, 3, 6);
            CheckIndex(frozen, 4, 5);
            CheckIndex(silenced, 5, 6);
        }

        static void ParentChecks()
        {
            GameplayTagRegistry registry = Build(
                "State.Control.Frozen",
                "State.Control.Silenced",
                "State.Burning");

            GameplayTag state = registry.Get("State");
            GameplayTag burning = registry.Get("State.Burning");
            GameplayTag control = registry.Get("State.Control");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag silenced = registry.Get("State.Control.Silenced");

            Check(control.IsParentOf(frozen), "控制应该是冰冻的父标签。");
            Check(control.IsParentOf(silenced), "控制应该是沉默的父标签。");
            Check(state.IsParentOf(frozen), "State 应该是冰冻的祖先。");
            Check(!control.IsParentOf(burning), "燃烧不在控制下面。");
            Check(!burning.IsParentOf(frozen), "燃烧不是冰冻的父标签。");
            Check(!frozen.IsParentOf(control), "子标签不是父标签的祖先。");
            Check(!control.IsParentOf(control), "标签不是自己的父标签。");
            Check(control.IsSameOrParentOf(control), "问自己应该算同一标签。");
            Check(control.IsSameOrParentOf(frozen), "父标签问子标签应该算有。");
            Check(!GameplayTag.None.IsParentOf(frozen), "空标签不是任何标签的父标签。");
        }

        static void AddingARootShiftsIndexesButKeepsParenthood()
        {
            GameplayTagRegistry small = Build("State.Control.Frozen", "State.Burning");
            GameplayTagRegistry grown = Build("State.Control.Frozen", "State.Burning", "Ability.Cast");

            Check(small.Get("State").RuntimeIndex != grown.Get("State").RuntimeIndex,
                "新加一棵根之后编号会重排，所以编号不能写进存档。");
            Check(grown.Get("Ability").RuntimeIndex == 1, "名字排序后 Ability 应该排在 State 前面。");
            Check(grown.Get("State.Control").IsParentOf(grown.Get("State.Control.Frozen")),
                "编号重排后，控制仍然是冰冻的父标签。");
            Check(!grown.Get("State.Control").IsParentOf(grown.Get("State.Burning")),
                "编号重排后，燃烧仍然不在控制下面。");
        }

        static void TagsFromAnotherTreeDoNotMatch()
        {
            GameplayTagRegistry left = Build("State.Control.Frozen");
            GameplayTagRegistry right = Build("State.Control.Frozen");
            GameplayTag frozenLeft = left.Get("State.Control.Frozen");
            GameplayTag controlRight = right.Get("State.Control");

            Check(!controlRight.IsParentOf(frozenLeft), "两张登记表的标签不能互相当父子。");
            Check(frozenLeft != right.Get("State.Control.Frozen"), "同名标签出自不同登记表，不算同一个。");
        }

        static void RegisteringTwiceDoesNotDuplicate()
        {
            var registry = new GameplayTagRegistry();
            registry.Register("State.Control.Frozen");
            registry.Register("State.Control.Frozen");
            registry.Register("State");
            registry.Build();
            Check(registry.Count == 3, "重复登记不应该把同一个标签算成多个。实际 " + registry.Count);
        }

        static void BadNamesAreRejected()
        {
            Reject(null);
            Reject(string.Empty);
            Reject("State.");
            Reject(".State");
            Reject("State..Control");
            Reject("State. Control");
            Reject("1State");
        }

        static void EmptyRegistryBuilds()
        {
            var registry = new GameplayTagRegistry();
            registry.Build();
            Check(registry.Count == 0, "空登记表建好后应该一个标签都没有。");
            Check(!registry.Contains("State"), "空登记表里不应该有 State。");
        }

        static void NameTableAddsParentsAndRejectsDuplicates()
        {
            var table = new GameplayTagNameTable();
            string error;
            Check(table.TryAdd("State.Control.Frozen", out error), error);
            Check(!table.TryAdd("State.Control.Frozen", out error), "重复登记应该失败。");
            Check(!table.TryAdd("State.", out error), "空段应该失败。");

            GameplayTagRegistry registry = table.CreateRegistry();
            Check(registry.Count == 3, "名字表只存冰冻，建表后应该有 3 个标签。实际 " + registry.Count);
            var tags = new System.Collections.Generic.List<GameplayTag>();
            registry.CopyTags(tags);
            Check(tags.Count == 3, "CopyTags 应该抄出 3 个。实际 " + tags.Count);
            Check(tags[0].Name == "State", "深度优先的第一个应该是 State。");
        }

        static void NameTableRemoveAndRenameRewriteChildren()
        {
            var table = new GameplayTagNameTable();
            string error;
            table.TryAdd("State.Control.Frozen", out error);
            table.TryAdd("State.Control.Silenced", out error);
            table.TryAdd("State.Burning", out error);

            Check(table.TryRename("State.Control", "State.Lock", out error), error);
            Check(HasName(table, "State.Lock.Frozen"), "重命名父节点后，冰冻应该跟着改。");
            Check(!HasName(table, "State.Control.Frozen"), "旧的冰冻名字应该消失。");
            Check(HasName(table, "State.Burning"), "燃烧不在被改的那一支下面，应该留着。");

            Check(!table.TryRename("State.Lock.Frozen", "State.Burning", out error), "改成已有名字应该失败。");
            Check(table.TryRemove("State.Lock", out error), error);
            Check(!HasName(table, "State.Lock.Silenced"), "删掉父节点后，下面登记过的名字应该一起去掉。");
            Check(HasName(table, "State.Burning"), "删掉控制这一支时，燃烧应该留着。");
        }

        static void NotesFollowTheTagWhenRenamedOrDeleted()
        {
            var table = new GameplayTagNameTable();
            string error;
            table.TryAdd("State.Control.Frozen", out error);
            table.TryAdd("State.Burning", out error);
            table.TryAdd("State.Lock.Held", out error);

            Check(table.SetNote("State.Control", "控制。冰冻也算。", out error), error);
            Check(table.SetNote("State.Control.Frozen", "不能行动。", out error), error);
            Check(table.SetNote("State.Burning", "持续掉血。", out error), error);
            Check(table.SetNote("State.Lock", "锁定。", out error), error);
            Check(table.ExplicitNames.Count == 3, "给自动补上的父标签写注释，不应该把它登记成新标签。");

            Check(!table.TryRename("State.Control", "State.Lock", out error), "两边都写了不同注释时，不应该改名盖掉。");
            Check(table.GetNote("State.Control") == "控制。冰冻也算。", "改名失败后，原来的注释应该还在。");
            Check(HasName(table, "State.Control.Frozen"), "改名失败后，原来的标签应该还在。");

            Check(table.SetNote("State.Lock", "   ", out error), error);
            Check(table.GetNote("State.Lock") == string.Empty, "只剩空白的注释应该清掉。");
            Check(table.TryRename("State.Control", "State.Lock", out error), error);
            Check(table.GetNote("State.Lock") == "控制。冰冻也算。", "父标签改名后，注释应该跟着走。");
            Check(table.GetNote("State.Lock.Frozen") == "不能行动。", "子标签改名后，注释应该跟着走。");

            Check(table.TryRemove("State.Lock", out error), error);
            Check(table.GetNote("State.Lock.Frozen") == string.Empty, "删掉这一支后，下面的注释应该一起去掉。");
            Check(table.GetNote("State.Burning") == "持续掉血。", "别的分支上的注释应该留着。");
        }

        static void PlayerAndBossKeepTheirOwnTags()
        {
            GameplayTagRegistry registry = Build(
                "State.Control.Frozen",
                "State.Control.Silenced",
                "State.Burning");
            GameplayTag control = registry.Get("State.Control");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag burning = registry.Get("State.Burning");

            var player = new GameplayTagContainer();
            var boss = new GameplayTagContainer();
            Check(player.AddTag(frozen), "玩家应该能挂上冰冻。");
            Check(!player.AddTag(frozen), "同一标签再挂一次，仍然只算一份。");
            Check(boss.AddTag(burning), "Boss 应该能挂上燃烧。");

            Check(player.HasTag(frozen), "玩家身上有冰冻。");
            Check(player.HasTag(control), "有冰冻时，问控制应该算有。");
            Check(!player.HasTagExact(control), "控制本身没有亲手挂上。");
            Check(!player.HasTag(burning), "玩家身上没有燃烧。");
            Check(boss.HasTag(burning), "Boss 身上有燃烧。");
            Check(!boss.HasTag(control), "Boss 没有被控制。");
            Check(!boss.HasTag(frozen), "Boss 身上没有冰冻。");

            Check(!player.RemoveTag(control), "没亲手加过的父标签，去掉时不应该成功。");
            Check(player.HasTag(frozen), "父标签没去掉冰冻。");
            Check(player.RemoveTag(frozen), "亲手加过的冰冻应该能去掉。");
            Check(!player.HasTag(control), "冰冻去掉之后，身上就没有控制了。");

            Check(player.AddTag(control), "也可以直接挂父标签本身。");
            Check(player.HasTagExact(control), "亲手挂上的控制，精确查询应该算有。");
            Check(!player.HasTag(frozen), "只有控制时，不能当成已经冰冻。");
        }

        static void TwoBurnsStayUntilBothAreRemoved()
        {
            GameplayTagRegistry registry = Build("State.Control.Frozen", "State.Burning");
            GameplayTag burning = registry.Get("State.Burning");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag control = registry.Get("State.Control");
            var body = new GameplayTagCountContainer();

            Check(body.AddTag(burning) == 1, "火球应该让燃烧变成 1 份。");
            Check(body.AddTag(burning) == 2, "火海再加一份，燃烧应该是 2 份。");
            Check(body.RemoveTag(burning) == 1, "火球结束只减 1 份。");
            Check(body.HasTag(burning), "还剩 1 份时，人仍然在烧。");
            Check(body.GetCount(burning) == 1, "剩下的那一份应该还在。");
            Check(body.RemoveTag(burning) == 0, "最后一份去掉后，份数是 0。");
            Check(!body.HasTag(burning), "份数到 0 才算没有燃烧。");

            body.AddTag(frozen);
            Check(body.HasTag(control), "有冰冻份数时，问控制应该算有。");
            Check(body.GetCount(control) == 0, "控制自己没被加过，份数仍是 0。");
            Check(body.RemoveTag(control) == 0, "去掉没加过的控制，不应该成功。");
            Check(body.GetCount(frozen) == 1, "冰冻那一份应该还在。");
        }

        static void ControlBlocksTheCast()
        {
            GameplayTagRegistry registry = Build(
                "State.Control.Frozen",
                "State.Control.Silenced",
                "State.Burning");
            GameplayTag control = registry.Get("State.Control");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag silenced = registry.Get("State.Control.Silenced");
            GameplayTag burning = registry.Get("State.Burning");

            var cast = new GameplayTagRequirements();
            cast.Forbid(control);

            var player = new GameplayTagContainer();
            string blockedBy;
            Check(cast.IsMet(player, out blockedBy), "身上什么都没有，应该可以放。");

            player.AddTag(frozen);
            Check(!cast.IsMet(player, out blockedBy), "有冰冻时，不能有控制的技能应该被拦住。");
            Check(blockedBy == "不能有 State.Control", "拦住的原因应该写明不能有控制。实际：" + blockedBy);

            player.RemoveTag(frozen);
            player.AddTag(silenced);
            Check(!cast.IsMet(player, out blockedBy), "有沉默时，同一条不能有控制也应该拦住。");

            player.RemoveTag(silenced);
            player.AddTag(burning);
            Check(cast.IsMet(player), "只有燃烧时，不能有控制的技能应该可以放。");

            var needControl = new GameplayTagRequirements();
            needControl.Require(control);
            var chilled = new GameplayTagContainer();
            chilled.AddTag(frozen);
            Check(needControl.IsMet(chilled), "必须有控制时，身上有冰冻应该算通过。");

            var onlyParent = new GameplayTagContainer();
            onlyParent.AddTag(control);
            var needFrozen = new GameplayTagRequirements();
            needFrozen.Require(frozen);
            Check(!needFrozen.IsMet(onlyParent), "必须有冰冻时，只挂了控制本身不应该通过。");

            bool rejected = false;
            try
            {
                cast.Require(control);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Check(rejected, "同一个标签不能又必须有又不能有。");
        }

        static void UnequipTakesOnlyThatSource()
        {
            GameplayTagRegistry registry = Build(
                "State.Control.Frozen",
                "State.Control.Silenced",
                "State.Burning");
            GameplayTag frozen = registry.Get("State.Control.Frozen");
            GameplayTag silenced = registry.Get("State.Control.Silenced");
            GameplayTag burning = registry.Get("State.Burning");
            GameplayTag control = registry.Get("State.Control");

            var hero = new GameplayTagCountContainer();
            var sword = new GameplayTagCountContainer();
            var aura = new GameplayTagCountContainer();
            sword.SetParent(hero);
            aura.SetParent(hero);
            sword.AddTag(frozen);
            sword.AddTag(burning);
            aura.AddTag(silenced);
            aura.AddTag(burning);

            Check(hero.HasTag(control), "剑和光环都挂上之后，角色应该算被控制。");
            Check(hero.GetCount(burning) == 2, "剑和光环各给一份燃烧，角色身上应该是 2 份。");

            sword.SetParent(null);
            Check(!hero.HasTag(frozen), "卸下剑之后，冰冻应该跟着走。");
            Check(hero.HasTag(silenced), "光环的沉默应该还在。");
            Check(hero.HasTag(control), "还挂着沉默时，问控制应该仍然算有。");
            Check(hero.GetCount(burning) == 1, "卸下剑只减剑的那一份燃烧。");
        }

        static bool HasName(GameplayTagNameTable table, string name)
        {
            var names = table.ExplicitNames;
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                    return true;
            }
            return false;
        }

        static GameplayTagRegistry Build(params string[] names)
        {
            var registry = new GameplayTagRegistry();
            for (int i = 0; i < names.Length; i++)
                registry.Register(names[i]);
            registry.Build();
            return registry;
        }

        static void Reject(string name)
        {
            var registry = new GameplayTagRegistry();
            bool threw = false;
            try
            {
                registry.Register(name);
            }
            catch (ArgumentException)
            {
                threw = true;
            }
            Check(threw, "非法标签名应该被拒绝：" + (name ?? "<null>"));
        }

        static void CheckIndex(GameplayTag tag, int index, int subtreeEnd)
        {
            Check(tag.RuntimeIndex == index, tag.Name + " 的编号应该是 " + index + "，实际 " + tag.RuntimeIndex);
            Check(tag.SubtreeEnd == subtreeEnd, tag.Name + " 的子树结束应该是 " + subtreeEnd + "，实际 " + tag.SubtreeEnd);
        }

        static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
