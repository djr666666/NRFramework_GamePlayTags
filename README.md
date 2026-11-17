# NRFramework Gameplay Tags

层次标签。策划在标签图里起名字，点一次「保存配置」，代码里就能直接点出来用。

支持 Unity **2021.3**、**2022.3**，以及 **Unity 6.0.6** 及以上。包上的 `2021.3` 是最低版本。

下面按第一次使用的顺序写。写业务时，每个角色只 `new` 一个容器，用来挂标签、问身上有没有。条件单、Inspector 下拉框、按字符串取标签都不是这件事，用到再看。

---

## 安装

本机要有 Git。Unity 菜单 `Window ▸ Package Manager` → 左上 `+` → `Add package from git URL...`，粘贴：

```
https://github.com/djr666666/NRFramework_GamePlayTags.git?path=/Assets/NRFramework_GamePlayTags
```

或在 `Packages/manifest.json` 里加：

```json
"com.nrframework.gameplaytags": "https://github.com/djr666666/NRFramework_GamePlayTags.git?path=/Assets/NRFramework_GamePlayTags"
```

你的业务代码如果在单独的 asmdef 里，装完后给那个程序集加上引用 `NRFramework.GameplayTags.Generated`。没有单独 asmdef 就不用加。

脚本顶部都写这一句：

```csharp
using NRFramework.GameplayTags;
```

---

## 第 1 步：打开编辑器，示例会自己出现

菜单 `NRFramework/Gameplay Tags/标签编辑器`。

包里已经带了一份示例标签表，里面有冰冻、灼烧、沉默这些名字。编辑器一打开，会把这份表复制到你自己的工程：

`Assets/GameplayTags/GameplayTagCatalog`

选中它，Inspector 上能看到已经登记了二十多条，也有注释。这些不用你一条条手打。

然后点绿色的 **保存配置**。同目录下会再出现三个文件，也是自动写的：

| 文件 | 你要不要改 |
|---|---|
| `Tags.cs` | 不要改。下次点保存配置会整文件重写。里面就是 `Tags.State.Control.Frozen` 这些字段。 |
| `Tags.User.cs` | 要加自己的方法时改这个。保存配置不会覆盖它。 |
| `NRFramework.GameplayTags.Generated.asmdef` | 不要改。它让导出的代码能看见这个包。 |

到这里就可以在代码里直接点 `Tags.State.Control.Frozen`。还没有挂到任何角色身上，挂标签从第 3 步开始。

想改名字或加新标签，就在这张图里改，改完再点一次保存配置。父名字 `State`、`State.Control` 会自动出现，不用单独登记。父子关系从父标签**右边**的圆点拖到子标签**左边**。点中一条线，按 Delete，或右键「取消连线」，线就拆了。

场景里如果多了一个叫「GameplayTag按Q演示」的物体，那是菜单 `NRFramework/Gameplay Tags/场景里放按 Q 演示` 放进去的。播放后 Q 挂冰冻，E / R 加减灼烧，T 戴上或卸下剑。它只是演示，不用的话从 Hierarchy 里删掉即可。

---

## 第 2 步：GameplayTag，一个标签的值

`GameplayTag` 是一个值，表示「哪一个标签」。它带着名字和编号。容器、条件、组件上选中的标签，传的都是它。

不要自己 `new GameplayTag()`。那样得到的是空标签 `GameplayTag.None`，挂到人身上不会生效。

保存配置之后，直接点出来：

```csharp
GameplayTag frozen = Tags.State.Control.Frozen;
GameplayTag control = Tags.State.Control.Tag;
```

叶子写成 `Tags.State.Control.Frozen`。这个节点下面还有子标签时，它自己写成 `Tags.State.Control.Tag`。

`frozen.Name` 是 `"State.Control.Frozen"`。这个字符串可以拿去显示、存档。编号会在下次保存配置时重排，不要把编号写进存档。

---

## 第 3 步：GameplayTagContainer，身上有没有

一个人、一只怪，各放一个容器。容器只记「有」或「没有」，同一条标签加第二次没有效果。

什么时候用它：冰冻、沉默、死亡这种，有和没有就够了。

