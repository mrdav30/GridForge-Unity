<#
.SYNOPSIS
Runs the GridForge Unity EditMode tests in batch mode.

.DESCRIPTION
Launches Unity with the Unity Test Framework command-line runner and writes an
NUnit XML result file. The command intentionally omits -quit because the Test
Framework exits Unity after the run is complete; passing -quit can shut Unity
down before tests execute.

.PARAMETER UnityPath
Path to the Unity executable. When omitted, UNITY_EDITOR is used first, then
Unity/Unity.exe on PATH, then the newest Unity Hub editor on Windows.

.PARAMETER ProjectPath
Path to the Unity project root. Relative paths are resolved from the package
repository root.

.PARAMETER AssemblyName
EditMode test assembly to run. Defaults to GridForge.Unity.Tests.EditMode.

.PARAMETER TestResults
Path to the NUnit XML result file. Defaults to the current user's temp folder.

.PARAMETER LogFile
Unity log destination. Defaults to '-' so Unity writes to stdout.

.PARAMETER WhatIf
Prints the Unity command without launching Unity.
#>
[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$ProjectPath,
    [string]$AssemblyName = "GridForge.Unity.Tests.EditMode",
    [string]$TestResults,
    [string]$LogFile = "-",
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "gridforge-unity-batch.ps1")

$resolvedUnityPath = Resolve-GridForgeUnityEditorPath -UnityPath $UnityPath
$resolvedProjectPath = Resolve-GridForgeUnityProjectPath -ProjectPath $ProjectPath
$packagesRoot = Get-GridForgePackagesRoot

if ([string]::IsNullOrWhiteSpace($TestResults)) {
    $resolvedTestResults = Join-Path ([System.IO.Path]::GetTempPath()) "gridforge-unity-editmode-results.xml"
}
else {
    $resolvedTestResults = Resolve-GridForgePath -Path $TestResults -BasePath $packagesRoot
}

$unityProjectPath = Convert-GridForgePathForUnity -Path $resolvedProjectPath -UnityPath $resolvedUnityPath
$unityLogFile = Convert-GridForgePathForUnity -Path $LogFile -UnityPath $resolvedUnityPath
$unityTestResults = Convert-GridForgePathForUnity -Path $resolvedTestResults -UnityPath $resolvedUnityPath

$arguments = @(
    "-batchmode",
    "-projectPath",
    $unityProjectPath,
    "-runTests",
    "-testPlatform",
    "EditMode",
    "-assemblyNames",
    $AssemblyName,
    "-testResults",
    $unityTestResults,
    "-logFile",
    $unityLogFile
)

Write-Output "Unity: $resolvedUnityPath"
Write-Output "Project: $resolvedProjectPath"
Write-Output "AssemblyName: $AssemblyName"
Write-Output "TestResults: $resolvedTestResults"

if ($unityProjectPath -ne $resolvedProjectPath) {
    Write-Output "Unity project argument: $unityProjectPath"
}

if ($WhatIf) {
    Write-Output "WhatIf: skipping Unity launch."
    $displayArguments = @($arguments | ForEach-Object { Format-GridForgeCommandArgument -Argument $_ })
    Write-Output "Command: $(Format-GridForgeCommandArgument -Argument $resolvedUnityPath) $($displayArguments -join ' ')"
    return
}

$resultsDirectory = Split-Path -Parent $resolvedTestResults
if (-not [string]::IsNullOrWhiteSpace($resultsDirectory)) {
    New-Item -ItemType Directory -Path $resultsDirectory -Force | Out-Null
}

Remove-Item -LiteralPath $resolvedTestResults -Force -ErrorAction SilentlyContinue

$exitCode = Invoke-GridForgeProcess -FilePath $resolvedUnityPath -Arguments $arguments

if (-not (Test-Path -LiteralPath $resolvedTestResults -PathType Leaf)) {
    throw "Unity EditMode test run exited with code $exitCode but did not write a result file: $resolvedTestResults"
}

[xml]$resultsXml = Get-Content -LiteralPath $resolvedTestResults
$testRun = $resultsXml."test-run"
Write-Output "Result: $($testRun.result); Total: $($testRun.total); Passed: $($testRun.passed); Failed: $($testRun.failed); Skipped: $($testRun.skipped)"

if ($exitCode -ne 0) {
    throw "Unity EditMode tests failed with exit code $exitCode. See result file: $resolvedTestResults"
}

if ($testRun.result -ne "Passed") {
    throw "Unity EditMode tests did not pass. Result: $($testRun.result). See result file: $resolvedTestResults"
}
