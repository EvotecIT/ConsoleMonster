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
. (Join-Path $PSScriptRoot 'Convert-TestNativeView.ps1')
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
    $after = if ($kind -eq 'Wide') { $rows | & $command @parameters | Out-ConsoleFormatCommand -AsWidget }
        else { Convert-TestNativeView -Rows $rows -Kind $kind -Arguments $parameters }
    Save-Frame "1-$($kind.ToLowerInvariant())-after" $after
}

$valueRows = @(
    [pscustomobject]@{ Name='web-01'; State='Pass'; Bytes=2048L; Date=[datetime]'2026-10-21T09:30:00'; Duration=[timespan]::FromSeconds(125); Tags=@('production','web') }
    [pscustomobject]@{ Name='sql-02'; State='Warning'; Bytes=1048576L; Date=[datetime]'2026-10-22T10:45:00'; Duration=[timespan]::FromSeconds(3661); Tags=@('database','east') }
    [pscustomobject]@{ Name='backup-03'; State='Fail'; Bytes=5368709120L; Date=[datetime]'2026-10-23T11:00:00'; Duration=[timespan]::FromSeconds(45); Tags=@('backup') }
)
$valueColumns = @('Name','State','Bytes','Date','Duration','Tags')
foreach ($kind in 'Table','List') {
    $before = $valueRows | & ('Microsoft.PowerShell.Utility\Format-'+$kind) -Property $valueColumns | Out-String -Width 120
    Save-Frame "1-values-$($kind.ToLowerInvariant())-before" (New-ConsoleText $before.TrimEnd())
    $after = Convert-TestNativeView -Rows $valueRows -Kind $kind -Arguments @{Property=$valueColumns}
    Save-Frame "1-values-$($kind.ToLowerInvariant())-after" $after
    $after | Export-ConsoleFrame -Format Markdown | Set-Content -LiteralPath (Join-Path $OutputDirectory "1-values-$($kind.ToLowerInvariant())-after.md") -Encoding UTF8
}

$markdownColumns = @('Name', @{Name='State';Alignment='Center'}, @{Name='Bytes';Aggregate='Sum'}, 'Date','Duration','Tags')
$valueRows | Format-ConsoleTable -Property $markdownColumns -GroupBy State -ShowGroupColumn -Total -As Markdown |
    Set-Content -LiteralPath (Join-Path $OutputDirectory '3-markdown-table.md') -Encoding UTF8
$valueRows | Format-ConsoleList -Property $valueColumns -As Markdown |
    Set-Content -LiteralPath (Join-Path $OutputDirectory '3-markdown-list.md') -Encoding UTF8
$valueRows[0] | Format-ConsoleTree -Title 'Service' -As Markdown |
    Set-Content -LiteralPath (Join-Path $OutputDirectory '3-markdown-tree.md') -Encoding UTF8
$valueRows | Format-ConsoleTable -Property $valueColumns -As Text -Width 100 |
    Set-Content -LiteralPath (Join-Path $OutputDirectory '3-text-layout.md') -Encoding UTF8

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

- Item 1: `1-table-*`, `1-list-*`, `1-wide-*`, `1-alignment-*`. `1-values-table-*` and `1-values-list-*` show statuses, bytes, dates, durations and collections. The after frames use captured native expression values and the same adapter as direct-host interception. The actual attached-console test separately compares the ordinary enabled Format-Table cells to Format-ConsoleTable.
- Item 2: `2-module-views-*`. The example module has no ConsoleMonster dependency. These are plumbing examples, not the final ADX/Cx/CertNoob/TestimoX designs.
- Item 3: `3-export-*`, plus the actual `3-export-result.md` and `3-export-result.html` documents. Open `3-markdown-table.md` (GFM alignment, groups and totals), `3-markdown-list.md`, `3-markdown-tree.md` and `3-text-layout.md` (the exact layout inside a text fence).
- Clipboard: `3-clipboard-before.*` and `3-clipboard-after-ssh.*` show the actual graceful refusal. No real clipboard was overwritten.
- Item 4: `4-links-*`, with a compact HTML variant. HTML includes clickable web links; file-path labels demonstrate the plain fallback. Terminal OSC 8 bytes and routing are covered separately by capability tests and the terminal assertions.

These files are local review evidence. Nothing has been published or deployed.
'@ | Set-Content -LiteralPath (Join-Path $OutputDirectory 'README.md') -Encoding UTF8
