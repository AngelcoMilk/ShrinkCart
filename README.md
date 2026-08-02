# ShrinkCart v0.2.47

---

<details open>
<summary><b>中文 / Chinese</b> (click to expand English)</summary>

<br>

> 战利品太多，购物车装不下？装进车里的东西会自动缩小，推着就走——拿出来，又变回原来的大小

给 R.E.P.O. v0.4.4.x 用的购物车缩小搬运模组
缩放的事交给 [ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/)，这个模组只关心一件事：东西放进 C.A.R.T / 购物车就缩，确认离开购物车就恢复
分类倍率、边缘防抖、冷却时间、商店用品过滤——这些细节也都帮你处理好了

### 功能

- **购物车缩小搬运**：能缩的物品放进车就缩，离开车就恢复，再大的战利品也能轻松推着走
- **商店用品默认不缩**：枪、血包、手雷、无人机这些，默认保持原样——打架、回血、用工具，该怎么用还怎么用；想缩就去配置里打开「启用商店用品缩小」
- **玩家站车循环**：默认开着。在购物车中心站一会儿，角色就会走一遍「原样 → 缩小 → 原样 → 增大 → 原样」的循环，不需要的话随时关掉
- **死亡头颅缩小**：默认关闭。开了之后，阵亡队友的头骨进车也会缩小，离车、进提取点/卡车或者复活前会自动恢复
- **代币/外观箱**：新版本抽奖箱有自己的开关和倍率，默认 `0.4`
- **敌人球与钱袋**：敌人球按 Small/Medium/Big/Berserker 各配倍率，钱袋/Surplus 单独配，普通杂物默认不缩
- **第三方地图贵重物**：REPOLib 地图里的标准 `ValuableObject` 会按体积自动分类；门、按钮、机关这些结构物，不会去碰
- **敌人进车秒杀**：默认开启，爬进购物车的敌人直接消灭（复刻 ShrinkerCartPlus 的行为）
- **主机决策 + ScalerCore 同步**：多人里只有主机决定缩不缩、什么时候恢复，最终大小由 ScalerCore 同步给所有玩家

### 动图演示

#### 商店用品默认不缩小

枪械、血包等实用品默认不缩——打架、治疗、工具使用都不受影响。想缩的话，去配置里打开开关就好

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### 敌人球和钱袋会缩小

敌人球走敌人球配置组，钱袋/SurplusValuable 走特殊物品配置组，各自的倍率各管各的

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### 大型贵重物会缩小

大、宽、高、超高的贵重物各按自己的倍率来缩，原本塞不进车的也能顺利推走

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x 特殊物品支持

新版本的特殊物品同样能识别、能缩小
v0.2.47 把最低依赖升到了 ScalerCore 1.0.4，继续用稳定的 1.x 强类型 API——1.0.4 修好了最新游戏版本里玩家缩放移动速度调整缺失的问题，站车循环更稳了
代币/外观箱适配、购物车列表刷新漏事件补处理、第三方地图贵重物分类也都保留着

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

### 游戏内设置

> 配置界面由 REPOConfig 提供，主要配置项为中文；可在「语言 / Language」分组切换为英文

| 分组 | 主要设置项 |
|---|---|
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

### 配置默认值

> 这些是出厂默认值，一般不用动；改了记得重启游戏

| 配置项 | 默认值 |
|---|---|
| 缩小速度 | `0.5` |
| 放大速度 | `0.2` |
| 离车防抖延迟 | `0.5` 秒 |
| 恢复后重新缩小冷却 | `0.5` 秒 |
| 启用重量随缩放降低 | `true` |
| 启用商店用品缩小 | `false` |
| 防止碰撞弹回原尺寸 | `true` |
| 启用玩家缩放 | `true` |
| 玩家进车缩放倍率 | `0.55` |
| 玩家进车增大倍率 | `1.5` |
| 玩家站车触发时间 | `2` 秒 |
| 玩家离车判定宽容时间 | `0.6` 秒 |
| 启用玩家受伤后自动恢复 | `true` |
| 启用死亡头颅进车缩小 | `false` |
| 死亡头颅缩小倍率 | `0.5` |
| 贵重物倍率 | Tiny `0.8` · Small `0.6` · Medium `0.45` · Big `0.4` · Wide `0.35` · Tall `0.35` · VeryTall `0.25` |
| 敌人球倍率 | Small `0.8` · Medium `0.65` · Big `0.45` · Berserker `0.45` |
| 商店用品倍率 | `0.5` |
| 钱袋/Surplus 倍率 | `0.25` |
| 代币/外观箱倍率 | `0.4` |
| 动态物品扫描 | 开启（最小 `0.15` 秒 · 最大 `1.0` 秒） |
| 敌人进车秒杀 | `true` |
| 调试日志 | `false` |
| 配置语言 | `中文` |

