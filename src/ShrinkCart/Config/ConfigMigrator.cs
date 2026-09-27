using System;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx.Configuration;
using ShrinkCart.Core;
using ShrinkCart.Domain;

namespace ShrinkCart.Config
{
    /// <summary>
    /// 配置迁移。schema 版本存于 cfg 的隐藏条目 [ShrinkCart.Meta] ConfigSchemaVersion（对 REPOConfig 隐藏），
    /// 不再使用 cfg 之外的 marker 文件。
    /// <list type="bullet">
    /// <item>每次启动（幂等）：旧键继承（仅当新键在 cfg 中没有值）、删除废弃键。</item>
    /// <item>schema 升级时一次性执行：见 <see cref="MigrateToCurrent"/>。</item>
    /// </list>
    /// 新增迁移：提升 <see cref="CurrentSchemaVersion"/>，在 MigrateToCurrent 中按 storedVersion 追加步骤，
    /// 并在 tests/fixtures/config 增加对应历史样本与用例。
    /// </summary>
    internal static class ConfigMigrator
    {
        internal const int CurrentSchemaVersion = 1;
        internal const string BackupSuffix = ".pre-0.3.1.bak";
        internal const string LegacyMassMarkerName = "ShrinkCart.v0.2.39.mass-default-enabled";

        /// <summary>早期版本的键 → 当前键。列出的旧键会在继承后删除。</summary>
        internal static readonly LegacyRename[] LegacyRenames =
        {
            new LegacyRename(ConfigSchema.ReshrinkCooldownSeconds, "购物车", "取出后恢复延迟", "Cart", "RestoreGraceSeconds"),
            new LegacyRename(ConfigSchema.SurplusEnabled, "特殊物品", "启用 Surplus 缩小"),
            new LegacyRename(ConfigSchema.SurplusScaleFactor, "特殊物品", "Surplus 倍率"),
            new LegacyRename(ConfigSchema.FallbackScaleFactor, "普通或未知物品", "默认缩小倍率"),
            new LegacyRename(ConfigSchema.EnemyInCartInstantKill, "车辆碾压", "车辆碾压秒杀敌人", "VehicleCrush", "InstantKillEnemies"),

            // v0.2.0–v0.2.1 的分组名，v0.2.2 改为「Tiny 微型贵重物」时没有迁移，旧分组一直残留在老玩家的 cfg 里。
            new LegacyRename(ConfigSchema.TinyEnabled, "Tiny 小型贵重物", "启用此分类缩小"),
            new LegacyRename(ConfigSchema.TinyScaleFactor, "Tiny 小型贵重物", "缩小倍率")
        };

        /// <summary>
        /// 历史版本（v0.2.0–v0.2.52）用过、之后被改掉的默认值，按各版本源码逐一核对整理。
        /// BepInEx 不会让已写入 cfg 的值跟随默认值变化，所以早期安装、从没改过的玩家一直停在这些旧默认值上。
        /// schema 0→1 时，值（两位小数）仍等于其中之一的配置项升级为当前默认值；玩家改过的值不动。
        /// 只收录数值项：开关类（如早期默认关闭的玩家缩放）保持玩家现有值；缩小/放大速度另行无条件统一。
        /// </summary>
        internal static readonly LegacyDefault[] LegacyDefaults =
        {
            new LegacyDefault(ConfigSchema.CartLeaveDebounceSeconds, 2.5f),
            new LegacyDefault(ConfigSchema.ReshrinkCooldownSeconds, 0.2f),
            new LegacyDefault(ConfigSchema.TinyScaleFactor, 0.55f),
            new LegacyDefault(ConfigSchema.SmallScaleFactor, 0.45f),
            new LegacyDefault(ConfigSchema.MediumScaleFactor, 0.35f, 0.33f, 0.4f),
            new LegacyDefault(ConfigSchema.BigScaleFactor, 0.3f, 0.2f, 0.25f),
            new LegacyDefault(ConfigSchema.WideScaleFactor, 0.3f, 0.2f, 0.25f),
            new LegacyDefault(ConfigSchema.TallScaleFactor, 0.28f, 0.3f, 0.25f),
            new LegacyDefault(ConfigSchema.VeryTallScaleFactor, 0.2f),
            new LegacyDefault(ConfigSchema.EnemyOrbSmallScaleFactor, 0.35f, 0.6f),
            new LegacyDefault(ConfigSchema.EnemyOrbMediumScaleFactor, 0.3f, 0.55f),
            new LegacyDefault(ConfigSchema.EnemyOrbBigScaleFactor, 0.25f, 0.2f),
            new LegacyDefault(ConfigSchema.EnemyOrbBerserkerScaleFactor, 0.25f, 0.2f),
            new LegacyDefault(ConfigSchema.FallbackScaleFactor, 0.4f)
        };

