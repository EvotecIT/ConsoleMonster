Import-Module ConsoleMonster

Enable-ConsoleFormatting -ErrorLayout Compact
try {
    # example-source-guard: intentional-error int.Parse production
    [int]::Parse('production')
} catch {
    $_ | Show-Console -ErrorLayout Compact -View Error
}
Disable-ConsoleFormatting -Feature Errors
