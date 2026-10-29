Import-Module ConsoleMonster

# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ Name = 'WebApi'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Worker'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Archive'; Status = 'Stopped'; StartType = 'Manual' }
)

# Format-Table, Format-List and Format-Wide as PowerShell draws them
$services | Format-Table Name, Status, StartType -AutoSize
$services[0] | Format-List Name, Status, StartType
$services | Format-Wide Name -Column 3

Enable-ConsoleFormatting

# The same commands, drawn by ConsoleMonster
$services | Format-Table Name, Status, StartType -AutoSize
$services[0] | Format-List Name, Status, StartType
$services | Format-Wide Name -Column 3
