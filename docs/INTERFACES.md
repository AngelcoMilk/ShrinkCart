# ShrinkCart 接口文档

本文列出 ShrinkCart 与外部（游戏、ScalerCore、REPOConfig、BepInEx 配置文件）之间的全部契约，以及内部模块之间的接口。
架构与设计原因见 [ARCHITECTURE.md](ARCHITECTURE.md)，日常维护流程见 [MAINTENANCE.md](MAINTENANCE.md)。

> **生成块**：文中 `<!-- BEGIN GENERATED: … -->` 与 `<!-- END GENERATED: … -->` 之间的内容由工具生成，不要手改。
>
> - `hooks`、`game-members`：由 `tools/ShrinkCart.BuildTools` 从编译产物推导（`build.ps1 -UpdateDocs` 或 `BuildTools docs`）。
> - `config-schema`、`config-migrations`：由单元测试 `InterfacesDocTests` 从 `ConfigSchema` / `ConfigMigrator` 生成。
>
> `build.ps1` 每次都会校验这些块是否与代码一致，不一致时构建失败。

## 1. Harmony Hook

每个 patch 类都用 `[PatchFeature]` 标注所属功能、目标方法的调用频率与执行端。
补丁安装按类进行，某个类失败只会让它所属的功能（以及依赖该功能的功能）不可用，见 [ARCHITECTURE.md §10](ARCHITECTURE.md#10-故障降级矩阵)。

所有 patch 都遵守同一约定：

- 只做转发。`ShrinkCartRuntime.Current` 为 `null`（初始化失败或已卸载）时直接放行原版逻辑。
- 转发调用包在 `try/catch` 中，异常交给 `ShrinkCartRuntime.ReportFailure(feature, context, ex)`，绝不抛回游戏。
- Prefix 返回 `false` 时 Harmony 仍会执行 Postfix，因此 Prefix 与 Postfix 之间用 `__state` 传递"是否已拦截 / 是否需要处理"。
- 普通参数按名称绑定到原方法参数（如 `_physGrabObject`）。游戏改了参数名时，构建期校验会报错。

<!-- BEGIN GENERATED: hooks -->
| 目标方法 | 补丁 | 所属功能 | 调用频率 | 执行端 | Patch 类 |
|---|---|---|---|---|---|
| `RunManager.ChangeLevel(bool, bool, RunManager.ChangeLevelType)` | Prefix | Core | 每次切换关卡一次 | 所有端调用；只有主机侧已激活的功能会恢复缩放 | `RunManagerChangeLevelPatch` |
| `PhysGrabCart.ObjectsInCart()` | Prefix + Postfix | CartObservation | 每帧 × 每辆车（原版仅在 12 m 内有玩家且计时器到期时每 0.5 秒真正扫描；ShrinkCart 只处理真实扫描） | 所有端调用，仅主机处理 | `PhysGrabCartObjectsInCartPatch` |
| `PhysGrabCart.Start()` | Postfix | CartObservation | 每辆车生成时一次 | 所有端（登记表是被动的，只有主机侧功能使用） | `PhysGrabCartStartPatch` |
| `PhysGrabInCart.Add(PhysGrabObject)` | Prefix + Postfix | CartObservation | 每个物理帧 × 车 trigger 内每个未休眠物体（OnTriggerStay） | 仅主机（原版在 OnTriggerStay 中判定 IsMasterClient） | `PhysGrabInCartAddPatch` |
| `PlayerDeathHead.Revive()` | Prefix | DeadHeadShrink | 每次玩家复活一次 | 所有端调用；只有主机侧死亡头颅功能激活时处理 | `PlayerDeathHeadRevivePatch` |
| `ItemValuableBox.Start()` | Postfix | ValuableBoxSupport | 每个代币箱生成时一次 | 所有端（与 PhysGrabObject.Start 互为兜底，取决于两者 Start 的先后顺序） | `ItemValuableBoxStartPatch` |
| `PhysGrabObject.Start()` | Postfix | ValuableBoxSupport | 每个 PhysGrabObject 生成时一次（非代币箱只做一次组件查找链） | 所有端（客户端也需要控制器接收 ScalerCore 同步） | `PhysGrabObjectStartPatch` |
<!-- END GENERATED: hooks -->

## 2. 非公开游戏成员（GameAccess）

对 `Assembly-CSharp` 非公开成员的访问全部集中在 `ShrinkCart.Game.GameAccess`。编译时引用的是 publicize 后的 `Assembly-CSharp`（只存在于 `obj/`，不入库），因此这些访问是强类型的，游戏改名或删字段会直接变成编译错误。

- 每个访问方法都标注 `[GameMember(typeof(Owner), "name", typeof(MemberType), FeatureId…)]` 和 `[MethodImpl(MethodImplOptions.NoInlining)]`。
- **启动自检**（`GameAccessSelfCheck`）按声明核对成员仍存在、字段类型不变；不满足时把声明中的功能标为不可用，并打一条 Warning。
- **构建期校验**（`BuildTools validate`）会确认三件事：
  - 对原版非公开成员的 IL 引用只出现在 `GameAccess` 中；
  - 每个方法的 `[GameMember]` 声明与它实际引用的成员完全一致；
  - 字段类型与声明一致。
- 其他代码只能使用原版中本来就是 `public` 的成员，这一点同样由构建期校验保证。

<!-- BEGIN GENERATED: game-members -->
| 成员 | 种类 | 类型 | 原版可见性 | GameAccess 方法 | 所属功能 |
|---|---|---|---|---|---|
| `ItemAttributes.itemType` | field | `SemiFunc.itemType` | private | `ItemTypeOf` | CartObservation |
| `PhysGrabCart.itemsInCart` | field | `List<PhysGrabObject>` | internal | `ItemsInCart` | CartObservation |
| `PhysGrabCart.objectInCartCheckTimer` | field | `float` | private | `CartCheckTimer` | CartObservation |
| `PhysGrabObject.isGun` | field | `bool` | internal | `IsGun` | CartObservation |
| `PhysGrabCart.haulCurrent` | field | `int` | internal | `HaulCurrent`, `SetHaulCurrent` | CartNestingGuard |
| `PhysGrabCart.itemsInCartCount` | field | `int` | internal | `ItemsInCartCount`, `SetItemsInCartCount` | CartNestingGuard |
| `ValuableObject.dollarValueCurrent` | field | `float` | internal | `DollarValueCurrent` | CartNestingGuard |
| `Enemy.Health` | field | `EnemyHealth` | internal | `HealthOf` | EnemyCrush |
| `EnemyHealth.dead` | field | `bool` | internal | `IsDead` | EnemyCrush |
| `EnemyHealth.healthCurrent` | field | `int` | internal | `CurrentHealth` | EnemyCrush |
| `PlayerDeathHead.inExtractionPoint` | field | `bool` | internal | `IsHeadInExtractionPoint` | DeadHeadShrink |
| `PlayerDeathHead.inTruck` | field | `bool` | internal | `IsHeadInTruck` | DeadHeadShrink |
| `PlayerDeathHead.physGrabObject` | field | `PhysGrabObject` | internal | `HeadPhysGrabObject` | DeadHeadShrink |
| `PlayerDeathHead.triggered` | field | `bool` | internal | `IsHeadTriggered` | DeadHeadShrink |
| `PhysGrabCart.inCart` | field | `Transform` | internal | `InCartVolume` | PlayerScaling |
| `PlayerAvatar.collider` | field | `Collider` | private | `AvatarCollider` | PlayerScaling |
<!-- END GENERATED: game-members -->

## 3. ScalerCore 契约

最低版本：**ScalerCore 1.0.6**（`package/manifest.json` 依赖 `Vippy-ScalerCore-1.0.6`）。只有 `ShrinkCart.Scaling.ScalerCoreGateway` 可以调用 `ScaleManager`，这由构建期的分层规则强制。

### 3.1 使用的 API

| API | 用途 |
|---|---|
| `ScaleManager.ApplyIfNotScaled(GameObject, ScaleOptions) : bool` | 施加缩放；目标已被缩放时返回 `false`，ShrinkCart 不接管 |
| `ScaleManager.IsScaled(GameObject) : bool` | 施加前检查是否已有其他来源的缩放 |
| `ScaleManager.GetController(GameObject) : ScaleController` | 读取 `IsScaled` 与 `CurrentOptions`，用于所有权核对与恢复 |
| `ScaleManager.UpdateOptions` / `Restore` | 物品、代币箱的恢复 |
| `ScaleManager.ForceUpdateOptions` / `ForceRestore` | 玩家、死亡头颅的恢复 |
| `ScaleManager.AllowDeadHeads`（静态） | 只在缩小头颅的那次调用期间临时设为 `true`，`finally` 中还原 |
| `ScaleHandlerRegistry.Register(IScaleHandler, Func<GameObject,bool>, int)` | 注册代币箱的空 handler（优先级 -10），让 ScalerCore 认领代币箱 |
| `ScaleController`（组件） | 代币箱生成时由 ShrinkCart 挂上（所有端） |

### 3.2 四类 ScaleOptions（`ScaleOptionsFactory`）

所有类型都从 `ScaleOptions.Default` 开始构建。

| 字段 | 物品 Item | 代币箱 ValuableBox | 死亡头颅 DeadHead | 玩家 Player |
|---|---|---|---|---|
| `Factor` | 分类倍率 | 代币箱倍率 | 死亡头颅缩小倍率 | 缩小倍率或增大倍率 |
| `Speed` / `RestoreSpeed` | 配置值 × 0.1 | 同左 | 同左 | 同左 |
| `Duration` | `0`（不自动恢复） | `0` | `0` | `0` |
| `AllowedTargets` | `Valuables \| Items` | `All` | `All` | `Players` |
| `SuppressValueDropExpand` | 「防止碰撞弹回原尺寸」 | 同左 | `true` | 默认 |
| `PreserveMass` | `!`「启用重量随缩放降低」 | 同左 | 同左 | 默认 |
| `SuppressImpactFlash` / `SuppressCameraShake` | `true` | `true` | `true` | `true` |
| `RejectExternalApply` | 默认 | 默认 | `false` | `false` |
| `IgnoreBonkExpand` | 默认 | 默认 | 默认 | `!`「启用玩家受伤后自动恢复」 |
| 恢复方式 | `UpdateOptions` + `Restore` | 同左 | `ForceUpdateOptions` + `ForceRestore` | 同左 |

恢复前，网关会用 `CurrentOptions` 覆盖 `RestoreSpeed`，并关闭闪光和镜头震动。恢复方式沿用 0.2.x 的实测选择。

### 3.3 ShrinkCart 依赖的 ScalerCore 行为

以下行为已在 1.0.6 的 IL 中核实。升级 ScalerCore 时必须重新核对，流程见 [MAINTENANCE.md §5](MAINTENANCE.md#5-依赖更新流程)。

1. `ApplyIfNotScaled` 在同一调用栈内同步调用 `DispatchShrink`。所以在调用前后临时开关 `AllowDeadHeads` 是有效的。
2. `DispatchShrink` 先检查 `IsMasterClientOrSingleplayer`，然后读取 `AllowDeadHeads`；它是这个开关的唯一读取者。
3. `DispatchExpand`（恢复路径）不读取 `AllowDeadHeads`。所以恢复头颅不需要打开开关。
4. `Factor` 和 `Speed` 只在 `≤ 0` 时才会被替换为默认值。因此 `CurrentOptions.Factor` 会原样保留 ShrinkCart 传入的倍率，所有权核对依赖这一点。
5. 缩放派发只在主机执行，结果通过 ScalerCore 自己的 RPC 同步到客户端。

## 4. REPOConfig 与配置文件契约

- 配置文件是 `BepInEx/config/AngelcoMilk.ShrinkCart.cfg`。文件名由插件 GUID `AngelcoMilk.ShrinkCart` 决定，构建期校验禁止修改 GUID。
- REPOConfig 直接显示 BepInEx 的 section 和 key 名，没有提供显示名的机制，所以键名按配置语言本地化（中文或 English）。
  - 每个配置项有稳定的语言无关 ID（见下表），迁移、测试和文档都使用这个 ID。
  - 切换语言时，旧语言键的值会迁移到新语言键，值不会丢失。
- `AcceptableValueRange<float>` 显示为滑条，`AcceptableValueList<string>` 显示为下拉框（配置语言）。
- 描述中带 `HideFromREPOConfig` 标签的条目不会在 REPOConfig 中显示（REPOConfig 1.2.6 识别 `HideFromREPOConfig` / `HideREPOConfig`）。schema 版本条目使用这个标签。
- 所有数值在读取和写入时都规整为两位小数，避免滑条的浮点误差（如 `0.3999999`）落盘后破坏 REPOConfig 的精度推断。
- 在游戏中修改任意配置都会触发 `SettingChanged`：
  - `ModConfig.ScalingConfigVersion` 递增，对象分类缓存里的倍率随之刷新；
  - 各功能的启用状态由 `ModLifecycle` 每帧读取，开关切换在下一帧生效。

### 4.1 特殊配置项

| 分组 / 键 | 说明 |
|---|---|
| `语言 / Language` / `配置语言 / Config Language` | `中文`（默认）或 `English`。切换后需重启游戏或重新加载配置，才会按新语言重建全部键名 |
| `ShrinkCart.Meta` / `ConfigSchemaVersion` | 配置结构版本，对 REPOConfig 隐藏，请勿手改 |

### 4.2 配置项

表中"范围"同时是 REPOConfig 滑条的范围，也是 `ModConfig.Safe*` 读取时再次夹紧所用的范围。速度类配置按 ×10 刻度存储，实际速度 = 配置值 × 0.1。

<!-- BEGIN GENERATED: config-schema -->
| ID | 所属功能 | 中文分组 / 键 | English section / key | 类型 | 默认值 | 范围 | 引入版本 |
|---|---|---|---|---|---|---|---|
| `ModEnabled` | Core | 通用 / 启用 ShrinkCart | General / Enable ShrinkCart | bool | true | — | 0.3.1 |
| `CartShrinkingEnabled` | ItemShrink | 购物车 / 启用购物车缩小 | Cart / Enable cart shrinking | bool | true | — | ≤0.2.52 |
| `CartScaleSpeed` | ItemShrink | 购物车 / 放入时缩小速度 | Cart / Shrink speed | float | 5 | 0.1 – 20 | ≤0.2.52 |
| `RestoreScaleSpeed` | ItemShrink | 购物车 / 取出后放大速度 | Cart / Restore speed | float | 2 | 0.1 – 20 | ≤0.2.52 |
| `CartLeaveDebounceSeconds` | ItemShrink | 购物车 / 离车防抖延迟 | Cart / Leave-cart debounce | float | 0.5 | 0.1 – 10 | ≤0.2.52 |
| `ReshrinkCooldownSeconds` | ItemShrink | 购物车 / 恢复后重新缩小冷却 | Cart / Reshrink cooldown | float | 0.5 | 0.05 – 10 | ≤0.2.52 |
| `ScaleMassWithSize` | ItemShrink | 购物车 / 启用重量随缩放降低 | Cart / Scale mass with size | bool | true | — | ≤0.2.52 |
| `ShrinkShopPlayerItems` | ItemShrink | 购物车 / 启用商店用品缩小 | Cart / Enable shop item shrinking | bool | false | — | ≤0.2.52 |
| `PlayerScalingModuleEnabled` | PlayerScaling | 玩家缩放 / 启用玩家缩放 | Player Scaling / Enable player scaling | bool | true | — | ≤0.2.52 |
| `PlayerCartScaleFactor` | PlayerScaling | 玩家缩放 / 玩家进车缩放倍率 | Player Scaling / Player cart scale factor | float | 0.55 | 0.05 – 1 | ≤0.2.52 |
| `PlayerCartGrowFactor` | PlayerScaling | 玩家缩放 / 玩家进车增大倍率 | Player Scaling / Player cart grow factor | float | 1.5 | 1 – 2 | ≤0.2.52 |
| `PlayerCartStandTriggerSeconds` | PlayerScaling | 玩家缩放 / 玩家站车触发时间 | Player Scaling / Player stand trigger time | float | 2 | 0.25 – 10 | ≤0.2.52 |
| `PlayerCartExitGraceSeconds` | PlayerScaling | 玩家缩放 / 玩家离车判定宽容时间 | Player Scaling / Player exit grace time | float | 0.6 | 0 – 2 | ≤0.2.52 |
| `PlayerCartDetectionIntervalSeconds` | PlayerScaling | 玩家缩放 / 玩家检测间隔 | Player Scaling / Player detection interval | float | 0.75 | 0.25 – 2 | ≤0.2.52 |
| `RestorePlayerOnDamage` | PlayerScaling | 玩家缩放 / 启用玩家受伤后自动恢复 | Player Scaling / Restore player on damage | bool | true | — | ≤0.2.52 |
| `SuppressValuableDamageRestore` | ItemShrink | 购物车 / 防止碰撞弹回原尺寸 | Cart / Suppress collision restore | bool | true | — | ≤0.2.52 |
| `CartNestingGuardEnabled` | CartNestingGuard | 购物车 / 阻止车类物品计入车内 | Cart / Ignore carts inside carts | bool | true | — | 0.3.1 |
| `DeadHeadScalingEnabled` | DeadHeadShrink | 死亡头颅 / 启用死亡头颅进车缩小 | Death Heads / Enable dead head cart shrinking | bool | false | — | ≤0.2.52 |
| `DeadHeadScaleFactor` | DeadHeadShrink | 死亡头颅 / 死亡头颅缩小倍率 | Death Heads / Dead head scale factor | float | 0.5 | 0.05 – 1 | ≤0.2.52 |
| `Tiny.Enabled` | ItemShrink | Tiny 微型贵重物 / 启用此分类缩小 | Tiny valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Tiny.Factor` | ItemShrink | Tiny 微型贵重物 / 缩小倍率 | Tiny valuables / Scale factor | float | 0.8 | 0.05 – 1 | ≤0.2.52 |
| `Small.Enabled` | ItemShrink | Small 小贵重物 / 启用此分类缩小 | Small valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Small.Factor` | ItemShrink | Small 小贵重物 / 缩小倍率 | Small valuables / Scale factor | float | 0.6 | 0.05 – 1 | ≤0.2.52 |
| `Medium.Enabled` | ItemShrink | Medium 中贵重物 / 启用此分类缩小 | Medium valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Medium.Factor` | ItemShrink | Medium 中贵重物 / 缩小倍率 | Medium valuables / Scale factor | float | 0.45 | 0.05 – 1 | ≤0.2.52 |
| `Big.Enabled` | ItemShrink | Big 大贵重物 / 启用此分类缩小 | Big valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Big.Factor` | ItemShrink | Big 大贵重物 / 缩小倍率 | Big valuables / Scale factor | float | 0.4 | 0.05 – 1 | ≤0.2.52 |
| `Wide.Enabled` | ItemShrink | Wide 宽贵重物 / 启用此分类缩小 | Wide valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Wide.Factor` | ItemShrink | Wide 宽贵重物 / 缩小倍率 | Wide valuables / Scale factor | float | 0.35 | 0.05 – 1 | ≤0.2.52 |
| `Tall.Enabled` | ItemShrink | Tall 高贵重物 / 启用此分类缩小 | Tall valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `Tall.Factor` | ItemShrink | Tall 高贵重物 / 缩小倍率 | Tall valuables / Scale factor | float | 0.35 | 0.05 – 1 | ≤0.2.52 |
| `VeryTall.Enabled` | ItemShrink | VeryTall 超高贵重物 / 启用此分类缩小 | VeryTall valuables / Enable this category | bool | true | — | ≤0.2.52 |
| `VeryTall.Factor` | ItemShrink | VeryTall 超高贵重物 / 缩小倍率 | VeryTall valuables / Scale factor | float | 0.25 | 0.05 – 1 | ≤0.2.52 |
| `EnemyOrbEnabled` | ItemShrink | 敌人球 / 启用敌人球缩小 | Enemy Orbs / Enable enemy orb shrinking | bool | true | — | ≤0.2.52 |
| `EnemyOrbSmallScaleFactor` | ItemShrink | 敌人球 / Small 敌人球倍率 | Enemy Orbs / Small enemy orb factor | float | 0.8 | 0.05 – 1 | ≤0.2.52 |
| `EnemyOrbMediumScaleFactor` | ItemShrink | 敌人球 / Medium 敌人球倍率 | Enemy Orbs / Medium enemy orb factor | float | 0.65 | 0.05 – 1 | ≤0.2.52 |
| `EnemyOrbBigScaleFactor` | ItemShrink | 敌人球 / Big 敌人球倍率 | Enemy Orbs / Big enemy orb factor | float | 0.45 | 0.05 – 1 | ≤0.2.52 |
| `EnemyOrbBerserkerScaleFactor` | ItemShrink | 敌人球 / Berserker 敌人球倍率 | Enemy Orbs / Berserker enemy orb factor | float | 0.45 | 0.05 – 1 | ≤0.2.52 |
| `SurplusEnabled` | ItemShrink | 特殊物品 / 启用钱袋/Surplus 缩小 | Special Objects / Enable money bag / Surplus shrinking | bool | true | — | ≤0.2.52 |
| `SurplusScaleFactor` | ItemShrink | 特殊物品 / 钱袋/Surplus 倍率 | Special Objects / Money bag / Surplus factor | float | 0.25 | 0.05 – 1 | ≤0.2.52 |
| `ValuableBoxEnabled` | ItemShrink | 特殊物品 / 启用代币箱缩小 | Special Objects / Enable token box shrinking | bool | true | — | ≤0.2.52 |
| `ValuableBoxScaleFactor` | ItemShrink | 特殊物品 / 代币箱倍率 | Special Objects / Token box factor | float | 0.4 | 0.05 – 1 | ≤0.2.52 |
| `FallbackScaleFactor` | ItemShrink | 商店用品 / 商店用品缩小倍率 | Shop Items / Shop item scale factor | float | 0.5 | 0.05 – 1 | ≤0.2.52 |
| `EnemyInCartInstantKill` | EnemyCrush | 车辆碾压 / 敌人进车秒杀 | Cart Crush / Instant-kill enemies in cart | bool | true | — | ≤0.2.52 |
| `DynamicItemScanEnabled` | ItemShrink | 性能 / 启用动态物品扫描 | Performance / Enable dynamic item scanning | bool | true | — | ≤0.2.52 |
| `MinimumItemScanIntervalSeconds` | ItemShrink | 性能 / 最小物品扫描间隔 | Performance / Minimum item scan interval | float | 0.15 | 0.05 – 1 | ≤0.2.52 |
| `MaximumItemScanIntervalSeconds` | ItemShrink | 性能 / 最大物品扫描间隔 | Performance / Maximum item scan interval | float | 1 | 0.1 – 2 | ≤0.2.52 |
| `DebugLogging` | Core | 诊断 / 启用调试日志 | Diagnostics / Enable debug logging | bool | false | — | ≤0.2.52 |
<!-- END GENERATED: config-schema -->

### 4.3 迁移

| schema | 引入版本 | 一次性迁移内容 |
|---|---|---|
| 0 | ≤ 0.2.52 | 没有版本条目 |
| 1 | 0.3.1 | 迁移前把 cfg 备份为 `AngelcoMilk.ShrinkCart.cfg.pre-0.3.1.bak`（已存在则跳过）。然后：<ul><li>缩小/放大速度无条件统一为默认值 `5` / `2`（×10 刻度不变）；</li><li>仍停在历史默认值的数值项升级为当前默认值（见下方生成表），玩家改过的值与开关类不动；</li><li>删除 `[ShrinkCart.Migration] SpeedScaleV2Migrated` 哨兵；</li><li>删除 marker 文件 `ShrinkCart.v0.2.39.mass-default-enabled`，不再覆盖「启用重量随缩放降低」</li></ul> |

规则：

- 只有在**改名、删除配置项或改刻度**时才提升 schema 版本。新增配置项不需要迁移，比如 0.3.1 新增的两个开关。
- 全新安装时直接写入最新版本，不执行一次性迁移。
- 不论 schema 版本是多少，每次启动都会执行下面两项（幂等）：
  - **旧键继承**：只有新键在 cfg 中没有值时才继承；
  - **废弃键删除**。

<!-- BEGIN GENERATED: config-migrations -->
当前 schema 版本：**1**（`[ShrinkCart.Meta] ConfigSchemaVersion`）。

| 新配置项 ID | 继承自旧键（section / key，按顺序取第一个存在的值） |
|---|---|
| `ReshrinkCooldownSeconds` | 购物车 / 取出后恢复延迟; Cart / RestoreGraceSeconds |
| `SurplusEnabled` | 特殊物品 / 启用 Surplus 缩小 |
| `SurplusScaleFactor` | 特殊物品 / Surplus 倍率 |
| `FallbackScaleFactor` | 普通或未知物品 / 默认缩小倍率 |
| `EnemyInCartInstantKill` | 车辆碾压 / 车辆碾压秒杀敌人; VehicleCrush / InstantKillEnemies |
| `Tiny.Enabled` | Tiny 小型贵重物 / 启用此分类缩小 |
| `Tiny.Factor` | Tiny 小型贵重物 / 缩小倍率 |

一次性升级（仅 schema 0→1 执行）：值仍等于某个历史默认值的配置项，视为从没改过，升级为当前默认值；玩家改过的值与开关类设置不动。缩小/放大速度另行无条件统一为 5 / 2。

| 配置项 ID | 历史默认值 | 升级为 |
|---|---|---|
| `CartLeaveDebounceSeconds` | 2.5 | 0.5 |
| `ReshrinkCooldownSeconds` | 0.2 | 0.5 |
| `Tiny.Factor` | 0.55 | 0.8 |
| `Small.Factor` | 0.45 | 0.6 |
| `Medium.Factor` | 0.35, 0.33, 0.4 | 0.45 |
| `Big.Factor` | 0.3, 0.2, 0.25 | 0.4 |
| `Wide.Factor` | 0.3, 0.2, 0.25 | 0.35 |
| `Tall.Factor` | 0.28, 0.3, 0.25 | 0.35 |
| `VeryTall.Factor` | 0.2 | 0.25 |
| `EnemyOrbSmallScaleFactor` | 0.35, 0.6 | 0.8 |
| `EnemyOrbMediumScaleFactor` | 0.3, 0.55 | 0.65 |
| `EnemyOrbBigScaleFactor` | 0.25, 0.2 | 0.45 |
| `EnemyOrbBerserkerScaleFactor` | 0.25, 0.2 | 0.45 |
| `FallbackScaleFactor` | 0.4 | 0.5 |

<details><summary>每次启动都会删除的废弃键（39 个）</summary>

- ShrinkCart.Migration / SpeedScaleV2Migrated
- Cart / Enabled
- Cart / ScaleFactor
- Cart / ScaleSpeed
- Cart / PreserveMass
- Cart / ShrinkNonValuableItems
- Cart / SuppressValuableDamageRestore
- Diagnostics / DebugLogging
- 购物车 / 保持原始重量
- VehicleCrush / InstantKillPlayers
- 视觉 / 隐藏缩放闪光
- 车辆碾压 / 车辆碾压秒杀玩家
- 提取点复活兼容 / 启用提取点复活
- 提取点复活兼容 / 复活前稳定检测时间
- 提取点复活兼容 / 复活检测间隔
- 提取点复活兼容 / 拦截外部立即复活调用
- 购物车 / 商店用品也缩小
- 购物车 / 玩家也缩小
- 购物车 / 启用实验性玩家缩放
- 购物车 / 玩家死亡前自动恢复
- 购物车 / 启用玩家缩放
- 购物车 / 玩家进车缩放倍率
- 购物车 / 玩家站车触发时间
- 购物车 / 玩家离车判定宽容时间
- 购物车 / 玩家进车切换间隔
- 购物车 / 普通物品也缩小
- 购物车 / 商店/人物用品也缩小
- 购物车 / 防止车辆互相重叠
- 购物车 / 车辆硬碰撞修正强度
- 购物车 / 车辆最大单帧修正距离
- 购物车 / 车辆挤压速度清除
- 购物车 / 车辆临时忽略碰撞时间（已废弃）
- 购物车 / 车辆临时忽略碰撞时间
- 购物车 / 车辆脱困强度
- 玩家缩放 / 旧版玩家缩放开关（已停用）
- 地图兼容 / Minecraft Stronghold 普通门进车破碎
- 地图兼容 / Minecraft Stronghold 门破碎确认时间
- Map Compatibility / Minecraft Stronghold door shatter in cart
- Map Compatibility / Minecraft Stronghold door shatter confirmation time

</details>
<!-- END GENERATED: config-migrations -->

## 5. 内部接口

以下签名均为 `internal`。修改签名时要同步更新本节。

### 5.1 生命周期

```csharp
enum FeatureId { Core, CartObservation, ItemShrink, CartNestingGuard, EnemyCrush, DeadHeadShrink, PlayerScaling, ValuableBoxSupport }
static class FeatureIds { const int Count; static FeatureId[] DependenciesOf(FeatureId feature); }

enum DeactivateReason { Disabled, AuthorityLost, Unavailable, Shutdown }

interface IFeature
{
    FeatureId Id { get; }
    bool IsConfiguredEnabled { get; }          // 功能自己的配置开关（总开关由 ModLifecycle 统一处理）
    void Activate(float now);
    void Deactivate(DeactivateReason reason, float now);
    void ResetForLevel(float now);             // 换关前：恢复并清空本关状态，保持激活
    void Tick(float now);                      // 激活期间每帧调用；功能自己决定节奏
}

sealed class ModLifecycle
{
    bool IsHost { get; }
    bool IsActive(FeatureId feature);
    bool IsAvailable(FeatureId feature);
    void Register(IFeature feature);
    void Update(float now, float unscaledNow); // 每帧：主机判定 → desired 计算 → 激活/停用 → Tick
    void OnLevelChanging(float now);
    void Shutdown(float now);
    void ReportFailure(FeatureId feature, string context, Exception ex); // 标记不可用；下一帧停用
}

sealed class FeatureAvailability
{
    void MarkUnavailable(FeatureId feature, string reason);
    bool IsAvailable(FeatureId feature);       // 自身与所有依赖都没有问题
    string Describe();                         // 启动摘要
}

sealed class Authority { bool IsHost { get; } bool Poll(float unscaledNow); } // 0.25 秒缓存，异常时 fail-closed
```

`DeactivateReason` 的语义：

| 值 | 触发 | 功能应做的事 |
|---|---|---|
| `Disabled` | 功能开关或总开关关闭 | 恢复本功能施加的全部缩放 |
| `AuthorityLost` | 不再是主机 | 只丢弃状态并放弃所有权，**不**恢复（客户端无法派发缩放） |
| `Unavailable` | 依赖失效或运行期异常 | 尽力恢复后停用 |
| `Shutdown` | 插件卸载 | 恢复全部 |

### 5.2 组合根与 patch 入口

```csharp
sealed class ShrinkCartRuntime
{
    static ShrinkCartRuntime Current { get; set; }   // patch 的唯一入口；null 时 patch 放行原版
    CartObservationRouter Router { get; }
    CartRegistry Carts { get; }
    ModLifecycle Lifecycle { get; }
    DeadHeadShrinkFeature DeadHeads { get; }
    void Update(float now, float unscaledNow);
    void OnLevelChanging(float now);
    void Shutdown(float now);
    void ReportFailure(FeatureId feature, string context, Exception ex);
}

sealed class CartObservationRouter
{
    bool ShouldBlockTriggerAdd(PhysGrabInCart inCart, PhysGrabObject item);  // PhysGrabInCart.Add 前置
    void OnTriggerPresence(PhysGrabInCart inCart, PhysGrabObject item);      // PhysGrabInCart.Add 之后
    bool ShouldObserveScan(PhysGrabCart cart);                               // ObjectsInCart 前置：计时器已到期
    void OnScanMaybeCompleted(PhysGrabCart cart);                            // ObjectsInCart 之后：计时器被重置 = 刚完成真实扫描
}
```

### 5.3 游戏侧服务

```csharp
sealed class CartRegistry
{
    event Action<int, float> CartRemoved;            // (车实例 ID, 时间)；由 Prune 发出
    Dictionary<int, CartInfo>.ValueCollection All { get; }
    CartInfo GetOrRegister(PhysGrabCart cart);
    void RegisterExisting();                         // 获得主机权限时补登记
    void Prune(float now);                           // 每秒一次，移除已销毁的车
    void Clear();                                    // 换关
}

sealed class ObjectClassifier
{
    ObjectInfo Get(PhysGrabObject item);             // 按实例 ID 缓存，换关清空
    bool TryGetShrinkFactor(ObjectInfo info, out float factor); // 按 ScalingConfigVersion 刷新
    void Clear();
}

enum ObjectKind { Unsupported, CartLike, DeathHead, Enemy, ShrinkCandidate }
```

### 5.4 缩放

```csharp
enum ScaleKind { Item, ValuableBox, DeadHead, Player }
enum RestoreResult { Restored, NotOwned, Failed }

sealed class ScalerCoreGateway
{
    bool IsScaled(GameObject target);
    bool IsOurs(GameObject target);                   // 有所有权记录，且 IsScaled 与倍率仍与记录一致
    bool TryApply(GameObject target, ScaleOptions options, ScaleKind kind); // 成功后登记所有权
    RestoreResult Restore(GameObject target, float restoreSpeed);           // 只恢复自己的缩放；先释放所有权
    void Forget(int instanceId);                      // 放弃所有权但不恢复
}

sealed class ScaleOwnership { void Register(GameObject, ScaleKind, float factor); bool TryGet(int id, out OwnedScale owned); void Release(int id); }

static class ValuableBoxSupport
{
    static void RegisterHandler(FeatureAvailability availability);
    static void OnPhysGrabObjectStarted(PhysGrabObject item);
    static void OnValuableBoxStarted(ItemValuableBox box);
    static bool EnsureController(GameObject target);  // false：本次刚挂上或外观箱控制器未就绪，下次再试
}
```

### 5.5 纯逻辑（Domain，可单元测试）

```csharp
sealed class PresenceTracker
{
    void Track(int id, int cartId, float now);
    bool TryMarkSeen(int id, int cartId, float now); // 在场信号；未跟踪时返回 false
    void BeginScan(int cartId);
    void EndScan(int cartId, float now);             // 属于该车但本轮未见 → 开始计离车时间
    void OnCartRemoved(int cartId, float now);
    bool HasDue(float now, float debounceSeconds);   // O(1)，每帧调用；可能偏早，不会偏晚
    void CollectDue(float now, float debounceSeconds, List<int> result); // 调用方负责移除返回的对象
    void Remove(int id); void Clear();
}

static class ScanCadence { static float Interval(bool dynamicEnabled, int trackedCount, float minimumSeconds, float maximumSeconds); }

sealed class PlayerCycle
{
    PlayerScaleState State { get; }                  // Normal / Shrunk / Grown
    bool NextIsGrow { get; }
    PlayerCycleAction Step(bool inRange, bool inZone, float now, float exitGraceSeconds, float standTriggerSeconds);
    void OnApplied(PlayerCycleAction action); void OnRestored(); void OnExternalRestore();
}

sealed class RetryCooldown { bool IsCooling(int id, float now); void Start(int id, float now, float seconds); void Cancel(int id); void Prune(float now); void Clear(); }
```

### 5.6 配置门面

```csharp
static class ModConfig
{
    static void Bind(ConfigFile config);             // 启动时调用一次，流程见 ARCHITECTURE.md §6
    static ConfigEntry<…> <配置项>;                   // 与 ConfigSchema 一一对应
    static int ScalingConfigVersion;                 // 任一配置变化时递增
    static bool IsEnglish { get; }
    static float SafeScaleSpeed();                   // 实际速度（配置值 × 0.1），以及其他 Safe* 读取方法：再次夹紧范围
    static bool TryGetScaleFactor(ShrinkCategory category, out float factor);
}
```

## 6. 日志约定

日志来源名为 `ShrinkCart`（BepInEx 控制台与 `BepInEx/LogOutput.log`）。

| 级别 | 内容 | 示例 |
|---|---|---|
| Info | 启动摘要、配置迁移、旧键继承 | `ShrinkCart 0.3.1 loaded: patches 7/7, missing game members 0, config language 中文. All features available.` |
| Info | 有功能不可用时的启动摘要 | `… Unavailable features: EnemyCrush (Game member missing: EnemyHealth.dead); …` |
| Warning | 游戏成员缺失/类型变化、patch 安装失败、ScalerCore 调用失败（同一问题只打一次）、主机判定失败 | `patch PhysGrabCartStartPatch failed: …` |
| Error | 功能在运行期异常而被停用（同一功能、同一上下文只打一次，附异常） | `ShrinkCart feature ItemShrink disabled after runtime error in tick: …` |
| Debug | 开启「启用调试日志」后以 Info 级别、`[debug] ` 前缀输出：缩小/恢复/激活等细节，以及每 10 秒一次的热路径计数 | `[debug] Counters (last 10s): triggerCalls=… triggerFastPath=… scansProcessed=… classified=…` |

热路径计数的预期见 [ARCHITECTURE.md §9](ARCHITECTURE.md#9-hook-频率与开销预算)。
