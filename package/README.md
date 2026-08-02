# ShrinkCart v0.2.47

---

<details open>
<summary><b>中文 / Chinese</b> (click to expand English)</summary>

<br>

> 战利品太多，购物车太小？放进去的东西自动"打包"缩小，推着就走，拿出来就恢复原样

ShrinkCart 是个给 R.E.P.O. v0.4.4.x 用的购物车缩小搬运模组。它自己不管缩放，那是 ScalerCore 的活儿——它负责在你把东西放进 C.A.R.T / 购物车时喊一声"缩"，等你确认把东西拿出车外，再喊一声"回来"。购物车触发、分类倍率、边缘防抖、恢复冷却、商店用品过滤这些脏活累活，全是它干

### 功能

- **购物车缩小搬运**：能缩的东西放进车就缩，拿出去走两步就恢复。大件战利品从此不用卡在车门上
- **商店用品默认不缩**：枪、血包、手雷、无人机这些打架吃饭的家伙，默认原样待在车里；想缩就在配置里开「启用商店用品缩小」
- **玩家站车循环**：默认开启。站进购物车中心一小会儿，就会按「原样 → 缩小 → 原样 → 增大 → 原样」表演一圈；不想要的话关掉，ShrinkCart 就完全不管玩家缩放
- **死亡头颅缩小**：默认关闭。开了之后，阵亡队友的头颅放进车里也会缩小，离车、进提取点或复活前恢复
- **代币/外观箱支持**：新版本抽奖箱有独立开关和倍率，默认 `0.4`
- **敌人球和钱袋**：敌人球按 Small/Medium/Big/Berserker 各配倍率，钱袋/Surplus 单独配；普通杂物默认不缩
- **第三方地图贵重物**：REPOLib 地图的标准 `ValuableObject` 按体积自动分类；门、按钮、机关这些结构物不会被乱缩
- **敌人进车秒杀**：默认开启。敢爬进车的敌人，当场没了
- **主机说了算**：多人里只有主机做缩小/恢复决定，ScalerCore 把最终大小同步给所有人，客户端跟着看就行

### 动图演示

#### 商店用品默认不缩小

打架、回血、拆机关的家伙们默认不会被缩，免得影响发挥；想缩，配置里开一下就行

