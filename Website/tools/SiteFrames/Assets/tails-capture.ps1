param(
    [Parameter(Mandatory = $true)][string]$Example,
    [Parameter(Mandatory = $true)][int]$Columns,
    [Parameter(Mandatory = $true)][string]$Appearance,
    [Parameter(Mandatory = $true)][string]$Destination
)

# Capture-only interop: the public example remains a native PowerShell script.
$profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($Columns, 12)
$target = [ConsoleMonster.Output.RecordingTarget]::new($profile, '')
$target.Theme = if ($Appearance -eq 'light') { [ConsoleMonster.Styling.Theme]::Light } else { [ConsoleMonster.Styling.Theme]::Dark }
Use-ConsoleTarget -Target $target { & $Example }
[IO.File]::WriteAllText($Destination, $target.Output, [Text.UTF8Encoding]::new($false))
