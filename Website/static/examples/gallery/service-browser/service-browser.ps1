# Browse fictional services; actions never change the local machine.
Import-Module -Name ConsoleMonster

# Data: explicit snapshots make the same demo portable across platforms.
$services = @(
    [pscustomobject] @{ Name = 'Spooler'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Print Spooler'; ProcessId = 1234; Account = 'LocalSystem'; Dependencies = 'RPC'; Description = 'Queues print jobs for lab printers.' }
    [pscustomobject] @{ Name = 'WinRM'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Windows Remote Management'; ProcessId = 2456; Account = 'NetworkService'; Dependencies = 'RPC'; Description = 'Receives remote administration requests.' }
    [pscustomobject] @{ Name = 'WSearch'; Status = 'Stopped'; StartType = 'Manual'; DisplayName = 'Windows Search'; ProcessId = 0; Account = 'LocalSystem'; Dependencies = 'RPC'; Description = 'Indexes lab documents for search.' }
    [pscustomobject] @{ Name = 'BITS'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Background Transfer'; ProcessId = 3100; Account = 'LocalSystem'; Dependencies = 'RPC'; Description = 'Transfers deployment packages.' }
    [pscustomobject] @{ Name = 'Dnscache'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'DNS Client'; ProcessId = 3220; Account = 'NetworkService'; Dependencies = 'NSI'; Description = 'Caches DNS lookups.' }
    [pscustomobject] @{ Name = 'W32Time'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Windows Time'; ProcessId = 3400; Account = 'LocalService'; Dependencies = 'RPC'; Description = 'Keeps lab clocks synchronized.' }
    [pscustomobject] @{ Name = 'EventLog'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Windows Event Log'; ProcessId = 3500; Account = 'LocalService'; Dependencies = 'RPC'; Description = 'Collects service and security events.' }
    [pscustomobject] @{ Name = 'Schedule'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Task Scheduler'; ProcessId = 3600; Account = 'LocalSystem'; Dependencies = 'RPC'; Description = 'Runs scheduled maintenance tasks.' }
    [pscustomobject] @{ Name = 'RemoteRegistry'; Status = 'Stopped'; StartType = 'Manual'; DisplayName = 'Remote Registry'; ProcessId = 0; Account = 'LocalService'; Dependencies = 'RPC'; Description = 'Allows approved remote configuration.' }
    [pscustomobject] @{ Name = 'BackupAgent'; Status = 'Running'; StartType = 'Automatic'; DisplayName = 'Lab Backup Agent'; ProcessId = 4120; Account = 'LabBackup'; Dependencies = 'RPC'; Description = 'Verifies nightly backup snapshots.' }
)

# Behaviour: explicit arguments enter isolated, cancellable action runspaces.
$actions = foreach ($definition in @(@('Restart', 'F5'), @('Stop', 'F7'), @('Start', 'F9'))) {
    New-ConsoleCommand -Key $definition[0] -Label $definition[0] -Shortcut $definition[1] `
        -ConfirmPrompt ($definition[0] + ' {Selection}?') -ArgumentList $definition[0], 'Lab' `
        -ImportModule Microsoft.PowerShell.Utility -ScriptBlock {
            param($Row, $Operation, $Environment)
            "$Operation $($Row.Name) in $Environment"
            Start-Sleep -Milliseconds 200
            "Verifying $($Row.Name): $($Row.Status)"
        }
}

# Layout: list, details, buttons, modal confirmation and streamed output.
$services | Show-ConsoleGrid -Title 'Service browser' -Property Name, Status, StartType, DisplayName, ProcessId, Account, Dependencies, Description `
    -Hide DisplayName, ProcessId, Account, Dependencies, Description -Action $actions
