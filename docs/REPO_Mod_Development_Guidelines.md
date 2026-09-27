# R.E.P.O. Mod 开发通用规范

作者：AngelcoMilk

本文是面向 R.E.P.O.（Unity Mono）的 BepInEx 5 / Harmony mod 开发规范。目标是让项目：

- 可维护：分层清楚，每个规则有明确出处；
- 可验证：尽量在构建期或启动时自动发现问题；
- 可降级：游戏或依赖更新时只影响相关功能；
- 少踩版权与联机风险。

本文只保留通用规则。ShrinkCart 自己的设计、接口与维护流程分别见 [ARCHITECTURE.md](ARCHITECTURE.md)、[INTERFACES.md](INTERFACES.md) 和 [MAINTENANCE.md](MAINTENANCE.md)，文中以它们作为示例；这些规则背后的真实案例见 [LESSONS_LEARNED.md](LESSONS_LEARNED.md)。

## 修订记录

| 版本 | 日期 | 内容 |
|---|---|---|
| 2.0 | 2026-09-26 | 结合 ShrinkCart 0.3.1 重构修订，详见下方说明 |
| 1.x | — | 初版 |

2.0 版的修订内容：

- **新增规则**：网络角色（§6）、非公开成员访问阶梯（§5）、hook 调用频率（§4）、配置迁移（§8）、依赖库全局状态与状态所有权（§9）、Unity 空值（§10）、可观测性（§11）。
- **修正**：
  - 包结构改为 DLL 放在 ZIP 根目录，并修正"三个文件"的措辞（§12.1）；
  - 规定版本号的唯一来源（§12.2）；
  - 构建期校验改为从编译产物推导（§12.3）；
  - 依赖只从指定 profile 解析，删除带日期的核对结论（§14）；
  - 放宽过度泛化的配置要求：不再要求"所有参数可配置""一律默认保守""一律提供紧急停止"（§7）；
  - 允许本地化配置键名（§7.3）。
- **替换与移出**：
  - 来自其他项目的示例（Stim、脚步事件、连接端口等）已替换；
  - Thunderstore API 准则移到[附录 A](appendix/Thunderstore_API.md)；
  - GTFO 对照移到[附录 B](appendix/GTFO_BepInExPack_Reference.md)。

## 1. 基本原则

- 只发布自己的代码、配置、图标和自制资产。不发布游戏原始 DLL、反编译源码、ripped 资源或 patched Unity 工程。
- 用 BepInEx + Harmony 做运行时补丁，不直接修改游戏的原始 DLL。
- 优先使用强类型引用，例如 `typeof(PhysGrabCart)`、`nameof(PhysGrabCart.ObjectsInCart)`。访问非公开成员时按 §5 的访问阶梯处理。
- 每个功能先确定自己的**网络角色**（§6），不做超出角色的事。
- 把"游戏更新后 hook 是否还有效"尽量交给编译和构建期校验去发现（§12.3），不要只靠人工核对。
- 出错时只降级受影响的功能，绝不让异常传回游戏。

## 2. 环境与引用

- 使用 .NET SDK 项目（csproj）加 `dotnet build`。插件目标框架用 `netstandard2.1`，用 `global.json` 锁定 SDK 主版本。
- 本机路径（游戏目录、r2modman profile）写在 git 忽略的本地 props 文件中（例如 `<Mod>.local.props`），仓库只提交 `.example` 模板。不要把本机路径写死在脚本里。
- 编译引用：
  - NuGet：
    - `BepInEx.Core` 与 `HarmonyX`：只在编译期使用（`ExcludeAssets="runtime"`），运行时由 BepInEx 提供；
    - `BepInEx.PluginInfoProps`：生成 GUID、名称和版本常量；
    - `BepInEx.AssemblyPublicizer.MSBuild`：见 §5；
    - `Microsoft.Unity.Analyzers`：见 §10。
  - 本机 HintPath，一律设 `Private=false`：
    - 游戏 `REPO_Data\Managed` 下的 `Assembly-CSharp.dll`、`UnityEngine*.dll`、Photon 等；
    - profile 中依赖 mod 的 DLL，只从 `<Team>-<Package>` 目录解析（§14）。
