# Unity/.NET analysis toolchain

This directory contains configuration and validation scripts only. Third-party binaries, game DLLs, decompiled source, patched assemblies, and ripped assets must stay outside the repository.

## Windows setup

Run from the repository root with Windows PowerShell:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1
```

This installs:

- **ILSpy** through the official `icsharpcode.ILSpy` winget package.
- **MonoMod 22.07.31.01 net452** from the official MonoMod GitHub release into `%LOCALAPPDATA%\Programs\MonoMod`.

Optional tools are deliberately opt-in because their archives are larger and overlap with ILSpy:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1 -InstallDnSpyEx
powershell.exe -ExecutionPolicy Bypass -File .\tools\Setup-ModAnalysisTools.ps1 -InstallAvaloniaILSpy
```

- `dnSpyEx` is the maintained continuation to use instead of the archived original dnSpy.
- AvaloniaILSpy is mainly a macOS/Linux or cross-platform fallback. Native ILSpy is preferred on Windows.

Downloads are pinned to official GitHub release URLs. Use `-Force` to redownload and replace extracted copies.

## Verify the environment

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Test-DevelopmentEnvironment.ps1
```

The validator auto-detects common Steam library locations. Override local paths when required:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\tools\Test-DevelopmentEnvironment.ps1 `
  -GameDir "D:\SteamLibrary\steamapps\common\REPO" `
  -R2Profile "$env:APPDATA\r2modmanPlus-local\REPO\profiles\REPO"
```

It checks game/profile dependencies, package metadata, exact icon dimensions, README UTF-8 validity, and installed analysis tools.

## Analysis workflow

1. Work from the installed game file at `<GameDir>\REPO_Data\Managed\Assembly-CSharp.dll`; never copy it into this repository.
2. Open it in ILSpy or dnSpyEx and add the containing `Managed` directory as the assembly resolution context.
3. Record the declaring type, full method signature, fields used, callers, and local/host authority behavior.
4. For a Harmony Transpiler, switch to IL view and record opcode/operand patterns plus a surrounding semantic anchor. Do not rely only on compiler-generated offsets.
5. Use MonoMod `DebugIL`/`HookGen` only on disposable copies in a user-local scratch directory. Do not patch the installed game's original DLL in place.
6. Implement the smallest strongly typed Harmony patch possible, then validate in game and in a private multiplayer session.

Example read-only launch:

```powershell
& "$env:LOCALAPPDATA\Programs\ILSpy\ILSpy.exe" `
  "C:\Program Files (x86)\Steam\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll"
```

## Legal and repository boundary

Do not commit or publish:

- `Assembly-CSharp.dll` or other Unity/game assemblies;
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

The script reads each profile's `mods.yml`. The shared `cache` can retain old versions, manually imported archives, and packages used by other profiles, so cache presence alone does not mean a package is enabled. Keep generated inventories, profile names, exports, configurations, logs, and installed binaries local unless a deliberately sanitized summary is needed in documentation.
