@{
    AliasesToExport      = @()
    Author               = 'Przemyslaw Klys'
    CmdletsToExport      = @()
    CompanyName          = 'Evotec'
    CompatiblePSEditions = @('Desktop', 'Core')
    Copyright            = '(c) 2011 - 2023 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description          = 'Console Monster is a PowerShell module to build cool GUI applications using Spectre Console and Terminal GUI'
    FunctionsToExport    = @('New-SpectreImage', 'New-SpectreRule', 'New-Terminal', 'New-TerminalButton', 'New-TerminalCheckBox', 'New-TerminalRadioGroup', 'New-TerminalText', 'New-TerminalWindow')
    GUID                 = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion        = '0.0.3'
    PowerShellVersion    = '5.1'
    PrivateData          = @{
        PSData = @{
            Tags       = @('Windows', 'MacOS', 'Linux')
            ProjectUri = 'https://github.com/EvotecIT/ConsoleMonster'
            IconUri    = 'https://evotec.xyz/wp-content/uploads/2022/08/ConsoleMonster-1.png'
        }
    }
    RootModule           = 'ConsoleMonster.psm1'
}