Import-Module ConsoleMonster

$recording = @{
    Destination = './command-review.html'
    Title = 'Command review'
    Width = 88
    Height = 10
}

Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Inspect'
    git --version

    Add-ConsoleRecordingMarker 'Report'
    Write-Host 'Review complete' -ForegroundColor Green
} @recording