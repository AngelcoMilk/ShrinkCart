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
        Fallback
    }

    internal static class ModConfig
    {
        internal static ConfigEntry<bool> CartShrinkingEnabled;
        internal static ConfigEntry<float> CartScaleSpeed;
        internal static ConfigEntry<float> RestoreScaleSpeed;
        internal static ConfigEntry<float> RestoreGraceSeconds;
        internal static ConfigEntry<bool> PreserveCartMass;
        internal static ConfigEntry<bool> ShrinkNonValuableItems;
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
        internal static ConfigEntry<float> FallbackScaleFactor;

        internal static ConfigEntry<bool> VehicleCrushInstantKill;
        internal static ConfigEntry<bool> VehicleCrushKillEnemies;
        internal static ConfigEntry<bool> DebugLogging;

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
                2.5f,
                Ranged("ScalerCore 缩小动画速度。数值越大越快。", 0.1f, 20.0f));

            RestoreScaleSpeed = config.Bind(
                "购物车",
                "取出后放大速度",
                0.75f,
                Ranged("正常取出购物车后的 ScalerCore 放大动画速度。数值越小越慢。", 0.1f, 20.0f));

            RestoreGraceSeconds = config.Bind(
                "购物车",
                "取出后恢复延迟",
                0.2f,
                Ranged("物品离开购物车检测后，等待多少秒开始恢复原尺寸。", 0.05f, 10.0f));

            PreserveCartMass = config.Bind(
                "购物车",
                "保持原始重量",
                true,
                "启用后只改变视觉尺寸，不降低物品重量，避免购物车承重过于作弊。");

            ShrinkNonValuableItems = config.Bind(
                "购物车",
                "普通物品也缩小",
                true,
                "启用后，非贵重物品也会使用“普通或未知物品倍率”。");

            SuppressValuableDamageRestore = config.Bind(
                "购物车",
                "防止碰撞弹回原尺寸",
                true,
                "启用后，贵重物品在购物车里轻微碰撞时不会立刻弹回原尺寸。ScalerCore 的安全恢复仍会保留。");

            TinyEnabled = BindCategoryEnabled(config, "Tiny 小型贵重物", true);
            TinyScaleFactor = BindCategoryFactor(config, "Tiny 小型贵重物", 0.55f);
            SmallEnabled = BindCategoryEnabled(config, "Small 小贵重物", true);
            SmallScaleFactor = BindCategoryFactor(config, "Small 小贵重物", 0.45f);
            MediumEnabled = BindCategoryEnabled(config, "Medium 中贵重物", true);
            MediumScaleFactor = BindCategoryFactor(config, "Medium 中贵重物", 0.35f);
            BigEnabled = BindCategoryEnabled(config, "Big 大贵重物", true);
            BigScaleFactor = BindCategoryFactor(config, "Big 大贵重物", 0.3f);
            WideEnabled = BindCategoryEnabled(config, "Wide 宽贵重物", true);
            WideScaleFactor = BindCategoryFactor(config, "Wide 宽贵重物", 0.3f);
            TallEnabled = BindCategoryEnabled(config, "Tall 高贵重物", true);
            TallScaleFactor = BindCategoryFactor(config, "Tall 高贵重物", 0.28f);
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
                0.35f,
                Ranged("Small 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbMediumScaleFactor = config.Bind(
                "敌人球",
                "Medium 敌人球倍率",
                0.3f,
                Ranged("Medium 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbBigScaleFactor = config.Bind(
                "敌人球",
                "Big 敌人球倍率",
                0.25f,
                Ranged("Big 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            EnemyOrbBerserkerScaleFactor = config.Bind(
                "敌人球",
                "Berserker 敌人球倍率",
                0.25f,
                Ranged("Berserker 敌人球放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            SurplusEnabled = config.Bind(
                "特殊物品",
                "启用 Surplus 缩小",
                true,
                "启用后，SurplusValuable 会使用单独倍率。");

            SurplusScaleFactor = config.Bind(
                "特殊物品",
                "Surplus 倍率",
                0.25f,
                Ranged("SurplusValuable 放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            FallbackScaleFactor = config.Bind(
                "普通或未知物品",
                "默认缩小倍率",
                0.4f,
                Ranged("普通物品或无法识别分类的物品放入购物车后的目标尺寸比例。", 0.05f, 1.0f));

            VehicleCrushInstantKill = config.Bind(
                "车辆碾压",
                "车辆碾压秒杀玩家",
                false,
                "启用后，C.A.R.T / 购物车车辆撞击玩家时会按秒杀处理。建议所有玩家都安装本 mod。");

            VehicleCrushKillEnemies = config.Bind(
                "车辆碾压",
                "车辆碾压秒杀敌人",
                false,
                "启用后，C.A.R.T / 购物车车辆撞击敌人时会按秒杀处理。");

            DebugLogging = config.Bind(
                "诊断",
                "启用调试日志",
                false,
                "启用后，在 BepInEx 日志中写入更多缩小、恢复、碾压识别信息。");
        }

        internal static float SafeScaleSpeed()
        {
            return Mathf.Clamp(CartScaleSpeed.Value, 0.1f, 20.0f);
        }

        internal static float SafeRestoreScaleSpeed()
        {
            return Mathf.Clamp(RestoreScaleSpeed.Value, 0.1f, 20.0f);
        }

        internal static float SafeRestoreGraceSeconds()
        {
            return Mathf.Clamp(RestoreGraceSeconds.Value, 0.05f, 10.0f);
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
    }
}
