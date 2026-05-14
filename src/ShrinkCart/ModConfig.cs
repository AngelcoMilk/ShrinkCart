using System;
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
        internal static ConfigEntry<bool> PreserveCartMass;
        internal static ConfigEntry<bool> ShrinkShopPlayerItems;
        internal static ConfigEntry<bool> ShrinkPlayers;
        internal static ConfigEntry<float> PlayerCartScaleFactor;
        internal static ConfigEntry<float> PlayerCartStandTriggerSeconds;
        internal static ConfigEntry<bool> PlayerAutoRestoreBeforeDeath;
        internal static ConfigEntry<bool> SuppressValuableDamageRestore;
        internal static ConfigEntry<bool> HideScaleFlash;

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

        internal static ConfigEntry<bool> VehicleCrushInstantKill;
        internal static ConfigEntry<bool> EnemyInCartInstantKill;
        internal static ConfigEntry<bool> DebugLogging;

        internal static int ScalingConfigVersion;

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

            PreserveCartMass = config.Bind(
                "购物车",
                "保持原始重量",
                true,
                "启用后只改变视觉尺寸，不降低物品重量，避免购物车承重过于取巧。");

            ShrinkShopPlayerItems = config.Bind(
                "购物车",
                "商店用品也缩小",
                false,
                "启用后，枪、血包、近战、手雷、工具、无人机、宝珠、升级、追踪器、地雷等商店购买类实用品会使用默认缩小倍率。大小推车、C.A.R.T. Cannon 和 C.A.R.T. Laser 始终不会缩小。");

            ShrinkPlayers = config.Bind(
                "购物车",
                "玩家也缩小",
                true,
                "启用后，玩家站在正式购物车中心区域一段时间会切换缩小/恢复状态。此开关不影响枪、血包、工具等商店用品。");

            PlayerCartScaleFactor = config.Bind(
                "购物车",
                "玩家进车缩放倍率",
                0.55f,
                Ranged("开启“玩家也缩小”后，玩家站在购物车中心区域触发缩放时的目标尺寸比例。", 0.05f, 1.0f));

            PlayerCartStandTriggerSeconds = config.Bind(
                "购物车",
                "玩家站车触发时间",
                2.0f,
                Ranged("开启“玩家也缩小”后，玩家站在购物车中心区域多久才切换缩小/恢复。离开中心区域会重置计时。", 0.25f, 10.0f));

            PlayerAutoRestoreBeforeDeath = config.Bind(
                "购物车",
                "玩家死亡前自动恢复",
                true,
                "启用后，由 ShrinkCart 缩小的玩家在死亡或复活流程前会先恢复原尺寸，减少死亡头颅和复活 mod 冲突。");

            SuppressValuableDamageRestore = config.Bind(
                "购物车",
                "防止碰撞弹回原尺寸",
                true,
                "启用后，贵重物品在购物车里轻微碰撞时不会立刻弹回原尺寸。ScalerCore 的安全恢复仍会保留。");

            HideScaleFlash = config.Bind(
                "视觉",
                "隐藏缩放闪光",
                true,
                "启用后，隐藏 ShrinkCart 购物车缩小和恢复时由 ScalerCore 触发的冲击闪光。不会关闭普通碰撞特效。");

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
                Ranged("开启“商店用品也缩小”后，枪、血包、工具等实用品放入购物车后的目标尺寸比例。也作为未知贵重物分类的兜底倍率。", 0.05f, 1.0f));

            VehicleCrushInstantKill = config.Bind(
                "车辆碾压",
                "车辆碾压秒杀玩家",
                false,
                "启用后，C.A.R.T / 购物车车辆撞击玩家时会按秒杀处理。建议所有玩家都安装本 mod。");

            EnemyInCartInstantKill = config.Bind(
                "车辆碾压",
                "敌人进车秒杀",
                LegacyBool(config, true, "车辆碾压", "车辆碾压秒杀敌人", "VehicleCrush", "InstantKillEnemies"),
                "启用后，敌人或敌人刚体进入购物车时会立刻死亡。此功能复刻 ShrinkerCartPlus 的敌人进车秒杀逻辑。");

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
            WatchScaling(PreserveCartMass);
            WatchScaling(ShrinkShopPlayerItems);
            WatchScaling(ShrinkPlayers);
            WatchScaling(PlayerCartScaleFactor);
            WatchScaling(PlayerCartStandTriggerSeconds);
            WatchScaling(PlayerAutoRestoreBeforeDeath);
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

        internal static float SafePlayerCartScaleFactor()
        {
            return Mathf.Clamp(PlayerCartScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafePlayerCartStandTriggerSeconds()
        {
            return Mathf.Clamp(PlayerCartStandTriggerSeconds.Value, 0.25f, 10.0f);
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
