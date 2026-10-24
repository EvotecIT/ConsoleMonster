param(
    [Parameter(Mandatory = $true)][string]$Example,
    [Parameter(Mandatory = $true)][int]$Columns,
    [Parameter(Mandatory = $true)][string]$Appearance,
    [Parameter(Mandatory = $true)][string]$Destination,
    [switch]$Navigation,
    [string]$Receipt
)

# Capture-only interop: the public example remains a native PowerShell script.
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed($Columns, 12)
$keys = if ($Navigation) { 'wait:300 PgUp wait:300 End wait:300 q' } else { '' }
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile, $keys)
if ($Navigation) { $target.Input.RealTime = $true }
$target.Theme = [ConsoleMonster.Styling.Theme]::FromName($Appearance)
if ($Navigation) {
    $path = Join-Path (Split-Path -Parent $Example) 'synthetic.log'
    Use-ConsoleTarget -Target $target { Watch-ConsoleLog -Path $path -Tail 16 -Highlight database }
} else {
    Use-ConsoleTarget -Target $target { & $Example }
}
[IO.File]::WriteAllText($Destination, $target.Output, [Text.UTF8Encoding]::new($false))
if ($Receipt) { [ordered]@{
    Runtime = $PSVersionTable.PSVersion.ToString()
    Edition = $PSVersionTable.PSEdition
    Module = (Get-Module ConsoleMonster).Path
    Assembly = (Get-Command Watch-ConsoleLog).DLL
    Theme = $target.Theme.Name
    Width = $Columns
    Navigation = [bool]$Navigation
} | ConvertTo-Json | Set-Content -LiteralPath $Receipt -Encoding UTF8 }
