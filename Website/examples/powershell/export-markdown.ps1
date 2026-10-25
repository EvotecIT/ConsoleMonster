Import-Module ConsoleMonster

# The largest files in PowerShell's folder, as a Markdown table for a report.
Get-ChildItem -Path $PSHOME -File |
    Sort-Object -Property Length -Descending |
    Select-Object -Property Name, Length, LastWriteTime -First 5 |
    Export-ConsoleFrame -Format Markdown
