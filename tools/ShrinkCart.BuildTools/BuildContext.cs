using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ShrinkCart.BuildTools
{
    /// <summary>
    /// 仓库路径与 MSBuild 求值结果。本机路径（游戏目录、r2modman profile）与版本号都用
    /// <c>dotnet msbuild -getProperty</c> 从 src/ShrinkCart/ShrinkCart.csproj 求值，与编译时使用的完全一致
    /// （包括 ShrinkCart.local.props 与命令行覆盖）。
    /// </summary>
    internal sealed class BuildContext
    {
        /// <summary>BepInEx 用 GUID 命名 cfg 文件（AngelcoMilk.ShrinkCart.cfg），改动会让所有玩家的配置变成新文件。</summary>
        internal const string ExpectedPluginGuid = "AngelcoMilk.ShrinkCart";

        private static readonly string[] EvaluatedProperties =
        {
            "Version",
            "TargetPath",
            "BepInExPluginGuid",
            "REPOGameDir",
            "REPOManagedDir",
            "R2ProfileDir",
            "R2PluginsDir",
            "ScalerCoreDir"
        };

        private BuildContext()
        {
        }

        internal string RepoRoot { get; private set; }

        internal string Configuration { get; private set; }

        internal string ProjectPath { get; private set; }

        internal string Version { get; private set; }

        internal string TargetPath { get; private set; }

        internal string PluginGuid { get; private set; }

        internal string GameDir { get; private set; }

        internal string ManagedDir { get; private set; }

        internal string ProfileDir { get; private set; }

        internal string PluginsDir { get; private set; }

        internal string ScalerCoreDir { get; private set; }

        internal string LocalPropsPath => Path.Combine(RepoRoot, "ShrinkCart.local.props");

        internal string RepoPath(params string[] parts)
        {
            return Path.Combine(RepoRoot, Path.Combine(parts));
        }

        internal static BuildContext Load(string repoOverride, string configuration)
        {
            string root = FindRepoRoot(repoOverride);
            string project = Path.Combine(root, "src", "ShrinkCart", "ShrinkCart.csproj");
            if (!File.Exists(project))
            {
                throw new BuildToolException("Project not found: " + project);
            }

            Dictionary<string, string> properties = EvaluateProperties(project, configuration);
            return new BuildContext
            {
                RepoRoot = root,
                Configuration = configuration,
                ProjectPath = project,
                Version = Get(properties, "Version"),
                TargetPath = Get(properties, "TargetPath"),
                PluginGuid = Get(properties, "BepInExPluginGuid"),
                GameDir = Get(properties, "REPOGameDir"),
                ManagedDir = Get(properties, "REPOManagedDir"),
                ProfileDir = Get(properties, "R2ProfileDir"),
                PluginsDir = Get(properties, "R2PluginsDir"),
                ScalerCoreDir = Get(properties, "ScalerCoreDir")
            };
        }

        internal static string FindRepoRoot(string repoOverride)
        {
            string start = string.IsNullOrEmpty(repoOverride) ? Directory.GetCurrentDirectory() : Path.GetFullPath(repoOverride);
            for (DirectoryInfo directory = new DirectoryInfo(start); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ShrinkCart.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new BuildToolException("ShrinkCart.sln not found at or above " + start + "; run from the repository or pass --repo <path>.");
        }

        private static string Get(Dictionary<string, string> properties, string name)
        {
            return properties.TryGetValue(name, out string value) ? value : string.Empty;
        }

        private static Dictionary<string, string> EvaluateProperties(string project, string configuration)
        {
            List<string> arguments = new List<string> { "msbuild", project, "-nologo", "-p:Configuration=" + configuration };
            foreach (string name in EvaluatedProperties)
            {
                arguments.Add("-getProperty:" + name);
            }

            DotnetHost.Result result = DotnetHost.Run(arguments);
            if (result.ExitCode != 0)
            {
                throw new BuildToolException("dotnet msbuild -getProperty failed (exit " + result.ExitCode + "):" + Environment.NewLine + result.Output);
            }

            Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                using (JsonDocument document = JsonDocument.Parse(result.StandardOutput))
                {
                    foreach (JsonProperty property in document.RootElement.GetProperty("Properties").EnumerateObject())
                    {
                        properties[property.Name] = property.Value.GetString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException)
            {
                throw new BuildToolException("Unexpected dotnet msbuild -getProperty output: " + ex.Message + Environment.NewLine + result.Output);
            }

            return properties;
        }
    }

    /// <summary>定位并调用当前运行时所属的 dotnet 宿主（不依赖 PATH）。</summary>
    internal static class DotnetHost
    {
        private static readonly string[] InheritedMSBuildVariables =
        {
            "MSBuildExtensionsPath",
            "MSBuildSDKsPath",
            "MSBUILD_EXE_PATH",
            "MSBuildLoadMicrosoftTargetsReadOnly"
        };

        internal static string HostPath { get; } = Locate();

        internal static Result Run(IEnumerable<string> arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo(HostPath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            foreach (string variable in InheritedMSBuildVariables)
            {
                start.Environment.Remove(variable);
            }

            start.Environment["DOTNET_CLI_FORCE_UTF8_ENCODING"] = "true";
            start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
            start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            start.Environment["DOTNET_NOLOGO"] = "1";

            using (Process process = Process.Start(start))
            {
                if (process == null)
                {
                    throw new BuildToolException("Failed to start " + HostPath);
                }

                System.Threading.Tasks.Task<string> error = process.StandardError.ReadToEndAsync();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return new Result(process.ExitCode, output, error.Result);
            }
        }

        private static string Locate()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
            if (!string.IsNullOrEmpty(fromEnvironment) && File.Exists(fromEnvironment))
            {
                return fromEnvironment;
            }

            // <dotnet 根目录>/shared/Microsoft.NETCore.App/<版本>/
            string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            DirectoryInfo runtime = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory().TrimEnd('\\', '/'));
            DirectoryInfo root = runtime.Parent?.Parent?.Parent;
            if (root != null && File.Exists(Path.Combine(root.FullName, executable)))
            {
                return Path.Combine(root.FullName, executable);
            }

            return executable;
        }

        internal sealed class Result
        {
            internal Result(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            internal int ExitCode { get; }

            internal string StandardOutput { get; }

            internal string StandardError { get; }

            internal string Output => (StandardOutput + Environment.NewLine + StandardError).Trim();
        }
    }
}
