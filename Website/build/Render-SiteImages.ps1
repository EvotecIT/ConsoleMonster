# Screenshots the pages tools/SiteFrames writes to _temp/images/ into the raster images the site needs:
# static/site/social-card.png (1200x630), static/apple-touch-icon.png (180x180) and static/site/icon-512.png, from which
# static/site/icon-192.png and static/favicon.ico (16, 32 and 48 px) are scaled, and the README's images listed in
# _temp/images/readme.json into static/readme/ (at twice their CSS size, for sharp text). Needs Microsoft Edge.
# Run after `dotnet run --project tools/SiteFrames -c Release -- render .` when the card's frame or the logo changes.
param(
    [string]$Edge = "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$pages = Join-Path $root '_temp/images'
$profileDir = Join-Path $pages 'edge-profile'
$targets = @(
    @{ Page = 'social-card.html'; Out = (Join-Path $root 'static/site/social-card.png'); Width = 1200; Height = 630; Scale = 1 }
    @{ Page = 'touch-icon.html'; Out = (Join-Path $root 'static/apple-touch-icon.png'); Width = 180; Height = 180; Scale = 1 }
    @{ Page = 'app-icon.html'; Out = (Join-Path $root 'static/site/icon-512.png'); Width = 512; Height = 512; Scale = 1 }
)
# The README's images: one page per frame and theme, written by the site tool's render with its size.
$readme = Join-Path $pages 'readme.json'
if (Test-Path -LiteralPath $readme) {
    New-Item -ItemType Directory -Force -Path (Join-Path $root 'static/readme') | Out-Null
    $targets += foreach ($image in (Get-Content -LiteralPath $readme -Raw | ConvertFrom-Json)) {
        @{ Page = $image.page; Out = (Join-Path $root $image.out); Width = [int] $image.width; Height = [int] $image.height; Scale = 2 }
    }
}
New-Item -ItemType Directory -Force -Path (Join-Path $root 'static/site') | Out-Null
try {
    foreach ($t in $targets) {
        $url = ([Uri](Join-Path $pages $t.Page)).AbsoluteUri
        if (Test-Path -LiteralPath $t.Out) { Remove-Item -LiteralPath $t.Out -Force }
        $arguments = @('--headless', '--disable-gpu', '--hide-scrollbars', "--user-data-dir=$profileDir", "--window-size=$($t.Width),$($t.Height)", "--force-device-scale-factor=$($t.Scale)", '--virtual-time-budget=2000', "--screenshot=$($t.Out)", $url)
        Start-Process -FilePath $Edge -ArgumentList $arguments -WindowStyle Hidden | Out-Null
        # Edge hands the work to a background process; wait for the file instead of the launcher.
        $deadline = (Get-Date).AddSeconds(60)
        while ((Get-Date) -lt $deadline -and -not ((Test-Path -LiteralPath $t.Out) -and (Get-Item -LiteralPath $t.Out).Length -gt 0)) { Start-Sleep -Milliseconds 300 }
        Start-Sleep -Milliseconds 800
        if (-not ((Test-Path -LiteralPath $t.Out) -and (Get-Item -LiteralPath $t.Out).Length -gt 0)) { throw "No non-empty screenshot for $($t.Page)." }
        # A headless window has a minimum size; a screenshot of another size means the page did not render as intended.
        $shot = [System.Drawing.Image]::FromFile($t.Out)
        try {
            if ($shot.Width -ne $t.Width * $t.Scale -or $shot.Height -ne $t.Height * $t.Scale) { throw "$($t.Page) rendered at $($shot.Width)x$($shot.Height), expected $($t.Width * $t.Scale)x$($t.Height * $t.Scale)." }
        } finally {
            $shot.Dispose()
        }
        Write-Output $t.Out
    }
} finally {
    # Close only the headless instances that used this script's profile.
    $escaped = [regex]::Escape($profileDir)
    Get-CimInstance Win32_Process -Filter "name='msedge.exe'" | Where-Object { $_.CommandLine -match $escaped } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}

# The smaller icons are scaled from the 512 px render: the 192 px app icon, and favicon.ico for browsers and tools that
# ask for /favicon.ico (PNG images inside an ICO container, which every current browser and Windows read).
function Get-ScaledPng([System.Drawing.Image] $Source, [int] $Size) {
    $bitmap = New-Object System.Drawing.Bitmap -ArgumentList $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($Source, 0, 0, $Size, $Size)
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return , $stream.ToArray()
    } finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}
$source = [System.Drawing.Image]::FromFile((Join-Path $root 'static/site/icon-512.png'))
try {
    [System.IO.File]::WriteAllBytes((Join-Path $root 'static/site/icon-192.png'), (Get-ScaledPng $source 192))
    $sizes = @(16, 32, 48)
    $pngs = foreach ($size in $sizes) { , (Get-ScaledPng $source $size) }
    $ico = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter -ArgumentList $ico
    # ICONDIR (reserved, type 1 = icon, count), then one 16-byte ICONDIRENTRY per image, then the PNG payloads.
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $writer.Write([byte]$sizes[$i]); $writer.Write([byte]$sizes[$i]); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$pngs[$i].Length); $writer.Write([uint32]$offset)
        $offset += $pngs[$i].Length
    }
    foreach ($png in $pngs) { $writer.Write($png) }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes((Join-Path $root 'static/favicon.ico'), $ico.ToArray())
    Write-Output (Join-Path $root 'static/site/icon-192.png')
    Write-Output (Join-Path $root 'static/favicon.ico')
} finally {
    $source.Dispose()
}
