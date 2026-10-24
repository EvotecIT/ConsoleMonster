# Regenerate the site's real terminal output and reject stale committed artifacts in CI.
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Push-Location $repository
try {
    dotnet run --project Website/tools/SiteFrames -c Release -- render Website
    if ($LASTEXITCODE -ne 0) { throw 'SiteFrames rendering failed.' }
    $generated = @(
        'Website/data', 'Website/content/pages/demos', 'Website/static/frames', 'Website/static/replays',
        'Website/static/recordings', 'Website/static/site', 'Website/static/css/frames.css',
        'Website/static/assets/player', 'Website/tools/SiteFrames/glyphs.txt'
    )
    $changes = @(git status --porcelain --untracked-files=all -- $generated)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect generated website output.' }
    if ($changes.Count) {
        $reportRoot = Join-Path $PSScriptRoot '../_reports'
        New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
        $changes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reportRoot 'site-frames-drift.json') -Encoding utf8
        $changes | ForEach-Object { Write-Host $_ }
        throw 'Generated website output differs. Run SiteFrames render and commit the resulting output.'
    }
} finally {
    Pop-Location
}