在角色脚本里声明，并在 `Awake` 里 `new` 出来。每个对象自己 new 一份，不要大家共用一个。

```csharp
public sealed class Actor : MonoBehaviour
{
    public GameplayTagContainer Tags { get; private set; }

    void Awake()
    {
        Tags = new GameplayTagContainer();
    }

    public void Freeze()
    {
        Tags.AddTag(Tags.State.Control.Frozen);
    }

    public void Unfreeze()
    {
        Tags.RemoveTag(Tags.State.Control.Frozen);
    }
}
```

这里有两个 `Tags`。前面的 `Tags` 是这个人身上的容器。`Tags.State.Control.Frozen` 是第 2 步那个标签值。名字撞在一起时，把容器改名为 `Body`：

```csharp
public GameplayTagContainer Body { get; private set; }

void Awake()
{
    Body = new GameplayTagContainer();
}

public void Freeze()
{
    Body.AddTag(Tags.State.Control.Frozen);
}
```

后面的例子用 `Body`，避免和导出的 `Tags` 混在一起。

示例里，冰冻和沉默都挂在控制下面：

```
控制
├─ 冰冻
└─ 沉默
```

给角色加上冰冻：

```csharp
Body.AddTag(Tags.State.Control.Frozen);
```

角色身上记下的就是「冰冻」这两个字。

控制下面还有别的标签，所以控制自己写成 `Tags.State.Control.Tag`。冰冻没有下级，写成 `Tags.State.Control.Frozen`。

**HasTagExact：问的就是这一个名字。**

```csharp
Body.HasTagExact(Tags.State.Control.Frozen); // true，身上有冰冻
Body.HasTagExact(Tags.State.Control.Tag);    // false，身上记下的名字是冰冻
```

**HasTag：问这一支里有没有任何一个。**

冰冻算控制，沉默也算控制。身上有冰冻时：

```csharp
Body.HasTag(Tags.State.Control.Tag); // true
```

身上换成只有沉默，问控制仍是 true。冰冻和沉默都没有时，问控制是 false。

技能被控制打断时用 `HasTag(控制)`。要区分是冰冻还是沉默时，用 `HasTagExact`。

**卸下时写加上的那个名字。**

```csharp
Body.RemoveTag(Tags.State.Control.Frozen); // 冰冻被卸掉
```

`RemoveTag(控制)` 不会把冰冻卸掉。加上的名字是冰冻，卸也写冰冻。卸掉之后，再问控制就是 false。

同一条再 `AddTag` 一次，返回 false，身上还是这一条。`Clear()` 一次清掉这个人身上的全部标签，对象回对象池时用。

---

## 第 4 步：这一问为什么很快

第 3 步已经用冰冻和控制分过两种问法。这里只补它为什么快。

名字是一棵树。`State.Control.Frozen` 就是 State → Control → Frozen。资源里只存你登记的那一条，中间的父节点是保存时补上的。建表之后每个标签有一段连续编号，父标签的子孙都落在它的编号范围里，所以这一问不用拆字符串。

400 个怪、每人身上 3 到 4 个标签时，这一问很便宜。只查询大约 0.006 毫秒；同一帧 400 个人都改一个标签再查询，大约 0.012 毫秒。60 帧的一帧有 16.7 毫秒。列表长开之后，这些增删每帧不再分配内存。生成 400 个容器并挂上标签，大约 0.1 毫秒，有无容器大约 70 KB，计数容器大约 106 KB，只发生在生成那一帧。

调用时用第 2 步的字段。不要在每只怪的 `Update` 里写 `GameplayTagLibrary.Get("State.Burning")`，那是按字符串查表。

---

## 第 5 步：GameplayTagCountContainer，同一条要算几份

这还是查角色身上有没有标签。和第 3 步是同一个活，只是同一条要记几份。

火圈和火球都可以各挂一份灼烧。两份都在才算烧着，减到 0 才灭。这种才 `new` 计数容器。只问有没有、不用数份数，继续用第 3 步那个容器。

