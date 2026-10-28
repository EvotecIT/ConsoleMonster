Import-Module ConsoleMonster

$confirmParameters = @{
    TypedConfirmation = 'FS01'
    Warning = 'Its history and reports are deleted too. This cannot be undone.'
}
if (Read-ConsoleConfirm 'Remove FS01 from the inventory?' @confirmParameters) {
    Write-ConsoleText -Markup '[warn]FS01 removed[/]'
}
