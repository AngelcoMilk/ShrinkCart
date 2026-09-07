# Changelog

ShrinkCart 更新日志。

- 格式遵循 Keep a Changelog；日期取自仓库 git 提交日期。
- 条目按仓库 git 发布记录整理。仓库未为每个补丁版本单独打 tag，中间补丁发布（0.2.6–0.2.8、0.2.13–0.2.15、0.2.17、0.2.21、0.2.23–0.2.32、0.2.36–0.2.44、0.2.48–0.2.49）的改动并入相邻里程碑条目，并在标题中注明区间。
- 每条尽量说明「现象 → 原因 → 修法 → 影响」。

## 0.2.51 (2026-09-07)

### 适配

- 将硬依赖 `Vippy-ScalerCore` 从 1.0.4 提升到 1.0.6。上游 1.0.5/1.0.6 相对 1.0.4 明确无 API 变化（未改 `ScaleManager`、`ScaleOptions`、`IScaleHandler` 的公开面），因此本版不涉及 hook 表变更，只同步构建引用、manifest 依赖与文档，锁定到上游当前稳定版。手动安装/旧依赖用户需把 ScalerCore 升到 1.0.6 后使用本版。
- 构建脚本照常对解析到的 `ScalerCore.dll` 执行 hook 目标校验（`ScaleManager` / `ScaleOptions` / `ScaleController` / `ScaleHandlerRegistry` 全套），编译前即可暴露上游任何意外签名漂移。

### 新增

- 新增本 `CHANGELOG.md`，并让 `build.ps1` 在构建/打包时自动把 `CHANGELOG.md` 放入 dist 根目录与发布 zip，随 Thunderstore 包一起发布。

### 改进

- 缩放速度改用 10 倍刻度：`放入时缩小速度` 默认 `0.5` → `5`，`取出后放大速度` 默认 `0.2` → `2`，实际速度 = 配置值 × 0.1。默认手感与旧版严格一致（5 × 0.1 = 0.5、2 × 0.1 = 0.2），但在 REPOConfig 菜单里每 0.01 调节步对应实际速度变化 0.001（原为 0.01），调节粒度细化为原来的十分之一，配置范围维持 `0.1 ~ 20` 不变。旧配置值自动 ×10 迁移（如 `0.5` → `5`），升级后手感不变；旧实际速度超过新刻度上限（`> 2.0`）的值自动钳到 `20`，并在 BepInEx 日志中标注。
- 修复 REPOConfig 菜单中数值显示超长小数、滑条步进失效的恶性循环：MenuLib 滑条吸附的浮点误差会被原样保存进配置文件（如 `0.3999999`），REPOConfig 按「默认值的小数位数」推算滑条精度，读到脏值后精度被撑爆（步进变成 1e-7 级、显示 F7）。现所有 float 配置在读取与写入（`SettingChanged`）时统一按两位小数规格化，历史脏值自动清洗，滑条精度恒为 0.01。

## 0.2.50 (2026-09-07)

### 改进

- README / 包内 README 全面中英文本地化改写：改为中文开、英文折叠的双语结构，按较友好的语气重写（对齐 YAPYAP 风格），移除 README 标题中的版本号与对玩家不友好的 API/技术术语，精简「已知限制」章节（9 → 5 项）并最终整章删除，默认值表由 45 行精简为 12 行关键项，特性章节合并为「可配置物品缩放」，代币/特殊物品与动图演示统一为「R.E.P.O. v0.4.x 代币/外观箱适配」表述，新增 100K 下载里程碑致谢与 GitHub Issues 链接。
- 构建脚本在 Release 出包路径上整理调试处理；`Plugin.cs` 与 manifest 版本号同步。

## 0.2.47 (2026-07-22)

### 修复

- 修复 Thunderstore 发布包 DLL 根布局：zip 内 DLL 调整到包根（与 `manifest.json` / `README.md` / `icon.png` 三件套同层），保证 Mod Manager 导入解析一致。

### 改进

- `build.ps1` 依赖解析改为**优先从目标 r2modman profile 的 `BepInEx\plugins` 读取** `ScalerCore.dll` / `REPOConfig.dll`，避免直接命中共享 cache 里未部署的旧版本；profile 未部署时才回退 cache 并打印警告、提示用 `-ScalerCoreDll` 显式指定。
- 新增 `tools/` 开发辅助目录（`Test-DevelopmentEnvironment.ps1`、`Setup-ModAnalysisTools.ps1`、`Get-LocalREPOProfileInventory.ps1` 与说明文档）。

### 适配

- 将 ScalerCore 最低依赖提升到 1.0.4，以采用其针对最新游戏版本玩家移动/缩放调用链的修复，并为死亡头颅缩放（`AllowDeadHeads`）提供支撑。

## 0.2.46 (2026-07-04)

### 内部

- 版本号与 README/构建元数据同步；ScalerCore 依赖锁定 1.0.2。相对 0.2.45 无用户可见功能变化。

## 0.2.45 (2026-06-16)（含 0.2.36–0.2.44 未单独标记的补丁发布）

### 新增

- 新增**死亡头颅进车缩放**模块（`DeadHeadCartScaleController`）：玩家死亡头颅可被放入购物车缩小、在复活等边界自动处理，与 ScalerCore 的 `AllowDeadHeads` 能力配合。

### 重构

- 移除旧的主机配置同步模块 `HostConfigSync`：配置不再由主机向客户端广播（大小同步统一交由 ScalerCore 网络层负责），ShrinkCart 恢复为纯主机决策触发方。
- `ModConfig` 大规模重构为本地化绑定体系（配置名与描述支持中英双语，经 REPOConfig 界面显示）。
- `PlayerCartScaleController` 站车检测/恢复路径增强；`ShrinkerCartPatches` 补全对应生命周期 hook。

