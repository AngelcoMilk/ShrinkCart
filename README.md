# ShrinkCart v0.2.10

适配 R.E.P.O. 4.0 版本的物品缩小搬运车。

作者 / Author: **AngelcoMilk**  
Thunderstore: https://thunderstore.io/c/repo/p/AngelcoMilk/ShrinkCart/  
GitHub 文档 / GitHub Docs: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart 会在物品放入 C.A.R.T / 购物车后自动缩小，方便搬运；物品真正离开购物车一段时间后会恢复原尺寸。缩放底层由 ScalerCore 负责，ShrinkCart 负责购物车触发、分类倍率、边缘防抖、恢复冷却、隐藏缩放闪光、商店/人物用品过滤、玩家站车计时切换缩放、主机配置同步，以及可选敌人进车秒杀。

## 依赖

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.6.1`
- `nickklmao-REPOConfig-1.2.6`

Thunderstore/r2modman 安装时会自动拉取依赖。手动安装时，请确保这些依赖和 ShrinkCart 安装在同一个 R.E.P.O. 配置文件中。

## 推荐安装方式

推荐房间里所有玩家都安装 ShrinkCart 和上述依赖。主机负责决定缩放倍率、商店/人物用品是否参与缩放、离车恢复时机和敌人进车秒杀；ScalerCore 负责把实际缩放状态同步给客户端。

ScalerCore 0.6.1 提供官方运行中更新缩放选项的 API，ShrinkCart 不再反射 ScalerCore 私有字段。缩放倍率、速度、恢复速度、隐藏闪光/声音都会由主机计算后通过 ScalerCore 同步。客户端本地配置与主机不同也不会改变缩放结果；推荐所有玩家都安装 ShrinkCart、ScalerCore 0.6.1 和 REPOConfig。

## 主要功能

- **物品缩小搬运车**：把支持的物品放入购物车后自动缩小，拿出后恢复。
- **物品/武器缩放可配置**：枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店用品默认不缩小，但可以打开“商店/人物用品也缩小”让它们按 fallback 倍率缩小。
- **玩家站车计时切换缩放**：打开“商店/人物用品也缩小”后，玩家站在购物车中心区域满 3 秒会切换缩小/恢复；离开中心区域会重置计时，触发后必须离开再重新站入。
- **特殊物品支持**：支持敌人球 Small/Medium/Big/Berserker、SurplusValuable、普通未知物品 fallback 倍率。
- **贵重物分类倍率**：支持 Tiny、Small、Medium、Big、Wide、Tall、VeryTall 单独开关和倍率。
- **永久排除项**：大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。
- **R.E.P.O. 4.0 适配**：优先读取游戏新版本内置 `ValuableObject.volumeType` 来判断贵重物大小分类。
- **主机同步**：多人游戏里以主机配置为准，缩放参数通过 ScalerCore 同步，ShrinkCart 配置快照通过 Photon 房间属性同步。
- **边缘防抖**：物品在购物车边缘短暂离开时不会立刻放大，减少反复缩放抽搐。
- **隐藏缩放闪光/声音**：默认通过 ScalerCore 0.6.1 的官方选项隐藏购物车缩小/恢复时的冲击特效和恢复镜头震动。
- **车撞车优化**：不再常驻改写车辆 hurt collider，减少两辆车相撞时的额外开销。
- **可选危险功能**：可开启敌人进车秒杀，也可开启车辆碾压秒杀玩家。

## 动图演示

### 商店/人物用品默认不缩小

枪械、血包等商店购买类实用品默认不会被购物车缩小，避免影响战斗、治疗和工具使用。需要时可以在配置里打开“商店/人物用品也缩小”。

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

### 敌人球和 Surplus 会缩小

敌人球和 SurplusValuable 属于特殊物品分类，会使用单独的缩小倍率，方便和普通贵重物分开调整。

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

### 大型贵重物会缩小

大型、宽型、高型、超高型贵重物会按各自分类倍率缩小，适合搬运原本难以放进车里的物品。

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

### R.E.P.O. 4.0 特殊物品支持

R.E.P.O. 新版本的特殊物品同样可以被识别并缩小，ShrinkCart 会优先使用游戏内置分类和自己的特殊物品规则。

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

## 配置说明

配置界面由 REPOConfig 提供，主要配置项为中文：

- 购物车：启用购物车缩小、放入时缩小速度、取出后放大速度、离车防抖延迟、恢复后重新缩小冷却、保持原始重量、普通物品也缩小、商店/人物用品也缩小、玩家进车缩放倍率、玩家站车触发时间、防止碰撞弹回原尺寸。
- 视觉：隐藏缩放闪光。
- Tiny/Small/Medium/Big/Wide/Tall/VeryTall 分类：启用此分类缩小、缩小倍率。
- 敌人球：启用敌人球缩小、Small/Medium/Big/Berserker 敌人球倍率。
- 特殊物品：启用 Surplus 缩小、Surplus 倍率。
- 普通或未知物品：默认缩小倍率。
- 车辆碾压：车辆碾压秒杀玩家、敌人进车秒杀。

倍率含义：`0.4` 表示目标尺寸为原尺寸的 40%。v0.2.10 默认值已按本地测试配置调整：缩小速度 `0.6`，放大速度 `0.2`，离车防抖延迟 `0.5`，恢复后重新缩小冷却 `0.5`，商店/人物用品缩放 `false`，玩家进车缩放倍率 `0.4`，玩家站车触发时间 `3` 秒。

玩家机制说明：玩家缩放只在“商店/人物用品也缩小”开启时生效。玩家必须站在正式购物车的中心区域满 3 秒才会触发切换；第一次触发会缩小，离开车外仍保持缩小，再次站到中心区域满 3 秒会恢复原尺寸。离开中心区域会重置计时；触发后如果一直站在车上不会连续反复切换，必须离开中心区域后重新站入才会开始下一次计时。小推车/口袋推车不会触发玩家站车缩放。

## 安装（r2modman）

1. 导入或安装 ShrinkCart 包。
2. 确认 DLL 路径：
   `BepInEx/plugins/ShrinkCart/ShrinkCart.dll`
3. 确认依赖已安装：BepInExPack、ScalerCore、REPOConfig。

## 已知限制

- 缩放动画由 ScalerCore 控制，ShrinkCart 不重写底层缩放系统。
- 如果 ScalerCore 因碰撞、安全恢复、对象禁用或同步保护选择瞬间恢复，ShrinkCart 不会强行绕过它。
- 隐藏闪光和声音依赖 ScalerCore 0.6.1 的缩放选项；未安装对应依赖的玩家可能看到不一致效果。
- 如果其他缩放类 mod 已经控制同一个物品，ShrinkCart 会尽量避免覆盖已经处于缩放状态的对象。

---

# ShrinkCart v0.2.10

A shrink-hauler cart for R.E.P.O. 4.0.

Author: **AngelcoMilk**  
Thunderstore: https://thunderstore.io/c/repo/p/AngelcoMilk/ShrinkCart/  
GitHub Docs: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart automatically shrinks supported items placed inside a C.A.R.T / cart for easier hauling, then restores them after they truly leave the cart. ScalerCore handles the low-level scaling; ShrinkCart handles cart triggers, category factors, edge debounce, restore cooldowns, hidden cart-scale flashes and impact sounds, shop/player item filtering, timed player stand-toggle shrinking, host config sync, and optional enemy-in-cart instant kill.

## Dependencies

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.6.1`
- `nickklmao-REPOConfig-1.2.6`