> 车类载物过滤属于内部保护，不提供配置项

### 安装

安装很简单：

**前置依赖**（Thunderstore/r2modman 安装时自动拉取）：

- `BepInEx-BepInExPack-5.4.2305`
- [Vippy-ScalerCore-1.0.4](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/)
- `nickklmao-REPOConfig-1.2.6`

**推荐**：通过 Thunderstore Mod Manager / r2modman 安装

**手动安装**：

```
1. 安装 BepInEx 5 及上述依赖
2. 将 ShrinkCart.dll 放入 BepInEx/plugins/ShrinkCart/
3. 启动游戏
```

配置文件路径：`BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### 联机说明

> **房主（Host）必须安装**；队友推荐安装，全员安装时体验最一致

- 缩小和恢复由房主统一确认，ScalerCore 同步给所有玩家
- 没装的玩家仍可能看到 ScalerCore 默认的闪光/音效

### 配置文件

```ini
## 购物车：开关、速度、防抖、冷却与质量选项
[购物车]
CartShrinkingEnabled = true
CartScaleSpeed = 0.5
RestoreScaleSpeed = 0.2
CartLeaveDebounceSeconds = 0.5
ReshrinkCooldownSeconds = 0.5
ScaleMassWithSize = true
ShrinkShopPlayerItems = false
SuppressValuableDamageRestore = true

## 玩家缩放：站车循环的倍率与触发时间
[玩家缩放]
PlayerScalingModuleEnabled = true
PlayerCartScaleFactor = 0.55
PlayerCartGrowFactor = 1.5
PlayerCartStandTriggerSeconds = 2
PlayerCartExitGraceSeconds = 0.6
RestorePlayerOnDamage = true

## 死亡头颅：默认关闭
[死亡头颅]
DeadHeadScalingEnabled = false
DeadHeadScaleFactor = 0.5

## 贵重物分类：每类各有启用开关与倍率（Tiny/…/VeryTall）
[Tiny]
TinyEnabled = true
TinyScaleFactor = 0.8

## 敌人球倍率
[敌人球]
EnemyOrbEnabled = true
EnemyOrbSmallScaleFactor = 0.8
EnemyOrbMediumScaleFactor = 0.65
EnemyOrbBigScaleFactor = 0.45
EnemyOrbBerserkerScaleFactor = 0.45

## 特殊物品：钱袋/Surplus 与代币/外观箱
[特殊物品]
SurplusEnabled = true
SurplusScaleFactor = 0.25
ValuableBoxEnabled = true
ValuableBoxScaleFactor = 0.4

## 商店用品倍率（也作为未知贵重物兜底）
[商店用品]
FallbackScaleFactor = 0.5

## 车辆碾压：敌人进车秒杀
[车辆碾压]
EnemyInCartInstantKill = true

## 性能：动态扫描间隔
[性能]
DynamicItemScanEnabled = true
MinimumItemScanIntervalSeconds = 0.15
MaximumItemScanIntervalSeconds = 1.0

## 诊断
[诊断]
DebugLogging = false

