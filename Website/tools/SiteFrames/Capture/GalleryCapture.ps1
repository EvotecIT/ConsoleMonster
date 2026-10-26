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
$binary = (Get-Command Show-Console).DLL
if (-not $binary.StartsWith($ModuleRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The gallery must capture the repository module build.'
}
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($Width, $Height, 'TrueColor', 'Full')
$profile = $profile.WithCulture([Globalization.CultureInfo]::GetCultureInfo('en-US'))
$metadata = Get-Content -LiteralPath (Join-Path (Split-Path -Parent $Source) 'metadata.json') -Raw | ConvertFrom-Json
$driver = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../../ConsoleMonster.PowerShell/Gallery/DemoCapture.ps1'))
$demo = [scriptblock]::Create("& '$($Source.Replace("'", "''"))' @args")
$interactive = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions')
$previous = $interactive::Default
try {
    if ($metadata.capture.actionClock) {
        if ($metadata.capture.actionClock -ne 'Frozen') { throw "Unsupported actionClock: $($metadata.capture.actionClock)" }
        $options = $previous.Clone()
        $clock = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.SimulatedClock'))::new()
        # The action worker samples a native delegate, never a script block on another runspace thread.
        $options.ActionClock = [Delegate]::CreateDelegate([Func[TimeSpan]], $clock, 'Read')
        $interactive::Default = $options
    }
    for ($step = 0; $step -lt $Steps; $step++) {
        $frame = & $driver -Demo $demo -Metadata $metadata -Step $step
        $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile)
        $outputWriter = [ConsoleMonster.Terminal]::CreateOutput($profile, $target.Writer)
        try { $outputWriter.WriteBlock($frame) }
        finally { $outputWriter.Dispose() }
        [IO.File]::WriteAllText((Join-Path $Output "$step.ansi"), $target.Output, [Text.UTF8Encoding]::new($false))
    }
} finally {
    $interactive::Default = $previous
}
