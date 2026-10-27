param(
    [Parameter(Mandatory = $true)][string]$Example,
    [Parameter(Mandatory = $true)][int]$Columns,
    [Parameter(Mandatory = $true)][string]$Appearance,
    [switch]$Navigation,
    [string]$Receipt
)

# Capture-only interop: the public example remains a native PowerShell script.
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($Columns, 12)
$keys = if ($Navigation) { 'wait:300 PgUp wait:300 End wait:300 q' } else { '' }
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile, $keys)
# A finite poll records its completed state, without a background repaint racing the first read.
$target.SkipDelays = -not $Navigation
if ($Navigation) { $target.Input.RealTime = $true }
$target.Theme = [ConsoleMonster.Styling.Theme]::FromName($Appearance)
if ($Navigation) {
    $path = Join-Path (Split-Path -Parent $Example) 'synthetic.log'
    Use-ConsoleTarget -Target $target { Watch-ConsoleLog -Path $path -Tail 16 -Highlight database }
} else {
    Use-ConsoleTarget -Target $target { & $Example }
}
if (-not $env:SITEFRAMES_OUTPUT_RECORDING) { throw 'Missing hosted capture destination.' }
$recording = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecordingOptions', $true))::new()
$recording.InProcess = $true
$recording.Width = $Columns
$recording.Height = 12
$recording.Path = $env:SITEFRAMES_OUTPUT_RECORDING
$recorder = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecorder', $true))::Start($recording)
try {
    $recorder.CaptureOutput($target.Output)
    if ($recorder.LimitReached) { throw 'Hosted tail capture reached the recorder byte limit.' }
} finally { $recorder.Dispose() }
if ($Receipt) { [ordered]@{
    Runtime = $PSVersionTable.PSVersion.ToString()
    Edition = $PSVersionTable.PSEdition
    Module = (Get-Module ConsoleMonster).Path
    Assembly = (Get-Command Watch-ConsoleLog).DLL
    Theme = $target.Theme.Name
    Width = $Columns
    Navigation = [bool]$Navigation
} | ConvertTo-Json | Set-Content -LiteralPath $Receipt -Encoding UTF8 }
