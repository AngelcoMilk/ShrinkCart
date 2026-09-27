using System.IO;
using BepInEx.Configuration;
using NUnit.Framework;
using ShrinkCart.Config;

namespace ShrinkCart.Tests.Config
{
    /// <summary>
    /// 配置迁移回归测试（历史版本样本见 tests/fixtures/config）。
    /// 维护决定：已存在的配置在升级到 schema 1 时把缩小/放大速度一次性统一为默认 5 / 2（×10 刻度不变），之后不再改动。
    /// </summary>
    public sealed class ConfigMigrationTests
    {
        [Test]
        public void F1_FreshInstall_UsesDefaultsWithoutMigration()
        {
            using (ConfigScenario scenario = ConfigScenario.Fresh())
            {
                scenario.Bind();

                Assert.That(File.Exists(scenario.ConfigPath), Is.True);
                AssertSpeeds(5f, 2f);
                Assert.That(ModConfig.SchemaVersion.Value, Is.EqualTo(ConfigMigrator.CurrentSchemaVersion));
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.True);
                Assert.That(File.Exists(scenario.BackupPath), Is.False);
                Assert.That(scenario.ResetLogged, Is.False);
                Assert.That(scenario.HasLine("ConfigSchemaVersion = 1"), Is.True);
            }
        }

