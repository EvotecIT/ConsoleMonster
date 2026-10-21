param(
    [switch] $Sample,
    [int] $Width
)
#region more
# A login overview (message of the day) for a Linux server: host facts, health and disk use, rendered once when someone
# logs in over SSH. Nothing is live; the script prints and exits, so login stays fast. Every collector is cheap and falls
# back quietly, so a missing tool never blocks a login. -Sample uses invented data, so the layout can be tried on any
# machine (Windows included).
#
# Hook it up (pick one, and keep it to interactive SSH sessions):
#
#   * pwsh as the login shell, in ~/.config/powershell/Microsoft.PowerShell_profile.ps1:
#
#         # ssh host command loads this profile too: SSH_TTY and a console that is not redirected mean a person logged in.
#         if ($env:SSH_TTY -and -not [Console]::IsOutputRedirected -and -not $env:CM_OVERVIEW_SHOWN) {
#             $env:CM_OVERVIEW_SHOWN = 1
#             & ~/bin/Example-LoginOverview.ps1
#         }
#
#   * bash as the login shell, at the end of ~/.profile (or ~/.bash_profile when you have one; bash then skips ~/.profile):
#
#         case $- in
#             *i*) if [ -n "$SSH_TTY" ] && command -v pwsh >/dev/null; then
#                      pwsh -NoLogo -NoProfile -File ~/bin/Example-LoginOverview.ps1
#                  fi ;;
#         esac
#
# Output that is not a terminal (scp, rsync, ssh host command) is skipped by both guards: the pwsh guard needs a terminal
# and output that is not redirected, the bash one an interactive shell, and ConsoleMonster prints plain text when output
# is redirected anyway.
#endregion
Import-Module ConsoleMonster

#region more
# Collectors: the last login, disk use per file system and the overview data (invented data with -Sample).
function Get-LastLogin {
    # The previous login of this user from `last` ("deploy pts/0 198.51.100.24 Mon Oct 19 22:41 - 23:10 (00:29)"), as
    # "from 198.51.100.24, Oct 19 22:41": the first line is the current session, so the second one is the previous login.
    $lines = @(& last -n 2 -w $env:USER 2>$null | Where-Object { $_ -and $_ -notmatch '^(wtmp|btmp) begins' })
    if ($lines.Count -lt 2) {
        return 'no earlier login recorded'
    }
    if ($lines[1] -match '^\S+\s+\S+\s+(?<from>\S+)\s+\w{3}\s+(?<when>\w{3}\s+\d+\s+\d{1,2}:\d{2})') {
        return 'from {0}, {1}' -f $Matches['from'], ($Matches['when'] -replace '\s+', ' ')
    }
    return ($lines[1] -replace '\s{2,}', ' ').Trim()
}

function Get-DiskUse {
    # Use in percent per file system: one row per device (bind mounts and repeated mounts of the same device are left
    # out), read-only snap images and memory file systems skipped.
    $disks = [ordered] @{}
    $seen = @{}
    foreach ($row in @(& df -P -x tmpfs -x devtmpfs -x squashfs -x overlay 2>$null | Select-Object -Skip 1)) {
        $fields = $row -split '\s+'
        if ($fields.Count -lt 6 -or $seen.ContainsKey($fields[0])) {
            continue
        }
        $seen[$fields[0]] = $true
        $disks[$fields[5]] = [int] ($fields[4].TrimEnd('%'))
    }
    $disks
}

