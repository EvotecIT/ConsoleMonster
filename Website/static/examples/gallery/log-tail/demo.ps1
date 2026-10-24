[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 4)] [int] $Step = 0
)

Import-Module ConsoleMonster
# All values are synthetic. This example never connects to a host.

$messages = @(
    '[muted]09:41:00[/] [accent]INFO[/]  Worker started'
    '[muted]09:41:01[/] [pass]INFO[/]  Loaded 12 queued jobs'
    '[muted]09:41:02[/] [warn]WARN[/]  Cache cold; using local fallback'
    '[muted]09:41:03[/] [pass]INFO[/]  Cache warm; queue draining'
    '[muted]09:41:04[/] [pass]INFO[/]  Batch complete; no jobs lost'
)
function New-DemoFrame($Tick) {
    New-ConsoleRows -Gap 1 {
        '[bold]Application log[/] [muted]· synthetic events[/]'
        New-ConsolePanel -Title './logs/worker.log' {
            foreach ($line in $messages[0..$Tick]) { $line }
        }
        "[muted]$($Tick + 1) / 5 events · bounded sample · Ctrl+C stops the live run[/]"
    }
}

if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-DemoFrame $Step | Write-ConsoleWidget
    return
}
$frames = @(0..4 | ForEach-Object { New-DemoFrame $_ })
$screen = New-ConsoleRegion -Name demo -Widget $frames[0]
Start-ConsoleDashboard $screen -ArgumentList (, $frames) {
    param($Frames)
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) { break }
        Set-ConsoleRegion -Name demo -Widget $frame
        Start-Sleep -Milliseconds 650
    }
}
