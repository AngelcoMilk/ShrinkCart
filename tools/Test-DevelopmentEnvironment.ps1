[CmdletBinding()]
param(
    [string]$GameDir = "",
    [string]$R2Profile = "$env:APPDATA\r2modmanPlus-local\REPO\profiles\REPO"
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Add-Failure([string]$Message) {
    $failures.Add($Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Add-Warning([string]$Message) {
    $warnings.Add($Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Add-Pass([string]$Message) {
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Find-RepoGameDir {
    $candidates = New-Object System.Collections.Generic.List[string]
    $candidates.Add("C:\Program Files (x86)\Steam\steamapps\common\REPO")
    $candidates.Add("D:\SteamLibrary\steamapps\common\REPO")

    $libraryFile = "C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf"
    if (Test-Path $libraryFile) {
        $content = Get-Content -Raw $libraryFile
        foreach ($match in [regex]::Matches($content, '"path"\s+"([^"]+)"')) {
            $library = $match.Groups[1].Value -replace '\\\\', '\'
            $candidates.Add((Join-Path $library "steamapps\common\REPO"))
        }
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        if (Test-Path (Join-Path $candidate "REPO_Data\Managed\Assembly-CSharp.dll")) {
            return $candidate
        }
    }
    return $null
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $GameDir = Find-RepoGameDir
}

Write-Host "ShrinkCart development environment validation"
Write-Host ""

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    Add-Failure "R.E.P.O. installation was not found. Pass -GameDir explicitly."
} elseif (Test-Path (Join-Path $GameDir "REPO_Data\Managed\Assembly-CSharp.dll")) {
    Add-Pass "Game assembly found: $GameDir\REPO_Data\Managed\Assembly-CSharp.dll"
} else {
    Add-Failure "Assembly-CSharp.dll is missing under: $GameDir"
}

$bepCore = Join-Path $R2Profile "BepInEx\core"
if (Test-Path $bepCore) {
    Add-Pass "r2modman profile found: $R2Profile"
} else {
    Add-Failure "BepInEx profile core is missing: $bepCore"
}

foreach ($name in @("BepInEx.dll", "0Harmony.dll", "Mono.Cecil.dll")) {
    $path = Join-Path $bepCore $name
    if (Test-Path $path) { Add-Pass $path } else { Add-Failure "Missing dependency: $path" }
}

$cacheRoot = Join-Path $env:APPDATA "r2modmanPlus-local\REPO\cache"
foreach ($dependency in @("ScalerCore.dll", "REPOConfig.dll")) {
    $found = Get-ChildItem -Path $cacheRoot -Filter $dependency -Recurse -File -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if ($found) { Add-Pass "$dependency candidate: $($found.FullName)" } else { Add-Failure "$dependency not found under $cacheRoot" }
}

$manifestPath = Join-Path $PSScriptRoot "..\package\manifest.json"
try {
    $manifestText = Get-Content -Raw -Encoding UTF8 $manifestPath
    $manifest = $manifestText | ConvertFrom-Json
    Add-Pass "manifest.json is valid JSON and UTF-8 readable"

    if ($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$') { Add-Failure "manifest name must match ^[A-Za-z0-9_]{1,128}$" }
    if ($manifest.description.Length -gt 250) { Add-Failure "manifest description exceeds 250 characters" }
    if ($manifest.version_number -notmatch '^\d+\.\d+\.\d+$') { Add-Failure "version_number must be Major.Minor.Patch" }
    if ($null -eq $manifest.website_url) { Add-Failure "website_url is required (an empty string is allowed)" }
    foreach ($dependency in $manifest.dependencies) {
        if ($dependency -notmatch '^.+-.+-\d+\.\d+\.\d+$') { Add-Failure "Invalid dependency string: $dependency" }
    }
} catch {
    Add-Failure "manifest.json validation failed: $($_.Exception.Message)"
}

$packageDir = Join-Path $PSScriptRoot "..\package"
foreach ($required in @("icon.png", "README.md", "manifest.json")) {
    if (Test-Path (Join-Path $packageDir $required)) { Add-Pass "Package root contains $required" } else { Add-Failure "Package root is missing $required" }
}

$iconPath = Join-Path $packageDir "icon.png"
try {
    Add-Type -AssemblyName System.Drawing
    $icon = [System.Drawing.Image]::FromFile($iconPath)
    try {
        if ($icon.Width -eq 256 -and $icon.Height -eq 256 -and $icon.RawFormat.Guid -eq [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
            Add-Pass "icon.png is a 256x256 PNG"
        } else {
            Add-Failure "icon.png must be exactly 256x256 PNG; found $($icon.Width)x$($icon.Height)"
        }
    } finally {
        $icon.Dispose()
    }
} catch {
    Add-Failure "Unable to inspect icon.png: $($_.Exception.Message)"
}

foreach ($readme in @((Join-Path $PSScriptRoot "..\README.md"), (Join-Path $packageDir "README.md"))) {
    try {
        $bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $readme))
        $utf8 = New-Object System.Text.UTF8Encoding($false, $true)
        $null = $utf8.GetString($bytes)
        Add-Pass "UTF-8 README: $readme"
    } catch {
        Add-Failure "README is not valid UTF-8: $readme"
    }
}

$ilSpy = Join-Path $env:LOCALAPPDATA "Programs\ILSpy\ILSpy.exe"
if (Test-Path $ilSpy) { Add-Pass "ILSpy: $ilSpy" } else { Add-Warning "ILSpy executable not found at the normal user install path" }
$monoMod = Join-Path $env:LOCALAPPDATA "Programs\MonoMod\22.07.31.01-net452\MonoMod.DebugIL.exe"
if (Test-Path $monoMod) { Add-Pass "MonoMod DebugIL: $monoMod" } else { Add-Warning "MonoMod DebugIL is not installed" }
$dnSpy = Join-Path $env:LOCALAPPDATA "Programs\dnSpyEx\6.6.0\dnSpy.exe"
if (Test-Path $dnSpy) { Add-Pass "dnSpyEx: $dnSpy" } else { Add-Warning "dnSpyEx is optional and not installed" }

Write-Host ""
Write-Host "Failures: $($failures.Count); warnings: $($warnings.Count)"
if ($failures.Count -gt 0) {
    exit 1
}
