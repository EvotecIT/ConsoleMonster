@{
    AliasesToExport        = @()
    Author                 = 'Przemyslaw Klys'
    CmdletsToExport        = @('Add-ConsoleRecordingMarker', 'Assert-ConsoleOutput', 'Compare-ConsoleObject', 'Compare-ConsoleText', 'Complete-ConsoleRun', 'Disable-ConsoleFormatting', 'Enable-ConsoleFormatting', 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Export-ConsoleTheme', 'Get-ConsoleCapability', 'Get-ConsoleDemo', 'Get-ConsoleFormatting', 'Invoke-ConsoleDashboard', 'Invoke-ConsolePester', 'Invoke-ConsoleRecording', 'Invoke-ConsoleRun', 'New-ConsoleActivityList', 'New-ConsoleBinding', 'New-ConsoleButton', 'New-ConsoleCalendar', 'New-ConsoleCellMap', 'New-ConsoleChart', 'New-ConsoleColumns', 'New-ConsoleCommand', 'New-ConsoleDashboard', 'New-ConsoleDataGrid', 'New-ConsoleDatePicker', 'New-ConsoleDialog', 'New-ConsoleField', 'New-ConsoleFlow', 'New-ConsoleForm', 'New-ConsoleImage', 'New-ConsoleKeyValue', 'New-ConsoleLanes', 'New-ConsoleLayoutGrid', 'New-ConsoleLogo', 'New-ConsoleLogRegion', 'New-ConsoleMenu', 'New-ConsoleMenuItem', 'New-ConsolePanel', 'New-ConsoleProgress', 'New-ConsoleRegion', 'New-ConsoleRows', 'New-ConsoleRule', 'New-ConsoleScreen', 'New-ConsoleScrollView', 'New-ConsoleSelect', 'New-ConsoleStack', 'New-ConsoleStatusList', 'New-ConsoleSummaryBar', 'New-ConsoleTable', 'New-ConsoleTabs', 'New-ConsoleText', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'New-ConsoleTree', 'New-ConsoleTreeSelect', 'Read-ConsoleConfirm', 'Read-ConsoleDate', 'Read-ConsoleForm', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSelect', 'Read-ConsoleText', 'Register-ConsoleView', 'Set-ConsoleFormValue', 'Set-ConsoleTheme', 'Show-Console', 'Show-ConsoleApp', 'Show-ConsoleDemo', 'Show-ConsoleGrid', 'Show-ConsoleHelp', 'Show-ConsoleImage', 'Show-ConsoleMenu', 'Show-ConsoleObject', 'Show-ConsoleOverview', 'Show-ConsolePresentation', 'Show-ConsoleReplay', 'Show-ConsoleStatusLine', 'Show-ConsoleTable', 'Start-ConsoleRun', 'Stop-ConsoleRecording', 'Unregister-ConsoleView', 'Update-Console', 'Use-ConsoleProgress', 'Use-ConsoleTarget', 'Wait-ConsoleDashboard', 'Watch-Console', 'Watch-ConsoleLog', 'Write-ConsoleLogEntry', 'Write-ConsoleText')
    CompanyName            = 'Evotec'
    CompatiblePSEditions   = @('Desktop', 'Core')
    Copyright              = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description            = 'Readable output for the commands you already run, and tables, trees, charts, live runs, dashboards, menus and forms when you need more. One engine for Windows PowerShell 5.1, PowerShell 7 and .NET, with no extra modules to install and plain text when output is redirected.'
    FormatsToProcess       = @('ConsoleMonster.format.ps1xml')
    FunctionsToExport      = @()
    GUID                   = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
    ModuleVersion          = '2.1.2'
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
