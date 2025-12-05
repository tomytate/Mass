#!/usr/bin/env pwsh
param(
    [string]$ProjectFile = "src/Mass.Spec/Mass.Spec.csproj",
    [string]$VersionBaselinePath = "src/Mass.Spec/version-baseline.txt"
)

Write-Host "Checking Semver compliance..." -ForegroundColor Cyan

# 1. Check for API Changes
Write-Host "Running API Diff..."
./scripts/Check-ApiDiff.ps1
$apiDiffExitCode = $LASTEXITCODE

if ($apiDiffExitCode -eq 0) {
    Write-Host "No API changes. Semver check passed." -ForegroundColor Green
    exit 0
}

# 2. API Changed - Check Version Bump
Write-Host "API changes detected. Verifying version bump..." -ForegroundColor Yellow

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

[xml]$csproj = Get-Content $ProjectFile
$currentVersionStr = $csproj.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($currentVersionStr)) {
    Write-Error "Could not find <Version> in $ProjectFile"
    exit 1
}
[Version]$currentVersion = $currentVersionStr

if (-not (Test-Path $VersionBaselinePath)) {
    Write-Host "No version baseline found. Setting baseline to $currentVersion..." -ForegroundColor Yellow
    $currentVersionStr | Set-Content $VersionBaselinePath
    exit 0
}

$baselineVersionStr = (Get-Content $VersionBaselinePath -Raw).Trim()
[Version]$baselineVersion = $baselineVersionStr

Write-Host "Current Version:  $currentVersion"
Write-Host "Baseline Version: $baselineVersion"

if ($currentVersion -gt $baselineVersion) {
    Write-Host "Version bumped successfully!" -ForegroundColor Green
    
    # Remind to update baselines
    Write-Host "`nIMPORTANT: Remember to update baselines before merging:" -ForegroundColor Magenta
    Write-Host "  ./scripts/Check-ApiDiff.ps1 -UpdateBaseline"
    Write-Host "  echo $currentVersionStr > $VersionBaselinePath"
    exit 0
}
else {
    Write-Error "API changed but version not bumped! Please increment version in $ProjectFile"
    exit 1
}
