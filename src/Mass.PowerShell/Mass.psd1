@{
    # Script module or binary module file associated with this manifest.
    RootModule        = 'Mass.psm1'

    # Version number of this module.
    ModuleVersion     = '1.0.0'

    # Supported PSEditions
    # CompatiblePSEditions = @('Desktop', 'Core')

    # ID used to uniquely identify this module
    GUID              = 'e1a2b3c4-d5e6-f7g8-h9i0-j1k2l3m4n5o6'

    # Author of this module
    Author            = 'Mass Suite Team'

    # Company or vendor of this module
    CompanyName       = 'Mass Suite'

    # Copyright statement for this module
    Copyright         = '(c) 2025 Mass Suite. All rights reserved.'

    # Description of the functionality provided by this module
    Description       = 'PowerShell module for Mass Suite automation'

    # Minimum version of the Windows PowerShell engine required by this module
    # PowerShellVersion = ''

    # Modules that must be imported into the global environment prior to importing this module
    # RequiredModules = @()

    # Assemblies that must be loaded prior to importing this module
    # RequiredAssemblies = @()

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    # ScriptsToProcess = @()

    # Type files (.ps1xml) to be loaded when importing this module
    # TypesToProcess = @()

    # Format files (.ps1xml) to be loaded when importing this module
    # FormatsToProcess = @()

    # Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
    # NestedModules = @()

    # Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
    FunctionsToExport = @('Invoke-MassBurn', 'Start-MassWorkflow', 'Get-MassWorkflow', 'Get-MassConfig', 'Set-MassConfig')

    # Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
    CmdletsToExport   = @()

    # Variables to export from this module
    VariablesToExport = '*'

    # Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
    AliasesToExport   = @()

    # List of all modules packaged with this module
    # ModuleList = @()

    # List of all files packaged with this module
    # FileList = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
    PrivateData       = @{

        PSData = @{

            # Tags applied to this module. These help with module discovery in online galleries.
            # Tags = @()

            # A URL to the license for this module.
            # LicenseUri = ''

            # A URL to the main website for this project.
            # ProjectUri = ''

            # A URL to an icon representing this module.
            # IconUri = ''

            # ReleaseNotes of this module
            # ReleaseNotes = ''

        } # End of PSData hashtable

    } # End of PrivateData hashtable

    # HelpInfo URI of this module
    # HelpInfoUri = ''

    # Default prefix for commands exported from the module. Override the default prefix using Import-Module -Prefix.
    # DefaultCommandPrefix = ''

}
