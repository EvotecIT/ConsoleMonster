# Regenerate the site's real terminal output and reject stale committed artifacts in CI.
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repository
try {
    dotnet run --project Website/tools/SiteFrames -c Release -- render Website
    if ($LASTEXITCODE -ne 0) { throw 'SiteFrames rendering failed.' }
    $generated = @(
        'Website/data', 'Website/static/frames', 'Website/static/replays',
        'Website/static/recordings', 'Website/static/site', 'Website/static/css/frames.css',
        'Website/static/assets/player', 'Website/tools/SiteFrames/glyphs.txt'
    )
    $changes = @(git status --porcelain --untracked-files=all -- $generated)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect generated website output.' }
    if ($changes.Count) { throw "Run SiteFrames render and commit the resulting output:`n$($changes -join "`n")" }
} finally {
    Pop-Location
}