### 适配

- ScalerCore 依赖从 0.6.1 时代迁移至 1.0.x（区间内先升到 1.0.0，0.2.45 锁定 1.0.1），配合上游 1.0 重构后的同步/动画 API。

## 0.2.35 (2026-05-31)

### 内部

- manifest / README / 版本号同步。相对 0.2.34 无用户可见功能变化。

## 0.2.34 (2026-05-31)

### 修复

- 购物车缩小与恢复流程细节修正（`ShrinkerCartController`），玩家站车判定增强（`PlayerCartScaleController`），并同步调整 `ModConfig`。整体为玩家站车循环与进车/恢复链路的稳定化热修。

## 0.2.33 (2026-05-22)（含 0.2.23–0.2.32 未单独标记的补丁发布）

### 重构

- 引入显式主机权威模块 `Authority.cs`：以 `SemiFunc.IsMasterClientOrSingleplayer()` + 0.25 秒节流缓存统一「主机或单人」判定（异常时保守视为主机），所有主机决策入口（进车、恢复、玩家站车、死亡头颅、敌人秒杀）统一走该判定，消除各处分散判断的不一致。
- 清理废弃模块：移除 `PlayerScaleDeathSafety`（死亡安全逻辑并入新链路）与 `VehicleCrushController`（车辆压扁处理废弃）。
- `PlayerCartScaleController` / `ShrinkerCartController` / `CartRegistry` / `ShrinkerCartPatches` 同步重构；`ModConfig` 配置项整理；`ValuableBoxScaleAdapter` 微调；README 大幅更新。

## 0.2.22 (2026-05-15)

### 修复

- 玩家站车缩小/恢复边界修正：离车判定与站车计时重置逻辑调整（`PlayerCartScaleController` / `PlayerScaleDeathSafety` 相关边界），插件在主机/单人身份切换时的注册/重置生命周期整理（`Plugin`），`ShrinkerCartPatches` 移除失效补丁，`ModConfig` 相应新增配置项。

## 0.2.20 (2026-05-15)

### 内部

- 大幅精简购物车注册逻辑（`CartRegistry` 删减约 400 行）、清理失效补丁与构建脚本中的冗余路径，仅保留被实际使用的注册/追踪结构。

## 0.2.19 (2026-05-15)

### 重构

- 代码瘦身与配置整理：`ModConfig` 默认值与分组精简，死亡安全/恢复逻辑并入统一路径（`PlayerScaleDeathSafety` 收敛），`HostConfigSync` 主机广播内容缩减，README 同步。

## 0.2.18 (2026-05-15)（含 0.2.17）

### 新增

- 新增 `CartRegistry`：全场景购物车注册表，统一维护所有 `PhysGrabCart` 及其进车引用，供玩家站车检测与物品进车追踪使用；`CartObjectGuard` 进车过滤逻辑随之接入。

## 0.2.16 (2026-05-15)（含 0.2.13–0.2.15）

### 新增

- 新增 `ValuableBoxScaleAdapter`：适配 R.E.P.O. v0.4.x 新增的代币箱/外观箱（`ItemValuableBox` / `CosmeticWorldObject`）缩放与取出恢复。
- 新增 `CartObjectGuard`：进车过滤（商店用品、车类载物等分类白名单判断入口）。
- 新增 `PlayerScaleDeathSafety`（后续版本并入他处后移除）：玩家缩小状态下的死亡/重生边界保护。

## 0.2.12 (2026-05-13)

### 内部

- manifest 元数据修正（`e14d88e`）、`ModConfig` 配置项微调与文档同步。无重大功能变化。

## 0.2.11 (2026-05-13)

### 修复

- 玩家站车缩放进一步调校：站车中心判定与循环切换逻辑修正（`PlayerCartScaleController` 增删约 120 行），构建脚本调整。

## 0.2.10 (2026-05-13)

### 修复

- 玩家站车缩放补强（`PlayerCartScaleController` 增约 150 行）：修正初版（0.2.9 引入）玩家站车判定在边界/多玩家下的误触发与恢复延迟问题，配套 `ModConfig` 新增相关配置。

## 0.2.9 (2026-05-13)（含 0.2.6–0.2.8）

### 新增

- 新增**玩家站车缩小/增大循环**（`PlayerCartScaleController`）：玩家站上购物车中心区域一段时间后自动循环「原样 → 缩小 → 原样 → 增大 → 原样」。

### 适配

- 升级 ScalerCore 0.5.2 → 0.6.1 并同步构建引用；移除旧的 `CartScaleVisualEffects` 视觉辅助；`HostConfigSync` 收缩；README/配置默认值同步（含早期 0.2.6–0.2.8 的多次发布迭代）。

## 0.2.5 (2026-05-12)（含 0.2.0–0.2.4 初版系列）

### 新增

- ShrinkCart 初版系列（0.2.0–0.2.5）：物品放入 C.A.R.T / 购物车自动缩小、真正离车后恢复原尺寸。
- 分类倍率与过滤：贵重物按体型（Tiny/Small/Medium/Big/Wide/Tall/VeryTall）分档，商店用品（枪械、血包等）默认不缩，敌人球/钱袋独立倍率，普通非贵重物默认不缩。
- 购物车触发体验：边缘防抖延迟、恢复后重新缩小冷却。
- 主机侧配置广播模块 `HostConfigSync`（后随架构演进移除）与车辆压扁处理 `VehicleCrushController`（后废弃移除）。
- 配套构建脚本、Thunderstore 包结构（manifest/README/icon）、README 与演示 GIF。
