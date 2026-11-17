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

## 第 1 步：先做出名字

标签不是在代码里现编的字符串。先在编辑器里登记，再点保存，才会有 `Tags.State.Control.Frozen` 这种字段。

1. 菜单 `NRFramework/Gameplay Tags/标签编辑器`。
2. 点「添加」，写下 `State.Control.Frozen`。父名字 `State`、`State.Control` 会自动出现，不用单独登记。
3. 再加几条你马上要用的，例如 `State.Burning`、`State.Control.Silenced`、`State.Combat.Casting`。
4. 父子关系：从父标签**右边**的圆点拖到子标签**左边**。点中一条线，按 Delete，或右键「取消连线」，线就拆了。
5. 点绿色的 **保存配置**。

保存之后，工程里出现文件夹 `Assets/GameplayTags/`：

| 文件 | 你要不要改 |
|---|---|
| `Tags.cs` | 不要改。下次点保存配置会整文件重写。 |
| `Tags.User.cs` | 要加自己的方法时改这个。保存配置不会覆盖它。 |
| `NRFramework.GameplayTags.Generated.asmdef` | 不要改。它让导出的代码能看见这个包。 |

到这里，名字准备好了。还没有挂到任何角色身上。

想先看别人写好的演示：菜单 `NRFramework/Gameplay Tags/场景里放按 Q 演示`，播放后 Q 挂冰冻，E / R 加减灼烧，T 戴上或卸下剑。演示能跑，只说明包装上了。你自己的角色还是要按下面的步骤写。

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

常用的四句：

```csharp
Body.AddTag(Tags.State.Control.Frozen);          // 挂上。已经有了就什么也不做
Body.RemoveTag(Tags.State.Control.Frozen);       // 卸下亲手挂上的那一条
bool frozen = Body.HasTagExact(Tags.State.Control.Frozen); // 是不是亲手挂了冰冻
bool controlled = Body.HasTag(Tags.State.Control.Tag);      // 身上有没有任何一种控制
```

`AddTag` 返回 `false` 表示没挂上：要么是空标签，要么这条已经在身上。

`Clear()` 把这个人身上的标签一次清掉，对象回收进对象池时用。

---

## 第 4 步：为什么有冰冻，问控制也算有

`State.Control.Frozen` 挂在 `State.Control` 下面。容器里只存了冰冻这一条。问 `HasTag(Tags.State.Control.Tag)` 时，会看身上的冰冻是不是控制的子孙。是，就算这个人处于控制。

反过来，身上只有「控制」本身、没有冰冻时，`HasTagExact(Tags.State.Control.Frozen)` 是 false。问父标签很宽，问叶子很严。

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

## 第 8 步：GameplayTagLink，在组件上选一个标签

策划要在 Inspector 里点选一个标签，而不是写死在代码里。字段类型用 `GameplayTagLink`。它是结构体，Unity 会自己序列化，一般不用在代码里 `new`。

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

`Effect` 在资源里只存名字，例如 `State.Burning`。`Resolve()` 在播放时换成第 2 步那种标签值。用之前要先做过第 1 步的保存配置，否则 `Resolve()` 会告诉你哪一条还没导出。

代码里确实要自己做一个链接时：

```csharp
GameplayTagLink link = new GameplayTagLink("State.Burning");
GameplayTag tag = link.Resolve();
```

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
