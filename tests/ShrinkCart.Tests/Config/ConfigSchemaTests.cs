using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using NUnit.Framework;
using ShrinkCart.Config;

namespace ShrinkCart.Tests.Config
{
    public sealed class ConfigSchemaTests
    {
        [Test]
        public void IdsAreUnique()
        {
            List<string> ids = ConfigSchema.All.Select(d => d.Id).ToList();
            Assert.That(ids, Is.Unique);
        }

        [Test]
        public void KeysAreUniquePerLanguage()
        {
            Assert.That(ConfigSchema.All.Select(d => d.ChineseSection + "|" + d.ChineseKey).ToList(), Is.Unique);
            Assert.That(ConfigSchema.All.Select(d => d.EnglishSection + "|" + d.EnglishKey).ToList(), Is.Unique);
        }

        [Test]
        public void DeprecatedAndLegacyKeysNeverCollideWithCurrentKeys()
        {
            HashSet<string> current = new HashSet<string>();
            foreach (ConfigKeyDefinition definition in ConfigSchema.All)
            {
                current.Add(definition.ChineseSection + "|" + definition.ChineseKey);
                current.Add(definition.EnglishSection + "|" + definition.EnglishKey);
            }

            for (int i = 0; i + 1 < ConfigMigrator.DeprecatedKeys.Length; i += 2)
            {
                string key = ConfigMigrator.DeprecatedKeys[i] + "|" + ConfigMigrator.DeprecatedKeys[i + 1];
                Assert.That(current, Does.Not.Contain(key), "deprecated key collides: " + key);
            }

            foreach (ConfigMigrator.LegacyRename rename in ConfigMigrator.LegacyRenames)
            {
                for (int i = 0; i + 1 < rename.SectionKeyPairs.Length; i += 2)
                {
                    string key = rename.SectionKeyPairs[i] + "|" + rename.SectionKeyPairs[i + 1];
                    Assert.That(current, Does.Not.Contain(key), "legacy key collides: " + key);
                }
            }
        }

        [Test]
        public void FloatDefaultsAreWithinRange()
        {
            foreach (ConfigKeyDefinition definition in ConfigSchema.All.Where(d => d.ValueType == typeof(float)))
            {
                float value = (float)definition.DefaultValue;
                Assert.That(definition.Minimum.HasValue && definition.Maximum.HasValue, Is.True, definition.Id + " has a range");
                Assert.That(value, Is.InRange(definition.Minimum.Value, definition.Maximum.Value), definition.Id);
            }
        }

        [Test]
        public void LegacyDefaults_AreDistinctFloatSettingsWithValidOldValues()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (ConfigMigrator.LegacyDefault legacy in ConfigMigrator.LegacyDefaults)
            {
                ConfigKeyDefinition target = legacy.Target;
                Assert.That(target.ValueType, Is.EqualTo(typeof(float)), "numeric settings only (toggles are never upgraded): " + target.Id);
                Assert.That(seen.Add(target.Id), Is.True, "listed once: " + target.Id);
                Assert.That(legacy.OldDefaults, Is.Not.Empty, target.Id);
                foreach (float old in legacy.OldDefaults)
                {
                    Assert.That(
                        ShrinkCart.Domain.MathUtil.RoundTo2Decimals(old),
                        Is.Not.EqualTo(ShrinkCart.Domain.MathUtil.RoundTo2Decimals((float)target.DefaultValue)),
                        "an old default must differ from the current default: " + target.Id);
                    Assert.That(old, Is.InRange(target.Minimum.Value, target.Maximum.Value), "old default within range: " + target.Id);
                }
            }

            Assert.That(seen, Does.Not.Contain(ConfigSchema.CartScaleSpeed.Id), "speeds are reset unconditionally");
            Assert.That(seen, Does.Not.Contain(ConfigSchema.RestoreScaleSpeed.Id), "speeds are reset unconditionally");
        }

        [Test]
        public void EverySchemaEntryIsBoundInBothLanguages()
        {
            foreach (bool english in new[] { false, true })
            {
                using (ConfigScenario scenario = ConfigScenario.Fresh())
                {
                    if (english)
                    {
                        System.IO.File.WriteAllText(scenario.ConfigPath, "[语言 / Language]\n配置语言 / Config Language = English\n");
                    }

                    ConfigFile config = scenario.Bind();
                    foreach (ConfigKeyDefinition definition in ConfigSchema.All)
                    {
                        ConfigDefinition key = new ConfigDefinition(definition.Section(english), definition.Key(english));
                        Assert.That(config.ContainsKey(key), Is.True, (english ? "en " : "zh ") + definition.Id);
                        Assert.That(config[key].DefaultValue, Is.EqualTo(definition.DefaultValue), "declared default is the BepInEx default: " + definition.Id);
                    }
                }
            }
        }
    }
}
