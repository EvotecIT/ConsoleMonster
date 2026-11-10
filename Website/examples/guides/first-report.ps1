Import-Module ConsoleMonster

# 1. Data: invented results for four servers; nothing is queried or changed.
$servers = @(
    [pscustomobject] @{ Name = 'fs01.contoso.example'; Role = 'File'; Disk = 62; Status = 'Pass' }
    [pscustomobject] @{ Name = 'fs02.contoso.example'; Role = 'File'; Disk = 81; Status = 'Warn' }
    [pscustomobject] @{ Name = 'sql01.contoso.example'; Role = 'Database'; Disk = 93
        Status = 'Fail' }
    [pscustomobject] @{ Name = 'web01.contoso.example'; Role = 'Web'; Disk = 40; Status = 'Pass' }
)
$passed = @($servers | Where-Object Status -EQ 'Pass').Count
$warnings = @($servers | Where-Object Status -EQ 'Warn').Count
$failed = @($servers | Where-Object Status -EQ 'Fail').Count

# 2. Layout: a title, a facts line, the table and the disk bars, one below another.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[accent bold]Nightly server report[/] [muted]· 2026-11-09[/]'
    New-ConsoleFacts -Content {
        @{ Value = $passed; Label = 'healthy'; Role = 'Pass' }
        @{ Value = $warnings; Label = 'warning'; Role = 'Warn' }
        @{ Value = $failed; Label = 'failed'; Role = 'Fail' }
        @{ Value = $servers.Count; Label = 'servers' }
    }
    New-ConsoleTable -InputObject $servers -Property Name, Role, Disk, Status -StatusProperty Status
    New-ConsolePanel -Title 'Disk used' -HeaderRight '% of capacity' -Content {
        $chartParameters = @{
            Property = 'Disk'
            Label = 'Name'
            Unit = '%'
            Maximum = 100
            Warning = 80
            Critical = 90
        }
        $servers | New-ConsoleChart @chartParameters
    }
}

# 3. Print it once.
$report | Show-Console
