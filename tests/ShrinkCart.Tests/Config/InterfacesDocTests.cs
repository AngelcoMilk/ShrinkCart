using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ShrinkCart.Config;

namespace ShrinkCart.Tests.Config
{
    /// <summary>
    /// docs/INTERFACES.md 的配置相关生成块必须与 <see cref="ConfigSchema"/> / <see cref="ConfigMigrator"/> 一致。
    /// 设置环境变量 SHRINKCART_UPDATE_DOCS=1 运行测试会改写生成块而不是失败（build.ps1 -UpdateDocs）。
    /// Hook 表与 GameMember 表由 tools/ShrinkCart.BuildTools 维护。
    /// </summary>
    public sealed class InterfacesDocTests
    {
        private const string UpdateVariable = "SHRINKCART_UPDATE_DOCS";

        [Test]
        public void ConfigSchemaBlockIsCurrent()
        {
            AssertBlock("config-schema", RenderSchema());
        }

        [Test]
        public void ConfigMigrationsBlockIsCurrent()
        {
            AssertBlock("config-migrations", RenderMigrations());
        }

        internal static string RenderSchema()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("| ID | 所属功能 | 中文分组 / 键 | English section / key | 类型 | 默认值 | 范围 | 引入版本 |\n");
            builder.Append("|---|---|---|---|---|---|---|---|\n");
            foreach (ConfigKeyDefinition definition in ConfigSchema.All)
            {
                builder.Append("| `").Append(definition.Id).Append("` | ")
                    .Append(definition.Feature).Append(" | ")
                    .Append(Cell(definition.ChineseSection)).Append(" / ").Append(Cell(definition.ChineseKey)).Append(" | ")
                    .Append(Cell(definition.EnglishSection)).Append(" / ").Append(Cell(definition.EnglishKey)).Append(" | ")
                    .Append(definition.ValueType == typeof(bool) ? "bool" : "float").Append(" | ")
                    .Append(Format(definition.DefaultValue)).Append(" | ")
                    .Append(definition.Minimum.HasValue && definition.Maximum.HasValue
                        ? Format(definition.Minimum.Value) + " – " + Format(definition.Maximum.Value)
                        : "—").Append(" | ")
                    .Append(Cell(definition.Since)).Append(" |\n");
            }

            return builder.ToString();
        }

        internal static string RenderMigrations()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("当前 schema 版本：**").Append(ConfigMigrator.CurrentSchemaVersion).Append("**（`[")
                .Append(ConfigSchema.MetaSection).Append("] ").Append(ConfigSchema.SchemaVersionKey).Append("`）。\n\n");
            builder.Append("| 新配置项 ID | 继承自旧键（section / key，按顺序取第一个存在的值） |\n");
            builder.Append("|---|---|\n");
            foreach (ConfigMigrator.LegacyRename rename in ConfigMigrator.LegacyRenames)
            {
                builder.Append("| `").Append(rename.Target.Id).Append("` | ");
                for (int i = 0; i + 1 < rename.SectionKeyPairs.Length; i += 2)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(Cell(rename.SectionKeyPairs[i])).Append(" / ").Append(Cell(rename.SectionKeyPairs[i + 1]));
                }

                builder.Append(" |\n");
            }

            builder.Append("\n一次性升级（仅 schema 0→1 执行）：值仍等于某个历史默认值的配置项，视为从没改过，升级为当前默认值；")
                .Append("玩家改过的值与开关类设置不动。缩小/放大速度另行无条件统一为 ")
                .Append(Format(ConfigSchema.CartScaleSpeed.DefaultValue)).Append(" / ")
                .Append(Format(ConfigSchema.RestoreScaleSpeed.DefaultValue)).Append("。\n\n");
            builder.Append("| 配置项 ID | 历史默认值 | 升级为 |\n");
            builder.Append("|---|---|---|\n");
            foreach (ConfigMigrator.LegacyDefault legacy in ConfigMigrator.LegacyDefaults)
            {
                builder.Append("| `").Append(legacy.Target.Id).Append("` | ")
                    .Append(string.Join(", ", legacy.OldDefaults.Select(v => Format(v)))).Append(" | ")
                    .Append(Format(legacy.Target.DefaultValue)).Append(" |\n");
            }

            builder.Append("\n<details><summary>每次启动都会删除的废弃键（")
                .Append(ConfigMigrator.DeprecatedKeys.Length / 2).Append(" 个）</summary>\n\n");
            for (int i = 0; i + 1 < ConfigMigrator.DeprecatedKeys.Length; i += 2)
            {
                builder.Append("- ").Append(Cell(ConfigMigrator.DeprecatedKeys[i])).Append(" / ").Append(Cell(ConfigMigrator.DeprecatedKeys[i + 1])).Append('\n');
            }

            builder.Append("\n</details>\n");
            return builder.ToString();
        }

        private static void AssertBlock(string name, string expected)
        {
            string path = Path.Combine(FindRepositoryRoot(), "docs", "INTERFACES.md");
            Assert.That(File.Exists(path), Is.True, "docs/INTERFACES.md exists");

            string text = File.ReadAllText(path, Encoding.UTF8);
            string begin = "<!-- BEGIN GENERATED: " + name + " -->";
            string end = "<!-- END GENERATED: " + name + " -->";
            int beginIndex = text.IndexOf(begin, StringComparison.Ordinal);
            int endIndex = text.IndexOf(end, StringComparison.Ordinal);
            Assert.That(beginIndex >= 0 && endIndex > beginIndex, Is.True, "generated block '" + name + "' present");

            int start = beginIndex + begin.Length;
            string actual = text.Substring(start, endIndex - start);
            if (Normalize(actual) == Normalize(expected))
            {
                return;
            }

            if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
            {
                string newline = text.Contains("\r\n") ? "\r\n" : "\n";
                string updated = text.Substring(0, start) + newline + expected.TrimEnd('\n').Replace("\n", newline) + newline + text.Substring(endIndex);
                File.WriteAllText(path, updated, new UTF8Encoding(false));
                return;
            }

            Assert.Fail("docs/INTERFACES.md block '" + name + "' is out of date; run build.ps1 -UpdateDocs (or set " + UpdateVariable + "=1 and run dotnet test).");
        }

        private static string FindRepositoryRoot()
        {
            for (DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ShrinkCart.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("ShrinkCart.sln not found above " + TestContext.CurrentContext.TestDirectory);
        }

        private static string Format(object value)
        {
            switch (value)
            {
                case bool flag:
                    return flag ? "true" : "false";
                case float number:
                    return number.ToString("0.###", CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string Cell(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").Trim('\n');
        }
    }
}