- 引用游戏 DLL 只是为了编译和类型检查。不要把它们放进仓库或发布包。
- 开发 profile 与日常游玩 profile 分开；多人兼容测试另建一个最小 profile，免得把其他 mod 的副作用误判成自己的问题。

## 3. 定位游戏内部方法

用 ILSpy 或 dnSpyEx 打开 `REPO_Data\Managed\Assembly-CSharp.dll`（工具安装见 [tools/README.md](../tools/README.md)），按以下顺序查找：

1. 用功能关键词搜索，例如 `Hurt`、`Death`、`ObjectsInCart`、`ChangeLevel`。
2. 找到候选方法后，记录完整签名：类名、方法名、参数名与类型、返回值、依赖的字段。
3. 查看调用者，确认调用时机。
4. 确认**执行端**和**调用频率**：
   - 执行端：只在本地玩家、只在主机，还是所有端；
   - 频率：每帧、每个物理帧、每个物体一次，还是一次性事件。

   例如，某个 hook 在所有端每帧都被调用，但只有主机需要处理。
5. 反编译结果只用来定位，实际行为必须在游戏内用日志或调试器验证。

记录格式建议：

```text
目标方法: PhysGrabCart.ObjectsInCart()
Patch 类型: Prefix + Postfix
调用频率: 每帧 × 每辆车；原版仅在 12 m 内有玩家且计时器到期时才真正扫描（约每 0.5 秒）
执行端: 所有端调用，只有主机处理
依赖字段: objectInCartCheckTimer（private）、itemsInCart（internal）
风险: 计时器语义或扫描条件在游戏更新后可能变化
```

## 4. Harmony Patch 规范

### 4.1 写法

```csharp
[PatchFeature(FeatureId.CartObservation,
    Frequency = "每辆车生成时一次",
    Side = "所有端（登记表是被动的，只有主机侧功能使用）")]
[HarmonyPatch(typeof(PhysGrabCart), nameof(PhysGrabCart.Start))]
internal static class PhysGrabCartStartPatch
{
    private static void Postfix(PhysGrabCart __instance)
    {
        ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
        if (runtime == null)
        {
            return; // 插件未初始化或已卸载：放行原版逻辑
        }

        try
        {
            runtime.Carts.GetOrRegister(__instance);
        }
        catch (Exception ex)
        {
            runtime.ReportFailure(FeatureId.CartObservation, "PhysGrabCart.Start postfix", ex);
        }
    }
}
```

- Patch 只负责捕获游戏事件并转发给自己的服务；业务逻辑不写在 patch 里。
- 转发调用包在 `try/catch` 中。异常只停用对应功能并记录日志，**绝不抛回游戏**。
- 每个 patch 类都要标明所属功能、调用频率和执行端，ShrinkCart 用 `[PatchFeature]`。文档中的 hook 表从这些标注生成。
- 命名用 `<目标类><目标方法>Patch`，例如 `PhysGrabInCartAddPatch`、`RunManagerChangeLevelPatch`。
- 选择补丁类型的顺序：
  1. **Postfix**：优先使用，对原逻辑的影响最小。
  2. **Prefix**：需要提前记录状态、拦截前置条件或跳过原方法时使用。
  3. **Transpiler**：Prefix / Postfix 无法实现时才用，并写清楚原因（§13.2）。
  4. **RuntimeDetour / Preloader patch**：只用于极少数必须提前处理程序集或底层调用的场景。

### 4.2 调用频率与性能

- 挂在每帧、每个物理帧或每个物体上的 hook，必须先做 **O(1) 早退**：功能未激活、没有相关对象时，立刻返回。
- 只在状态**变化**时做重活。例如，每帧都会调用的扫描方法，只在原版真正完成扫描后才处理结果。
- 组件查找、名称清洗、分类这类开销，按实例缓存，并明确失效点（§9.3）。
- 高频路径上禁止：反射 `GetValue` / `Invoke`、LINQ、字符串拼接、日志输出，以及每次都做 `GetComponent` 链。

### 4.3 Harmony 语义陷阱

