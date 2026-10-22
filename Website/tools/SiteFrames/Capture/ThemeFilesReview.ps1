<#
.SYNOPSIS
Export the public native PowerShell theme example for offline review.
.DESCRIPTION
Uses SiteFrames' source-module capture runner, runs the published script and exports the
widget it actually created. Rendering interop belongs here, outside the shown
example. Writes eight HTML/text pairs; does not start a browser or server.
.PARAMETER ExamplePath
The public theme-files.ps1 script beside its team JSON files.
.PARAMETER OutputDirectory
The task-owned review destination, outside the source checkout.
.EXAMPLE
SiteFrames theme-files-review Website D:/Dev/Scratch/consolemonster-themes/frames --powershell
.OUTPUTS
Draws the public example and reports the output folder. Files contain actual
PowerShell-created widgets exported through ConsoleMonster's engine.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ExamplePath,

    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
$ExamplePath = (Resolve-Path -LiteralPath $ExamplePath).ProviderPath
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
Import-Module ConsoleMonster -ErrorAction Stop

# Secondary .NET interop: capture/export plumbing, never part of the public example.
$culture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')
[System.Threading.Thread]::CurrentThread.CurrentCulture = $culture
[System.Threading.Thread]::CurrentThread.CurrentUICulture = $culture
foreach ($scheme in 'Dark', 'Light') {
    $exports = Join-Path $OutputDirectory 'presets'
    foreach ($width in 120, 60) {
        . $ExamplePath -BaseTheme $scheme -Width $width -ExportDirectory $exports
        $teamTheme = [ConsoleMonster.Terminal]::Theme
        foreach ($stage in 'before', 'after') {
            if ($stage -eq 'before') {
                $theme = [ConsoleMonster.Styling.Theme]::FromName($scheme)
            } else {
                $theme = $teamTheme
            }
            $options = [ConsoleMonster.Export.ExportOptions]::new()
            $options.Width = $width
            $options.Theme = $scheme
            $options.Culture = $culture
            $options.WindowChrome = $true
            $options.Title = $stage + '-' + $scheme.ToLowerInvariant() + '-' + $width + ' | Build queue (sample data)'
            $frame = [ConsoleMonster.Export.ConsoleExporter]::Render($themeReport, $options, $theme)
            $name = $stage + '-' + $scheme.ToLowerInvariant() + '-' + $width
            $html = [ConsoleMonster.Export.ConsoleExporter]::ToHtml($frame, $options)
            $text = [ConsoleMonster.Export.ConsoleExporter]::ToText($frame)
            [IO.File]::WriteAllText((Join-Path $OutputDirectory ($name + '.html')), $html, [Text.UTF8Encoding]::new($false))
            [IO.File]::WriteAllText((Join-Path $OutputDirectory ($name + '.txt')), $text, [Text.UTF8Encoding]::new($false))
        }
    }
}
$receipt = @{
    Source = $ExamplePath
    PowerShell = $PSVersionTable.PSVersion.ToString()
    Frames = 8
} | ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'review-complete.json'), $receipt, [Text.UTF8Encoding]::new($false))
Write-Output ('PowerShell theme review: ' + $OutputDirectory)
