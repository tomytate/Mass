#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Mass Suite Developer Bootstrap..." -ForegroundColor Cyan

# 1. Check Prerequisites
Write-Host "`n[1/5] Checking prerequisites..."
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet SDK is required but not found."
    exit 1
}
$dotnetVersion = dotnet --version
Write-Host "  - dotnet SDK: $dotnetVersion" -ForegroundColor Green

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "git is required but not found."
    exit 1
}
Write-Host "  - git: Found" -ForegroundColor Green

# 2. Restore Solution
Write-Host "`n[2/5] Restoring solution..."
dotnet restore Mass.sln
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "  - Restore complete" -ForegroundColor Green

# 3. Setup Local NuGet Feed
Write-Host "`n[3/5] Setting up local NuGet feed..."
$feedPath = Join-Path $PSScriptRoot "..\..\.nupkg-feed"
if (-not (Test-Path $feedPath)) {
    New-Item -ItemType Directory -Path $feedPath | Out-Null
}
Write-Host "  - Feed path: $feedPath"

# Pack Mass.Spec
Write-Host "  - Packing Mass.Spec..."
dotnet pack src/Mass.Spec/Mass.Spec.csproj -o $feedPath -c Release
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "  - Mass.Spec packed" -ForegroundColor Green

# 4. Build Solution
Write-Host "`n[4/5] Building solution..."
dotnet build Mass.sln -c Debug
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "  - Build complete" -ForegroundColor Green

# 5. Setup Dev Data
Write-Host "`n[5/5] Setting up dev data..."
$devDataPath = Join-Path $PSScriptRoot "..\..\dev-data"
if (-not (Test-Path $devDataPath)) {
    New-Item -ItemType Directory -Path $devDataPath | Out-Null
}

# Create sample settings
$settingsPath = Join-Path $devDataPath "settings.json"
if (-not (Test-Path $settingsPath)) {
    $settings = @{
        "Logging"   = @{ "Level" = "Debug" }
        "Telemetry" = @{ "Enabled" = $false }
    } | ConvertTo-Json -Depth 4
    $settings | Set-Content $settingsPath
    Write-Host "  - Created sample settings.json" -ForegroundColor Green
}

Write-Host "`n✅ Bootstrap Complete! You are ready to code." -ForegroundColor Cyan
Write-Host "Run 'dotnet run --project src/Mass.Launcher' to start the app."
