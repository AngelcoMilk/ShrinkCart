using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BepInEx.Logging;
using NUnit.Framework;
using ShrinkCart.Config;
using ShrinkCart.Core;

namespace ShrinkCart.Tests.Config
{
    /// <summary>在临时目录中用历史 cfg 样本（tests/fixtures/config）运行 ModConfig.Bind，并捕获日志。</summary>
    internal sealed class ConfigScenario : IDisposable
    {
        internal const string ConfigFileName = "AngelcoMilk.ShrinkCart.cfg";

        private readonly ManualLogSource _log = new ManualLogSource("ShrinkCart.Tests");
        private readonly List<string> _messages = new List<string>();

        private ConfigScenario(string fixture)
        {
            Directory = Path.Combine(Path.GetTempPath(), "ShrinkCartTests_" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            ConfigPath = Path.Combine(Directory, ConfigFileName);
            if (fixture != null)
            {
                File.Copy(Path.Combine(TestContext.CurrentContext.TestDirectory, "fixtures", "config", fixture), ConfigPath);
            }

            _log.LogEvent += (sender, e) => _messages.Add(e.Level + ": " + e.Data);
            ModLog.Initialize(_log, () => false);
        }

        internal string Directory { get; }

        internal string ConfigPath { get; }

        internal string BackupPath => ConfigPath + ConfigMigrator.BackupSuffix;

        internal string Saved => File.ReadAllText(ConfigPath, Encoding.UTF8);

        internal bool ResetLogged => _messages.Exists(m => m.Contains("reset once to defaults"));

        internal static ConfigScenario Fresh()
        {
            return new ConfigScenario(null);
        }

        internal static ConfigScenario FromFixture(string fixture)
        {
            return new ConfigScenario(fixture);
        }

        internal ConfigFile Bind()
        {
            _messages.Clear();
            ConfigFile config = new ConfigFile(ConfigPath, false);
            ModConfig.Bind(config);
            return config;
        }

        internal bool HasLine(string linePrefix)
        {
            return Regex.IsMatch(Saved, "^" + Regex.Escape(linePrefix), RegexOptions.Multiline);
        }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, true);
            }
            catch
            {
            }
        }
    }
}
