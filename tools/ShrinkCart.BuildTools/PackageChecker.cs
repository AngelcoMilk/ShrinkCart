using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace ShrinkCart.BuildTools
{
    /// <summary>
    /// 打包素材、版本号一致性与 Thunderstore ZIP 布局。
    /// 版本号唯一来源是 csproj 的 &lt;Version&gt;；manifest.json、CHANGELOG.md 顶部标题与 DLL 中的 BepInPlugin 版本必须与它一致。
    /// </summary>
    internal static class PackageChecker
    {
        /// <summary>ZIP 根目录必须恰好包含这些文件（DLL 放在根目录，r2modman 会安装到 BepInEx/plugins/&lt;Team&gt;-&lt;Package&gt;/）。</summary>
        internal static readonly string[] ZipEntries = { "manifest.json", "README.md", "icon.png", "CHANGELOG.md", "ShrinkCart.dll" };

        private static readonly Regex ManifestName = new Regex(@"^[A-Za-z0-9_]{1,128}$", RegexOptions.CultureInvariant);
        private static readonly Regex SemanticVersion = new Regex(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);
        private static readonly Regex ChangelogHeading = new Regex(@"^##\s+(\d+\.\d+\.\d+)", RegexOptions.CultureInvariant);

        internal static void CheckSources(BuildContext context, Report report)
        {
            int failuresBefore = report.FailureCount;
            PackageManifest manifest = PackageManifest.Load(context.RepoPath("package", "manifest.json"));
            if (manifest.Name == null || !ManifestName.IsMatch(manifest.Name))
            {
                report.Fail("manifest.json name must match ^[A-Za-z0-9_]{1,128}$.");
            }

            if (manifest.VersionNumber == null || !SemanticVersion.IsMatch(manifest.VersionNumber))
            {
                report.Fail("manifest.json version_number must be Major.Minor.Patch.");
            }

            if (manifest.WebsiteUrl == null)
            {
                report.Fail("manifest.json website_url is required (an empty string is allowed).");
            }

            if (manifest.Description == null || manifest.Description.Length > 250)
            {
                report.Fail("manifest.json description is required and must be at most 250 characters.");
            }

            foreach (string dependency in manifest.Dependencies)
            {
                if (!PackageManifest.TryParseDependency(dependency, out _, out _))
                {
                    report.Fail("manifest.json dependency is not Team-Package-Major.Minor.Patch: " + dependency);
                }
            }

            string icon = context.RepoPath("package", "icon.png");
            if (!TryReadPngSize(File.Exists(icon) ? File.ReadAllBytes(icon) : null, out int width, out int height))
            {
                report.Fail("package/icon.png is missing or not a PNG.");
            }
            else if (width != 256 || height != 256)
            {
                report.Fail("package/icon.png must be 256x256; found " + width + "x" + height + ".");
            }

            string readme = context.RepoPath("README.md");
            string packageReadme = context.RepoPath("package", "README.md");
            foreach (string path in new[] { readme, packageReadme, context.RepoPath("CHANGELOG.md") })
            {
                if (!IsStrictUtf8(path))
                {
                    report.Fail(Relative(context, path) + " is missing or not valid UTF-8.");
                }
            }

            if (File.Exists(readme) && File.Exists(packageReadme) && NormalizedText(readme) != NormalizedText(packageReadme))
            {
                report.Fail("package/README.md differs from README.md; the Thunderstore README must stay in sync with the repository README.");
            }

            if (report.FailureCount == failuresBefore)
            {
                report.Pass("Package sources: manifest fields valid, icon 256x256 PNG, README/CHANGELOG UTF-8, package README in sync.");
            }
        }

        internal static void CheckVersions(BuildContext context, AssemblyModel model, Report report)
        {
            int failuresBefore = report.FailureCount;
            string version = context.Version;
            PackageManifest manifest = PackageManifest.Load(context.RepoPath("package", "manifest.json"));
            if (manifest.VersionNumber != version)
            {
                report.Fail("package/manifest.json version_number " + manifest.VersionNumber + " != csproj <Version> " + version + ".");
            }

            string changelogVersion = TopChangelogVersion(context.RepoPath("CHANGELOG.md"));
            if (changelogVersion != version)
            {
                report.Fail("CHANGELOG.md top entry " + (changelogVersion ?? "(none)") + " != csproj <Version> " + version + ".");
            }

            if (context.PluginGuid != BuildContext.ExpectedPluginGuid)
            {
                report.Fail("BepInExPluginGuid is " + context.PluginGuid + "; it must stay " + BuildContext.ExpectedPluginGuid + " (the config file name derives from it).");
            }

            CustomAttribute plugin = model.Mod.Types
                .SelectMany(t => t.CustomAttributes)
                .FirstOrDefault(a => a.AttributeType.FullName == "BepInEx.BepInPlugin");
            if (plugin == null || plugin.ConstructorArguments.Count < 3)
            {
                report.Fail("No [BepInPlugin] found in " + model.Mod.Name + ".");
            }
            else
            {
                string guid = plugin.ConstructorArguments[0].Value as string;
                string pluginVersion = plugin.ConstructorArguments[2].Value as string;
                if (guid != BuildContext.ExpectedPluginGuid)
                {
                    report.Fail("[BepInPlugin] GUID is " + guid + ", expected " + BuildContext.ExpectedPluginGuid + ".");
                }

                if (pluginVersion != version)
                {
                    report.Fail("[BepInPlugin] version " + pluginVersion + " != csproj <Version> " + version + " (stale build?).");
                }
            }

            if (report.FailureCount == failuresBefore)
            {
                report.Pass("Version " + version + " matches csproj, package/manifest.json, CHANGELOG.md and [BepInPlugin]; GUID " + BuildContext.ExpectedPluginGuid + ".");
            }
        }

        internal static void CheckZip(BuildContext context, string zipPath, Report report)
        {
            int failuresBefore = report.FailureCount;
            if (!File.Exists(zipPath))
            {
                report.Fail("Package not found: " + zipPath);
                return;
            }

            Dictionary<string, string> sources = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "manifest.json", context.RepoPath("package", "manifest.json") },
                { "README.md", context.RepoPath("package", "README.md") },
                { "icon.png", context.RepoPath("package", "icon.png") },
                { "CHANGELOG.md", context.RepoPath("CHANGELOG.md") },
                { "ShrinkCart.dll", context.TargetPath }
            };

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                List<string> names = archive.Entries.Select(e => e.FullName).ToList();
                foreach (string name in names)
                {
                    if (!ZipEntries.Contains(name, StringComparer.Ordinal))
                    {
                        report.Fail("Unexpected ZIP entry: " + name + " (the ZIP root must contain exactly " + string.Join(", ", ZipEntries) + ").");
                    }
                }

                foreach (string expected in ZipEntries)
                {
                    ZipArchiveEntry entry = archive.GetEntry(expected);
                    if (entry == null)
                    {
                        report.Fail("ZIP is missing root entry " + expected + ".");
                        continue;
                    }

                    byte[] packed = ReadAll(entry);
                    if (!File.Exists(sources[expected]) || !packed.AsSpan().SequenceEqual(File.ReadAllBytes(sources[expected])))
                    {
                        report.Fail("ZIP entry " + expected + " differs from " + sources[expected] + " (stale package).");
                    }
                }
            }

            if (report.FailureCount == failuresBefore)
            {
                report.Pass("ZIP layout: exactly " + string.Join(", ", ZipEntries) + " at the root, identical to the sources and build output.");
            }
        }

        internal static string TopChangelogVersion(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            foreach (string line in File.ReadLines(path, Encoding.UTF8))
            {
                Match match = ChangelogHeading.Match(line);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        internal static bool TryReadPngSize(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (bytes == null || bytes.Length < 24 || !bytes.AsSpan(0, 8).SequenceEqual(signature) || Encoding.ASCII.GetString(bytes, 12, 4) != "IHDR")
            {
                return false;
            }

            width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return true;
        }

        private static bool IsStrictUtf8(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                new UTF8Encoding(false, true).GetString(File.ReadAllBytes(path));
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static string NormalizedText(string path)
        {
            return File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n");
        }

        private static byte[] ReadAll(ZipArchiveEntry entry)
        {
            using (Stream stream = entry.Open())
            using (MemoryStream buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }

        private static string Relative(BuildContext context, string path)
        {
            return Path.GetRelativePath(context.RepoRoot, path).Replace('\\', '/');
        }
    }
}
