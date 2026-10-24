Import-Module ConsoleMonster

New-ConsoleLogRegion -Height 5 -Content {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -AtSeconds 2
    New-ConsoleLogEntry 'DC02: replication partner DC05 last success 3h ago' -Level Warning -AtSeconds 31
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -AtSeconds 44
    New-ConsoleLogEntry 'Collected 186 rule inputs from DC01' -Level Info -AtSeconds 52
    New-ConsoleLogEntry 'Using cached schema snapshot' -Level Verbose -AtSeconds 53
} | Show-Console