        /// <summary>已废弃、直接删除的键（section, key 成对排列）。</summary>
        internal static readonly string[] DeprecatedKeys =
        {
            "ShrinkCart.Migration", "SpeedScaleV2Migrated",
            "Cart", "Enabled",
            "Cart", "ScaleFactor",
            "Cart", "ScaleSpeed",
            "Cart", "PreserveMass",
            "Cart", "ShrinkNonValuableItems",
            "Cart", "SuppressValuableDamageRestore",
            "Diagnostics", "DebugLogging",
            "购物车", "保持原始重量",
            "VehicleCrush", "InstantKillPlayers",
            "视觉", "隐藏缩放闪光",
            "车辆碾压", "车辆碾压秒杀玩家",
            "提取点复活兼容", "启用提取点复活",
            "提取点复活兼容", "复活前稳定检测时间",
            "提取点复活兼容", "复活检测间隔",
            "提取点复活兼容", "拦截外部立即复活调用",
            "购物车", "商店用品也缩小",
            "购物车", "玩家也缩小",
            "购物车", "启用实验性玩家缩放",
            "购物车", "玩家死亡前自动恢复",
            "购物车", "启用玩家缩放",
            "购物车", "玩家进车缩放倍率",
            "购物车", "玩家站车触发时间",
            "购物车", "玩家离车判定宽容时间",
            "购物车", "玩家进车切换间隔",
            "购物车", "普通物品也缩小",
            "购物车", "商店/人物用品也缩小",
            "购物车", "防止车辆互相重叠",
            "购物车", "车辆硬碰撞修正强度",
            "购物车", "车辆最大单帧修正距离",
            "购物车", "车辆挤压速度清除",
            "购物车", "车辆临时忽略碰撞时间（已废弃）",
            "购物车", "车辆临时忽略碰撞时间",
            "购物车", "车辆脱困强度",
            "玩家缩放", "旧版玩家缩放开关（已停用）",
            "地图兼容", "Minecraft Stronghold 普通门进车破碎",
            "地图兼容", "Minecraft Stronghold 门破碎确认时间",
            "Map Compatibility", "Minecraft Stronghold door shatter in cart",
            "Map Compatibility", "Minecraft Stronghold door shatter confirmation time"
        };

        /// <summary>读出并移除 cfg 中存储的 schema 版本（没有则为 0，即 0.2.52 及更早的配置）。</summary>
        internal static int TakeStoredSchemaVersion(RawConfigAccess raw)
        {
            if (raw.TryTake(ConfigSchema.MetaSection, ConfigSchema.SchemaVersionKey, out string text) &&
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int version))
            {
                return version;
            }

            return 0;
        }

