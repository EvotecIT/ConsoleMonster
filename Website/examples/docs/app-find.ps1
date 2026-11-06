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
# LogViewport has no cmdlet; PowerShell takes the type from the module's assembly.
$logViewport = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.LogViewport')
$log = $logViewport::new('Log')
$log.Label = 'deploy.log'
$log.ShowElapsed = $false
$log.Height = 8
foreach ($line in $lines) { $log.Write($line.Message, $line.Level) }
Show-ConsoleApp -Content {
    '[bold]Deploy 4.12.2[/] [muted]· / finds text[/]'
    $log
}