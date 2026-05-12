param(
    [string]$Configuration = "Debug",
    [string]$GameDir = "D:\SteamLibrary\steamapps\common\REPO",
    [string]$R2Profile = "$env:APPDATA\r2modmanPlus-local\REPO\profiles\REPO",
    [string]$ScalerCoreDll = "",
    [string]$REPOConfigDll = "",
    [switch]$InstallToProfile,
    [switch]$PackageToDesktop
)

$ErrorActionPreference = "Stop"

$modName = "ShrinkCart"
$modVersion = "0.2.7"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src\ShrinkCart"
$distRoot = Join-Path $root "dist"
$outDir = Join-Path $distRoot "BepInEx\plugins\$modName"
$managed = Join-Path $GameDir "REPO_Data\Managed"
$bepCore = Join-Path $R2Profile "BepInEx\core"
$pluginOut = Join-Path $outDir "$modName.dll"
$desktopZip = Join-Path ([Environment]::GetFolderPath("Desktop")) "$modName-$modVersion.zip"

if (!(Test-Path (Join-Path $managed "Assembly-CSharp.dll"))) {
    throw "Assembly-CSharp.dll not found under $managed"
}
if (!(Test-Path (Join-Path $bepCore "BepInEx.dll"))) {
    throw "BepInEx core not found under $bepCore"
}
if (!(Test-Path $src)) {
    throw "Source directory not found at $src"
}

if ([string]::IsNullOrWhiteSpace($ScalerCoreDll)) {
    $candidate = Get-ChildItem -LiteralPath "$env:APPDATA\r2modmanPlus-local\REPO\cache" -Recurse -Filter "ScalerCore.dll" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($candidate -ne $null) {
        $ScalerCoreDll = $candidate.FullName
    }
}

if (!(Test-Path $ScalerCoreDll)) {
    throw "ScalerCore.dll not found. Install Vippy-ScalerCore in the REPO r2modman profile, or pass -ScalerCoreDll <path>."
}

if ([string]::IsNullOrWhiteSpace($REPOConfigDll)) {
    $candidate = Get-ChildItem -LiteralPath "$env:APPDATA\r2modmanPlus-local\REPO\cache" -Recurse -Filter "REPOConfig.dll" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($candidate -ne $null) {
        $REPOConfigDll = $candidate.FullName
    }
}

if (!(Test-Path $REPOConfigDll)) {
    throw "REPOConfig.dll not found. Install nickklmao-REPOConfig in the REPO r2modman profile, or pass -REPOConfigDll <path>."
}

