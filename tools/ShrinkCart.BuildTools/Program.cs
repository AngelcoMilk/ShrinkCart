using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ShrinkCart.BuildTools
{
    internal static class Program
    {
        private const string Usage =
            "Usage: dotnet run --project tools/ShrinkCart.BuildTools -- <command> [options]\n" +
            "\n" +
            "Commands:\n" +
            "  validate   Build-time checks derived from the build output (Harmony targets, non-public game member access,\n" +
            "             layer rules, profile dependency versions, version consistency, package sources, generated docs).\n" +
            "  docs       Regenerate the tool-owned blocks in docs/INTERFACES.md (hooks, game-members).\n" +
            "  doctor     Check the local development environment (SDK, local props, game dir, r2modman profile, dependencies).\n" +
            "\n" +
            "Options:\n" +
            "  --configuration <Debug|Release>   Build configuration to inspect (default: Release).\n" +
            "  --zip <path>                      validate: also check the Thunderstore ZIP layout and contents.\n" +
            "  --repo <path>                     Repository root (default: search upward for ShrinkCart.sln).\n";

        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
            {
                Console.Write(Usage);
                return args.Length == 0 ? 2 : 0;
            }

            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.Write(Usage);
                return 2;
            }

            Report report = new Report();
            try
            {
                switch (options.Command)
                {
                    case "validate":
                        Validate(options, report);
                        break;
                    case "docs":
                        Docs(options, report);
                        break;
                    case "doctor":
                        Doctor(options, report);
                        break;
                    default:
                        Console.Error.WriteLine("Unknown command: " + options.Command);
                        Console.Error.Write(Usage);
                        return 2;
                }
            }
            catch (BuildToolException ex)
            {
                report.Fail(ex.Message);
            }

            return report.Finish();
        }

        private static void Validate(Options options, Report report)
        {
            BuildContext context = LoadContext(options, report);
            using (AssemblyModel model = AssemblyModel.Load(context))
            {
                report.Section("Harmony patches");
                List<PatchInfo> patches = HarmonyPatchScanner.Scan(model, report);

                report.Section("Game member access");
                List<GameMemberInfo> members = GameMemberScanner.Scan(model, report);

                report.Section("Layer rules");
                LayerRules.Check(model, report);

                report.Section("Dependencies (r2modman profile)");
                DependencyChecker.Check(context, report);

                report.Section("Versions and package");
                PackageChecker.CheckVersions(context, model, report);
                PackageChecker.CheckSources(context, report);
                if (!string.IsNullOrEmpty(options.Zip))
                {
                    PackageChecker.CheckZip(context, Path.GetFullPath(options.Zip), report);
                }

                report.Section("Generated documentation");
                InterfacesDoc.Check(context, patches, members, report);
            }
        }

        private static void Docs(Options options, Report report)
        {
            BuildContext context = LoadContext(options, report);
            using (AssemblyModel model = AssemblyModel.Load(context))
            {
                report.Section("Scan");
                List<PatchInfo> patches = HarmonyPatchScanner.Scan(model, report);
                List<GameMemberInfo> members = GameMemberScanner.Scan(model, report);

                report.Section("docs/INTERFACES.md");
                InterfacesDoc.Update(context, patches, members, report);
                report.Info("The config-schema block is maintained by the unit tests: set SHRINKCART_UPDATE_DOCS=1 and run 'dotnet test'.");
            }
        }

        private static void Doctor(Options options, Report report)
        {
            report.Section(".NET SDK");
            DotnetHost.Result sdk = DotnetHost.Run(new[] { "--version" });
            if (sdk.ExitCode == 0)
            {
                report.Pass(".NET SDK " + sdk.StandardOutput.Trim() + " (" + DotnetHost.HostPath + ")");
            }
            else
            {
                report.Fail("'dotnet --version' failed: " + sdk.Output);
            }

            string root = BuildContext.FindRepoRoot(options.Repo);
            string globalJson = Path.Combine(root, "global.json");
            if (File.Exists(globalJson))
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(globalJson)))
                {
                    report.Info("global.json requires SDK " + document.RootElement.GetProperty("sdk").GetProperty("version").GetString() + " (rollForward latestFeature).");
                }
            }

            BuildContext context;
            try
            {
                context = LoadContext(options, report);
            }
            catch (BuildToolException ex)
            {
                report.Fail(ex.Message);
                return;
            }

            report.Section("Local configuration");
            if (File.Exists(context.LocalPropsPath))
            {
                report.Pass("ShrinkCart.local.props found.");
            }
            else
            {
                report.Warn("ShrinkCart.local.props not found; using defaults. Copy ShrinkCart.local.props.example and set REPOGameDir / R2ProfileDir.");
            }

            report.Section("Game");
            foreach (string file in new[] { "Assembly-CSharp.dll", "UnityEngine.dll", "UnityEngine.CoreModule.dll", "UnityEngine.PhysicsModule.dll", "PhotonUnityNetworking.dll", "netstandard.dll" })
            {
                RequireFile(Path.Combine(context.ManagedDir, file), report);
            }

            report.Section("r2modman profile");
            if (!Directory.Exists(context.ProfileDir))
            {
                report.Fail("Profile not found: " + context.ProfileDir);
            }
            else
            {
                string core = Path.Combine(context.ProfileDir, "BepInEx", "core");
                foreach (string file in new[] { "BepInEx.dll", "0Harmony.dll" })
                {
                    string path = Path.Combine(core, file);
                    if (RequireFile(path, report))
                    {
                        report.Info(file + " file version " + System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion);
                    }
                }

                DependencyChecker.Check(context, report);
            }

            report.Section("Package sources");
            PackageChecker.CheckSources(context, report);

            report.Section("Optional analysis tools");
            string localPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
            Optional(Path.Combine(localPrograms, "ILSpy", "ILSpy.exe"), "ILSpy", report);
            Optional(Path.Combine(localPrograms, "dnSpyEx", "6.6.0", "dnSpy.exe"), "dnSpyEx", report);
        }

        private static BuildContext LoadContext(Options options, Report report)
        {
            BuildContext context = BuildContext.Load(options.Repo, options.Configuration);
            report.Section("Build context");
            report.Info("Repository: " + context.RepoRoot);
            report.Info("Version " + context.Version + ", configuration " + context.Configuration + ", output " + context.TargetPath);
            report.Info("Game: " + context.GameDir);
            report.Info("Profile: " + context.ProfileDir);
            return context;
        }

        private static bool RequireFile(string path, Report report)
        {
            if (File.Exists(path))
            {
                report.Pass(path);
                return true;
            }

            report.Fail("Missing: " + path);
            return false;
        }

        private static void Optional(string path, string name, Report report)
        {
            report.Info(File.Exists(path) ? name + ": " + path : name + " not installed (optional; see tools/README.md).");
        }

        private sealed class Options
        {
            internal string Command;
            internal string Configuration = "Release";
            internal string Zip;
            internal string Repo;

            internal static Options Parse(string[] args)
            {
                Options options = new Options { Command = args[0] };
                for (int i = 1; i < args.Length; i++)
                {
                    string name = args[i];
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for " + name);
                    }

                    string value = args[++i];
                    switch (name)
                    {
                        case "--configuration":
                        case "-c":
                            if (value != "Debug" && value != "Release")
                            {
                                throw new ArgumentException("--configuration must be Debug or Release.");
                            }

                            options.Configuration = value;
                            break;
                        case "--zip":
                            options.Zip = value;
                            break;
                        case "--repo":
                            options.Repo = value;
                            break;
                        default:
                            throw new ArgumentException("Unknown option: " + name);
                    }
                }

                return options;
            }
        }
    }
}
