# ShrinkCart

---

<details open>
<summary><b>中文 / Chinese</b> (click to expand English)</summary>

<br>
适配 R.E.P.O. v0.4.4.x 的购物车缩小搬运模组——物品进车自动缩小，离车恢复原尺寸
> ShrinkCart 会在物品放入 C.A.R.T / 购物车后自动缩小，方便搬运；物品真正离开购物车后会恢复原尺寸。底层缩放由 ScalerCore 负责，ShrinkCart 负责购物车触发、分类倍率、边缘防抖、恢复冷却、商店用品过滤、车类载物过滤、主机决策，以及可选的玩家站车缩小/增大循环、死亡头颅进车缩小和敌人进车秒杀。

---

### 功能

**购物车缩小搬运**

- 支持的物品放入 C.A.R.T / 购物车后自动缩小，拿出并离车后恢复
- 缩放由 [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) 提供

**可配置物品缩放**

- 枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品默认保持原尺寸，不影响战斗、治疗和工具使用
- 敌人球和钱袋使用独立倍率
- 普通非贵重物默认不缩小

**玩家缩放**

- 默认开启。在购物车中心站一会儿，角色自动循环「原样 → 缩小 → 原样 → 增大 → 原样」
- 不需要时随时在配置中关闭

**敌人进车秒杀**

- 默认开启，进车的敌人立刻死亡

**主机决策 + [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) 同步**

- 多人游戏中只有主机/单人执行缩小和恢复决策
- 最终大小由 [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) 同步给客户端

---

### 动图演示

#### 商店用品默认不缩小

枪械、血包等商店购买类实用品默认不会被购物车缩小，避免影响战斗、治疗和工具使用

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### 敌人球和钱袋会缩小

敌人球和钱袋各有独立的缩小倍率，互不影响

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### 大型贵重物会缩小

大型、宽型、高型、超高型贵重物都会缩小，方便搬运原本难以放进车里的物品

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### 新版本代币/外观箱适配

已适配 R.E.P.O. v0.4.x 新增的代币箱与外观箱

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

---

### 游戏内设置

配置界面由 REPOConfig 提供，可在「语言 / Language」分组切换为英文：

| 分组 | 主要设置项 |
|:---|:---|
| 购物车 | 启用购物车缩小、放入时缩小速度、取出后放大速度、离车防抖延迟、恢复后重新缩小冷却、启用重量随缩放降低、启用商店用品缩小、防止碰撞弹回原尺寸 |
| 玩家缩放 | 启用玩家缩放、玩家进车缩放倍率、玩家进车增大倍率、玩家站车触发时间、玩家离车判定宽容时间、玩家检测间隔、启用玩家受伤后自动恢复 |
| 死亡头颅 | 启用死亡头颅进车缩小、死亡头颅缩小倍率 |
| 贵重物分类 | Tiny、Small、Medium、Big、Wide、Tall、VeryTall 各自开关和倍率 |
| 敌人球 | 启用敌人球缩小、Small/Medium/Big/Berserker 倍率 |
| 特殊物品 | 钱袋/Surplus 开关和倍率、代币/外观箱开关和倍率 |
| 商店用品 | 商店用品缩小倍率 |
| 车辆碾压 | 敌人进车秒杀 |
| 性能 | 启用动态物品扫描、最小/最大物品扫描间隔 |
| 诊断 | 启用调试日志 |
| 语言 / Language | 配置语言，默认中文 |

---

### 配置默认值

> 核心项如下，完整配置见 `AngelcoMilk.ShrinkCart.cfg`

| 配置项 | 默认值 |
|:---|---:|
| 购物车缩小 | 开启 |
| 缩小 / 放大速度 | `0.5` / `0.2` |
| 商店用品缩小 | 关闭 |
| 玩家站车缩放 | 开启（缩小 `0.55` / 增大 `1.5`） |
| 死亡头颅进车缩小 | 关闭 |
| 贵重物缩小倍率 | `0.25` ~ `0.8`（按体型分档） |
| 商店用品倍率 | `0.5` |
| 敌人球倍率 | `0.45` ~ `0.8`（按体型分档） |
| 钱袋/Surplus 倍率 | `0.25` |
| 代币/外观箱倍率 | `0.4` |
| 敌人进车秒杀 | 开启 |
| 语言 | `中文` |

---

### 安装

**推荐**：通过 Thunderstore Mod Manager / r2modman 安装（依赖 `BepInEx-BepInExPack-5.4.2305`、[Vippy-ScalerCore-1.0.4](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/)、`nickklmao-REPOConfig-1.2.6`）

**手动安装**：

