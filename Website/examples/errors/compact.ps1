Import-Module ConsoleMonster

Enable-ConsoleFormatting -CompactErrors
try {
    # example-source-guard: intentional-error int.Parse production
    [int]::Parse('production')
} catch {
    $_ | Show-Console -Compact -View Error
}
Disable-ConsoleFormatting -Feature Errors
