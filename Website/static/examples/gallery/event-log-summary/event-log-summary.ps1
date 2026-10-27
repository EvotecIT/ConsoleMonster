# Summarise a day of Windows event logs: trend, noisy sources, top IDs and the critical events.
Import-Module -Name ConsoleMonster

# Data: fictional counts already grouped from the System and Application logs of six servers.
$errorsPerHour = @(4, 3, 5, 2, 3, 4, 6, 9, 14, 11, 8, 7, 6, 9, 31, 44, 27, 12, 8, 6, 5, 4, 6, 5)
$warningsPerHour = @(12, 10, 11, 9, 10, 12, 15, 22, 30, 26, 21, 19, 18, 20, 41, 52, 38, 24, 19, 16, 14, 13, 15, 14)
$sources = @(
    [pscustomobject] @{ Source = 'Service Control Manager'; Errors = 74 }
    [pscustomobject] @{ Source = 'Microsoft-Windows-DNS-Client'; Errors = 52 }
    [pscustomobject] @{ Source = 'MSSQLSERVER'; Errors = 41 }
    [pscustomobject] @{ Source = 'Schannel'; Errors = 23 }
    [pscustomobject] @{ Source = 'Disk'; Errors = 17 }
    [pscustomobject] @{ Source = 'Kernel-Power'; Errors = 4 }
)
$eventIds = @(
    [pscustomobject] @{ Id = 7031; Count = 48; Meaning = 'Service terminated unexpectedly' }
    [pscustomobject] @{ Id = 1014; Count = 52; Meaning = 'Name resolution timed out' }
    [pscustomobject] @{ Id = 17806; Count = 37; Meaning = 'SSPI handshake failed' }
    [pscustomobject] @{ Id = 36887; Count = 23; Meaning = 'Fatal alert received' }
    [pscustomobject] @{ Id = 153; Count = 17; Meaning = 'IO operation retried' }
)
$critical = @(
    [pscustomobject] @{ Time = '14:52'; Host = 'sql-01'; Source = 'Kernel-Power'; Id = 41; Message = 'Rebooted without a clean shutdown' }
    [pscustomobject] @{ Time = '15:07'; Host = 'sql-01'; Source = 'MSSQLSERVER'; Id = 824; Message = 'Logical consistency I/O error on tempdb' }
    [pscustomobject] @{ Time = '15:31'; Host = 'fs-02'; Source = 'Disk'; Id = 7; Message = 'Bad block on \Device\Harddisk1\DR1' }
    [pscustomobject] @{ Time = '16:10'; Host = 'web-03'; Source = 'Kernel-Power'; Id = 41; Message = 'Rebooted without a clean shutdown' }
)

# Behaviour: hour 0 is midnight; the busiest hour and the totals come from the same counts the charts use.
$errorTotal = ($errorsPerHour | Measure-Object -Sum).Sum
$warningTotal = ($warningsPerHour | Measure-Object -Sum).Sum
$peak = [array]::IndexOf($errorsPerHour, [int] ($errorsPerHour | Measure-Object -Maximum).Maximum)
$topIds = @($eventIds | Sort-Object -Property Count -Descending)

# Layout: headline, the 24-hour trend, the noisiest sources beside the top IDs, then criticals.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Event log summary[/] [muted]· 6 servers · Sunday 2026-10-25[/]'
    "[fail]$($critical.Count) critical[/]   [warn]$errorTotal errors[/]   [muted]$warningTotal warnings · busiest hour $('{0:00}:00' -f $peak)[/]"
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Errors per hour' -Content {
            New-ConsoleChart -Type Sparkline -Label 'errors' -Height 3 -Role Fail -Values $errorsPerHour
        }
        New-ConsolePanel -Title 'Warnings per hour' -Content {
            New-ConsoleChart -Type Sparkline -Label 'warnings' -Height 3 -Role Warn -Values $warningsPerHour
        }
    }
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Noisiest sources' -Content {
            $sources | ForEach-Object -Process { [pscustomobject] @{ Label = $_.Source; Value = $_.Errors } } |
                New-ConsoleChart -Type Bar
        }
        New-ConsolePanel -Title 'Top event IDs' -Content {
            New-ConsoleTable -InputObject $topIds -Property Id, Count, Meaning -Border None -Expand
        }
    }
    New-ConsolePanel -Title 'Critical events' -Content {
        New-ConsoleTable -InputObject $critical -Property Time, Host, Source, Id, Message -Border None -Expand
    }
}

# Behaviour: print once; feed it Get-WinEvent results grouped the same way for a real day.
$report | Show-Console
