# Site-only driver: the displayed files contain only the runnable example.
param(
    [Parameter(Mandatory)] [string] $ModuleRoot,
    [Parameter(Mandatory)] [string] $Source,
    [Parameter(Mandatory)] [string] $Output,
    [int] $Width,
    [int] $Height,
    [int] $Steps
)
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
[Threading.Thread]::CurrentThread.CurrentUICulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
$env:PSModulePath = $ModuleRoot + [IO.Path]::PathSeparator + $env:PSModulePath
Import-Module ConsoleMonster
$binary = (Get-Command Write-ConsoleWidget).DLL
if (-not $binary.StartsWith($ModuleRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The gallery must capture the repository module build.'
}
$profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($Width, $Height, 'TrueColor', 'Full')
$profile = $profile.WithCulture([Globalization.CultureInfo]::GetCultureInfo('en-US'))
for ($step = 0; $step -lt $Steps; $step++) {
    $target = [ConsoleMonster.Output.RecordingTarget]::new($profile)
    $target.Theme = [ConsoleMonster.Styling.Theme]::Dark
    Use-ConsoleTarget -Target $target { & $Source -StaticSnapshot -Step $step }
    [IO.File]::WriteAllText((Join-Path $Output "$step.ansi"), $target.Output, [Text.UTF8Encoding]::new($false))
}