## 语言：中文 / English
[语言 / Language]
配置语言 / Config Language = 中文
```

> 配置语言切换为 English 后，重启/重新加载配置会用英文重建全部分组与键名

### 已知限制

- 缩放动画和可选质量缩放由 ScalerCore 控制；本模组不直接操作 `Rigidbody.mass`，只传递 `PreserveMass` 选项
- 玩家增大只属于站车循环，不会影响到物品、贵重物、敌人球或车辆
- 死亡头颅缩小默认关闭；开启后只恢复自己缩小过的头骨，在 `PlayerDeathHead.Revive` 前做精简恢复，不会干预复活逻辑
- 「启用重量随缩放降低」只在 ScalerCore 支持该对象时才生效；有些 ItemHandler 缩了之后看着小了，拎起来分量没变——正常现象
- 不接管车与车之间的物理碰撞，不会做穿透修正或限速；撞车效果全看原版
- 别的缩放模组如果已经在管某个对象，这个模组会尽量不碰
- 第三方地图里的标准贵重物按原版 `ValuableObject` 识别；门、按钮、机关这些结构物不会被缩放
- 跟死亡/复活流程有冲突的话（比如 REPOFidelity 或提取点复活模组），请用独立的 `ExtractionReviveFix`
- 一个 r2modman profile 里只留一个 ShrinkCart DLL，装重复了 BepInEx 会报跳过

</details>

---

<details>
<summary><b>English</b> (click to expand)</summary>

<br>

> Too much loot, not enough cart? Things you put in shrink down automatically — haul them away, and they pop back to full size when they leave

A cart-scaling hauling mod for R.E.P.O. v0.4.4.x
[ScalerCore](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/) handles the actual scaling. This mod just watches the cart: supported object enters a C.A.R.T. / shopping cart? Shrink. Leaves for real? Restore.
Category factors, edge debounce, timers, shop-item filtering — all taken care of

### Features

- **Cart shrink hauling**: supported objects shrink when placed into the cart and restore after leaving. Even the biggest valuables become easy to haul
- **Shop items stay full-size by default**: guns, medkits, grenades, drones — by default they keep their original size, so combat and tool use aren't affected. Flip the config switch if you want them shrunk
- **Player cart cycle**: on by default. Stand in the cart center area for a bit and your character cycles normal → shrink → normal → grow → normal. Turn it off whenever you want
- **Dead-head scaling**: off by default. When enabled, triggered player death skulls shrink in carts and restore on leaving, entering extraction/truck flow, or before revive
- **Token/cosmetic boxes**: newer token boxes have their own toggle and factor, default `0.4`
- **Enemy orbs and money bags**: enemy orbs have per-size factors (Small/Medium/Big/Berserker), money bags / Surplus have their own, ordinary items don't shrink by default
- **Third-party map valuables**: standard `ValuableObject` items added by REPOLib maps are classified by volume; doors, buttons, and mechanisms are never touched
- **Enemy instant kill in cart**: on by default, enemies climbing into the cart get eliminated (mirrors ShrinkerCartPlus behavior)
- **Host decides, ScalerCore syncs**: in multiplayer only the host makes shrink and restore calls, and ScalerCore syncs the final size to everyone

### GIF Demos

#### Shop Items Do Not Shrink By Default

Shop items such as guns and medkits are not shrunk by default — combat, healing, and tool use all stay normal. Flip the switch in config if you want them shrunk

![Shop items not scaled by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### Enemy Orbs And Surplus Shrink

Enemy orbs use the enemy-orb config group and money bags / SurplusValuable use the special-object group, each with its own factor

![Enemy orb and Surplus scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### Large Valuables Shrink

Large, wide, tall, and very tall valuables shrink with their own category factors — oversized ones that barely fit anywhere become easy to haul

![Large item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x Special Item Support

Special items from newer R.E.P.O. versions are also detected and scaled
v0.2.47 raises the minimum dependency to ScalerCore 1.0.4, keeping its stable, strongly typed 1.x API — 1.0.4 fixes the missing player movement-speed adjustment on the latest game version, so the player cart cycle is more stable
Token/cosmetic-box support, missed cart-enter retry during vanilla cart list refreshes, and third-party map valuable classification are all retained

![R.E.P.O. 4.0 special item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

### In-Game Settings

> Configuration is provided through REPOConfig. Most entries are localized in Chinese; switch to English in the "Language" group if you prefer

| Group | Main settings |
|---|---|
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

### Default Values

> These are the defaults; in most cases you won't need to change them. Restart the game after editing

| Setting | Default |
|---|---|
| Shrink speed | `0.5` |
| Restore speed | `0.2` |
| Leave-cart debounce | `0.5` seconds |
| Reshrink cooldown | `0.5` seconds |
| Mass scaling with size | `true` |
| Shop-item scaling | `false` |
| Suppress collision restore | `true` |
| Player scaling | `true` |
| Player shrink factor | `0.55` |
| Player grow factor | `1.5` |
| Player stand trigger time | `2` seconds |
| Player exit grace time | `0.6` seconds |
| Restore player on damage | `true` |
| Dead-head cart shrinking | `false` |
| Dead-head factor | `0.5` |
| Valuable factors | Tiny `0.8` · Small `0.6` · Medium `0.45` · Big `0.4` · Wide `0.35` · Tall `0.35` · VeryTall `0.25` |
| Enemy-orb factors | Small `0.8` · Medium `0.65` · Big `0.45` · Berserker `0.45` |
| Shop-item factor | `0.5` |
| Money bag / Surplus factor | `0.25` |
| Token/cosmetic box factor | `0.4` |
| Dynamic item scanning | On (min `0.15` s · max `1.0` s) |
| Instant-kill enemies in cart | `true` |
| Debug logging | `false` |
| Config language | `中文` |

> Cart-object filtering is an internal protection and is not exposed as a config option

### Installation

Easy to install:

**Dependencies** (installed automatically by Thunderstore / r2modman):

- `BepInEx-BepInExPack-5.4.2305`
- [Vippy-ScalerCore-1.0.4](https://thunderstore.io/c/repo/p/Vippy/ScalerCore/)
- `nickklmao-REPOConfig-1.2.6`

**Recommended**: Install via Thunderstore Mod Manager / r2modman

**Manual**:

```
1. Install BepInEx 5 and the dependencies above
2. Place ShrinkCart.dll in BepInEx/plugins/ShrinkCart/
3. Launch the game
```

Config file: `BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### Multiplayer

