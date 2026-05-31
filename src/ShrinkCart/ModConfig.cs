using System;
using System.Collections;
using System.Reflection;
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
        internal static ConfigEntry<float> PlayerCartStandTriggerSeconds;
        internal static ConfigEntry<float> PlayerCartExitGraceSeconds;
        internal static ConfigEntry<float> PlayerCartDetectionIntervalSeconds;
        internal static ConfigEntry<bool> RestorePlayerOnDamage;
        internal static ConfigEntry<bool> SuppressValuableDamageRestore;

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

        internal static int ScalingConfigVersion;

        private static readonly PropertyInfo ConfigFileOrphanedEntriesProperty =
            typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void Bind(ConfigFile config)
        {
            CartShrinkingEnabled = config.Bind(
                "购物车",
                "启用购物车缩小",
                true,
                "启用后，放进 C.A.R.T / 购物车的支持物品会自动缩小，取出后恢复。");

            CartScaleSpeed = config.Bind(
                "购物车",
                "放入时缩小速度",
                0.5f,
                Ranged("ScalerCore 缩小动画速度。数值越大越快。", 0.1f, 20.0f));

            RestoreScaleSpeed = config.Bind(
                "购物车",
                "取出后放大速度",
                0.2f,
                Ranged("正常取出购物车后的 ScalerCore 放大动画速度。数值越小越慢。", 0.1f, 20.0f));

            CartLeaveDebounceSeconds = config.Bind(
                "购物车",
                "离车防抖延迟",
                0.5f,
                Ranged("物品触发缩小后，离开购物车检测范围多久才开始恢复原尺寸。调高可减少车边缘抽搐。", 0.1f, 10.0f));

            ReshrinkCooldownSeconds = config.Bind(
                "购物车",
                "恢复后重新缩小冷却",
                LegacyFloat(config, 0.5f, "购物车", "取出后恢复延迟", "Cart", "RestoreGraceSeconds"),
                Ranged("物品开始恢复后，等待多少秒才允许再次被购物车缩小。", 0.05f, 10.0f));

            ScaleMassWithSize = config.Bind(
                "购物车",
                "启用重量随缩放降低",
                !LegacyBool(config, true, "购物车", "保持原始重量", "Cart", "PreserveMass"),
                "启用后通过 ScalerCore 的 PreserveMass 选项允许支持对象按缩放倍率降低质量；关闭则只改变视觉尺寸并保持原始重量。");

            ShrinkShopPlayerItems = config.Bind(
                "购物车",
                "启用商店用品缩小",
                false,
                "启用后，枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品会使用默认缩小倍率。大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。");

            PlayerScalingModuleEnabled = config.Bind(
                "玩家缩放",
                "启用玩家缩放",
                true,
                "默认开启。启用后才会运行玩家站车检测和玩家缩放；关闭时 ShrinkCart 不参与任何玩家缩放逻辑。");

            PlayerCartScaleFactor = config.Bind(
                "玩家缩放",
                "玩家进车缩放倍率",
                0.55f,
                Ranged("开启“启用玩家缩放”后，玩家站在购物车中心区域触发缩放时的目标尺寸比例。", 0.05f, 1.0f));

            PlayerCartStandTriggerSeconds = config.Bind(
                "玩家缩放",
                "玩家站车触发时间",
                2.0f,
                Ranged("开启“启用玩家缩放”后，玩家站在购物车中心区域多久才切换缩小/恢复。离开中心区域会重置计时。", 0.25f, 10.0f));

            PlayerCartExitGraceSeconds = config.Bind(
                "玩家缩放",
                "玩家离车判定宽容时间",
                0.6f,
                Ranged("玩家仍在购物车中心区域附近但短暂跳起、踩到车内物品或被货物顶起时，保留车内状态多久后才判定离开。设为 0 可恢复严格判定。", 0.0f, 2.0f));

            PlayerCartDetectionIntervalSeconds = config.Bind(
                "玩家缩放",
                "玩家检测间隔",
                0.75f,
                Ranged("玩家缩放开启时，主机多久检测一次玩家是否位于正式购物车底面投影范围内。数值越大越省性能，也越能防误触发。", 0.25f, 2.0f));

            RestorePlayerOnDamage = config.Bind(
                "玩家缩放",
                "启用玩家受伤后自动恢复",
                true,
                "启用后，玩家缩小时使用 ScalerCore 的受伤/碰撞恢复链路；关闭后，玩家只会通过再次站车切换恢复。");

            SuppressValuableDamageRestore = config.Bind(
                "购物车",
                "防止碰撞弹回原尺寸",
                true,
                "启用后，贵重物品在购物车里轻微碰撞时不会立刻弹回原尺寸。ScalerCore 的安全恢复仍会保留。");

            TinyEnabled = BindCategoryEnabled(config, "Tiny 微型贵重物", true);
            TinyScaleFactor = BindCategoryFactor(config, "Tiny 微型贵重物", 0.8f);
            SmallEnabled = BindCategoryEnabled(config, "Small 小贵重物", true);
            SmallScaleFactor = BindCategoryFactor(config, "Small 小贵重物", 0.6f);
            MediumEnabled = BindCategoryEnabled(config, "Medium 中贵重物", true);
            MediumScaleFactor = BindCategoryFactor(config, "Medium 中贵重物", 0.45f);
            BigEnabled = BindCategoryEnabled(config, "Big 大贵重物", true);
            BigScaleFactor = BindCategoryFactor(config, "Big 大贵重物", 0.4f);
            WideEnabled = BindCategoryEnabled(config, "Wide 宽贵重物", true);
            WideScaleFactor = BindCategoryFactor(config, "Wide 宽贵重物", 0.35f);
            TallEnabled = BindCategoryEnabled(config, "Tall 高贵重物", true);
            TallScaleFactor = BindCategoryFactor(config, "Tall 高贵重物", 0.35f);
            VeryTallEnabled = BindCategoryEnabled(config, "VeryTall 超高贵重物", true);
            VeryTallScaleFactor = BindCategoryFactor(config, "VeryTall 超高贵重物", 0.25f);

            EnemyOrbEnabled = config.Bind(
                "敌人球",
                "启用敌人球缩小",
                true,
                "启用后，Enemy - Small/Medium/Big/Berserker 类贵重物会按下方倍率缩小。");

            EnemyOrbSmallScaleFactor = config.Bind(
                "敌人球",
                "Small 敌人球倍率",
                0.8f,
                Ranged("Small 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbMediumScaleFactor = config.Bind(
                "敌人球",
                "Medium 敌人球倍率",
                0.65f,
                Ranged("Medium 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbBigScaleFactor = config.Bind(
                "敌人球",
                "Big 敌人球倍率",
                0.45f,
                Ranged("Big 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbBerserkerScaleFactor = config.Bind(
                "敌人球",
                "Berserker 敌人球倍率",
                0.45f,
                Ranged("Berserker 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            SurplusEnabled = config.Bind(
                "特殊物品",
                "启用钱袋/Surplus 缩小",
                LegacyBool(config, true, "特殊物品", "启用 Surplus 缩小"),
                "启用后，钱袋/SurplusValuable 会使用单独倍率。");

            SurplusScaleFactor = config.Bind(
                "特殊物品",
                "钱袋/Surplus 倍率",
                LegacyFloat(config, 0.25f, "特殊物品", "Surplus 倍率"),
                Ranged("钱袋/SurplusValuable 放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            ValuableBoxEnabled = config.Bind(
                "特殊物品",
                "启用代币箱缩小",
                true,
                "启用后，新版本抽奖用代币箱 ItemValuableBox 会使用单独倍率。");

            ValuableBoxScaleFactor = config.Bind(
                "特殊物品",
                "代币箱倍率",
                0.4f,
                Ranged("抽奖用代币箱 ItemValuableBox 放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            FallbackScaleFactor = config.Bind(
                "商店用品",
                "商店用品缩小倍率",
                LegacyFloat(config, 0.5f, "普通或未知物品", "默认缩小倍率"),
                Ranged("开启“启用商店用品缩小”后，枪、血包、工具等实用品放入购物车后的目标尺寸比例。也作为未知贵重物分类的兜底倍率。", 0.05f, 1.0f));

            EnemyInCartInstantKill = config.Bind(
                "车辆碾压",
                "敌人进车秒杀",
                LegacyBool(config, true, "车辆碾压", "车辆碾压秒杀敌人", "VehicleCrush", "InstantKillEnemies"),
                "启用后，敌人或敌人刚体进入购物车时会立刻死亡。此功能复刻 ShrinkerCartPlus 的敌人进车秒杀逻辑。");

            DynamicItemScanEnabled = config.Bind(
                "性能",
                "启用动态物品扫描",
                true,
                "启用后，ShrinkCart 会根据当前跟踪的缩小物品数量自动拉长状态扫描间隔，减少车内物品很多时的卡顿。");

            MinimumItemScanIntervalSeconds = config.Bind(
                "性能",
                "最小物品扫描间隔",
                0.15f,
                Ranged("少量物品时的最短状态扫描间隔。数值越小，离车恢复越灵敏，但开销更高。", 0.05f, 1.0f));

            MaximumItemScanIntervalSeconds = config.Bind(
                "性能",
                "最大物品扫描间隔",
                1.0f,
                Ranged("大量物品时允许使用的最长状态扫描间隔。数值越大越省性能，但离车恢复最多会延后一个扫描间隔。", 0.1f, 2.0f));

            DebugLogging = config.Bind(
                "诊断",
                "启用调试日志",
                false,
                "启用后，在 BepInEx 日志中写入更多缩小、恢复、敌人进车和碾压识别信息。");

            WatchScaling(CartShrinkingEnabled);
            WatchScaling(CartScaleSpeed);
            WatchScaling(RestoreScaleSpeed);
            WatchScaling(CartLeaveDebounceSeconds);
            WatchScaling(ReshrinkCooldownSeconds);
            WatchScaling(ScaleMassWithSize);
            WatchScaling(ShrinkShopPlayerItems);
            WatchScaling(PlayerScalingModuleEnabled);
            WatchScaling(PlayerCartScaleFactor);
            WatchScaling(PlayerCartStandTriggerSeconds);
            WatchScaling(PlayerCartExitGraceSeconds);
            WatchScaling(PlayerCartDetectionIntervalSeconds);
            WatchScaling(RestorePlayerOnDamage);
            WatchScaling(SuppressValuableDamageRestore);
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

            RemoveDeprecatedEntries(config);
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

        private static ConfigEntry<bool> BindCategoryEnabled(ConfigFile config, string section, bool defaultValue)
        {
            return config.Bind(
                section,
                "启用此分类缩小",
                defaultValue,
                "启用后，该分类物品放入购物车时会自动缩小。");
        }

        private static ConfigEntry<float> BindCategoryFactor(ConfigFile config, string section, float defaultValue)
        {
            return config.Bind(
                section,
                "缩小倍率",
                defaultValue,
                Ranged("该分类物品放入购物车后的目标尺寸比例。0.4 表示原尺寸的 40%。", 0.05f, 1.0f));
        }

        private static ConfigDescription Ranged(string description, float min, float max)
        {
            return new ConfigDescription(description, new AcceptableValueRange<float>(min, max));
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
    }
}