```
1. 安装 BepInEx 5 及上述依赖
2. 将 ShrinkCart.dll 放入 BepInEx/plugins/ShrinkCart/
3. 启动游戏
```

配置文件路径：`BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### 联机说明

> **Host 必须安装**，客户端推荐安装，全员安装时体验最一致

---

### 配置文件

```ini
[购物车]
CartShrinkingEnabled = true
CartScaleSpeed = 0.5
RestoreScaleSpeed = 0.2
CartLeaveDebounceSeconds = 0.5
ReshrinkCooldownSeconds = 0.5
ScaleMassWithSize = true
ShrinkShopPlayerItems = false
SuppressValuableDamageRestore = true

[玩家缩放]
PlayerScalingModuleEnabled = true
PlayerCartScaleFactor = 0.55
PlayerCartGrowFactor = 1.5
PlayerCartStandTriggerSeconds = 2
PlayerCartExitGraceSeconds = 0.6
RestorePlayerOnDamage = true

[死亡头颅]
DeadHeadScalingEnabled = false
DeadHeadScaleFactor = 0.5

[Tiny]
TinyEnabled = true
TinyScaleFactor = 0.8

[敌人球]
EnemyOrbEnabled = true
EnemyOrbSmallScaleFactor = 0.8
EnemyOrbMediumScaleFactor = 0.65
EnemyOrbBigScaleFactor = 0.45
EnemyOrbBerserkerScaleFactor = 0.45

[特殊物品]
SurplusEnabled = true
SurplusScaleFactor = 0.25
ValuableBoxEnabled = true
ValuableBoxScaleFactor = 0.4

[商店用品]
FallbackScaleFactor = 0.5

[车辆碾压]
EnemyInCartInstantKill = true

[性能]
DynamicItemScanEnabled = true
MinimumItemScanIntervalSeconds = 0.15
MaximumItemScanIntervalSeconds = 1.0

[诊断]
DebugLogging = false

[语言 / Language]
配置语言 / Config Language = 中文
```

### 致谢

累积下载突破 `100K`，感谢每一位使用者与社区的持续付出 (◍•ᴗ•◍)❤
遇到问题欢迎来 [GitHub Issues](https://github.com/AngelcoMilk/ShrinkCart/issues) 反馈，这个模组会持续维护下去

`BepInEx` · `Harmony` · `ScalerCore` · `REPOConfig` · `Thunderstore`

</details>

---

<details>
<summary><b>English</b> (click to expand)</summary>

<br>
A cart-scaling hauling mod for R.E.P.O. v0.4.4.x — items shrink automatically when placed into the cart and restore to full size after leaving
> ShrinkCart shrinks supported objects when placed into a C.A.R.T. / shopping cart and restores them after they truly leave. ScalerCore handles the actual scaling; ShrinkCart manages cart triggers, category factors, edge debounce, restore cooldowns, shop-item filtering, cart-object filtering, host-side decisions, and optional player shrink/grow cycling, dead-head cart shrinking, and enemy-in-cart instant kill.

---

### Features

**Cart Shrink Hauling**

- Supported objects shrink when placed into a C.A.R.T. / shopping cart and restore after leaving
- [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) handles the scaling. This mod watches one thing: item enters the cart? Shrink. Leaves for real? Restore.

**Configurable Item Scaling**

- Guns, medkits, melee items, grenades, tools, drones, pearls, upgrades, trackers, mines, and similar shop purchases keep their original size by default — combat, healing, and tool use stay unaffected. Enable "shop item shrinking" in config to apply the shop-item factor
- Newer token / cosmetic boxes have their own toggle and factor, default `0.4`
- Enemy orbs use independent factors for Small / Medium / Big / Berserker; money bags / Surplus use their own factor
- Ordinary non-valuable objects do not shrink by default

**Player Scaling**

- On by default. Stand in the cart center to cycle normal → shrink → normal → grow → normal
- Turn it off whenever you want

**Enemy Instant Kill in Cart**

- On by default — enemies die when entering a cart

**Host Decisions + [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) Sync**

- In multiplayer, only the host / singleplayer side decides shrink and restore
- [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) syncs the final size to all clients

---

### GIF Demos

#### Shop Items Do Not Shrink By Default

Shop items such as guns and medkits are not shrunk by default, so combat, healing, and tool use stay unaffected

![Shop items not scaled by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### Enemy Orbs And Surplus Shrink

Enemy orbs and money bags each have their own scale factors

![Enemy orb and Surplus scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### Large Valuables Shrink

Large, wide, tall, and very tall valuables all shrink — easy to haul

![Large item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### Token & Cosmetic Box Support

Supports token boxes and cosmetic boxes introduced in R.E.P.O. v0.4.x

![R.E.P.O. 4.0 special item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

---

### In-Game Settings

Configuration is provided through REPOConfig. Switch to English in the "Language" group if preferred:

| Group | Main settings |
|:---|:---|
| Cart | enable cart shrinking, shrink speed, restore speed, leave-cart debounce, reshrink cooldown, mass scaling with size, shop-item scaling, suppress collision restore |
| Player Scaling | enable player scaling, player cart scale factor, player cart grow factor, stand trigger time, exit grace time, detection interval, restore player on damage |
| Death Heads | enable dead-head cart shrinking, dead-head scale factor |
| Valuable Categories | toggles and factors for Tiny, Small, Medium, Big, Wide, Tall, VeryTall |
| Enemy Orbs | enemy-orb toggle plus Small / Medium / Big / Berserker factors |
| Special Objects | money bag / Surplus toggle and factor, token/cosmetic box toggle and factor |
| Shop Items | shop-item scale factor |
| Cart Crush | instant-kill enemies in cart |
| Performance | dynamic item scanning, min/max scan intervals |
| Diagnostics | debug logging |
| Language | config language, default Chinese |

---

### Default Values

> Key settings below — see `AngelcoMilk.ShrinkCart.cfg` for the full list

| Setting | Default |
|:---|---:|
| Cart shrinking | On |
| Shrink / restore speed | `0.5` / `0.2` |
| Shop-item shrinking | Off |
| Player cart scaling | On (shrink `0.55` / grow `1.5`) |
| Dead-head cart shrinking | Off |
| Valuable scale range | `0.25` ~ `0.8` (by size category) |
| Shop-item factor | `0.5` |
| Enemy-orb scale range | `0.45` ~ `0.8` (by size) |
| Money bag / Surplus factor | `0.25` |
| Token/cosmetic box factor | `0.4` |
| Enemy instant kill in cart | On |
| Language | `中文` |

---

### Installation

**Recommended**: Install via Thunderstore Mod Manager / r2modman (requires `BepInEx-BepInExPack-5.4.2305`, [Vippy-ScalerCore-1.0.4](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/), `nickklmao-REPOConfig-1.2.6`)

**Manual**:

```
1. Install BepInEx 5 and the dependencies above
2. Place ShrinkCart.dll in BepInEx/plugins/ShrinkCart/
3. Launch the game
```

Config file: `BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### Multiplayer

