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
$options = ($assembly.GetType('ConsoleMonster.Sessions.SessionRecordingOptions', $true))::new()
$options.InProcess = $true
$options.Width = $Width
$options.Height = $Height
if (-not $env:SITEFRAMES_OUTPUT_RECORDING) { throw 'Missing hosted capture destination.' }
$options.Path = $env:SITEFRAMES_OUTPUT_RECORDING
$recorder = ($assembly.GetType('ConsoleMonster.Sessions.SessionRecorder', $true))::Start($options)
$writer = ($assembly.GetType('ConsoleMonster.Output.StringTerminalWriter', $true))::new()
$output = [ConsoleMonster.Terminal]::CreateOutput($profile, $writer)
try {
    $output.BeginLive()
    $recorder.CaptureOutput($writer.ToString()); $writer.Clear()
    try {
        for ($index = 0; $index -lt $target.Screens.Frames.Count; $index++) {
            $output.RenderFrame($target.Screens.Frames[$index])
            $recorder.CaptureOutput($writer.ToString()); $writer.Clear()
            if ($target.Screens.FrameHolds.ContainsKey($index)) { $recorder.AddMarker($target.Screens.FrameHolds[$index]) }
            if ($recorder.LimitReached) { throw 'Hosted capture reached its recorder byte limit.' }
        }
    } finally { $output.EndLive() }
    $output.WriteBlock($target.Transcript())
    $recorder.CaptureOutput($writer.ToString())
    if ($recorder.LimitReached) { throw 'Hosted capture reached its recorder byte limit.' }
} finally { $output.Dispose(); $recorder.Dispose(); $writer.Dispose() }
