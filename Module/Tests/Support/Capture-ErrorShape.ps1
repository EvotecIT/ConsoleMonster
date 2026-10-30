[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Shape,
    [Parameter(Mandatory)][ValidateSet('native', 'compact')][string] $Style,
    [Parameter(Mandatory)][string] $Directory
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'New-ErrorShapeFixture.ps1')
. (Join-Path $PSScriptRoot 'Get-ErrorShapeRecords.ps1')
$invocation = New-ErrorShapeFixture -Directory $Directory -MixedOutput:($Shape -eq 'mixed-output')
$nativeView = if ($PSVersionTable.PSVersion.Major -ge 7) { 'ConciseView' } else { 'NormalView' }
$ErrorView = $nativeView
Disable-ConsoleFormatting
if ($Style -eq 'compact') { Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact }
try {
    if ($Shape -eq 'mixed-output') {
        if ($Style -eq 'compact') {
            & $invocation 2>&1 | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))) -Shim -TopLevel
        } else {
            & $invocation 2>&1 | Microsoft.PowerShell.Core\Out-Default
        }
    } else {
        $record = Get-ErrorShapeRecords -Shape $Shape -Invocation $invocation
        if ($record -isnot [Management.Automation.ErrorRecord]) { throw 'The fixture did not produce an ErrorRecord.' }
        if ($Style -eq 'compact') { $record | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))) -Shim -TopLevel }
        else { $record | Microsoft.PowerShell.Core\Out-Default }
    }
} finally { Disable-ConsoleFormatting }
