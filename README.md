# NRFramework Gameplay Tags

层次标签。策划在标签图里起名字，点一次「保存配置」，代码里就能直接点出来用。

支持 Unity **2021.3**、**2022.3**，以及 **Unity 6.0.6** 及以上。包上的 `2021.3` 是最低版本。

下面按第一次使用的顺序写。先把名字做出来，再一个类一个类地 `new`。每一节只解决一件事。

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

火圈和火球都可以各挂一份灼烧。两份都在才算烧着，减到 0 才灭。这种用计数容器，不要用第 3 步的容器。

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

## 第 6 步：GameplayTagRequirements，这一下能不能放

技能的条件单独放一个对象。必须有的标签都要有，不能有的标签一个都不能有。禁止「控制」时，冰冻和沉默都会拦住，因为第 4 步的父标签规则在这里同样生效。

条件在技能配好时 `new` 一次，以后每次施放拿人身上的容器去问。不要每放一次就 new 一个。

```csharp
GameplayTagRequirements cast = new GameplayTagRequirements();
cast.Require(Tags.State.Combat.Casting);
cast.Forbid(Tags.State.Control.Tag);

string blockedBy;
if (cast.IsMet(Body, out blockedBy))
{
    // 可以放。Body 是第 3 步的 GameplayTagContainer。
}
else
{
    // blockedBy 是「缺少 State.Combat.Casting」或「不能有 State.Control」
}
```

计数容器也能拿来问，因为它和有无容器都实现了同一句 `HasTag`：

```csharp
cast.IsMet(Burns, out blockedBy);
```

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

## 第 9 步：GameplayTagCatalog 和 GameplayTagLibrary

`GameplayTagCatalog` 是第 1 步那个标签表资源。编辑器负责创建它。游戏运行时不要 `new`，也不要 `CreateInstance`。

`GameplayTagLibrary` 是静态类，不能 `new`。保存配置生成的代码会在加载时自己调用 `Install`，把导出的标签交进来。业务代码不用自己装表。

只有手里是字符串、不是 `Tags.XXX` 时才找它。`GameplayTagLink.Resolve()` 内部就是这样：

```csharp
GameplayTag tag = GameplayTagLibrary.Get("State.Burning");
```

开局如果想确认资源里的名字都已经导出，把标签表拖到脚本上，调用一次：

```csharp
public GameplayTagCatalog Catalog;

void Awake()
{
    if (Catalog != null)
        GameplayTagLibrary.Use(Catalog);
}
```

`Use` 对不上时会指出哪一条还没点保存配置。对得上就什么也不做。平常只点 `Tags.XXX` 的项目，可以不调用它。

`IsReady` 为 true 表示导出的表已经装上。

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
- 每个角色自己 `new` 容器。不要在 Job 里改同一份。
- `GameplayTag` 和 `GameplayTagLink` 是值。`GameplayTagContainer`、`GameplayTagCountContainer`、`GameplayTagRequirements` 才是你 `new` 的对象。
- `GameplayTagCatalog` 用编辑器创建。`GameplayTagLibrary` 和 `Tags` 不用 new。

---

## 接口一览

下面是业务代码会碰到的接口。每一条只做一件事。

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

### GameplayTagContainer

一个人一份。只记有没有，同一条加第二次没有效果。

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

### GameplayTagCountContainer

同一条可以有好几份。减到 0 才算没有。

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

下面三样都不是「给角色挂标签」。挂标签、问身上有没有，用前面的 `Body` 和 `Tags.XXX`。

### GameplayTagRequirements：这一下能不能放

一份条件单。技能配好时 `new` 一次，以后每次施放拿角色身上的容器来问。

火球的例子：必须正在施法，而且不能处于控制。冰冻和沉默都挂在控制下面，身上有其中一个，这一下就不许放。

```csharp
GameplayTagRequirements cast = new GameplayTagRequirements();
cast.Require(Tags.State.Combat.Casting);
cast.Forbid(Tags.State.Control.Tag);

string blockedBy;
if (cast.IsMet(Body, out blockedBy))
{
    // 可以放
}
else
{
    // blockedBy 是「缺少 State.Combat.Casting」或「不能有 State.Control」
}
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

### GameplayTagLibrary：手里只有一段字符串时才用

平时写 `Tags.State.Burning`。保存配置之后，生成的代码会自己把表装好，业务里不用 `new`，也不用自己装表。

下拉框里选中的是一段字符串。`Effect.Resolve()` 内部就是按这段字符串来取。存档读出来也是字符串时，同样用它：

```csharp
GameplayTag tag = GameplayTagLibrary.Get("State.Burning");
Body.AddTag(tag);
```

名字在标签表里没有时，`Get` 会报错，并提示先点保存配置。不确定有没有这个名字时：

```csharp
GameplayTag tag;
if (GameplayTagLibrary.TryGet("State.Burning", out tag))
    Body.AddTag(tag);
```

| 写法 | 作用 |
| --- | --- |
| `GameplayTagLibrary.Get("State.Burning")` | 用字符串取出标签。没有这个名字会报错。 |
| `GameplayTagLibrary.TryGet("State.Burning", out tag)` | 同样是取。没有这个名字时返回 false，不报错。 |
| `GameplayTagLibrary.IsReady` | 导出的表装上了没有。保存配置之后，进游戏时一般已经是 true。 |
| `GameplayTagLibrary.Use(catalog)` | 拿标签表资源对一遍名字。有没导出的会指出是哪一条。平常可以不调用。 |

### GameplayTagCatalog

标签表资源，在 `Assets/GameplayTags/GameplayTagCatalog`。编辑器创建和保存。运行时不要 `new`，也不要 `CreateInstance`。

| 写法 | 作用 |
| --- | --- |
| `catalog.NoteCount` | 这份资源里写了几条注释。 |
| `catalog.CopyTable()` | 复制出一份内存里的标签表。编辑器和 `Use` 在用。 |
| `catalog.ReplaceWith(table)` | 用一份内存表覆盖资源。标签编辑器保存时用。业务代码不要调用。 |
