Import-Module ConsoleMonster

$allTasks = @(
    [pscustomobject] @{ Id = 'DiskCleanup'; Name = 'Disk cleanup'; Category = 'Maintenance'; Description = 'Removes temporary files, old update caches and crash dumps older than 30 days.'; Minutes = 3; Changes = $true }
    #region more
    [pscustomobject] @{ Id = 'LogRotation'; Name = 'Log rotation'; Category = 'Maintenance'; Description = 'Compresses application logs older than 7 days and deletes archives older than 90 days.'; Minutes = 2; Changes = $true }
    [pscustomobject] @{ Id = 'RestartPending'; Name = 'Pending restart check'; Category = 'Maintenance'; Description = 'Reports servers waiting for a restart after updates or role changes.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'PatchStatus'; Name = 'Patch status'; Category = 'Security'; Description = 'Lists missing security updates and the date of the last successful install.'; Minutes = 2; Changes = $false }
    [pscustomobject] @{ Id = 'Certificates'; Name = 'Certificate expiry'; Category = 'Security'; Description = 'Finds machine certificates that expire within 30 days and the services that use them.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'LocalAdmins'; Name = 'Local administrators audit'; Category = 'Security'; Description = 'Compares local Administrators membership with the approved list.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Firewall'; Name = 'Firewall rules review'; Category = 'Security'; Description = 'Flags inbound allow rules open to any address.'; Minutes = 2; Changes = $false }
    [pscustomobject] @{ Id = 'EventErrors'; Name = 'Event log errors'; Category = 'Health'; Description = 'Summarizes System and Application errors from the last 24 hours.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Services'; Name = 'Service health'; Category = 'Health'; Description = 'Checks that automatic services are running and restarts the ones that stopped.'; Minutes = 1; Changes = $true }
    [pscustomobject] @{ Id = 'DiskSpace'; Name = 'Disk space'; Category = 'Health'; Description = 'Warns below 15% free and fails below 5% free on every fixed volume.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Uptime'; Name = 'Uptime and reboots'; Category = 'Health'; Description = 'Reports uptime and unexpected reboots in the last 30 days.'; Minutes = 1; Changes = $false }
    #endregion
)

$tasks = $allTasks | Read-ConsoleMultiSelection 'Tasks' -Display Name -GroupBy Category -Minimum 1 -Preview {
    New-ConsoleRows -Gap 1 {
        New-ConsoleText $_.Description
        New-ConsoleKeyValue -Markup ([ordered] @{
            Group     = $_.Category
            Duration  = "about $($_.Minutes) min per server"
            Changes   = if ($_.Changes) { '[warn]yes[/], makes changes' } else { '[pass]no[/], read only' }
            Parameter = "[accent]$($_.Id)[/]"
        })
    }
}
Write-ConsoleMarkup "Running [bold]$($tasks.Count)[/] tasks"
