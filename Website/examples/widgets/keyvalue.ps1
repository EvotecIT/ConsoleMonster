Import-Module ConsoleMonster

New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
    Module     = '[bold]ConsoleMonster[/]'
    Version    = '0.0.4 [muted]→[/] [bold]2.0.0[/]'
    Frameworks = 'netstandard2.0, net472, net8.0'
}) -Content {
    New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
    New-ConsoleKeyValueItem 'Tests' '1 skipped on PowerShell 7' -Status Warn
} | Show-Console
