# ShrinkCart v0.2.45

适配 R.E.P.O. v0.4.4.x 的购物车缩小搬运 mod。

ShrinkCart 会在物品放入 C.A.R.T / 购物车后自动缩小，方便搬运；物品真正离开购物车后会恢复原尺寸。底层缩放由 ScalerCore 负责，ShrinkCart 负责购物车触发、分类倍率、边缘防抖、恢复冷却、商店用品过滤、车类载物过滤、主机决策，以及可选的玩家站车缩小/增大循环、死亡头颅进车缩小和敌人进车秒杀。

## 依赖

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-1.0.1`
- `nickklmao-REPOConfig-1.2.6`

Thunderstore/r2modman 安装时会自动拉取依赖。推荐房间内所有玩家都安装 ShrinkCart 和这些依赖，缩放表现最一致。

## 主要功能

- **购物车缩小搬运**：支持的物品放入购物车后自动缩小，拿出并离车后恢复。
- **可配置物品/武器缩放**：枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店用品默认不缩小，可在配置中开启“启用商店用品缩小”并使用商店用品倍率。
- **玩家缩放**：默认开启，独立在“玩家缩放”配置分组中。开启后，玩家站在正式购物车中心区域一段时间会按“原样 -> 缩小 -> 原样 -> 增大 -> 原样”循环；关闭时 ShrinkCart 不运行玩家扫描或玩家缩放逻辑。
- **死亡头颅缩小**：默认关闭。开启后，已触发的玩家死亡头颅进入购物车会缩小，离车、进入提取点、进入卡车或复活前会恢复。
- **代币/外观箱支持**：新版本代币类箱子独立开关和倍率，默认倍率 `0.4`。
- **特殊物品支持**：敌人球 Small/Medium/Big/Berserker 使用独立倍率；钱袋/Surplus 使用独立倍率；普通非贵重物默认不缩小。
- **第三方地图标准贵重物支持**：REPOLib 地图新增的标准 `ValuableObject` 会按体积分类默认识别；普通门、按钮、机关和非贵重地图结构物默认不缩放。
- **主机决策 + ScalerCore 同步**：多人游戏中只有主机/单人执行缩小和恢复决策，最终大小由 ScalerCore 同步给客户端。

## 动图演示

### 商店用品默认不缩小

枪械、血包等商店购买类实用品默认不会被购物车缩小，避免影响战斗、治疗和工具使用。需要时可以在配置里开启“启用商店用品缩小”。

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

### 敌人球和钱袋会缩小

敌人球使用独立的敌人球配置；钱袋/SurplusValuable 使用特殊物品配置。

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

### 大型贵重物会缩小

大型、宽型、高型、超高型贵重物会按各自分类倍率缩小，适合搬运原本难以放进车里的物品。

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

### R.E.P.O. v0.4.x 特殊物品支持

R.E.P.O. 新版本的特殊物品同样可以被识别并缩小；
v0.2.45 使用 ScalerCore 1.0.1 稳定 API，保留 `CosmeticWorldObject` / `ItemValuableBox` 代币/外观箱适配，并会在原版购物车列表刷新时补处理漏掉的进车事件。第三方地图只要使用标准 `ValuableObject` / `ValuableVolume`，通常会按现有分类倍率处理。ScalerCore 1.0.1 修复了缩小贵重物的提取点检测偏移问题，靠近提取区时的判定更稳定。

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

## 配置说明

配置界面由 REPOConfig 提供，主要配置项为中文：

- 购物车：启用购物车缩小、放入时缩小速度、取出后放大速度、离车防抖延迟、恢复后重新缩小冷却、启用重量随缩放降低、启用商店用品缩小、防止碰撞弹回原尺寸。
- 玩家缩放：启用玩家缩放、玩家进车缩放倍率、玩家进车增大倍率、玩家站车触发时间、玩家离车判定宽容时间、启用玩家受伤后自动恢复。
- 死亡头颅：启用死亡头颅进车缩小、死亡头颅缩小倍率，默认关闭。
- 贵重物分类：Tiny、Small、Medium、Big、Wide、Tall、VeryTall 各自开关和倍率。
- 敌人球：启用敌人球缩小、Small/Medium/Big/Berserker 倍率。
- 特殊物品：钱袋/Surplus 开关和倍率、代币/外观箱开关和倍率。
- 商店用品：商店用品缩小倍率。
- 车辆碾压：敌人进车秒杀。
- 诊断：启用调试日志，默认 `false`。
- 语言 / Language：配置语言，默认 `中文`，可切换为 `English`。切换后应用并重启/重新加载配置，ShrinkCart 会保留当前值并重建为对应语言的配置项。

默认值：缩小速度 `0.5`，放大速度 `0.2`，离车防抖 `0.5`，恢复冷却 `0.5`，启用重量随缩放降低 `true`，启用商店用品缩小 `false`，启用玩家缩放 `true`，玩家缩放倍率 `0.55`，玩家增大倍率 `1.5`，玩家站车触发时间 `2` 秒，玩家离车判定宽容时间 `0.6` 秒，启用玩家受伤后自动恢复 `true`，启用死亡头颅进车缩小 `false`，死亡头颅缩小倍率 `0.5`，配置语言 `中文`。车类载物过滤固定为内部保护，不再作为配置项显示。贵重物倍率：Tiny `0.8`，Small `0.6`，Medium `0.45`，Big `0.4`，Wide `0.35`，Tall `0.35`，VeryTall `0.25`。敌人球倍率：Small `0.8`，Medium `0.65`，Big `0.45`，Berserker `0.45`。商店用品倍率 `0.5`，钱袋/Surplus `0.25`，代币/外观箱 `0.4`。

## 已知限制

- 缩放动画和可选质量缩小由 ScalerCore 控制，ShrinkCart 不手动写入 `Rigidbody.mass`，只传递 `PreserveMass` 选项。
- 玩家增大只属于玩家站车循环，不会让普通物品、贵重物、敌人球或车辆变大。
- 死亡头颅缩小默认关闭；开启后 ShrinkCart 只恢复自己缩小过的头颅，并且只在 `PlayerDeathHead.Revive` 前做窄范围恢复，不接管复活流程。
- “启用重量随缩放降低”只在 ScalerCore 支持该对象质量缩放时生效；部分 ItemHandler 对象可能视觉缩小明显，但手感变化不明显。
- ShrinkCart 不接管车对车物理碰撞，也不会使用穿透修正或速度钳制；车辆真实碰撞表现以原版为准。
- 如果其他缩放类 mod 已经控制同一个对象，ShrinkCart 会尽量避免覆盖该对象。
- 第三方地图新增的标准贵重物默认按原版 `ValuableObject` 识别；非贵重机关和地图结构物不会为了兼容而强制缩放。
- 客户端未安装 ShrinkCart 时，仍可能看到 ScalerCore 默认闪光/声音；全员安装效果最稳定。
- ShrinkCart 不接管死亡或复活流程，只在死亡头颅功能开启且头颅正由 ShrinkCart 缩小时，于 `PlayerDeathHead.Revive` 前恢复该头颅。REPOFidelity 与提取点复活类 mod 的冲突请使用独立的 `ExtractionReviveFix`，不要在 ShrinkCart 内处理。
- 多人中缩小和恢复由主机统一确认并通过 ScalerCore 同步，物品离车后可能略等原版购物车刷新和防抖确认再恢复，但所有客户端会跟随同一主机时机。
- 同一 r2modman profile 里只应保留一个 ShrinkCart DLL；重复手动安装会导致 BepInEx 输出跳过重复插件的日志。

---

## English

ShrinkCart is a cart-scaling hauling mod for R.E.P.O. v0.4.4.x.

When supported objects are placed into a C.A.R.T. / shopping cart, ShrinkCart asks ScalerCore to shrink them for easier hauling. Objects restore to their original size after the host confirms they have truly left the cart. ShrinkCart handles cart triggers, category factors, edge debounce, restore cooldowns, shop-item filtering, cart-object filtering, host-side decisions, optional player shrink/grow cart cycling, optional dead-head cart shrinking, and enemy-in-cart instant kill.

## Dependencies

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-1.0.1`
- `nickklmao-REPOConfig-1.2.6`

