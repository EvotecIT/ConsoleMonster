[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$RuntimePath,
    [Parameter(Mandatory=$true)][string]$AssemblyDirectory,
    [Parameter(Mandatory=$true)][string]$ResultsPath,
    [ValidateSet('Console', 'Output', 'Error')][string]$Route = 'Console'
)
$ErrorActionPreference = 'Stop'
function Quote-CollectStatus([string]$Text) { "'" + $Text.Replace("'", "''") + "'" }
$inputPath = [IO.Path]::ChangeExtension($ResultsPath, 'input.ps1')
$transcriptPath = [IO.Path]::ChangeExtension($ResultsPath, 'transcript.txt')
$filePath = [IO.Path]::ChangeExtension($ResultsPath, 'native.txt')
$commands = @(
    ('. ' + (Quote-CollectStatus (Join-Path $PSScriptRoot 'CollectLoopSetup.ps1')) + ' -AssemblyDirectory ' + (Quote-CollectStatus $AssemblyDirectory)),
    ('$global:R9TranscriptPath=' + (Quote-CollectStatus $transcriptPath)),
    ('$global:R9FilePath=' + (Quote-CollectStatus $filePath)),
    '$global:R9Builder=$global:CollectBuilder; $global:R9StatusAtFrame=$false; $global:CollectBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($objects,$context) $global:R9StatusAtFrame=$global:R9StatusAtFrame -or ([DisplayHostContract.ConsoleText]::Read() -match ''collecting''); $global:R9Builder.Invoke($objects,$context)}; Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''final-a'',''a''); Start-Sleep -Milliseconds 1100; $global:R9QuietStatus=Read-CollectStatus; [DisplayHostContract.Row]::new(''final-b'',''a''); $global:R9Status=Read-CollectStatus }',
    'Assert-Collect ''Collect_StatusRowDriven_ClearedBeforeFinalBuilder'' (-not $global:R9QuietStatus -and $global:R9Status -and -not $global:R9StatusAtFrame -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and $global:CollectBatches.Count -eq 1)',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''warning-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''warning-b'',''a''); $global:R9Status=Read-CollectStatus; Write-Warning ''R9_WARNING_FINAL_RECORD'' }',
    'Assert-Collect ''Collect_StatusNativeWarning_NoLaterRowLeavesNoStatus'' ($global:R9Status -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and (Test-CollectOrder @(''R9_WARNING_FINAL_RECORD'',''COLLECT<warning-a,warning-b>'')))',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''error-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''error-b'',''a''); $global:R9Status=Read-CollectStatus; Write-Error ''R9_ERROR_BOUNDARY'' -ErrorAction Continue; [DisplayHostContract.Row]::new(''error-c'',''a'') }',
    'Assert-Collect ''Collect_StatusNativeError_ClearsBeforeObservableBoundary'' ($global:R9Status -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and (Test-CollectOrder @(''COLLECT<error-a,error-b>'',''R9_ERROR_BOUNDARY'',''COLLECT<error-c>'')))',
    'Use-CollectView',
    'Start-Transcript -LiteralPath $global:R9TranscriptPath | Out-Null',
    '& { [DisplayHostContract.Row]::new(''recorded-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''recorded-b'',''a''); $global:R9Status=Read-CollectStatus }',
    'Stop-Transcript | Out-Null; $recorded=Get-Content -LiteralPath $global:R9TranscriptPath -Raw; Assert-Collect ''Collect_StatusActiveTranscript_NativeOnlyWithoutStatus'' (-not $global:R9Status -and $global:CollectBatches.Count -eq 0 -and $recorded -match ''recorded-a'' -and $recorded -match ''recorded-b'' -and $recorded -notmatch ''collecting [0-9]'' -and $recorded -notmatch ''COLLECT<(recorded|start-recording|recording-next)-a'')',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''start-recording-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''start-recording-b'',''a''); $global:R9Status=Read-CollectStatus; Start-Transcript -LiteralPath $global:R9TranscriptPath -Force | Out-Null }',
    '$global:R9StatusAfterTranscript=[DisplayHostContract.ConsoleText]::Read() -match ''collecting''; Stop-Transcript | Out-Null; $recorded=Get-Content -LiteralPath $global:R9TranscriptPath -Raw; Assert-Collect ''Collect_StatusTranscriptStartsBeforeComplete_ClearsExistingLineWithoutRecordingIt'' ($global:R9Status -and -not $global:R9StatusAfterTranscript -and $global:CollectBatches.Count -eq 0 -and $recorded -match ''start-recording-a'' -and $recorded -notmatch ''collecting [0-9]'' -and $recorded -notmatch ''COLLECT<(recorded|start-recording|recording-next)-a'')',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''recording-next-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''recording-next-b'',''a''); $global:R9Status=Read-CollectStatus; Start-Transcript -LiteralPath $global:R9TranscriptPath -Force | Out-Null; [DisplayHostContract.Row]::new(''recording-next-c'',''a'') }',
    '$global:R9StatusAfterTranscript=[DisplayHostContract.ConsoleText]::Read() -match ''collecting''; Stop-Transcript | Out-Null; $recorded=Get-Content -LiteralPath $global:R9TranscriptPath -Raw; Assert-Collect ''Collect_StatusTranscriptStartsBeforeArrival_ClearsExistingLineWithoutRecordingIt'' ($global:R9Status -and -not $global:R9StatusAfterTranscript -and $global:CollectBatches.Count -eq 0 -and $recorded -match ''recording-next-c'' -and $recorded -notmatch ''collecting [0-9]'' -and $recorded -notmatch ''COLLECT<(recorded|start-recording|recording-next)-a'')',
    'Use-CollectView',
    '$nativeText=& { [DisplayHostContract.Row]::new(''string-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''string-b'',''a'') } | Out-String; Assert-Collect ''Collect_StatusOutString_NativeOnlyWithoutStatus'' ($global:CollectBatches.Count -eq 0 -and $nativeText -match ''string-a'' -and $nativeText -match ''string-b'' -and $nativeText -notmatch ''collecting [0-9]'' -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting''))',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''file-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''file-b'',''a'') } | Out-File -LiteralPath $global:R9FilePath; $nativeText=Get-Content -LiteralPath $global:R9FilePath -Raw; Assert-Collect ''Collect_StatusOutFile_NativeOnlyWithoutStatus'' ($global:CollectBatches.Count -eq 0 -and $nativeText -match ''file-a'' -and $nativeText -match ''file-b'' -and $nativeText -notmatch ''collecting [0-9]'' -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting''))',
    'Use-CollectView',
    '& { [DisplayHostContract.Row]::new(''redirect-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''redirect-b'',''a'') } > $global:R9FilePath; $nativeText=Get-Content -LiteralPath $global:R9FilePath -Raw; Assert-Collect ''Collect_StatusPowerShellRedirection_NativeOnlyWithoutStatus'' ($global:CollectBatches.Count -eq 0 -and $nativeText -match ''redirect-a'' -and $nativeText -match ''redirect-b'' -and $nativeText -notmatch ''collecting [0-9]'' -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting''))',
    '$global:CollectLease.Dispose()',
    ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath ' + (Quote-CollectStatus $ResultsPath) + ' -Encoding UTF8'),
    'exit'
)
if ($Route -ne 'Console') {
    $commands = @(
        ('. ' + (Quote-CollectStatus (Join-Path $PSScriptRoot 'ConsoleLoopSetup.ps1')) + ' -AssemblyDirectory ' + (Quote-CollectStatus $AssemblyDirectory)),
        '$global:DisplayLoopLease.Dispose(); $global:R9Lease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule(''RedirectHost'',[version]''1.0.0'',[string[]]@(''DisplayHostContract.Row'')); $null=$global:R9Lease.Register(''DisplayHostContract.Row'',''redirect.collect'',[version]''1.0.0'',''Collect'',[string[]]@(''Scope''),$global:DisplayLoopBuilder,$null)',
        '& { [DisplayHostContract.Row]::new(''os-redirect-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''os-redirect-b'',''a'') }',
        ('$routeIsRedirected=[Console]::Is' + $Route + 'Redirected; Assert-ConsoleLoop ' + (Quote-CollectStatus ('Collect_StatusOS' + $Route + 'Redirected_NativeOnlyWithoutStatus')) + ' ($routeIsRedirected -and $global:DisplayLoopBuilds -eq 0) ''An OS redirected sink invoked the rich builder or lacked redirected handles.'''),
        '$global:R9Lease.Dispose()',
        ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath ' + (Quote-CollectStatus $ResultsPath) + ' -Encoding UTF8'),
        'exit'
    )
}
[IO.File]::WriteAllLines($inputPath, $commands)
$started = [DateTime]::UtcNow
$redirectedPath = [IO.Path]::ChangeExtension($ResultsPath, 'redirected.txt')
$startParameters = @{
    FilePath = $RuntimePath
    ArgumentList = @('-NoLogo', '-NoProfile', '-Command', '-')
    RedirectStandardInput = $inputPath
    WindowStyle = 'Hidden'
    PassThru = $true
}
if ($Route -eq 'Output') { $startParameters.RedirectStandardOutput = $redirectedPath }
if ($Route -eq 'Error') { $startParameters.RedirectStandardError = $redirectedPath }
$process = Start-Process @startParameters
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath} | ConvertTo-Json -Compress
try {
    if (-not $process.WaitForExit(45000)) { throw 'The owned collecting status proof exceeded 45 seconds.' }
    $process.Refresh()
    $report = Get-Content -LiteralPath $ResultsPath -Raw | ConvertFrom-Json
    $report.Tests | Format-Table Name, Passed -AutoSize
    $expectedCount = if ($Route -eq 'Console') { 9 } else { 1 }
    if ($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne $expectedCount -or [DateTime]$report.RecordedUtc -lt $started) {
        throw 'Collecting status proof failed; see its result file.'
    }
    if ($Route -eq 'Output') {
        $redirectedText = Get-Content -LiteralPath $redirectedPath -Raw
        if ($redirectedText -notmatch 'os-redirect-a' -or $redirectedText -notmatch 'os-redirect-b' -or $redirectedText -match 'collecting [0-9]|RICH os-redirect') {
            throw 'The OS redirected output was missing native rows or contained rich/status output.'
        }
    }
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
    $process.Dispose()
}
