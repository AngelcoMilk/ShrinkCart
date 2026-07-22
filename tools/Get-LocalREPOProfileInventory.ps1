param(
    [string]$ProfileName = "",
    [string]$Root = "$env:APPDATA\r2modmanPlus-local\REPO",
    [switch]$AsJson,
    [switch]$IncludeDisabled
)

$ErrorActionPreference = "Stop"

function Read-R2ModsFile {
    param([Parameter(Mandatory = $true)][string]$Path)

    $items = New-Object System.Collections.Generic.List[object]
    $current = $null
    $section = ""

    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        if ($line -match "^- manifestVersion:\s*(.+)$") {
            if ($null -ne $current) {
                $items.Add([pscustomobject]$current)
            }

            $current = [ordered]@{
                Name = ""
                DisplayName = ""
                Version = ""
                Enabled = $false
                WebsiteUrl = ""
                Dependencies = New-Object System.Collections.Generic.List[string]
            }
            $section = ""
            continue
        }

        if ($null -eq $current) { continue }

        if ($line -match "^  name:\s*(.*)$") { $current.Name = $Matches[1]; continue }
        if ($line -match "^  displayName:\s*(.*)$") { $current.DisplayName = $Matches[1]; continue }
        if ($line -match "^  websiteUrl:\s*(.*)$") { $current.WebsiteUrl = $Matches[1]; continue }
        if ($line -match "^  enabled:\s*(true|false)$") { $current.Enabled = [bool]::Parse($Matches[1]); continue }
        if ($line -match "^  dependencies:\s*$") { $section = "dependencies"; continue }
        if ($line -match "^  versionNumber:\s*$") { $section = "version"; continue }

        if ($section -eq "dependencies" -and $line -match "^    -\s*(.+)$") {
            $current.Dependencies.Add($Matches[1])
            continue
        }

        if ($section -eq "version" -and $line -match "^    major:\s*(\d+)$") { $major = $Matches[1]; continue }
        if ($section -eq "version" -and $line -match "^    minor:\s*(\d+)$") { $minor = $Matches[1]; continue }
        if ($section -eq "version" -and $line -match "^    patch:\s*(\d+)$") {
            $current.Version = "$major.$minor.$($Matches[1])"
            continue
        }

        if ($line -match "^  [A-Za-z].*:") { $section = "" }
    }

    if ($null -ne $current) {
        $items.Add([pscustomobject]$current)
    }

    return $items
}

$profilesRoot = Join-Path $Root "profiles"
if (!(Test-Path -LiteralPath $profilesRoot -PathType Container)) {
    throw "R.E.P.O. r2modman profile root not found: $profilesRoot"
}

$profileDirs = if ([string]::IsNullOrWhiteSpace($ProfileName)) {
    @(Get-ChildItem -LiteralPath $profilesRoot -Directory | Sort-Object Name)
} else {
    $requested = Join-Path $profilesRoot $ProfileName
    if (!(Test-Path -LiteralPath $requested -PathType Container)) {
        throw "Profile not found: $ProfileName"
    }
    @((Get-Item -LiteralPath $requested))
}

$result = foreach ($profile in $profileDirs) {
    $modsPath = Join-Path $profile.FullName "mods.yml"
    if (!(Test-Path -LiteralPath $modsPath -PathType Leaf)) {
        Write-Warning "Skipping profile without mods.yml: $($profile.Name)"
        continue
    }

    $packages = @(Read-R2ModsFile -Path $modsPath)
    if (!$IncludeDisabled) {
        $packages = @($packages | Where-Object Enabled)
    }

    [pscustomobject]@{
        Profile = $profile.Name
        TotalRecorded = @(Read-R2ModsFile -Path $modsPath).Count
        EnabledRecorded = @((Read-R2ModsFile -Path $modsPath) | Where-Object Enabled).Count
        Packages = @($packages | Sort-Object Name | Select-Object Name, DisplayName, Version, Enabled, Dependencies, WebsiteUrl)
    }
}

if ($AsJson) {
    $result | ConvertTo-Json -Depth 6
    exit 0
}

foreach ($profile in $result) {
    Write-Host "Profile: $($profile.Profile) ($($profile.EnabledRecorded)/$($profile.TotalRecorded) enabled)"
    $profile.Packages | Select-Object Name, Version, Enabled | Format-Table -AutoSize
}

Write-Host "Source: local mods.yml metadata only; cache contents are not treated as installed/enabled packages."
