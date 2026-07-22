# R.E.P.O. Mod 开发通用规范

作者：AngelcoMilk

本文是一份面向 R.E.P.O. 的 BepInEx / Harmony Mod 开发规范，目标是让项目可维护、可验证、少踩版权与联机风险，并尽量使用强类型引用，避免不必要的反射。

## 1. 基本原则

- 只发布自己的代码、配置、图标和自制资产，不发布游戏原始 DLL、反编译源码、ripped 资源或 patched Unity 工程。
- 优先使用 BepInEx + Harmony 做运行时补丁；不要直接修改游戏原始 DLL。
- 优先引用本地游戏程序集并使用强类型写法，例如 `typeof(PlayerHealth)`、`PlayerAvatar.instance`。
- 不必要不使用反射。能通过本地程序集引用、公开字段、公开方法和 Harmony 参数拿到的数据，就不要用 `AccessTools`、`GetField`、`GetMethod`、`Invoke`。
- 每次游戏更新后重新核对目标方法签名、字段名和行为，不假设旧版本 hook 永久有效。
- 联机相关逻辑只影响本地客户端，不修改游戏网络状态，不向其他玩家强制同步非必要行为。

## 2. 环境与引用

推荐把开发 profile 与日常游玩 profile 分开，避免测试插件污染正常游玩环境。

常用本地路径：

```text
游戏目录:
C:\Program Files (x86)\Steam\steamapps\common\REPO
或
D:\SteamLibrary\steamapps\common\REPO

游戏程序集:
REPO_Data\Managed\Assembly-CSharp.dll

r2modman profile:
%APPDATA%\r2modmanPlus-local\REPO\profiles\<ProfileName>

BepInEx 核心:
%APPDATA%\r2modmanPlus-local\REPO\profiles\<ProfileName>\BepInEx\core
```

常用编译引用：

- `Assembly-CSharp.dll`：游戏核心类型。
- `UnityEngine.dll`、`UnityEngine.CoreModule.dll`、`UnityEngine.IMGUIModule.dll` 等：Unity API。
- `BepInEx.dll`：插件入口与配置。
- `0Harmony.dll`：Harmony patch。
- 其他游戏实际需要的托管 DLL，例如 Photon、websocket-sharp、Newtonsoft.Json。

引用这些 DLL 是为了编译和类型检查，不要把游戏 DLL 放进公开仓库或发布包。

## 3. 如何获取内部方法

先用 ILSpy 或 dnSpyEx 打开：

```text
REPO_Data\Managed\Assembly-CSharp.dll
```

查找内部方法时按这个顺序做：

1. 先从功能关键词搜索，例如 `Hurt`、`Death`、`Footstep`、`Jump`、`Land`、`Slide`。
2. 找到候选类后，记录完整信息：类名、方法名、参数、返回值、字段依赖。
3. 查看调用者，确认方法是否只在本地玩家触发，还是所有玩家/敌人都会触发。
4. 反编译结果只作为定位依据，实际 patch 必须在游戏内用日志或调试验证。
5. 对多人游戏事件，必须确认本地玩家判断，例如 `PlayerAvatar.instance`、`photonView.IsMine`。

记录格式建议：

```text
目标事件: 玩家受击
目标类: PlayerHealth
目标方法: Hurt(int damage)
Patch 类型: Postfix
本地判断: PlayerAvatar.instance.playerHealth == __instance
风险: 游戏更新后参数名或调用时机可能变化
```

## 4. Harmony Patch 规范

优先使用强类型类引用：

```csharp
[HarmonyPatch(typeof(PlayerHealth), "Hurt")]
internal static class PlayerHealthHurtPatch
{
    private static void Postfix(PlayerHealth __instance, int damage)
    {
        if (!RepoGuards.IsLocalHealth(__instance))
        {
            return;
        }

        Plugin.Instance.Stim.TriggerHurt(damage);
    }
}
```

推荐顺序：

