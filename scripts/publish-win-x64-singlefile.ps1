[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$SelfContained = $true,
    [switch]$IncludeNativeLibrariesForSelfExtract = $true
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'DesktopShell\DesktopShell.csproj'

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}

$sc = if ($SelfContained) { 'true' } else { 'false' }
$includeNative = if ($IncludeNativeLibrariesForSelfExtract) { 'true' } else { 'false' }

$publishArgs = @(
    'publish',
    $projectPath,
    '-c', $Configuration,
    '-r', $Runtime,
    "--self-contained", $sc,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=$includeNative"
)

Write-Host "Running: dotnet $($publishArgs -join ' ')"
& dotnet @publishArgs
