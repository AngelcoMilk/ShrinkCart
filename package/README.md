# ShrinkCart v0.2.47

---

<details open>
<summary><b>中文 / Chinese</b> (click to expand English)</summary>

<br>

> 适配 R.E.P.O. v0.4.4.x 的购物车缩小搬运模组——物品进车自动缩小，离车恢复原尺寸

---

### 功能

**购物车缩小搬运**

- 支持的物品放入 C.A.R.T / 购物车后自动缩小，拿出并离车后恢复
- 底层缩放由 [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) 负责，本模组负责购物车触发、分类倍率、边缘防抖、恢复冷却、商店用品过滤、车类载物过滤和主机决策

**可配置物品与武器缩放**

枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品：

- 默认保持原尺寸，不影响战斗、治疗和工具使用
- 可在配置中开启「启用商店用品缩小」并使用商店用品倍率

**玩家缩放**

默认开启，独立在「玩家缩放」配置分组中：

> 玩家站在正式购物车中心区域一段时间，会按「原样 → 缩小 → 原样 → 增大 → 原样」循环

- 关闭「启用玩家缩放」后，不再运行玩家扫描或玩家缩放逻辑

**死亡头颅缩小**

默认关闭：

- 开启后，已触发的玩家死亡头颅进入购物车会缩小
- 离车、进入提取点、进入卡车或复活前恢复

**代币/外观箱**

- 新版本代币类箱子独立开关和倍率，默认 `0.4`

**特殊物品支持**

- 敌人球 Small/Medium/Big/Berserker 使用独立倍率
- 钱袋/Surplus 使用独立倍率
- 普通非贵重物默认不缩小

**第三方地图标准贵重物**

- REPOLib 地图新增的标准 `ValuableObject` 会按体积分类默认识别
- 普通门、按钮、机关和非贵重地图结构物默认不缩放

**敌人进车秒杀**

默认开启：

- 敌人进入购物车时立刻死亡（复刻 ShrinkerCartPlus 行为）

**主机决策 + ScalerCore 同步**

- 多人游戏中只有主机/单人执行缩小和恢复决策
- 最终大小由 ScalerCore 同步给客户端

---

### 动图演示

#### 商店用品默认不缩小

枪械、血包等商店购买类实用品默认不会被购物车缩小，避免影响战斗、治疗和工具使用。需要时可在配置里开启「启用商店用品缩小」

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### 敌人球和钱袋会缩小

敌人球使用独立的敌人球配置组；钱袋/SurplusValuable 使用特殊物品配置组

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### 大型贵重物会缩小

大型、宽型、高型、超高型贵重物按各自分类倍率缩小，适合搬运原本难以放进车里的物品

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x 特殊物品支持

新版本的特殊物品同样可以被识别并缩小。v0.2.47 将最低依赖更新到 ScalerCore 1.0.4 并继续使用其稳定强类型 API，ScalerCore 1.0.4 修复了最新游戏版本中玩家缩放移动速度调整调用缺失的问题，改善了玩家站车循环兼容性。代币/外观箱适配、购物车列表刷新漏事件补处理与第三方地图贵重物分类支持均已保留

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

| 配置项 | 默认值 |
|:---|---:|
| **购物车** | |
| 　缩小速度 | `0.5` |
| 　放大速度 | `0.2` |
| 　离车防抖延迟 | `0.5` 秒 |
| 　恢复后重新缩小冷却 | `0.5` 秒 |
| 　启用重量随缩放降低 | `true` |
| 　启用商店用品缩小 | `false` |
| 　防止碰撞弹回原尺寸 | `true` |
| **玩家缩放** | |
| 　启用玩家缩放 | `true` |
| 　玩家进车缩放倍率 | `0.55` |
| 　玩家进车增大倍率 | `1.5` |
| 　玩家站车触发时间 | `2` 秒 |
| 　玩家离车判定宽容时间 | `0.6` 秒 |
| 　启用玩家受伤后自动恢复 | `true` |
| **死亡头颅** | |
| 　启用死亡头颅进车缩小 | `false` |
| 　死亡头颅缩小倍率 | `0.5` |
| **贵重物倍率** | |
| 　Tiny | `0.8` |
| 　Small | `0.6` |
| 　Medium | `0.45` |
| 　Big | `0.4` |
| 　Wide | `0.35` |
| 　Tall | `0.35` |
| 　VeryTall | `0.25` |
| **敌人球倍率** | |
| 　Small | `0.8` |
| 　Medium | `0.65` |
| 　Big | `0.45` |
| 　Berserker | `0.45` |
| **特殊物品** | |
| 　商店用品倍率 | `0.5` |
| 　钱袋/Surplus 倍率 | `0.25` |
| 　代币/外观箱倍率 | `0.4` |
| **动态物品扫描** | |
| 　启用扫描 | `true` |
| 　最小间隔 | `0.15` 秒 |
| 　最大间隔 | `1.0` 秒 |
| **车辆碾压** | |
| 　敌人进车秒杀 | `true` |
| **诊断** | |
| 　调试日志 | `false` |
| **语言** | |
| 　配置语言 | `中文` |

