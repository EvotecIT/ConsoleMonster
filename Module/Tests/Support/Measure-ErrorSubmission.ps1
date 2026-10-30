[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ModulePath,
    [Parameter(Mandatory)][string] $ReceiptPath,
    [Parameter(Mandatory)][ValidateSet('Transcript', 'Disable')][string] $Scenario
)

$ErrorActionPreference = 'Stop'
try {
    if ([Console]::IsOutputRedirected -or $Host.Name -ne 'ConsoleHost') { throw 'A console-attached child is required.' }
    Import-Module $ModulePath
    . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
    $record = [Management.Automation.ErrorRecord]::new([InvalidOperationException]::new('SUBMISSION_FAILURE'), 'SubmissionError', 'InvalidOperation', 'synthetic')
    $nativePath = $ReceiptPath + '.native.txt'
    $enabledPath = $ReceiptPath + '.enabled.txt'

    function Invoke-Submission {
        param([switch] $Enabled)
        Clear-Host
        $start = [Console]::CursorTop
        if ($Enabled) {
            Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact
            & {
                if ($Scenario -eq 'Transcript') { $null = Start-Transcript -LiteralPath $enabledPath }
                if ($Scenario -eq 'Disable') { Disable-ConsoleFormatting -Feature Errors }
                Write-Host BEGIN_SUBMISSION_ERROR
                $record
            } | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))) -Shim -TopLevel
        } else {
            Disable-ConsoleFormatting
            & {
                if ($Scenario -eq 'Transcript') { $null = Start-Transcript -LiteralPath $nativePath }
                Write-Host BEGIN_SUBMISSION_ERROR
                $record
            } | Microsoft.PowerShell.Core\Out-Default
        }
        $height = [Console]::CursorTop - $start
        $frame = [ConsoleMonsterTestConsoleFrame]::LastFrame($height, [Console]::BufferWidth)
        Write-Host END_SUBMISSION_ERROR
        if ($Scenario -eq 'Transcript') { $null = Stop-Transcript }
        $frame
    }

    $native = Invoke-Submission
    $enabled = Invoke-Submission -Enabled
    if ($enabled -cne $native) { throw "Same-submission $Scenario changed the native error frame.`nNative:`n$native`nEnabled:`n$enabled" }
    if ($Scenario -eq 'Transcript') {
        $pattern = '(?s)BEGIN_SUBMISSION_ERROR\r?\n(?<body>.*?)END_SUBMISSION_ERROR'
        $before = [regex]::Match([IO.File]::ReadAllText($nativePath), $pattern)
        $after = [regex]::Match([IO.File]::ReadAllText($enabledPath), $pattern)
        if (-not $before.Success -or -not $after.Success -or $before.Groups['body'].Value -cne $after.Groups['body'].Value) { throw 'Same-submission transcript differs from native PowerShell.' }
    }
    Disable-ConsoleFormatting
    @{ Passed = $true; Scenario = $Scenario; Runtime = $PSVersionTable.PSVersion.ToString() } | ConvertTo-Json | Set-Content -LiteralPath $ReceiptPath
} catch {
    @{ Passed = $false; Error = ($_ | Out-String) } | ConvertTo-Json | Set-Content -LiteralPath $ReceiptPath
    exit 1
}
