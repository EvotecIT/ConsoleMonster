Import-Module ConsoleMonster

# A few of PowerShell's own files, grouped by extension.
Get-ChildItem -Path $PSHOME -File |
    Where-Object Extension -in '.exe', '.json', '.ps1' |
    Format-ConsoleTable -Property Name, Length, LastWriteTime -GroupBy Extension
