@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Add-ConsoleItem', 'Add-ConsoleRecordingMarker', 'Add-ConsoleValue', 'Assert-ConsoleOutput', 'Clear-ConsoleScreen', 'Compare-ConsoleObject', 'Compare-ConsoleText', 'Complete-ConsoleRun', 'Copy-ConsoleOutput', 'Disable-ConsoleFormatting', 'Enable-ConsoleFormatting', 'Export-ConsoleDemo', 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Export-ConsoleTheme', 'Format-ConsoleCommand', 'Get-ConsoleCapability', 'Get-ConsoleDemo', 'Invoke-ConsoleDashboard', 'Invoke-ConsolePester', 'Invoke-ConsolePresentation', 'Invoke-ConsoleRecording', 'Invoke-ConsoleRun', 'New-ConsoleActivityList', 'New-ConsoleAlign', 'New-ConsoleBarChart', 'New-ConsoleBarItem', 'New-ConsoleBinding', 'New-ConsoleButton', 'New-ConsoleCellMap', 'New-ConsoleChartSeries', 'New-ConsoleColumnFormat', 'New-ConsoleColumns', 'New-ConsoleDashboard', 'New-ConsoleField', 'New-ConsoleFooterBar', 'New-ConsoleForm', 'New-ConsoleFormPreset', 'New-ConsoleGrid', 'New-ConsoleHeaderBar', 'New-ConsoleHistogram', 'New-ConsoleKeyValue', 'New-ConsoleKeyValueItem', 'New-ConsoleLane', 'New-ConsoleLanes', 'New-ConsoleLineChart', 'New-ConsoleLiveTable', 'New-ConsoleLogEntry', 'New-ConsoleLogo', 'New-ConsoleLogRegion', 'New-ConsoleMenu', 'New-ConsoleMenuItem', 'New-ConsoleMenuPreset', 'New-ConsoleMenuRunResult', 'New-ConsoleMultiSelect', 'New-ConsolePadding', 'New-ConsolePanel', 'New-ConsolePhases', 'New-ConsoleProgress', 'New-ConsoleRegion', 'New-ConsoleRows', 'New-ConsoleRule', 'New-ConsoleSelect', 'New-ConsoleSimulatedClock', 'New-ConsoleSparkline', 'New-ConsoleSpinner', 'New-ConsoleStack', 'New-ConsoleStatusItem', 'New-ConsoleStatusList', 'New-ConsoleSummaryBar', 'New-ConsoleTable', 'New-ConsoleText', 'New-ConsoleTextArea', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'New-ConsoleTree', 'New-ConsoleTreeNode', 'Out-ConsoleDefault', 'Out-ConsoleFormatCommand', 'Read-ConsoleConfirm', 'Read-ConsoleForm', 'Read-ConsoleMultiSelect', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSecret', 'Read-ConsoleSelect', 'Read-ConsoleText', 'Register-ConsoleView', 'Send-ConsoleEvent', 'Set-ConsoleFormValue', 'Set-ConsoleRegion', 'Set-ConsoleTheme', 'Set-ConsoleValue', 'Show-Console', 'Show-ConsoleApp', 'Show-ConsoleChanges', 'Show-ConsoleChart', 'Show-ConsoleCode', 'Show-ConsoleDemo', 'Show-ConsoleError', 'Show-ConsoleGrid', 'Show-ConsoleHelp', 'Show-ConsoleJson', 'Show-ConsoleList', 'Show-ConsoleMarkdown', 'Show-ConsoleMenu', 'Show-ConsoleObject', 'Show-ConsoleOverview', 'Show-ConsolePresentation', 'Show-ConsoleReplay', 'Show-ConsoleRule', 'Show-ConsoleStatusLine', 'Show-ConsoleTable', 'Show-ConsoleTree', 'Start-ConsoleRun', 'Stop-ConsoleRecording', 'Unregister-ConsoleView', 'Update-ConsoleActivity', 'Update-ConsoleCell', 'Update-ConsoleLane', 'Update-ConsoleLiveTable', 'Update-ConsolePhase', 'Update-ConsoleProgress', 'Update-ConsoleRun', 'Update-ConsoleSpinner', 'Update-ConsoleStatusItem', 'Update-ConsoleSummaryBar', 'Use-ConsoleProgress', 'Use-ConsoleTarget', 'Wait-ConsoleDashboard', 'Wait-ConsoleRun', 'Watch-Console', 'Watch-ConsoleLog', 'Write-ConsoleLogEntry', 'Write-ConsoleProgress', 'Write-ConsoleText')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.'
    FormatsToProcess       = @('ConsoleMonster.format.ps1xml')
    FunctionsToExport      = @()
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '2.0.2'
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