## Recommended Multiplayer Setup

All players in the room should install ShrinkCart and its dependencies. The host decides scale factors, shop/player item filtering, restore timing, and enemy-in-cart instant kill. ScalerCore syncs the actual scale state, while ShrinkCart syncs a host config snapshot for consistent client-side visuals.

ScalerCore 0.6.1 provides an official live options update API, so ShrinkCart no longer reflects into ScalerCore private fields. The host's scale factor, shrink speed, restore speed, and quiet visual/audio options are sent through ScalerCore's multiplayer sync. Client-side config cannot override the host result.

## Features

- Shrinks supported items inside carts and restores them after removal.
- Configurable item and weapon scaling: shop utility items such as guns, health packs, melee items, grenades, tools, drones, orbs, upgrades, trackers, and mines are excluded by default, but can be enabled and scaled with the fallback factor.
- Timed player stand-toggle scaling: when "Shrink shop/player items" is enabled, standing in the cart's center zone for 3 seconds toggles player shrink/restore. Leaving the center zone resets the timer, and the player must leave and re-enter the zone before another toggle can happen.
- Supports special items such as enemy orbs, SurplusValuable, and fallback normal/unknown items.
- Per-category factors for Tiny, Small, Medium, Big, Wide, Tall, and VeryTall valuables.
- Small/big carts, C.A.R.T. Cannon, and C.A.R.T. Laser are always excluded.
- Built for R.E.P.O. 4.0, using `ValuableObject.volumeType` for valuable size categories.
- Host-authoritative scaling through ScalerCore multiplayer sync plus ShrinkCart host config snapshots.
- Cart-edge debounce to prevent rapid shrink/restore jitter.
- Hidden cart-scale impact flash, sound, and restore camera shake by default through ScalerCore 0.6.1 options.
- Reduced vehicle collision overhead by avoiding persistent hurt collider edits.
- Optional enemy-in-cart instant kill and optional player vehicle-crush instant kill.

