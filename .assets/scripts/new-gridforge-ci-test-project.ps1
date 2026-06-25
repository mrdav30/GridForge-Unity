<#
.SYNOPSIS
Creates the temporary Unity project used by GridForge package CI.

.DESCRIPTION
Builds a minimal Unity project that imports one GridForge package variant,
copies the shared EditMode tests, and writes a variant-specific test asmdef.
Dependency URLs and versions are read from unity-package-versions.json so CI
uses the same package graph as the bootstrap installer.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$PackageName,

    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [string]$ConfigPath,

    [string]$UnityVersion = "6000.5.0f1",

    [string]$UnityVersionWithRevision = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-PackagesRoot {
    $assetsRoot = Split-Path -Parent $PSScriptRoot
    return [System.IO.Path]::GetFullPath((Split-Path -Parent $assetsRoot))
}

function Resolve-UnderRoot {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Description
    )

    $candidate = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $Root $Path))
    }

    $normalizedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar
    ) + [System.IO.Path]::DirectorySeparatorChar

    if (-not $candidate.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description must resolve under the package repository root. Got: $candidate"
    }

    return $candidate
}

function Read-JsonFile {
    param([string]$Path)

    try {
        return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        throw "Failed to parse JSON file '$Path': $($_.Exception.Message)"
    }
}

function Get-RequiredString {
    param(
        [object]$Object,
        [string]$PropertyName,
        [string]$Context
    )

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or $null -eq $property.Value -or [string]::IsNullOrWhiteSpace([string]$property.Value)) {
        throw "$Context must define '$PropertyName'."
    }

    return [string]$property.Value
}

function Get-VariantSettings {
    param([string]$Name)

    if ($Name.EndsWith(".lean", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [pscustomobject]@{
            RuntimeAssembly = "GridForge.Lean.Runtime"
            FixedMathAssembly = "FixedMathSharp.Lean.Runtime"
            SwiftCollectionsAssembly = "SwiftCollections.Lean.Runtime"
            TestVariantDefine = "GRIDFORGE_TEST_LEAN_ONLY"
            FixedMathPackage = "com.mrdav30.fixedmathsharp.lean"
            SwiftCollectionsPackage = "com.mrdav30.swiftcollections.lean"
        }
    }

    return [pscustomobject]@{
        RuntimeAssembly = "GridForge.Runtime"
        FixedMathAssembly = "FixedMathSharp.Runtime"
        SwiftCollectionsAssembly = "SwiftCollections.Runtime"
        TestVariantDefine = "GRIDFORGE_TEST_STANDARD_ONLY"
        FixedMathPackage = "com.mrdav30.fixedmathsharp"
        SwiftCollectionsPackage = "com.mrdav30.swiftcollections"
    }
}

function ConvertTo-OrderedDependencyMap {
    param(
        [object]$PackageConfig,
        [hashtable]$DependencyVersions,
        [string]$LocalPackageName,
        [string]$LocalPackagePath
    )

    $dependencies = [ordered]@{}
    $dependencies[$LocalPackageName] = "file:../../../$LocalPackagePath"

    foreach ($dependency in @($PackageConfig.dependencies)) {
        $name = Get-RequiredString -Object $dependency -PropertyName "name" -Context "Dependency"
        $gitUrl = Get-RequiredString -Object $dependency -PropertyName "gitUrl" -Context "Dependency '$name'"
        $versionKey = Get-RequiredString -Object $dependency -PropertyName "versionKey" -Context "Dependency '$name'"

        if (-not $DependencyVersions.ContainsKey($versionKey)) {
            throw "Dependency '$name' uses unknown versionKey '$versionKey'."
        }

        $version = [string]$DependencyVersions[$versionKey]
        if ([string]::IsNullOrWhiteSpace($version)) {
            throw "dependencyVersions.$versionKey must be non-empty."
        }

        $dependencies[$name] = "$gitUrl#$version"
    }

    $dependencies["com.unity.modules.imgui"] = "1.0.0"
    $dependencies["com.unity.modules.physics"] = "1.0.0"
    $dependencies["com.unity.test-framework"] = "1.6.0"

    return $dependencies
}

$packagesRoot = Get-PackagesRoot
if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path (Split-Path -Parent $PSScriptRoot) "unity-package-versions.json"
}

$resolvedProjectPath = Resolve-UnderRoot -Root $packagesRoot -Path $ProjectPath -Description "ProjectPath"
$resolvedConfigPath = if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    [System.IO.Path]::GetFullPath($ConfigPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $packagesRoot $ConfigPath))
}

