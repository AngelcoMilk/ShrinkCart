[CmdletBinding()]
param(
    [switch]$InstallDnSpyEx,
    [switch]$InstallAvaloniaILSpy,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$programs = Join-Path $env:LOCALAPPDATA "Programs"
$downloadDir = Join-Path $env:TEMP "ShrinkCart-tools"
New-Item -ItemType Directory -Force -Path $programs, $downloadDir | Out-Null

function Invoke-Download {
    param(
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if ((Test-Path $Destination) -and !$Force) {
        Write-Host "Using existing download: $Destination"
        return
    }

    Write-Host "Downloading $Uri"
    Invoke-WebRequest -UseBasicParsing -Uri $Uri -OutFile $Destination
}

function Expand-ToolArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Archive,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if ((Test-Path $Destination) -and $Force) {
        Remove-Item -Recurse -Force $Destination
    }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Expand-Archive -LiteralPath $Archive -DestinationPath $Destination -Force
}

if (!(Get-Command winget.exe -ErrorAction SilentlyContinue)) {
    throw "winget.exe is required to install Windows ILSpy."
}

Write-Host "Installing/updating ILSpy from the official winget package..."
& winget.exe install --exact --id icsharpcode.ILSpy --accept-package-agreements --accept-source-agreements --disable-interactivity
if ($LASTEXITCODE -ne 0) {
    # winget can return a non-zero result if the package is already current. Verify instead of blindly failing.
    & winget.exe list --exact --id icsharpcode.ILSpy --accept-source-agreements | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "ILSpy installation could not be verified."
    }
}

# MonoMod's latest standalone release is old but remains useful for DebugIL/HookGen and
# inspecting IL. Do not run MonoMod.exe against the game's original assemblies in place.
$monoModVersion = "22.07.31.01"
$monoModFlavor = "net452"
$monoModArchive = Join-Path $downloadDir "MonoMod-$monoModVersion-$monoModFlavor.zip"
$monoModDestination = Join-Path $programs "MonoMod\$monoModVersion-$monoModFlavor"
Invoke-Download `
    -Uri "https://github.com/MonoMod/MonoMod/releases/download/v$monoModVersion/MonoMod-$monoModVersion-$monoModFlavor.zip" `
    -Destination $monoModArchive
Expand-ToolArchive -Archive $monoModArchive -Destination $monoModDestination

if ($InstallDnSpyEx) {
    $dnSpyVersion = "6.6.0"
    $dnSpyArchive = Join-Path $downloadDir "dnSpy-net-win64-v$dnSpyVersion.zip"
    $dnSpyDestination = Join-Path $programs "dnSpyEx\$dnSpyVersion"
    Invoke-Download `
        -Uri "https://github.com/dnSpyEx/dnSpy/releases/download/v$dnSpyVersion/dnSpy-net-win64.zip" `
        -Destination $dnSpyArchive
    Expand-ToolArchive -Archive $dnSpyArchive -Destination $dnSpyDestination
}

if ($InstallAvaloniaILSpy) {
    $avaloniaVersion = "7.2-rc"
    $avaloniaArchive = Join-Path $downloadDir "AvaloniaILSpy-Windows.x64-$avaloniaVersion.zip"
    $avaloniaDestination = Join-Path $programs "AvaloniaILSpy\$avaloniaVersion"
    Invoke-Download `
        -Uri "https://github.com/icsharpcode/AvaloniaILSpy/releases/download/v$avaloniaVersion/Windows.x64.Release.zip" `
        -Destination $avaloniaArchive
    Expand-ToolArchive -Archive $avaloniaArchive -Destination $avaloniaDestination
}

Write-Host ""
Write-Host "Tool verification"
$ilSpy = Join-Path $programs "ILSpy\ILSpy.exe"
Write-Host ("ILSpy:      " + $(if (Test-Path $ilSpy) { $ilSpy } else { "installed by winget; locate with 'winget list --id icsharpcode.ILSpy'" }))
Write-Host ("MonoMod:    " + (Join-Path $monoModDestination "MonoMod.exe"))
if ($InstallDnSpyEx) {
    Write-Host ("dnSpyEx:     " + (Join-Path $programs "dnSpyEx\6.6.0\dnSpy.exe"))
}
if ($InstallAvaloniaILSpy) {
    Write-Host ("AvaloniaILSpy: " + (Join-Path $programs "AvaloniaILSpy\7.2-rc"))
}
Write-Host ""
Write-Host "Open <GameDir>\REPO_Data\Managed\Assembly-CSharp.dll read-only for analysis."
Write-Host "Never commit game DLLs, decompiled source, patched assemblies, or ripped assets."