```csharp
public GameplayTagCountContainer Burns { get; private set; }

void Awake()
{
    Body = new GameplayTagContainer();
    Burns = new GameplayTagCountContainer();
}

public void Ignite()
{
    Burns.AddTag(Tags.State.Burning);
}

public void ExtinguishOne()
{
    int left = Burns.RemoveTag(Tags.State.Burning);
    // left 是卸掉一份之后还剩几份。0 表示已经不烧了。
}
```

```csharp
int stacks = Burns.GetCount(Tags.State.Burning);
bool stillBurning = Burns.HasTag(Tags.State.Burning);
```

`AddTag` 的返回值就是加上之后的份数。同一条从 0 到 1 的那一次，这个人开始处于灼烧；从 1 减到 0 的那一次，灼烧结束。

一个人可以同时有一个 `GameplayTagContainer` 和一个 `GameplayTagCountContainer`。冰冻放前者，灼烧层数放后者。

---

## 第 6 步：放火球，查自己身上的标签

按下火球，查这个玩家自己的 `Body`。有正在施法，并且没有控制，就放。不用 `new` 别的类。

```csharp
bool canCast = Body.HasTag(Tags.State.Combat.Casting)
    && !Body.HasTag(Tags.State.Control.Tag);
if (canCast)
    ShootFireball();
```

冰冻和沉默都算控制，`HasTag(控制)` 已经算进去了。

`canCast` 是 false 时，这里看不出是缺了施法，还是因为冰冻了。界面上要显示这句原因，才 `new` 一个 `GameplayTagRequirements`。只要决定放不放，写到上面为止。

---

## 第 7 步：装备挂到人身上

剑、光环各自是一个计数容器。`SetParent` 把它们算进角色。卸下时只收回这一件自己的份数。

`SetParent` 用在穿上和卸下。每一帧的加减仍然用 `AddTag` / `RemoveTag`。

```csharp
GameplayTagCountContainer hero = new GameplayTagCountContainer();
GameplayTagCountContainer sword = new GameplayTagCountContainer();
GameplayTagCountContainer aura = new GameplayTagCountContainer();

sword.SetParent(hero);
aura.SetParent(hero);

sword.AddTag(Tags.State.Control.Frozen);
sword.AddTag(Tags.State.Burning);
aura.AddTag(Tags.State.Control.Silenced);

bool frozen = hero.HasTag(Tags.State.Control.Frozen);

sword.SetParent(null);

bool stillSilenced = hero.HasTag(Tags.State.Control.Silenced);
bool stillControlled = hero.HasTag(Tags.State.Control.Tag);
```

卸下剑之后，冰冻跟着走，沉默还在，所以问控制仍然算有。

---

## 第 8 步：GameplayTagLink，给策划一个下拉框

标签编辑器和 `GameplayTagCatalog` 的 Inspector 上都没有这个下拉框。按 Q 的演示也没有。那些地方用的是代码里的 `Tags.XXX`。

代码里已经知道是哪一个标签时，继续写 `Tags.XXX`，这一步跳过。

要让策划在物体上自己选时，在你的脚本里加一个字段，类型写成 `GameplayTagLink`。脚本挂到场景里的物体上，选中那个物体，Inspector 里这个字段是一个下拉按钮，字样是「点这里选择」。点开就是标签表，名字按 `State/Burning` 这样排。选中的名字存在这个组件上。

```csharp
public sealed class IgniteSkill : MonoBehaviour
{
    public GameplayTagLink Effect;

    public void ApplyTo(Actor actor)
    {
        if (Effect.IsEmpty)
            return;
        actor.Burns.AddTag(Effect.Resolve());
    }
}
```

`Effect.IsEmpty` 表示策划还没选。`Effect.Resolve()` 把选中的名字换成 `Tags.State.Burning` 那种值，然后才能 `AddTag`。用之前先做过第 1 步的保存配置。这一条还没导出时，`Resolve()` 会告诉你是哪个名字。

---

## 第 9 步：存档里是一串字，和那个标签表文件

平时挂灼烧，代码里直接写：

```csharp
Body.AddTag(Tags.State.Burning);
```

这句在写代码时就能点出来。你知道这是灼烧。

