# Mass Suite PowerShell Module
# Wraps the Mass.CLI tool for PowerShell integration

function Invoke-MassBurn {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$IsoPath,

        [Parameter(Mandatory = $true)]
        [string]$Drive,

        [Parameter()]
        [ValidateSet("FAT32", "NTFS", "ExFAT")]
        [string]$FileSystem = "FAT32",

        [Parameter()]
        [ValidateSet("GPT", "MBR")]
        [string]$PartitionScheme = "GPT"
    )

    $cliPath = Join-Path $PSScriptRoot "..\Mass.CLI\bin\Debug\net10.0\Mass.CLI.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Error "Mass.CLI executable not found at $cliPath. Please build the solution first."
        return
    }

    & $cliPath burn --iso "$IsoPath" --drive "$Drive" --filesystem $FileSystem --partition $PartitionScheme
}

function Start-MassWorkflow {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$FilePath
    )

    $cliPath = Join-Path $PSScriptRoot "..\Mass.CLI\bin\Debug\net10.0\Mass.CLI.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Error "Mass.CLI executable not found at $cliPath. Please build the solution first."
        return
    }

    & $cliPath workflow run --file "$FilePath"
}

function Get-MassWorkflow {
    [CmdletBinding()]
    param ()

    $cliPath = Join-Path $PSScriptRoot "..\Mass.CLI\bin\Debug\net10.0\Mass.CLI.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Error "Mass.CLI executable not found at $cliPath. Please build the solution first."
        return
    }

    & $cliPath workflow list
}

function Get-MassConfig {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Key
    )

    $cliPath = Join-Path $PSScriptRoot "..\Mass.CLI\bin\Debug\net10.0\Mass.CLI.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Error "Mass.CLI executable not found at $cliPath. Please build the solution first."
        return
    }

    & $cliPath config get $Key
}

function Set-MassConfig {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Key,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $cliPath = Join-Path $PSScriptRoot "..\Mass.CLI\bin\Debug\net10.0\Mass.CLI.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Error "Mass.CLI executable not found at $cliPath. Please build the solution first."
        return
    }

    & $cliPath config set $Key $Value
}

Export-ModuleMember -Function Invoke-MassBurn, Start-MassWorkflow, Get-MassWorkflow, Get-MassConfig, Set-MassConfig
