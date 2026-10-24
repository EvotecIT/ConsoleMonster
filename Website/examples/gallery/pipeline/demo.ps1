[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 4)] [int] $Step = 0
)

Import-Module ConsoleMonster
# All values are synthetic. This example never connects to a host.

$stages = @('Restore', 'Compile', 'Test', 'Package')
function New-DemoFrame($Tick) {
    New-ConsoleRows -Gap 1 {
        '[bold]Build pipeline[/] [muted]· dry run[/]'
        New-ConsolePanel -Title 'Release checklist' {
            foreach ($index in 0..3) {
                $status = if ($index -lt $Tick) { 'pass' } elseif ($index -eq $Tick) { 'accent' } else { 'muted' }
                $label = if ($index -lt $Tick) { 'complete' } elseif ($index -eq $Tick) { 'running' } else { 'pending' }
                "[$status]$($stages[$index])[/]  $label"
            }
        }
        New-ConsoleProgress -Label Overall -Value $Tick -Total 4
        New-ConsoleKeyValue -Data ([ordered] @{
            'Project' = 'Orbit.Tools'
            'Version' = '1.2.0'
            'Artifacts' = './artifacts/'
            'Publish' = 'disabled'
        })
        '[muted]Nothing is built, signed or published. Ctrl+C stops the live run.[/]'
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
