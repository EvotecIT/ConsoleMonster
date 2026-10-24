Import-Module ConsoleMonster

New-ConsolePanel -Title '[bold]Forest[/]' -HeaderRight 'ad.adatum.com' -Caption '[muted]checked 14:30[/]' -Content {
    New-ConsoleText -Markup 'Four controllers answered. [warn]DC02[/] lags by 42 minutes.'
} | Show-Console