Thunderstore/r2modman installs dependencies automatically. For the most consistent multiplayer experience, all players should install ShrinkCart and its dependencies.

## Features

- **Cart shrink hauling**: supported objects shrink when placed into the cart and restore after leaving it.
- **Configurable shop-item scaling**: guns, medkits, melee items, grenades, tools, drones, pearls, upgrades, trackers, mines, and similar shop items do not shrink by default. Enable shop-item scaling in config if you want them to use the shop-item factor.
- **Player scaling**: enabled by default in its own config section. A player standing in the center floor area of a regular cart for the configured duration cycles normal -> shrink -> normal -> grow -> normal. When disabled, ShrinkCart does not run player scanning or player-scaling logic.
- **Dead-head scaling**: disabled by default. When enabled, triggered player death heads shrink in carts and restore when leaving the cart, entering extraction/truck flow, or before revive.
- **Token/cosmetic box support**: token/cosmetic boxes use their own toggle and factor. Default factor is `0.4`.
- **Special object support**: enemy orbs use independent enemy-orb factors; money bags / Surplus use their own factor; normal non-valuable objects do not shrink by default.
- **Third-party map valuable support**: standard `ValuableObject` objects added by REPOLib maps are detected through their volume category. Ordinary doors, buttons, mechanisms, and non-valuable map structures do not shrink by default.
- **Host decisions + ScalerCore sync**: in multiplayer, only the host/singleplayer side decides shrink and restore. ScalerCore syncs the final size to clients.

## GIF Demos

### Shop Items Do Not Shrink By Default