function Get-OverviewData {
    param([switch] $Sample)
    if ($Sample) {
        return [pscustomobject] @{
            Host        = 'app-web-03'
            Os          = 'Ubuntu 24.04.3 LTS'
            Kernel      = '6.8.0-85-generic'
            Uptime      = New-TimeSpan -Days 41 -Hours 6 -Minutes 12
            Load        = @(0.42, 0.61, 0.58)
            Cpus        = 8
            MemoryTotal = 32GB
            MemoryUsed  = 19.4GB
            Disks       = [ordered] @{ '/' = 71; '/var' = 88; '/home' = 34; '/srv/data' = 93 }
            FailedUnits = @('backup-sync.service', 'certbot-renew.timer')
            Updates     = [pscustomobject] @{ Total = 14; Security = 3; RebootRequired = $true }
            LastLogin   = 'from 198.51.100.24, Oct 19 22:41'
        }
    }
    $os = (Get-Content /etc/os-release -ErrorAction SilentlyContinue | Where-Object { $_ -like 'PRETTY_NAME=*' }) -replace '^PRETTY_NAME="?|"$'
    $uptimeSeconds = [double] ((Get-Content /proc/uptime -ErrorAction SilentlyContinue) -split ' ')[0]
    $load = @(((Get-Content /proc/loadavg -ErrorAction SilentlyContinue) -split ' ')[0..2] | ForEach-Object { [double] $_ })
    $memory = @{}
    foreach ($line in Get-Content /proc/meminfo -ErrorAction SilentlyContinue) {
        if ($line -match '^(MemTotal|MemAvailable):\s+(\d+)') {
            $memory[$Matches[1]] = [double] $Matches[2] * 1KB
        }
    }
    $failed = @(& systemctl --failed --no-legend --plain 2>$null | ForEach-Object { ($_.Trim() -split '\s+')[0] } | Where-Object { $_ })
    $updates = [pscustomobject] @{ Total = $null; Security = $null; RebootRequired = (Test-Path /var/run/reboot-required) }
    if (Test-Path /var/lib/update-notifier/updates-available) {
        $text = Get-Content /var/lib/update-notifier/updates-available -Raw
        if ($text -match '(\d+) updates? can be applied') { $updates.Total = [int] $Matches[1] }
        if ($text -match '(\d+) of these updates? (are|is a) standard security') { $updates.Security = [int] $Matches[1] }
    }
    [pscustomobject] @{
        Host        = [System.Net.Dns]::GetHostName()
        Os          = $os
        Kernel      = (& uname -r 2>$null)
        Uptime      = [timespan]::FromSeconds($uptimeSeconds)
        Load        = $load
        Cpus        = [Environment]::ProcessorCount
        MemoryTotal = $memory['MemTotal']
        MemoryUsed  = $memory['MemTotal'] - $memory['MemAvailable']
        Disks       = Get-DiskUse
        FailedUnits = $failed
        Updates     = $updates
        LastLogin   = Get-LastLogin
    }
}
#endregion

$data = Get-OverviewData -Sample:$Sample
$memoryPercent = if ($data.MemoryTotal -gt 0) { [math]::Round(100 * $data.MemoryUsed / $data.MemoryTotal) } else { 0 }
$invariant = [System.Globalization.CultureInfo]::InvariantCulture
# Percentages the way every ConsoleMonster widget writes them (the bar chart below too): whole percents rounded down, "61%".
$percentText = { param([double] $Value) '{0}%' -f [math]::Floor([math]::Max(0, [math]::Min(100, $Value))) }
$facts = [ordered] @{
    'OS'         = $data.Os
    'Kernel'     = $data.Kernel
    'Uptime'     = '{0} days {1} h {2} min' -f $data.Uptime.Days, $data.Uptime.Hours, $data.Uptime.Minutes
    'Load'       = (($data.Load | ForEach-Object { $_.ToString('0.00', $invariant) }) -join ' / ') + " ($($data.Cpus) CPUs)"
    'Memory'     = '{0} of {1} GB ({2})' -f ($data.MemoryUsed / 1GB).ToString('0.0', $invariant), ($data.MemoryTotal / 1GB).ToString('0.0', $invariant), (& $percentText $memoryPercent)
    'Last login' = $data.LastLogin
}
$health = [ordered] @{}
$health["Memory $(& $percentText $memoryPercent)"] = if ($memoryPercent -ge 90) { 'fail' } elseif ($memoryPercent -ge 75) { 'warn' } else { 'pass' }
foreach ($mount in $data.Disks.Keys) {
    $percent = $data.Disks[$mount]
    $health["Disk $mount $(& $percentText $percent)"] = if ($percent -ge 90) { 'fail' } elseif ($percent -ge 80) { 'warn' } else { 'pass' }
}
if ($data.FailedUnits.Count -eq 0) {
    $health['No failed units'] = 'pass'
}
foreach ($unit in $data.FailedUnits) {
    $health["Failed: $unit"] = 'fail'
}
if ($null -ne $data.Updates.Total) {
    $security = if ($null -ne $data.Updates.Security) { " ($($data.Updates.Security) security)" } else { '' }
    $health["$($data.Updates.Total) updates$security"] = if ($data.Updates.Security -gt 0) { 'warn' } elseif ($data.Updates.Total -gt 0) { 'info' } else { 'pass' }
}
if ($data.Updates.RebootRequired) {
    $health['Reboot required'] = 'warn'
}

$common = @{}
if ($Width -gt 0) {
    $common.Width = $Width
}
Write-ConsoleRule @common -Title "[bold]$($data.Host)[/] [muted]· $(Get-Date -Format 'yyyy-MM-dd HH:mm')[/]"
New-ConsoleColumns -Gap 2 -Content {
    New-ConsolePanel -Title 'System' -Widget (New-ConsoleKeyValue -Data $facts)
    New-ConsolePanel -Title 'Health' -Widget (New-ConsoleStatusList -Data $health)
} | Write-ConsoleWidget @common
# Disk use with thresholds: 80 % and more is a warning, 90 % and more critical.
New-ConsolePanel -Title 'Disk use' -Widget (New-ConsoleBarChart -Data $data.Disks -Unit '%' -Warning 80 -Critical 90) | Write-ConsoleWidget @common
