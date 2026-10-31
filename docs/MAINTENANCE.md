# ShrinkCart 维护手册

本文面向维护者，覆盖以下内容：环境搭建、日常命令、游戏或依赖更新后的检查流程、发布清单、游戏内测试矩阵和日志排障。
设计说明见 [ARCHITECTURE.md](ARCHITECTURE.md)，外部契约见 [INTERFACES.md](INTERFACES.md)，通用规则见 [REPO_Mod_Development_Guidelines.md](REPO_Mod_Development_Guidelines.md)，开发中踩过的坑见 [LESSONS_LEARNED.md](LESSONS_LEARNED.md)。

## 1. 环境搭建

1. 安装 .NET SDK 10：`winget install Microsoft.DotNet.SDK.10`。仓库根目录的 `global.json` 锁定 SDK 主版本，允许自动使用更新的功能版本。
2. 准备 r2modman 开发 profile，建议与日常游玩的 profile 分开，只装最小依赖集。需要安装：
   - `BepInEx-BepInExPack` 5.4.2305
   - `Vippy-ScalerCore` ≥ 1.1.1
   - `REPOModding-REPOConfig` ≥ 1.2.7（会同时装上 `REPOModding-MenuLib`）
3. 复制 `ShrinkCart.local.props.example` 为 `ShrinkCart.local.props`（已被 git 忽略），填写本机路径：
   - `REPOGameDir`：游戏目录，例如 `D:\SteamLibrary\steamapps\common\REPO`
   - `R2ProfileDir`：开发 profile 目录，例如 `%APPDATA%\r2modmanPlus-local\REPO\profiles\<名称>`
4. 检查环境：

   ```powershell
   dotnet run --project tools/ShrinkCart.BuildTools -- doctor
   ```

   它会检查以下内容，并逐项打印版本和路径：
   - SDK
   - 本机 props
   - 游戏 `Managed` 目录
   - profile 的 BepInEx 核心文件
   - 依赖的安装与启用状态和版本（以 profile 的 `mods.yml` 为准）
   - 打包素材

所有依赖都只从 `R2ProfileDir` 下的 `<Team>-<Package>` 目录解析，不递归搜索，也不回退到 r2modman 的共享 cache。

## 2. 目录结构

```text
ShrinkCart.sln
Directory.Build.props            公共 MSBuild 设置；导入本机 ShrinkCart.local.props
global.json / NuGet.Config       SDK 版本；nuget.org + BepInEx NuGet 源
build.ps1                        构建编排（ASCII only）
src/ShrinkCart/                  mod 本体（netstandard2.1），分层见 ARCHITECTURE.md §4
tests/ShrinkCart.Tests/          单元测试（net472，以源码链接方式编入 Domain/Config）
tests/fixtures/config/           历史版本 cfg 样本（配置迁移测试）
tools/ShrinkCart.BuildTools/     构建期工具：validate / docs / doctor
tools/*.ps1                      分析工具安装、profile 只读盘点
package/                         Thunderstore 素材：manifest.json、README.md、icon.png
docs/                            架构、接口、维护文档与开发规范
```

构建时编译引用的是 publicize 后的 `Assembly-CSharp`，它只生成在 `src/ShrinkCart/obj/`。不要提交它，也不要复制到别处。

## 3. 日常命令

