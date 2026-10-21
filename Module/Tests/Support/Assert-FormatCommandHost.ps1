<#
.SYNOPSIS
Checks FormatCommands from an actual terminal, including destinations that remain native inside a console host.
.DESCRIPTION
Run this integration assertion with powershell.exe or pwsh.exe in a terminal. It refuses redirected stdout so a passing
result proves output routing with a console attached. All data is synthetic. The script does not use the clipboard.
.PARAMETER ResultPath
Optional JSON receipt path. Temporary comparison files are removed after the checks.
.EXAMPLE
pwsh -NoProfile -File ./Assert-FormatCommandHost.ps1 -ResultPath ./host-result.json
#>
[CmdletBinding()]
param([string] $ResultPath)

$ErrorActionPreference = 'Stop'
if ([Console]::IsOutputRedirected) { throw 'This integration assertion requires a terminal with non-redirected stdout.' }
Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
Import-Module (Join-Path $PSScriptRoot '../Fixtures/EverydayViews/EverydayViews.psd1')
$temporary = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporary

function Assert-DirectRoute {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)] $InputObject,
        [bool] $Expected
    )
    process {
        $actual = [ConsoleMonster.PowerShell.FormatCommandRouter]::IsDirectHost($PSCmdlet)
        if ($actual -ne $Expected) { throw "Output route was $actual, expected $Expected" }
        $InputObject
    }
}

function Get-Capture {
    param([string] $Command, [string] $Arguments)
    $body = 'Get-EverydayValue | ' + $Command + ' ' + $Arguments
    $records = @(& ([scriptblock]::Create($body)))
    $text = & ([scriptblock]::Create($body + ' | Out-String -Width 80'))
    $continuation = @(& ([scriptblock]::Create($body + ' | ForEach-Object { $_.GetType().FullName }'))) -join ','
    $file = Join-Path $temporary 'capture.txt'
    & ([scriptblock]::Create($body + ' | Out-File -LiteralPath $file -Width 80'))
    $outFile = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    & ([scriptblock]::Create($body + ' > $file'))
    $redirect = [Convert]::ToBase64String([IO.File]::ReadAllBytes($file))
    $global:hostCaptured = @()
    & ([scriptblock]::Create($body + ' -OutVariable global:hostCaptured | Out-Null'))
    [pscustomobject]@{
        Records = [Management.Automation.PSSerializer]::Serialize($records, 20)
        Text = $text
        Continuation = $continuation
        OutFile = $outFile
        Redirect = $redirect
        OutVariable = [Management.Automation.PSSerializer]::Serialize($global:hostCaptured, 20)
    }
}

try {
    $cases = @(
        @{ Kind = 'Table'; Arguments = '-Property Name, State -GroupBy Site -AutoSize -Wrap' }
        @{ Kind = 'List'; Arguments = '-Property N*, @{Label="Health";Expression={ $_.State }} -GroupBy Site' }
        @{ Kind = 'Wide'; Arguments = '-Property Name -Column 2' }
    )
    $expected = @{}
    foreach ($case in $cases) { $expected[$case.Kind] = Get-Capture ("Microsoft.PowerShell.Utility\Format-" + $case.Kind) $case.Arguments }
    Enable-ConsoleFormatting -Feature FormatCommands
    foreach ($case in $cases) {
        $actual = Get-Capture ('Format-' + $case.Kind) $case.Arguments
        foreach ($property in $actual.PSObject.Properties.Name) {
            if ($actual.$property -cne $expected[$case.Kind].$property) { throw "$($case.Kind) changed native $property with a console attached" }
        }
    }
    # This command has no consumer: its destination is the host's implicit Out-Default.
    'DIRECT_HOST_ROUTE_OK' | Assert-DirectRoute -Expected $true

    $transcript = Join-Path $temporary 'transcript.txt'
    $null = Start-Transcript -LiteralPath $transcript
    'TRANSCRIPT_ROUTE_NATIVE_OK' | Assert-DirectRoute -Expected $false
    $null = Stop-Transcript

    # Visible positive examples use the ordinary native cmdlet names, with no following command.
    Get-EverydayValue | Format-Table Name, State -AutoSize
    Get-EverydayValue | Format-List Name, State
    Get-EverydayValue | Format-Wide Name -Column 2
    Disable-ConsoleFormatting
    if ((Get-Command Format-Table).CommandType -ne 'Cmdlet') { throw 'Format-Table was not restored' }
    if ($ResultPath) {
        [pscustomobject]@{ Runtime = $PSVersionTable.PSVersion.ToString(); Host = $Host.Name; Redirected = [Console]::IsOutputRedirected; NativeCaptureMatrix = 'Passed'; DirectHostRoute = 'Passed'; TranscriptRoute = 'Passed'; Restoration = 'Passed' } |
            ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    }
    'HOST_CONTRACTS_OK'
} finally {
    try { Stop-Transcript -ErrorAction Stop | Out-Null } catch { }
    Disable-ConsoleFormatting
    Remove-Variable -Name hostCaptured -Scope Global -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $temporary -Recurse
}
