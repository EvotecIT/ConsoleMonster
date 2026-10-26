Import-Module ConsoleMonster

# Invented package artifacts, with fixed sizes and UTC timestamps for a repeatable example.
$files = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.dll'; Length = 1048576; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z'; Extension = '.dll' }
    [pscustomobject] @{ Name = 'ConsoleMonster.deps.json'; Length = 4096; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z'; Extension = '.json' }
    [pscustomobject] @{ Name = 'start.ps1'; Length = 512; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z'; Extension = '.ps1' }
)
$files | Show-ConsoleTable -Property Name, Length, LastWriteTime -GroupBy Extension -Presentation @{ TimeZone = [TimeZoneInfo]::Utc }
