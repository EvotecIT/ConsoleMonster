Import-Module ConsoleMonster

New-ConsoleLog -Height 5 {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    New-ConsoleLogEntry 'DC02: replication partner DC05 last success 3h ago' -Level Warning -At 31s
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
    New-ConsoleLogEntry 'Collected 186 rule inputs from DC01' -Level Info -At 52s
    New-ConsoleLogEntry 'Using cached schema snapshot' -Level Verbose -At 53s
} | Write-ConsoleWidget
