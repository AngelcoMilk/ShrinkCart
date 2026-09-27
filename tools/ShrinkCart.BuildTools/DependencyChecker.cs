using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ShrinkCart.BuildTools
{
    /// <summary>package/manifest.json 的内容。</summary>
    internal sealed class PackageManifest
    {
        internal string Name;
        internal string VersionNumber;
        internal string WebsiteUrl;
        internal string Description;
        internal readonly List<string> Dependencies = new List<string>();

        internal static PackageManifest Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new BuildToolException("Package manifest not found: " + path);
            }

            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8)))
                {
                    JsonElement root = document.RootElement;
                    PackageManifest manifest = new PackageManifest
                    {
                        Name = OptionalString(root, "name"),
                        VersionNumber = OptionalString(root, "version_number"),
                        WebsiteUrl = OptionalString(root, "website_url"),
                        Description = OptionalString(root, "description")
                    };
                    if (root.TryGetProperty("dependencies", out JsonElement dependencies) && dependencies.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement dependency in dependencies.EnumerateArray())
                        {
                            manifest.Dependencies.Add(dependency.GetString() ?? string.Empty);
                        }
                    }

                    return manifest;
                }
            }
            catch (JsonException ex)
            {
                throw new BuildToolException("Invalid JSON in " + path + ": " + ex.Message);
            }
        }

        /// <summary>"Team-Package-1.2.3" → ("Team-Package", 1.2.3)。</summary>
        internal static bool TryParseDependency(string dependency, out string name, out Version version)
        {
            name = null;
            version = null;
            int dash = dependency == null ? -1 : dependency.LastIndexOf('-');
            if (dash <= 0 || dependency.IndexOf('-') == dash)
            {
                return false;
            }

            name = dependency.Substring(0, dash);
            return Version.TryParse(dependency.Substring(dash + 1), out version);
        }

        private static string OptionalString(JsonElement root, string property)
        {
            return root.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
    }

    /// <summary>r2modman profile 的 mods.yml（只解析 name / versionNumber / enabled）。</summary>
    internal sealed class ProfileInventory
    {
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        internal string Path { get; private set; }

        internal static ProfileInventory Load(string profileDir)
        {
            string path = System.IO.Path.Combine(profileDir, "mods.yml");
            if (!File.Exists(path))
            {
                return null;
            }

            ProfileInventory inventory = new ProfileInventory { Path = path };
            Entry current = null;
            bool inVersion = false;
            foreach (string rawLine in File.ReadAllLines(path, Encoding.UTF8))
            {
                string line = rawLine.TrimEnd();
                if (line.StartsWith("- ", StringComparison.Ordinal))
                {
                    current = new Entry();
                    inVersion = false;
                    line = "  " + line.Substring(2);
                }

                if (current == null || line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("  ", StringComparison.Ordinal) && !line.StartsWith("   ", StringComparison.Ordinal))
                {
                    string field = line.Trim();
                    inVersion = field == "versionNumber:";
                    if (field.StartsWith("name: ", StringComparison.Ordinal))
                    {
                        current.Name = field.Substring(6).Trim();
                        inventory._entries[current.Name] = current;
                    }
                    else if (field.StartsWith("enabled: ", StringComparison.Ordinal))
                    {
                        current.Enabled = field.Substring(9).Trim() == "true";
                    }
                }
                else if (inVersion && line.StartsWith("    ", StringComparison.Ordinal))
                {
                    string field = line.Trim();
                    int colon = field.IndexOf(':');
                    if (colon > 0 && int.TryParse(field.Substring(colon + 1).Trim(), out int number))
                    {
                        switch (field.Substring(0, colon))
                        {
                            case "major":
                                current.Major = number;
                                break;
                            case "minor":
                                current.Minor = number;
                                break;
                            case "patch":
                                current.Patch = number;
                                break;
                        }
                    }
                }
            }

            return inventory;
        }

        internal bool TryGet(string name, out Entry entry)
        {
            return _entries.TryGetValue(name, out entry);
        }

        internal sealed class Entry
        {
            internal string Name;
            internal bool Enabled = true;
            internal int Major;
            internal int Minor;
            internal int Patch;

            internal Version Version => new Version(Major, Minor, Patch);
        }
    }

    /// <summary>
    /// 依赖只从指定 profile 解析：manifest.json 声明的每个依赖都必须在 profile 中安装、启用，且版本不低于声明版本；
    /// 编译引用的 ScalerCore.dll 必须来自该 profile 的 Vippy-ScalerCore 目录。
    /// </summary>
    internal static class DependencyChecker
    {
        internal static void Check(BuildContext context, Report report)
        {
            PackageManifest manifest = PackageManifest.Load(context.RepoPath("package", "manifest.json"));
            if (!Directory.Exists(context.ProfileDir))
            {
                report.Fail("r2modman profile not found: " + context.ProfileDir + " (set R2ProfileDir in ShrinkCart.local.props).");
                return;
            }

            ProfileInventory inventory = ProfileInventory.Load(context.ProfileDir);
            if (inventory == null)
            {
                report.Fail("mods.yml not found in profile " + context.ProfileDir + "; is this an r2modman profile?");
                return;
            }

            foreach (string dependency in manifest.Dependencies)
            {
                if (!PackageManifest.TryParseDependency(dependency, out string name, out Version required))
                {
                    report.Fail("Invalid dependency string in package/manifest.json: " + dependency);
                    continue;
                }

                if (!inventory.TryGet(name, out ProfileInventory.Entry entry))
                {
                    report.Fail(name + " is not installed in profile " + context.ProfileDir + " (declared " + required + ").");
                    continue;
                }

                if (!entry.Enabled)
                {
                    report.Fail(name + " " + entry.Version + " is disabled in profile " + context.ProfileDir + ".");
                    continue;
                }

                if (entry.Version < required)
                {
                    report.Fail(name + " " + entry.Version + " in the profile is older than the declared dependency " + required + ".");
                    continue;
                }

                string pluginDir = Path.Combine(context.PluginsDir, name);
                string location = Directory.Exists(pluginDir) ? pluginDir : context.ProfileDir;
                report.Pass(name + " " + entry.Version + " (declared >= " + required + ") at " + location);

                string pluginManifest = Path.Combine(pluginDir, "manifest.json");
                if (File.Exists(pluginManifest))
                {
                    string installed = PackageManifest.Load(pluginManifest).VersionNumber;
                    if (installed != entry.Version.ToString())
                    {
                        report.Warn(name + ": mods.yml says " + entry.Version + " but " + pluginManifest + " says " + installed + ".");
                    }
                }
            }

            string scalerCore = Path.Combine(context.ScalerCoreDir, "ScalerCore.dll");
            string pluginsRoot = Path.GetFullPath(context.PluginsDir);
            if (!File.Exists(scalerCore))
            {
                report.Fail("Compile reference not found: " + scalerCore);
            }
            else if (!Path.GetFullPath(scalerCore).StartsWith(pluginsRoot, StringComparison.OrdinalIgnoreCase))
            {
                report.Warn("ScalerCore compile reference is outside the profile: " + scalerCore);
            }
            else
            {
                report.Pass("ScalerCore compile reference: " + scalerCore);
            }
        }
    }
}
