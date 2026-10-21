Import-Module ConsoleMonster

if (Read-ConsoleConfirm 'Remove FS01 from the inventory?' -TypedConfirmation 'FS01' -Warning 'Its history and reports are deleted too. This cannot be undone.') {
    Write-ConsoleMarkup '[warn]FS01 removed[/]'
}