> **Host must install**; teammates are recommended to install too for the most consistent experience

- Shrink and restore are confirmed by the host and synced to all players through ScalerCore
- Players without ShrinkCart may still see or hear ScalerCore default flash/sound effects

### Configuration

```ini
## Cart: toggles, speeds, debounce, cooldown, mass options
[Cart]
CartShrinkingEnabled = true
CartScaleSpeed = 0.5
RestoreScaleSpeed = 0.2
CartLeaveDebounceSeconds = 0.5
ReshrinkCooldownSeconds = 0.5
ScaleMassWithSize = true
ShrinkShopPlayerItems = false
SuppressValuableDamageRestore = true

## Player scaling: cart-cycle factors and timing
[Player Scaling]
PlayerScalingModuleEnabled = true
PlayerCartScaleFactor = 0.55
PlayerCartGrowFactor = 1.5
PlayerCartStandTriggerSeconds = 2
PlayerCartExitGraceSeconds = 0.6
RestorePlayerOnDamage = true

## Death heads: disabled by default
[Death Heads]
DeadHeadScalingEnabled = false
DeadHeadScaleFactor = 0.5

## Valuable categories: each has its own toggle and factor (Tiny/.../VeryTall)
[Tiny]
TinyEnabled = true
TinyScaleFactor = 0.8

## Enemy orbs
[Enemy Orbs]
EnemyOrbEnabled = true
EnemyOrbSmallScaleFactor = 0.8
EnemyOrbMediumScaleFactor = 0.65
EnemyOrbBigScaleFactor = 0.45
EnemyOrbBerserkerScaleFactor = 0.45

## Special objects: money bags / Surplus and token boxes
[Special Objects]
SurplusEnabled = true
SurplusScaleFactor = 0.25
ValuableBoxEnabled = true
ValuableBoxScaleFactor = 0.4

## Shop items (also the fallback for unknown valuable categories)
[Shop Items]
FallbackScaleFactor = 0.5

## Cart crush: instant-kill enemies in cart
[Cart Crush]
EnemyInCartInstantKill = true

## Performance: dynamic scan intervals
[Performance]
DynamicItemScanEnabled = true
MinimumItemScanIntervalSeconds = 0.15
MaximumItemScanIntervalSeconds = 1.0

## Diagnostics
[Diagnostics]
DebugLogging = false

## Language: 中文 / English
[语言 / Language]
配置语言 / Config Language = 中文
```

> Switching the config language to English rebuilds all sections and keys on restart/reload

### Known Limits

- ScalerCore controls scaling animations and optional mass scaling. This mod doesn't touch `Rigidbody.mass` directly — it only passes the `PreserveMass` option
- Player growth is only part of the cart cycle and never applies to items, valuables, enemy orbs, or vehicles
- Dead-head shrinking is off by default. When enabled, only heads scaled by this mod are restored, with a narrow `PlayerDeathHead.Revive` prefix — it won't mess with revive logic
- "Mass scaling with size" only kicks in where ScalerCore supports it for that object. Some ItemHandler objects may look smaller but feel the same when you pick them up — that's normal
- Doesn't control cart-to-cart physics, no penetration correction, no velocity clamping. Cart collisions behave however vanilla handles them
- If another scaling mod already has dibs on an object, this one tries to stay out of the way
- Third-party map valuables are recognized through vanilla `ValuableObject` classification; non-valuable mechanisms and map structures are never force-scaled
- For death/revive flow conflicts (e.g. REPOFidelity or extraction-point revive mods), use the separate `ExtractionReviveFix`
- Keep only one ShrinkCart DLL per r2modman profile — duplicates make BepInEx skip the extra plugin

</details>
