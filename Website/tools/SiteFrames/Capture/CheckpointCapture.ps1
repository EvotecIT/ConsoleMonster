# Runs an ordinary synthetic live program with the shared recording-target checkpoint controls.
param([Parameter(Mandatory)] [scriptblock] $ScriptBlock, [int] $Width, [int] $Height)
$assembly = [ConsoleMonster.Terminal].Assembly
$culture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
$profile = ($assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile', $true))::Fixed($Width, $Height, 'TrueColor', 'Full').WithCulture($culture).WithLanguage($culture).WithMode('Interactive')
$target = ($assembly.GetType('ConsoleMonster.Output.RecordingTarget', $true))::new($profile)
$target.SkipDelays = $true
$target.CaptureViewport = $true
$scope = [ConsoleMonster.Terminal]::Use($target)
try { $null = & $ScriptBlock } finally { $scope.Dispose() }
if (-not $target.Screens.Frames.Count) { throw 'The program produced no logical frame checkpoints.' }
$output = [ConsoleMonster.Terminal]::CreateOutput()
try {
    $output.BeginLive()
    try { foreach ($frame in $target.Screens.Frames) { $output.RenderFrame($frame) } }
    finally { $output.EndLive() }
    $output.WriteBlock($target.Transcript())
} finally { $output.Dispose() }