if (Test-Path $distRoot) {
    $resolvedDist = (Resolve-Path -LiteralPath $distRoot).Path
    $resolvedRoot = (Resolve-Path -LiteralPath $root).Path
    if (!$resolvedDist.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean dist outside workspace: $resolvedDist"
    }

    Remove-Item -LiteralPath $distRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (!(Test-Path $csc)) {
    throw "No .NET Framework csc.exe found."
}

$sources = Get-ChildItem -LiteralPath $src -Filter "*.cs" -Recurse | ForEach-Object { $_.FullName }

$refs = @(
    (Join-Path $bepCore "BepInEx.dll"),
    (Join-Path $bepCore "0Harmony.dll"),
    (Join-Path $managed "Assembly-CSharp.dll"),
    (Join-Path $managed "UnityEngine.dll"),
    (Join-Path $managed "UnityEngine.CoreModule.dll"),
    (Join-Path $managed "UnityEngine.IMGUIModule.dll"),
    (Join-Path $managed "UnityEngine.InputLegacyModule.dll"),
    (Join-Path $managed "UnityEngine.PhysicsModule.dll"),
    (Join-Path $managed "PhotonUnityNetworking.dll"),
    (Join-Path $managed "PhotonRealtime.dll"),
    (Join-Path $managed "Photon3Unity3D.dll"),
    (Join-Path $managed "netstandard.dll"),
    $ScalerCoreDll,
    $REPOConfigDll
)

$refArgs = $refs | ForEach-Object { "/reference:$_" }

function Test-GameHookTargets {
    param(
        [string]$AssemblyPath,
        [string]$CecilPath
    )

    if (!(Test-Path $CecilPath)) {
        Write-Warning "Mono.Cecil.dll not found; skipping Harmony hook target validation."
        return
    }

    Add-Type -Path $CecilPath
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
    $targets = @(
        @{ Type = "PhysGrabInCart"; Method = "Add"; Parameters = @("PhysGrabObject") },
        @{ Type = "ItemEquippable"; Method = "IsEquipped"; Parameters = @() },
        @{ Type = "HurtCollider"; Method = "PlayerHurt"; Parameters = @("PlayerAvatar") },
        @{ Type = "EnemyHealth"; Method = "Hurt"; Parameters = @("System.Int32", "UnityEngine.Vector3") },
        @{ Type = "RunManager"; Method = "ChangeLevel"; Parameters = @("System.Boolean", "System.Boolean", "RunManager/ChangeLevelType") }
    )
    $requiredFields = @(
        @{ Type = "PhysGrabInCart"; Field = "cart" },
        @{ Type = "HurtCollider"; Field = "playerKill" },
        @{ Type = "HurtCollider"; Field = "playerLogic" },
        @{ Type = "EnemyRigidbody"; Field = "enemy" },
        @{ Type = "Enemy"; Field = "Health" },
        @{ Type = "EnemyHealth"; Field = "healthCurrent" },
        @{ Type = "ItemAttributes"; Field = "itemType" },
        @{ Type = "PhysGrabObject"; Field = "isGun" }
    )

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($target in $targets) {
        $type = $assembly.MainModule.Types | Where-Object { $_.Name -eq $target.Type -or $_.FullName -eq $target.Type } | Select-Object -First 1
        if ($null -eq $type) {
            $errors.Add("Missing type: $($target.Type)")
            continue
        }

        $methods = @($type.Methods | Where-Object { $_.Name -eq $target.Method })
        if ($methods.Count -eq 0) {
            $errors.Add("Missing method: $($target.Type).$($target.Method)")
            continue
        }

        $expectedParameters = @($target.Parameters)
        $hasCompatibleSignature = $false
        foreach ($method in $methods) {
            if ($method.Parameters.Count -ne $expectedParameters.Count) {
                continue
            }

            $hasCompatibleSignature = $true
            break
        }

        if (!$hasCompatibleSignature) {
            $errors.Add("Incompatible signature: $($target.Type).$($target.Method)($($expectedParameters -join ', '))")
        }
    }

    foreach ($requiredField in $requiredFields) {
        $type = $assembly.MainModule.Types | Where-Object { $_.Name -eq $requiredField.Type -or $_.FullName -eq $requiredField.Type } | Select-Object -First 1
        if ($null -eq $type) {
            $errors.Add("Missing field owner type: $($requiredField.Type)")
            continue
        }

        $field = $type.Fields | Where-Object { $_.Name -eq $requiredField.Field } | Select-Object -First 1
        if ($null -eq $field) {
            $errors.Add("Missing field: $($requiredField.Type).$($requiredField.Field)")
        }
    }

    if ($errors.Count -gt 0) {
        throw "Harmony hook target validation failed:`n$($errors -join "`n")"
    }

    Write-Host "Validated Harmony hook targets against $AssemblyPath"
}

function Test-ScalerCoreHookTargets {
    param(
        [string]$AssemblyPath,
        [string]$CecilPath
    )

    if (!(Test-Path $CecilPath)) {
        Write-Warning "Mono.Cecil.dll not found; skipping ScalerCore hook target validation."
        return
    }

    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($AssemblyPath)
    $targets = @(
        @{ Type = "ScalerCore.ScaleManager"; Method = "ApplyIfNotScaled"; Parameters = @("UnityEngine.GameObject", "ScalerCore.ScaleOptions") },
        @{ Type = "ScalerCore.ScaleManager"; Method = "Restore"; Parameters = @("UnityEngine.GameObject") },
        @{ Type = "ScalerCore.ScaleManager"; Method = "IsScaled"; Parameters = @("UnityEngine.GameObject") },
        @{ Type = "ScalerCore.ScaleManager"; Method = "GetController"; Parameters = @("UnityEngine.GameObject") }
    )
    $requiredFields = @(
        @{ Type = "ScalerCore.ScaleController"; Field = "_options" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "Factor" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "Speed" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "RestoreSpeed" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "AllowedTargets" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "SuppressValueDropExpand" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "PreserveMass" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "SuppressImpactFlash" },
        @{ Type = "ScalerCore.ScaleOptions"; Field = "SuppressCameraShake" }
    )

    $errors = New-Object System.Collections.Generic.List[string]
    foreach ($target in $targets) {
        $type = $assembly.MainModule.Types | Where-Object { $_.Name -eq $target.Type -or $_.FullName -eq $target.Type } | Select-Object -First 1
        if ($null -eq $type) {
            $errors.Add("Missing type: $($target.Type)")
            continue
        }

        $methods = @($type.Methods | Where-Object { $_.Name -eq $target.Method })
        if ($methods.Count -eq 0) {
            $errors.Add("Missing method: $($target.Type).$($target.Method)")
            continue
        }

        $expectedParameters = @($target.Parameters)
        $hasCompatibleSignature = $false
        foreach ($method in $methods) {
            if ($method.Parameters.Count -eq $expectedParameters.Count) {
                $hasCompatibleSignature = $true
                break
            }
        }

        if (!$hasCompatibleSignature) {
            $errors.Add("Incompatible signature: $($target.Type).$($target.Method)($($expectedParameters -join ', '))")
        }
    }

    foreach ($requiredField in $requiredFields) {
        $type = $assembly.MainModule.Types | Where-Object { $_.Name -eq $requiredField.Type -or $_.FullName -eq $requiredField.Type } | Select-Object -First 1
        if ($null -eq $type) {
            $errors.Add("Missing field owner type: $($requiredField.Type)")
            continue
        }

        $field = $type.Fields | Where-Object { $_.Name -eq $requiredField.Field } | Select-Object -First 1
        if ($null -eq $field) {
            $errors.Add("Missing field: $($requiredField.Type).$($requiredField.Field)")
        }
    }

    if ($errors.Count -gt 0) {
        throw "ScalerCore hook target validation failed:`n$($errors -join "`n")"
    }

    Write-Host "Validated ScalerCore hook targets against $AssemblyPath"
}

Test-GameHookTargets -AssemblyPath (Join-Path $managed "Assembly-CSharp.dll") -CecilPath (Join-Path $bepCore "Mono.Cecil.dll")
Test-ScalerCoreHookTargets -AssemblyPath $ScalerCoreDll -CecilPath (Join-Path $bepCore "Mono.Cecil.dll")

& $csc /nologo /codepage:65001 /target:library /optimize+ /debug:full /nowarn:1701 /out:$pluginOut $refArgs $sources
if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

Copy-Item -LiteralPath (Join-Path $root "package\manifest.json") -Destination (Join-Path $distRoot "manifest.json") -Force
Copy-Item -LiteralPath (Join-Path $root "package\README.md") -Destination (Join-Path $distRoot "README.md") -Force
Copy-Item -LiteralPath (Join-Path $root "package\icon.png") -Destination (Join-Path $distRoot "icon.png") -Force

$profilePluginDir = Join-Path $R2Profile "BepInEx\plugins\$modName"
if ($InstallToProfile -and (Test-Path (Join-Path $R2Profile "BepInEx"))) {
    New-Item -ItemType Directory -Force -Path $profilePluginDir | Out-Null
    Copy-Item -LiteralPath $pluginOut -Destination (Join-Path $profilePluginDir "$modName.dll") -Force
    Write-Host "Installed to $profilePluginDir"
}

if ($PackageToDesktop) {
    if (Test-Path $desktopZip) {
        Remove-Item -LiteralPath $desktopZip -Force
    }

    $packageStage = Join-Path $root "dist_package"
    if (Test-Path $packageStage) {
        $resolvedStage = (Resolve-Path -LiteralPath $packageStage).Path
        $resolvedRoot = (Resolve-Path -LiteralPath $root).Path
        if (!$resolvedStage.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean package stage outside workspace: $resolvedStage"
        }

        Remove-Item -LiteralPath $packageStage -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $packageStage "BepInEx\plugins\$modName") | Out-Null
    Copy-Item -LiteralPath (Join-Path $distRoot "manifest.json") -Destination (Join-Path $packageStage "manifest.json") -Force
    Copy-Item -LiteralPath (Join-Path $distRoot "README.md") -Destination (Join-Path $packageStage "README.md") -Force
    Copy-Item -LiteralPath (Join-Path $distRoot "icon.png") -Destination (Join-Path $packageStage "icon.png") -Force
    Copy-Item -LiteralPath $pluginOut -Destination (Join-Path $packageStage "BepInEx\plugins\$modName\$modName.dll") -Force

    Compress-Archive -LiteralPath `
        (Join-Path $packageStage "manifest.json"), `
        (Join-Path $packageStage "README.md"), `
        (Join-Path $packageStage "icon.png"), `
        (Join-Path $packageStage "BepInEx") `
        -DestinationPath $desktopZip

    Write-Host "Packaged $desktopZip"
}

Write-Host "Built $pluginOut"
Write-Host "Using ScalerCore: $ScalerCoreDll"
Write-Host "Using REPOConfig: $REPOConfigDll"
