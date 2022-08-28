@{
    AliasesToExport      = @()
    Author               = 'Przemyslaw Klys'
    CmdletsToExport      = @()
    CompanyName          = 'Evotec'
    CompatiblePSEditions = @('Desktop', 'Core')
    Copyright            = '(c) 2011 - 2022 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description          = 'Console Monster'
    FunctionsToExport    = @('New-SpectreImage', 'New-SpectreRule', 'New-Terminal', 'New-TerminalButton', 'New-TerminalText', 'New-TerminalWindow')
    GUID                 = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion        = '0.0.1'
    PowerShellVersion    = '5.1'
    PrivateData          = @{
        PSData = @{
            Tags = @('Windows', 'MacOS', 'Linux')
        }
    }
    RequiredModules      = @(@{
            ModuleVersion = '0.0.238'
            Guid          = 'ee272aa8-baaa-4edf-9f45-b6d6f7d844fe'
            ModuleName    = 'PSSharedGoods'
        })
    RootModule           = 'ConsoleMonster.psm1'
}