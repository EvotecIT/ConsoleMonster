<#
.SYNOPSIS
Checks the Prompt feature's native capture and restoration contracts in a real console.
.DESCRIPTION
Runs synthetic comparisons with terminal handles attached. Interactive prompt status, transcript and nested-prompt
transitions can then be exercised with -NoExit; the function is left enabled for that session.
.PARAMETER ResultPath
Task-owned JSON receipt path.
.EXAMPLE
pwsh -NoProfile -NoExit -File ./Assert-PromptHost.ps1 -ResultPath ./prompt-host.json
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $ResultPath)

$ErrorActionPreference = 'Stop'
if ([Console]::IsOutputRedirected -or [Console]::IsInputRedirected) { throw 'A real terminal is required.' }
Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1') -Force
$Error.Clear()
$initialPrompt = ${function:global:prompt}
$original = { 'ORIGINAL_NATIVE> ' }.GetNewClosure()
Set-Item Function:global:prompt $original
$native = prompt | Out-String
$nativeTable = [pscustomobject]@{ Name = 'sample'; Value = 42 } | Format-Table | Out-String
$preferences = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
$readline = Get-Module PSReadLine -ListAvailable | Select-Object -First 1
if ($readline) {
    Import-Module PSReadLine
    Set-PSReadLineOption -AddToHistoryHandler { param($line) $true }
}
$handler = if ($readline) { (Get-PSReadLineOption).AddToHistoryHandler }

Enable-ConsoleFormatting -Include Prompt -Exclude Output, FormatCommands, Errors, Streams, ReadLine
$assigned = prompt
$captured = prompt | Out-String
if ($captured -cne $native -or $assigned -cne 'ORIGINAL_NATIVE> ') { throw 'Capture changed prompt bytes.' }
$file = $ResultPath + '.redirected'
try {
    prompt > $file
    $bytesEnabled = [IO.File]::ReadAllBytes($file)
    Disable-ConsoleFormatting -Feature Prompt
    prompt > $file
    $bytesNative = [IO.File]::ReadAllBytes($file)
    if ([Convert]::ToBase64String($bytesEnabled) -cne [Convert]::ToBase64String($bytesNative)) { throw 'Redirected bytes changed.' }
} finally {
    if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
}
Enable-ConsoleFormatting -Include Prompt -Exclude Output, FormatCommands, Errors, Streams, ReadLine
$table = [pscustomobject]@{ Name = 'sample'; Value = 42 } | Format-Table | Out-String
if ($table -cne $nativeTable) { throw 'Native format pipeline changed.' }
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new([ConsoleMonster.Terminal]::GetCurrentProfile())
$scope = [ConsoleMonster.Terminal]::Use($target)
try {
    if ((prompt | Out-String) -cne $native) { throw 'Recording changed prompt bytes.' }
} finally { $scope.Dispose() }
if ($Error.Count -ne 0) { throw "New error: $($Error[0])" }
$after = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
if (($preferences -join '|') -cne ($after -join '|')) { throw 'Preferences changed.' }
if ($readline -and -not [object]::ReferenceEquals($handler, (Get-PSReadLineOption).AddToHistoryHandler)) { throw 'ReadLine handler changed.' }
Disable-ConsoleFormatting
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Native closure restoration failed.' }
Enable-ConsoleFormatting -Include Prompt -Exclude Output, FormatCommands, Errors, Streams, ReadLine
Remove-Module ConsoleMonster -Force
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Native module-removal restoration failed.' }

[ordered]@{
    Version = $PSVersionTable.PSVersion.ToString()
    Assigned = $true
    Piped = $true
    RedirectedBytes = $true
    NativePipeline = $true
    Recording = $true
    Preferences = $true
    ErrorsUnchanged = $true
    ReadLineHandler = $true
    DisableRestore = $true
    ModuleRemovalRestore = $true
} | ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8

Set-Item Function:global:prompt $initialPrompt
Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1') -Force
. (Join-Path $PSScriptRoot 'Assert-PromptFallback.ps1')
$fallbackRoutes = foreach ($route in 'SimpleMerge', 'Merge', 'AllStreams', 'Suppress', 'File', 'ErrorVariable', 'AppendVariables', 'PreferenceContinue', 'PreferenceSilent', 'Stop', 'Throw', 'Recursive', 'Recording') {
    Assert-PromptFallback -Route $route
}
[ordered]@{
    Version = $PSVersionTable.PSVersion.ToString()
    Routes = @($fallbackRoutes)
    AttachedConsole = $true
} | ConvertTo-Json | Set-Content -LiteralPath ($ResultPath + '.fallback.json') -Encoding UTF8
Set-ConsoleTheme -Theme Dark
Enable-ConsoleFormatting -Include Prompt -Exclude Output, FormatCommands, Errors, Streams, ReadLine
[Console]::WriteLine('PROMPT_HOST_CONTRACTS_OK')
