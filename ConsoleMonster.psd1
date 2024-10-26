@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @()
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2024 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Console Monster is a PowerShell module to build cool GUI applications using Spectre Console and Terminal GUI'
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport      = @('New-SpectreImage', 'New-SpectreRule', 'New-Terminal', 'New-TerminalButton', 'New-TerminalCheckBox', 'New-TerminalRadioGroup', 'New-TerminalText', 'New-TerminalWindow')
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '0.0.4'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            ExternalModuleDependencies = @('Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Management')
            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2022/08/ConsoleMonster-1.png'
            ProjectUri                 = 'https://github.com/EvotecIT/ConsoleMonster'
            Tags                       = @('Windows', 'MacOS', 'Linux')
        }
    }
    RequiredModules        = @('Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Management')
    RootModule             = 'ConsoleMonster.psm1'
}