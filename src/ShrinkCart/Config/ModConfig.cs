using System;
using System.IO;
using BepInEx.Configuration;
using ShrinkCart.Core;
using ShrinkCart.Domain;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 配置的类型化门面：各功能只通过这里的条目与 Safe* 读取方法访问配置（Safe* 会再次夹紧范围）。
    /// 绑定流程见 <see cref="Bind"/>；配置项定义见 <see cref="ConfigSchema"/>；迁移见 <see cref="ConfigMigrator"/>。
    /// </summary>
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> ModEnabled;
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
        internal static ConfigEntry<bool> CartNestingGuardEnabled;
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
        internal static ConfigEntry<int> SchemaVersion;

        /// <summary>任一配置变化时递增，用于让对象分类缓存中的倍率失效。</summary>
        internal static int ScalingConfigVersion;

        private static bool _normalizingFloatEntry;

        internal static bool IsEnglish { get; private set; }

        /// <summary>
        /// 绑定全部配置：判定首次安装 → 读取 schema 版本 → 需要升级时备份 → 按语言绑定 → 旧键继承与废弃键清理
        /// → 一次性迁移 → 写入 schema 版本 → 数值规格化 → 统一保存一次。
        /// </summary>
        internal static void Bind(ConfigFile config)
        {
            // 必须在任何 Bind/Save 之前判断：BepInEx 构造插件 ConfigFile 时不写文件，首次安装此刻 cfg 尚不存在。
            bool configFileExisted = File.Exists(config.ConfigFilePath);
            bool saveOnConfigSet = config.SaveOnConfigSet;
            config.SaveOnConfigSet = false;
            try
            {
                RawConfigAccess raw = new RawConfigAccess(config);
                int storedVersion = ConfigMigrator.TakeStoredSchemaVersion(raw);
                bool upgrade = configFileExisted && storedVersion < ConfigMigrator.CurrentSchemaVersion;
                if (upgrade)
                {
                    ConfigMigrator.BackupConfigFile(config.ConfigFilePath);
                }

                string language = TakeLanguage(raw);
                IsEnglish = language == ConfigSchema.EnglishLanguage;

                LocalizedConfigBinder binder = new LocalizedConfigBinder(config, raw, IsEnglish);
                BindEntries(binder);

                ConfigLanguage = config.Bind(
                    ConfigSchema.LanguageSection,
                    ConfigSchema.LanguageKey,
                    ConfigSchema.ChineseLanguage,
                    new ConfigDescription(
                        "切换 ShrinkCart 配置文件语言。Switch ShrinkCart config file language. Apply and restart/reload to rebuild all config keys.",
                        new AcceptableValueList<string>(ConfigSchema.ChineseLanguage, ConfigSchema.EnglishLanguage)));
                ConfigLanguage.Value = language;

                ConfigMigrator.ApplyRenamesAndCleanup(raw, binder);
                if (upgrade)
                {
                    ConfigMigrator.MigrateToCurrent(storedVersion, Path.GetDirectoryName(config.ConfigFilePath), binder);
                }

                SchemaVersion = config.Bind(
                    ConfigSchema.MetaSection,
                    ConfigSchema.SchemaVersionKey,
                    0,
                    new ConfigDescription(
                        "ShrinkCart 内部配置结构版本，请勿修改。Internal config schema version; do not edit.",
                        null,
                        "HideFromREPOConfig"));
                SchemaVersion.Value = ConfigMigrator.CurrentSchemaVersion;

                foreach (ConfigEntryBase entry in binder.Entries)
                {
                    ConfigEntry<float> floatEntry = entry as ConfigEntry<float>;
                    if (floatEntry != null)
                    {
                        NormalizeFloatEntry(floatEntry);
                        floatEntry.SettingChanged += OnFloatEntrySettingChanged;
                    }
                }

                config.SettingChanged -= OnAnySettingChanged;
                config.SettingChanged += OnAnySettingChanged;
            }
            finally
            {
                config.SaveOnConfigSet = saveOnConfigSet;
            }

            try
            {
                config.Save();
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to save ShrinkCart config: " + ex.Message);
            }
        }

        internal static bool ModEnabledValue()
        {
            return ModEnabled != null && ModEnabled.Value;
        }

        internal static bool DebugLoggingEnabled => DebugLogging != null && DebugLogging.Value;

        internal static bool CartNestingGuardEnabledValue()
        {
            return CartNestingGuardEnabled != null && CartNestingGuardEnabled.Value;
        }

        internal static bool PlayerScalingEnabled()
        {
            return CartShrinkingEnabled != null &&
                   PlayerScalingModuleEnabled != null &&
                   CartShrinkingEnabled.Value &&
                   PlayerScalingModuleEnabled.Value;
        }

        internal static bool DeadHeadScalingEnabledValue()
        {
            return CartShrinkingEnabled != null &&
                   DeadHeadScalingEnabled != null &&
                   CartShrinkingEnabled.Value &&
                   DeadHeadScalingEnabled.Value;
        }

        internal static bool DynamicItemScanEnabledValue()
        {
            return DynamicItemScanEnabled != null && DynamicItemScanEnabled.Value;
        }

        /// <summary>实际缩小速度：配置按 ×10 刻度存储，实际速度 = 配置值 × 0.1（默认 5 → 0.5）。</summary>
        internal static float SafeScaleSpeed()
        {
            return MathUtil.Clamp(CartScaleSpeed.Value, 0.1f, 20.0f) * 0.1f;
        }

        /// <summary>实际恢复速度：配置按 ×10 刻度存储，实际速度 = 配置值 × 0.1（默认 2 → 0.2）。</summary>
        internal static float SafeRestoreScaleSpeed()
        {
            return MathUtil.Clamp(RestoreScaleSpeed.Value, 0.1f, 20.0f) * 0.1f;
        }

        internal static float SafeCartLeaveDebounceSeconds()
        {
            return MathUtil.Clamp(CartLeaveDebounceSeconds.Value, 0.1f, 10.0f);
        }

        internal static float SafeReshrinkCooldownSeconds()
        {
            return MathUtil.Clamp(ReshrinkCooldownSeconds.Value, 0.05f, 10.0f);
        }

        internal static bool ShouldPreserveMass()
        {
            return ScaleMassWithSize == null || !ScaleMassWithSize.Value;
        }

        internal static float SafePlayerCartScaleFactor()
        {
            return MathUtil.Clamp(PlayerCartScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafePlayerCartGrowFactor()
        {
            return MathUtil.Clamp(PlayerCartGrowFactor.Value, 1.0f, 2.0f);
        }

        internal static float SafePlayerCartStandTriggerSeconds()
        {
            return MathUtil.Clamp(PlayerCartStandTriggerSeconds.Value, 0.25f, 10.0f);
        }

        internal static float SafePlayerCartExitGraceSeconds()
        {
            return MathUtil.Clamp(PlayerCartExitGraceSeconds.Value, 0.0f, 2.0f);
        }

        internal static float SafePlayerCartDetectionIntervalSeconds()
        {
            return MathUtil.Clamp(PlayerCartDetectionIntervalSeconds.Value, 0.25f, 2.0f);
        }

        internal static float SafeDeadHeadScaleFactor()
        {
            return MathUtil.Clamp(DeadHeadScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafeMinimumItemScanIntervalSeconds()
        {
            return MathUtil.Clamp(MinimumItemScanIntervalSeconds.Value, 0.05f, 1.0f);
        }

        internal static float SafeMaximumItemScanIntervalSeconds()
        {
            return Math.Max(SafeMinimumItemScanIntervalSeconds(), MathUtil.Clamp(MaximumItemScanIntervalSeconds.Value, 0.1f, 2.0f));
        }

        /// <summary>分类是否启用及其倍率。Fallback（商店用品/未知体型）始终返回 true，是否缩小商店用品由调用方判断。</summary>
        internal static bool TryGetScaleFactor(ShrinkCategory category, out float factor)
        {
            switch (category)
            {
                case ShrinkCategory.Tiny:
                    return Category(TinyEnabled, TinyScaleFactor, out factor);
                case ShrinkCategory.Small:
                    return Category(SmallEnabled, SmallScaleFactor, out factor);
                case ShrinkCategory.Medium:
                    return Category(MediumEnabled, MediumScaleFactor, out factor);
                case ShrinkCategory.Big:
                    return Category(BigEnabled, BigScaleFactor, out factor);
                case ShrinkCategory.Wide:
                    return Category(WideEnabled, WideScaleFactor, out factor);
                case ShrinkCategory.Tall:
                    return Category(TallEnabled, TallScaleFactor, out factor);
                case ShrinkCategory.VeryTall:
                    return Category(VeryTallEnabled, VeryTallScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbSmall:
                    return Category(EnemyOrbEnabled, EnemyOrbSmallScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbMedium:
                    return Category(EnemyOrbEnabled, EnemyOrbMediumScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbBig:
                    return Category(EnemyOrbEnabled, EnemyOrbBigScaleFactor, out factor);
                case ShrinkCategory.EnemyOrbBerserker:
                    return Category(EnemyOrbEnabled, EnemyOrbBerserkerScaleFactor, out factor);
                case ShrinkCategory.Surplus:
                    return Category(SurplusEnabled, SurplusScaleFactor, out factor);
                case ShrinkCategory.ValuableBox:
                    return Category(ValuableBoxEnabled, ValuableBoxScaleFactor, out factor);
                default:
                    factor = SafeFactor(FallbackScaleFactor.Value);
                    return true;
            }
        }

        private static void BindEntries(LocalizedConfigBinder binder)
        {
            ModEnabled = binder.Bind<bool>(ConfigSchema.ModEnabled);
            CartShrinkingEnabled = binder.Bind<bool>(ConfigSchema.CartShrinkingEnabled);
            CartScaleSpeed = binder.Bind<float>(ConfigSchema.CartScaleSpeed);
            RestoreScaleSpeed = binder.Bind<float>(ConfigSchema.RestoreScaleSpeed);
            CartLeaveDebounceSeconds = binder.Bind<float>(ConfigSchema.CartLeaveDebounceSeconds);
            ReshrinkCooldownSeconds = binder.Bind<float>(ConfigSchema.ReshrinkCooldownSeconds);
            ScaleMassWithSize = binder.Bind<bool>(ConfigSchema.ScaleMassWithSize);
            ShrinkShopPlayerItems = binder.Bind<bool>(ConfigSchema.ShrinkShopPlayerItems);
            PlayerScalingModuleEnabled = binder.Bind<bool>(ConfigSchema.PlayerScalingModuleEnabled);
            PlayerCartScaleFactor = binder.Bind<float>(ConfigSchema.PlayerCartScaleFactor);
            PlayerCartGrowFactor = binder.Bind<float>(ConfigSchema.PlayerCartGrowFactor);
            PlayerCartStandTriggerSeconds = binder.Bind<float>(ConfigSchema.PlayerCartStandTriggerSeconds);
            PlayerCartExitGraceSeconds = binder.Bind<float>(ConfigSchema.PlayerCartExitGraceSeconds);
            PlayerCartDetectionIntervalSeconds = binder.Bind<float>(ConfigSchema.PlayerCartDetectionIntervalSeconds);
            RestorePlayerOnDamage = binder.Bind<bool>(ConfigSchema.RestorePlayerOnDamage);
            SuppressValuableDamageRestore = binder.Bind<bool>(ConfigSchema.SuppressValuableDamageRestore);
            CartNestingGuardEnabled = binder.Bind<bool>(ConfigSchema.CartNestingGuardEnabled);
            DeadHeadScalingEnabled = binder.Bind<bool>(ConfigSchema.DeadHeadScalingEnabled);
            DeadHeadScaleFactor = binder.Bind<float>(ConfigSchema.DeadHeadScaleFactor);
            TinyEnabled = binder.Bind<bool>(ConfigSchema.TinyEnabled);
            TinyScaleFactor = binder.Bind<float>(ConfigSchema.TinyScaleFactor);
            SmallEnabled = binder.Bind<bool>(ConfigSchema.SmallEnabled);
            SmallScaleFactor = binder.Bind<float>(ConfigSchema.SmallScaleFactor);
            MediumEnabled = binder.Bind<bool>(ConfigSchema.MediumEnabled);
            MediumScaleFactor = binder.Bind<float>(ConfigSchema.MediumScaleFactor);
            BigEnabled = binder.Bind<bool>(ConfigSchema.BigEnabled);
            BigScaleFactor = binder.Bind<float>(ConfigSchema.BigScaleFactor);
            WideEnabled = binder.Bind<bool>(ConfigSchema.WideEnabled);
            WideScaleFactor = binder.Bind<float>(ConfigSchema.WideScaleFactor);
            TallEnabled = binder.Bind<bool>(ConfigSchema.TallEnabled);
            TallScaleFactor = binder.Bind<float>(ConfigSchema.TallScaleFactor);
            VeryTallEnabled = binder.Bind<bool>(ConfigSchema.VeryTallEnabled);
            VeryTallScaleFactor = binder.Bind<float>(ConfigSchema.VeryTallScaleFactor);
            EnemyOrbEnabled = binder.Bind<bool>(ConfigSchema.EnemyOrbEnabled);
            EnemyOrbSmallScaleFactor = binder.Bind<float>(ConfigSchema.EnemyOrbSmallScaleFactor);
            EnemyOrbMediumScaleFactor = binder.Bind<float>(ConfigSchema.EnemyOrbMediumScaleFactor);
            EnemyOrbBigScaleFactor = binder.Bind<float>(ConfigSchema.EnemyOrbBigScaleFactor);
            EnemyOrbBerserkerScaleFactor = binder.Bind<float>(ConfigSchema.EnemyOrbBerserkerScaleFactor);
            SurplusEnabled = binder.Bind<bool>(ConfigSchema.SurplusEnabled);
            SurplusScaleFactor = binder.Bind<float>(ConfigSchema.SurplusScaleFactor);
            ValuableBoxEnabled = binder.Bind<bool>(ConfigSchema.ValuableBoxEnabled);
            ValuableBoxScaleFactor = binder.Bind<float>(ConfigSchema.ValuableBoxScaleFactor);
            FallbackScaleFactor = binder.Bind<float>(ConfigSchema.FallbackScaleFactor);
            EnemyInCartInstantKill = binder.Bind<bool>(ConfigSchema.EnemyInCartInstantKill);
            DynamicItemScanEnabled = binder.Bind<bool>(ConfigSchema.DynamicItemScanEnabled);
            MinimumItemScanIntervalSeconds = binder.Bind<float>(ConfigSchema.MinimumItemScanIntervalSeconds);
            MaximumItemScanIntervalSeconds = binder.Bind<float>(ConfigSchema.MaximumItemScanIntervalSeconds);
            DebugLogging = binder.Bind<bool>(ConfigSchema.DebugLogging);
        }

        private static string TakeLanguage(RawConfigAccess raw)
        {
            if (!raw.TryTake(ConfigSchema.LanguageSection, ConfigSchema.LanguageKey, out string value))
            {
                return ConfigSchema.ChineseLanguage;
            }

            return string.Equals(value, ConfigSchema.EnglishLanguage, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "EN", StringComparison.OrdinalIgnoreCase)
                ? ConfigSchema.EnglishLanguage
                : ConfigSchema.ChineseLanguage;
        }

        private static bool Category(ConfigEntry<bool> enabled, ConfigEntry<float> factorEntry, out float factor)
        {
            factor = SafeFactor(factorEntry.Value);
            return enabled.Value;
        }

        private static float SafeFactor(float value)
        {
            return MathUtil.Clamp(value, 0.05f, 1.0f);
        }

        // 数值统一保留两位小数：REPOConfig/MenuLib 滑条的浮点误差（如 0.3999999）原样落盘会让其精度探测越来越脏。
        // 使用精确比较：Approximately 会放过这类小误差。
        private static void NormalizeFloatEntry(ConfigEntry<float> entry)
        {
            float rounded = MathUtil.RoundTo2Decimals(entry.Value);
            if (entry.Value != rounded)
            {
                entry.Value = rounded;
            }
        }

        private static void OnFloatEntrySettingChanged(object sender, EventArgs args)
        {
            ConfigEntry<float> entry = sender as ConfigEntry<float>;
            if (entry == null || _normalizingFloatEntry)
            {
                return;
            }

            _normalizingFloatEntry = true;
            try
            {
                NormalizeFloatEntry(entry);
            }
            finally
            {
                _normalizingFloatEntry = false;
            }
        }

        private static void OnAnySettingChanged(object sender, SettingChangedEventArgs args)
        {
            ScalingConfigVersion++;
        }
    }
}