- **Prefix 返回 `false` 时，Harmony 仍会执行 Postfix。** 需要区分"被拦截"的调用时，Prefix 用 `out bool __state` 记录，Postfix 读取 `bool __state`。
- 补丁方法的普通参数**按名称**绑定到原方法参数（例如 `_physGrabObject`）。游戏改了参数名，patch 会在安装时失败，所以这一点要纳入构建期校验。
- 目标方法有重载时，必须在 `[HarmonyPatch]` 中写明参数类型；否则 Harmony 会报 ambiguous match。
- **按类安装补丁**，逐个类调用 `harmony.CreateClassProcessor(type).Patch()` 并各自捕获异常，某个类失败只降级对应功能。不要用 `PatchAll` 那种全有或全无的方式。

## 5. 访问非公开成员（访问阶梯）

R.E.P.O. 的关键字段大多是 `internal` 或 `private`，"默认禁止反射"并不现实。按下面的阶梯选择访问方式：

1. **public 成员**：直接用强类型访问。
2. **非公开成员**：用本机 publicize 的引用（`BepInEx.AssemblyPublicizer.MSBuild`，对 `Assembly-CSharp` 设 `Publicize="true"`；公开化的副本只在 `obj/`，不入库），做强类型访问。要求：
   - 所有非公开访问集中在**一个访问类**里（ShrinkCart 中是 `GameAccess`），每个方法只包装一个成员访问；
   - 每个方法声明它访问的成员和所属功能（ShrinkCart 用 `[GameMember]`），并标记 `[MethodImpl(MethodImplOptions.NoInlining)]`，让成员缺失时的 JIT 异常只发生在调用处；
   - 启动时按声明做一次**自检**：成员缺失或类型变化时，只停用对应功能并打 Warning；
   - **构建期校验**以原版（未 publicize 的）程序集为准，确认非公开访问只出现在访问类中，且与声明一致。
3. **字符串反射**（`AccessTools.Field`、`GetMethod` 等）：只用于兼容多个游戏版本的 fallback。要求：
   - 只在初始化时查找一次并缓存；
   - 失败时安全降级，并记录目标版本、目标成员和原因；
   - 不在 `Update()`、物理回调或网络事件中反复查找或调用。

可以接受的反射：枚举解析、JSON 序列化、启动时的自检。不可以接受的反射：散落在各个文件里的 `FieldInfo`，以及高频路径上的 `GetValue` / `Invoke`。

## 6. 网络角色与联机安全

每个功能都要先确定自己的网络角色：

| 角色 | 典型例子 | 规则 |
|---|---|---|
| **本地型** | 本地视觉效果、本地 UI、外部设备联动 | 只处理本地玩家的事件（见下方示例）；不修改任何网络状态；不把本地效果同步给全房间 |
| **主机权威型** | 缩放决策、给敌人造成伤害、改变物品计数 | 见下方的主机权威型规则 |
| **全员同步型** | 需要每个客户端都安装才能正确显示的内容（例如给物体挂接收同步的组件） | 所有端运行，但不在客户端做决策；README 说明未安装的客户端会看到什么 |

**主机权威型的规则**

- 用 `SemiFunc.IsMasterClientOrSingleplayer()` 判定。判定抛出异常时按"非主机"处理（fail-closed）；判定结果可以做短时间缓存。
- 只能通过游戏或依赖库的同步通道修改状态，例如 ScalerCore 的 RPC、`EnemyHealth.Hurt`。不要自己广播状态。
- 失去主机权限时只丢弃本地状态，不尝试恢复，因为客户端无法派发同步。
- 会影响其他玩家的功能必须可以关闭，并在 README 中写明默认状态与安装要求（主机必装、客户端是否需要）。

本地型的本地玩家过滤示例：

```csharp
internal static bool IsLocalAvatar(PlayerAvatar avatar)
{
    if (avatar == null)
    {
        return false;
    }

    if (object.ReferenceEquals(avatar, PlayerAvatar.instance))
    {
        return true;
    }

    try
    {
        return avatar.photonView != null && avatar.photonView.IsMine;
    }
    catch
    {
        return false;
    }
}
```