存档文件里存不下这句代码，只能存字。这个人身上有灼烧，你写进存档的是 `State.Burning`。下次读档，程序拿到的是一个变量，里面是这串字。这一行没有 `Tags.State.Burning` 可以点。

`GameplayTagLibrary` 全游戏只有一份，不用 `new`。它把存档里的字换回和 `Tags.State.Burning` 同一个标签，然后才能往人身上挂：

```csharp
string saved = "State.Burning";                 // 存档里读出来的字
GameplayTag tag = GameplayTagLibrary.Get(saved); // 换成标签
Body.AddTag(tag);                                // 和 Body.AddTag(Tags.State.Burning) 相同
```

| 存档里的字 | 结果 |
| --- | --- |
| `State.Burning`，并且点过保存配置 | 换成灼烧，挂到身上 |
| `State.Burning`，改完图还没点保存配置 | `Get` 报错，提示先保存配置 |
| 标签表里没有的名字 | `Get` 报错。改用 `TryGet` 则返回 false，身上不加 |

一个人身上存了四个标签，读档就调用四次 `Get`。一百个人也是读档那一下换完，不是每帧换。不用给每个技能、每个人 `new` 一个 Library。

`GameplayTagCatalog` 是 Project 里那个文件 `Assets/GameplayTags/GameplayTagCatalog`。点开它，Inspector 上的已登记、已写注释，就是这个文件。全游戏一份。改标签图再点保存配置，编辑器把图写进这个文件，并生成 `Tags.cs`。角色和技能的代码不问这个文件。

---

## 第 10 步：状态变了，自己发消息

这个包不广播。动画、AI、界面要自己接。挂上或卸下之后，调用你自己的方法：

```csharp
public void Freeze()
{
    bool added = Body.AddTag(Tags.State.Control.Frozen);
    if (added)
        OnFrozenChanged(true);
}

public void Unfreeze()
{
    bool removed = Body.RemoveTag(Tags.State.Control.Frozen);
    if (removed)
        OnFrozenChanged(false);
}

void OnFrozenChanged(bool frozen)
{
    // 这里接动画、AI、头顶图标。
}
```

想少写一点，把常用判断放进 `Assets/GameplayTags/Tags.User.cs`。这个文件不会被保存配置覆盖。

```csharp
namespace NRFramework.GameplayTags
{
    public static partial class Tags
    {
        public static bool IsControlled(GameplayTagContainer body)
        {
            return body != null && body.HasTag(State.Control.Tag);
        }
    }
}
```

调用写成 `Tags.IsControlled(actor.Body)`。

---

## 写的时候记住

- 全游戏一张标签表。改了名字或连线，再点一次保存配置。
- 查身上有没有标签，每个角色自己 `new` 一个容器。有没有用 `GameplayTagContainer`。同一条要叠几层，改用 `GameplayTagCountContainer`。两种都要，才各 `new` 一个。
- `Tags.XXX` 是标签名字，不用 `new`。
- 条件单、下拉框、按字符串取标签，不参与「身上有没有」。用不到就不用 `new`。

---

## 接口一览

查角色身上有没有标签，就是下面两个类，做的是同一件事。

- 只问有没有：`new GameplayTagContainer()`。
- 同一条要算几份：改成 `new GameplayTagCountContainer()`。
- 冰冻只问有没有、灼烧要叠层：这两个各 `new` 一个。

不是五个类都要 `new`。`Tags.XXX` 只是传进去的名字。条件单、下拉框、字符串取标签放在这两个类后面，不看也不影响挂标签和查询。

### Tags

保存配置生成。业务里直接点，不用 new。

| 写法 | 作用 |
| --- | --- |
| `Tags.State.Control.Frozen` | 一个标签值。叶子就这样写。 |
| `Tags.State.Control.Tag` | 这个节点下面还有子标签时，节点自己用 `.Tag`。 |
| `Tags.TryGet("State.Burning", out tag)` | 手里是字符串时试着取出标签。没有这个名字就返回 false。 |

### GameplayTag

一个标签的值。从 `Tags.XXX` 拿，不要自己 new。