- Postfix：优先使用，最不容易破坏原游戏逻辑。
- Prefix：只在需要提前记录状态或拦截前置条件时使用。
- Transpiler：只在 Prefix/Postfix 无法满足时使用，并且必须写清楚原因。
- RuntimeDetour / Preloader patch：只用于极少数必须提前处理程序集或底层调用的场景。

Patch 命名建议：

```text
<目标类><目标方法>Patch
PlayerHealthHurtPatch
PlayerAvatarDeathPatch
PlayerAvatarVisualsFootstepLightPatch
```

不要在 patch 中写大量业务逻辑。Patch 应该只负责捕获游戏事件，然后转发给自己的控制器：

```csharp
stim.TriggerFootstepFromVisuals("Light");
```

## 5. 反射使用边界

默认禁止在核心逻辑里大量使用反射。

可以接受的情况：

- 配置 UI 枚举解析：`Enum.Parse`、`Enum.GetValues`。
- JSON 导入导出。
- 兼容不同游戏版本的 optional hook，但必须集中封装，并有日志说明。

需要谨慎的情况：

- `AccessTools.Field`
- `AccessTools.Method`
- `BindingFlags.NonPublic`
- `GetField` / `GetMethod`
- `MethodInfo.Invoke`
- 通过字符串访问私有字段并频繁读取

如果确实必须使用反射，遵守这些规则：

- 只在启动或初始化时查找一次，缓存结果。
- 失败时安全降级，不阻塞游戏启动。
- 写清楚目标版本、目标字段/方法、为什么不能强类型引用。
- 不在 `Update()`、高频脚步事件、网络事件中反复查找。

示例：

```csharp
// 仅作为版本兼容 fallback，不作为主路径。
private static readonly FieldInfo SomeField =
    AccessTools.Field(typeof(SomeGameType), "somePrivateField");
```

## 6. 本地玩家与联机安全

所有玩家事件默认必须做本地过滤：

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

不要做这些事：

- 不要修改其他玩家状态。
- 不要篡改 Photon ownership。
- 不要把本地娱乐效果同步给全房间。
- 不要在公共房间测试不稳定功能。

推荐测试顺序：

1. 单机或训练环境。
2. 私人房间，只有自己。
3. 私人房间，多人同版本。
4. 确认不影响联机状态后再考虑发布。

## 7. 配置与面板

所有可能影响体验或安全的参数都应该可配置：

- 启用/禁用总开关。
- 事件开关。
- 强度上限。
- 持续时间。
- 冷却时间。
- 键位。
- 调试日志。
- 紧急停止键。

默认值应保守。尤其是体感、联动、音频、网络、自动触发类 Mod，应默认提供紧急停止。

配置命名建议：

```text
Safety.Enabled
Safety.EmergencyStopKey
Safety.MaxIntensity
Events.HurtEnabled
Events.DeathEnabled
Events.FootstepEnabled
Connection.Port
Diagnostics.VerboseLogging
```

UI 面板应显示：

- 当前启用状态。
- 当前连接状态。
- 当前 profile / preset。
- 关键事件计数。
- 最近一次触发原因。
- 最近一次拦截原因。
- 紧急停止按钮。

## 8. 日志规范

日志要能回答三个问题：

- Mod 是否加载成功。
- Patch 是否生效。
- 事件为什么触发或为什么被拦截。

推荐日志级别：

- `Info`：启动、绑定、profile 切换、关键状态变化。
- `Warning`：目标方法缺失、配置不合法、连接失败。
- `Error`：不可恢复异常。
- `Debug`：高频事件计数，不默认开启。

不要在每帧或每个脚步都默认刷大量日志。

## 9. 构建与发布

### 9.1 Thunderstore 官方包结构

ZIP 根目录必须直接包含以下三个**大小写敏感**的文件名，不能先套一层文件夹：

```text
manifest.json
README.md
icon.png
BepInEx/plugins/YourMod/YourMod.dll
BepInEx/plugins/YourMod/YourDependency.dll
```

`CHANGELOG.md` 可选。官方当前文档给出的软包体积上限约为 `5,242,880,000` bytes，但发布物仍应保持最小化。

强制要求：

