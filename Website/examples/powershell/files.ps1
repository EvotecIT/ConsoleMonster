Import-Module ConsoleMonster

# Invented package artifacts with fixed sizes and UTC timestamps.
$recordedAt = [datetimeoffset] '2026-10-21T12:00:00Z'
$files = @(
    [pscustomobject] @{
        Name = 'ConsoleMonster.dll'; Length = 1048576
        LastWriteTime = $recordedAt; Extension = '.dll'
    }
    [pscustomobject] @{
        Name = 'ConsoleMonster.deps.json'; Length = 4096
        LastWriteTime = $recordedAt; Extension = '.json'
    }
    [pscustomobject] @{
        Name = 'start.ps1'; Length = 512
        LastWriteTime = $recordedAt; Extension = '.ps1'
    }
)
$files | Show-ConsoleTable -Property Name, Length, LastWriteTime `
    -GroupBy Extension -Presentation @{ TimeZone = [TimeZoneInfo]::Utc }
