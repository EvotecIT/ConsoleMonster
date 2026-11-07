# Summarize a server estate in one dashboard frame: facts, meters, problems first, then the details.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$sites = @(
    [pscustomobject] @{ Site = 'London'; Servers = '22'; Healthy = '21' }
    [pscustomobject] @{ Site = 'Amsterdam'; Servers = '16'; Healthy = '15' }
    [pscustomobject] @{ Site = 'New York'; Servers = '10'; Healthy = '8' }
)
$backups = @(
    [pscustomobject] @{ Job = 'Nightly SQL'; 'Last run' = '02:10'; Size = '84 GB' }
    [pscustomobject] @{ Job = 'Weekly files'; 'Last run' = 'Sun 01:00'; Size = '312 GB' }
)

# Layout: the frame places each part for the terminal's width.
$frame = @{
    Title = 'Server estate'
    Fact = 'contoso.example', '48 servers', '3 sites'
    State = @(
        [pscustomobject] @{ State = 'Failing'; Count = 1; Word = 'failing' }
        [pscustomobject] @{ State = 'Warning'; Count = 3; Word = 'warning' }
    )
    Meter = @(
        [pscustomobject] @{ Label = 'Servers healthy'; Healthy = 44; Warning = 3; Failing = 1 }
        [pscustomobject] @{ Label = 'Backups current'; Healthy = 46; Whole = 48 }
        [pscustomobject] @{ Label = 'Oldest patch'; Value = '41 days'; Fact = 'FS-02'
            State = 'Warning' }
        [pscustomobject] @{ Label = 'Open alerts'; Value = 5 }
    )
    Attention = @(
        [pscustomobject] @{ State = 'Failing'
            Identity = 'SQL-03'
            Reason = 'Disk D: 97% full'
            Order = 1
                Values = '97%' }
        [pscustomobject] @{ State = 'Warning'
            Identity = 'FS-02'
            Reason = 'Patches 41 days old'
            Order = 2
                Values = '41 d' }
        [pscustomobject] @{ State = 'Warning'
            Identity = 'WEB-07'
            Reason = 'Certificate expires in 9 days'
                Order = 3
            Values = '9 d' }
    )
    AttentionNoun = 'server'
    AttentionOutOf = 48
    Section = @(
        [pscustomobject] @{ Title = 'Sites'; Facts = 'by location'
            Content = { $sites | New-ConsoleTable } }
        [pscustomobject] @{ Title = 'Backups'; Placement = 'Detail'
            Content = { $backups | New-ConsoleTable } }
    )
    Legend = [ordered] @{ Healthy = 'healthy'; Warning = 'warning'; Failing = 'failing' }
    FooterCount = '48 servers'
}

# Behaviour: print the frame once and return.
New-ConsoleDashboardFrame @frame | Show-Console