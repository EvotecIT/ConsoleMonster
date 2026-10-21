<#
.SYNOPSIS
Exports synthetic before/after frames for the everyday-formatting features.
.DESCRIPTION
Uses the module's normal formatters, native PowerShell format records and the shared frame exporter. Clipboard evidence
uses the actual SSH refusal path and never replaces the real clipboard. Product-specific module designs remain separate.
.PARAMETER OutputDirectory
Directory for text, HTML and Markdown review evidence.
.EXAMPLE
./Export-EverydayReviewFrames.ps1 -OutputDirectory ./review-frames
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $OutputDirectory)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
Import-Module (Join-Path $PSScriptRoot '../Fixtures/EverydayViews/EverydayViews.psd1')
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$rows = @(
    [pscustomobject]@{ Name = 'web-01'; State = 'Pass'; Site = 'Warsaw'; Url = 'https://example.com/web-01'; Path = 'C:\Reports\web-01.html' }
    [pscustomobject]@{ Name = 'sql-02'; State = 'Warning'; Site = 'Dublin'; Url = 'https://example.com/sql-02'; Path = 'C:\Reports\sql-02.html' }
    [pscustomobject]@{ Name = 'backup-03'; State = 'Fail'; Site = 'Warsaw'; Url = 'https://example.com/backup-03'; Path = 'C:\Reports\backup-03.html' }
)

function Save-Frame {
    param([string] $Name, [object] $Widget)
    $Widget | Export-ConsoleFrame -Format Text -Path (Join-Path $OutputDirectory ($Name + '.txt')) -Width 96
    $Widget | Export-ConsoleFrame -Format Html -Path (Join-Path $OutputDirectory ($Name + '.html')) -Width 96 -Title $Name -WindowChrome
}

foreach ($kind in 'Table', 'List', 'Wide') {
    $parameters = @{ Property = @('Name', 'State') }
    if ($kind -eq 'Wide') { $parameters = @{ Property = 'Name'; Column = 3 } }
    $command = Get-Command ("Microsoft.PowerShell.Utility\Format-$kind")
    $before = $rows | & $command @parameters | Out-String -Width 96
    Save-Frame "1-$($kind.ToLowerInvariant())-before" (New-ConsoleText $before.TrimEnd())
    $after = $rows | & $command @parameters | Out-ConsoleFormatCommand -AsWidget
    Save-Frame "1-$($kind.ToLowerInvariant())-after" $after
}

$alignmentRows = @(
    [pscustomobject]@{ Name = 'short'; Centered = 7; Count = 12 }
    [pscustomobject]@{ Name = 'longer-name'; Centered = 123; Count = 3456 }
)
$alignmentColumns = @('Name', @{ Name = 'Centered'; Expression = 'Centered'; Alignment = 'Center' }, 'Count')
$alignmentBefore = $alignmentRows | Microsoft.PowerShell.Utility\Format-Table -Property $alignmentColumns | Out-String -Width 96
Save-Frame '1-alignment-before' (New-ConsoleText $alignmentBefore.TrimEnd())
Save-Frame '1-alignment-after' ($alignmentRows | Microsoft.PowerShell.Utility\Format-Table -Property $alignmentColumns | Out-ConsoleFormatCommand -AsWidget)

$moduleBefore = Get-EverydayValue | Microsoft.PowerShell.Utility\Format-Table -Property Name, State, Site, HiddenDetail | Out-String -Width 96
Save-Frame '2-module-views-before' (New-ConsoleText $moduleBefore.TrimEnd())
$moduleAfter = Get-EverydayValue | Format-ConsoleTable -AsWidget
Save-Frame '2-module-views-after' $moduleAfter

$table = $rows | Format-ConsoleTable -Property Name, State, Site -AsWidget
Save-Frame '3-export-before' (New-ConsoleText ($rows | Format-ConsoleTable -Property Name, State, Site -AsString -Width 96))
Save-Frame '3-export-after' $table
$rows | Format-ConsoleTable -Property Name, State, Site -As Markdown -Width 96 | Set-Content -LiteralPath (Join-Path $OutputDirectory '3-export-result.md') -Encoding UTF8
$rows | Format-ConsoleTable -Property Name, State, Site -As Html -Width 96 | Set-Content -LiteralPath (Join-Path $OutputDirectory '3-export-result.html') -Encoding UTF8

$plain = $rows | Format-ConsoleTable -Property Name, State -AsString -Width 96
Save-Frame '3-clipboard-before' (New-ConsoleText $plain)
$savedSsh = [Environment]::GetEnvironmentVariable('SSH_CONNECTION', 'Process')
try {
    [Environment]::SetEnvironmentVariable('SSH_CONNECTION', 'synthetic-review-session', 'Process')
    $copyErrors = @()
    $plain | Copy-ConsoleOutput -As Markdown -ErrorAction SilentlyContinue -ErrorVariable copyErrors
    Save-Frame '3-clipboard-after-ssh' (New-ConsolePanel -Title 'Copy-ConsoleOutput over SSH' -Content (New-ConsoleText ($copyErrors[0].ToString())))
} finally { [Environment]::SetEnvironmentVariable('SSH_CONNECTION', $savedSsh, 'Process') }

$beforeLinks = $rows | Microsoft.PowerShell.Utility\Format-Table Name, Url | Out-String -Width 96
Save-Frame '4-links-before' (New-ConsoleText $beforeLinks.TrimEnd())
$links = $rows | Format-ConsoleTable -AsWidget -Property Name, @{
    Name = 'Report'
    Expression = { New-ConsoleText 'Open report' -Link $_.Url }
}, @{ Name = 'Path'; Expression = 'Path'; Presenter = 'Link' }
Save-Frame '4-links-after' $links
$links | Export-ConsoleFrame -Format Html -Path (Join-Path $OutputDirectory '4-links-compact.html') -Width 48 -Title 'Links at 48 columns' -WindowChrome

@'
# Everyday-formatting review frames

All data is synthetic. Each before/after pair has a `.txt` console frame and a self-contained `.html` export with light/dark themes.

- Item 1: `1-table-*`, `1-list-*`, `1-wide-*`, `1-alignment-*`. The after frames use native format records and the same adapter as direct-host interception; the alignment example includes a centered calculated property and right-aligned numbers.
- Item 2: `2-module-views-*`. The example module has no ConsoleMonster dependency. These are plumbing examples, not the final ADX/Cx/CertNoob/TestimoX designs.
- Item 3: `3-export-*`, plus the actual `3-export-result.md` and `3-export-result.html` documents. Markdown preserves the rendered layout in a fenced text block.
- Clipboard: `3-clipboard-before.*` and `3-clipboard-after-ssh.*` show the actual graceful refusal. No real clipboard was overwritten.
- Item 4: `4-links-*`, with a compact HTML variant. HTML includes clickable web links; file-path labels demonstrate the plain fallback. Terminal OSC 8 bytes and routing are covered separately by capability tests and the terminal assertions.

These files are local review evidence. Nothing has been published or deployed.
'@ | Set-Content -LiteralPath (Join-Path $OutputDirectory 'README.md') -Encoding UTF8
