# ShrinkCart

作者 / Author: **AngelcoMilk**

## 中文说明

ShrinkCart 是一个 R.E.P.O. 的购物车缩小物品 mod。把物品放进 C.A.R.T / 购物车后会自动缩小，拿出后恢复原尺寸。缩放本身由 ScalerCore 负责，因此会尽量保留它对物理、质量、网络同步和特殊物品的兼容处理。

## 功能

- 放入购物车后自动缩小支持的物品。
- 拿出购物车后自动恢复原尺寸，并提供独立的“取出后放大速度”配置。
- 按游戏新版本内置的贵重物品大小分类设置倍率：Tiny、Small、Medium、Big、Wide、Tall、VeryTall。
- 支持 ShrinkerCartPlus 风格的敌人球分类：Small、Medium、Big、Berserker。
- 支持 SurplusValuable 单独倍率。
- 普通物品或未知分类物品使用 fallback 默认倍率。
- 可选车辆碾压秒杀玩家。
- 可选车辆碾压秒杀敌人。
- 使用 REPOConfig 显示中文游戏内配置界面。

## 依赖

- BepInExPack
- ScalerCore
- REPOConfig

Thunderstore/r2modman 安装时会自动拉取依赖。手动安装时，请确保这些依赖已经放进同一个 R.E.P.O. 配置文件。

## 配置

配置界面由 REPOConfig 提供，主要配置项为中文：

- 购物车
  - 启用购物车缩小
  - 放入时缩小速度
  - 取出后放大速度
  - 取出后恢复延迟
  - 保持原始重量
  - 普通物品也缩小
  - 防止碰撞弹回原尺寸
- Tiny/Small/Medium/Big/Wide/Tall/VeryTall 分类
  - 启用此分类缩小
  - 缩小倍率
- 敌人球
  - 启用敌人球缩小
  - Small/Medium/Big/Berserker 敌人球倍率
- 特殊物品
  - 启用 Surplus 缩小
  - Surplus 倍率
- 普通或未知物品
  - 默认缩小倍率
- 车辆碾压
  - 车辆碾压秒杀玩家
  - 车辆碾压秒杀敌人

倍率含义：`0.4` 表示目标尺寸为原尺寸的 40%。如果你觉得物品太小，可以把对应分类倍率调高。

## 关于平滑放大

ShrinkCart 会在正常取出物品时调用 ScalerCore 的恢复流程，并把恢复速度改为“取出后放大速度”。如果 ScalerCore 因碰撞、安全恢复、对象禁用或同步保护选择瞬间恢复，ShrinkCart 不会强行绕过它。这样稳定性更高，也更不容易破坏物理和多人同步。

## 多人联机影响

推荐所有玩家都安装 ShrinkCart、ScalerCore 和 REPOConfig。

- 只有主机安装：主机可能可以触发缩小，但未安装的客户端可能看不到缩放，或出现视觉/碰撞不同步。
- 只有客户端安装：多人中基本不会生效，因为购物车检测和缩放触发由主机/单人侧执行。
- 车辆碾压秒杀玩家最推荐所有玩家安装，因为玩家受伤逻辑会经过 Photon 所有权检查，未安装的玩家可能只按原版伤害处理。

## 已知限制

- 缩放动画由 ScalerCore 控制，ShrinkCart 不重写底层缩放系统。
- 普通非贵重物品没有游戏内完整大小分类，默认走“普通或未知物品”倍率。
- 如果其他缩放类 mod 同时操作同一个物品，ShrinkCart 会尽量避免覆盖已经处于缩放状态的对象。

---

## English

ShrinkCart is a R.E.P.O. cart item shrinking mod by **AngelcoMilk**. Items placed inside a C.A.R.T / cart shrink automatically and restore after removal. The actual scale handling is delegated to ScalerCore for better physics, mass, special item, and networking compatibility.

## Features

- Automatically shrinks supported items while they are inside a cart.
- Restores items after removal with a separate restore speed setting.
- Configurable scale factors for the current game valuable size classes: Tiny, Small, Medium, Big, Wide, Tall, and VeryTall.
- ShrinkerCartPlus-style enemy orb categories: Small, Medium, Big, and Berserker.
- Separate SurplusValuable scaling.
- Fallback scale factor for normal or unknown items.
- Optional vehicle crush instant-kill for players.
- Optional vehicle crush instant-kill for enemies.
- Chinese in-game configuration through REPOConfig.

## Dependencies

- BepInExPack
- ScalerCore
- REPOConfig

Thunderstore/r2modman should install the dependencies automatically. For manual installs, place all dependencies in the same R.E.P.O. profile.

## Configuration

The in-game configuration is provided by REPOConfig. Scale factor values are direct size multipliers: `0.4` means 40% of the original size.

Important groups:

- Cart shrinking, shrink speed, restore speed, restore delay, mass preservation, and collision restore suppression.
- Valuable size classes: Tiny, Small, Medium, Big, Wide, Tall, VeryTall.
- Enemy orb scale factors.
- SurplusValuable scale factor.
- Fallback scale factor for normal or unknown items.
- Vehicle crush instant-kill toggles for players and enemies.

## Smooth Restore

ShrinkCart uses ScalerCore's restore path and adjusts the restore speed before restoring an item. If ScalerCore chooses an immediate restore for safety, collision, disabled objects, or networking reasons, ShrinkCart keeps that behavior instead of forcing a custom animation.

## Multiplayer

All players should install ShrinkCart, ScalerCore, and REPOConfig for the most consistent result.

- Host only: shrinking may trigger, but clients without the mod may see original sizes or mismatched collisions.
- Client only: usually no effect in multiplayer, because cart detection and scaling are host/singleplayer-side.
- Vehicle crush instant-kill is most reliable when every player has the mod, because player damage goes through Photon ownership checks.

## Known Limits

- ScalerCore owns the low-level scale behavior.
- Normal non-valuable items use the fallback factor because the game does not expose the same full size category data for them.
- If another scale mod already controls an item, ShrinkCart avoids taking over that object.
