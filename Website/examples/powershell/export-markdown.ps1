Import-Module ConsoleMonster

# The largest files in PowerShell's folder, as a Markdown table for a report.
Get-ChildItem -Path $PSHOME -File |
    Sort-Object -Property Length -Descending |
    Select-Object -First 5 |
    Format-ConsoleTable -Property Name, Length, LastWriteTime -As Markdown