> 车类载物过滤属于内部保护，不提供配置项

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

---

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

---

### 已知限制

- 缩放动画和可选质量缩小由 ScalerCore 控制，ShrinkCart 不手动写入 `Rigidbody.mass`，只传递 `PreserveMass` 选项
- 玩家增大只属于玩家站车循环，不会让普通物品、贵重物、敌人球或车辆变大
- 死亡头颅缩小默认关闭；开启后 ShrinkCart 只恢复自己缩小过的头颅，并在 `PlayerDeathHead.Revive` 前做窄范围恢复，不接管复活流程
- 「启用重量随缩放降低」只在 ScalerCore 支持质量缩放时生效；部分 ItemHandler 对象可能视觉缩小明显但手感变化不明显
- ShrinkCart 不接管车对车物理碰撞，不使用穿透修正或速度钳制，车辆真实碰撞表现以原版为准
- 其他缩放类 mod 已控制同一对象时，ShrinkCart 会尽量避免覆盖
- 第三方地图新增的标准贵重物默认按原版 `ValuableObject` 识别；非贵重机关和地图结构物不会被强制缩放
- 死亡/复活流程冲突请使用独立的 `ExtractionReviveFix`（如 REPOFidelity 与提取点复活类 mod），不要在 ShrinkCart 内处理
- 同一 r2modman profile 只保留一个 ShrinkCart DLL，重复手动安装会导致 BepInEx 输出跳过重复插件的日志

---

### 致谢

`BepInEx` · `Harmony` · `ScalerCore` · `REPOConfig` · `Thunderstore`

</details>

---

<details>
<summary><b>English</b> (click to expand)</summary>

<br>

> A cart-scaling hauling mod for R.E.P.O. v0.4.4.x — items shrink automatically when placed into the cart and restore to full size after leaving

---

### Features

**Cart Shrink Hauling**

- Supported objects shrink when placed into a C.A.R.T. / shopping cart and restore after leaving
- [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) handles the actual scaling; this mod manages cart triggers, category factors, edge debounce, restore cooldowns, shop-item filtering, cart-object filtering, and host-side decisions

**Configurable Shop-Item Scaling**

Guns, medkits, melee items, grenades, tools, drones, pearls, upgrades, trackers, mines, and similar shop purchases:

- Keep their original size by default — combat, healing, and tool use stay unaffected
- Enable "shop item shrinking" in config to apply the shop-item factor

**Player Scaling**

Enabled by default, in its own "Player Scaling" config section:

> Standing in the cart center area for the configured duration cycles normal → shrink → normal → grow → normal

- When disabled, ShrinkCart does not run player scanning or player-scaling logic

**Dead-Head Scaling**

Disabled by default:

- When enabled, triggered player death heads shrink in carts
- Restore on leaving the cart, entering extraction/truck flow, or before revive

**Token / Cosmetic Boxes**

- Newer token boxes have their own toggle and factor, default `0.4`

**Special Object Support**

- Enemy orbs use independent factors for Small / Medium / Big / Berserker
- Money bags / Surplus use their own factor
- Ordinary non-valuable objects do not shrink by default

**Third-Party Map Valuables**

- Standard `ValuableObject` items from REPOLib maps are classified by volume
- Doors, buttons, mechanisms, and non-valuable map structures do not shrink by default

**Enemy Instant Kill in Cart**

Enabled by default:

- Enemies die when entering a cart (mirrors ShrinkerCartPlus behavior)

**Host Decisions + ScalerCore Sync**

- In multiplayer, only the host / singleplayer side decides shrink and restore
- ScalerCore syncs the final size to all clients

