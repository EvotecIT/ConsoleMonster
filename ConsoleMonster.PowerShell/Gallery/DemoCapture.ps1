# The runner owns capture controls; the copied program has no test parameters.
param($Demo, $Metadata, [int] $Step)
if ($Step -ge $Metadata.steps) { throw "Snapshot $Step does not exist for $($Metadata.slug)." }
$state = $Metadata.capture.states[$Step]
$culture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile', $true))::Fixed($Metadata.terminal.width, $Metadata.terminal.height, 'TrueColor', 'Full').WithCulture($culture).WithLanguage($culture)
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget', $true))::new($profile, $state.keys)
$target.SkipDelays = $true
$target.Checkpoints = if ($null -ne $state.checkpoint) { [int[]] @($state.checkpoint) } else { [int[]] @() }
$previousStorage = $env:CONSOLEMONSTER_STORAGE_ROOT
$captureStorage = Join-Path ([IO.Path]::GetTempPath()) ('cm-demo-presets-' + [guid]::NewGuid().ToString('N'))
$env:CONSOLEMONSTER_STORAGE_ROOT = $captureStorage
$previous = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default
$options = $previous.Clone()
$options.Target = $target
$options.Input = $target.Input
if ($null -ne $target.Input) { $target.Input.RealTime = [bool] $state.realTime }
$options.Capture = $target.Screens
$options.Profile = $profile
$options.Theme = $target.Theme
$options.Mode = if ($null -ne $target.Input) { 'Rich' } else { 'None' }
$options.NonInteractive = $false
try {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default = $options
    Use-ConsoleTarget -Target $target -ScriptBlock {
        try {
            $arguments = @($state.arguments)
            if ($null -eq $state.arguments) { $arguments = @() }
            $null = & $Demo @arguments
        }
        catch {
            # A partial input script deliberately stops at the screen being documented.
            if (-not $state.incomplete -or $_.FullyQualifiedErrorId -notlike 'InputClosed*') { throw }
        }
    }
} finally {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default = $previous
    $env:CONSOLEMONSTER_STORAGE_ROOT = $previousStorage
    if ([IO.Directory]::Exists($captureStorage)) { [IO.Directory]::Delete($captureStorage, $true) }
}
if ($null -ne $state.checkpoint -and $target.CheckpointCount -le $state.checkpoint) {
    throw "The program did not reach checkpoint $($state.checkpoint)."
}
if ($state.contains) {
    $matches = @($target.Screens.Frames | Where-Object { $_.ToPlainText().Contains($state.contains) })
    if (-not $matches.Count) { throw "The capture never reached '$($state.contains)'." }
    if ($state.first) { return $matches[0] }
    return $matches[-1]
}
if (-not $state.outputStart -and $null -ne $target.Screens.LastFrame) { return $target.Screens.LastFrame }
$frame = $target.Transcript()
if ($state.outputStart) {
    $starts = @(for ($row = 0; $row -lt $frame.Height; $row++) {
        if ($frame.GetRowText($row).Contains($state.outputStart)) { $row }
    })
    $at = if ($null -ne $state.occurrence) { [int] $state.occurrence } else { $starts.Count - 1 }
    if ($at -ge $starts.Count -or $at -lt 0) { throw "The output never reached '$($state.outputStart)'." }
    $start = $starts[$at]
    $height = if ($state.rows) { [int] $state.rows } else { $frame.Height - $start }
    $selected = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Canvas.CellBuffer', $true))::new($frame.Width, $height)
    $selected.Blit($frame, 0, $start, $frame.Width, $height, 0, 0)
    return $selected
}
return $frame
