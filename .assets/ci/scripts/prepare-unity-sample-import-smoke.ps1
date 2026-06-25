<#
.SYNOPSIS
Creates the temporary Unity project used by GridForge sample import smoke CI.

.DESCRIPTION
Builds a minimal Unity project that installs one GridForge package variant from
a local Git URL, installs the configured package dependencies, imports the Demo
Scene sample, and runs a tiny EditMode assertion against the imported sample.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [Parameter(Mandatory = $true)]
    [string]$PackageName,

    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedSampleAsmdef,

    [Parameter(Mandatory = $true)]
    [string]$WorkspacePath,

    [Parameter(Mandatory = $true)]
    [string]$GitSha,

    [string]$ConfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-PackagesRoot {
    $ciRoot = Split-Path -Parent $PSScriptRoot
    $assetsRoot = Split-Path -Parent $ciRoot
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

function Write-Utf8NoBomFile {
    param(
        [string]$Path,
        [AllowEmptyString()]
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function ConvertTo-JsonText {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [object]$InputObject,

        [int]$Depth = 2
    )

    process {
        $json = $InputObject | ConvertTo-Json -Depth $Depth
        return ($json -join [System.Environment]::NewLine) + [System.Environment]::NewLine
    }
}

function ConvertTo-GitFilePackageUrl {
    param(
        [string]$Path,
        [string]$PackageSubPath,
        [string]$Sha
    )

    $normalizedPath = $Path.Replace('\', '/')
    if ($normalizedPath -match '^[A-Za-z]:/') {
        return "git+file:///$normalizedPath`?path=/$PackageSubPath#$Sha"
    }

    if ($normalizedPath.StartsWith("/", [System.StringComparison]::Ordinal)) {
        return "git+file://$normalizedPath`?path=/$PackageSubPath#$Sha"
    }

    return "git+file:///$normalizedPath`?path=/$PackageSubPath#$Sha"
}

function New-ManifestDependencies {
    param(
        [object]$PackageConfig,
        [hashtable]$DependencyVersions,
        [string]$LocalPackageName,
        [string]$LocalPackageUrl
    )

    $dependencies = [ordered]@{}
    $dependencies[$LocalPackageName] = $LocalPackageUrl

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
    $ConfigPath = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "unity-package-versions.json"
}

$resolvedProjectPath = Resolve-UnderRoot -Root $packagesRoot -Path $ProjectPath -Description "ProjectPath"
$templateRoot = Join-Path $packagesRoot ".assets/ci/unity-sample-import-smoke"

if (-not (Test-Path -LiteralPath $templateRoot -PathType Container)) {
    throw "Unity sample import smoke template was not found: $templateRoot"
}

$resolvedConfigPath = if ([System.IO.Path]::IsPathRooted($ConfigPath)) {
    [System.IO.Path]::GetFullPath($ConfigPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $packagesRoot $ConfigPath))
}

if (Test-Path -LiteralPath $resolvedProjectPath) {
    Remove-Item -LiteralPath $resolvedProjectPath -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedProjectPath -Force | Out-Null
Get-ChildItem -LiteralPath $templateRoot -Force |
    Copy-Item -Destination $resolvedProjectPath -Recurse -Force

$config = Read-JsonFile -Path $resolvedConfigPath
$packageConfig = @($config.packages | Where-Object { [string]$_.path -eq $PackagePath }) | Select-Object -First 1
if ($null -eq $packageConfig) {
    throw "Could not find package path '$PackagePath' in '$resolvedConfigPath'."
}

$dependencyVersions = @{}
foreach ($property in $config.dependencyVersions.PSObject.Properties) {
    $dependencyVersions[$property.Name] = [string]$property.Value
}

$packageUrl = ConvertTo-GitFilePackageUrl -Path $WorkspacePath -PackageSubPath $PackagePath -Sha $GitSha
$manifest = [ordered]@{
    dependencies = New-ManifestDependencies `
        -PackageConfig $packageConfig `
        -DependencyVersions $dependencyVersions `
        -LocalPackageName $PackageName `
        -LocalPackageUrl $packageUrl
}

Write-Utf8NoBomFile `
    -Path (Join-Path $resolvedProjectPath "Packages/manifest.json") `
    -Content ($manifest | ConvertTo-JsonText -Depth 8)

$smokeConfig = [ordered]@{
    packageName = $PackageName
    expectedSampleAsmdef = $ExpectedSampleAsmdef
}

Write-Utf8NoBomFile `
    -Path (Join-Path $resolvedProjectPath "Assets/GridForgeSampleImportSmokeConfig.json") `
    -Content ($smokeConfig | ConvertTo-JsonText -Depth 4)

Write-Output "Created GridForge sample import smoke project at $resolvedProjectPath for $PackageName."
