Import-Module ConsoleMonster

# Format-Table, Format-List and Format-Wide as PowerShell draws them
Get-Service W* | Select-Object -First 8 | Format-Table Name, Status, StartType -AutoSize
Get-Command Get-Service | Format-List Name, CommandType, Source, Version
Get-ChildItem | Format-Wide Name -Column 3

Enable-ConsoleFormatting -Feature FormatCommands

# The same commands, drawn by ConsoleMonster
Get-Service W* | Select-Object -First 8 | Format-Table Name, Status, StartType -AutoSize
Get-Command Get-Service | Format-List Name, CommandType, Source, Version
Get-ChildItem | Format-Wide Name -Column 3
