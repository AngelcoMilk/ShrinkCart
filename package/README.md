# ShrinkCart

Author: AngelcoMilk

ShrinkCart automatically shrinks supported items while they are inside a cart, then smoothly restores them when removed. It is intentionally simple: no shrink gun, no shop item, no extra UI, just a roomier cart and an optional vehicle crush instant-kill toggle.

## Features

- Auto-shrinks supported objects placed in carts.
- Smoothly restores objects after they leave the cart.
- Uses Vippy-ScalerCore for scaling animation, physics, collision handling, mass handling, and multiplayer sync.
- Uses the current game cart flow through `PhysGrabInCart.Add`.
- Skips players, enemies, carts, vehicles, equipped items, C.A.R.T. Cannon, and C.A.R.T. Laser by default.
- Optional vehicle crush instant-kill for players and enemies.

## Dependencies

- BepInExPack 5.4.2305
- Vippy-ScalerCore 0.5.2+

You do not need ShrinkerGun, REPOLib, ScaleInCart, ShrinkerCartPlus, or any older cart shrinker.

## Configuration

The config file is generated after first launch:

```text
BepInEx/config/AngelcoMilk.ShrinkCart.cfg
```

Default options:

```ini
[Cart]
Enabled = true
ScaleFactor = 0.4
ScaleSpeed = 2.5
RestoreGraceSeconds = 0.75
PreserveMass = true
ShrinkNonValuableItems = true
SuppressValuableDamageRestore = true

[VehicleCrush]
InstantKillPlayers = false
InstantKillEnemies = false

[Diagnostics]
DebugLogging = false
```

Vehicle instant-kill is disabled by default and is best kept for private lobbies.

## Multiplayer

All players should install ShrinkCart and ScalerCore. The host or single-player instance handles the cart detection, while ScalerCore handles scaling behavior and sync.

## Known Limitations

- Items already scaled by another ScalerCore-based mod are left alone.
- Equipped items are skipped to avoid inventory state issues.
- C.A.R.T. Cannon and C.A.R.T. Laser are skipped so cart tools remain usable.
- Game updates can change method names or signatures, so hooks must be rechecked after major R.E.P.O. updates.

## 中文说明

作者：AngelcoMilk

ShrinkCart 是一个新版 R.E.P.O. 轻量缩小车 mod。物品放进购物车后会自动平滑缩小，从购物车拿出来后会平滑恢复原大小。它不加入缩小枪、商店物品或额外 UI，只保留基础缩小车体验，以及一个默认关闭的车辆碾压瞬杀开关。

## 功能

- 物品放进购物车后自动平滑缩小。
- 物品从购物车拿出来后自动平滑恢复原大小。
- 使用 Vippy-ScalerCore 处理缩放动画、物理、碰撞、质量和联机同步。
- 使用新版游戏内置的 `PhysGrabInCart.Add` 作为进车触发点。
- 默认跳过玩家、敌人、购物车本体、载具、已装备物品、C.A.R.T. Cannon 和 C.A.R.T. Laser。
- 可选启用车辆碾压瞬杀玩家和敌人。

## 依赖

- BepInExPack 5.4.2305
- Vippy-ScalerCore 0.5.2+

不需要安装 ShrinkerGun、REPOLib、ScaleInCart、ShrinkerCartPlus 或其他旧版缩小车。

## 配置

首次启动后会生成配置文件：

```text
BepInEx/config/AngelcoMilk.ShrinkCart.cfg
```

常用配置：

```ini
[Cart]
Enabled = true
ScaleFactor = 0.4
ScaleSpeed = 2.5
RestoreGraceSeconds = 0.75
PreserveMass = true
ShrinkNonValuableItems = true
SuppressValuableDamageRestore = true

[VehicleCrush]
InstantKillPlayers = false
InstantKillEnemies = false

[Diagnostics]
DebugLogging = false
```

车辆碾压瞬杀默认关闭，建议只在私人房间里开启。

## 联机说明

建议所有玩家都安装 ShrinkCart 和 ScalerCore。房主或单机端负责购物车检测，ScalerCore 负责缩放行为和同步。

## 已知限制

- 已经被其他 ScalerCore mod 缩小的物体不会重复处理。
- 已装备物品会被跳过，避免背包状态异常。
- C.A.R.T. Cannon 和 C.A.R.T. Laser 会被跳过，避免影响购物车工具使用。
- 游戏大版本更新后需要重新检查 hook 方法和字段。
