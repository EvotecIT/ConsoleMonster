# A guided tour of typed prompts, task previews and a dashboard paused for confirmation.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 8)] [int] $Step = 0
)
Import-Module ConsoleMonster

# Data: fictional inventory and tasks. Answers never trigger the operations described.
# The path points into an absent sample folder; no directories or presets are created.
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Role = 'File'; OS = 'Windows Server 2022'; Offline = $null }
    [pscustomobject] @{ Name = 'FS02'; Site = 'Warsaw'; Role = 'File'; OS = 'Windows Server 2022'; Offline = $null }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Warsaw'; Role = 'Web'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Warsaw'; Role = 'Database'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'FS11'; Site = 'Berlin'; Role = 'File'; OS = 'Windows Server 2022'; Offline = $null }
    [pscustomobject] @{ Name = 'WEB11'; Site = 'Berlin'; Role = 'Web'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'PRN11'; Site = 'Berlin'; Role = 'Print'; OS = 'Windows Server 2022'; Offline = 'offline since 09:12' }
    [pscustomobject] @{ Name = 'BKP21'; Site = 'Dublin'; Role = 'Backup'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'SQL21'; Site = 'Dublin'; Role = 'Database'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'LAB01'; Site = 'Lab'; Role = 'Test'; OS = 'Linux'; Offline = $null }
    [pscustomobject] @{ Name = 'LAB02'; Site = 'Lab'; Role = 'Test'; OS = 'Linux'; Offline = 'in maintenance until Friday' }
)
$tasks = @(
    [pscustomobject] @{ Id = 'DiskCleanup'; Name = 'Disk cleanup'; Category = 'Maintenance'; Description = 'Removes temporary files, old update caches and crash dumps older than 30 days.'; Minutes = 3; Changes = $true }
    [pscustomobject] @{ Id = 'LogRotation'; Name = 'Log rotation'; Category = 'Maintenance'; Description = 'Compresses logs older than 7 days and removes archives older than 90 days.'; Minutes = 2; Changes = $true }
    [pscustomobject] @{ Id = 'RestartPending'; Name = 'Pending restart check'; Category = 'Maintenance'; Description = 'Reports servers waiting for a restart after updates or role changes.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'PatchStatus'; Name = 'Patch status'; Category = 'Security'; Description = 'Lists missing security updates and the last successful install.'; Minutes = 2; Changes = $false }
    [pscustomobject] @{ Id = 'Certificates'; Name = 'Certificate expiry'; Category = 'Security'; Description = 'Finds certificates expiring within 30 days and the services using them.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'LocalAdmins'; Name = 'Local administrators audit'; Category = 'Security'; Description = 'Compares local administrator membership with the approved list.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Firewall'; Name = 'Firewall rules review'; Category = 'Security'; Description = 'Flags inbound allow rules open to any address.'; Minutes = 2; Changes = $false }
    [pscustomobject] @{ Id = 'EventErrors'; Name = 'Event log errors'; Category = 'Health'; Description = 'Summarizes errors from the last 24 hours.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Services'; Name = 'Service health'; Category = 'Health'; Description = 'Checks automatic services and restarts stopped ones.'; Minutes = 1; Changes = $true }
    [pscustomobject] @{ Id = 'DiskSpace'; Name = 'Disk space'; Category = 'Health'; Description = 'Warns below 15% free and fails below 5% on each volume.'; Minutes = 1; Changes = $false }
    [pscustomobject] @{ Id = 'Uptime'; Name = 'Uptime and reboots'; Category = 'Health'; Description = 'Reports uptime and unexpected reboots in the last 30 days.'; Minutes = 1; Changes = $false }
)
$titles = @('Typed selection', 'Multiple selection', 'Confirmation', 'Text', 'Secret', 'Number', 'Path', 'Typed confirmation', 'Dashboard pause')
$reportFolder = './toolbox-reports/weekly'
$snapshot = $StaticSnapshot -or [Console]::IsInputRedirected
# Native prompt snapshots need the common Read-Console* -StaticSnapshot API.
$promptOptions = if ($snapshot) { @{ StaticSnapshot = $true } } else { @{} }

# Layout: the prompts themselves draw choices, validation, masks and editing hints.
$taskPreview = {
    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleText $_.Description
        New-ConsoleKeyValue -Content {
            New-ConsoleKeyValueItem Group $_.Category
            New-ConsoleKeyValueItem Duration "about $($_.Minutes) min per server"
            New-ConsoleKeyValueItem Changes $(if ($_.Changes) { '[warn]yes[/], makes changes' } else { '[pass]no[/], read only' }) -Markup
            New-ConsoleKeyValueItem Parameter "[accent]$($_.Id)[/]" -Markup
        }
    }
}
$taskBadge = {
    if ($_.Changes) { New-ConsoleText -Markup "[warn]changes [/][muted]~$($_.Minutes) min[/]" }
    else { New-ConsoleText -Markup "[muted]~$($_.Minutes) min[/]" }
}

# Behaviour: each answer is returned as its original type; the tour performs no action.
$first = if ($snapshot) { $Step } else { 0 }
$last = if ($snapshot) { $Step } else { 8 }
foreach ($page in $first..$last) {
    if (-not $snapshot) { Show-ConsoleRule "[bold]$($page + 1) / 9 · $($titles[$page])[/]" }
    switch ($page) {
        0 {
            $server = Read-ConsoleSelect 'Target server' -Choices $servers -Display Name -GroupBy Site -Badge { New-ConsoleText $_.Role -Role Muted } -DisabledReason { $_.Offline } -Describe { "$($_.Role) server · $($_.OS)" } -Description 'Type to filter; offline servers show why they cannot be picked.' -Default $servers[0] @promptOptions
            if (-not $snapshot) { Write-ConsoleText "returned: $($server.Name) (Server)" }
        }
        1 {
            $selected = @(Read-ConsoleMultiSelect Tasks -Choices $tasks -Display Name -GroupBy Category -Describe Description -Badge $taskBadge -Selected @($tasks | Where-Object Id -In PatchStatus, DiskSpace) -Minimum 1 -Preview $taskPreview @promptOptions)
            if (-not $snapshot) { Write-ConsoleText "returned: $($selected.Id -join ', ')" }
        }
        2 { $null = Read-ConsoleConfirm 'Restart the report agent on FS01?' -Default $true @promptOptions }
        3 {
            $null = Read-ConsoleText 'Share name' -Default Reports -Pattern '[A-Za-z][A-Za-z0-9_-]{1,23}' -PatternMessage 'Start with a letter; letters, digits, - and _ only' -Description 'Shown to users when they browse the server.' @promptOptions
        }
        4 { $null = Read-ConsoleSecret 'Password for svc-backup' -MinLength 8 -Description 'Try example-passphrase; at least 8 characters.' @promptOptions }
        5 { $null = Read-ConsoleNumber Port -Integer -Minimum 1 -Maximum 65535 -Default 443 @promptOptions }
        6 { $null = Read-ConsolePath 'Report folder' -Kind Folder -Default $reportFolder @promptOptions }
        7 {
            $null = Read-ConsoleConfirm 'Remove FS01 from the inventory?' -TypedConfirmation FS01 -Default $false -Warning 'Its history and reports would be removed too. Type FS01 to confirm.' @promptOptions
        }
        8 {
            if ($snapshot) {
                $null = Read-ConsoleConfirm 'WEB01 failed. Continue with the other servers?' -Default $true -Description 'The patch dashboard pauses for your answer, then resumes.' @promptOptions
                break
            }
            $hosts = @($servers | Select-Object -First 5 -ExpandProperty Name)
            $dashboard = New-ConsoleDashboard -Title '[bold]Patching[/]' -Subtitle '5 servers' -Content {
                New-ConsolePanel -Title Servers -Content { New-ConsoleLanes -Lane $hosts -Total 6 }
                New-ConsolePanel -Title Log -Fill -Content { New-ConsoleLogRegion -Height 6 }
            }
            Invoke-ConsoleDashboard $dashboard -ArgumentList (,$hosts) {
                param($Hosts)
                for ($tick = 1; $tick -le 6; $tick++) {
                    foreach ($hostName in $Hosts) {
                        $status = if ($hostName -eq 'WEB01' -and $tick -eq 3) { 'Fail' } else { 'Pass' }
                        Update-ConsoleLane -Key $hostName -Result $status -Detail "step $tick"
                    }
                    Write-ConsoleLogEntry "step $tick done"
                    Start-Sleep -Milliseconds 350
                    if ($tick -eq 3) {
                        Write-ConsoleLogEntry 'WEB01: update verification failed' -Level Error
                        $go = Read-ConsoleConfirm 'WEB01 failed. Continue with the other servers?' -Default $true -Description 'The patch dashboard pauses for your answer, then resumes.'
                        Write-ConsoleLogEntry $(if ($go) { 'continuing' } else { 'stopping after this step' })
                        if (-not $go) { break }
                    }
                }
            }
        }
    }
}
