Import-Module ConsoleMonster

Enable-ConsoleFormatting -Feature Errors
try {
    [int]::Parse('production')
} catch {
    $_ | Format-ConsoleError -Compact
}
Disable-ConsoleFormatting -Feature Errors
