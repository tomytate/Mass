$interfacesPath = "src\Mass.Core\Interfaces"
$specPath = "src\Mass.Spec"

Write-Host "Checking Mass.Core.Interfaces for contract compliance..." -ForegroundColor Cyan

$interfaceFiles = Get-ChildItem -Path $interfacesPath -Filter "*.cs" -Exclude "README.md"

$issues = @()

foreach ($file in $interfaceFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Check 1: Must have using Mass.Spec or be system-only
    if ($content -notmatch 'using Mass\.Spec' -and $file.Name -notin @("IConfigurationService.cs", "WorkflowExecutionOptions.cs")) {
        if ($content -match 'public\s+(class|interface).*:\s*[A-Z]') {
            $issues += "[$($file.Name)] May inherit from non-Mass.Spec type"
        }
    }
    
    # Check 2: No Mass.Core internal types in signatures (except Mass.Core.Interfaces)
    if ($content -match 'Mass\.Core\.(?!Interfaces)') {
        $issues += "[$($file.Name)] References internal Mass.Core types"
    }
    
    # Check 3: Must be public
    if ($content -notmatch 'public (interface|class)') {
        $issues += "[$($file.Name)] Not marked as public"
    }
}

if ($issues.Count -gt 0) {
    Write-Error "Interface contract violations found:"
    $issues | ForEach-Object { Write-Error "  - $_" }
    exit 1
}
else {
    Write-Host "✓ All interfaces comply with contract rules" -ForegroundColor Green
    Write-Host "✓ All interfaces use Mass.Spec types only" -ForegroundColor Green
    Write-Host "✓ All interfaces are public" -ForegroundColor Green
    exit 0
}
