[CmdletBinding()]
param(
    [string]$InstallRoot = 'C:\Program Files\DesktopShell',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [switch]$SkipVRService,
    [switch]$NoRestart
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'DesktopShell\DesktopShell.csproj'
$publishedExe = Join-Path $repoRoot 'DesktopShell\bin\Release\DesktopShell.exe'
$destExe = Join-Path $InstallRoot 'DesktopShell.exe'
$vrServiceDeploy = Resolve-Path -Path (Join-Path $repoRoot '..\VRService\scripts\deploy.ps1') -ErrorAction SilentlyContinue

if (-not (Test-Path $projectPath)) { throw "Project not found: $projectPath" }
if (-not (Test-Path $InstallRoot)) { throw "Install root not found: $InstallRoot" }

# Stop the running shell so we can overwrite its exe
$running = Get-Process DesktopShell -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "[desktopshell] stopping running process(es): $($running.Id -join ', ')"
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

Write-Host "[desktopshell] publishing $Configuration/$Runtime"
& dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableWindowsTargeting=true
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

if (-not (Test-Path $publishedExe)) { throw "Expected publish output missing: $publishedExe" }

Write-Host "[desktopshell] copying -> $destExe"
Copy-Item -Path $publishedExe -Destination $destExe -Force

if (-not $SkipVRService) {
    if (-not $vrServiceDeploy) {
        throw "VRService deploy script not found at ..\VRService\scripts\deploy.ps1 (pass -SkipVRService to bypass)"
    }
    Write-Host "[desktopshell] delegating to $vrServiceDeploy"
    & $vrServiceDeploy -InstallRoot $InstallRoot -Configuration $Configuration -Runtime $Runtime
    if ($LASTEXITCODE -ne 0) { throw "VRService deploy failed (exit $LASTEXITCODE)" }
}

if (-not $NoRestart) {
    Write-Host "[desktopshell] starting $destExe"
    Start-Process -FilePath $destExe -WorkingDirectory $InstallRoot
}

Write-Host "[desktopshell] done"
