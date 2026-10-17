param(
    [ValidateSet('Dashboard', 'Progress', 'Both')][string] $Part = 'Both',
    [int] $Delay = 120
)
# An inline live area: progress and lanes stay at the bottom while output scrolls above, with no alternate screen.
# The first part is a small dashboard with -Inline; the second runs an unchanged Write-Progress loop through
# Use-ConsoleProgress. Nothing is really deployed; the data is invented.
Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force

if ($Part -in 'Dashboard', 'Both') {
    Write-Host 'Deploying release 2026.10 to three servers'
    $dashboard = New-ConsoleDashboard -Title 'Deploy' {
        New-ConsoleProgress -Name overall -Label 'Overall' -Total 12
        New-ConsoleLanes -Name lanes
    }
    Start-ConsoleDashboard $dashboard -Inline -ArgumentList $Delay {
        param($Delay)
        foreach ($server in 'WEB01', 'WEB02', 'API01') {
            Update-ConsoleLane -Key $server -Status Running -Detail 'copying files' -Total 4
            foreach ($step in 'copy', 'configure', 'restart', 'verify') {
                Start-Sleep -Milliseconds ($Delay * 3)
                Update-ConsoleLane -Key $server -Detail $step -Result Pass
                Update-ConsoleProgress -Name overall -Increment 1
            }
            Write-Host "$server deployed"
            Update-ConsoleLane -Key $server -Complete
        }
    }
    Write-Host 'Done.'
}

if ($Part -in 'Progress', 'Both') {
    Use-ConsoleProgress {
        $files = 1..40
        foreach ($i in $files) {
            Write-Progress -Id 1 -Activity 'Backup' -Status "File $i of $($files.Count)" -PercentComplete ($i * 100 / $files.Count) -SecondsRemaining (($files.Count - $i) * $Delay / 1000)
            Write-Progress -Id 2 -ParentId 1 -Activity 'Compressing' -Status "chunk $(($i % 4) + 1) of 4" -PercentComplete ((($i % 4) + 1) * 25)
            if ($i % 10 -eq 0) { Write-Host "checkpoint after $i files" }
            Start-Sleep -Milliseconds $Delay
        }
        Write-Progress -Id 1 -Activity 'Backup' -Completed
        "backed up $($files.Count) files"
    }
}