| 写法 | 作用 |
| --- | --- |
| `tag.Name` | 点分名字，例如 `"State.Control.Frozen"`。可以拿去显示、存档。 |
| `tag.IsNone` | 是不是空标签。 |
| `GameplayTag.None` | 空标签。 |
| `tag.IsParentOf(other)` | 自己是不是 other 的上级。自己和自己比，结果是 false。 |
| `tag.IsSameOrParentOf(other)` | 自己就是 other，或者自己是它的上级。`HasTag` 内部用这一条。 |
| `a == b` / `a != b` | 是不是同一个标签。 |
| `tag.ToString()` | 空标签得到 `"None"`，否则得到 `Name`。 |

`GameplayTag.Baked(...)` 是保存配置写进 `Tags.cs` 的。业务代码不要调用。

### GameplayTagContainer：身上有没有

一个人 `new` 一份。只记有没有，同一条加第二次没有效果。查询就是 `HasTag` 和 `HasTagExact`。

| 写法 | 作用 |
| --- | --- |
| `new GameplayTagContainer()` | 做出这个人身上的清单。 |
| `Body.Count` | 清单里现在有几条。 |
| `Body.AddTag(tag)` | 把这一条写进清单。写进去返回 true。已经有了，或传入空标签，返回 false。 |
| `Body.RemoveTag(tag)` | 从清单删掉名字完全一样的那一条。删掉了返回 true。清单里没有这一条，返回 false。不会连带删掉它下面的标签。 |
| `Body.HasTagExact(tag)` | 清单里有没有名字完全一样的这一条。 |
| `Body.HasTag(tag)` | 清单里有这一条，或者有它下面的某一条。 |
| `Body.Clear()` | 清空这个人身上的标签。 |
| `Body.CopyExplicitTags(list)` | 把清单里现有的标签复制进你给的 `List`。会先清空那个 List。 |

### GameplayTagCountContainer：身上有没有，并且要算几份

和第 3 步同一个活。同一条可以有好几份，减到 0 才算没有。不需要份数就不要 `new` 它。

| 写法 | 作用 |
| --- | --- |
| `new GameplayTagCountContainer()` | 做出一份计数容器。 |
| `Burns.TagCount` | 现在有几种标签。这是种类数，不是份数加总。 |
| `Burns.AddTag(tag)` | 这一条加 1 份，返回加上之后的份数。空标签返回 0。如果 `SetParent` 过，父容器也会加 1 份。 |
| `Burns.RemoveTag(tag)` | 这一条减 1 份，返回剩下的份数。本来就没有，返回 0。父容器也会减 1 份。 |
| `Burns.GetCount(tag)` | 这一条现在有几份。没有返回 0。只看这一条，不把下面的份数加进来。 |
| `Burns.HasTagExact(tag)` | 这一条的份数大于 0。 |
| `Burns.HasTag(tag)` | 这一条份数大于 0，或者它下面某一条份数大于 0。 |
| `Burns.SetParent(hero)` | 把这一份算进 `hero`。已经加过的份数会一起算进去。 |
| `Burns.SetParent(null)` | 卸下。只收回这一份自己的份数，父容器上别人的份数还在。 |

上面两个类就是业务里查身上有没有标签的全部。能不能放，也是拿它们的 `HasTag` 来写。

### GameplayTagRequirements：给技能写规则，把原因显示给玩家

程序员写 `HasTag` 时，自己知道是冰冻还是眩晕导致不放。玩家只看到放不出来。这个类按这个技能的规则再查一遍身上的标签，把没过的那一条变成一句字，给界面显示。

一条规则用一次 `Require` 或 `Forbid`。两条就写两行，四条就写四行。它按顺序问，停在第一条没过的上面。

一百个技能就一百份规则，每份在技能配好时 `new` 一次，存在这个技能上。按下哪个技能，就只问那一份。不是每次按键都 `new`，也不是每帧把一百个都问一遍。

```csharp
public sealed class Fireball : MonoBehaviour
{
    GameplayTagRequirements _rule; // 这一份一直留着

    void Awake()
    {
        _rule = new GameplayTagRequirements();
        _rule.Require(Tags.State.Combat.Casting);
        _rule.Forbid(Tags.State.Control.Tag);
    }

    public void OnPress(GameplayTagContainer body)
    {
        string blockedBy;
        if (_rule.IsMet(body, out blockedBy))
            Shoot();
        else
            Show(blockedBy);
    }
}
```

