using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ShrinkCart
{
    internal enum ShrinkCategory
    {
        Tiny,
        Small,
        Medium,
        Big,
        Wide,
        Tall,
        VeryTall,
        EnemyOrbSmall,
        EnemyOrbMedium,
        EnemyOrbBig,
        EnemyOrbBerserker,
        Surplus,
        ValuableBox,
        Fallback
    }

    internal static class ModConfig
    {
        internal static ConfigEntry<bool> CartShrinkingEnabled;
        internal static ConfigEntry<float> CartScaleSpeed;
        internal static ConfigEntry<float> RestoreScaleSpeed;
        internal static ConfigEntry<float> CartLeaveDebounceSeconds;
        internal static ConfigEntry<float> ReshrinkCooldownSeconds;
        internal static ConfigEntry<bool> ScaleMassWithSize;
        internal static ConfigEntry<bool> ShrinkShopPlayerItems;
        internal static ConfigEntry<bool> PlayerScalingModuleEnabled;
        internal static ConfigEntry<float> PlayerCartScaleFactor;
        internal static ConfigEntry<float> PlayerCartGrowFactor;
        internal static ConfigEntry<float> PlayerCartStandTriggerSeconds;
        internal static ConfigEntry<float> PlayerCartExitGraceSeconds;
        internal static ConfigEntry<float> PlayerCartDetectionIntervalSeconds;
        internal static ConfigEntry<bool> RestorePlayerOnDamage;
        internal static ConfigEntry<bool> SuppressValuableDamageRestore;
        internal static ConfigEntry<bool> DeadHeadScalingEnabled;
        internal static ConfigEntry<float> DeadHeadScaleFactor;

        internal static ConfigEntry<bool> TinyEnabled;
        internal static ConfigEntry<float> TinyScaleFactor;
        internal static ConfigEntry<bool> SmallEnabled;
        internal static ConfigEntry<float> SmallScaleFactor;
        internal static ConfigEntry<bool> MediumEnabled;
        internal static ConfigEntry<float> MediumScaleFactor;
        internal static ConfigEntry<bool> BigEnabled;
        internal static ConfigEntry<float> BigScaleFactor;
        internal static ConfigEntry<bool> WideEnabled;
        internal static ConfigEntry<float> WideScaleFactor;
        internal static ConfigEntry<bool> TallEnabled;
        internal static ConfigEntry<float> TallScaleFactor;
        internal static ConfigEntry<bool> VeryTallEnabled;
        internal static ConfigEntry<float> VeryTallScaleFactor;

        internal static ConfigEntry<bool> EnemyOrbEnabled;
        internal static ConfigEntry<float> EnemyOrbSmallScaleFactor;
        internal static ConfigEntry<float> EnemyOrbMediumScaleFactor;
        internal static ConfigEntry<float> EnemyOrbBigScaleFactor;
        internal static ConfigEntry<float> EnemyOrbBerserkerScaleFactor;

        internal static ConfigEntry<bool> SurplusEnabled;
        internal static ConfigEntry<float> SurplusScaleFactor;
        internal static ConfigEntry<bool> ValuableBoxEnabled;
        internal static ConfigEntry<float> ValuableBoxScaleFactor;
        internal static ConfigEntry<float> FallbackScaleFactor;

        internal static ConfigEntry<bool> EnemyInCartInstantKill;
        internal static ConfigEntry<bool> DynamicItemScanEnabled;
        internal static ConfigEntry<float> MinimumItemScanIntervalSeconds;
        internal static ConfigEntry<float> MaximumItemScanIntervalSeconds;
        internal static ConfigEntry<bool> DebugLogging;
        internal static ConfigEntry<string> ConfigLanguage;

        internal static int ScalingConfigVersion;

        private const string ChineseLanguage = "中文";
        private const string EnglishLanguage = "English";
        private const string LanguageSection = "语言 / Language";
        private const string LanguageKey = "配置语言 / Config Language";
        private const string MassDefaultMigrationMarkerName = "ShrinkCart.v0.2.39.mass-default-enabled";

        private static bool _useEnglish;
        private static ConfigFile _boundConfig;
        private static readonly List<LocalizedDefinition> ActiveDefinitions = new List<LocalizedDefinition>();

        private static readonly PropertyInfo ConfigFileOrphanedEntriesProperty =
            typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void Bind(ConfigFile config)
        {
            _boundConfig = config;
            ActiveDefinitions.Clear();

            _useEnglish = IsEnglishLanguage(DetectConfiguredLanguage(config));

            CartShrinkingEnabled = Bind(
                config,
                "CartShrinkingEnabled",
                "购物车",
                "Cart",
                "启用购物车缩小",
                "Enable cart shrinking",
                true,
                Text(
                    "启用后，放进 C.A.R.T / 购物车的支持物品会自动缩小，取出后恢复。",
                    "When enabled, supported objects placed into a C.A.R.T. / shopping cart shrink automatically and restore after leaving."));

            CartScaleSpeed = Bind(
                config,
                "CartScaleSpeed",
                "购物车",
                "Cart",
                "放入时缩小速度",
                "Shrink speed",
                0.5f,
                Ranged(Text("ScalerCore 缩小动画速度。数值越大越快。", "ScalerCore shrink animation speed. Higher values are faster."), 0.1f, 20.0f));

            RestoreScaleSpeed = Bind(
                config,
                "RestoreScaleSpeed",
                "购物车",
                "Cart",
                "取出后放大速度",
                "Restore speed",
                0.2f,
                Ranged(Text("正常取出购物车后的 ScalerCore 放大动画速度。数值越小越慢。", "ScalerCore restore animation speed after leaving the cart. Lower values are slower."), 0.1f, 20.0f));

            CartLeaveDebounceSeconds = Bind(
                config,
                "CartLeaveDebounceSeconds",
                "购物车",
                "Cart",
                "离车防抖延迟",
                "Leave-cart debounce",
                0.5f,
                Ranged(Text("物品触发缩小后，离开购物车检测范围多久才开始恢复原尺寸。调高可减少车边缘抽搐。", "How long an item must remain outside the cart before restoring. Higher values reduce edge jitter."), 0.1f, 10.0f));

            ReshrinkCooldownSeconds = Bind(
                config,
                "ReshrinkCooldownSeconds",
                "购物车",
                "Cart",
                "恢复后重新缩小冷却",
                "Reshrink cooldown",
                LegacyFloat(config, 0.5f, "购物车", "取出后恢复延迟", "Cart", "RestoreGraceSeconds"),
                Ranged(Text("物品开始恢复后，等待多少秒才允许再次被购物车缩小。", "How many seconds to wait after restore starts before the cart may shrink the item again."), 0.05f, 10.0f));

            ScaleMassWithSize = Bind(
                config,
                "ScaleMassWithSize",
                "购物车",
                "Cart",
                "启用重量随缩放降低",
                "Scale mass with size",
                true,
                Text("启用后通过 ScalerCore 的 PreserveMass 选项允许支持对象按缩放倍率降低质量；关闭则只改变视觉尺寸并保持原始重量。", "When enabled, ShrinkCart passes ScalerCore options that allow supported objects to reduce mass with scale. When disabled, only visual size changes and original mass is preserved."));

            ShrinkShopPlayerItems = Bind(
                config,
                "ShrinkShopPlayerItems",
                "购物车",
                "Cart",
                "启用商店用品缩小",
                "Enable shop item shrinking",
                false,
                Text("启用后，枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品会使用默认缩小倍率。大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。", "When enabled, shop utility items such as guns, medkits, melee items, grenades, tools, drones, pearls, upgrades, trackers, and mines use the shop-item scale factor. Carts, C.A.R.T. Cannon, and C.A.R.T. Laser never shrink."));

            PlayerScalingModuleEnabled = Bind(
                config,
                "PlayerScalingModuleEnabled",
                "玩家缩放",
                "Player Scaling",
                "启用玩家缩放",
                "Enable player scaling",
                true,
                Text("默认开启。启用后才会运行玩家站车检测和玩家缩放；关闭时 ShrinkCart 不参与任何玩家缩放逻辑。", "Enabled by default. When enabled, ShrinkCart runs player cart-floor detection and player scaling. When disabled, ShrinkCart does not run player scaling logic."));

            PlayerCartScaleFactor = Bind(
                config,
                "PlayerCartScaleFactor",
                "玩家缩放",
                "Player Scaling",
                "玩家进车缩放倍率",
                "Player cart scale factor",
                0.55f,
                Ranged(Text("开启“启用玩家缩放”后，玩家站在购物车中心区域触发缩放时的目标尺寸比例。", "Target player size factor when player scaling is enabled and a player stands in the cart center area."), 0.05f, 1.0f));

            PlayerCartGrowFactor = Bind(
                config,
                "PlayerCartGrowFactor",
                "玩家缩放",
                "Player Scaling",
                "玩家进车增大倍率",
                "Player cart grow factor",
                1.5f,
                Ranged(Text("玩家站车循环中的增大目标比例。循环顺序为：原样 -> 缩小 -> 原样 -> 增大 -> 原样。", "Target player growth factor for the cart cycle: normal -> shrink -> normal -> grow -> normal."), 1.0f, 2.0f));

            PlayerCartStandTriggerSeconds = Bind(
                config,
                "PlayerCartStandTriggerSeconds",
                "玩家缩放",
                "Player Scaling",
                "玩家站车触发时间",
                "Player stand trigger time",
                2.0f,
                Ranged(Text("开启“启用玩家缩放”后，玩家站在购物车中心区域多久才执行一次缩小/恢复/增大循环。离开中心区域会重置计时。", "How long a player must stand in the cart center area before the shrink/restore/grow cycle advances. Leaving the center area resets the timer."), 0.25f, 10.0f));

            PlayerCartExitGraceSeconds = Bind(
                config,
                "PlayerCartExitGraceSeconds",
                "玩家缩放",
                "Player Scaling",
                "玩家离车判定宽容时间",
                "Player exit grace time",
                0.6f,
                Ranged(Text("玩家仍在购物车中心区域附近但短暂跳起、踩到车内物品或被货物顶起时，保留车内状态多久后才判定离开。设为 0 可恢复严格判定。", "How long to keep cart-inside state when the player briefly jumps, stands on cart cargo, or is pushed up while still near the cart center area. Set to 0 for strict detection."), 0.0f, 2.0f));

            PlayerCartDetectionIntervalSeconds = Bind(
                config,
                "PlayerCartDetectionIntervalSeconds",
                "玩家缩放",
                "Player Scaling",
                "玩家检测间隔",
                "Player detection interval",
                0.75f,
                Ranged(Text("玩家缩放开启时，主机多久检测一次玩家是否位于正式购物车底面投影范围内。数值越大越省性能，也越能防误触发。", "When player scaling is enabled, how often the host checks whether players are inside a regular cart floor projection. Higher values cost less and reduce accidental triggers."), 0.25f, 2.0f));

            RestorePlayerOnDamage = Bind(
                config,
                "RestorePlayerOnDamage",
                "玩家缩放",
                "Player Scaling",
                "启用玩家受伤后自动恢复",
                "Restore player on damage",
                true,
                Text("启用后，玩家缩放时使用 ScalerCore 的受伤/碰撞恢复链路；关闭后，玩家只会通过再次站车切换恢复。", "When enabled, ShrinkCart-scaled players use ScalerCore's damage/collision restore path. When disabled, players restore only by standing in the cart again."));

            SuppressValuableDamageRestore = Bind(
                config,
                "SuppressValuableDamageRestore",
                "购物车",
                "Cart",
                "防止碰撞弹回原尺寸",
                "Suppress collision restore",
                true,
                Text("启用后，贵重物品在购物车里轻微碰撞时不会立刻弹回原尺寸。ScalerCore 的安全恢复仍会保留。", "When enabled, valuables do not immediately restore from light collision value drops while in the cart. ScalerCore safety restore remains active."));

            DeadHeadScalingEnabled = Bind(
                config,
                "DeadHeadScalingEnabled",
                "死亡头颅",
                "Death Heads",
                "启用死亡头颅进车缩小",
                "Enable dead head cart shrinking",
                false,
                Text("默认关闭。启用后，已触发的玩家死亡头颅进入购物车时会缩小，离车、进入提取点、进入卡车或复活前会恢复。", "Disabled by default. When enabled, triggered player death heads shrink in carts and restore when leaving, entering extraction/truck flow, or before revive."));

            DeadHeadScaleFactor = Bind(
                config,
                "DeadHeadScaleFactor",
                "死亡头颅",
                "Death Heads",
                "死亡头颅缩小倍率",
                "Dead head scale factor",
                0.5f,
                Ranged(Text("死亡头颅进入购物车后的目标尺寸比例。", "Target size factor for death heads inside carts."), 0.05f, 1.0f));

            TinyEnabled = BindCategoryEnabled(config, "Tiny", "Tiny 微型贵重物", "Tiny valuables", true);
            TinyScaleFactor = BindCategoryFactor(config, "Tiny", "Tiny 微型贵重物", "Tiny valuables", 0.8f);
            SmallEnabled = BindCategoryEnabled(config, "Small", "Small 小贵重物", "Small valuables", true);
            SmallScaleFactor = BindCategoryFactor(config, "Small", "Small 小贵重物", "Small valuables", 0.6f);
            MediumEnabled = BindCategoryEnabled(config, "Medium", "Medium 中贵重物", "Medium valuables", true);
            MediumScaleFactor = BindCategoryFactor(config, "Medium", "Medium 中贵重物", "Medium valuables", 0.45f);
            BigEnabled = BindCategoryEnabled(config, "Big", "Big 大贵重物", "Big valuables", true);
            BigScaleFactor = BindCategoryFactor(config, "Big", "Big 大贵重物", "Big valuables", 0.4f);
            WideEnabled = BindCategoryEnabled(config, "Wide", "Wide 宽贵重物", "Wide valuables", true);
            WideScaleFactor = BindCategoryFactor(config, "Wide", "Wide 宽贵重物", "Wide valuables", 0.35f);
            TallEnabled = BindCategoryEnabled(config, "Tall", "Tall 高贵重物", "Tall valuables", true);
            TallScaleFactor = BindCategoryFactor(config, "Tall", "Tall 高贵重物", "Tall valuables", 0.35f);
            VeryTallEnabled = BindCategoryEnabled(config, "VeryTall", "VeryTall 超高贵重物", "VeryTall valuables", true);
            VeryTallScaleFactor = BindCategoryFactor(config, "VeryTall", "VeryTall 超高贵重物", "VeryTall valuables", 0.25f);

            EnemyOrbEnabled = Bind(
                config,
                "EnemyOrbEnabled",
                "敌人球",
                "Enemy Orbs",
                "启用敌人球缩小",
                "Enable enemy orb shrinking",
                true,
                Text("启用后，Enemy - Small/Medium/Big/Berserker 类贵重物会按下方倍率缩小。", "When enabled, Enemy - Small/Medium/Big/Berserker valuables shrink using the factors below."));

            EnemyOrbSmallScaleFactor = Bind(
                config,
                "EnemyOrbSmallScaleFactor",
                "敌人球",
                "Enemy Orbs",
                "Small 敌人球倍率",
                "Small enemy orb factor",
                0.8f,
                Ranged(Text("Small 敌人球放入购物车后的目标尺寸比例。", "Target size factor for Small enemy orbs in carts."), 0.05f, 1.0f));

            EnemyOrbMediumScaleFactor = Bind(
                config,
                "EnemyOrbMediumScaleFactor",
                "敌人球",
                "Enemy Orbs",
                "Medium 敌人球倍率",
                "Medium enemy orb factor",
                0.65f,
                Ranged(Text("Medium 敌人球放入购物车后的目标尺寸比例。", "Target size factor for Medium enemy orbs in carts."), 0.05f, 1.0f));

            EnemyOrbBigScaleFactor = Bind(
                config,
                "EnemyOrbBigScaleFactor",
                "敌人球",
                "Enemy Orbs",
                "Big 敌人球倍率",
                "Big enemy orb factor",
                0.45f,
                Ranged(Text("Big 敌人球放入购物车后的目标尺寸比例。", "Target size factor for Big enemy orbs in carts."), 0.05f, 1.0f));

            EnemyOrbBerserkerScaleFactor = Bind(
                config,
                "EnemyOrbBerserkerScaleFactor",
                "敌人球",
                "Enemy Orbs",
                "Berserker 敌人球倍率",
                "Berserker enemy orb factor",
                0.45f,
                Ranged(Text("Berserker 敌人球放入购物车后的目标尺寸比例。", "Target size factor for Berserker enemy orbs in carts."), 0.05f, 1.0f));

            SurplusEnabled = Bind(
                config,
                "SurplusEnabled",
                "特殊物品",
                "Special Objects",
                "启用钱袋/Surplus 缩小",
                "Enable money bag / Surplus shrinking",
                LegacyBool(config, true, "特殊物品", "启用 Surplus 缩小"),
                Text("启用后，钱袋/SurplusValuable 会使用单独倍率。", "When enabled, money bags / SurplusValuable use their own scale factor."));

            SurplusScaleFactor = Bind(
                config,
                "SurplusScaleFactor",
                "特殊物品",
                "Special Objects",
                "钱袋/Surplus 倍率",
                "Money bag / Surplus factor",
                LegacyFloat(config, 0.25f, "特殊物品", "Surplus 倍率"),
                Ranged(Text("钱袋/SurplusValuable 放入购物车后的目标尺寸比例。", "Target size factor for money bags / SurplusValuable in carts."), 0.05f, 1.0f));

            ValuableBoxEnabled = Bind(
                config,
                "ValuableBoxEnabled",
                "特殊物品",
                "Special Objects",
                "启用代币箱缩小",
                "Enable token box shrinking",
                true,
                Text("启用后，新版本抽奖用代币箱 ItemValuableBox 会使用单独倍率。", "When enabled, newer token boxes / ItemValuableBox objects use their own scale factor."));

            ValuableBoxScaleFactor = Bind(
                config,
                "ValuableBoxScaleFactor",
                "特殊物品",
                "Special Objects",
                "代币箱倍率",
                "Token box factor",
                0.4f,
                Ranged(Text("抽奖用代币箱 ItemValuableBox 放入购物车后的目标尺寸比例。", "Target size factor for token boxes / ItemValuableBox in carts."), 0.05f, 1.0f));

            FallbackScaleFactor = Bind(
                config,
                "FallbackScaleFactor",
                "商店用品",
                "Shop Items",
                "商店用品缩小倍率",
                "Shop item scale factor",
                LegacyFloat(config, 0.5f, "普通或未知物品", "默认缩小倍率"),
                Ranged(Text("开启“启用商店用品缩小”后，枪、血包、工具等实用品放入购物车后的目标尺寸比例。也作为未知贵重物分类的兜底倍率。", "Target size factor for utility items such as guns, medkits, and tools when shop-item scaling is enabled. Also used as fallback for unknown valuable categories."), 0.05f, 1.0f));

            EnemyInCartInstantKill = Bind(
                config,
                "EnemyInCartInstantKill",
                "车辆碾压",
                "Cart Crush",
                "敌人进车秒杀",
                "Instant-kill enemies in cart",
                LegacyBool(config, true, "车辆碾压", "车辆碾压秒杀敌人", "VehicleCrush", "InstantKillEnemies"),
                Text("启用后，敌人或敌人刚体进入购物车时会立刻死亡。此功能复刻 ShrinkerCartPlus 的敌人进车秒杀逻辑。", "When enabled, enemies or enemy rigidbodies die when entering a cart. This mirrors ShrinkerCartPlus enemy-in-cart instant kill behavior."));

            DynamicItemScanEnabled = Bind(
                config,
                "DynamicItemScanEnabled",
                "性能",
                "Performance",
                "启用动态物品扫描",
                "Enable dynamic item scanning",
                true,
                Text("启用后，ShrinkCart 会根据当前跟踪的缩小物品数量自动拉长状态扫描间隔，减少车内物品很多时的卡顿。", "When enabled, ShrinkCart lengthens state scan intervals based on the number of tracked shrunken objects to reduce stutter with many cart items."));

            MinimumItemScanIntervalSeconds = Bind(
                config,
                "MinimumItemScanIntervalSeconds",
                "性能",
                "Performance",
                "最小物品扫描间隔",
                "Minimum item scan interval",
                0.15f,
                Ranged(Text("少量物品时的最短状态扫描间隔。数值越小，离车恢复越灵敏，但开销更高。", "Shortest state scan interval for few items. Lower values make restore more responsive but cost more."), 0.05f, 1.0f));

            MaximumItemScanIntervalSeconds = Bind(
                config,
                "MaximumItemScanIntervalSeconds",
                "性能",
                "Performance",
                "最大物品扫描间隔",
                "Maximum item scan interval",
                1.0f,
                Ranged(Text("大量物品时允许使用的最长状态扫描间隔。数值越大越省性能，但离车恢复最多会延后一个扫描间隔。", "Longest state scan interval allowed for many items. Higher values cost less, but leaving-cart restore may be delayed by up to one scan interval."), 0.1f, 2.0f));

            DebugLogging = Bind(
                config,
                "DebugLogging",
                "诊断",
                "Diagnostics",
                "启用调试日志",
                "Enable debug logging",
                false,
                Text("启用后，在 BepInEx 日志中写入更多缩小、恢复、敌人进车和碾压识别信息。", "When enabled, writes additional shrink, restore, enemy-in-cart, map compatibility, and recognition details to BepInEx logs."));

            ConfigLanguage = config.Bind(
                LanguageSection,
                LanguageKey,
                _useEnglish ? EnglishLanguage : ChineseLanguage,
                new ConfigDescription(
                    "切换 ShrinkCart 配置文件语言。Switch ShrinkCart config file language. Apply and restart/reload to rebuild all config keys.",
                    new AcceptableValueList<string>(ChineseLanguage, EnglishLanguage)));

            WatchScaling(CartShrinkingEnabled);
            WatchScaling(CartScaleSpeed);
            WatchScaling(RestoreScaleSpeed);
            WatchScaling(CartLeaveDebounceSeconds);
            WatchScaling(ReshrinkCooldownSeconds);
            WatchScaling(ScaleMassWithSize);
            WatchScaling(ShrinkShopPlayerItems);
            WatchScaling(PlayerScalingModuleEnabled);
            WatchScaling(PlayerCartScaleFactor);
            WatchScaling(PlayerCartGrowFactor);
            WatchScaling(PlayerCartStandTriggerSeconds);
            WatchScaling(PlayerCartExitGraceSeconds);
            WatchScaling(PlayerCartDetectionIntervalSeconds);
            WatchScaling(RestorePlayerOnDamage);
            WatchScaling(SuppressValuableDamageRestore);
            WatchScaling(DeadHeadScalingEnabled);
            WatchScaling(DeadHeadScaleFactor);
            WatchScaling(TinyEnabled);
            WatchScaling(TinyScaleFactor);
            WatchScaling(SmallEnabled);
            WatchScaling(SmallScaleFactor);
            WatchScaling(MediumEnabled);
            WatchScaling(MediumScaleFactor);
            WatchScaling(BigEnabled);
            WatchScaling(BigScaleFactor);
            WatchScaling(WideEnabled);
            WatchScaling(WideScaleFactor);
            WatchScaling(TallEnabled);
            WatchScaling(TallScaleFactor);
            WatchScaling(VeryTallEnabled);
            WatchScaling(VeryTallScaleFactor);
            WatchScaling(EnemyOrbEnabled);
            WatchScaling(EnemyOrbSmallScaleFactor);
            WatchScaling(EnemyOrbMediumScaleFactor);
            WatchScaling(EnemyOrbBigScaleFactor);
            WatchScaling(EnemyOrbBerserkerScaleFactor);
            WatchScaling(SurplusEnabled);
            WatchScaling(SurplusScaleFactor);
            WatchScaling(ValuableBoxEnabled);
            WatchScaling(ValuableBoxScaleFactor);
            WatchScaling(FallbackScaleFactor);
            MigrateMassScalingDefaultOnce();
            RemoveDeprecatedEntries(config);
            RemoveInactiveLanguageEntries(config);
        }

        internal static float SafeScaleSpeed()
        {
            return Mathf.Clamp(CartScaleSpeed.Value, 0.1f, 20.0f);
        }

        internal static float SafeRestoreScaleSpeed()
        {
            return Mathf.Clamp(RestoreScaleSpeed.Value, 0.1f, 20.0f);
        }

        internal static float SafeCartLeaveDebounceSeconds()
        {
            return Mathf.Clamp(CartLeaveDebounceSeconds.Value, 0.1f, 10.0f);
        }

        internal static float SafeReshrinkCooldownSeconds()
        {
            return Mathf.Clamp(ReshrinkCooldownSeconds.Value, 0.05f, 10.0f);
        }

        internal static bool PlayerScalingEnabled()
        {
            return CartShrinkingEnabled != null &&
                   PlayerScalingModuleEnabled != null &&
                   CartShrinkingEnabled.Value &&
                   PlayerScalingModuleEnabled.Value;
        }

        internal static bool ShouldPreserveMass()
        {
            return ScaleMassWithSize == null || !ScaleMassWithSize.Value;
        }

        internal static float SafePlayerCartScaleFactor()
        {
            return Mathf.Clamp(PlayerCartScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafePlayerCartGrowFactor()
        {
            return PlayerCartGrowFactor == null
                ? 1.5f
                : Mathf.Clamp(PlayerCartGrowFactor.Value, 1.0f, 2.0f);
        }

        internal static float SafePlayerCartStandTriggerSeconds()
        {
            return Mathf.Clamp(PlayerCartStandTriggerSeconds.Value, 0.25f, 10.0f);
        }

        internal static float SafePlayerCartExitGraceSeconds()
        {
            return Mathf.Clamp(PlayerCartExitGraceSeconds.Value, 0.0f, 2.0f);
        }

        internal static float SafePlayerCartDetectionIntervalSeconds()
        {
            return Mathf.Clamp(PlayerCartDetectionIntervalSeconds.Value, 0.25f, 2.0f);
        }

        internal static bool DynamicItemScanEnabledValue()
        {
            return DynamicItemScanEnabled != null && DynamicItemScanEnabled.Value;
        }

        internal static bool DeadHeadScalingEnabledValue()
        {
            return CartShrinkingEnabled != null &&
                   DeadHeadScalingEnabled != null &&
                   CartShrinkingEnabled.Value &&
                   DeadHeadScalingEnabled.Value;
        }

        internal static float SafeDeadHeadScaleFactor()
        {
            return DeadHeadScaleFactor == null
                ? 0.5f
                : Mathf.Clamp(DeadHeadScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafeMinimumItemScanIntervalSeconds()
        {
            return MinimumItemScanIntervalSeconds == null
                ? 0.15f
                : Mathf.Clamp(MinimumItemScanIntervalSeconds.Value, 0.05f, 1.0f);
        }

        internal static float SafeMaximumItemScanIntervalSeconds()
        {
            float minimum = SafeMinimumItemScanIntervalSeconds();
            float configured = MaximumItemScanIntervalSeconds == null
                ? 1.0f
                : Mathf.Clamp(MaximumItemScanIntervalSeconds.Value, 0.1f, 2.0f);
            return Mathf.Max(minimum, configured);
        }

        internal static bool TryGetScaleFactor(ShrinkCategory category, out float factor)
        {
            switch (category)
            {
                case ShrinkCategory.Tiny:
                    return TryCategory(TinyEnabled, TinyScaleFactor, out factor);
                case ShrinkCategory.Small:
                    return TryCategory(SmallEnabled, SmallScaleFactor, out factor);
                case ShrinkCategory.Medium:
                    return TryCategory(MediumEnabled, MediumScaleFactor, out factor);
                case ShrinkCategory.Big:
                    return TryCategory(BigEnabled, BigScaleFactor, out factor);
                case ShrinkCategory.Wide:
                    return TryCategory(WideEnabled, WideScaleFactor, out factor);
                case ShrinkCategory.Tall:
                    return TryCategory(TallEnabled, TallScaleFactor, out factor);
                case ShrinkCategory.VeryTall:
                    return TryCategory(VeryTallEnabled, VeryTallScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbSmall:
                    return TryEnemyOrb(EnemyOrbSmallScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbMedium:
                    return TryEnemyOrb(EnemyOrbMediumScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbBig:
                    return TryEnemyOrb(EnemyOrbBigScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbBerserker:
                    return TryEnemyOrb(EnemyOrbBerserkerScaleFactor, out factor);
                case ShrinkCategory.Surplus:
                    return TryCategory(SurplusEnabled, SurplusScaleFactor, out factor);
                case ShrinkCategory.ValuableBox:
                    return TryCategory(ValuableBoxEnabled, ValuableBoxScaleFactor, out factor);
                default:
                    factor = SafeFactor(FallbackScaleFactor.Value);
                    return true;
            }
        }

        private static ConfigEntry<bool> BindCategoryEnabled(
            ConfigFile config,
            string id,
            string chineseSection,
            string englishSection,
            bool defaultValue)
        {
            return Bind(
                config,
                id + ".Enabled",
                chineseSection,
                englishSection,
                "启用此分类缩小",
                "Enable this category",
                defaultValue,
                Text("启用后，该分类物品放入购物车时会自动缩小。", "When enabled, objects in this category shrink when placed into a cart."));
        }

        private static ConfigEntry<float> BindCategoryFactor(
            ConfigFile config,
            string id,
            string chineseSection,
            string englishSection,
            float defaultValue)
        {
            return Bind(
                config,
                id + ".Factor",
                chineseSection,
                englishSection,
                "缩小倍率",
                "Scale factor",
                defaultValue,
                Ranged(Text("该分类物品放入购物车后的目标尺寸比例。0.4 表示原尺寸的 40%。", "Target size factor for this category in carts. 0.4 means 40% of original size."), 0.05f, 1.0f));
        }

        private static ConfigDescription Ranged(string description, float min, float max)
        {
            return new ConfigDescription(description, new AcceptableValueRange<float>(min, max));
        }

        private static string Text(string chinese, string english)
        {
            return _useEnglish ? english : chinese;
        }

        private static ConfigEntry<T> Bind<T>(
            ConfigFile config,
            string id,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            T defaultValue,
            string description)
        {
            return Bind(config, id, chineseSection, englishSection, chineseKey, englishKey, defaultValue, new ConfigDescription(description));
        }

        private static ConfigEntry<T> Bind<T>(
            ConfigFile config,
            string id,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            T defaultValue,
            ConfigDescription description)
        {
            T value = ReadLocalizedValue(config, id, chineseSection, englishSection, chineseKey, englishKey, defaultValue);
            string section = _useEnglish ? englishSection : chineseSection;
            string key = _useEnglish ? englishKey : chineseKey;
            ActiveDefinitions.Add(new LocalizedDefinition(id, chineseSection, englishSection, chineseKey, englishKey));
            return config.Bind(section, key, value, description);
        }

        private static T ReadLocalizedValue<T>(
            ConfigFile config,
            string id,
            string chineseSection,
            string englishSection,
            string chineseKey,
            string englishKey,
            T defaultValue)
        {
            ConfigEntry<T> entry;
            T englishOrphanedValue;
            T chineseOrphanedValue;
            if (_useEnglish)
            {
                if (config.TryGetEntry(englishSection, englishKey, out entry))
                {
                    return entry.Value;
                }

                if (config.TryGetEntry(chineseSection, chineseKey, out entry))
                {
                    return entry.Value;
                }

                if (TryGetOrphanedValue(config, englishSection, englishKey, out englishOrphanedValue))
                {
                    return englishOrphanedValue;
                }

                if (TryGetOrphanedValue(config, chineseSection, chineseKey, out chineseOrphanedValue))
                {
                    return chineseOrphanedValue;
                }
            }
            else
            {
                if (config.TryGetEntry(chineseSection, chineseKey, out entry))
                {
                    return entry.Value;
                }

                if (config.TryGetEntry(englishSection, englishKey, out entry))
                {
                    return entry.Value;
                }

                if (TryGetOrphanedValue(config, chineseSection, chineseKey, out chineseOrphanedValue))
                {
                    return chineseOrphanedValue;
                }

                if (TryGetOrphanedValue(config, englishSection, englishKey, out englishOrphanedValue))
                {
                    return englishOrphanedValue;
                }
            }

            return defaultValue;
        }

        private static string DetectConfiguredLanguage(ConfigFile config)
        {
            ConfigEntry<string> entry;
            if (config.TryGetEntry(LanguageSection, LanguageKey, out entry))
            {
                return NormalizeLanguage(entry.Value);
            }

            string orphanedValue;
            if (TryGetOrphanedValue(config, LanguageSection, LanguageKey, out orphanedValue))
            {
                return NormalizeLanguage(orphanedValue);
            }

            return ChineseLanguage;
        }

        private static bool TryGetOrphanedValue<T>(ConfigFile config, string section, string key, out T value)
        {
            value = default(T);
            IDictionary orphanedEntries = ConfigFileOrphanedEntriesProperty == null
                ? null
                : ConfigFileOrphanedEntriesProperty.GetValue(config, null) as IDictionary;
            if (orphanedEntries == null)
            {
                return false;
            }

            ConfigDefinition definition = new ConfigDefinition(section, key);
            if (!orphanedEntries.Contains(definition))
            {
                return false;
            }

            string rawValue = orphanedEntries[definition] as string;
            if (rawValue == null)
            {
                return false;
            }

            try
            {
                Type targetType = typeof(T);
                if (targetType == typeof(string))
                {
                    value = (T)(object)rawValue;
                    return true;
                }

                if (targetType == typeof(bool))
                {
                    bool parsed;
                    if (bool.TryParse(rawValue, out parsed))
                    {
                        value = (T)(object)parsed;
                        return true;
                    }
                }

                if (targetType == typeof(float))
                {
                    float parsed;
                    if (float.TryParse(rawValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed) ||
                        float.TryParse(rawValue, out parsed))
                    {
                        value = (T)(object)parsed;
                        return true;
                    }
                }

                if (targetType == typeof(int))
                {
                    int parsed;
                    if (int.TryParse(rawValue, out parsed))
                    {
                        value = (T)(object)parsed;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool IsEnglishLanguage(string value)
        {
            return NormalizeLanguage(value) == EnglishLanguage;
        }

        private static string NormalizeLanguage(string value)
        {
            if (string.Equals(value, EnglishLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "EN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "English", StringComparison.OrdinalIgnoreCase))
            {
                return EnglishLanguage;
            }

            return ChineseLanguage;
        }

        private static bool TryCategory(ConfigEntry<bool> enabled, ConfigEntry<float> factorEntry, out float factor)
        {
            factor = SafeFactor(factorEntry.Value);
            return enabled.Value;
        }

        private static bool TryEnemyOrb(ConfigEntry<float> factorEntry, out float factor)
        {
            factor = SafeFactor(factorEntry.Value);
            return EnemyOrbEnabled.Value;
        }

        private static float SafeFactor(float value)
        {
            return Mathf.Clamp(value, 0.05f, 1.0f);
        }

        private static void MigrateMassScalingDefaultOnce()
        {
            if (ScaleMassWithSize == null || _boundConfig == null)
            {
                return;
            }

            string markerPath = GetMassDefaultMigrationMarkerPath();
            if (File.Exists(markerPath))
            {
                return;
            }

            ScaleMassWithSize.Value = true;

            try
            {
                _boundConfig.Save();
                File.WriteAllText(markerPath, "0.2.39");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to persist mass scaling default migration marker: " + ex.Message);
            }
        }

        private static string GetMassDefaultMigrationMarkerPath()
        {
            string configPath = _boundConfig == null ? null : _boundConfig.ConfigFilePath;
            string directory = string.IsNullOrEmpty(configPath)
                ? Paths.ConfigPath
                : Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Paths.ConfigPath;
            }

            return Path.Combine(directory, MassDefaultMigrationMarkerName);
        }

        private static bool LegacyBool(ConfigFile config, bool defaultValue, params string[] sectionKeyPairs)
        {
            for (int i = 0; i + 1 < sectionKeyPairs.Length; i += 2)
            {
                ConfigEntry<bool> entry;
                if (config.TryGetEntry(sectionKeyPairs[i], sectionKeyPairs[i + 1], out entry))
                {
                    return entry.Value;
                }
            }

            return defaultValue;
        }

        private static float LegacyFloat(ConfigFile config, float defaultValue, params string[] sectionKeyPairs)
        {
            for (int i = 0; i + 1 < sectionKeyPairs.Length; i += 2)
            {
                ConfigEntry<float> entry;
                if (config.TryGetEntry(sectionKeyPairs[i], sectionKeyPairs[i + 1], out entry))
                {
                    return entry.Value;
                }
            }

            return defaultValue;
        }

        private static void RemoveDeprecatedEntries(ConfigFile config)
        {
            bool changed = false;

            changed |= RemoveDeprecatedEntry(config, "Cart", "Enabled");
            changed |= RemoveDeprecatedEntry(config, "Cart", "ScaleFactor");
            changed |= RemoveDeprecatedEntry(config, "Cart", "ScaleSpeed");
            changed |= RemoveDeprecatedEntry(config, "Cart", "RestoreGraceSeconds");
            changed |= RemoveDeprecatedEntry(config, "Cart", "PreserveMass");
            changed |= RemoveDeprecatedEntry(config, "Cart", "ShrinkNonValuableItems");
            changed |= RemoveDeprecatedEntry(config, "Cart", "SuppressValuableDamageRestore");
            changed |= RemoveDeprecatedEntry(config, "Diagnostics", "DebugLogging");
            changed |= RemoveDeprecatedEntry(config, "购物车", "保持原始重量");

            changed |= RemoveDeprecatedEntry(config, "VehicleCrush", "InstantKillPlayers");
            changed |= RemoveDeprecatedEntry(config, "VehicleCrush", "InstantKillEnemies");
            changed |= RemoveDeprecatedEntry(config, "视觉", "隐藏缩放闪光");
            changed |= RemoveDeprecatedEntry(config, "车辆碾压", "车辆碾压秒杀玩家");

            changed |= RemoveDeprecatedEntry(config, "提取点复活兼容", "启用提取点复活");
            changed |= RemoveDeprecatedEntry(config, "提取点复活兼容", "复活前稳定检测时间");
            changed |= RemoveDeprecatedEntry(config, "提取点复活兼容", "复活检测间隔");
            changed |= RemoveDeprecatedEntry(config, "提取点复活兼容", "拦截外部立即复活调用");

            changed |= RemoveDeprecatedEntry(config, "购物车", "商店用品也缩小");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家也缩小");
            changed |= RemoveDeprecatedEntry(config, "购物车", "启用实验性玩家缩放");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家死亡前自动恢复");
            changed |= RemoveDeprecatedEntry(config, "购物车", "启用玩家缩放");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家进车缩放倍率");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家站车触发时间");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家离车判定宽容时间");
            changed |= RemoveDeprecatedEntry(config, "购物车", "玩家进车切换间隔");
            changed |= RemoveDeprecatedEntry(config, "购物车", "普通物品也缩小");
            changed |= RemoveDeprecatedEntry(config, "购物车", "商店/人物用品也缩小");
            changed |= RemoveDeprecatedEntry(config, "购物车", "防止车辆互相重叠");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆硬碰撞修正强度");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆最大单帧修正距离");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆挤压速度清除");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆临时忽略碰撞时间（已废弃）");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆临时忽略碰撞时间");
            changed |= RemoveDeprecatedEntry(config, "购物车", "车辆脱困强度");

            changed |= RemoveDeprecatedEntry(config, "玩家缩放", "旧版玩家缩放开关（已停用）");
            changed |= RemoveDeprecatedEntry(config, "地图兼容", "Minecraft Stronghold 普通门进车破碎");
            changed |= RemoveDeprecatedEntry(config, "地图兼容", "Minecraft Stronghold 门破碎确认时间");
            changed |= RemoveDeprecatedEntry(config, "Map Compatibility", "Minecraft Stronghold door shatter in cart");
            changed |= RemoveDeprecatedEntry(config, "Map Compatibility", "Minecraft Stronghold door shatter confirmation time");

            if (changed)
            {
                config.Save();
            }
        }

        private static void RemoveInactiveLanguageEntries(ConfigFile config)
        {
            bool changed = false;
            for (int i = 0; i < ActiveDefinitions.Count; i++)
            {
                LocalizedDefinition definition = ActiveDefinitions[i];
                if (_useEnglish)
                {
                    changed |= RemoveDeprecatedEntry(config, definition.ChineseSection, definition.ChineseKey);
                }
                else
                {
                    changed |= RemoveDeprecatedEntry(config, definition.EnglishSection, definition.EnglishKey);
                }
            }

            if (changed)
            {
                config.Save();
            }
        }

        private static bool RemoveDeprecatedEntry(ConfigFile config, string section, string key)
        {
            ConfigDefinition definition = new ConfigDefinition(section, key);
            bool removed = config.Remove(definition);

            IDictionary orphanedEntries = ConfigFileOrphanedEntriesProperty == null
                ? null
                : ConfigFileOrphanedEntriesProperty.GetValue(config, null) as IDictionary;
            if (orphanedEntries != null && orphanedEntries.Contains(definition))
            {
                orphanedEntries.Remove(definition);
                removed = true;
            }

            return removed;
        }

        private static void WatchScaling<T>(ConfigEntry<T> entry)
        {
            entry.SettingChanged += OnScalingSettingChanged;
        }

        private static void OnScalingSettingChanged(object sender, EventArgs args)
        {
            ScalingConfigVersion++;
        }

        private sealed class LocalizedDefinition
        {
            internal readonly string Id;
            internal readonly string ChineseSection;
            internal readonly string EnglishSection;
            internal readonly string ChineseKey;
            internal readonly string EnglishKey;

            internal LocalizedDefinition(
                string id,
                string chineseSection,
                string englishSection,
                string chineseKey,
                string englishKey)
            {
                Id = id;
                ChineseSection = chineseSection;
                EnglishSection = englishSection;
                ChineseKey = chineseKey;
                EnglishKey = englishKey;
            }
        }
    }
}
