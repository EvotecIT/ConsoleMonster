# Browse fictional services and run a confirmed restart without leaving the app.
Import-Module -Name ConsoleMonster

# Data: fixed service snapshots, so this demo never changes the local machine.
$services = @(
    [pscustomobject] @{ Name = 'Spooler'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'WinRM'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'WSearch'; Status = 'Stopped'; StartType = 'Manual' }
)

# Behaviour: explicit arguments cross into an isolated action runspace.
$restart = New-ConsoleCommand -Key Restart -Label Restart -Shortcut F5 `
    -ConfirmPrompt 'Restart the selected services?' -ArgumentList 'Lab' `
    -ImportModule Microsoft.PowerShell.Utility -ScriptBlock {
        param($Row, $Environment)
        "Stopping $($Row.Name) in $Environment"
        Start-Sleep -Milliseconds 200
        "Starting $($Row.Name) in $Environment"
        Start-Sleep -Milliseconds 200
        "$($Row.Name): Running"
    }

# Layout: selected-row details, actions, modal confirmation and embedded output.
$services | Show-ConsoleGrid -Title 'Service browser' -Property Name, Status, StartType -Action $restart