技能栏要把一百个图标都刷成灰色时，才一百份各问一次。之前测过，四百个人各问几条标签大约 0.006 毫秒。一百个技能问同一个人，比那个还少。一帧有 16.7 毫秒。

火球：必须正在施法，不能处于控制。冰冻、眩晕都挂在控制下面，写一条「不能有控制」就同时拦住这两个。

```csharp
GameplayTagRequirements cast = new GameplayTagRequirements();
cast.Require(Tags.State.Combat.Casting); // 必须有：正在施法
cast.Forbid(Tags.State.Control.Tag);     // 不能有：控制。冰冻、眩晕都算

string blockedBy;
bool ok = cast.IsMet(Body, out blockedBy); // 拿这个玩家身上的标签对上面两行
if (!ok)
    Show(blockedBy); // 给玩家看这句字
```

身上有正在施法，也有冰冻：第一行过了，第二行不过。`ok` 是 false，`blockedBy` 是「不能有 State.Control」。玩家看到这句。

圣光斩要四条：必须圣光，必须血量低于 20%，不能眩晕，不能冰冻。圣光和「血量低于 20%」先在标签图里加好，再点保存配置。血量是数字，这个类读不到。扣血扣到 20% 以下时，你自己 `Body.AddTag(Tags.State.LowHealth)`，回升后再 `RemoveTag`。

```csharp
GameplayTagRequirements holy = new GameplayTagRequirements();
holy.Require(Tags.State.Holy);              // 必须有：圣光
holy.Require(Tags.State.LowHealth);         // 必须有：血量低于 20% 时你挂上的那个标签
holy.Forbid(Tags.State.Control.Stun);       // 不能有：眩晕
holy.Forbid(Tags.State.Control.Frozen);     // 不能有：冰冻

string blockedBy;
bool ok = holy.IsMet(Body, out blockedBy);
if (ok)
    CastHolySlash();
else
    Show(blockedBy);
```

四种结果：

| 身上现在有什么 | blockedBy |
| --- | --- |
| 没挂圣光 | 缺少 State.Holy |
| 有圣光，血量还高于 20%，没挂 LowHealth | 缺少 State.LowHealth |
| 有圣光，有 LowHealth，并且眩晕 | 不能有 State.Control.Stun |
| 有圣光，有 LowHealth，没有眩晕，但是冰冻 | 不能有 State.Control.Frozen |

四条都过，`ok` 是 true，`blockedBy` 是 null，圣光斩放出去。

示例标签里已经有的四条，不用新加名字。必须有护盾、必须正在施法，不能沉默、不能冰冻：

```csharp
GameplayTagRequirements smash = new GameplayTagRequirements();
smash.Require(Tags.State.Guard.Shield);     // 必须有：护盾
smash.Require(Tags.State.Combat.Casting);   // 必须有：正在施法
smash.Forbid(Tags.State.Control.Silenced);  // 不能有：沉默
smash.Forbid(Tags.State.Control.Frozen);    // 不能有：冰冻

string blockedBy;
bool ok = smash.IsMet(Body, out blockedBy);
if (!ok)
    Show(blockedBy); // 「缺少 State.Guard.Shield」或「不能有 State.Control.Frozen」
```

| 写法 | 作用 |
| --- | --- |
| `new GameplayTagRequirements()` | 做出这份条件单。 |
| `cast.Require(tag)` | 加上一条「必须有」。它下面的标签也算有。 |
| `cast.Forbid(tag)` | 加上一条「不能有」。它下面的标签也算有，会拦住。 |
| `cast.IsMet(Body)` | 问这个人过不过。过了返回 true。 |
| `cast.IsMet(Body, out blockedBy)` | 同上。没过时 `blockedBy` 写明缺哪条，或不能有哪条。 |

`Body` 换成灼烧那种计数容器也可以问。同一个标签不要又 `Require` 又 `Forbid`。

### GameplayTagLink：策划在 Inspector 里选一个标签

代码里已经写了 `Tags.State.Burning`，就不用它。标签编辑器上也不会出现这个下拉框。