---

### GIF Demos

#### Shop Items Do Not Shrink By Default

Shop items such as guns and medkits are not shrunk by default, so combat, healing, and tool use stay unaffected. Enable "shop item shrinking" in config if needed

![Shop items not scaled by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### Enemy Orbs And Surplus Shrink

Enemy orbs use the enemy-orb config group. Money bags / SurplusValuable use the special-object config group

![Enemy orb and Surplus scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### Large Valuables Shrink

Large, wide, tall, and very tall valuables shrink with their own category factors, making oversized valuables easier to carry in carts

![Large item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x Special Item Support

Special items from newer R.E.P.O. versions can also be detected and scaled. v0.2.47 raises the minimum dependency to ScalerCore 1.0.4 while keeping its stable, strongly typed 1.x API — ScalerCore 1.0.4 fixes the missing player movement-speed adjustment on the latest game version, improving the player cart cycle. Token/cosmetic-box support, missed cart-enter retry during vanilla cart list refreshes, and third-party map valuable classification are all retained

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

| Setting | Default |
|:---|---:|
| **Cart** | |
| 　Shrink speed | `0.5` |
| 　Restore speed | `0.2` |
| 　Leave-cart debounce | `0.5` s |
| 　Reshrink cooldown | `0.5` s |
| 　Mass scaling with size | `true` |
| 　Shop-item scaling | `false` |
| 　Suppress collision restore | `true` |
| **Player Scaling** | |
| 　Player scaling | `true` |
| 　Player shrink factor | `0.55` |
| 　Player grow factor | `1.5` |
| 　Stand trigger time | `2` s |
| 　Exit grace time | `0.6` s |
| 　Restore player on damage | `true` |
| **Death Heads** | |
| 　Dead-head cart shrinking | `false` |
| 　Dead-head factor | `0.5` |
| **Valuable Factors** | |
| 　Tiny | `0.8` |
| 　Small | `0.6` |
| 　Medium | `0.45` |
| 　Big | `0.4` |
| 　Wide | `0.35` |
| 　Tall | `0.35` |
| 　VeryTall | `0.25` |
| **Enemy-Orb Factors** | |
| 　Small | `0.8` |
| 　Medium | `0.65` |
| 　Big | `0.45` |
| 　Berserker | `0.45` |
| **Special Objects** | |
| 　Shop-item factor | `0.5` |
| 　Money bag / Surplus factor | `0.25` |
| 　Token/cosmetic box factor | `0.4` |
| **Dynamic Item Scanning** | |
| 　Enabled | `true` |
| 　Min interval | `0.15` s |
| 　Max interval | `1.0` s |
| **Cart Crush** | |
| 　Instant-kill enemies | `true` |
| **Diagnostics** | |
| 　Debug logging | `false` |
| **Language** | |
| 　Config language | `中文` |

> Cart-object filtering is an internal protection and is not exposed as a config option

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

---

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

---

### Known Limits

- ScalerCore controls scaling animations and optional mass scaling. ShrinkCart does not manually write `Rigidbody.mass`; it only passes the `PreserveMass` option
- Player growth is only part of the player cart cycle. It does not make regular objects, valuables, enemy orbs, or vehicles grow
- Dead-head shrinking is disabled by default. When enabled, ShrinkCart only restores heads it scaled itself, and its narrow `PlayerDeathHead.Revive` prefix restore does not take over revive flow
- "Mass scaling with size" only has visible handling changes when ScalerCore supports mass scaling for that object. Some ItemHandler objects may shrink visually while feeling similar in hand
- ShrinkCart does not control cart-to-cart physics collision, does not run penetration correction, and does not clamp vehicle velocity. Real cart collision behavior stays vanilla
- If another scaling mod already controls an object, ShrinkCart tries to avoid overriding it
- Third-party map valuables are recognized as vanilla `ValuableObject`s; non-valuable mechanisms and map structures are not force-scaled
- For death/revive flow conflicts (e.g. REPOFidelity and extraction-point revive mods), use the separate `ExtractionReviveFix`; do not handle them inside ShrinkCart
- Keep only one ShrinkCart DLL in the same r2modman profile. Duplicate manual installs can cause BepInEx to log skipped duplicate plugins

---

### Credits

`BepInEx` · `Harmony` · `ScalerCore` · `REPOConfig` · `Thunderstore`

</details>
