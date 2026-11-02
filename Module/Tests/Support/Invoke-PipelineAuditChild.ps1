param([Parameter(Mandatory)][string] $Name, [Parameter(Mandatory)][string] $ModulePath, [Parameter(Mandatory)][string] $ScratchPath)
$ErrorActionPreference = 'Stop'
$env:APPDATA = $ScratchPath
$env:LOCALAPPDATA = $ScratchPath
$env:TEMP = $ScratchPath
$env:TMP = $ScratchPath
$env:CONSOLEMONSTER_MODE = 'plain'
Set-Location -LiteralPath $ScratchPath
Import-Module $ModulePath -Force
$command = Get-Command -Name $Name -Module ConsoleMonster -ErrorAction Stop
$records = @(1..300 | ForEach-Object { [pscustomobject]@{ Name = ('audit-item-{0:D3}' -f $_); Value = $_ } })
$auditErrors = @()
$parameters = @{ ErrorAction = 'SilentlyContinue'; ErrorVariable = 'auditErrors' }
switch ($Name) {
    { $_ -in @('New-ConsoleButton', 'New-ConsoleDatePicker', 'New-ConsoleField', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'New-ConsoleTreeSelect') } { $parameters.Key = 'audit' }
    'New-ConsoleTreeSelect' { $parameters.InputObject = [pscustomobject]@{ Name = 'sample' } }
    'New-ConsoleCommand' { $parameters.Key = 'audit'; $parameters.ScriptBlock = {} }
    'New-ConsoleSelect' { $parameters.Key = 'audit'; $parameters.Choices = @('choice') }
    { $_ -in @('New-ConsoleDashboard', 'New-ConsoleDialog', 'New-ConsoleForm', 'New-ConsoleMenu') } { $parameters.Title = 'audit' }
    'New-ConsoleBinding' { $parameters.Content = {} }
    'New-ConsoleLogo' { $parameters.Text = 'audit' }
    'New-ConsoleRegion' { $parameters.WidgetId = 'audit' }
    { $_ -in @('Register-ConsoleView', 'Unregister-ConsoleView') } { $parameters.TypeName = 'ConsoleMonster.Audit' }
    'Set-ConsoleTheme' { $parameters.Theme = 'Dark' }
    'Compare-ConsoleObject' { $parameters.ReferenceObject = @('a'); $parameters.PassThru = $true }
    'Compare-ConsoleText' { $parameters.ReferenceText = 'a'; $parameters.DifferenceText = 'b' }
    { $_ -in @('Invoke-ConsoleRun', 'Use-ConsoleProgress', 'Use-ConsoleTarget') } { $parameters.ScriptBlock = {} }
    'Start-ConsoleRun' { $parameters.InputObject = @('audit') }
    'Export-ConsoleTheme' { $parameters.Path = 'theme.json' }
    'Show-ConsoleApp' { $parameters.Content = {} }
    'Wait-ConsoleDashboard' { $parameters.Seconds = 0 }
    'Watch-Console' { $parameters.ScriptBlock = {}; $parameters.Count = 1; $parameters.NoLive = $true }
    'Watch-ConsoleLog' { Set-Content -LiteralPath 'audit.log' -Value 'audit'; $parameters.Path = 'audit.log'; $parameters.Count = 1; $parameters.NoLive = $true }
    'Export-ConsoleRecording' { $parameters.RecordingPath = 'missing.cast' }
    'Show-ConsolePresentation' { $parameters.ScriptPath = 'missing.ps1' }
    'Assert-ConsoleOutput' { [IO.File]::WriteAllText((Join-Path $ScratchPath 'golden.txt'), 'audit'); $parameters.GoldenPath = 'golden.txt'; $parameters.PassThru = $true; $records = @('audit') * 300 }
    'Complete-ConsoleRun' { $parameters.Run = Start-ConsoleRun -InputObject @('audit') -NoLive }
    'Invoke-ConsoleDashboard' { $parameters.Dashboard = New-ConsoleText 'audit'; $parameters.ScriptBlock = {}; $parameters.Parallel = $true }
    'Invoke-ConsoleRecording' { $parameters.ScriptBlock = {}; $parameters.Path = 'audit.cast' }
    'Set-ConsoleFormValue' { $parameters.Form = New-ConsoleForm -Title 'audit' -Content { New-ConsoleField -Key 'Name' } }
    'Show-ConsoleDiagram' { Set-Content -LiteralPath 'audit.cfx' -Value "server a ""A""`nserver b ""B""`na -> b ""calls"""; $parameters.Path = 'audit.cfx'; $parameters.StaticSnapshot = $true }
    'Show-ConsoleReplay' { $parameters.Dashboard = New-ConsoleText 'audit' }
    'New-ConsoleDataGrid' { $type = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.Grid'); $parameters.Grid = $type::new('audit'); $parameters.Key = 'audit' }
    'Update-Console' { $parameters.WidgetId = 'audit'; $parameters.Status = 'Pending' }
}
if ($command.Parameters.ContainsKey('WhatIf')) { $parameters.WhatIf = $true }
if ($Name -in @('Read-ConsoleConfirm', 'Read-ConsoleDate', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSelect', 'Read-ConsoleText', 'Show-ConsoleMenu')) { $parameters.Mode = 'None' }
$terminating = $null
$produced = 0
try { $records | & $command @parameters *>&1 | ForEach-Object { $produced++ } } catch { $terminating = $_; if ($auditErrors.Count -eq 0) { $auditErrors += $_ } }
$firstId = if ($terminating) { $terminating.FullyQualifiedErrorId } elseif ($auditErrors.Count) { $auditErrors[0].FullyQualifiedErrorId } else { '' }
[Console]::Out.WriteLine('AUDIT_RESULT:' + (@{ Name = $Name; Errors = @($auditErrors).Count; FirstId = $firstId; OutputCount = $produced } | ConvertTo-Json -Compress))
