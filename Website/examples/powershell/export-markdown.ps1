Import-Module ConsoleMonster

# The largest files in PowerShell's folder, as a Markdown table for a report.
Get-ChildItem -Path $PSHOME -File |
    Sort-Object -Property Length -Descending |
    Select-Object -First 5 |
    Show-ConsoleTable -Property Name, Length, LastWriteTime -AsWidget | Export-ConsoleFrame -Format Markdown