所有角色都不要：

- 篡改 Photon ownership；
- 在公共房间测试不稳定的功能。

推荐的测试顺序：

1. 单机；
2. 私人房间，只有自己；
3. 私人房间，多人都装同一版本；
4. 私人房间，混合安装：只有主机装，或者客户端没装；
5. 确认不影响联机状态后，再考虑发布。

## 7. 配置

### 7.1 必须做到

- 改变玩法、影响安全或兼容性的行为必须**可以关闭**，并提供一个**总开关**。
- 每个配置项都必须真实生效，并有测试或验证覆盖。不要保留"写了但不起作用"的配置。
- 配置描述写的是实际行为。实现改变时，描述要同步修改。
- 读取配置时再次夹紧范围，不要信任文件里的值。

### 7.2 默认值

- **"默认保守"只约束安全、硬件、网络和外部联动类功能**，这些功能默认关闭或取最保守的值。
- 玩法类改动可以默认开启，但要在 README 中显著标注，并且可以关闭。例如"敌人进车秒杀"默认开启。
- **紧急停止**只要求外部联动类 mod（硬件、外部程序、网络服务）提供；普通玩法 mod 有总开关就够了。
- 如果 mod 自带配置面板，面板应显示当前启用状态、关键计数和最近一次触发或拦截的原因。

### 7.3 命名

- REPOConfig 只显示 BepInEx 的 section 和 key 名，没有显示名机制，所以**允许本地化键名**。但必须满足：
  - 每个配置项有一个稳定的、与语言无关的内部 ID，迁移、测试和文档都使用这个 ID；
  - 维护一张双语映射表，也就是声明式的配置 schema；
  - 切换语言时值不能丢：读取时当前语言的键优先，另一种语言的键兜底。
- **键名是公共接口**：改名、删除或修改刻度都必须走版本化迁移（§8）。
- 滑条会产生浮点误差（例如 `0.3999999`），数值在读取和写入时都要规整到固定精度。

## 8. 配置迁移

- schema 版本号存放在 cfg 文件内，作为一个隐藏条目（对 REPOConfig 加 `HideFromREPOConfig` 标签）。不要用 cfg 之外的 marker 文件记录迁移状态。
- 插件 `Awake` 时，文件里的值都还在孤儿条目中。**不要在 `Bind` 之前用 `TryGetEntry` 读取旧值**，它读不到。应当用公开 API 读取原始值，例如用哨兵默认值 `Bind<string>` 读出原文，再 `Remove`。
- 在第一次 `Bind` 之前记录 cfg 文件是否存在，用来区分"首次安装"和"升级"。首次安装直接写入最新版本，不执行一次性迁移。
- 迁移期间关闭 `SaveOnConfigSet`，最后只保存一次。
- 一次性迁移之前，先备份原 cfg 文件。备份已存在时不覆盖；备份失败只记录日志，不中断启动。
- "新键不存在时才继承旧键"这类幂等清理，每次启动都可以执行。改刻度这类一次性步骤，只能按版本号执行一次。
- 每个迁移都要有真实历史版本的 cfg 样本（fixture）和自动化测试，覆盖首次安装、升级、重复运行三种情况。
- 一次性迁移要在 CHANGELOG 中公告：影响了什么、之后不会再发生、备份在哪里。
- **修改默认值时，老玩家不会跟着变**：BepInEx 只在配置项不存在时写入默认值，已写入 cfg 的值不随新版本的默认值变化。需要统一时，写一次性迁移，只升级仍等于某个历史默认值的值；玩家改过的值和开关类不动。
- 发布前用历史版本源码扫描所有用过的键名，确认每个旧键都被继承或清理，不留下永远不生效的残留分组。

## 9. 状态所有权、生命周期与依赖库全局状态

### 9.1 所有权

- 只恢复自己施加的状态。每次施加时**显式记录所有权**（目标、种类、参数），恢复前核对状态仍与记录一致；不一致就放弃所有权，不要去恢复。
- 不要用"参数长得像我设置的"来推断所有权，那样会误恢复别的 mod 或游戏机制施加的状态。

### 9.2 生命周期

