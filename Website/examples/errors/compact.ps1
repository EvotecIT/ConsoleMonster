Import-Module ConsoleMonster

Enable-ConsoleFormatting -Feature Errors
try {
    [int]::Parse('production')
} catch {
    $_ | Show-ConsoleError -Compact
}
Disable-ConsoleFormatting -Feature Errors
