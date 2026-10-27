Import-Module ConsoleMonster

New-ConsoleLogRegion -Height 5 -Content {
    [pscustomobject]@{ Message = 'Discovered 4 domain controllers in 2 domains'; Level = 'Info'; AtSeconds = 2 }
    [pscustomobject]@{ Message = 'DC02: replication partner DC05 last success 3h ago'; Level = 'Warning'; AtSeconds = 31 }
    [pscustomobject]@{ Message = 'DC02: USN rollback check failed (event 2095)'; Level = 'Error'; AtSeconds = 44 }
    [pscustomobject]@{ Message = 'Collected 186 rule inputs from DC01'; Level = 'Info'; AtSeconds = 52 }
    [pscustomobject]@{ Message = 'Using cached schema snapshot'; Level = 'Verbose'; AtSeconds = 53 }
} | Show-Console
