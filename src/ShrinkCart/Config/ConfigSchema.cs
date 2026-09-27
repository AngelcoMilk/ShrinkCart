using System.Collections.Generic;
using ShrinkCart.Core;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 全部配置项的声明式定义（顺序即绑定顺序）。键名是公共接口：改名、删除或改刻度必须在
    /// <see cref="ConfigMigrator"/> 增加迁移步骤并提升 schema 版本；新增配置项不需要迁移。
    /// docs/INTERFACES.md 的配置表由单元测试保证与本类一致。
    /// </summary>
    internal static class ConfigSchema
    {
        internal const string ChineseLanguage = "中文";
        internal const string EnglishLanguage = "English";
        internal const string LanguageSection = "语言 / Language";
        internal const string LanguageKey = "配置语言 / Config Language";
        internal const string MetaSection = "ShrinkCart.Meta";
        internal const string SchemaVersionKey = "ConfigSchemaVersion";

        private const string Before031 = "≤0.2.52";

        // 必须在所有定义字段之前声明：静态字段按文本顺序初始化。
        private static readonly List<ConfigKeyDefinition> Ordered = new List<ConfigKeyDefinition>();

        internal static readonly ConfigKeyDefinition ModEnabled = Bool(
            "ModEnabled", FeatureId.Core,
            "通用", "General",
            "启用 ShrinkCart", "Enable ShrinkCart",
            true,
            "总开关。关闭后 ShrinkCart 的全部主机侧功能（物品缩小、玩家缩放、死亡头颅、敌人进车秒杀、车类物品拦截）都会停止，并恢复已被缩放的对象。",
            "Master switch. When disabled, every host-side ShrinkCart feature (item shrinking, player scaling, death heads, enemy instant kill, cart nesting guard) stops and scaled objects are restored.",
            "0.3.1");

        internal static readonly ConfigKeyDefinition CartShrinkingEnabled = Bool(
            "CartShrinkingEnabled", FeatureId.ItemShrink,
            "购物车", "Cart",
            "启用购物车缩小", "Enable cart shrinking",
            true,
            "启用后，放进 C.A.R.T / 购物车的支持物品会自动缩小，取出后恢复。关闭后玩家缩放与死亡头颅缩小也会一起停用。",
            "When enabled, supported objects placed into a C.A.R.T. / shopping cart shrink automatically and restore after leaving. Player scaling and dead-head shrinking also require this switch.",
            Before031);

        internal static readonly ConfigKeyDefinition CartScaleSpeed = Float(
            "CartScaleSpeed", FeatureId.ItemShrink,
            "购物车", "Cart",
            "放入时缩小速度", "Shrink speed",
            5f, 0.1f, 20.0f,
            "ScalerCore 缩小动画速度 = 该值 × 0.1。数值越大越快，默认 5 即实际速度 0.5。每调 0.01 实际变化 0.001。",
            "ScalerCore shrink animation speed = value x 0.1. Higher is faster; default 5 equals actual speed 0.5. Each 0.01 step changes the actual speed by 0.001.",
            Before031);

        internal static readonly ConfigKeyDefinition RestoreScaleSpeed = Float(
            "RestoreScaleSpeed", FeatureId.ItemShrink,
            "购物车", "Cart",
            "取出后放大速度", "Restore speed",
            2f, 0.1f, 20.0f,
            "取出购物车后的 ScalerCore 放大动画速度 = 该值 × 0.1。数值越小越慢，默认 2 即实际速度 0.2。",
            "ScalerCore restore animation speed after leaving the cart = value x 0.1. Lower is slower; default 2 equals actual speed 0.2.",
            Before031);

        internal static readonly ConfigKeyDefinition CartLeaveDebounceSeconds = Float(
            "CartLeaveDebounceSeconds", FeatureId.ItemShrink,
            "购物车", "Cart",
            "离车防抖延迟", "Leave-cart debounce",
            0.5f, 0.1f, 10.0f,
            "物品离开购物车检测范围后，再等多久才恢复原尺寸。原版约每 0.5 秒检测一次车内物品，所以实际会在离车后「该值」到「该值 + 0.5 秒」之间恢复。调高可减少车边缘抽搐。",
            "How long an item must stay outside the cart before it restores. The game checks cart contents about every 0.5 s, so restore happens between this value and this value + 0.5 s after leaving. Higher values reduce edge jitter.",
            Before031);

        internal static readonly ConfigKeyDefinition ReshrinkCooldownSeconds = Float(
            "ReshrinkCooldownSeconds", FeatureId.ItemShrink,
            "购物车", "Cart",
            "恢复后重新缩小冷却", "Reshrink cooldown",
            0.5f, 0.05f, 10.0f,
            "物品开始恢复后，等待多少秒才允许再次被购物车缩小。",
            "How many seconds to wait after restore starts before the cart may shrink the item again.",
            Before031);

        internal static readonly ConfigKeyDefinition ScaleMassWithSize = Bool(
            "ScaleMassWithSize", FeatureId.ItemShrink,
            "购物车", "Cart",
            "启用重量随缩放降低", "Scale mass with size",
            true,
            "启用后通过 ScalerCore 的 PreserveMass 选项允许支持对象按缩放倍率降低质量；关闭则只改变视觉尺寸并保持原始重量。",
            "When enabled, ShrinkCart passes ScalerCore options that allow supported objects to reduce mass with scale. When disabled, only visual size changes and original mass is preserved.",
            Before031);

        internal static readonly ConfigKeyDefinition ShrinkShopPlayerItems = Bool(
            "ShrinkShopPlayerItems", FeatureId.ItemShrink,
            "购物车", "Cart",
            "启用商店用品缩小", "Enable shop item shrinking",
            false,
            "启用后，枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品会使用默认缩小倍率。大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。",
            "When enabled, shop utility items such as guns, medkits, melee items, grenades, tools, drones, pearls, upgrades, trackers, and mines use the shop-item scale factor. Carts, C.A.R.T. Cannon, and C.A.R.T. Laser never shrink.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerScalingModuleEnabled = Bool(
            "PlayerScalingModuleEnabled", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "启用玩家缩放", "Enable player scaling",
            true,
            "默认开启。启用后才会运行玩家站车检测和玩家缩放；关闭时 ShrinkCart 不参与任何玩家缩放逻辑。",
            "Enabled by default. When enabled, ShrinkCart runs player cart-floor detection and player scaling. When disabled, ShrinkCart does not run player scaling logic.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerCartScaleFactor = Float(
            "PlayerCartScaleFactor", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "玩家进车缩放倍率", "Player cart scale factor",
            0.55f, 0.05f, 1.0f,
            "开启“启用玩家缩放”后，玩家站在购物车中心区域触发缩放时的目标尺寸比例。",
            "Target player size factor when player scaling is enabled and a player stands in the cart center area.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerCartGrowFactor = Float(
            "PlayerCartGrowFactor", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "玩家进车增大倍率", "Player cart grow factor",
            1.5f, 1.0f, 2.0f,
            "玩家站车循环中的增大目标比例。循环顺序为：原样 -> 缩小 -> 原样 -> 增大 -> 原样。",
            "Target player growth factor for the cart cycle: normal -> shrink -> normal -> grow -> normal.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerCartStandTriggerSeconds = Float(
            "PlayerCartStandTriggerSeconds", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "玩家站车触发时间", "Player stand trigger time",
            2.0f, 0.25f, 10.0f,
            "开启“启用玩家缩放”后，玩家站在购物车中心区域多久才执行一次缩小/恢复/增大循环。离开中心区域会重置计时。",
            "How long a player must stand in the cart center area before the shrink/restore/grow cycle advances. Leaving the center area resets the timer.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerCartExitGraceSeconds = Float(
            "PlayerCartExitGraceSeconds", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "玩家离车判定宽容时间", "Player exit grace time",
            0.6f, 0.0f, 2.0f,
            "玩家仍在购物车中心区域附近但短暂跳起、踩到车内物品或被货物顶起时，保留车内状态多久后才判定离开。设为 0 可恢复严格判定。",
            "How long to keep cart-inside state when the player briefly jumps, stands on cart cargo, or is pushed up while still near the cart center area. Set to 0 for strict detection.",
            Before031);

        internal static readonly ConfigKeyDefinition PlayerCartDetectionIntervalSeconds = Float(
            "PlayerCartDetectionIntervalSeconds", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "玩家检测间隔", "Player detection interval",
            0.75f, 0.25f, 2.0f,
            "玩家缩放开启时，主机多久检测一次玩家是否位于正式购物车底面投影范围内。数值越大越省性能，也越能防误触发。",
            "When player scaling is enabled, how often the host checks whether players are inside a regular cart floor projection. Higher values cost less and reduce accidental triggers.",
            Before031);

        internal static readonly ConfigKeyDefinition RestorePlayerOnDamage = Bool(
            "RestorePlayerOnDamage", FeatureId.PlayerScaling,
            "玩家缩放", "Player Scaling",
            "启用玩家受伤后自动恢复", "Restore player on damage",
            true,
            "启用后，玩家缩放时使用 ScalerCore 的受伤/碰撞恢复链路；关闭后，玩家只会通过再次站车切换恢复。",
            "When enabled, ShrinkCart-scaled players use ScalerCore's damage/collision restore path. When disabled, players restore only by standing in the cart again.",
            Before031);

        internal static readonly ConfigKeyDefinition SuppressValuableDamageRestore = Bool(
            "SuppressValuableDamageRestore", FeatureId.ItemShrink,
            "购物车", "Cart",
            "防止碰撞弹回原尺寸", "Suppress collision restore",
            true,
            "启用后，贵重物品在购物车里轻微碰撞时不会立刻弹回原尺寸。ScalerCore 的安全恢复仍会保留。",
            "When enabled, valuables do not immediately restore from light collision value drops while in the cart. ScalerCore safety restore remains active.",
            Before031);

        internal static readonly ConfigKeyDefinition CartNestingGuardEnabled = Bool(
            "CartNestingGuardEnabled", FeatureId.CartNestingGuard,
            "购物车", "Cart",
            "阻止车类物品计入车内", "Ignore carts inside carts",
            true,
            "启用后，购物车、小推车、载具、C.A.R.T. Cannon / Laser 等车类物品放进另一辆车时，不会被计入该车的车内物品数量与总价，也不会被当作车内物品处理。",
            "When enabled, carts, pocket carts, vehicles, and C.A.R.T. Cannon / Laser placed inside another cart are not counted in that cart's item count or haul value and are not treated as cart contents.",
            "0.3.1");

        internal static readonly ConfigKeyDefinition DeadHeadScalingEnabled = Bool(
            "DeadHeadScalingEnabled", FeatureId.DeadHeadShrink,
            "死亡头颅", "Death Heads",
            "启用死亡头颅进车缩小", "Enable dead head cart shrinking",
            false,
            "默认关闭。启用后，已触发的玩家死亡头颅进入购物车时会缩小，离车、进入提取点、进入卡车或复活前会恢复。",
            "Disabled by default. When enabled, triggered player death heads shrink in carts and restore when leaving, entering extraction/truck flow, or before revive.",
            Before031);

        internal static readonly ConfigKeyDefinition DeadHeadScaleFactor = Float(
            "DeadHeadScaleFactor", FeatureId.DeadHeadShrink,
            "死亡头颅", "Death Heads",
            "死亡头颅缩小倍率", "Dead head scale factor",
            0.5f, 0.05f, 1.0f,
            "死亡头颅进入购物车后的目标尺寸比例。",
            "Target size factor for death heads inside carts.",
            Before031);

        internal static readonly ConfigKeyDefinition TinyEnabled = CategoryEnabled("Tiny", "Tiny 微型贵重物", "Tiny valuables");
        internal static readonly ConfigKeyDefinition TinyScaleFactor = CategoryFactor("Tiny", "Tiny 微型贵重物", "Tiny valuables", 0.8f);
        internal static readonly ConfigKeyDefinition SmallEnabled = CategoryEnabled("Small", "Small 小贵重物", "Small valuables");
        internal static readonly ConfigKeyDefinition SmallScaleFactor = CategoryFactor("Small", "Small 小贵重物", "Small valuables", 0.6f);
        internal static readonly ConfigKeyDefinition MediumEnabled = CategoryEnabled("Medium", "Medium 中贵重物", "Medium valuables");
        internal static readonly ConfigKeyDefinition MediumScaleFactor = CategoryFactor("Medium", "Medium 中贵重物", "Medium valuables", 0.45f);
        internal static readonly ConfigKeyDefinition BigEnabled = CategoryEnabled("Big", "Big 大贵重物", "Big valuables");
        internal static readonly ConfigKeyDefinition BigScaleFactor = CategoryFactor("Big", "Big 大贵重物", "Big valuables", 0.4f);
        internal static readonly ConfigKeyDefinition WideEnabled = CategoryEnabled("Wide", "Wide 宽贵重物", "Wide valuables");
        internal static readonly ConfigKeyDefinition WideScaleFactor = CategoryFactor("Wide", "Wide 宽贵重物", "Wide valuables", 0.35f);
        internal static readonly ConfigKeyDefinition TallEnabled = CategoryEnabled("Tall", "Tall 高贵重物", "Tall valuables");
        internal static readonly ConfigKeyDefinition TallScaleFactor = CategoryFactor("Tall", "Tall 高贵重物", "Tall valuables", 0.35f);
        internal static readonly ConfigKeyDefinition VeryTallEnabled = CategoryEnabled("VeryTall", "VeryTall 超高贵重物", "VeryTall valuables");
        internal static readonly ConfigKeyDefinition VeryTallScaleFactor = CategoryFactor("VeryTall", "VeryTall 超高贵重物", "VeryTall valuables", 0.25f);

        internal static readonly ConfigKeyDefinition EnemyOrbEnabled = Bool(
            "EnemyOrbEnabled", FeatureId.ItemShrink,
            "敌人球", "Enemy Orbs",
            "启用敌人球缩小", "Enable enemy orb shrinking",
            true,
            "启用后，Enemy - Small/Medium/Big/Berserker 类贵重物会按下方倍率缩小。",
            "When enabled, Enemy - Small/Medium/Big/Berserker valuables shrink using the factors below.",
            Before031);

        internal static readonly ConfigKeyDefinition EnemyOrbSmallScaleFactor = EnemyOrbFactor("Small", 0.8f);
        internal static readonly ConfigKeyDefinition EnemyOrbMediumScaleFactor = EnemyOrbFactor("Medium", 0.65f);
        internal static readonly ConfigKeyDefinition EnemyOrbBigScaleFactor = EnemyOrbFactor("Big", 0.45f);
        internal static readonly ConfigKeyDefinition EnemyOrbBerserkerScaleFactor = EnemyOrbFactor("Berserker", 0.45f);

        internal static readonly ConfigKeyDefinition SurplusEnabled = Bool(
            "SurplusEnabled", FeatureId.ItemShrink,
            "特殊物品", "Special Objects",
            "启用钱袋/Surplus 缩小", "Enable money bag / Surplus shrinking",
            true,
            "启用后，钱袋/SurplusValuable 会使用单独倍率。",
            "When enabled, money bags / SurplusValuable use their own scale factor.",
            Before031);

        internal static readonly ConfigKeyDefinition SurplusScaleFactor = Float(
            "SurplusScaleFactor", FeatureId.ItemShrink,
            "特殊物品", "Special Objects",
            "钱袋/Surplus 倍率", "Money bag / Surplus factor",
            0.25f, 0.05f, 1.0f,
            "钱袋/SurplusValuable 放入购物车后的目标尺寸比例。",
            "Target size factor for money bags / SurplusValuable in carts.",
            Before031);

        internal static readonly ConfigKeyDefinition ValuableBoxEnabled = Bool(
            "ValuableBoxEnabled", FeatureId.ItemShrink,
            "特殊物品", "Special Objects",
            "启用代币箱缩小", "Enable token box shrinking",
            true,
            "启用后，新版本抽奖用代币箱 ItemValuableBox 会使用单独倍率。",
            "When enabled, newer token boxes / ItemValuableBox objects use their own scale factor.",
            Before031);

        internal static readonly ConfigKeyDefinition ValuableBoxScaleFactor = Float(
            "ValuableBoxScaleFactor", FeatureId.ItemShrink,
            "特殊物品", "Special Objects",
            "代币箱倍率", "Token box factor",
            0.4f, 0.05f, 1.0f,
            "抽奖用代币箱 ItemValuableBox 放入购物车后的目标尺寸比例。",
            "Target size factor for token boxes / ItemValuableBox in carts.",
            Before031);

        internal static readonly ConfigKeyDefinition FallbackScaleFactor = Float(
            "FallbackScaleFactor", FeatureId.ItemShrink,
            "商店用品", "Shop Items",
            "商店用品缩小倍率", "Shop item scale factor",
            0.5f, 0.05f, 1.0f,
            "开启“启用商店用品缩小”后，枪、血包、工具等实用品放入购物车后的目标尺寸比例。也作为未知贵重物分类的兜底倍率。",
            "Target size factor for utility items such as guns, medkits, and tools when shop-item scaling is enabled. Also used as fallback for unknown valuable categories.",
            Before031);

        internal static readonly ConfigKeyDefinition EnemyInCartInstantKill = Bool(
            "EnemyInCartInstantKill", FeatureId.EnemyCrush,
            "车辆碾压", "Cart Crush",
            "敌人进车秒杀", "Instant-kill enemies in cart",
            true,
            "启用后，敌人或敌人刚体进入购物车时会立刻死亡（重生后再次进车同样有效）。此功能复刻 ShrinkerCartPlus 的敌人进车秒杀逻辑。",
            "When enabled, enemies or enemy rigidbodies die when entering a cart (again after they respawn). This mirrors ShrinkerCartPlus enemy-in-cart instant kill behavior.",
            Before031);

        internal static readonly ConfigKeyDefinition DynamicItemScanEnabled = Bool(
            "DynamicItemScanEnabled", FeatureId.ItemShrink,
            "性能", "Performance",
            "启用动态物品扫描", "Enable dynamic item scanning",
            true,
            "启用后，按当前被缩小跟踪的物品数量自动调整状态检查间隔（装备前恢复、被外部恢复检测、死亡头颅提取点/卡车检测）：物品少时使用最小间隔，物品多时逐步放宽到最大间隔，减少大量物品时的开销。关闭后始终使用最小间隔。离车恢复不受影响，始终按「离车防抖延迟」准时进行。",
            "When enabled, the interval of state checks for tracked shrunken objects (restore before equipping, external-restore detection, death-head extraction/truck checks) adapts to how many objects are tracked: few objects use the minimum interval, many objects gradually use up to the maximum interval to reduce overhead. When disabled, the minimum interval is always used. Leave-cart restore is not affected and always follows the leave-cart debounce.",
            Before031);

        internal static readonly ConfigKeyDefinition MinimumItemScanIntervalSeconds = Float(
            "MinimumItemScanIntervalSeconds", FeatureId.ItemShrink,
            "性能", "Performance",
            "最小物品扫描间隔", "Minimum item scan interval",
            0.15f, 0.05f, 1.0f,
            "物品较少（或关闭动态扫描）时的状态检查间隔。数值越小，装备前恢复等检查越及时，开销略高。",
            "State-check interval with few tracked objects (or when dynamic scanning is off). Lower values make checks such as restore-before-equip more prompt at slightly higher cost.",
            Before031);

        internal static readonly ConfigKeyDefinition MaximumItemScanIntervalSeconds = Float(
            "MaximumItemScanIntervalSeconds", FeatureId.ItemShrink,
            "性能", "Performance",
            "最大物品扫描间隔", "Maximum item scan interval",
            1.0f, 0.1f, 2.0f,
            "物品很多时允许的最长状态检查间隔。数值越大越省性能，装备前恢复等检查最多延后一个间隔。",
            "Longest state-check interval allowed with many tracked objects. Higher values cost less; checks such as restore-before-equip may be delayed by up to one interval.",
            Before031);

        internal static readonly ConfigKeyDefinition DebugLogging = Bool(
            "DebugLogging", FeatureId.Core,
            "诊断", "Diagnostics",
            "启用调试日志", "Enable debug logging",
            false,
            "启用后，在 BepInEx 日志中写入更多缩小、恢复、敌人进车和识别信息，并每 10 秒输出一次热路径计数。",
            "When enabled, writes additional shrink, restore, enemy-in-cart, and recognition details, plus 10-second hot-path counters, to BepInEx logs.",
            Before031);

        internal static IReadOnlyList<ConfigKeyDefinition> All => Ordered;

        private static ConfigKeyDefinition Bool(
            string id,
            FeatureId feature,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            bool defaultValue,
            string chineseDescription,
            string englishDescription,
            string since)
        {
            return Add(new ConfigKeyDefinition(
                id, feature, chineseSection, englishSection, chineseKey, englishKey,
                typeof(bool), defaultValue, null, null, chineseDescription, englishDescription, since));
        }

        private static ConfigKeyDefinition Float(
            string id,
            FeatureId feature,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            float defaultValue,
            float minimum,
            float maximum,
            string chineseDescription,
            string englishDescription,
            string since)
        {
            return Add(new ConfigKeyDefinition(
                id, feature, chineseSection, englishSection, chineseKey, englishKey,
                typeof(float), defaultValue, minimum, maximum, chineseDescription, englishDescription, since));
        }

        private static ConfigKeyDefinition CategoryEnabled(string id, string chineseSection, string englishSection)
        {
            return Bool(
                id + ".Enabled", FeatureId.ItemShrink,
                chineseSection, englishSection,
                "启用此分类缩小", "Enable this category",
                true,
                "启用后，该分类物品放入购物车时会自动缩小。",
                "When enabled, objects in this category shrink when placed into a cart.",
                Before031);
        }

        private static ConfigKeyDefinition CategoryFactor(string id, string chineseSection, string englishSection, float defaultValue)
        {
            return Float(
                id + ".Factor", FeatureId.ItemShrink,
                chineseSection, englishSection,
                "缩小倍率", "Scale factor",
                defaultValue, 0.05f, 1.0f,
                "该分类物品放入购物车后的目标尺寸比例。0.4 表示原尺寸的 40%。",
                "Target size factor for this category in carts. 0.4 means 40% of original size.",
                Before031);
        }

        private static ConfigKeyDefinition EnemyOrbFactor(string size, float defaultValue)
        {
            return Float(
                "EnemyOrb" + size + "ScaleFactor", FeatureId.ItemShrink,
                "敌人球", "Enemy Orbs",
                size + " 敌人球倍率", size + " enemy orb factor",
                defaultValue, 0.05f, 1.0f,
                size + " 敌人球放入购物车后的目标尺寸比例。",
                "Target size factor for " + size + " enemy orbs in carts.",
                Before031);
        }

        private static ConfigKeyDefinition Add(ConfigKeyDefinition definition)
        {
            Ordered.Add(definition);
            return definition;
        }
    }
}
