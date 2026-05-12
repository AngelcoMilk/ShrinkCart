# ShrinkCart v0.2.4

适配 R.E.P.O. 4.0 版本的物品缩小搬运车。

作者 / Author: **AngelcoMilk**  
GitHub: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart 会在物品放入 C.A.R.T / 购物车后自动缩小，方便搬运；物品真正离开购物车一段时间后会恢复原尺寸。缩放底层由 ScalerCore 负责，ShrinkCart 负责购物车触发、分类倍率、边缘防抖、恢复冷却、隐藏缩放闪光、商店/人物用品过滤，以及可选敌人进车秒杀。

## 依赖

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.5.2`
- `nickklmao-REPOConfig-1.2.6`

Thunderstore/r2modman 安装时会自动拉取依赖。手动安装时，请确保这些依赖和 ShrinkCart 安装在同一个 R.E.P.O. 配置文件中。

## 推荐安装方式

推荐房间里所有玩家都安装 ShrinkCart 和上述依赖。主机负责决定缩放倍率、商店/人物用品是否参与缩放、离车恢复时机和敌人进车秒杀；所有人都安装时，缩放显示、隐藏闪光和配置体验最一致。

如果只有主机安装 ShrinkCart，缩放触发仍以主机为准，但客户端至少需要 ScalerCore 才能可靠看到缩放效果。如果客户端没有安装 ShrinkCart，可能仍会看到 ScalerCore 默认的冲击闪光。

## 主要功能

- **物品缩小搬运车**：把支持的物品放入购物车后自动缩小，拿出后恢复。
- **物品/武器缩放可配置**：枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店/人物用品默认不缩小，但可以打开“商店/人物用品也缩小”让它们按 fallback 倍率缩小。
- **特殊物品支持**：支持敌人球 Small/Medium/Big/Berserker、SurplusValuable、普通未知物品 fallback 倍率。
- **贵重物分类倍率**：支持 Tiny、Small、Medium、Big、Wide、Tall、VeryTall 单独开关和倍率。
- **永久排除项**：大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。
- **R.E.P.O. 4.0 适配**：优先读取游戏新版本内置 `ValuableObject.volumeType` 来判断贵重物大小分类。
- **主机同步**：多人游戏里以主机配置为准，缩放参数通过 ScalerCore 同步。
- **边缘防抖**：物品在购物车边缘短暂离开时不会立刻放大，减少反复缩放抽搐。
- **隐藏缩放闪光**：默认隐藏购物车缩小/恢复时的 ScalerCore 冲击闪光，不关闭普通碰撞特效。
- **可选危险功能**：可开启敌人进车秒杀，也可开启车辆碾压秒杀玩家。

## 配置说明

配置界面由 REPOConfig 提供，主要配置项为中文：

- 购物车：启用购物车缩小、放入时缩小速度、取出后放大速度、离车防抖延迟、恢复后重新缩小冷却、保持原始重量、普通物品也缩小、商店/人物用品也缩小、防止碰撞弹回原尺寸。
- 视觉：隐藏缩放闪光。
- Tiny/Small/Medium/Big/Wide/Tall/VeryTall 分类：启用此分类缩小、缩小倍率。
- 敌人球：启用敌人球缩小、Small/Medium/Big/Berserker 敌人球倍率。
- 特殊物品：启用 Surplus 缩小、Surplus 倍率。
- 普通或未知物品：默认缩小倍率。
- 车辆碾压：车辆碾压秒杀玩家、敌人进车秒杀。

倍率含义：`0.4` 表示目标尺寸为原尺寸的 40%。v0.2.4 默认值：缩小速度 `0.9`，放大速度 `0.55`，离车防抖延迟 `2.5`，恢复后重新缩小冷却 `0.5`，商店/人物用品缩放 `false`。

## 安装（r2modman）

1. 导入或安装 ShrinkCart 包。
2. 确认 DLL 路径：
   `BepInEx/plugins/ShrinkCart/ShrinkCart.dll`
3. 确认依赖已安装：BepInExPack、ScalerCore、REPOConfig。

## 已知限制

- 缩放动画由 ScalerCore 控制，ShrinkCart 不重写底层缩放系统。
- 如果 ScalerCore 因碰撞、安全恢复、对象禁用或同步保护选择瞬间恢复，ShrinkCart 不会强行绕过它。
- 隐藏闪光只屏蔽 ShrinkCart 购物车缩放流程里的 ScalerCore 冲击特效。
- 如果其他缩放类 mod 已经控制同一个物品，ShrinkCart 会尽量避免覆盖已经处于缩放状态的对象。

---

# ShrinkCart v0.2.4

A shrink-hauler cart for R.E.P.O. 4.0.

Author: **AngelcoMilk**  
GitHub: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart automatically shrinks supported items placed inside a C.A.R.T / cart for easier hauling, then restores them after they truly leave the cart. ScalerCore handles the low-level scaling; ShrinkCart handles cart triggers, category factors, edge debounce, restore cooldowns, hidden cart-scale flashes, shop/player item filtering, and optional enemy-in-cart instant kill.

## Dependencies

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.5.2`
- `nickklmao-REPOConfig-1.2.6`

## Recommended Multiplayer Setup

All players in the room should install ShrinkCart and its dependencies. The host decides scale factors, shop/player item filtering, restore timing, and enemy-in-cart instant kill; installing it for everyone gives the most consistent visuals and config behavior.

## Features

- Shrinks supported items inside carts and restores them after removal.
- Configurable item and weapon scaling: shop/player utility items such as guns, health packs, melee items, grenades, tools, drones, orbs, upgrades, trackers, and mines are excluded by default, but can be enabled and scaled with the fallback factor.
- Supports special items such as enemy orbs, SurplusValuable, and fallback normal/unknown items.
- Per-category factors for Tiny, Small, Medium, Big, Wide, Tall, and VeryTall valuables.
- Small/big carts, C.A.R.T. Cannon, and C.A.R.T. Laser are always excluded.
- Built for R.E.P.O. 4.0, using `ValuableObject.volumeType` for valuable size categories.
- Host-authoritative scaling through ScalerCore multiplayer sync.
- Cart-edge debounce to prevent rapid shrink/restore jitter.
- Hidden cart-scale impact flash by default.
- Optional enemy-in-cart instant kill and optional player vehicle-crush instant kill.

## Configuration

Configuration is provided through REPOConfig. Scale factors are direct size multipliers: `0.4` means 40% of the original size. v0.2.4 defaults: shrink speed `0.9`, restore speed `0.55`, cart leave debounce `2.5`, post-restore reshrink cooldown `0.5`, shop/player item scaling `false`.

## Installation (r2modman)

1. Install or import the ShrinkCart package.
2. Confirm DLL path:
   `BepInEx/plugins/ShrinkCart/ShrinkCart.dll`
3. Confirm dependencies are installed: BepInExPack, ScalerCore, REPOConfig.

## Known Limits

- Scaling animation is controlled by ScalerCore.
- ShrinkCart does not override ScalerCore safety restores.
- Hidden flash only applies to ShrinkCart cart scaling effects.
- Other scaling mods may still control objects they already scaled.
