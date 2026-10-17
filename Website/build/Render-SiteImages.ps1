# Screenshots the pages tools/SiteFrames writes to _temp/images/ into the raster images the site needs:
# static/site/social-card.png (1200x630) and static/apple-touch-icon.png (180x180). Needs Microsoft Edge.
# Run after `dotnet run --project tools/SiteFrames -c Release -- render .` when the card's frame or the logo changes.
param(
    [string]$Edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$images = Join-Path $root '_temp/images'
$profileDir = Join-Path $images 'edge-profile'
$targets = @(
    @{ Page = 'social-card.html'; Out = (Join-Path $root 'static/site/social-card.png'); Size = '1200,630' }
    @{ Page = 'touch-icon.html'; Out = (Join-Path $root 'static/apple-touch-icon.png'); Size = '180,180' }
)
New-Item -ItemType Directory -Force -Path (Join-Path $root 'static/site') | Out-Null
foreach ($t in $targets) {
    $url = ([Uri](Join-Path $images $t.Page)).AbsoluteUri
    if (Test-Path -LiteralPath $t.Out) { Remove-Item -LiteralPath $t.Out -Force }
    $arguments = @('--headless', '--disable-gpu', '--hide-scrollbars', "--user-data-dir=$profileDir", "--window-size=$($t.Size)", '--force-device-scale-factor=1', '--virtual-time-budget=2000', "--screenshot=$($t.Out)", $url)
    Start-Process -FilePath $Edge -ArgumentList $arguments -WindowStyle Hidden | Out-Null
    # Edge hands the work to a background process; wait for the file instead of the launcher.
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline -and -not ((Test-Path -LiteralPath $t.Out) -and (Get-Item -LiteralPath $t.Out).Length -gt 0)) { Start-Sleep -Milliseconds 300 }
    Start-Sleep -Milliseconds 800
    if (-not (Test-Path -LiteralPath $t.Out)) { throw "No screenshot for $($t.Page)." }
    Write-Output $t.Out
}
# Close only the headless instances that used this script's profile.
$escaped = [regex]::Escape($profileDir)
Get-CimInstance Win32_Process -Filter "name='msedge.exe'" | Where-Object { $_.CommandLine -match $escaped } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