- `icon.png` 必须是**精确 256x256** 的 PNG，不只是“建议”。
- `README.md` 必须可按 UTF-8 解码；上传前使用 Thunderstore Markdown Preview 检查渲染。
- ZIP 内的 `icon.png`、`README.md`、`manifest.json` 必须直接位于根目录。
- 文件名大小写敏感；不要写成 `readme.md`、`Manifest.json` 或 `Icon.png`。

### 9.2 manifest.json 约束

必需字段为 `name`、`description`、`version_number`、`dependencies`、`website_url`。没有网站时也保留 `"website_url": ""`。

- `name`：只允许 `a-z A-Z 0-9 _`，长度 1–128。
- `description`：最长 250 字符。
- `version_number`：必须是三段 `Major.Minor.Patch`，例如 `0.2.46`。
- `dependencies`：每项格式为 `{team name}-{package name}-{package version}`，依赖版本同样使用三段版本号。
- 更新包时必须递增版本；Thunderstore 展示最高版本号。

本仓库可运行：

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Test-DevelopmentEnvironment.ps1
```

验证 manifest、UTF-8 README、256x256 PNG 和本地开发依赖。打包后还应列出 ZIP 条目，人工确认没有额外顶层目录。

### 9.3 禁止进入发布包的内容

不要把这些内容放进发布包：

- `Assembly-CSharp.dll`
- Unity 游戏原始 DLL
- ripped 游戏资源
- patched Unity 工程或 patched 游戏程序集
- 反编译导出的游戏源码
- 本地用户配置、日志或 token

## 10. Thunderstore API 使用准则

官方 Swagger UI 位于：

```text
https://thunderstore.io/api/docs/
```

该页面实际读取的机器可读 schema 是：

```text
https://thunderstore.io/api/docs/?format=openapi
```

返回类型为 `application/openapi+json`，当前 schema 是 Swagger/OpenAPI 2.0、标题 `Thunderstore API v1`。官方 schema 自己明确警告：**自动生成且不完全准确**。因此：

- API 集成必须以实际响应、状态码和官方网页行为再次验证，不能只依赖生成 schema。
- 不要猜测 `/swagger.json`、`/openapi.json` 或 `/api/schema/`；这些路径不等于文档 UI 当前使用的 schema。
- schema 顶层声明 Basic Authentication，但这不代表每个读取端点都必须认证；按每个操作的实际行为测试。
- 不要在源码、manifest、日志或 issue 中提交用户名、密码、session、token 或 Authorization header。

### 10.1 接口稳定性分类

当前 schema 主要包含：

- `/api/v1/package/` 与 community-scoped `/c/{community_identifier}/api/v1/package/`：包列表/读取。
- `/api/v1/package-metrics/...`：包与版本指标。
- `/api/v1/current-user/info/`、评分和 bot 管理操作：用户或写操作，按需认证并最小授权。
- `/api/experimental/...`：社区、frontend、package、wiki、submission、validation、usermedia 等实验接口。
- `/api/cyberstorm/...`：Cyberstorm 前端相关接口。

开发准则：

1. 优先使用满足需求的 `/api/v1/` 读取接口。
2. `/api/experimental/` 和 `/api/cyberstorm/` 视为不稳定实现细节；调用方必须容忍字段增删、状态码变化和接口迁移。
3. 发布上传、异步 submission、wiki、评分、审核等写操作不得在构建脚本中默认执行；必须显式启用并保护凭据。
4. 为 HTTP 调用设置超时、清晰的 User-Agent、有限重试和失败降级；尊重 `429` 与服务端错误。
5. 缓存公开元数据，避免在游戏 `Update()` 或高频事件中请求 Thunderstore。
6. 发布前重新下载 schema 并复核所用端点；不要把下载的动态 schema 当作仓库内永久真相。

示例（只读取 schema）：

```powershell
Invoke-RestMethod -Uri "https://thunderstore.io/api/docs/?format=openapi"
```

官方参考：

- API docs: <https://thunderstore.io/api/docs/>
- Creating a Package: <https://wiki.thunderstore.io/mods/creating-a-package>

## 11. Unity/.NET 反编译与 IL 工具链

仓库脚本和完整命令见 [`tools/README.md`](../tools/README.md)。工具安装到 `%LOCALAPPDATA%\Programs`，不把第三方二进制提交到仓库。

### 11.1 工具选择

- **ILSpy**：Windows 首选；查看 C#、IL、引用、调用关系和程序集元数据。
- **dnSpyEx**：传统 dnSpy 已停止维护，使用维护分支 dnSpyEx；适合 IL/C# 浏览和需要调试器工作流的场景。
- **AvaloniaILSpy**：ILSpy 的跨平台 UI 备选，主要用于 macOS/Linux；Windows 通常无需与原生 ILSpy 重复安装。
- **MonoMod**：用于 `DebugIL`、`HookGen`、RuntimeDetour/IL 辅助分析。它不是 Java 工具，不要求 Java。独立 release 较旧，新增运行时依赖时应锁定并测试 NuGet 版本，不能无审查升级。

Windows 安装：

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1
# 可选：
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1 -InstallDnSpyEx
```

