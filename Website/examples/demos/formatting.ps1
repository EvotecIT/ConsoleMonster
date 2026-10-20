# Everyday commands as PowerShell shows them, then the same commands after one line: Enable-ConsoleFormatting.
$windows = $env:OS -eq 'Windows_NT'
$process = if ($windows) { @{ Name = 'svchost' } } else { @{ Id = $PID } }
#region hidden
# The hidden lines only print the prompt before each command, as a terminal would.
function Write-Prompt([string] $Command) { Start-Sleep -Milliseconds 700; Write-Host "PS> $Command" -ForegroundColor Yellow; Start-Sleep -Milliseconds 400 }
Set-Location -LiteralPath (Join-Path $PSHOME 'Modules')
Write-Prompt "# PowerShell's own output"
Write-Prompt ("Get-Process $(if ($windows) { 'svchost' } else { "-Id $PID" }) | Sort-Object CPU -Descending | Select-Object -First 8")
#endregion
# PowerShell's own output
Get-Process @process | Sort-Object CPU -Descending | Select-Object -First 8 | Out-Default
#region hidden
Write-Prompt 'Get-ChildItem'
#endregion
Get-ChildItem | Out-Default
#region hidden
Write-Prompt '@{ Domain = ''evotec.xyz''; Records = 14; DNSSEC = $false }'
#endregion
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false } | Out-Default
#region hidden
Write-Prompt 'Enable-ConsoleFormatting'
#endregion
Enable-ConsoleFormatting
#region hidden
Write-Prompt '# The same commands now'
Write-Prompt ("Get-Process $(if ($windows) { 'svchost' } else { "-Id $PID" }) | Sort-Object CPU -Descending | Select-Object -First 8")
#endregion
# The same commands now
Get-Process @process | Sort-Object CPU -Descending | Select-Object -First 8 | Out-Default
#region hidden
Write-Prompt 'Get-ChildItem'
#endregion
Get-ChildItem | Out-Default
#region hidden
Write-Prompt '@{ Domain = ''evotec.xyz''; Records = 14; DNSSEC = $false }'
#endregion
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false } | Out-Default
#region hidden
Write-Prompt $(if ($windows) { 'Get-Service W* | Format-ConsoleTable -GroupBy Status -Top 6' } else { "Get-Process -Id $PID | Format-ConsoleTable -GroupBy ProcessName -Top 6" })
#endregion
if ($windows) {
    Get-Service W* | Format-ConsoleTable -GroupBy Status -Top 6
} else {
    Get-Process @process | Format-ConsoleTable -GroupBy ProcessName -Top 6
}
#region hidden
Start-Sleep -Milliseconds 600
Disable-ConsoleFormatting
#endregion