        internal static void BackupConfigFile(string configPath)
        {
            try
            {
                string backupPath = configPath + BackupSuffix;
                if (!File.Exists(backupPath))
                {
                    File.Copy(configPath, backupPath);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to back up config before migration: " + ex.Message);
            }
        }

        /// <summary>每次启动执行（幂等）：旧键继承与废弃键清理。</summary>
        internal static void ApplyRenamesAndCleanup(RawConfigAccess raw, LocalizedConfigBinder binder)
        {
            for (int i = 0; i < LegacyRenames.Length; i++)
            {
                LegacyRename rename = LegacyRenames[i];
                string inherited = null;
                for (int k = 0; k + 1 < rename.SectionKeyPairs.Length; k += 2)
                {
                    if (raw.TryTake(rename.SectionKeyPairs[k], rename.SectionKeyPairs[k + 1], out string value) && inherited == null)
                    {
                        inherited = value;
                    }
                }

                if (inherited == null || binder.HadStoredValue(rename.Target))
                {
                    continue;
                }

                binder.Entry(rename.Target).SetSerializedValue(inherited);
                ModLog.Info("Inherited legacy config value for " + rename.Target.Id + ".");
            }

            for (int i = 0; i + 1 < DeprecatedKeys.Length; i += 2)
            {
                raw.Delete(DeprecatedKeys[i], DeprecatedKeys[i + 1]);
            }
        }

        /// <summary>
        /// 已存在的配置从 <paramref name="storedVersion"/> 升级到 <see cref="CurrentSchemaVersion"/> 时一次性执行。
        /// 版本 1：
        /// <list type="bullet">
        /// <item>0.2.51/0.2.52 的 ×10 刻度迁移从未生效，按维护决定把缩小/放大速度无条件统一为默认值
        /// （×10 存储刻度不变，5 / 2 即旧版默认实际速度 0.5 / 0.2）。</item>
        /// <item>仍停在历史默认值（<see cref="LegacyDefaults"/>）的数值项升级为当前默认值。</item>
        /// <item>删除遗留的质量 marker 文件（不再覆盖玩家设置）。</item>
        /// </list>
        /// </summary>
        internal static void MigrateToCurrent(int storedVersion, string configDirectory, LocalizedConfigBinder binder)
        {
            if (storedVersion < 1)
            {
                ModConfig.CartScaleSpeed.Value = (float)ConfigSchema.CartScaleSpeed.DefaultValue;
                ModConfig.RestoreScaleSpeed.Value = (float)ConfigSchema.RestoreScaleSpeed.DefaultValue;
                string upgraded = UpgradeLegacyDefaults(binder);
                DeleteLegacyMassMarker(configDirectory);
                ModLog.Info(
                    "Config migrated to schema " + CurrentSchemaVersion +
                    ": shrink/restore speed reset once to defaults " +
                    ModConfig.CartScaleSpeed.Value.ToString("0.##", CultureInfo.InvariantCulture) + " / " +
                    ModConfig.RestoreScaleSpeed.Value.ToString("0.##", CultureInfo.InvariantCulture) +
                    " (actual speed = value x 0.1)" +
                    (upgraded.Length > 0 ? "; settings still at old defaults upgraded: " + upgraded : "; no settings at old defaults") +
                    ". Previous file kept as *" + BackupSuffix + ".");
            }
        }

        /// <returns>被升级的配置项说明（逗号分隔），没有则为空字符串。</returns>
        private static string UpgradeLegacyDefaults(LocalizedConfigBinder binder)
        {
            StringBuilder upgraded = new StringBuilder();
            for (int i = 0; i < LegacyDefaults.Length; i++)
            {
                LegacyDefault legacy = LegacyDefaults[i];
                ConfigEntry<float> entry = binder.Entry(legacy.Target) as ConfigEntry<float>;
                if (entry == null)
                {
                    continue;
                }

                float stored = MathUtil.RoundTo2Decimals(entry.Value);
                if (!legacy.IsOldDefault(stored))
                {
                    continue;
                }

                float target = (float)legacy.Target.DefaultValue;
                entry.Value = target;
                if (upgraded.Length > 0)
                {
                    upgraded.Append(", ");
                }

                upgraded.Append(legacy.Target.Id).Append(' ')
                    .Append(stored.ToString("0.##", CultureInfo.InvariantCulture)).Append("->")
                    .Append(target.ToString("0.##", CultureInfo.InvariantCulture));
            }

            return upgraded.ToString();
        }

        private static void DeleteLegacyMassMarker(string configDirectory)
        {
            if (string.IsNullOrEmpty(configDirectory))
            {
                return;
            }

            try
            {
                string markerPath = Path.Combine(configDirectory, LegacyMassMarkerName);
                if (File.Exists(markerPath))
                {
                    File.Delete(markerPath);
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn("Failed to delete legacy mass migration marker: " + ex.Message);
            }
        }

        internal sealed class LegacyDefault
        {
            internal LegacyDefault(ConfigKeyDefinition target, params float[] oldDefaults)
            {
                Target = target;
                OldDefaults = oldDefaults;
            }

            internal ConfigKeyDefinition Target { get; }

            /// <summary>历史上用过的默认值（不含当前默认值）。</summary>
            internal float[] OldDefaults { get; }

            /// <param name="stored">已按两位小数规整的存储值。</param>
            internal bool IsOldDefault(float stored)
            {
                for (int i = 0; i < OldDefaults.Length; i++)
                {
                    if (MathUtil.RoundTo2Decimals(OldDefaults[i]) == stored)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        internal sealed class LegacyRename
        {
            internal LegacyRename(ConfigKeyDefinition target, params string[] sectionKeyPairs)
            {
                Target = target;
                SectionKeyPairs = sectionKeyPairs;
            }

            internal ConfigKeyDefinition Target { get; }

            /// <summary>旧键（section, key 成对排列），按顺序取第一个存在的值。</summary>
            internal string[] SectionKeyPairs { get; }
        }
    }
}
