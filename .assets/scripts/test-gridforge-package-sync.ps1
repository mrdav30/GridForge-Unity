Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$AssetsRoot = Split-Path -Parent $PSScriptRoot
$PackageRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $AssetsRoot))
$BaseRoot = Join-Path $PackageRoot "Build/Base"

$PackageRoots = @(
    "com.mrdav30.gridforge",
    "com.mrdav30.gridforge.lean"
)

$ManagedEntries = @(
    "COPYRIGHT",
    "LICENSE",
    "NOTICE",
    "Editor/Blockers",
    "Editor/Configuration",
    "Editor/Utility/Debugging",
    "Runtime/Blockers",
    "Runtime/Configuration",
    "Runtime/GridWorldComponent.cs",
    "Runtime/Utility/Debugging",
    "Samples/GridforgeDemo/Scripts"
)

function Join-NormalizedPath {
    param(
        [string]$Left,
        [string]$Right
    )

    return [System.IO.Path]::GetFullPath((Join-Path $Left ($Right -replace '/', [System.IO.Path]::DirectorySeparatorChar)))
}

function Get-RelativeManagedFiles {
    param([string]$Root)

    $files = @{}

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        return $files
    }

    foreach ($file in Get-ChildItem -LiteralPath $Root -Recurse -File) {
        if ($file.Name.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $relative = [System.IO.Path]::GetRelativePath($Root, $file.FullName).Replace("\", "/")
        $files[$relative] = $file.FullName
    }

    return $files
}

function Test-ManagedFileMatch {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [System.Collections.Generic.List[string]]$Issues
    )

    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        $Issues.Add("Missing source file: $SourcePath")
        return
    }

    if (-not (Test-Path -LiteralPath $DestinationPath -PathType Leaf)) {
        $Issues.Add("Missing package file: $DestinationPath")
        return
    }

    $sourceHash = (Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash
    $destinationHash = (Get-FileHash -LiteralPath $DestinationPath -Algorithm SHA256).Hash

    if ($sourceHash -ne $destinationHash) {
        $Issues.Add("Out-of-sync package file: $DestinationPath")
    }
}

function Test-ManagedDirectoryMatch {
    param(
        [string]$SourceRoot,
        [string]$DestinationRoot,
        [System.Collections.Generic.List[string]]$Issues
    )

    $sourceFiles = Get-RelativeManagedFiles -Root $SourceRoot
    $destinationFiles = Get-RelativeManagedFiles -Root $DestinationRoot

    foreach ($relativePath in $sourceFiles.Keys) {
        if (-not $destinationFiles.ContainsKey($relativePath)) {
            $Issues.Add("Missing package file: $(Join-Path $DestinationRoot $relativePath)")
            continue
        }

        Test-ManagedFileMatch `
            -SourcePath $sourceFiles[$relativePath] `
            -DestinationPath $destinationFiles[$relativePath] `
            -Issues $Issues
    }

    foreach ($relativePath in $destinationFiles.Keys) {
        if ($sourceFiles.ContainsKey($relativePath)) {
            continue
        }

        $Issues.Add("Extra package file: $($destinationFiles[$relativePath])")
    }
}

$issues = [System.Collections.Generic.List[string]]::new()

foreach ($packageRootName in $PackageRoots) {
    $variantRoot = Join-Path $PackageRoot $packageRootName

    if (-not (Test-Path -LiteralPath $variantRoot -PathType Container)) {
        $issues.Add("Missing package root: $variantRoot")
        continue
    }

    foreach ($entry in $ManagedEntries) {
        $sourcePath = Join-NormalizedPath -Left $BaseRoot -Right $entry
        $destinationPath = Join-NormalizedPath -Left $variantRoot -Right $entry

        if (Test-Path -LiteralPath $sourcePath -PathType Leaf) {
            Test-ManagedFileMatch -SourcePath $sourcePath -DestinationPath $destinationPath -Issues $issues
            continue
        }

        Test-ManagedDirectoryMatch -SourceRoot $sourcePath -DestinationRoot $destinationPath -Issues $issues
    }
}

if ($issues.Count -gt 0) {
    foreach ($issue in $issues) {
        Write-Error $issue -ErrorAction Continue
    }

    throw "Package sync validation failed with $($issues.Count) issue(s)."
}

Write-Output "Package sync validation passed. Build/Base managed files match both package variants."
