Import-Module ConsoleMonster

# PowerShell's own output
Get-Process svchost | Sort-Object CPU -Descending | Select-Object -First 8
Get-ChildItem
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }

Enable-ConsoleFormatting

# The same commands now
Get-Process svchost | Sort-Object CPU -Descending | Select-Object -First 8
Get-ChildItem
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }
Get-Service W* | Format-ConsoleTable -GroupBy Status -Top 6