- 生命周期要有**统一入口**，由一个编排者分发：获得或失去主机权限、开关切换、换关、卸载。不要让各个功能各自订阅这些事件。
- 为不同的停用原因定义不同语义：
  - 配置关闭：恢复；
  - 失去权限：只丢弃；
  - 依赖失效：尽力恢复；
  - 卸载：恢复。
- 换关前恢复并清空本关状态。
- 功能是否激活的判断要集中在一处，例如：是主机 && 总开关 && 功能可用 && 功能开关。

### 9.3 缓存

每个缓存都要写明失效点：换关、配置版本变化、对象销毁或定期清理。按 Unity 实例 ID 缓存时，要考虑对象被销毁或复用（例如敌人实例会重生）。

### 9.4 依赖库的全局状态

不要长期改写依赖库的全局静态开关，这会影响其他 mod 和依赖库自己的功能。确实需要时，只在最小作用域内临时修改，并在 `finally` 中还原：

```csharp
bool previous = ScaleManager.AllowDeadHeads;
try
{
    ScaleManager.AllowDeadHeads = true;
    return ScaleManager.ApplyIfNotScaled(target, options);
}
finally
{
    ScaleManager.AllowDeadHeads = previous;
}
```

前提是已经核实依赖库在这次调用内同步读取该开关，并把这个前提写进接口文档，依赖升级时重新核对。

## 10. Unity 空值规则

- `UnityEngine.Object` 重载了 `==`：已销毁的对象与 `null` 比较结果为 `true`。`?.`、`??`、`is null`、`is not null` 都绕过了这个重载，会把已销毁的对象当成有效对象。
- **禁止对 `UnityEngine.Object` 使用 `?.`、`??`、`is null`。** 用 `== null` / `!= null` 判断。
- 启用 `Microsoft.Unity.Analyzers`，并在 `.editorconfig` 中把 `UNT0007`、`UNT0008`、`UNT0029` 设为 `error`。
- 缓存 Unity 对象引用时，每次使用前都用 `== null` 判断是否已销毁。
- 项目不启用可空引用类型分析（`Nullable disable`），因为它不理解 Unity 的"假 null"语义。

## 11. 日志与可观测性

日志要能回答三个问题：

- mod 是否加载成功；
- 各个 hook 和功能是否生效；
- 事件为什么被触发，或者为什么被拦截。

具体要求：

- 启动时输出**一条摘要**：补丁安装数量、自检结果、不可用的功能及原因、配置语言等关键状态。
- 日志级别：
  - `Info`：启动摘要、配置迁移、关键状态变化；
  - `Warning`：成员缺失、patch 安装失败、依赖调用失败、配置不合法；
  - `Error`：功能因运行期异常被停用，附堆栈；
  - `Debug`：受配置开关控制，默认关闭。
- 同一个问题的重复告警只打一次，按功能和上下文去重。
- 高频路径不打日志。需要观察热路径时，用计数器，在调试开关打开时周期性输出汇总。
- 测试结束先保存日志：Unity 只保留最近两次会话的 `Player.log`，BepInEx 的 `LogOutput.log` 只保留当前一次（可在 `BepInEx.cfg` 设置 `[Logging.Disk] AppendLog = true`）。

## 12. 构建与发布

### 12.1 Thunderstore 包结构

ZIP 根目录直接放以下文件，不能先套一层文件夹。文件名**大小写敏感**：

```text
manifest.json     必需
README.md         必需
icon.png          必需，精确 256x256 的 PNG
CHANGELOG.md      可选，建议提供
YourMod.dll       插件 DLL 放在根目录
```

- r2modman 等 mod 管理器会把根目录的 DLL 安装到 `BepInEx/plugins/<Team>-<Package>/`。不要在 ZIP 中自建 `BepInEx/plugins/<Mod>/` 嵌套目录，部分 mod 管理器无法识别这种布局（ShrinkCart 0.2.47 修复过这个问题）。
- `README.md` 必须能按 UTF-8 解码。上传前用 Thunderstore 的 Markdown Preview 检查渲染效果。
- 官方文档给出的软包体积上限约为 `5,242,880,000` bytes，但发布物仍应尽量精简。

