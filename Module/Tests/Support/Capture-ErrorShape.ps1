[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Shape,
    [Parameter(Mandatory)][ValidateSet('native', 'compact')][string] $Style,
    [Parameter(Mandatory)][string] $Directory
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'New-ErrorShapeFixture.ps1')
. (Join-Path $PSScriptRoot 'Get-ErrorShapeRecords.ps1')
$invocation = New-ErrorShapeFixture -Directory $Directory
$nativeView = if ($PSVersionTable.PSVersion.Major -ge 7) { 'ConciseView' } else { 'NormalView' }
$ErrorView = $nativeView
Disable-ConsoleFormatting
if ($Style -eq 'compact') { Enable-ConsoleFormatting -Feature Errors }
try {
    if ($Shape -eq 'mixed-output') {
        if ($Style -eq 'compact') {
            Test-SyntheticErrorShape -Shape mixed-output -ErrorAction Continue 2>&1 | Out-ConsoleDefault -Shim -TopLevel
        } else {
            Test-SyntheticErrorShape -Shape mixed-output -ErrorAction Continue 2>&1 | Microsoft.PowerShell.Core\Out-Default
        }
    } else {
        $record = Get-ErrorShapeRecords -Shape $Shape -Invocation $invocation
        if ($record -isnot [Management.Automation.ErrorRecord]) { throw 'The fixture did not produce an ErrorRecord.' }
        if ($Style -eq 'compact') { $record | Out-ConsoleDefault -Shim -TopLevel }
        else { $record | Microsoft.PowerShell.Core\Out-Default }
    }
} finally { Disable-ConsoleFormatting }
