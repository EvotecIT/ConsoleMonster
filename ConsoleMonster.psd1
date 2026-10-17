@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Add-ConsoleValue', 'Assert-ConsoleOutput', 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Format-ConsoleCode', 'Format-ConsoleError', 'Format-ConsoleJson', 'Format-ConsoleList', 'Format-ConsoleTable', 'Format-ConsoleTree', 'Get-ConsoleCapability', 'Invoke-ConsolePester', 'New-ConsoleAlign', 'New-ConsoleBarChart', 'New-ConsoleColumns', 'New-ConsoleDashboard', 'New-ConsoleField', 'New-ConsoleForm', 'New-ConsoleGrid', 'New-ConsoleHistogram', 'New-ConsoleKeyValue', 'New-ConsoleLanes', 'New-ConsoleLineChart', 'New-ConsoleLog', 'New-ConsoleLogo', 'New-ConsoleMenu', 'New-ConsoleMenuItem', 'New-ConsolePanel', 'New-ConsolePhases', 'New-ConsoleProgress', 'New-ConsoleRows', 'New-ConsoleRule', 'New-ConsoleSparkline', 'New-ConsoleSpinner', 'New-ConsoleStack', 'New-ConsoleStatusList', 'New-ConsoleTable', 'New-ConsoleText', 'New-ConsoleTree', 'Read-ConsoleConfirm', 'Read-ConsoleForm', 'Read-ConsoleMultiSelection', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSecret', 'Read-ConsoleSelection', 'Read-ConsoleText', 'Send-ConsoleEvent', 'Set-ConsoleTheme', 'Set-ConsoleValue', 'Show-ConsoleMenu', 'Start-ConsoleDashboard', 'Start-ConsoleReplay', 'Update-ConsoleLane', 'Update-ConsolePhase', 'Update-ConsoleProgress', 'Update-ConsoleSpinner', 'Update-ConsoleStatus', 'Use-ConsoleProgress', 'Write-ConsoleException', 'Write-ConsoleKeyValue', 'Write-ConsoleLog', 'Write-ConsoleMarkup', 'Write-ConsoleRule', 'Write-ConsoleStatus', 'Write-ConsoleTable', 'Write-ConsoleText', 'Write-ConsoleTree', 'Write-ConsoleWidget')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.'
    FunctionsToExport      = @()
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '2.0.0'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            ExternalModuleDependencies = @()
            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2022/08/ConsoleMonster-1.png'
            ProjectUri                 = 'https://github.com/EvotecIT/ConsoleMonster'
            Tags                       = @('Windows', 'MacOS', 'Linux', 'Console', 'Terminal', 'Dashboard', 'Table', 'Progress', 'TUI')
            RequireLicenseAcceptance   = $false
}
    }
    RequiredModules        = @()
    RootModule             = 'ConsoleMonster.psm1'
}