### 12.2 manifest.json 与版本号

- 必需字段：`name`、`description`、`version_number`、`dependencies`、`website_url`。没有网站时也要保留 `"website_url": ""`。
  - `name`：只允许 `a-z A-Z 0-9 _`，长度 1–128。
  - `description`：最长 250 个字符。
  - `version_number`：三段式 `Major.Minor.Patch`，例如 `0.3.1`。
  - `dependencies`：每项格式为 `{team}-{package}-{Major.Minor.Patch}`。
- **版本号只有一个来源：csproj 的 `<Version>`。** 插件 `[BepInPlugin]` 的版本由 `BepInEx.PluginInfoProps` 从它生成。`manifest.json` 的 `version_number` 和 CHANGELOG 顶部条目由构建期校验保证一致。
- 每次更新包都必须递增版本号，Thunderstore 展示版本号最高的那一版。
- 插件 GUID 决定 cfg 文件名，发布后不要修改。

### 12.3 构建期自动校验

校验必须**从编译产物推导**，而不是手写清单；手写清单会过时，而且往往只比较参数个数。至少覆盖：

1. 每个 Harmony 目标在原版程序集中存在且唯一，补丁参数能按名称注入，每个 patch 类都标注了所属功能。
2. 对原版非公开成员的访问只出现在访问类中，并与声明一致。
3. 分层依赖方向正确（例如纯逻辑层不引用 Unity）。
4. profile 中依赖的版本不低于 manifest 声明的版本，并打印实际使用的名称、版本和路径。
5. csproj、manifest、CHANGELOG 和 DLL 中的版本号一致。
6. ZIP 根目录的文件清单正确，内容与源文件和编译产物一致。
7. 由工具生成的文档是最新的。

