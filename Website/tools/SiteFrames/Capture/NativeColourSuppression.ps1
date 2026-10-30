[CmdletBinding()]
param([Parameter(Mandatory)][string] $ModulePath, [switch] $Enabled)

$ErrorActionPreference = 'Stop'
Import-Module $ModulePath
Import-Module PSReadLine
Set-PSReadLineOption -HistorySaveStyle SaveNothing
if ((Get-Command Set-PSReadLineOption).Parameters.ContainsKey('PredictionSource')) { Set-PSReadLineOption -PredictionSource None }
$options = Get-PSReadLineOption
foreach ($property in $options.PSObject.Properties) {
    if ($property.Name.EndsWith('Color') -and $property.IsSettable) { $property.Value = "$([char]27)[0m" }
}
if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = 'PlainText' }
if ($Enabled) { Enable-ConsoleFormatting -Exclude Output, FormatCommands, Errors, Streams }
[Console]::Write("$([char]27)]0;CM_READY$([char]7)")
$legacy = [Microsoft.PowerShell.PSConsoleReadLine].GetMethods() | Where-Object { $_.Name -eq 'ReadLine' -and $_.GetParameters().Count -eq 2 }
$line = if ($legacy) { [Microsoft.PowerShell.PSConsoleReadLine]::ReadLine([runspace]::DefaultRunspace, $ExecutionContext) }
else { [Microsoft.PowerShell.PSConsoleReadLine]::ReadLine([runspace]::DefaultRunspace, $ExecutionContext, $null) }
if ($line -cne 'Get-Process') { throw 'Native editor did not receive the complete scripted command.' }
Disable-ConsoleFormatting
[Console]::WriteLine('NATIVE_EDITOR_OK')