if (Test-Path -LiteralPath $resolvedProjectPath) {
    Remove-Item -LiteralPath $resolvedProjectPath -Recurse -Force
}

$assetsTestsPath = Join-Path $resolvedProjectPath "Assets/Tests/EditMode"
$packagesPath = Join-Path $resolvedProjectPath "Packages"
$projectSettingsPath = Join-Path $resolvedProjectPath "ProjectSettings"
New-Item -ItemType Directory -Path $assetsTestsPath, $packagesPath, $projectSettingsPath -Force | Out-Null

$testsRoot = Join-Path $packagesRoot "Tests/EditMode"
$testFiles = @(Get-ChildItem -LiteralPath $testsRoot -Filter "*.cs" -File)
if ($testFiles.Count -eq 0) {
    throw "No EditMode test sources found under Tests/EditMode."
}

Copy-Item -LiteralPath $testFiles.FullName -Destination $assetsTestsPath

$config = Read-JsonFile -Path $resolvedConfigPath
$packageConfig = @($config.packages | Where-Object { [string]$_.path -eq $PackagePath }) | Select-Object -First 1
if ($null -eq $packageConfig) {
    throw "Could not find package path '$PackagePath' in '$resolvedConfigPath'."
}

$dependencyVersions = @{}
foreach ($property in $config.dependencyVersions.PSObject.Properties) {
    $dependencyVersions[$property.Name] = [string]$property.Value
}

$variant = Get-VariantSettings -Name $PackageName
$manifestDependencies = ConvertTo-OrderedDependencyMap `
    -PackageConfig $packageConfig `
    -DependencyVersions $dependencyVersions `
    -LocalPackageName $PackageName `
    -LocalPackagePath $PackagePath

if (-not $manifestDependencies.Contains($variant.FixedMathPackage)) {
    throw "Package '$PackageName' dependencies must include '$($variant.FixedMathPackage)'."
}

if (-not $manifestDependencies.Contains($variant.SwiftCollectionsPackage)) {
    throw "Package '$PackageName' dependencies must include '$($variant.SwiftCollectionsPackage)'."
}

$manifest = [ordered]@{
    dependencies = $manifestDependencies
}

$manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $packagesPath "manifest.json") -Encoding UTF8

$asmdef = [ordered]@{
    name = "GridForge.Unity.Tests.EditMode"
    references = @(
        $variant.RuntimeAssembly,
        $variant.FixedMathAssembly,
        $variant.SwiftCollectionsAssembly,
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    )
    includePlatforms = @("Editor")
    excludePlatforms = @()
    allowUnsafeCode = $false
    overrideReferences = $true
    precompiledReferences = @(
        "nunit.framework.dll",
        "GridForge.dll",
        "FixedMathSharp.dll",
        "Chronicler.dll",
        "SwiftCollections.dll"
    )
    autoReferenced = $false
    defineConstraints = @(
        "UNITY_INCLUDE_TESTS",
        "GRIDFORGE_CI_HAS_FIXEDMATHSHARP",
        "GRIDFORGE_CI_HAS_SWIFTCOLLECTIONS"
    )
    versionDefines = @(
        [ordered]@{
            name = $PackageName
            expression = [string]$config.packageVersion
            define = $variant.TestVariantDefine
        },
        [ordered]@{
            name = $variant.FixedMathPackage
            expression = "5.0.0"
            define = "GRIDFORGE_CI_HAS_FIXEDMATHSHARP"
        },
        [ordered]@{
            name = $variant.SwiftCollectionsPackage
            expression = "5.0.0"
            define = "GRIDFORGE_CI_HAS_SWIFTCOLLECTIONS"
        }
    )
}

$asmdef |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $assetsTestsPath "GridForge.Unity.Tests.EditMode.asmdef") -Encoding UTF8

$projectVersionLines = @("m_EditorVersion: $UnityVersion")
if (-not [string]::IsNullOrWhiteSpace($UnityVersionWithRevision)) {
    $projectVersionLines += "m_EditorVersionWithRevision: $UnityVersionWithRevision"
}

$projectVersionLines | Set-Content -LiteralPath (Join-Path $projectSettingsPath "ProjectVersion.txt") -Encoding UTF8

Write-Output "Created GridForge CI Unity project at $resolvedProjectPath for $PackageName."