ShrinkCart 的实现是 `tools/ShrinkCart.BuildTools validate`，见 [MAINTENANCE.md §3.1](MAINTENANCE.md#31-validate-检查什么)。

### 12.4 禁止进入发布包的内容

- `Assembly-CSharp.dll`、Unity 或游戏的原始 DLL，以及 publicize 后的副本；
- ripped 游戏资源、patched Unity 工程或 patched 游戏程序集；
- 反编译导出的游戏源码；
- 本地用户配置、日志、token。

## 13. 反编译与 IL 工具链

仓库脚本和完整命令见 [tools/README.md](../tools/README.md)。工具安装到 `%LOCALAPPDATA%\Programs`，不把第三方二进制提交到仓库。

### 13.1 工具选择

- **ILSpy**：Windows 首选。可以查看 C#、IL、引用、调用关系和程序集元数据。
- **dnSpyEx**：原版 dnSpy 已停止维护，使用维护分支 dnSpyEx。适合浏览 IL/C#，以及需要调试器的工作流。
- **AvaloniaILSpy**：ILSpy 的跨平台界面，主要用于 macOS/Linux；Windows 上通常不需要和原生 ILSpy 重复安装。
- **MonoMod**：用于 `DebugIL`、`HookGen`、RuntimeDetour 和 IL 辅助分析。它不是 Java 工具，不需要 Java。独立 release 版本较旧，新增运行时依赖时要锁定并测试 NuGet 版本，不能不经审查就升级。

### 13.2 Harmony Transpiler 的 IL 分析流程

只有 Prefix / Postfix 无法实现需求时才写 Transpiler：

1. 在 ILSpy / dnSpyEx 中定位完整方法签名，切换到 IL 视图。
2. 记录目标逻辑前后的 opcode、operand、分支和局部变量用途。
3. 用语义锚点匹配，例如稳定的方法调用或字段访问。不要依赖固定的 IL offset、局部变量编号或很短的单一指令序列。
4. 明确预期的匹配次数。次数不符时记录警告并安全放弃这个 patch，不要生成损坏的 IL。
5. 检查堆栈平衡、标签、异常处理块和所有控制流分支。
6. 游戏更新后重新比较 IL，然后运行构建期校验和游戏内冒烟测试。

反编译出的 C# 只用来帮助理解语义，Transpiler 以实际 IL 为准。MonoMod / DebugIL 只对用户本地的临时副本操作，不要原地修改 Steam 游戏目录中的程序集。

## 14. 本地 mod 管理器 profile

- 开发时使用哪个 profile，由本地 props 指定（例如 `R2ProfileDir`），不要在脚本里写死 profile 名。不同 profile 中的依赖版本可能不同（旧 profile 里可能是很老的 ScalerCore）。
- 依赖**只从目标 profile 的 `BepInEx/plugins/<Team>-<Package>/` 解析**。**不要**递归搜索，也**不要**回退到 r2modman 的共享 `cache`：cache 会保留旧版本和手动导入的包，看到文件不代表该 profile 启用了它。
- profile 的 `mods.yml` 是已安装版本和启用状态的主要依据。构建时打印实际使用的依赖名称、版本和路径。
- 只读盘点 profile 可以用 [`tools/Get-LocalREPOProfileInventory.ps1`](../tools/Get-LocalREPOProfileInventory.ps1)。
- 不提交 `mods.yml`、profile 导出、配置、日志、缓存清单或本地绝对路径。文档中不写带日期的本机核对结论，版本要求以 manifest 和构建期校验为准。

## 15. 游戏更新后的维护流程

1. 启动游戏，确认 BepInEx 正常加载。
2. 对新的 `Assembly-CSharp.dll` 重新编译：
   - 被改名或删除的成员会直接变成**编译错误**，这正是强类型访问的好处；
   - 构建期校验会报告 patch 目标缺失、出现重载、参数改名、公开成员变成非公开等问题。
3. 按报错修复。只有行为发生变化时（调用时机、执行端），才需要用 ILSpy 人工核对，并更新 hook 的频率和执行端标注。
4. 跑测试矩阵（§16），至少覆盖受影响的功能和冒烟项。
5. 更新版本号、CHANGELOG 和 README，然后重新打包。

## 16. 测试矩阵与质量门槛

**自动化**，每次构建都要全部通过：

- 编译无错误。
- 单元测试通过：纯逻辑层（状态机、判定、节奏）和配置迁移（历史版本样本）。
- 构建期校验全部通过（§12.3），包括 ZIP 布局检查。

**游戏内测试**，按网络角色选择环境：

| 环境 | 本地型 | 主机权威型 | 全员同步型 |
|---|---|---|---|
| 单机 | ✓ | ✓ | ✓ |
| 私人房间，全员安装 | ✓（非本地玩家不会误触发） | ✓（客户端看到同步结果） | ✓ |
| 私人房间，只有主机安装 | — | ✓（未安装的客户端不出错） | ✓（README 中声明的差异符合预期） |
| 私人房间，只有客户端安装 | ✓ | ✓（客户端不做任何决策） | — |

每次发布前的最低门槛：

- 启动日志显示插件已加载，补丁全部安装，没有不可用的功能；
- 核心功能正常触发；
- 总开关关闭后功能停止，已施加的状态被恢复；
- 换关正常；
- 配置从上一个发布版本升级后值正确；
- 异常、断线、配置错误都不会导致游戏崩溃；
- 发布包不包含游戏专有文件。

## 17. 推荐开发流程

```text
确认需求与网络角色
-> 反编译定位目标方法（签名、执行端、调用频率）
-> 写最小的 Harmony patch，只转发
-> 在自己的服务 / 功能中实现逻辑（可降级、可关闭、只恢复自己的状态）
-> 非公开成员走访问类；配置项走 schema；需要迁移的写迁移和样本测试
-> 编译 + 单元测试 + 构建期校验
-> 游戏内验证（单机 → 私人多人 → 混合安装）
-> 更新 CHANGELOG / README / 版本号
-> 打包发布
```

核心思想：hook 要小而准；逻辑要集中、可配置、可诊断、可停止；把"是否仍然有效"交给编译器和构建期校验去回答。

## 附录

- [附录 A：Thunderstore API 使用准则](appendix/Thunderstore_API.md)
- [附录 B：GTFO 的 BepInExPack 只能作为跨游戏对照](appendix/GTFO_BepInExPack_Reference.md)
