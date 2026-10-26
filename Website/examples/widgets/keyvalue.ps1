Import-Module ConsoleMonster

New-ConsoleKeyValue -Markup -InputObject ([ordered] @{
    Module     = '[bold]ConsoleMonster[/]'
    Version    = '0.0.4 [muted]→[/] [bold]2.0.0[/]'
    Frameworks = 'netstandard2.0, net472, net8.0'
}) -Content {
    [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
    [pscustomobject]@{ Key = 'Tests'; Value = '1 skipped on PowerShell 7'; Status = 'Warn' }
} | Show-Console
