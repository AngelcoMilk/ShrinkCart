# ShrinkCart

作者 / Author: **AngelcoMilk**

## 中文说明

ShrinkCart 是一个 R.E.P.O. 的购物车缩小物品 mod。把物品放进 C.A.R.T / 购物车后会自动缩小，拿出后恢复原尺寸。缩放本身由 ScalerCore 负责，ShrinkCart 只负责购物车触发、分类倍率、恢复冷却和可选的敌人进车秒杀。

## 功能

- 放入购物车后自动缩小支持的物品。
- 拿出购物车后自动恢复原尺寸，并提供独立的“取出后放大速度”配置。
- 恢复后会短暂冷却，避免物品刚放大又立刻被购物车触发缩小。
- 按游戏新版本内置的贵重物品大小分类设置倍率：Tiny、Small、Medium、Big、Wide、Tall、VeryTall。
- 支持 ShrinkerCartPlus 风格的敌人球分类：Small、Medium、Big、Berserker。
- 支持 SurplusValuable 单独倍率。
- 普通物品或未知分类物品使用 fallback 默认倍率。
- 可选车辆碾压秒杀玩家。
- 可选敌人进入购物车秒杀，复刻 ShrinkerCartPlus 的“Instant Kill Enemy In Cart”思路。
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
  - 敌人进车秒杀

倍率含义：`0.4` 表示目标尺寸为原尺寸的 40%。v0.2.1 的默认值参考本地配置：缩小速度 `0.9`，放大速度 `0.55`，恢复延迟 `0.5`。

## 多人同步

多人游戏里，缩放触发只在主机/单人侧执行。主机用自己的配置计算倍率，然后通过 ScalerCore 同步缩放参数，所以新进入购物车的物品以主机倍率为准。

推荐所有玩家都安装 ShrinkCart、ScalerCore 和 REPOConfig。至少客户端需要 ScalerCore 才能稳定看到缩放效果。只有客户端安装 ShrinkCart、主机没安装时，购物车缩小基本不会生效。

## 关于平滑放大

ShrinkCart 会在正常取出物品时调用 ScalerCore 的恢复流程，并把恢复速度改为“取出后放大速度”。如果 ScalerCore 因碰撞、安全恢复、对象禁用或同步保护选择瞬间恢复，ShrinkCart 不会强行绕过它。这样稳定性更高，也更不容易破坏物理和多人同步。

## 性能说明

v0.2.1 减少了每帧工作：

- 购物车内物品恢复检测改为定时检查。
- 分类和倍率会缓存，配置变化后才重新计算。
- 车辆 hurt collider 不再每帧刷新，只在车辆生成或配置变化时更新。
- 恢复后冷却会自动清理过期记录。

## 已知限制

- 缩放动画由 ScalerCore 控制，ShrinkCart 不重写底层缩放系统。
- 普通非贵重物品没有游戏内完整大小分类，默认走“普通或未知物品”倍率。
- 如果其他缩放类 mod 已经控制同一个物品，ShrinkCart 会尽量避免覆盖已经处于缩放状态的对象。
- 敌人进车秒杀由主机执行；多人里建议所有玩家安装以减少观感差异。

---

## English

ShrinkCart is a R.E.P.O. cart item shrinking mod by **AngelcoMilk**. Items placed inside a C.A.R.T / cart shrink automatically and restore after removal. ScalerCore owns the low-level scale behavior; ShrinkCart handles cart triggers, category factors, restore cooldowns, and optional enemy-in-cart instant kill.

## Features

- Automatically shrinks supported items while they are inside a cart.
- Restores items after removal with a separate restore speed setting.
- Adds a short post-restore cooldown so an item does not immediately shrink again while it is expanding.
- Configurable scale factors for Tiny, Small, Medium, Big, Wide, Tall, and VeryTall valuables.
- ShrinkerCartPlus-style enemy orb categories: Small, Medium, Big, and Berserker.
- Separate SurplusValuable scaling.
- Fallback scale factor for normal or unknown items.
- Optional vehicle crush instant-kill for players.
- Optional instant kill when enemies enter the cart.
- Chinese in-game configuration through REPOConfig.

## Multiplayer

Scaling is host-authoritative. In multiplayer, only the host/singleplayer side triggers cart shrinking and computes scale factors. ScalerCore then synchronizes the scale parameters to clients.

All players should install ShrinkCart, ScalerCore, and REPOConfig for the most consistent result. At minimum, clients need ScalerCore to reliably see the scale changes. Client-only ShrinkCart installs usually do not affect multiplayer carts.

## Configuration

The in-game configuration is provided by REPOConfig. Scale factor values are direct size multipliers: `0.4` means 40% of the original size. v0.2.1 defaults are based on the local config used during development: shrink speed `0.9`, restore speed `0.55`, restore delay `0.5`.

## Smooth Restore

ShrinkCart uses ScalerCore's restore path and adjusts the restore speed before restoring an item. If ScalerCore chooses an immediate restore for safety, collision, disabled objects, or networking reasons, ShrinkCart keeps that behavior instead of forcing a custom animation.