### 11.2 Harmony Transpiler 的 IL 分析流程

只有 Prefix/Postfix 无法实现需求时才写 Transpiler：

1. 在 ILSpy/dnSpyEx 中定位完整方法签名并切换到 IL 视图。
2. 记录目标逻辑前后的 opcode、operand、分支和局部变量用途。
3. 使用语义锚点匹配，例如稳定的方法调用或字段访问；不要依赖固定 IL offset、局部变量编号或单一短指令序列。
4. 明确预期匹配次数；匹配数量不符时记录警告并安全放弃 patch，而不是产生损坏 IL。
5. 检查堆栈平衡、标签、异常处理块和所有控制流分支。
6. 游戏更新后重新比较 IL，并运行 `build.ps1` 中的 Cecil hook 校验及游戏内 smoke test。

反编译 C# 只帮助理解语义，Transpiler 的事实来源是实际 IL。MonoMod/DebugIL 只对用户本地的临时副本操作，不原地修改 Steam 游戏程序集。

### 11.3 GTFO 的 BepInExPack 只能作为跨游戏对照

已核对 Thunderstore 的 GTFO 包页和 `BepInEx-BepInExPack_GTFO-3.2.2` 下载包。它不是通用 BepInExPack 的同名版本，也不是 R.E.P.O. 可替换依赖：

- 最新包版本为 `3.2.2`，manifest 无额外 Thunderstore dependencies；README 标明基础为 **BepInEx 6.0.0-be.665**。
- GTFO 是 IL2CPP 工作流；包中包含 `BepInEx.Unity.IL2CPP.dll`、Il2CppInterop/Cpp2IL、随包 CoreCLR/.NET 运行时，以及 `GTFO-API.dll` 0.5.0。
- 包内容位于 `BepInExPack_GTFO/` 子目录，README 的手动安装步骤明确要求先解压，再把该目录的内容复制到 GTFO 游戏根目录。
- Thunderstore 发布元数据仍然位于 ZIP 根：`manifest.json`、`README.md`、`icon.png`。因此“发布元数据必须在 ZIP 根”与“payload 可按安装器约定放在子目录”可以同时成立。
- 该包使用 bleeding-edge BepInEx 6，并明确提示不保证稳定；不能把其程序集、GTFO-API、CoreCLR payload、IL2CPP hook 方法或编译目标照搬到 R.E.P.O.

ShrinkCart 当前目标是 R.E.P.O. 的 Mono/BepInEx 5 配置，manifest 应继续依赖社区对应的 `BepInEx-BepInExPack-5.4.2305`。开发和构建引用必须来自 R.E.P.O. profile 与 R.E.P.O. 的 `Assembly-CSharp.dll`；不同社区中团队名相同不表示二进制兼容。

可复用的是方法而不是文件：锁定框架版本、记录上游 build、提供游戏专用默认配置、说明手动安装的目录边界、在 changelog 中标出 breaking runtime transitions，并保持游戏 API 适配层与通用插件代码分离。

官方/一手来源：

