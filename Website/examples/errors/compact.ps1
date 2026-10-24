Import-Module ConsoleMonster

Enable-ConsoleFormatting -Feature Errors
try {
    # example-source-guard: intentional-error int.Parse production
    [int]::Parse('production')
} catch {
    $_ | Format-ConsoleError -Compact
}
Disable-ConsoleFormatting -Feature Errors
