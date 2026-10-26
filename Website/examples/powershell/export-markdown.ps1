Import-Module ConsoleMonster

# Invented package artifacts, exported as a Markdown table for a report.
$files = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.dll'; Length = 1048576; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z' }
    [pscustomobject] @{ Name = 'ConsoleMonster.deps.json'; Length = 4096; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z' }
    [pscustomobject] @{ Name = 'start.ps1'; Length = 512; LastWriteTime = [datetimeoffset] '2026-10-21T12:00:00Z' }
)
$files | Show-ConsoleTable -Property Name, Length, LastWriteTime `
    -Presentation @{ TimeZone = [TimeZoneInfo]::Utc } -AsWidget |
    Export-ConsoleFrame -Format Markdown