## GIF Demos

### Shop and player utility items are excluded by default

Guns, health packs, and similar shop utility items are not shrunk by default, so combat, healing, and tool use stay predictable. Enable "Shrink shop/player items" in config if you want them to use the fallback scale factor.

![Guns and health packs are not shrunk by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

### Enemy orbs and Surplus shrink

Enemy orbs and SurplusValuable use special categories and can be tuned separately from normal valuables.

![Enemy orb and Surplus shrink demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

### Large valuables shrink

Big, wide, tall, and very tall valuables shrink by category factor, making bulky items easier to haul in the cart.

![Large item shrink demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

### R.E.P.O. 4.0 special item support

Special items from newer R.E.P.O. versions are also supported through the game's built-in classification plus ShrinkCart's special-item rules.

![R.E.P.O. 4.0 special item shrink demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

## Configuration

Configuration is provided through REPOConfig. Scale factors are direct size multipliers: `0.4` means 40% of the original size. v0.2.10 defaults now match the local test config: shrink speed `0.6`, restore speed `0.2`, cart leave debounce `0.5`, post-restore reshrink cooldown `0.5`, shop/player item scaling `false`, player cart scale factor `0.4`, player cart stand trigger time `3` seconds.

Player mechanism: player scaling only works when "Shrink shop/player items" is enabled. The player must stand in the center zone of a regular cart for 3 seconds to toggle the state. The first trigger shrinks the player, leaving the cart keeps the player small, and standing in the center zone again for 3 seconds restores the original size. Staying in the zone will not repeatedly toggle; leave and re-enter the center zone to start the next timer. Small/pocket carts do not trigger player stand-toggle scaling.

## Installation (r2modman)

1. Install or import the ShrinkCart package.
2. Confirm DLL path:
   `BepInEx/plugins/ShrinkCart/ShrinkCart.dll`
3. Confirm dependencies are installed: BepInExPack, ScalerCore, REPOConfig.

## Known Limits

- Scaling animation is controlled by ScalerCore.
- ShrinkCart does not override ScalerCore safety restores.
- Hidden flash and sound only apply to ShrinkCart cart scaling effects.
- Other scaling mods may still control objects they already scaled.
