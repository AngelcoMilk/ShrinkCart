using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ShrinkCart.BuildTools
{
    /// <summary>
    /// docs/INTERFACES.md 中由工具维护的生成块：
    /// <c>&lt;!-- BEGIN GENERATED: hooks --&gt;</c> 与 <c>&lt;!-- BEGIN GENERATED: game-members --&gt;</c>。
    /// 块内容完全由编译产物推导（[PatchFeature]/[HarmonyPatch]、[GameMember] 与原版可见性），手写修改会被 validate 判为过期。
    /// 配置表（config-schema 块）由单元测试 InterfacesDocTests 维护。
    /// </summary>
    internal static class InterfacesDoc
    {
        internal const string HooksBlock = "hooks";
        internal const string GameMembersBlock = "game-members";

        internal static string PathOf(BuildContext context)
        {
            return context.RepoPath("docs", "INTERFACES.md");
        }

        internal static string RenderHooks(IEnumerable<PatchInfo> patches)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("| 目标方法 | 补丁 | 所属功能 | 调用频率 | 执行端 | Patch 类 |\n");
            builder.Append("|---|---|---|---|---|---|\n");
            foreach (PatchInfo patch in patches.OrderBy(p => p.FeatureOrder).ThenBy(p => p.Target, StringComparer.Ordinal))
            {
                builder.Append("| `").Append(Cell(patch.Target)).Append("` | ")
                    .Append(Cell(patch.Kinds)).Append(" | ")
                    .Append(Cell(patch.Feature)).Append(" | ")
                    .Append(Cell(patch.Frequency)).Append(" | ")
                    .Append(Cell(patch.Side)).Append(" | `")
                    .Append(Cell(patch.PatchClass)).Append("` |\n");
            }

            return builder.ToString();
        }

        internal static string RenderGameMembers(IEnumerable<GameMemberInfo> members)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("| 成员 | 种类 | 类型 | 原版可见性 | GameAccess 方法 | 所属功能 |\n");
            builder.Append("|---|---|---|---|---|---|\n");
            foreach (GameMemberInfo member in members)
            {
                builder.Append("| `").Append(Cell(member.Member)).Append("` | ")
                    .Append(Cell(member.Kind)).Append(" | `")
                    .Append(Cell(member.MemberType)).Append("` | ")
                    .Append(Cell(member.Visibility)).Append(" | ")
                    .Append(string.Join(", ", member.Accessors.Select(a => "`" + Cell(a) + "`"))).Append(" | ")
                    .Append(Cell(string.Join(", ", member.Features.Values))).Append(" |\n");
            }

            return builder.ToString();
        }

        internal static void Check(BuildContext context, List<PatchInfo> patches, List<GameMemberInfo> members, Report report)
        {
            string path = PathOf(context);
            if (!File.Exists(path))
            {
                report.Fail("docs/INTERFACES.md not found.");
                return;
            }

            string text = File.ReadAllText(path, Encoding.UTF8);
            bool current = true;
            foreach (KeyValuePair<string, string> block in Blocks(patches, members))
            {
                if (!TryGetBlock(text, block.Key, out string existing))
                {
                    report.Fail("docs/INTERFACES.md has no '" + block.Key + "' generated block (" + Begin(block.Key) + " ... " + End(block.Key) + ").");
                    current = false;
                }
                else if (Normalize(existing) != Normalize(block.Value))
                {
                    report.Fail("docs/INTERFACES.md '" + block.Key + "' block is out of date; run: dotnet run --project tools/ShrinkCart.BuildTools -- docs");
                    current = false;
                }
            }

            if (current)
            {
                report.Pass("docs/INTERFACES.md generated blocks (hooks, game-members) match the build output.");
            }
        }

        internal static void Update(BuildContext context, List<PatchInfo> patches, List<GameMemberInfo> members, Report report)
        {
            string path = PathOf(context);
            if (!File.Exists(path))
            {
                report.Fail("docs/INTERFACES.md not found.");
                return;
            }

            string original = File.ReadAllText(path, Encoding.UTF8);
            string newline = original.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            string text = original;
            foreach (KeyValuePair<string, string> block in Blocks(patches, members))
            {
                if (!TryReplaceBlock(text, block.Key, block.Value.Replace("\n", newline), out text))
                {
                    report.Fail("docs/INTERFACES.md has no '" + block.Key + "' generated block to update.");
                    return;
                }
            }

            if (text == original)
            {
                report.Pass("docs/INTERFACES.md generated blocks are already up to date.");
                return;
            }

            File.WriteAllText(path, text, new UTF8Encoding(false));
            report.Pass("Updated docs/INTERFACES.md generated blocks (hooks: " + patches.Count + " patch classes, game-members: " + members.Count + " members).");
        }

        internal static string Begin(string name)
        {
            return "<!-- BEGIN GENERATED: " + name + " -->";
        }

        internal static string End(string name)
        {
            return "<!-- END GENERATED: " + name + " -->";
        }

        internal static bool TryGetBlock(string text, string name, out string content)
        {
            content = null;
            int begin = text.IndexOf(Begin(name), StringComparison.Ordinal);
            int end = text.IndexOf(End(name), StringComparison.Ordinal);
            if (begin < 0 || end < begin)
            {
                return false;
            }

            int start = begin + Begin(name).Length;
            content = text.Substring(start, end - start);
            return true;
        }

        internal static bool TryReplaceBlock(string text, string name, string content, out string updated)
        {
            updated = text;
            int begin = text.IndexOf(Begin(name), StringComparison.Ordinal);
            int end = text.IndexOf(End(name), StringComparison.Ordinal);
            if (begin < 0 || end < begin)
            {
                return false;
            }

            string newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            int start = begin + Begin(name).Length;
            updated = text.Substring(0, start) + newline + content.TrimEnd('\r', '\n') + newline + text.Substring(end);
            return true;
        }

        private static IEnumerable<KeyValuePair<string, string>> Blocks(List<PatchInfo> patches, List<GameMemberInfo> members)
        {
            yield return new KeyValuePair<string, string>(HooksBlock, RenderHooks(patches));
            yield return new KeyValuePair<string, string>(GameMembersBlock, RenderGameMembers(members));
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").Trim('\n');
        }

        private static string Cell(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
