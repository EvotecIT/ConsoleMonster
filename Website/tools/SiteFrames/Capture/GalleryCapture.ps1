# Site-only driver: the displayed files contain only the runnable example.
param(
    [Parameter(Mandatory)] [string] $ModuleRoot,
    [Parameter(Mandatory)] [string] $Source,
    [int] $Width,
    [int] $Height,
    [int] $Steps
)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
[Threading.Thread]::CurrentThread.CurrentUICulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
$env:PSModulePath = $ModuleRoot + [IO.Path]::PathSeparator + $env:PSModulePath
Import-Module ConsoleMonster
$binary = (Get-Command Show-Console).DLL
if (-not $binary.StartsWith($ModuleRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The gallery must capture the repository module build.'
}
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($Width, $Height, 'TrueColor', 'Full')
$profile = $profile.WithCulture([Globalization.CultureInfo]::GetCultureInfo('en-US'))
$metadata = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $Source) 'metadata.json') -Raw | ConvertFrom-Json
$driver = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../ConsoleMonster.PowerShell/Gallery/DemoCapture.ps1'))
$demo = [scriptblock]::Create("& '$($Source.Replace("'", "''"))' @args")
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecordingOptions'))::new()
$options.InProcess = $true
$options.Width = $Width
$options.Height = $Height
if (-not $env:SITEFRAMES_OUTPUT_RECORDING) { throw 'Missing hosted capture destination.' }
$options.Path = $env:SITEFRAMES_OUTPUT_RECORDING
$recorder = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecorder'))::Start($options)
try {
    for ($step = 0; $step -lt $Steps; $step++) {
        $frame = & $driver -Demo $demo -Metadata $metadata -Step $step
        $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile)
        $outputWriter = [ConsoleMonster.Terminal]::CreateOutput($profile, $target.Writer)
        try { $outputWriter.WriteBlock($frame) }
        finally { $outputWriter.Dispose() }
        $recorder.CaptureOutput(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecorder'))::MarkerSequence("state:$step"))
        $recorder.CaptureOutput("$([char]27)[2J$([char]27)[H")
        $recorder.CaptureOutput($target.Output)
    }
    $recorder.CaptureOutput(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecorder'))::MarkerSequence('end'))
    if ($recorder.LimitReached) { throw 'Hosted capture reached its recorder byte limit.' }
} finally { $recorder.Dispose() }