要让策划自己选时，在脚本里加一个字段，把脚本挂到物体上，选中那个物体：

```csharp
public GameplayTagLink Effect;
```

Inspector 里 `Effect` 是一个按钮，字样是「点这里选择」。点开是 `State/Burning` 这种名字。选中的只是名字，还不能直接 `AddTag`。挂到人身上之前先换成标签值：

```csharp
if (Effect.IsEmpty)
    return;
actor.Burns.AddTag(Effect.Resolve());
```

| 写法 | 作用 |
| --- | --- |
| `public GameplayTagLink Effect;` | 在物体上留一个下拉框。 |
| `Effect.IsEmpty` | 策划还没选。这时不要调用 `Resolve`。 |
| `Effect.Name` | 选中的名字，例如 `"State.Burning"`。 |
| `Effect.Resolve()` | 把名字换成能 `AddTag` 的标签值。这一条还没保存配置时，会告诉你是哪个名字。 |

### GameplayTagLibrary：存档里是一串字，点不到 Tags.XXX

平时挂灼烧，直接写：

```csharp
Body.AddTag(Tags.State.Burning);
```

写代码的时候你点得到 `Tags.State.Burning`，你知道这是灼烧。

存档里存的不是这句代码，是一串字。这个人身上有灼烧，存进去的是 `State.Burning`。读档时这串字在变量里：

```csharp
string saved = "State.Burning"; // 从存档读出来的。这里没有 Tags.XXX 可以点
```

你自己知道这是灼烧。这行代码只看见一串字，没法写成 `Tags.State.Burning`。`Get` 把这串字换成和 `Tags.State.Burning` 同一个标签，然后才能挂上：

```csharp
GameplayTag tag = GameplayTagLibrary.Get(saved); // 字换成标签
Body.AddTag(tag);                                // 人身上有了灼烧
```

| 存档里的字 | 结果 |
| --- | --- |
| `State.Burning`，并且点过保存配置 | 换成灼烧，挂到身上 |
| 改完标签图，还没点保存配置 | `Get` 报错，提示先保存配置 |
| 标签表里没有这个名字 | `Get` 报错。`TryGet` 返回 false，身上不加 |

全游戏只有这一份 Library，不用 `new`。一个人存了四个标签，读档调用四次 `Get`。一百个人也只在读档那一下换完。不是每个技能 new 一个，也不是每帧问一次。

策划在下拉框里选中的同样是名字。`Effect.Resolve()` 内部就是这次 `Get`。

| 写法 | 作用 |
| --- | --- |
| `GameplayTagLibrary.Get("State.Burning")` | 把这串字换成标签。没有这个名字会报错。 |
| `GameplayTagLibrary.TryGet("State.Burning", out tag)` | 同样是换。没有这个名字时返回 false。 |
| `GameplayTagLibrary.IsReady` | 保存配置生成的表装上了没有。进游戏后一般已经是 true。 |
| `GameplayTagLibrary.Use(catalog)` | 拿标签表文件对一遍名字。有没导出的会指出是哪一条。平常不用调用。 |

### GameplayTagCatalog：全游戏那一个标签表文件

你已经点开过它。Project 里的 `Assets/GameplayTags/GameplayTagCatalog`，Inspector 上「已登记 23」「已写注释 29」，就是这个文件里的内容。

它不是人身上的容器，也不是技能上的规则。全游戏一份。你在标签图里改名字、连线，点保存配置，编辑器把图写进这个文件，同时生成 `Tags.cs`。

角色和技能的代码不问这个文件。问身上有没有，用容器的 `HasTag`。问存档里的字，用上面的 `GameplayTagLibrary.Get`。

`NoteCount`、`CopyTable`、`ReplaceWith` 是编辑器读写这个文件用的。技能脚本、角色脚本不要调用。

| 写法 | 作用 |
| --- | --- |
| `catalog.NoteCount` | 这个文件里写了几条注释。 |
| `catalog.CopyTable()` | 编辑器把文件读进内存。 |
| `catalog.ReplaceWith(table)` | 点保存配置时，编辑器把图写回这个文件。 |