![枪械和血包默认不缩小](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### 敌人球和钱袋会缩小

敌人球用敌人球自己的配置，钱袋/Surplus 用特殊物品配置，各缩各的

![敌人球和 Surplus 缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### 大型贵重物会缩小

大、宽、高、超高的贵重物按各自倍率缩，原本塞不进车的，现在能推着走了

![大型物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x 特殊物品支持

新版本的特殊物品也能认出来、缩起来。v0.2.47 把最低依赖升到了 ScalerCore 1.0.4，继续用稳定的 1.x 强类型 API——1.0.4 修好了最新游戏版本里玩家缩放时移动速度调整调用缺失的问题，玩家站车循环更顺了；代币/外观箱适配、购物车列表刷新漏事件补处理、第三方地图贵重物分类也都保留着

![R.E.P.O. 4.0 特殊物品缩小演示](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-repo40-special-item-scaled.gif)

### 游戏内设置

> 配置界面由 REPOConfig 提供，主要配置项是中文；想看图说话，在「语言 / Language」里切到英文

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

> 以下是出厂设置，基本不用动；想微调，改完重启生效

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

> 车类载物过滤是内部保护，故意不给你看，也别想改

### 安装

装起来很省心：

**前置依赖**（Thunderstore/r2modman 安装时自动拉取）：

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-1.0.4`
- `nickklmao-REPOConfig-1.2.6`

**推荐**：Thunderstore Mod Manager / r2modman 一键装，依赖自动补齐

**手动安装**：

```
1. 安装 BepInEx 5 和上面的依赖
2. 把 ShrinkCart.dll 丢进 BepInEx/plugins/ShrinkCart/
3. 启动游戏，收工
```

配置文件在：`BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### 联机说明

> **房主（Host）必须装**；队友建议也装，全队都装体验最一致

- 缩小和恢复由房主确认，ScalerCore 同步给所有人，节奏以房主为准
- 队友没装的话，还是可能看到 ScalerCore 默认的闪光/音效

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

> 配置语言切到 English 后，重启/重新加载配置会用英文重建全部分组和键名

### 已知限制

- 缩放动画和可选质量缩小是 ScalerCore 的活，ShrinkCart 不手动写 `Rigidbody.mass`，只传 `PreserveMass` 选项
- 玩家"变大"只属于站车循环，别指望它把物品、贵重物、敌人球或车一起变大
- 死亡头颅缩小默认关闭；开了也只恢复 ShrinkCart 自己缩过的头，并在 `PlayerDeathHead.Revive` 前做窄范围恢复，不抢复活流程
- 「启用重量随缩放降低」得看 ScalerCore 支不支持那个对象；有些 ItemHandler 可能看着缩了，手感没变化
- ShrinkCart 不碰车对车物理碰撞，不用穿透修正也不钳速度，真实碰撞表现以原版为准
- 别的缩放 mod 已经在管同一个对象时，ShrinkCart 会尽量让路
- 第三方地图的贵重物按原版 `ValuableObject` 认；非贵重机关和地图结构物不会被硬缩
- 死亡/复活流程的冲突（比如 REPOFidelity 和提取点复活类 mod）请用独立的 `ExtractionReviveFix`，别指望在 ShrinkCart 里解决
- 同一个 r2modman profile 只放一个 ShrinkCart DLL；重复手动安装会让 BepInEx 打日志说"跳过重复插件"

</details>

---

<details>
<summary><b>English</b> (click to expand)</summary>

<br>

> Too much loot, too small a cart? Things you put in shrink down automatically — haul them away, and they pop back when they leave

ShrinkCart is a cart-scaling hauling mod for R.E.P.O. v0.4.4.x. It doesn't do the scaling itself — that's ScalerCore's job. ShrinkCart just yells "shrink!" when you put something into a C.A.R.T. / shopping cart, and "come back!" once it's truly out of the cart. Cart triggers, category factors, edge debounce, restore cooldowns, and shop-item filtering? All ShrinkCart.

### Features

- **Cart shrink hauling**: supported objects shrink in the cart and restore after leaving. No more giant valuables stuck in the doorway
- **Shop items stay full-size by default**: guns, medkits, grenades, drones, and other combat-and-tools items stay as-is unless you enable "shop item shrinking" in config
- **Player cart cycle**: on by default. Stand in the cart center for a bit and you cycle normal → shrink → normal → grow → normal. Not into it? Turn it off and ShrinkCart leaves player scaling alone entirely
- **Dead-head scaling**: off by default. When enabled, triggered player death heads shrink in carts and restore on leaving, entering extraction/truck flow, or before revive
- **Token/cosmetic boxes**: newer token boxes have their own toggle and factor, default `0.4`
- **Enemy orbs and money bags**: enemy orbs get per-size factors (Small/Medium/Big/Berserker), money bags / Surplus get their own; ordinary junk doesn't shrink by default
- **Third-party map valuables**: standard `ValuableObject` items from REPOLib maps are classified by volume; doors, buttons, and mechanisms are left alone
- **Enemy instant kill in cart**: on by default. Enemies climbing into your cart? Not anymore
- **Host decides, ScalerCore syncs**: in multiplayer only the host/singleplayer side makes shrink and restore decisions, and ScalerCore syncs the final size to everyone

### GIF Demos

#### Shop Items Do Not Shrink By Default

Your combat, healing, and tool items stay normal-sized so you can actually use them. Want them shrunk? Flip the config switch

![Shop items not scaled by default](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-shop-items-not-scaled.gif)

#### Enemy Orbs And Surplus Shrink

Enemy orbs use the enemy-orb config group; money bags / SurplusValuable use the special-object group. Each shrinks its own way

![Enemy orb and Surplus scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-enemy-orb-surplus-scaled.gif)

#### Large Valuables Shrink

Large, wide, tall, and very tall valuables shrink with their own category factors — stuff that never fit in a cart now rolls along fine

![Large item scaling demo](https://github.com/AngelcoMilk/ShrinkCart/releases/download/v0.2.5/shrinkcart-large-item-scaled.gif)

#### R.E.P.O. v0.4.x Special Item Support

Special items from newer R.E.P.O. versions are detected and scaled too. v0.2.47 raises the minimum dependency to ScalerCore 1.0.4 while keeping its stable, strongly typed 1.x API — 1.0.4 fixes the missing player movement-speed adjustment on the latest game version, so the player cart cycle is smoother. Token/cosmetic-box support, missed cart-enter retry during vanilla cart list refreshes, and third-party map valuable classification all remain

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

> These are the out-of-the-box defaults. Most people never touch them; tweak and restart if you do

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

> Cart-object filtering is an internal protection — hidden on purpose, and not configurable

### Installation

Easy:

**Dependencies** (installed automatically by Thunderstore / r2modman):

- `BepInEx-BepInExPack-5.4.2305`
- `Vippy-ScalerCore-1.0.4`
- `nickklmao-REPOConfig-1.2.6`

**Recommended**: Thunderstore Mod Manager / r2modman — one click, dependencies handled

**Manual**:

```
1. Install BepInEx 5 and the dependencies above
2. Drop ShrinkCart.dll into BepInEx/plugins/ShrinkCart/
3. Launch the game and go
```

Config file: `BepInEx/config/AngelcoMilk.ShrinkCart.cfg`

### Multiplayer

> **Host must install ShrinkCart**; teammates are recommended to install too for the most consistent experience

- Shrink and restore are confirmed by the host and synced to all clients through ScalerCore
- Clients without ShrinkCart may still see or hear ScalerCore default flash/sound effects

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

- ScalerCore controls scaling animations and optional mass scaling. ShrinkCart does not manually write `Rigidbody.mass`; it only passes the `PreserveMass` option
- Player growth is only part of the cart cycle. It won't make items, valuables, enemy orbs, or vehicles grow
- Dead-head shrinking is off by default. When on, ShrinkCart only restores heads it scaled itself, with a narrow `PlayerDeathHead.Revive` prefix restore — it never takes over revive flow
- "Mass scaling with size" only kicks in where ScalerCore supports mass scaling for that object. Some ItemHandler objects may look smaller but feel the same in hand
- ShrinkCart does not control cart-to-cart physics, does not run penetration correction, and does not clamp vehicle velocity. Real cart collision behavior stays vanilla
- If another scaling mod already controls an object, ShrinkCart tries to get out of its way
- Third-party map valuables are recognized as vanilla `ValuableObject`s; non-valuable mechanisms and map structures are never force-scaled
- For death/revive flow conflicts (e.g. REPOFidelity and extraction-point revive mods), use the separate `ExtractionReviveFix`; don't expect ShrinkCart to solve them
- Keep only one ShrinkCart DLL in the same r2modman profile. Duplicate manual installs make BepInEx log "skipped duplicate plugin"

</details>
