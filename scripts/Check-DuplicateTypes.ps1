$specPath = "src\Mass.Spec"
$corePath = "src\Mass.Core"

function Get-PublicTypes {
    param ($path)
    $types = @()
    $files = Get-ChildItem -Path $path -Recurse -Filter "*.cs"
    foreach ($file in $files) {
        $content = Get-Content $file.FullName
        foreach ($line in $content) {
            if ($line -match '^\s*public\s+(class|record|struct|enum|interface)\s+(\w+)') {
                $types += $matches[2]
            }
        }
    }
    return $types | Sort-Object -Unique
}

$specTypes = Get-PublicTypes -path $specPath
$coreTypes = Get-PublicTypes -path $corePath

$duplicates = @()
foreach ($type in $specTypes) {
    if ($coreTypes -contains $type) {
        # Exclude allowed duplicates
        if ($type -notin @("Extensions", "Program", "ServiceCollectionExtensions")) {
            $duplicates += $type
        }
    }
}

if ($duplicates.Count -gt 0) {
    Write-Error "Found duplicate public types in Mass.Core matching Mass.Spec:"
    $duplicates | ForEach-Object { Write-Error "- $_" }
    exit 1
}
else {
    Write-Host "No duplicate types found."
    exit 0
}