| 目的 | 命令 |
|---|---|
| 完整构建、测试、打包、校验 | `powershell -ExecutionPolicy Bypass -File .\build.ps1` |
| 同时更新生成文档 | `.\build.ps1 -UpdateDocs` |
| 安装到开发 profile | `.\build.ps1 -InstallToProfile`（复制到 `BepInEx\plugins\AngelcoMilk-ShrinkCart\`） |
| 复制 ZIP 到桌面 | `.\build.ps1 -PackageToDesktop` |
| 只编译 | `dotnet build src/ShrinkCart/ShrinkCart.csproj -c Release` |
| 只跑单元测试 | `dotnet test tests/ShrinkCart.Tests/ShrinkCart.Tests.csproj -c Release` |
| 只跑构建期校验 | `dotnet run --project tools/ShrinkCart.BuildTools -- validate [--zip dist/ShrinkCart-<版本>.zip]` |
| 重新生成 Hook / GameMember 表 | `dotnet run --project tools/ShrinkCart.BuildTools -- docs` |
| 检查开发环境 | `dotnet run --project tools/ShrinkCart.BuildTools -- doctor` |

`build.ps1` 的步骤：

1. 编译 mod 和构建工具；
2. 单元测试；
3. 如果带 `-UpdateDocs`，重新生成文档里的生成块；
4. 打包到 `dist/ShrinkCart-<版本>.zip`；
5. `validate --zip`。

任何一步失败都会中止，`-InstallToProfile` 和 `-PackageToDesktop` 只在全部通过后才执行。

`-InstallToProfile` 只是复制文件，r2modman 的界面仍显示之前安装的版本。要让 r2modman 记录新版本，在 r2modman 中导入 ZIP（本地导入 mod）。

### 3.1 `validate` 检查什么

所有检查都从编译产物和原版（未 publicize 的）`Assembly-CSharp` 推导，没有手写清单：

1. **Harmony**
   - patch 类都位于 `ShrinkCart.Patches`，并带 `[PatchFeature]`（含 Frequency 和 Side）；
   - 目标在原版中存在且唯一；
   - 补丁参数能被 Harmony 注入：参数名对应原方法、`__instance` / `__result` / `___字段` 的类型匹配、`__state` 的用法正确。
2. **非公开成员**
   - 对原版非公开成员的 IL 引用只出现在 `GameAccess` 中；
   - `[GameMember]` 声明与 IL 引用完全一致，字段类型一致；
   - 每个访问方法都标记 `NoInlining`。
3. **分层**：依赖只向下；Domain 只用 BCL；Config 不接触 Unity、游戏和 ScalerCore；只有网关调用 `ScaleManager`。
4. **依赖**：`package/manifest.json` 声明的每个依赖都在 profile 中安装、启用，且版本不低于声明版本；ScalerCore 的编译引用来自该 profile。
5. **版本**：csproj 的 `<Version>`、`manifest.json` 的 `version_number`、CHANGELOG 顶部标题、DLL 中 `[BepInPlugin]` 的版本四者一致；插件 GUID 仍为 `AngelcoMilk.ShrinkCart`。
6. **打包素材**：manifest 字段合法，`icon.png` 为 256×256 PNG，README 和 CHANGELOG 是合法 UTF-8，`package/README.md` 与根目录 `README.md` 内容一致。
7. **ZIP**（传入 `--zip` 时）：根目录恰好是 `manifest.json`、`README.md`、`icon.png`、`CHANGELOG.md`、`ShrinkCart.dll` 五个文件，且与源文件、编译产物逐字节一致。
8. **生成文档**：`docs/INTERFACES.md` 的 hooks 与 game-members 块与编译产物一致。config-schema 与 config-migrations 块由单元测试检查。

## 4. 游戏更新流程

1. 更新前先把 `REPO_Data\Managed\Assembly-CSharp.dll` 复制到仓库外的本机目录，留作新旧 IL 对比的基线（`obj/` 里的副本下次构建就会被覆盖）。然后用 Steam 更新游戏，启动一次，确认 BepInEx 能正常加载。
2. 运行 `.\build.ps1`：
   - 编译会引用新的 `Assembly-CSharp`，被改名或删除的字段和方法直接变成**编译错误**，错误位置在 `GameAccess` 或使用公开成员的代码里；
   - `validate` 会报告 patch 目标缺失、出现重载、参数改名，以及原来公开的成员变成非公开等问题。
3. 按报错修复：
   - 目标方法改名：修改 `[HarmonyPatch]`；
   - 出现重载：写明参数类型；
   - 参数改名：修改补丁方法的参数名；
   - 成员变成非公开：把访问移进 `GameAccess` 并声明 `[GameMember]`。
4. 如果只是行为变化（例如调用时机、执行端变了），用 ILSpy / dnSpyEx 核对，然后更新对应 `[PatchFeature]` 的 Frequency 和 Side，并运行 `.\build.ps1 -UpdateDocs`。
5. 按 §7 的测试矩阵做游戏内测试，至少覆盖受影响的功能和冒烟项。
6. 更新版本号（§6）后发布。

## 5. 依赖更新流程

**ScalerCore**

1. 在开发 profile 中升级，运行 `doctor` 确认版本。
2. 编译会暴露 API 层面的变化。
3. 用 ILSpy 重新核对 [INTERFACES.md §3.3](INTERFACES.md#33-shrinkcart-依赖的-scalercore-行为) 列出的行为：
   - `ApplyIfNotScaled` 是否仍同步调用 `DispatchShrink`；
   - `DispatchShrink` 是否仍先判定主机、并且是 `AllowDeadHeads` 的唯一读取者；
   - `DispatchExpand` 是否仍不读取 `AllowDeadHeads`；
   - `Factor` 是否仍只在 `≤ 0` 时被替换；
   - `NativeSync` 是否仍只由 `NativeRespawn.Decide` 读取，派发时读到 `false` 是否仍会清除延迟重生标记；
   - 原生重生的默认值是否已改为开启（`RespawnRules.NativeSyncDefault`）。

   任何一条变化，都要重新评估 `ScalerCoreGateway` 的临时开关与所有权核对。
4. 游戏内重点测试：物品缩小和恢复、死亡头颅、玩家循环（受伤恢复）、代币箱。
5. 如果需要提高最低版本，同时修改 `package/manifest.json` 的依赖、README 的安装说明和 CHANGELOG。

**REPOConfig**：确认 `HideFromREPOConfig` 标签仍被识别（schema 版本条目不应出现在界面中），滑条范围和两位小数显示正常。

**BepInEx**：配置子系统依赖 [ARCHITECTURE.md §6](ARCHITECTURE.md#6-配置子系统) 列出的 BepInEx 行为，升级后运行单元测试；配置迁移测试直接使用 BepInEx 的 `ConfigFile`。

## 6. 发布清单

1. 修改 `src/ShrinkCart/ShrinkCart.csproj` 的 `<Version>`，这是版本号的唯一来源。
2. 同步 `package/manifest.json` 的 `version_number`，并在 `CHANGELOG.md` 顶部新增同版本条目。CHANGELOG 面向玩家书写；一次性迁移必须写明影响和备份位置。
3. 功能或配置有变化时，同时更新 `README.md` 和 `package/README.md`（两者必须一致）。
4. 运行 `.\build.ps1 -UpdateDocs`，再运行一次 `.\build.ps1`，确认全部通过、生成文档没有新的差异。
5. 按 §7 完成游戏内测试。
6. 把 `dist/ShrinkCart-<版本>.zip` 导入一个干净的 profile，确认可以正常加载。
7. 提交、打 tag，再上传 Thunderstore。

## 7. 游戏内测试矩阵

**测试环境**

| 编号 | 环境 |
|---|---|
| E1 | 单机 |
| E2 | 私人房间：主机和客户端都装 ShrinkCart |
| E3 | 私人房间：主机装 ShrinkCart，客户端没装（但装有 ScalerCore） |

**测试场景**

| 场景 | 期望 | 环境 |
|---|---|---|
| 各类贵重物进车、离车 | 进车缩小；离车后约 0.5–1 秒恢复（与 0.2.52 相同）；各分类倍率正确 | E1 E2 E3 |
| 车离所有玩家超过 12 m | 车内物品保持缩小，不被误恢复 | E1 E2 |
| 从车里拿出可装备物品并装备 | 装备前恢复原尺寸 | E1 E2 |
| 关闭「防止碰撞弹回原尺寸」后，把车内贵重物撞到掉价 | ScalerCore 让它弹回原尺寸；冷却结束后如果仍在车内，会再次缩小 | E1 |
| 缩小枪缩小的物品放进车里 | ShrinkCart 不接管；拿出后也不被 ShrinkCart 恢复 | E1 |
| 代币箱、外观箱、钱袋 / Surplus、敌人球 | 按各自倍率缩小并恢复；E3 客户端看不到代币箱缩放（预期行为） | E1 E2 E3 |
| 车、小推车、C.A.R.T. Cannon / Laser 放进另一辆车 | 不缩小；不计入数量和总价；关闭「阻止车类物品计入车内」后按原版计入 | E1 E2 |
| 敌人进车 → 死亡 → 重生后再次进车 | 两次都被秒杀 | E1 E2 |
| 站在车中心 | 循环：缩小 → 原样 → 增大 → 原样；短暂跳起不打断；受伤后恢复，循环照常推进 | E1 E2 |
| 开启死亡头颅功能：头颅进车、离车、进入提取点、复活 | 缩小；以及离车、进入提取点、复活前恢复；调试日志显示 AllowDeadHeads 已还原 | E1 E2 |
| 骑雪地摩托或高尔夫球车进车，下车；抓车内已缩小的同类贵重物；拿出车恢复后再骑 | 有人坐着时不缩小；下车约 1.5 秒后缩小；缩小时和恢复动画中上不了座，能正常拖动；恢复后能骑，车身模型比例正常 | E1 E2 |
| 主机打开 ScalerCore 的 `Native scale respawn`，贵重物滚进车、抓着放进车、拿出车 | 日志没有这些物品的 `[SC] native respawn`；离车正常恢复，没有物品一直保持缩小 | E2 E3 |
| 换关 | 所有缩放恢复，新关卡正常工作 | E1 E2 |
| 在 REPOConfig 中切换总开关和各功能开关 | 关闭时恢复已缩放的对象，重新打开后正常工作 | E1 E2 |
| 切换配置语言并重启 | 所有值保留，键名变为另一种语言 | E1 |
| 分别从 0.2.50、0.2.52 的配置升级，再重启一次 | 首次启动：速度统一为 5 / 2，停在历史默认值的数值升级为当前默认值，生成 `.pre-0.3.1.bak`，日志列出升级项，其他值保留；再次启动：不再改动 | E1 |
| 作为主机开一局后回到主菜单，再以客户端身份加入别人的房间 | 本机不再做任何缩放决策（功能以"失去主机权限"停用，只丢弃状态） | E2 |
| 性能：开启调试日志 | `Counters` 符合 [ARCHITECTURE.md §9](ARCHITECTURE.md#9-hook-频率与开销预算) 的预期 | E1 |

冒烟项（任何发布都要跑）：

- 启动日志有 `ShrinkCart <版本> loaded: patches 7/7, missing game members 0 … All features available.`
- 单机中物品进车缩小、离车恢复；
- 换关正常。

## 8. 日志排障

日志位置：`<profile>\BepInEx\LogOutput.log`。

| 现象 | 查看 |
|---|---|
| 什么都不缩小 | 1. 启动摘要：有没有 `Unavailable features`，补丁数量是否完整。<br/>2. 有没有 `ShrinkCart failed to initialize`（配置异常会导致插件整体停用）。<br/>3. 确认自己是主机，并且总开关和「启用购物车缩小」都已开启 |
| 某个功能失效 | 搜索 `feature <名称> disabled after runtime error`，附带的堆栈会指出出错位置 |
| 更新游戏后功能消失 | 启动摘要中的 `Game member missing` 或 `patch … failed`，然后按 §4 处理 |
| 缩放没有恢复或恢复太慢 | 开启调试日志，看 `Restored …` 和 `Scale was released outside ShrinkCart`；恢复延迟的构成见 ARCHITECTURE.md §7.2 |
| 担心性能 | 开启调试日志，看每 10 秒一次的 `Counters` |
| 配置被重置 | 搜索 `Config migrated to schema`。这条迁移每份配置只执行一次，原文件保存为 `AngelcoMilk.ShrinkCart.cfg.pre-0.3.1.bak` |

## 9. 配置迁移操作

- 新增配置项不需要迁移。
- 改名：在 `ConfigMigrator.LegacyRenames` 中登记。
- 删除：在 `DeprecatedKeys` 中登记。
- 修改刻度或语义：
  1. 提升 `ConfigMigrator.CurrentSchemaVersion`；
  2. 在 `MigrateToCurrent` 中按 `storedVersion < N` 追加一次性步骤；
  3. 如有需要，调整备份文件名。
- 每次迁移都要：
  - 在 `tests/fixtures/config/` 中加入真实的历史 cfg 样本（去除个人信息）；
  - 在 `ConfigMigrationTests` 中增加用例，覆盖首次安装、升级、重复运行三种情况；
  - 在 CHANGELOG 中写明对玩家的影响。
