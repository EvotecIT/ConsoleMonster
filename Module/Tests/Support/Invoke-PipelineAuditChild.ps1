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
if ($command.Parameters.ContainsKey('WhatIf')) { $parameters.WhatIf = $true }
if ($Name -in @('Read-ConsoleConfirm', 'Read-ConsoleDate', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleSelect', 'Read-ConsoleText', 'Show-ConsoleMenu', 'Show-ConsoleObject')) { $parameters.Mode = 'None' }
try { $records | & $command @parameters *>&1 | Out-Null } catch { if ($auditErrors.Count -eq 0) { $auditErrors += $_ } }
[Console]::Out.WriteLine('AUDIT_RESULT:' + (@{ Name = $Name; Errors = @($auditErrors).Count } | ConvertTo-Json -Compress))
