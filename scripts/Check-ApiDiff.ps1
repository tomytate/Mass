#!/usr/bin/env pwsh
param(
    [string]$BaselinePath = "src/Mass.Spec/api-baseline.txt",
    [switch]$UpdateBaseline
)

Write-Host "Checking Mass.Spec API compatibility..." -ForegroundColor Cyan

# Build Mass.Spec
dotnet build src/Mass.Spec/Mass.Spec.csproj -c Release -v q

# Generate current API surface
$assemblyPath = "src/Mass.Spec/bin/Release/net10.0/Mass.Spec.dll"

if (-not (Test-Path $assemblyPath)) {
    Write-Error "Assembly not found: $assemblyPath"
    exit 1
}

# Extract public API using reflection
$currentApi = & {
    Add-Type -Path $assemblyPath
    [System.Reflection.Assembly]::LoadFrom($assemblyPath).GetExportedTypes() |
    Where-Object { $_.IsPublic } |
    ForEach-Object {
        "Type: $($_.FullName)"
        $_.GetMembers([System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance -bor [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::DeclaredOnly) | 
        ForEach-Object {
            "  Member: $($_.ToString())"
        } | Sort-Object
    } | Sort-Object
} | Out-String

$currentApi = $currentApi.Trim()

# Compare with baseline
if (Test-Path $BaselinePath) {
    $baseline = (Get-Content $BaselinePath -Raw).Trim()
    
    if ($currentApi -ne $baseline) {
        Write-Host "`nAPI differences detected!`n" -ForegroundColor Red
        
        # Show diff (simple line diff)
        $currentLines = $currentApi -split "`r?`n"
        $baselineLines = $baseline -split "`r?`n"
        
        $removed = $baselineLines | Where-Object { $_ -notin $currentLines }
        $added = $currentLines | Where-Object { $_ -notin $baselineLines }
        
        if ($removed) {
            Write-Host "Removed:" -ForegroundColor Red
            $removed | ForEach-Object { Write-Host "  - $_" }
        }
        
        if ($added) {
            Write-Host "`nAdded:" -ForegroundColor Green
            $added | ForEach-Object { Write-Host "  + $_" }
        }
        
        if ($UpdateBaseline) {
            Write-Host "`nUpdating baseline..." -ForegroundColor Yellow
            $currentApi | Set-Content $BaselinePath
            Write-Host "Baseline updated successfully!" -ForegroundColor Green
            exit 0
        }
        else {
            Write-Host "`nTo update baseline, run: ./scripts/Check-ApiDiff.ps1 -UpdateBaseline" -ForegroundColor Yellow
            exit 1
        }
    }
    else {
        Write-Host "No API changes detected!" -ForegroundColor Green
        exit 0
    }
}
else {
    Write-Host "No baseline found. Creating initial baseline..." -ForegroundColor Yellow
    $currentApi | Set-Content $BaselinePath
    Write-Host "Baseline created successfully!" -ForegroundColor Green
    exit 0
}
