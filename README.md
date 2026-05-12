# ShrinkCart

Author: AngelcoMilk

ShrinkCart is a lightweight R.E.P.O. mod that automatically shrinks supported items while they are inside a cart, then smoothly restores them when they are removed. It keeps the feature set intentionally small: no shrink gun, no shop item, no extra UI, just a roomier cart and an optional vehicle crush instant-kill toggle.

## Features

- Automatically shrinks supported objects when they are placed in a cart.
- Smoothly restores objects to normal size after they leave the cart.
- Uses `Vippy-ScalerCore` for scaling animation, physics, collision handling, mass handling, and multiplayer sync.
- Hooks the current game cart flow through `PhysGrabInCart.Add`, which is called by `PhysGrabObjectImpactDetector.OnTriggerStay` when an object is actually inside a cart trigger.
- Skips players, enemies, carts, vehicles, equipped items, C.A.R.T. Cannon, and C.A.R.T. Laser by default.
- Optional vehicle crush instant-kill for players.
- Optional vehicle crush instant-kill for enemies.

## Dependencies

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.5.2`

You do not need ShrinkerGun, REPOLib, ScaleInCart, ShrinkerCartPlus, or any older cart shrinker.

## Installation

Install with r2modman, Thunderstore Mod Manager, or manually by placing the package contents in your R.E.P.O. profile.

Manual layout:

```text
BepInEx/plugins/ShrinkCart/ShrinkCart.dll
```

All players should install ShrinkCart and ScalerCore when playing together. The host or single-player instance drives the shrink trigger.

## Configuration

The config file is generated after the first launch:

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

`ScaleFactor` controls the shrunken size. `0.4` means items shrink to 40% of their normal size.

`RestoreGraceSeconds` is a short buffer before restoring an item after it stops being detected in a cart. This prevents flicker from brief cart trigger updates.

`PreserveMass` keeps item mass unchanged while the object is visually smaller, so carts still feel physically fair.

`SuppressValuableDamageRestore` prevents valuables from expanding just because they bump into other objects while inside the cart.

Vehicle instant-kill options are disabled by default. They are intended for private lobbies or custom rule sets.

## Compatibility Notes

- Built for newer R.E.P.O. versions using `PhysGrabInCart.Add` and current `ItemVehicle` hurt collider behavior.
- ScalerCore handles the risky parts of scaling, including smooth animation and multiplayer state.
- This mod does not add or modify a shrink gun.
- This mod does not modify Photon ownership or force extra network state outside ScalerCore behavior.

## Known Limitations

- Items already scaled by another ScalerCore-based mod are left alone.
- Equipped items are skipped to avoid inventory state issues.
- C.A.R.T. Cannon and C.A.R.T. Laser are skipped so cart tools remain usable.
- Game updates can change method names or signatures; the build script validates the important hooks before compiling.

## Build

Default paths:

```text
Game: D:\SteamLibrary\steamapps\common\REPO
r2modman Profile: %APPDATA%\r2modmanPlus-local\REPO\profiles\REPO
ScalerCore: %APPDATA%\r2modmanPlus-local\REPO\cache\Vippy-ScalerCore\...\ScalerCore.dll
```

Build:

```powershell
.\build.ps1
```

Build and install to the selected r2modman profile:

```powershell
.\build.ps1 -InstallToProfile
```

Build and package to desktop:

```powershell
.\build.ps1 -PackageToDesktop
```

Output:

```text
dist/BepInEx/plugins/ShrinkCart/ShrinkCart.dll
```

## 中文说明

作者：AngelcoMilk

ShrinkCart 是一个面向新版 R.E.P.O. 的轻量缩小车 mod。它会在物品放入购物车后自动平滑缩小，并在物品拿出购物车后平滑恢复原大小。功能刻意保持简单：没有缩小枪、没有商店物品、没有额外 UI，只保留基础缩小车体验，以及一个默认关闭的车辆碾压瞬杀开关。

## 功能

- 物品放进购物车后自动平滑缩小。
- 物品从购物车拿出来后自动平滑恢复原大小。
- 使用 `Vippy-ScalerCore` 处理缩放动画、物理、碰撞、质量和联机同步。
- 缩小触发点使用新版游戏内置流程：`PhysGrabObjectImpactDetector.OnTriggerStay` 确认物品在购物车 trigger 内后调用 `PhysGrabInCart.Add`。
- 默认跳过玩家、敌人、购物车本体、载具、已装备物品、C.A.R.T. Cannon 和 C.A.R.T. Laser。
- 可选启用车辆碾压瞬杀玩家。
- 可选启用车辆碾压瞬杀敌人。

## 依赖

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.5.2`

不需要安装 ShrinkerGun、REPOLib、ScaleInCart、ShrinkerCartPlus 或其他旧版缩小车。

## 安装

推荐使用 r2modman 或 Thunderstore Mod Manager 安装。

手动安装结构：

```text
BepInEx/plugins/ShrinkCart/ShrinkCart.dll
```

联机时建议所有玩家都安装 ShrinkCart 和 ScalerCore。缩小触发由房主或单机端负责。

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

`ScaleFactor` 是缩小后的比例。`0.4` 表示缩到原大小的 40%。

`RestoreGraceSeconds` 是物品离开购物车检测后等待恢复的缓冲时间，用来避免 trigger 短暂刷新造成反复缩放。

`PreserveMass` 会让物品视觉变小但保持原始质量，让购物车重量表现更稳定。

`SuppressValuableDamageRestore` 会避免贵重物品在车里互相碰撞时突然恢复原大小。

车辆碾压瞬杀默认关闭，建议只在私人房间或自定义规则中开启。

## 兼容与限制

- 已经被其他 ScalerCore mod 缩小的物体不会重复处理。
- 已装备物品会被跳过，避免背包状态异常。
- C.A.R.T. Cannon 和 C.A.R.T. Laser 会被跳过，避免影响购物车工具使用。
- 不修改 Photon ownership，不强制同步额外网络状态。
- 游戏更新后需要重新校验 `PhysGrabInCart.Add`、`PhysGrabObjectImpactDetector.OnTriggerStay`、`ItemVehicle` 和 `HurtCollider` 的方法签名。
