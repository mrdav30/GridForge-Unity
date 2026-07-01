Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptUnderTest = Join-Path $PSScriptRoot "update-unity-package-versions.ps1"
$TestRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("gridforge-versioning-tests-" + [System.Guid]::NewGuid().ToString("N"))

function Assert-Equal {
    param(
        [object]$Expected,
        [object]$Actual,
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        throw "$Message Expected '$Expected', got '$Actual'."
    }
}

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Expected,
        [string]$Message
    )

    if (-not $Text.Contains($Expected)) {
        throw "$Message Expected text to contain '$Expected'. Actual text: $Text"
    }
}

function New-TestFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    Set-Content -Path $Path -Value $Content -Encoding UTF8
}

function New-TestRepo {
    $repoRoot = Join-Path $TestRoot ([System.Guid]::NewGuid().ToString("N"))

    New-TestFile -Path (Join-Path $repoRoot ".assets/unity-package-versions.json") -Content @'
{
  "packageRoot": ".",
  "packageVersion": "4.0.6",
  "dependencyVersions": {
    "FixedMathSharpUnity": "v5.0.0",
    "SwiftCollectionsUnity": "v5.0.0"
  },
  "packages": [
    {
      "path": "com.mrdav30.gridforge",
      "updatePackageVersion": true,
      "installer": "Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs",
      "dependencies": [
        {
          "name": "com.mrdav30.fixedmathsharp",
          "gitUrl": "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp",
          "versionKey": "FixedMathSharpUnity",
          "asmdefVersionDefine": true
        },
        {
          "name": "com.mrdav30.swiftcollections",
          "gitUrl": "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections",
          "versionKey": "SwiftCollectionsUnity",
          "asmdefVersionDefine": true
        }
      ],
      "asmdefs": [
        "Runtime/GridForge.Runtime.asmdef"
      ],
      "optionalAsmdefs": [
        "Samples/GridforgeDemo/GridForge.Samples.asmdef",
        "Samples/GridforgeDemo/Missing.Optional.asmdef"
      ]
    },
    {
      "path": "com.mrdav30.gridforge.lean",
      "updatePackageVersion": true,
      "installer": "Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs",
      "dependencies": [
        {
          "name": "com.mrdav30.fixedmathsharp.lean",
          "gitUrl": "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean",
          "versionKey": "FixedMathSharpUnity",
          "asmdefVersionDefine": true
        },
        {
          "name": "com.mrdav30.swiftcollections.lean",
          "gitUrl": "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean",
          "versionKey": "SwiftCollectionsUnity",
          "asmdefVersionDefine": true
        }
      ],
      "asmdefs": [
        "Runtime/GridForge.Lean.Runtime.asmdef"
      ],
      "optionalAsmdefs": [
        "Samples/GridforgeDemo/GridForge.Lean.Samples.asmdef"
      ]
    }
  ]
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge/package.json") -Content @'
{
    "name": "com.mrdav30.gridforge",
    "version": "4.0.5",
    "displayName": "gridforge"
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/package.json") -Content @'
{
    "name": "com.mrdav30.gridforge.lean",
    "version": "4.0.5",
    "displayName": "gridforge lean"
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs") -Content @'
private static readonly Dependency[] RequiredDependencies =
{
    new(
        "com.mrdav30.fixedmathsharp",
        "https://example.invalid/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp",
        "v4.0.0"
    ),
    new(
        "com.mrdav30.swiftcollections",
        "https://example.invalid/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections",
        "v4.0.0"
    )
};
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs") -Content @'
private static readonly Dependency[] RequiredDependencies =
{
    new(
        "com.mrdav30.fixedmathsharp.lean",
        "https://example.invalid/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean",
        "v4.0.0"
    ),
    new(
        "com.mrdav30.swiftcollections.lean",
        "https://example.invalid/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean",
        "v4.0.0"
    )
};
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Runtime/GridForge.Runtime.asmdef") -Content @'
{
    "name": "GridForge.Runtime",
    "defineConstraints": [
        "GRIDFORGE_HAS_FIXEDMATHSHARP",
        "GRIDFORGE_HAS_SWIFTCOLLECTIONS"
    ],
    "versionDefines": [
        {
            "name": "com.mrdav30.fixedmathsharp",
            "expression": "4.0.0",
            "define": "GRIDFORGE_HAS_FIXEDMATHSHARP"
        },
        {
            "name": "com.mrdav30.swiftcollections",
            "expression": "4.0.0",
            "define": "GRIDFORGE_HAS_SWIFTCOLLECTIONS"
        }
    ]
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Samples/GridforgeDemo/GridForge.Samples.asmdef") -Content @'
{
    "name": "GridForge.Samples",
    "versionDefines": [
        {
            "name": "com.mrdav30.fixedmathsharp",
            "expression": "4.0.0",
            "define": "GRIDFORGE_HAS_FIXEDMATHSHARP"
        },
        {
            "name": "com.mrdav30.swiftcollections",
            "expression": "4.0.0",
            "define": "GRIDFORGE_HAS_SWIFTCOLLECTIONS"
        }
    ]
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Runtime/GridForge.Lean.Runtime.asmdef") -Content @'
{
    "name": "GridForge.Lean.Runtime",
    "defineConstraints": [
        "GRIDFORGE_LEAN_HAS_FIXEDMATHSHARP",
        "GRIDFORGE_LEAN_HAS_SWIFTCOLLECTIONS"
    ],
    "versionDefines": [
        {
            "name": "com.mrdav30.fixedmathsharp.lean",
            "expression": "4.0.0",
            "define": "GRIDFORGE_LEAN_HAS_FIXEDMATHSHARP"
        },
        {
            "name": "com.mrdav30.swiftcollections.lean",
            "expression": "4.0.0",
            "define": "GRIDFORGE_LEAN_HAS_SWIFTCOLLECTIONS"
        }
    ]
}
'@

    New-TestFile -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Samples/GridforgeDemo/GridForge.Lean.Samples.asmdef") -Content @'
{
    "name": "GridForge.Lean.Samples",
    "versionDefines": [
        {
            "name": "com.mrdav30.fixedmathsharp.lean",
            "expression": "4.0.0",
            "define": "GRIDFORGE_LEAN_HAS_FIXEDMATHSHARP"
        },
        {
            "name": "com.mrdav30.swiftcollections.lean",
            "expression": "4.0.0",
            "define": "GRIDFORGE_LEAN_HAS_SWIFTCOLLECTIONS"
        }
    ]
}
'@

    return $repoRoot
}

function Invoke-Updater {
    param(
        [string]$RepoRoot,
        [switch]$Apply,
        [switch]$ValidateOnly
    )

    $configPath = Join-Path $RepoRoot ".assets/unity-package-versions.json"

    if ($Apply) {
        $output = & $ScriptUnderTest -ConfigPath $configPath -Apply 2>&1 | Out-String
    }
    elseif ($ValidateOnly) {
        $output = & $ScriptUnderTest -ConfigPath $configPath -ValidateOnly 2>&1 | Out-String
    }
    else {
        $output = & $ScriptUnderTest -ConfigPath $configPath 2>&1 | Out-String
    }

    return $output.Trim()
}

function Get-PackageVersion {
    param([string]$Path)

    $manifest = Get-Content -Raw -Path $Path | ConvertFrom-Json
    return $manifest.version
}

function Get-FileText {
    param([string]$Path)

    return Get-Content -Raw -Path $Path
}

function Test-DryRunReportsChangesWithoutMutatingFiles {
    $repoRoot = New-TestRepo
    $output = Invoke-Updater -RepoRoot $repoRoot

    Assert-Contains -Text $output -Expected "DRY-RUN" -Message "Dry-run output should identify the mode."
    Assert-Contains -Text $output -Expected "com.mrdav30.gridforge/package.json version 4.0.5 -> 4.0.6" -Message "Dry-run should report package version drift."

    Assert-Equal -Expected "4.0.5" -Actual (Get-PackageVersion (Join-Path $repoRoot "com.mrdav30.gridforge/package.json")) -Message "Dry-run must not mutate package.json."
}

function Test-ApplyUpdatesPackageAndInstallerVersions {
    $repoRoot = New-TestRepo
    $output = Invoke-Updater -RepoRoot $repoRoot -Apply

    Assert-Contains -Text $output -Expected "APPLY" -Message "Apply output should identify the mode."
    Assert-Equal -Expected "4.0.6" -Actual (Get-PackageVersion (Join-Path $repoRoot "com.mrdav30.gridforge/package.json")) -Message "Apply should update package.json."
    Assert-Equal -Expected "4.0.6" -Actual (Get-PackageVersion (Join-Path $repoRoot "com.mrdav30.gridforge.lean/package.json")) -Message "Apply should update every configured package.json."

    $standardInstaller = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs")
    Assert-Contains -Text $standardInstaller -Expected "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp" -Message "Apply should update standard FixedMathSharp git URL."
    Assert-Contains -Text $standardInstaller -Expected "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections" -Message "Apply should update standard SwiftCollections git URL."
    Assert-Contains -Text $standardInstaller -Expected '"v5.0.0"' -Message "Apply should update standard dependency versions."

    $standardAsmdef = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Runtime/GridForge.Runtime.asmdef")
    Assert-Contains -Text $standardAsmdef -Expected '"expression": "5.0.0"' -Message "Apply should update standard asmdef dependency version defines."

    $standardAuthoringSamplesAsmdef = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge/Samples/GridforgeDemo/GridForge.Samples.asmdef")
    Assert-Contains -Text $standardAuthoringSamplesAsmdef -Expected '"expression": "5.0.0"' -Message "Apply should update optional standard authoring sample asmdef dependency version defines."

    $leanInstaller = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs")
    Assert-Contains -Text $leanInstaller -Expected "https://github.com/mrdav30/FixedMathSharp-Unity.git?path=/com.mrdav30.fixedmathsharp.lean" -Message "Apply should update lean FixedMathSharp git URL."
    Assert-Contains -Text $leanInstaller -Expected "https://github.com/mrdav30/SwiftCollections-Unity.git?path=/com.mrdav30.swiftcollections.lean" -Message "Apply should update lean SwiftCollections git URL."
    Assert-Contains -Text $leanInstaller -Expected '"v5.0.0"' -Message "Apply should update lean dependency versions."

    $leanAsmdef = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Runtime/GridForge.Lean.Runtime.asmdef")
    Assert-Contains -Text $leanAsmdef -Expected '"expression": "5.0.0"' -Message "Apply should update lean asmdef dependency version defines."

    $leanAuthoringSamplesAsmdef = Get-FileText -Path (Join-Path $repoRoot "com.mrdav30.gridforge.lean/Samples/GridforgeDemo/GridForge.Lean.Samples.asmdef")
    Assert-Contains -Text $leanAuthoringSamplesAsmdef -Expected '"expression": "5.0.0"' -Message "Apply should update optional lean authoring sample asmdef dependency version defines."
}

function Test-ValidateOnlyFailsWhenFilesDrift {
    $repoRoot = New-TestRepo

    try {
        Invoke-Updater -RepoRoot $repoRoot -ValidateOnly | Out-Null
    }
    catch {
        Assert-Contains -Text $_.Exception.Message -Expected "Validation failed" -Message "ValidateOnly should explain drift failures."
        return
    }

    throw "ValidateOnly should fail when files do not match the config."
}

function Test-ValidateOnlyFailsOnUnconfiguredInstallerDependency {
    $repoRoot = New-TestRepo
    Invoke-Updater -RepoRoot $repoRoot -Apply | Out-Null

    $installerPath = Join-Path $repoRoot "com.mrdav30.gridforge/Editor/Utility/DependencyInstaller/GitDependencyInstaller.cs"
    $installer = Get-FileText -Path $installerPath
    $installer = $installer.Replace(
        "};",
        @'
    new(
        "com.mrdav30.unexpected",
        "https://example.invalid/Unexpected.git?path=/com.mrdav30.unexpected",
        "v1.0.0"
    )
};
'@)
    Set-Content -Path $installerPath -Value $installer -Encoding UTF8

    try {
        Invoke-Updater -RepoRoot $repoRoot -ValidateOnly | Out-Null
    }
    catch {
        Assert-Contains -Text $_.Exception.Message -Expected "Validation failed" -Message "ValidateOnly should fail when installer dependencies drift from config."
        return
    }

    throw "ValidateOnly should fail when an installer contains an unconfigured dependency."
}

function Test-ValidateOnlyPassesAfterApply {
    $repoRoot = New-TestRepo
    Invoke-Updater -RepoRoot $repoRoot -Apply | Out-Null
    $output = Invoke-Updater -RepoRoot $repoRoot -ValidateOnly

    Assert-Contains -Text $output -Expected "VALIDATE" -Message "ValidateOnly output should identify the mode."
    Assert-Contains -Text $output -Expected "All configured Unity package versions are in sync." -Message "ValidateOnly should report success after apply."
}

try {
    New-Item -ItemType Directory -Path $TestRoot -Force | Out-Null

    $tests = @(
        "Test-DryRunReportsChangesWithoutMutatingFiles",
        "Test-ApplyUpdatesPackageAndInstallerVersions",
        "Test-ValidateOnlyFailsWhenFilesDrift",
        "Test-ValidateOnlyFailsOnUnconfiguredInstallerDependency",
        "Test-ValidateOnlyPassesAfterApply"
    )

    foreach ($test in $tests) {
        & $test
        Write-Host "PASS $test"
    }
}
finally {
    if (Test-Path $TestRoot) {
        Remove-Item -Path $TestRoot -Recurse -Force
    }
}