- GTFO package page: <https://thunderstore.io/c/gtfo/p/BepInEx/BepInExPack_GTFO/>
- Thunderstore package API metadata: <https://thunderstore.io/api/experimental/package/BepInEx/BepInExPack_GTFO/>
- Package manifest/README: 来自该页面指向的官方 `3.2.2` 下载包，仅在本地临时目录检查，未提交二进制。

## 12. 本地 mod 管理器档案检查

本机可能同时安装 r2modman 与 Thunderstore Mod Manager，但 ShrinkCart 当前构建路径使用 r2modman 的：

```text
%APPDATA%\r2modmanPlus-local\REPO
```

只读盘点命令见 [`tools/Get-LocalREPOProfileInventory.ps1`](../tools/Get-LocalREPOProfileInventory.ps1)：

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Get-LocalREPOProfileInventory.ps1 `
  -ProfileName "REPO" -IncludeDisabled
```

判断规则：

1. 以每个 profile 的 `mods.yml` 为已记录版本和 enabled 状态的主要来源。
2. `cache` 是多 profile 共用缓存，会保留旧版本和手动导入包；看到 DLL/manifest 不等于当前 profile 已启用。
3. 如需确认部署结果，再只读核对该 profile 的 `BepInEx\plugins`、`patchers` 与 `core`；不要把这些文件复制进仓库。
4. 不提交 `mods.yml`、profile/export、配置、日志、缓存清单或本地绝对路径。文档只保留与依赖兼容性直接相关的脱敏结论。

本次脱敏核对结论：相关开发 profile 记录了 ShrinkCart `0.2.46`、BepInExPack `5.4.2305`、ScalerCore `1.0.4`、REPOConfig `1.2.6`，均启用。它满足 manifest 的 BepInEx `5.4.2305` 和 REPOConfig `1.2.6`。ShrinkCart `0.2.47` 发布适配已将 ScalerCore 最低依赖提升到 `1.0.4`，以采用其针对最新游戏玩家移动调用的缩放修复。

构建脚本必须避免“从共享 cache 按路径倒序取第一个 DLL”所带来的隐式版本选择。优先从目标 profile 的 `BepInEx\plugins` 解析已启用依赖；若显式传入 `-ScalerCoreDll` / `-REPOConfigDll`，应把实际路径和版本打印在构建日志中。多人兼容测试应另建最小 profile，避免把大型日常 mod 集合的副作用误判为 ShrinkCart 问题。

## 13. 游戏更新后的维护流程

游戏更新后按顺序检查：

1. 启动游戏，确认 BepInEx 正常加载。
2. 用 ILSpy/dnSpyEx 重新打开新的 `Assembly-CSharp.dll`。
3. 核对所有 Harmony 目标方法是否还存在。
4. 核对参数数量和类型是否变化。
5. 跑一次最小 smoke test。
6. 测试每个核心事件。
7. 更新版本号和 README。
8. 重新打包。

建议维护一个事件表：

```text
PlayerHealth.Hurt
PlayerAvatar.PlayerDeathRPC
PlayerAvatarVisuals.FootstepLight
PlayerAvatarVisuals.FootstepMedium
PlayerAvatarVisuals.FootstepHeavy
PlayerAvatar.Jump
PlayerAvatar.Land
PlayerAvatar.Slide
PlayerController.Update
```

## 14. 最小质量门槛

提交或发布前至少确认：

- 能编译。
- 能启动游戏。
- BepInEx 日志显示插件加载。
- 面板能打开和关闭。
- 紧急停止可用。
- 本地玩家事件能触发。
- 非本地玩家不会误触发。
- 断线、异常、配置错误不会导致游戏崩溃。
- 发布包不包含游戏专有文件。

## 15. 推荐开发流程

```text
确认需求
-> 反编译定位目标方法
-> 记录签名和本地玩家判断
-> 写最小 Harmony patch
-> 转发到自己的控制器
-> 加配置和安全开关
-> 游戏内验证
-> 私房多人验证
-> 清理日志和 README
-> 打包发布
```

核心思想：先让 hook 小而准，再让业务逻辑可配置、可诊断、可停止。
