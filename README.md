# NRFramework Gameplay Tags

层次标签。策划在标签图里配名字和父子，点一次「保存配置」，项目里就有可以直接点出来的代码。

支持 Unity **2021.3**、**2022.3**，以及 **Unity 6.0.6** 及以上。包上的 `2021.3` 是最低版本，2022 和 Unity 6 可以直接装。

它只管「身上现在有没有、有几份、父标签算不算有」。持续时间、周期掉血、动画和消息，由用的人自己接。

---

## 安装

本机要有 Git。Unity 菜单 `Window ▸ Package Manager` → 左上 `+` → `Add package from git URL...`，粘贴：

```
https://github.com/djr666666/NRFramework_GamePlayTags.git?path=/Assets/NRFramework_GamePlayTags
```

或在项目的 `Packages/manifest.json` 里加：

```json
"com.nrframework.gameplaytags": "https://github.com/djr666666/NRFramework_GamePlayTags.git?path=/Assets/NRFramework_GamePlayTags"
```

---

## 原理

名字用点号分成一棵树。`State.Control.Frozen` 是 `State` → `Control` → `Frozen`。

资源里只存你亲手登记的名字。`State` 和 `State.Control` 这种中间父节点，建表时按点号补上，不用一条条登记。

建好之后做一次深度优先遍历，同一层按名字排序，摊成连续编号。父标签的全部子孙占一段连续编号。所以「冰冻是不是控制的子标签」不用拆字符串，看编号落不落在控制的那一段里：

- 控制的编号是 `11`，它这段子树到 `20` 之前结束
- 冰冻的编号是 `14`
- `14` 大于 `11` 并且小于 `20`，所以控制是冰冻的祖先

每个人身上单独放一个容器，只记下亲手挂上的那几条。问「有没有控制」时，只扫这个人身上现有的几条。身上有冰冻，问控制算有。身上只有控制，问冰冻不算有。

同一条标签要算几份（三层灼烧），用计数容器。减到 0 才算没有。

运行时的编号每次建表都会重排，不能写进存档。存档和组件上只存名字。

这套东西不发消息。你在 `AddTag` / `RemoveTag` 之后自己调自己的监听。

---

## 400 个怪要多久

测的是容器本身：400 个人，每人身上 3 或 4 个标签（眩晕、冰冻、灼烧，一半人再多一个减速）。每帧每个人问 4 次：有没有控制、灼烧、死亡、减速。换标签是卸掉灼烧、挂上中毒。

| | 只查询 | 400 人同一帧都换 1 个，再查询 |
|---|---:|---:|
| 有无容器 | 0.006 ms | 0.012 ms |
| 计数容器 | 0.005 ms | 0.013 ms |

60 帧时一帧有 16.7 ms。400 人同时改一次状态大约 0.012 ms，占一帧的 0.07%。列表长开之后，这些增删每帧分配 0 字节。

同一帧把 400 个容器生成出来并挂上标签，大约 0.1 ms。有无容器这一下大约 70 KB，计数容器大约 106 KB，只发生在生成那一帧。

这是本机 Release 下跑真实容器代码的结果。Unity 编辑器里的 Mono 会慢几倍，400 人全部改一遍仍然远小于 1 ms。会把画面吃掉的是模型、碰撞和特效。

调用时要直接点导出的字段，例如 `Tags.State.Burning`。这个字段在导出时已经带好编号。不要在每个怪的 `Update` 里用字符串现查名字。

---

## 怎么用

1. 菜单 `NRFramework/Gameplay Tags/标签编辑器`。
2. 从父标签右边的圆点拖到子标签左边，就是连上。点一条线，按 Delete，或右键「取消连线」，就是拆开。
3. 全部改完，点绿色的 **保存配置**。
4. 工程里会出现 `Assets/GameplayTags/`：
   - `Tags.cs`：每次保存都会重写，别手改
   - `Tags.User.cs`：自己的包装写这里，保存不会覆盖
5. 代码里直接点：

```csharp
using NRFramework.GameplayTags;

unit.AddTag(Tags.State.Control.Frozen);
unit.RemoveTag(Tags.State.Control.Frozen);

// 身上有冰冻时，这一句也是 true
bool controlled = unit.HasTag(Tags.State.Control.Tag);

// 只问是不是亲手把「控制」这两个字挂了上去
bool pinned = unit.HasTagExact(Tags.State.Control.Tag);
```

