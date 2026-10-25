<#
.SYNOPSIS
Regenerates Module/ConsoleMonster.format.ps1xml from the widget types.

.DESCRIPTION
Imports the module from this repository (development binaries: build ConsoleMonster.PowerShell first) and writes
((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.WidgetFormatData'))::Generate() to ConsoleMonster.format.ps1xml next to the manifest. The file lists
every public widget type, so it changes when a widget is added or renamed; ConsoleMonster.Output.Tests.ps1 fails until it
is regenerated.

.EXAMPLE
dotnet build ConsoleMonster.PowerShell/ConsoleMonster.PowerShell.csproj -c Debug
pwsh -NoProfile -File Module/Build/Update-ConsoleFormatData.ps1
#>
[CmdletBinding()]
param()
$moduleRoot = Split-Path -Parent $PSScriptRoot
$target = Join-Path $moduleRoot 'ConsoleMonster.format.ps1xml'
if (-not (Test-Path -LiteralPath $target)) {
    # The manifest loads this file on import: an empty one lets the module load to generate the real one.
    [System.IO.File]::WriteAllText($target, '<Configuration><ViewDefinitions /></Configuration>', [System.Text.UTF8Encoding]::new($false))
}
Import-Module (Join-Path $moduleRoot 'ConsoleMonster.psd1') -Force -ErrorAction Stop
[System.IO.File]::WriteAllText($target, ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.WidgetFormatData'))::Generate(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $target"
