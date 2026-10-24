Import-Module ConsoleMonster

# Relative dates count from CONSOLEMONSTER_NOW instead of the clock, so a recording says the same thing every time.
$env:CONSOLEMONSTER_NOW = '2026-10-21T12:00:00Z'

$reports = @(
    [pscustomobject] @{ Name = 'Mail flow'; Changed = [datetimeoffset] '2026-10-21T11:58:00Z' }
    [pscustomobject] @{ Name = 'Replication'; Changed = [datetimeoffset] '2026-10-21T09:00:00Z' }
    [pscustomobject] @{ Name = 'Certificates'; Changed = [datetimeoffset] '2026-10-18T12:00:00Z' }
)
$reports | Show-ConsoleTable -Property Name, @{ Name = 'Changed'; Presenter = 'DateRelative' }

Remove-Item Env:CONSOLEMONSTER_NOW