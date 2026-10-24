<#
.SYNOPSIS
Initializes console-attached Errors assertions for separate prompt pipelines.
.DESCRIPTION
Run Initialize, then e, then v as separate prompt commands. BeginTranscript, then $Error[0], then EndTranscript checks
the real host's transcription fallback. Finish restores formatting and writes the receipt. All errors are synthetic.
.PARAMETER Phase
The assertion phase. Separate commands are required to exercise PowerShell's implicit Out-Default.
.PARAMETER ResultPath
The task-owned JSON receipt path, required when initializing.
.EXAMPLE
./Assert-ErrorFormattingHost.ps1 -Phase Initialize -ResultPath D:/Dev/Scratch/errors-host.json
.EXAMPLE
e
v
#>
[CmdletBinding()]
param(
    [ValidateSet('Initialize', 'Detailed', 'BeginNativeTranscript', 'EndNativeTranscript', 'BeginTranscript', 'EndTranscript', 'Finish')]
    [string] $Phase = 'Initialize',
    [string] $ResultPath
)

if ([Console]::IsOutputRedirected -or $Host.Name -ne 'ConsoleHost') { throw 'A real console host is required.' }
switch ($Phase) {
    'Initialize' {
        Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1') -ErrorAction Stop
        . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
        . (Join-Path $PSScriptRoot 'ErrorFormattingHarness.ps1')
        $global:cmErrorReceipt = $ResultPath
        $global:cmErrorTranscript = $ResultPath + '.transcript.txt'
        $global:cmErrorChecks = [Collections.Generic.List[string]]::new()
        $global:cmErrorOriginalView = $ErrorView
        $global:cmDetailedError = $false
        $directory = Join-Path ([IO.Path]::GetTempPath()) ('cm-error-host-' + [guid]::NewGuid().ToString('N'))
        $null = New-Item -ItemType Directory -Path $directory
        try {
            Disable-ConsoleFormatting
            try { [int]::Parse('abc') } catch { $record = $_ }
            $capture = { Get-ErrorFormattingSnapshot $record $directory }
            $before = & $capture
            Enable-ConsoleFormatting -Feature Errors
            $after = & $capture
            foreach ($name in $before.PSObject.Properties.Name) {
                if ($before.$name -cne $after.$name) {
                    $before | ConvertTo-Json | Set-Content ($ResultPath + '.before.json')
                    $after | ConvertTo-Json | Set-Content ($ResultPath + '.after.json')
                    throw "Console-attached native error capture changed: $name"
                }
            }
            $global:cmErrorChecks.Add('Console-attached native assignment, records, continuation, Out-String, Out-File, > and 2> parity')
        } finally { Remove-Item -LiteralPath $directory -Recurse -Force }
        function global:e {
            throw [InvalidOperationException]::new('Inventory load failed.', [IO.IOException]::new('Synthetic source unavailable.', [Exception]::new('Synthetic root cause.')))
        }
        function global:v {
            $widget = $Error[0] | Show-ConsoleError -Compact -AsWidget
            $widget.ShowStack = $global:cmDetailedError
            $expected = [ConsoleMonster.Terminal]::RenderText($widget, [ConsoleMonster.Terminal]::Profile.WithSize(($Host.UI.RawUI.WindowSize.Width - 1), 200))
            $expected = ($expected -replace "`r", '').TrimEnd("`n") + "`n"
            $lines = @($expected.TrimEnd("`n") -split "`n")
            $width = ($lines | ForEach-Object { $_.Length } | Measure-Object -Maximum).Maximum
            $actual = [ConsoleMonsterTestConsoleFrame]::LastFrame($lines.Count, $width)
            if ($actual -cne $expected) { throw "Host Errors cells differ from the shared view.`nExpected:`n$expected`nActual:`n$actual" }
            $global:cmErrorChecks.Add($(if ($global:cmDetailedError) { 'Detailed host cells equal the shared view, including stacks and causes' } else { 'Implicit host cells equal the shared compact view, including invocation span and folded causes' }))
            'COMPACT_HOST_CELLS_OK'
        }
        Enable-ConsoleFormatting -Feature Errors
        'ERROR_HOST_INITIALIZED'
    }
    'BeginTranscript' {
        Enable-ConsoleFormatting -Feature Errors
        $null = Start-Transcript -LiteralPath $global:cmErrorTranscript
    }
    'Detailed' {
        Enable-ConsoleFormatting -Feature Errors -Detailed
        $global:cmDetailedError = $true
    }
    'BeginNativeTranscript' {
        Disable-ConsoleFormatting -Feature Errors
        $null = Start-Transcript -LiteralPath ($global:cmErrorTranscript + '.native')
    }
    'EndNativeTranscript' {
        $null = Stop-Transcript
    }
    'EndTranscript' {
        $null = Stop-Transcript
        $text = Get-Content -LiteralPath $global:cmErrorTranscript -Raw
        if ($text -match 'Get-Error for details|\+2 inner exceptions') { throw 'Transcript contained ConsoleMonster rendering.' }
        if ($text -notmatch 'Inventory load failed\.') { throw 'Transcript lost the error.' }
        function Get-TranscriptErrorBody {
            param([string] $Content)
            $lines = @($Content.Replace("`r`n", "`n") -split "`n")
            $begin = -1
            for ($n = 0; $n -lt $lines.Count; $n++) {
                if ($lines[$n].StartsWith('OperationStopped:') -or $lines[$n] -ceq 'Inventory load failed.') { $begin = $n; break }
            }
            if ($begin -lt 0) { throw 'Native transcript error body was not found.' }
            $end = $begin + 1
            while ($end -lt $lines.Count -and -not $lines[$end].StartsWith('PS ')) { $end++ }
            ($lines[$begin..($end - 1)] -join "`n").TrimEnd("`n")
        }
        $native = Get-Content -LiteralPath ($global:cmErrorTranscript + '.native') -Raw
        if ((Get-TranscriptErrorBody $native) -cne (Get-TranscriptErrorBody $text)) { throw 'Transcript error body differs from native PowerShell.' }
        $global:cmErrorChecks.Add('The actual transcript error body equals native PowerShell exactly')
        'TRANSCRIPT_NATIVE_OK'
    }
    'Finish' {
        Disable-ConsoleFormatting
        if ($ErrorView -cne $global:cmErrorOriginalView) { throw 'ErrorView changed.' }
        if ((Get-Command Out-Default).CommandType -ne 'Cmdlet') { throw 'Out-Default was not restored.' }
        $global:cmErrorChecks.Add('ErrorView unchanged and native Out-Default restored')
        [pscustomobject]@{ Runtime = $PSVersionTable.PSVersion.ToString(); Host = $Host.Name; Checks = @($global:cmErrorChecks) } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $global:cmErrorReceipt -Encoding UTF8
        Remove-Item Function:/e, Function:/v
        'ERROR_HOST_CONTRACTS_OK'
    }
}
