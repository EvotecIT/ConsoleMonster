# Site-only capture harness. The example itself stays native PowerShell.
$width = [int] $env:CM_SITE_APP_WIDTH
$height = [int] $env:CM_SITE_APP_HEIGHT
$capture = [ConsoleMonster.Interactive.ScreenCapture]::new($width, $height)
$keys = if ($env:CM_SITE_APP_STATE -eq 'initial') { 'Escape' } else { '"sql" Escape' }
$inputSource = [ConsoleMonster.Input.ScriptedInput]::new($keys)
$appOptions = @{
    Capture = $capture
    InputSource = $inputSource
    Theme = [ConsoleMonster.Terminal]::Theme
}
$example = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../examples/app/live-filter.ps1'))
& $example -AppOptions $appOptions

$profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width, $height)
$output = [ConsoleMonster.Terminal]::CreateOutput($profile)
try { $output.WriteBlock($capture.LastFrame) } finally { $output.Dispose() }
