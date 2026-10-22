Import-Module ConsoleMonster

New-ConsolePanel -Title '[bold]Forest[/]' -Caption 'ad.adatum.com' -Footer '[muted]checked 14:30[/]' {
    New-ConsoleText -Markup 'Four controllers answered. [warn]DC02[/] lags by 42 minutes.'
} | Write-ConsoleWidget
