Import-Module ConsoleMonster

$form = New-ConsoleForm -Title 'Service inventory' -Content {
    New-ConsoleField -Key Name -Label 'Service name' -Type Text -Default 'WinRM' -Required
}
$answers = $form | Read-ConsoleForm
# Collect answers only. The caller decides whether to execute a command.
