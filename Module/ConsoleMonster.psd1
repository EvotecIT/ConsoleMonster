@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Add-ConsoleJobSummary', 'Add-ConsoleRecordingMarker', 'Assert-ConsoleOutput', 'Compare-ConsoleObject', 'Compare-ConsoleText', 'Complete-ConsoleRun', 'ConvertTo-ConsoleView', 'Disable-ConsoleFormatting', 'Enable-ConsoleFormatting', 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Export-ConsoleTheme', 'Get-ConsoleCapability', 'Get-ConsoleDemo', 'Get-ConsoleFormatting', 'Get-ConsoleSetting', 'Import-ConsoleTheme', 'Invoke-ConsoleDashboard', 'Invoke-ConsoleHeadless', 'Invoke-ConsolePester', 'Invoke-ConsoleRecording', 'Invoke-ConsoleRun', 'New-ConsoleAccordion', 'New-ConsoleActivityList', 'New-ConsoleBinding', 'New-ConsoleBreadcrumb', 'New-ConsoleButton', 'New-ConsoleCalendar', 'New-ConsoleCellMap', 'New-ConsoleChart', 'New-ConsoleChartView', 'New-ConsoleCollapsible', 'New-ConsoleColumns', 'New-ConsoleCommand', 'New-ConsoleDashboard', 'New-ConsoleDashboardFrame', 'New-ConsoleDataGrid', 'New-ConsoleDatePicker', 'New-ConsoleDiagram', 'New-ConsoleDiagramGroup', 'New-ConsoleDiagramLink', 'New-ConsoleDiagramNode', 'New-ConsoleDiagramStep', 'New-ConsoleDiagramUseCase', 'New-ConsoleDialog', 'New-ConsoleFacts', 'New-ConsoleField', 'New-ConsoleFlow', 'New-ConsoleForm', 'New-ConsoleImage', 'New-ConsoleKeyValue', 'New-ConsoleLanes', 'New-ConsoleLayoutGrid', 'New-ConsoleLogo', 'New-ConsoleLogRegion', 'New-ConsoleMenu', 'New-ConsoleMenuItem', 'New-ConsoleOutline', 'New-ConsolePanel', 'New-ConsoleProgress', 'New-ConsoleRegion', 'New-ConsoleRows', 'New-ConsoleRule', 'New-ConsoleScreen', 'New-ConsoleScrollView', 'New-ConsoleSelect', 'New-ConsoleSplitter', 'New-ConsoleStack', 'New-ConsoleStatusList', 'New-ConsoleSummaryBar', 'New-ConsoleTable', 'New-ConsoleTabs', 'New-ConsoleText', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'New-ConsoleTree', 'New-ConsoleTreeSelect', 'Read-ConsoleConfirm', 'Read-ConsoleDate', 'Read-ConsoleForm', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSelect', 'Read-ConsoleText', 'Register-ConsoleView', 'Reset-ConsoleSetting', 'Set-ConsoleFormValue', 'Set-ConsoleSetting', 'Set-ConsoleTheme', 'Show-Console', 'Show-ConsoleApp', 'Show-ConsoleDemo', 'Show-ConsoleDiagram', 'Show-ConsoleGrid', 'Show-ConsoleHelp', 'Show-ConsoleImage', 'Show-ConsoleMenu', 'Show-ConsoleObject', 'Show-ConsoleOverview', 'Show-ConsolePresentation', 'Show-ConsoleReplay', 'Show-ConsoleSetting', 'Show-ConsoleStatusLine', 'Show-ConsoleTable', 'Start-ConsoleRun', 'Stop-ConsoleRecording', 'Test-ConsoleTheme', 'Unregister-ConsoleView', 'Update-Console', 'Use-ConsoleGroup', 'Use-ConsoleProgress', 'Use-ConsoleTarget', 'Wait-ConsoleDashboard', 'Watch-Console', 'Watch-ConsoleLog', 'Write-ConsoleAnnotation', 'Write-ConsoleLogEntry', 'Write-ConsoleText')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Readable output for the commands you already run, and tables, trees, charts, live runs, dashboards, menus and forms when you need more. One engine for Windows PowerShell 5.1, PowerShell 7 and .NET, with no extra modules to install and plain text when output is redirected.'
    FormatsToProcess       = @('ConsoleMonster.format.ps1xml')
    FunctionsToExport      = @()
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '2.4.0'
    PowerShellVersion      = '5.1'
    PrivateData            = @{
        PSData = @{
            ExternalModuleDependencies = @()
            LicenseUri                 = 'https://github.com/EvotecIT/ConsoleMonster/blob/master/LICENSE'
            IconUri                    = 'https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/site/icon-512.png'
            ProjectUri                 = 'https://consolemonster.dev/'
            Tags                       = @('Windows', 'MacOS', 'Linux', 'Console', 'Terminal', 'Formatting', 'Table', 'Tree', 'Chart', 'Progress', 'Dashboard', 'Menu', 'Prompt', 'TUI', 'ANSI')
            RequireLicenseAcceptance   = $false
}
    }
    RequiredModules        = @()
    RootModule             = 'ConsoleMonster.psm1'
}