> **Host must install**; clients are recommended for the most consistent experience

---

### Configuration

```ini
[Cart]
CartShrinkingEnabled = true
CartScaleSpeed = 0.5
RestoreScaleSpeed = 0.2
CartLeaveDebounceSeconds = 0.5
ReshrinkCooldownSeconds = 0.5
ScaleMassWithSize = true
ShrinkShopPlayerItems = false
SuppressValuableDamageRestore = true

[Player Scaling]
PlayerScalingModuleEnabled = true
PlayerCartScaleFactor = 0.55
PlayerCartGrowFactor = 1.5
PlayerCartStandTriggerSeconds = 2
PlayerCartExitGraceSeconds = 0.6
RestorePlayerOnDamage = true

[Death Heads]
DeadHeadScalingEnabled = false
DeadHeadScaleFactor = 0.5

[Tiny]
TinyEnabled = true
TinyScaleFactor = 0.8

[Enemy Orbs]
EnemyOrbEnabled = true
EnemyOrbSmallScaleFactor = 0.8
EnemyOrbMediumScaleFactor = 0.65
EnemyOrbBigScaleFactor = 0.45
EnemyOrbBerserkerScaleFactor = 0.45

[Special Objects]
SurplusEnabled = true
SurplusScaleFactor = 0.25
ValuableBoxEnabled = true
ValuableBoxScaleFactor = 0.4

[Shop Items]
FallbackScaleFactor = 0.5

[Cart Crush]
EnemyInCartInstantKill = true

[Performance]
DynamicItemScanEnabled = true
MinimumItemScanIntervalSeconds = 0.15
MaximumItemScanIntervalSeconds = 1.0

[Diagnostics]
DebugLogging = false

[Language]
Config Language = 中文
```

### Credits

Over 100,000 downloads — thank you for the trust and community support (◍•ᴗ•◍)❤
Found an issue? Open one on [GitHub](https://github.com/AngelcoMilk/ShrinkCart/issues) — this mod is actively maintained

`BepInEx` · `Harmony` · `ScalerCore` · `REPOConfig` · `Thunderstore`

</details>