有子标签的节点，自身叫 `.Tag`。叶子没有 `.Tag`，直接是 `Tags.State.Burning`。

组件上要存一个标签时，用 `GameplayTagLink`。资源里存的是名字。

---

## 案例

### 有没有被控制

冰冻、沉默、击晕都挂在 `State.Control` 下面。人身上任意一条在，问控制都算有。

```csharp
if (actor.Tags.HasTag(Tags.State.Control.Tag))
{
    // 不能行动。具体是冰冻还是沉默，这里不用拆。
    return;
}
```

### 灼烧叠层

同一条灼烧可以来自火圈和火球。两份都在才算烧着，减到 0 才灭。

```csharp
GameplayTagCountContainer burns = actor.Burns;

burns.AddTag(Tags.State.Burning);
burns.AddTag(Tags.State.Burning);
int left = burns.RemoveTag(Tags.State.Burning);

bool stillBurning = burns.HasTag(Tags.State.Burning);
int stacks = burns.GetCount(Tags.State.Burning);
```

`left` 是卸掉一份之后还剩几份。`stillBurning` 在份数大于 0 时为真。

### 不能在控制下施法

条件写一次，之后拿任意一个人的容器来问。禁止「控制」时，冰冻和沉默都会拦住。

```csharp
var cast = new GameplayTagRequirements();
cast.Forbid(Tags.State.Control.Tag);
cast.Require(Tags.State.Combat.Casting);

string blockedBy;
if (cast.IsMet(actor.Tags, out blockedBy))
{
    // 可以放
}
else
{
    // blockedBy 形如「不能有 State.Control」或「缺少 State.Combat.Casting」
}
```

### 装备把自己的标签算进角色

剑挂上角色时，剑上的冰冻和灼烧算进角色。卸下只收回这把剑的份数，光环的沉默还在。

```csharp
var hero = new GameplayTagCountContainer();
var sword = new GameplayTagCountContainer();
var aura = new GameplayTagCountContainer();

sword.SetParent(hero);
aura.SetParent(hero);
sword.AddTag(Tags.State.Control.Frozen);
sword.AddTag(Tags.State.Burning);
aura.AddTag(Tags.State.Control.Silenced);

bool frozen = hero.HasTag(Tags.State.Control.Frozen);

sword.SetParent(null);

bool stillControlled = hero.HasTag(Tags.State.Control.Tag);
bool stillSilenced = hero.HasTag(Tags.State.Control.Silenced);
```

卸下之后 `frozen` 不再成立，`stillSilenced` 仍然成立，所以 `stillControlled` 也成立。

`SetParent` 会按份数把标签搬到父容器上，用在穿上和卸下。每一帧的加减用 `AddTag` / `RemoveTag`。

### 自己的消息

库不广播。状态变了，在你自己的系统里发：

```csharp
bool had = actor.Tags.HasTagExact(Tags.State.Control.Frozen);
if (had)
    actor.Tags.RemoveTag(Tags.State.Control.Frozen);
else
    actor.Tags.AddTag(Tags.State.Control.Frozen);

actor.RaiseFrozenChanged(!had);
```

`RaiseFrozenChanged` 是项目自己的方法，可以接到动画、AI 或界面。

### 自己的包装写在 Tags.User.cs

这个文件不会被保存配置覆盖。

```csharp
namespace NRFramework.GameplayTags
{
    public static partial class Tags
    {
        public static bool IsControlled(GameplayTagContainer unit)
        {
            return unit != null && unit.HasTag(State.Control.Tag);
        }
    }
}
```

调用写成 `Tags.IsControlled(unit)`。

---

## 约定

- 全游戏一张标签表。保存配置之后，代码和资源里的名字要对得上。
- 每个人一个容器。不要在 Job 里改同一份容器。
- 你的业务程序集如果是单独的 asmdef，需要引用 `NRFramework.GameplayTags.Generated`，才能点到 `Tags`。
- 菜单 `NRFramework/Gameplay Tags/场景里放按 Q 演示` 会在当前场景放一个物体。播放后 Q 挂上或去掉冰冻，E / R 加减灼烧，T 卸下或戴上剑。用之前要先保存过一次配置。
