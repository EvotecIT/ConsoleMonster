@{
    AliasesToExport        = @('fcl', 'fct', 'ForEach-ConsoleItem')
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Add-ConsoleItem', 'Add-ConsoleValue', 'Assert-ConsoleOutput', 'Clear-ConsoleScreen', 'Compare-ConsoleObject', 'Compare-ConsoleText', 'Copy-ConsoleOutput', 'Disable-ConsoleFormatting', 'Enable-ConsoleFormatting', 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Export-ConsoleTheme', 'Format-ConsoleCode', 'Format-ConsoleError', 'Format-ConsoleJson', 'Format-ConsoleList', 'Format-ConsoleTable', 'Format-ConsoleTree', 'Get-ConsoleCapability', 'Invoke-ConsolePester', 'Invoke-ConsoleRun', 'New-ConsoleActivityList', 'New-ConsoleAlign', 'New-ConsoleBarChart', 'New-ConsoleBarItem', 'New-ConsoleBinding', 'New-ConsoleButton', 'New-ConsoleCellMap', 'New-ConsoleChartSeries', 'New-ConsoleClock', 'New-ConsoleColumns', 'New-ConsoleDashboard', 'New-ConsoleField', 'New-ConsoleForm', 'New-ConsoleFormPreset', 'New-ConsoleGrid', 'New-ConsoleHistogram', 'New-ConsoleKeyValue', 'New-ConsoleKeyValueItem', 'New-ConsoleLane', 'New-ConsoleLanes', 'New-ConsoleLineChart', 'New-ConsoleLiveTable', 'New-ConsoleLog', 'New-ConsoleLogEntry', 'New-ConsoleLogo', 'New-ConsoleMenu', 'New-ConsoleMenuItem', 'New-ConsoleMenuPreset', 'New-ConsoleMultiSelect', 'New-ConsolePadding', 'New-ConsolePanel', 'New-ConsolePhases', 'New-ConsoleProgress', 'New-ConsoleRegion', 'New-ConsoleRows', 'New-ConsoleRule', 'New-ConsoleRunResult', 'New-ConsoleSelect', 'New-ConsoleSparkline', 'New-ConsoleSpinner', 'New-ConsoleStack', 'New-ConsoleStatusItem', 'New-ConsoleStatusList', 'New-ConsoleSummaryBar', 'New-ConsoleTable', 'New-ConsoleText', 'New-ConsoleTextArea', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'New-ConsoleTree', 'New-ConsoleTreeNode', 'Out-ConsoleChart', 'Out-ConsoleDefault', 'Out-ConsoleFormatCommand', 'Read-ConsoleConfirm', 'Read-ConsoleForm', 'Read-ConsoleMultiSelection', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSecret', 'Read-ConsoleSelection', 'Read-ConsoleText', 'Register-ConsoleView', 'Send-ConsoleEvent', 'Set-ConsoleFormValue', 'Set-ConsoleRegion', 'Set-ConsoleTheme', 'Set-ConsoleValue', 'Show-Console', 'Show-ConsoleApp', 'Show-ConsoleChanges', 'Show-ConsoleGrid', 'Show-ConsoleHelp', 'Show-ConsoleMenu', 'Show-ConsoleObject', 'Show-ConsoleReplay', 'Start-ConsoleDashboard', 'Start-ConsoleReplay', 'Update-ConsoleActivity', 'Update-ConsoleCell', 'Update-ConsoleLane', 'Update-ConsolePhase', 'Update-ConsoleProgress', 'Update-ConsoleSpinner', 'Update-ConsoleStatus', 'Update-ConsoleSummaryBar', 'Update-ConsoleTable', 'Use-ConsoleProgress', 'Use-ConsoleTarget', 'Watch-Console', 'Watch-ConsoleLog', 'Write-ConsoleException', 'Write-ConsoleKeyValue', 'Write-ConsoleLog', 'Write-ConsoleMarkup', 'Write-ConsoleProgress', 'Write-ConsoleRule', 'Write-ConsoleStatus', 'Write-ConsoleTable', 'Write-ConsoleText', 'Write-ConsoleTree', 'Write-ConsoleWidget')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.'
    FormatsToProcess       = @('ConsoleMonster.format.ps1xml')
    FunctionsToExport      = @()
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '2.0.1'
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
