# Find stale Active Directory accounts and see which passwords expire this month.
Import-Module -Name ConsoleMonster

# Data: fictional accounts and a fixed report date; no domain controller is queried.
$today = [datetime]'2026-11-02'
$accounts = @(
    [pscustomobject] @{ Account = 'amy.lee'; Department = 'Finance'; LastLogon = [datetime]'2026-10-30'; PasswordSet = [datetime]'2026-08-20'; PasswordExpires = [datetime]'2026-11-18' }
    [pscustomobject] @{ Account = 'ben.ortiz'; Department = 'IT'; LastLogon = [datetime]'2026-11-01'; PasswordSet = [datetime]'2026-08-06'; PasswordExpires = [datetime]'2026-11-04' }
    [pscustomobject] @{ Account = 'svc-backup'; Department = 'Service'; LastLogon = [datetime]'2026-11-02'; PasswordSet = [datetime]'2025-03-01'; PasswordExpires = $null }
    [pscustomobject] @{ Account = 'cara.diaz'; Department = 'HR'; LastLogon = [datetime]'2026-07-12'; PasswordSet = [datetime]'2026-05-30'; PasswordExpires = [datetime]'2026-08-28' }
    [pscustomobject] @{ Account = 'dan.reyes'; Department = 'Sales'; LastLogon = [datetime]'2026-08-25'; PasswordSet = [datetime]'2026-08-11'; PasswordExpires = [datetime]'2026-11-09' }
    [pscustomobject] @{ Account = 'contractor-07'; Department = 'External'; LastLogon = [datetime]'2026-05-02'; PasswordSet = [datetime]'2026-04-20'; PasswordExpires = [datetime]'2026-07-19' }
    [pscustomobject] @{ Account = 'eve.chen'; Department = 'Marketing'; LastLogon = [datetime]'2026-10-28'; PasswordSet = [datetime]'2026-08-14'; PasswordExpires = [datetime]'2026-11-12' }
    [pscustomobject] @{ Account = 'finn.park'; Department = 'IT'; LastLogon = [datetime]'2026-10-31'; PasswordSet = [datetime]'2026-08-28'; PasswordExpires = [datetime]'2026-11-26' }
    [pscustomobject] @{ Account = 'gia.rossi'; Department = 'Finance'; LastLogon = [datetime]'2026-09-01'; PasswordSet = [datetime]'2026-08-24'; PasswordExpires = [datetime]'2026-11-22' }
    [pscustomobject] @{ Account = 'training-01'; Department = 'Shared'; LastLogon = [datetime]'2026-02-14'; PasswordSet = [datetime]'2025-11-03'; PasswordExpires = $null }
    [pscustomobject] @{ Account = 'hal.novak'; Department = 'Legal'; LastLogon = [datetime]'2026-11-02'; PasswordSet = [datetime]'2026-08-10'; PasswordExpires = [datetime]'2026-11-08' }
)

# Behaviour: stale means no logon for 60 days or a password older than a year.
$stale = foreach ($account in $accounts) {
    $inactive = ($today - $account.LastLogon).Days
    $age = ($today - $account.PasswordSet).Days
    if ($inactive -gt 60 -or $age -gt 365) {
        $status = if ($inactive -gt 90 -or $age -gt 365) { 'Fail' } else { 'Warn' }
        [pscustomobject] @{ Account = $account.Account; Department = $account.Department; LastLogon = $account.LastLogon.ToString('yyyy-MM-dd'); Inactive = $inactive; PasswordAge = $age; Status = $status }
    }
}
$expiring = @($accounts | Where-Object -FilterScript { $_.PasswordExpires -ge $today -and $_.PasswordExpires -le $today.AddDays(30) } | Sort-Object -Property PasswordExpires)
$marks = @(
    [pscustomobject] @{ Date = [datetime]'2026-11-16'; Status = 'Fail'; Label = "Disable $(@($stale).Count) accounts" }
    foreach ($account in $expiring) {
        $status = if ($account.PasswordExpires -le $today.AddDays(7)) { 'Warn' } else { 'Info' }
        [pscustomobject] @{ Date = $account.PasswordExpires; Status = $status; Label = "$($account.Account) password" }
    }
) | Sort-Object -Property Date

# Layout: the accounts to act on, then the month of expiries beside the dated list.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Account hygiene[/] [muted]· adatum.example.test · as of 2026-11-02[/]'
    New-ConsoleTable -InputObject $stale -Property Account, Department, LastLogon, Inactive, PasswordAge, Status -Expand
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'November' -Content {
            $marks | New-ConsoleCalendar -Year 2026 -Month 11 -FirstDayOfWeek Monday -Today $today
        }
        New-ConsolePanel -Title 'Coming up' -Content {
            New-ConsoleStatusList -Content {
                foreach ($mark in $marks) {
                    [pscustomobject] @{ Label = $mark.Date.ToString('ddd dd MMM', [cultureinfo]'en-US'); Status = $mark.Status; Detail = $mark.Label }
                }
            }
        }
    }
}

# Behaviour: print once; confirm each stale account with its manager before disabling it.
$report | Show-Console
