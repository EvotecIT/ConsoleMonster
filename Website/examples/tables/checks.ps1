Import-Module ConsoleMonster

# Resolve advanced C# types without registering global type accelerators.
$CellPresentersType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.CellPresenters', $true)

$rows = @(
    [pscustomobject] @{
        Check = 'SPF'
        Status = [ConsoleMonster.Styling.StatusKind]::Pass
        Detail = '9 DNS lookups (limit 10); ends with -all'
        Took = [timespan]::FromMilliseconds(84)
        RFC = '7208'
    }
    [pscustomobject] @{
        Check = 'DMARC'
        Status = [ConsoleMonster.Styling.StatusKind]::Pass
        Detail = 'p=reject, aggregate and forensic reports configured'
        Took = [timespan]::FromMilliseconds(41)
        RFC = '7489'
    }
    [pscustomobject] @{
        Check = 'DKIM'
        Status = [ConsoleMonster.Styling.StatusKind]::Warn
        Detail = 'selector2 key is 1024-bit; selector1 is 2048-bit. Rotate selector2 to a 2048-bit key'
        Took = [timespan]::FromMilliseconds(212)
        RFC = '6376'
    }
    [pscustomobject] @{
        Check = 'MTA-STS'
        Status = [ConsoleMonster.Styling.StatusKind]::Fail
        Detail = 'policy file returns 404 at https://mta-sts.evotec.xyz/.well-known/mta-sts.txt'
        Took = [timespan]::FromSeconds(1.4)
        RFC = '8461'
    }
    [pscustomobject] @{
        Check = 'TLS-RPT'
        Status = [ConsoleMonster.Styling.StatusKind]::Skip
        Detail = 'not published'
        Took = [timespan]::FromMilliseconds(22)
        RFC = '8460'
    }
    [pscustomobject] @{
        Check = 'DNSSEC'
        Status = [ConsoleMonster.Styling.StatusKind]::Warn
        Detail = 'zone is not signed'
        Took = [timespan]::FromMilliseconds(95)
        RFC = '4033'
    }
    [pscustomobject] @{
        Check = 'BIMI'
        Status = [ConsoleMonster.Styling.StatusKind]::Info
        Detail = 'no record; optional'
        Took = [timespan]::FromMilliseconds(19)
        RFC = $null
    }
)

$rows | Show-ConsoleTable -Title '[bold]evotec.xyz[/] [muted]· mail security checks[/]' -Property @(
    @{ Name = 'Check'; Key = $true; Overflow = 'Never' }
    @{ Name = 'Status'; Priority = 10; Presenter = $CellPresentersType::Status }
    @{ Name = 'Detail'; Priority = 5; Overflow = 'Wrap'; MaxLines = 2; Flexible = $true; MinWidth = 18 }
    @{ Name = 'Took'; Priority = 1; Presenter = 'Duration' }
    @{ Name = 'RFC'; Priority = 0; Overflow = 'Never' }
)
