# ShrinkCart v0.2.16

适配 R.E.P.O. 4.0 版本的物品缩小搬运车。

作者 / Author: **AngelcoMilk**  
Thunderstore: https://thunderstore.io/c/repo/p/AngelcoMilk/ShrinkCart/  
GitHub / Source: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart 会在物品放入 C.A.R.T / 购物车后自动缩小，方便搬运；物品真正离开购物车一段时间后会恢复原尺寸。底层缩放由 ScalerCore 负责，ShrinkCart 负责购物车触发、分类倍率、边缘防抖、恢复冷却、商店用品过滤、玩家站车缩放、主机配置同步，以及可选的敌人进车秒杀。

## 依赖

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.6.1`
- `nickklmao-REPOConfig-1.2.6`

Thunderstore/r2modman 安装时会自动拉取依赖。mod 管理器的跳转按钮来自 manifest 的 `website_url`，因此会打开 Thunderstore 详情页；源码地址请看上方 GitHub 链接。推荐房间里所有玩家都安装 ShrinkCart 和这些依赖，缩放表现和特效隐藏最一致。

## 主要功能

- **购物车缩小搬运**：支持的物品放入购物车后自动缩小，拿出并离车后恢复。
- **可配置物品/武器缩放**：枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店用品默认不缩小，可在配置中打开并使用商店用品倍率。
- **玩家站车缩放**：玩家缩放默认开启；玩家必须实际站在正式购物车中心区域一段时间才会切换缩小/恢复，离开中心区域会重置计时。
- **代币/外观箱修复**：新版本抽奖/代币类箱子包含 `CosmeticWorldObject` 与 `ItemValuableBox`，会走“代币/外观箱”独立开关和倍率，默认倍率 `0.4`。
- **特殊物品支持**：敌人球 Small/Medium/Big/Berserker 使用独立倍率；钱袋/Surplus 使用独立倍率；普通非贵重物默认不缩小。
- **车辆冲突保护**：大小推车、车辆、C.A.R.T. Cannon、C.A.R.T. Laser 永远不缩小，并且会被排除出购物车 in-cart 处理，减少两辆车互相卡住。
- **死亡安全恢复**：由 ShrinkCart 缩小的玩家在死亡/复活流程前会自动恢复原尺寸，减少死亡头颅、Instant Revive、复活类 mod 的冲突。
- **主机权威同步**：多人游戏中由主机配置决定缩放倍率、速度、玩家缩放、特殊物品和恢复时机；客户端不会用自己的配置覆盖结果。
- **隐藏缩放特效**：默认通过 ScalerCore 0.6.1 的官方选项隐藏购物车缩放闪光、冲击声和恢复镜头震动。

## 动图演示

### 商店用品默认不缩小

枪械、血包等商店购买类实用品默认不会被购物车缩小，避免影响战斗、治疗和工具使用。需要时可以在配置里打开“商店用品也缩小”。玩家缩放是单独开关，默认开启。

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

### 敌人球和 Surplus 会缩小

敌人球使用独立的敌人球配置；钱袋/SurplusValuable 使用特殊物品配置。

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

### 大型贵重物会缩小

大型、宽型、高型、超高型贵重物会按各自分类倍率缩小，适合搬运原本难以放进车里的物品。

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

### R.E.P.O. 4.0 特殊物品支持

R.E.P.O. 新版本的特殊物品同样可以被识别并缩小；v0.2.16 额外适配了 `CosmeticWorldObject` 代币/外观箱。

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

## 配置说明

配置界面由 REPOConfig 提供，主要配置项为中文：

- 购物车：启用购物车缩小、缩小/放大速度、离车防抖、恢复冷却、保持原始重量、商店用品也缩小、玩家也缩小、玩家缩放倍率、玩家站车触发时间、玩家死亡前自动恢复、防止碰撞弹回原尺寸。
- 视觉：隐藏缩放闪光。
- 贵重物分类：Tiny、Small、Medium、Big、Wide、Tall、VeryTall 各自开关和倍率。
- 敌人球：启用敌人球缩小、Small/Medium/Big/Berserker 倍率。
- 特殊物品：钱袋/Surplus 开关和倍率、代币/外观箱开关和倍率。
- 商店用品：商店用品缩小倍率。
- 车辆碾压：车辆碾压秒杀玩家、敌人进车秒杀。

默认值：缩小速度 `0.5`，放大速度 `0.2`，离车防抖 `0.5`，恢复冷却 `0.5`，商店用品缩放 `false`，玩家缩放 `true`，玩家缩放倍率 `0.55`，玩家站车触发时间 `2` 秒，玩家死亡前自动恢复 `true`。贵重物倍率：Tiny `0.8`，Small `0.6`，Medium `0.45`，Big `0.4`，Wide `0.35`，Tall `0.35`，VeryTall `0.25`。敌人球倍率：Small `0.8`，Medium `0.65`，Big `0.45`，Berserker `0.45`。商店用品倍率 `0.5`，钱袋/Surplus `0.25`，代币/外观箱 `0.4`。

## 已知限制

- 缩放动画由 ScalerCore 控制，ShrinkCart 不重写底层缩放系统。
- 如果其他缩放类 mod 已经控制同一个对象，ShrinkCart 会尽量避免覆盖该对象。
- 客户端未安装 ShrinkCart 时，仍可能看到 ScalerCore 默认闪光/声音；全员安装效果最稳定。
- 日志中如果死亡/复活栈仍指向 Instant Revive 或死亡头颅类 mod，需要用“关闭玩家缩放”和“关闭复活类 mod”分别 A/B 测试来定位。

---

# ShrinkCart v0.2.16

A shrink-hauler cart for R.E.P.O. 4.0.

Author: **AngelcoMilk**  
Thunderstore: https://thunderstore.io/c/repo/p/AngelcoMilk/ShrinkCart/  
GitHub / Source: https://github.com/AngelcoMilk/ShrinkCart

ShrinkCart shrinks supported objects placed in carts, restores them after removal, and keeps scaling host-authoritative through ScalerCore.

## Features

- Configurable valuable, enemy orb, Surplus, token/cosmetic box, shop item, and player scaling.
- Token/cosmetic boxes include both `CosmeticWorldObject` and `ItemValuableBox`; default factor is `0.4`.
- Shop utility items and weapons are excluded by default, but can be enabled with the shop item factor.
- Carts, vehicles, C.A.R.T. Cannon, and C.A.R.T. Laser are never shrunk and are blocked from cart-in-cart processing to reduce cart collision sticking.
- ShrinkCart-scaled players are restored before death/revive flows to reduce death-head and revive-mod conflicts.
- All scale decisions are host-authoritative; clients do not override host scale settings.

## Dependencies

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-0.6.1`
- `nickklmao-REPOConfig-1.2.6`

All players should install ShrinkCart and its dependencies for the most consistent multiplayer experience.
