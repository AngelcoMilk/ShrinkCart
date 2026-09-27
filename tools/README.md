# Tools

This directory contains the build-time tool, analysis-tool setup, and read-only profile inspection scripts. Third-party binaries, game DLLs, decompiled source, patched assemblies, and ripped assets must stay outside the repository.

## ShrinkCart.BuildTools

A .NET 10 console tool (Mono.Cecil) that `build.ps1` runs after every build. It can also be run on its own from the repository root:

```powershell
dotnet run --project tools/ShrinkCart.BuildTools -- doctor
dotnet run --project tools/ShrinkCart.BuildTools -- validate [--configuration Release] [--zip dist/ShrinkCart-<version>.zip]
dotnet run --project tools/ShrinkCart.BuildTools -- docs
```

- `doctor` checks the local development environment:
  - the .NET SDK and `ShrinkCart.local.props`;
  - the game `Managed` directory;
  - the r2modman profile's BepInEx core;
  - each manifest dependency's installed and enabled version (read from the profile's `mods.yml`);
  - package assets.

  It replaces the former `Test-DevelopmentEnvironment.ps1`.
- `validate` runs the build-time checks, all derived from the build output and the original (non-publicized) `Assembly-CSharp.dll`:
  - Harmony targets and injectable patch parameters;
  - non-public game members accessed only through `GameAccess`, matching its `[GameMember]` declarations;
  - layer rules;
  - profile dependency versions;
  - version consistency across csproj, manifest, CHANGELOG and `[BepInPlugin]`;
  - package assets and ZIP layout;
  - up-to-date generated docs.

  See `docs/MAINTENANCE.md` section 3.1.
- `docs` regenerates the tool-owned blocks of `docs/INTERFACES.md` (hooks, game members).

Local paths come from the same MSBuild evaluation as the build (`ShrinkCart.local.props`), so the tool always inspects exactly what was compiled.

## Analysis tools (Windows)

Run from the repository root with Windows PowerShell:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1
```

This installs:

- **ILSpy** through the official `icsharpcode.ILSpy` winget package.
- **MonoMod 22.07.31.01 net452** from the official MonoMod GitHub release into `%LOCALAPPDATA%\Programs\MonoMod`.

Optional tools are opt-in because their archives are larger and overlap with ILSpy:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1 -InstallDnSpyEx
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1 -InstallAvaloniaILSpy
```

- `dnSpyEx` is the maintained continuation to use instead of the archived original dnSpy.
- AvaloniaILSpy is mainly a macOS/Linux or cross-platform fallback. Native ILSpy is preferred on Windows.

Downloads are pinned to official GitHub release URLs. Use `-Force` to redownload and replace extracted copies.

## Analysis workflow

1. Work from the installed game file at `<GameDir>\REPO_Data\Managed\Assembly-CSharp.dll`; never copy it into this repository.
2. Open it in ILSpy or dnSpyEx and add the containing `Managed` directory as the assembly resolution context.
3. Record the declaring type, full method signature (including parameter names, which Harmony binds by name), fields used, callers, executing side (local player / host / all clients) and call frequency.
4. For a Harmony Transpiler, switch to IL view and record opcode/operand patterns plus a surrounding semantic anchor. Do not rely only on compiler-generated offsets.
5. Use MonoMod `DebugIL`/`HookGen` only on disposable copies in a user-local scratch directory. Do not patch the installed game's original DLL in place.
6. Implement the smallest strongly typed Harmony patch possible, run `build.ps1` (which runs `validate`), then test in game and in a private multiplayer session.

Example read-only launch:

```powershell
& "$env:LOCALAPPDATA\Programs\ILSpy\ILSpy.exe" `
  "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll"
```

## Legal and repository boundary

Do not commit or publish:

- `Assembly-CSharp.dll`, other Unity/game assemblies, or the publicized copy generated under `obj/`;
- decompiler exports or reconstructed game source;
- patched copies of game assemblies;
- ripped textures, audio, models, scenes, or prefabs;
- user profiles, tokens, or private logs.

Only commit original mod source, documentation, scripts, configuration, and assets you have the right to distribute.

## Inspect local r2modman R.E.P.O. profiles

Use the read-only inventory script instead of treating every directory under the shared cache as installed:

```powershell
# List enabled packages in every local R.E.P.O. profile
powershell.exe -ExecutionPolicy Bypass -File .\tools\Get-LocalREPOProfileInventory.ps1

# Include disabled packages for one profile
powershell.exe -ExecutionPolicy Bypass -File .\tools\Get-LocalREPOProfileInventory.ps1 `
  -ProfileName "REPO" -IncludeDisabled

# Machine-readable output for a local comparison; do not commit it
powershell.exe -ExecutionPolicy Bypass -File .\tools\Get-LocalREPOProfileInventory.ps1 `
  -ProfileName "REPO" -IncludeDisabled -AsJson
```

The script reads each profile's `mods.yml`.

- The shared `cache` can retain old versions, manually imported archives, and packages used by other profiles, so cache presence alone does not mean a package is enabled.
- Keep generated inventories, profile names, exports, configurations, logs, and installed binaries local, unless a deliberately sanitized summary is needed in documentation.