        [Test]
        public void F2_Version0250_ResetsSpeedsAndKeepsOtherValues()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f2-v0.2.50-zh.cfg"))
            {
                scenario.Bind();

                AssertSpeeds(5f, 2f);
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.False, "mass setting preserved");
                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.32f).Within(1e-6f), "unrelated custom value preserved");
                Assert.That(File.Exists(scenario.BackupPath), Is.True);
                Assert.That(scenario.ResetLogged, Is.True);
                Assert.That(scenario.HasLine("放入时缩小速度 = 5"), Is.True);
                Assert.That(scenario.HasLine("取出后放大速度 = 2"), Is.True);
            }
        }

        [Test]
        public void F3_Version0252Broken_ResetsSpeedsAndRemovesOldSentinel()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f3-v0.2.52-broken-zh.cfg"))
            {
                scenario.Bind();

                AssertSpeeds(5f, 2f);
                Assert.That(scenario.Saved, Does.Not.Contain("SpeedScaleV2Migrated"));
                Assert.That(scenario.Saved, Does.Not.Contain("[ShrinkCart.Migration]"));
            }
        }

        [Test]
        public void F4_Version0252Adjusted_AlsoUnifiedToDefaults()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f4-v0.2.52-adjusted-zh.cfg"))
            {
                scenario.Bind();

                AssertSpeeds(5f, 2f);
            }
        }

        [Test]
        public void F5_EnglishKeys_ResetAndStayEnglish()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f5-v0.2.52-broken-en.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.IsEnglish, Is.True);
                AssertSpeeds(5f, 2f);
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.False);
                Assert.That(scenario.HasLine("Shrink speed = 5"), Is.True);
                Assert.That(scenario.HasLine("Restore speed = 2"), Is.True);
                Assert.That(scenario.Saved, Does.Not.Contain("放入时缩小速度"));
            }
        }

        [Test]
        public void F6_LegacyKeysOnly_AreInheritedAndRemoved()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f6-legacy-keys-only.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.ReshrinkCooldownSeconds.Value, Is.EqualTo(3f).Within(1e-6f));
                Assert.That(ModConfig.FallbackScaleFactor.Value, Is.EqualTo(0.7f).Within(1e-6f));
                Assert.That(ModConfig.EnemyInCartInstantKill.Value, Is.False);
                Assert.That(ModConfig.SurplusEnabled.Value, Is.False);
                Assert.That(ModConfig.SurplusScaleFactor.Value, Is.EqualTo(0.3f).Within(1e-6f));
                Assert.That(scenario.HasLine("取出后恢复延迟 ="), Is.False);
                Assert.That(scenario.Saved, Does.Not.Contain("[普通或未知物品]"));
                Assert.That(scenario.HasLine("车辆碾压秒杀敌人 ="), Is.False);
                Assert.That(scenario.HasLine("启用 Surplus 缩小 ="), Is.False);
                Assert.That(scenario.HasLine("Surplus 倍率 ="), Is.False);
            }
        }

        [Test]
        public void F7_NewKeysWinOverLegacyKeys()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f7-legacy-and-new-keys.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.ReshrinkCooldownSeconds.Value, Is.EqualTo(1.5f).Within(1e-6f));
                Assert.That(ModConfig.EnemyInCartInstantKill.Value, Is.True);
                Assert.That(scenario.HasLine("取出后恢复延迟 ="), Is.False);
                Assert.That(scenario.HasLine("车辆碾压秒杀敌人 ="), Is.False);
            }
        }

        [Test]
        public void F8_SecondRun_KeepsUserEditsAndBackup()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f3-v0.2.52-broken-zh.cfg"))
            {
                ConfigFile first = scenario.Bind();
                string backupBefore = File.ReadAllText(scenario.BackupPath);
                ModConfig.CartScaleSpeed.Value = 7f;
                ModConfig.RestoreScaleSpeed.Value = 3f;
                first.Save();

                scenario.Bind();

                AssertSpeeds(7f, 3f);
                Assert.That(scenario.ResetLogged, Is.False);
                Assert.That(File.ReadAllText(scenario.BackupPath), Is.EqualTo(backupBefore));
            }
        }

        [Test]
        public void F9_LegacyMassMarker_IsDeletedWithoutOverridingUser()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f2-v0.2.50-zh.cfg"))
            {
                string marker = Path.Combine(scenario.Directory, ConfigMigrator.LegacyMassMarkerName);
                File.WriteAllText(marker, "0.2.39");

                scenario.Bind();

                Assert.That(File.Exists(marker), Is.False);
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.False);
            }
        }

        [Test]
        public void F10_AlreadyMigratedBy0253_IsNotResetAgain()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f10-v0.2.53-migrated-zh.cfg"))
            {
                scenario.Bind();

                AssertSpeeds(7f, 3f);
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.False);
                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.3f).Within(1e-6f), "old-default upgrade is part of the one-time migration only");
                Assert.That(ModConfig.CartLeaveDebounceSeconds.Value, Is.EqualTo(2.5f).Within(1e-6f), "old-default upgrade is part of the one-time migration only");
                Assert.That(File.Exists(scenario.BackupPath), Is.False);
                Assert.That(scenario.ResetLogged, Is.False);
            }
        }

        [Test]
        public void F11_SwitchToEnglish_MovesValuesToEnglishKeys()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f11-switch-to-english.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.IsEnglish, Is.True);
                Assert.That(ModConfig.CartScaleSpeed.Value, Is.EqualTo(7f).Within(1e-6f));
                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.3f).Within(1e-6f));
                Assert.That(ModConfig.ScaleMassWithSize.Value, Is.False);
                Assert.That(scenario.HasLine("Shrink speed = 7"), Is.True);
                Assert.That(scenario.Saved, Does.Contain("[Big valuables]"));
                Assert.That(scenario.Saved, Does.Not.Contain("放入时缩小速度"));
                Assert.That(scenario.Saved, Does.Not.Contain("[Big 大贵重物]"));
            }
        }

        [Test]
        public void F12_DirtyFloats_AreNormalizedToTwoDecimals()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f12-dirty-floats.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.4f));
                Assert.That(ModConfig.CartLeaveDebounceSeconds.Value, Is.EqualTo(0.75f));
                Assert.That(scenario.HasLine("缩小倍率 = 0.4"), Is.True);
            }
        }

        [Test]
        public void F13_Version021TinySection_IsInheritedAndRemoved()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f13-v0.2.1-tiny-section-zh.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.TinyEnabled.Value, Is.False, "inherited from [Tiny 小型贵重物]");
                Assert.That(ModConfig.TinyScaleFactor.Value, Is.EqualTo(0.62f).Within(1e-6f), "customized value inherited from [Tiny 小型贵重物]");
                Assert.That(scenario.Saved, Does.Not.Contain("[Tiny 小型贵重物]"));
                Assert.That(scenario.Saved, Does.Contain("[Tiny 微型贵重物]"));
                Assert.That(scenario.HasLine("缩小倍率 = 0.62"), Is.True);
            }
        }

        [Test]
        public void F15_Version025_ValuesStillAtOldDefaults_AreUpgradedOnce()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f15-v0.2.5-old-defaults-zh.cfg"))
            {
                ConfigFile first = scenario.Bind();

                AssertSpeeds(5f, 2f);
                Assert.That(ModConfig.CartLeaveDebounceSeconds.Value, Is.EqualTo(0.5f), "v0.2.2–v0.2.8 default 2.5 upgraded");
                Assert.That(ModConfig.ReshrinkCooldownSeconds.Value, Is.EqualTo(0.5f), "already current default");
                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.4f), "old default 0.2 upgraded");
                Assert.That(ModConfig.TallScaleFactor.Value, Is.EqualTo(0.35f), "old default 0.3 (with slider noise) upgraded");
                Assert.That(ModConfig.EnemyOrbSmallScaleFactor.Value, Is.EqualTo(0.8f), "old default 0.35 upgraded");
                Assert.That(ModConfig.FallbackScaleFactor.Value, Is.EqualTo(0.5f), "legacy 默认缩小倍率 0.4 inherited, then upgraded");
                Assert.That(ModConfig.MediumScaleFactor.Value, Is.EqualTo(0.5f).Within(1e-6f), "customized value kept");
                Assert.That(ModConfig.EnemyOrbMediumScaleFactor.Value, Is.EqualTo(0.7f).Within(1e-6f), "customized value kept");
                Assert.That(ModConfig.PlayerScalingModuleEnabled.Value, Is.False, "toggles are never upgraded");
                Assert.That(scenario.HasLine("离车防抖延迟 = 0.5"), Is.True);
                Assert.That(File.Exists(scenario.BackupPath), Is.True);

                // 再启动一次：已是 schema 1，玩家之后改回旧值也不会再被升级。
                ModConfig.BigScaleFactor.Value = 0.2f;
                first.Save();
                scenario.Bind();
                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.2f).Within(1e-6f));
                Assert.That(scenario.ResetLogged, Is.False);
            }
        }

        [Test]
        public void F14_OldAndNewTinySections_NewWinsAndOldIsRemoved()
        {
            // 真实老玩家配置的形态：v0.2.2 改名后旧分组一直残留，新分组早已有值（实际生效的就是新值）。
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f14-tiny-old-and-new-sections-zh.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.TinyEnabled.Value, Is.True);
                Assert.That(ModConfig.TinyScaleFactor.Value, Is.EqualTo(0.8f), "existing value kept (normalized), not overridden by the stale section");
                Assert.That(scenario.Saved, Does.Not.Contain("[Tiny 小型贵重物]"));
                Assert.That(scenario.HasLine("缩小倍率 = 0.55"), Is.False);
                AssertSpeeds(5f, 2f);
            }
        }

        [Test]
        public void SliderNoise_IsNormalizedOnWrite()
        {
            using (ConfigScenario scenario = ConfigScenario.Fresh())
            {
                scenario.Bind();

                ModConfig.BigScaleFactor.Value = 0.3999999f;

                Assert.That(ModConfig.BigScaleFactor.Value, Is.EqualTo(0.4f));
            }
        }

        [Test]
        public void NewKeysIn031_HaveBehaviorPreservingDefaults()
        {
            using (ConfigScenario scenario = ConfigScenario.FromFixture("f2-v0.2.50-zh.cfg"))
            {
                scenario.Bind();

                Assert.That(ModConfig.ModEnabled.Value, Is.True);
                Assert.That(ModConfig.CartNestingGuardEnabled.Value, Is.True);
                Assert.That(scenario.HasLine("启用 ShrinkCart = true"), Is.True);
                Assert.That(scenario.HasLine("阻止车类物品计入车内 = true"), Is.True);
            }
        }

        [Test]
        public void AnySettingChange_InvalidatesClassificationCache()
        {
            using (ConfigScenario scenario = ConfigScenario.Fresh())
            {
                scenario.Bind();
                int before = ModConfig.ScalingConfigVersion;

                ModConfig.TinyEnabled.Value = !ModConfig.TinyEnabled.Value;

                Assert.That(ModConfig.ScalingConfigVersion, Is.GreaterThan(before));
            }
        }

        private static void AssertSpeeds(float shrink, float restore)
        {
            Assert.That(ModConfig.CartScaleSpeed.Value, Is.EqualTo(shrink).Within(1e-6f), "shrink speed");
            Assert.That(ModConfig.RestoreScaleSpeed.Value, Is.EqualTo(restore).Within(1e-6f), "restore speed");
            Assert.That(ModConfig.SafeScaleSpeed(), Is.EqualTo(shrink * 0.1f).Within(1e-6f), "actual shrink speed = value x 0.1");
        }
    }
}