Shop items such as guns and medkits do not shrink by default, so combat, healing, and tools remain usable unless shop-item scaling is enabled.

![Shop items not scaled by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

### Enemy Orbs And Surplus Shrink

Enemy orbs use the enemy-orb config group. Money bags / SurplusValuable use the special-object config group.

![Enemy orb and Surplus scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

### Large Valuables Shrink

Large, wide, tall, and very tall valuables shrink with their own category factors, making oversized valuables easier to carry in carts.

![Large item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

### R.E.P.O. v0.4.x Special Item Support

Special items from newer R.E.P.O. versions can also be detected and scaled.
v0.2.45 uses ScalerCore 1.0.1 stable APIs, keeps support for `CosmeticWorldObject` / `ItemValuableBox` token and cosmetic boxes, and can retry missed cart-enter events during vanilla cart list refreshes. Third-party map objects that use standard `ValuableObject` / `ValuableVolume` are usually handled by the existing category factors. ScalerCore 1.0.1 fixes extraction-point detection offsets for shrunken valuables, making extraction-zone checks more stable near the extraction area.

![R.E.P.O. 4.0 special item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

## Configuration

Configuration is provided through REPOConfig. Main groups:

- Cart: enable cart shrinking, shrink speed, restore speed, leave-cart debounce, reshrink cooldown, enable mass scaling with size, enable shop-item scaling, and suppress collision restore.
- Player scaling: enable player scaling, player cart scale factor, player cart grow factor, stand trigger time, exit grace time, and restore player on damage.
- Death heads: enable dead-head cart shrinking and dead-head scale factor, default off.
- Valuable categories: toggles and factors for Tiny, Small, Medium, Big, Wide, Tall, and VeryTall.
- Enemy orbs: enemy-orb toggle plus Small / Medium / Big / Berserker factors.
- Special objects: money bag / Surplus toggle and factor, token/cosmetic box toggle and factor.
- Shop items: shop-item scale factor.
- Vehicle crush: enemy instant kill in cart.
- Diagnostics: debug logging, default `false`.
- Language / Language: config language, default `中文`, can be switched to `English`. Apply and restart/reload config after switching; ShrinkCart keeps current values and rebuilds config entries in the selected language.

Default values: shrink speed `0.5`, restore speed `0.2`, leave-cart debounce `0.5`, reshrink cooldown `0.5`, mass scaling with size `true`, shop-item scaling `false`, player scaling `true`, player shrink factor `0.55`, player grow factor `1.5`, player stand trigger time `2` seconds, player exit grace time `0.6` seconds, restore player on damage `true`, dead-head cart shrinking `false`, dead-head factor `0.5`, config language `中文`. Cart-object filtering is an internal protection and is no longer exposed as a config option. Valuable factors: Tiny `0.8`, Small `0.6`, Medium `0.45`, Big `0.4`, Wide `0.35`, Tall `0.35`, VeryTall `0.25`. Enemy-orb factors: Small `0.8`, Medium `0.65`, Big `0.45`, Berserker `0.45`. Shop-item factor `0.5`, money bag / Surplus `0.25`, token/cosmetic box `0.4`.

## Known Limits

- ScalerCore controls scaling animations and optional mass scaling. ShrinkCart does not manually write `Rigidbody.mass`; it only passes the `PreserveMass` option.
- Player growth is only part of the player cart cycle. It does not make regular objects, valuables, enemy orbs, or vehicles grow.
- Dead-head shrinking is disabled by default. When enabled, ShrinkCart only restores heads it scaled itself, and its `PlayerDeathHead.Revive` prefix only performs this narrow restore before revive; it does not take over revive flow.
- "Mass scaling with size" only has visible handling changes when ScalerCore supports mass scaling for that object. Some ItemHandler objects may shrink visually while feeling similar in hand.
- ShrinkCart does not control cart-to-cart physics collision, does not run penetration correction, and does not clamp vehicle velocity. Real cart collision behavior stays vanilla.
- If another scaling mod already controls the same object, ShrinkCart tries to avoid overriding it.
- Third-party map valuables are handled through vanilla `ValuableObject` recognition; non-valuable mechanisms and map structures are not forced into scaling for compatibility.
- If a client does not install ShrinkCart, they may still see or hear ScalerCore default flash/sound effects. Full-room installation is recommended.
- ShrinkCart does not take over death or revive flow. It only restores a ShrinkCart-scaled dead head before `PlayerDeathHead.Revive` when dead-head scaling is enabled. Use the separate `ExtractionReviveFix` for REPOFidelity and extraction-point revive compatibility.
- In multiplayer, shrink and restore are confirmed by the host and synced through ScalerCore. Restore after leaving the cart may wait for vanilla cart refresh plus debounce, but all clients follow the same host timing.
- Keep only one ShrinkCart DLL in the same r2modman profile. Duplicate manual installs can cause BepInEx to log skipped duplicate plugins.
