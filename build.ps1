<#
.SYNOPSIS
    ShrinkCart build orchestration: build -> unit tests -> (optional) regenerate docs -> package the Thunderstore ZIP -> build-time validation.

.DESCRIPTION
    Local paths (game directory, r2modman profile) live only in ShrinkCart.local.props
    (copy ShrinkCart.local.props.example). The version lives only in <Version> of src/ShrinkCart/ShrinkCart.csproj.
    See docs/MAINTENANCE.md.

    Keep this file ASCII-only: Windows PowerShell 5.1 reads BOM-less scripts in the ANSI code page.

.EXAMPLE
    powershell.exe -ExecutionPolicy Bypass -File .\build.ps1
    powershell.exe -ExecutionPolicy Bypass -File .\build.ps1 -UpdateDocs
    powershell.exe -ExecutionPolicy Bypass -File .\build.ps1 -InstallToProfile
    powershell.exe -ExecutionPolicy Bypass -File .\build.ps1 -PackageToDesktop
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    # Regenerate the generated blocks of docs/INTERFACES.md (hooks, game members, config schema) before validating.
    [switch]$UpdateDocs,

    # Copy the package files into <R2ProfileDir>\BepInEx\plugins\AngelcoMilk-ShrinkCart\ for local testing.
    [switch]$InstallToProfile,

    # Copy the ZIP to the desktop (Release only).
    [switch]$PackageToDesktop
)

$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"

$root = $PSScriptRoot
$modName = "ShrinkCart"
$packageFolder = "AngelcoMilk-ShrinkCart"
$project = Join-Path $root "src\ShrinkCart\ShrinkCart.csproj"
$testProject = Join-Path $root "tests\ShrinkCart.Tests\ShrinkCart.Tests.csproj"
$toolProject = Join-Path $root "tools\ShrinkCart.BuildTools\ShrinkCart.BuildTools.csproj"
$toolDll = Join-Path $root "tools\ShrinkCart.BuildTools\bin\Release\net10.0\ShrinkCart.BuildTools.dll"

if ($PackageToDesktop -and $Configuration -ne "Release") {
    throw "-PackageToDesktop requires -Configuration Release."
}

$dotnet = $null
$command = Get-Command dotnet -ErrorAction SilentlyContinue
if ($command) {
    $dotnet = $command.Source
} elseif (Test-Path (Join-Path $env:ProgramFiles "dotnet\dotnet.exe")) {
    $dotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
} else {
    throw ".NET SDK not found. Install it with: winget install Microsoft.DotNet.SDK.10"
}

function Invoke-Dotnet {
    param([string[]]$Arguments)
    & $dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Assert-InsideRepository([string]$Path) {
    $resolved = [System.IO.Path]::GetFullPath($Path)
    if (!$resolved.StartsWith([System.IO.Path]::GetFullPath($root), [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the repository: $resolved"
    }
}

Write-Host "== Build ($Configuration)"
Invoke-Dotnet @("build", $project, "-c", $Configuration)
Invoke-Dotnet @("build", $toolProject, "-c", "Release")

Write-Host "== Unit tests"
if ($UpdateDocs) {
    # InterfacesDocTests rewrites the config-schema block instead of failing when this is set.
    $env:SHRINKCART_UPDATE_DOCS = "1"
}
try {
    Invoke-Dotnet @("test", $testProject, "-c", $Configuration)
} finally {
    Remove-Item Env:SHRINKCART_UPDATE_DOCS -ErrorAction SilentlyContinue
}

if ($UpdateDocs) {
    Write-Host "== Update generated docs"
    Invoke-Dotnet @($toolDll, "docs", "--configuration", $Configuration)
}

$json = (& $dotnet msbuild $project -nologo "-p:Configuration=$Configuration" "-getProperty:Version" "-getProperty:TargetPath" "-getProperty:R2PluginsDir") -join "`n"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet msbuild -getProperty failed: $json"
}
$properties = ($json | ConvertFrom-Json).Properties
$version = $properties.Version
$pluginDll = $properties.TargetPath

Write-Host "== Package $modName $version"
$dist = Join-Path $root "dist"
$stage = Join-Path $dist "package"
$zipPath = Join-Path $dist "$modName-$version.zip"
Assert-InsideRepository $dist
if (Test-Path $dist) {
    Remove-Item -LiteralPath $dist -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# Exactly five files at the ZIP root; the DLL sits at the root and r2modman installs it to BepInEx\plugins\<Team>-<Package>\.
$files = [ordered]@{
    "manifest.json" = Join-Path $root "package\manifest.json"
    "README.md"     = Join-Path $root "package\README.md"
    "icon.png"      = Join-Path $root "package\icon.png"
    "CHANGELOG.md"  = Join-Path $root "CHANGELOG.md"
    "$modName.dll"  = $pluginDll
}
foreach ($entry in $files.GetEnumerator()) {
    Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $stage $entry.Key) -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($entry in $files.GetEnumerator()) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $stage $entry.Key), $entry.Key) | Out-Null
    }
} finally {
    $zip.Dispose()
}
Write-Host "Packaged $zipPath"

Write-Host "== Validate"
Invoke-Dotnet @($toolDll, "validate", "--configuration", $Configuration, "--zip", $zipPath)

if ($InstallToProfile) {
    $pluginsDir = $properties.R2PluginsDir
    if (!(Test-Path $pluginsDir)) {
        throw "Profile plugins directory not found: $pluginsDir (set R2ProfileDir in ShrinkCart.local.props)."
    }

    $target = Join-Path $pluginsDir $packageFolder
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    foreach ($entry in $files.GetEnumerator()) {
        Copy-Item -LiteralPath (Join-Path $stage $entry.Key) -Destination (Join-Path $target $entry.Key) -Force
    }
    Write-Host "Installed to $target"
    Write-Host "Note: r2modman keeps listing the previously installed version; import the ZIP in r2modman to update its record."

    $legacy = Join-Path $pluginsDir $modName
    if (Test-Path (Join-Path $legacy "$modName.dll")) {
        Write-Warning "Found an older manual install at $legacy; remove it to avoid loading ShrinkCart twice."
    }
}

if ($PackageToDesktop) {
    $desktopZip = Join-Path ([Environment]::GetFolderPath("Desktop")) "$modName-$version.zip"
    Copy-Item -LiteralPath $zipPath -Destination $desktopZip -Force
    Write-Host "Copied $desktopZip"
}

Write-Host "Done: $modName $version ($Configuration)"
