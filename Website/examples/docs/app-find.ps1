Import-Module ConsoleMonster

$lines = @(
    @{ Level = 'info'; Message = 'Deploy 4.12.2 started' }
    @{ Level = 'info'; Message = 'web-01 drained' }
    @{ Level = 'warn'; Message = 'web-01 health check timeout, retrying' }
    @{ Level = 'info'; Message = 'web-01 updated' }
    @{ Level = 'warn'; Message = 'web-02 health check timeout, retrying' }
    @{ Level = 'info'; Message = 'web-02 updated' }
    @{ Level = 'pass'; Message = 'Deploy complete' }
)
Show-ConsoleApp -Content {
    '[bold]Deploy 4.12.2[/] [muted]· / finds text[/]'
    New-ConsoleLogRegion -WidgetId deploy.log -NoElapsed -Height 8 -Content { $lines }
}