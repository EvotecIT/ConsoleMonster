[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
function Quote-Sample([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$commands=@(
    ('. '+(Quote-Sample (Join-Path $PSScriptRoot 'SampleLoopSetup.ps1'))+' -AssemblyDirectory '+(Quote-Sample $AssemblyDirectory)),
    '& { [DisplayHostContract.Row]::new(''small-a'',''a''); [DisplayHostContract.Row]::new(''small-b'',''a'') }',
    'Assert-Collect ''Sample_CompletedBatch_UsesCollectedBuilderOnce'' ($global:SampleClosed -eq 1 -and $global:AdaptiveRows -eq 0 -and $global:SampleHeaders -eq 0 -and (Test-CollectOrder @(''CLOSED<small-a,small-b>'')))',
    'Use-SampleView',
    '& { for($i=1;$i -le 200;$i++){[DisplayHostContract.Row]::new((''row-{0:d3}'' -f $i),''a'')}; $global:OpenedAtLimit=$global:SampleHeaders -eq 1 -and $global:SampleClosed -eq 0 }',
    'Assert-Collect ''Sample_TwoHundredRows_OpensOneStreamWithoutBuildingRowsTwice'' ($global:OpenedAtLimit -and $global:AdaptiveRows -eq 200 -and $global:AdaptiveFooters -eq 1)',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''timed-a'',''a''); Start-Sleep -Milliseconds 300; $global:QuietHeld=$global:SampleHeaders -eq 0 -and $global:AdaptiveRows -eq 0; [DisplayHostContract.Row]::new(''timed-b'',''a''); $global:OpenedOnArrival=$global:SampleHeaders -eq 1 }',
    'Assert-Collect ''Sample_QuietProducer_WritesOnlyOnTheNextRunspaceCallback'' ($global:QuietHeld -and $global:OpenedOnArrival -and $global:SampleClosed -eq 0 -and $global:AdaptiveRows -eq 2 -and $global:AdaptiveFooters -eq 1)',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''late-single'',''a''); Start-Sleep -Milliseconds 300 }',
    'Assert-Collect ''Sample_TimeBoundaryOnCompletion_OpensAndClosesTheStream'' ($global:SampleHeaders -eq 1 -and $global:AdaptiveRows -eq 1 -and $global:AdaptiveSingles -eq 0 -and $global:AdaptiveFooters -eq 1)',
    'Use-SampleView -Bytes 512',
    '& { [DisplayHostContract.Row]::new(''budget-a'',''a''); [DisplayHostContract.Row]::new((''budget-b-''+(''x''*300)),''a''); [DisplayHostContract.Row]::new(''budget-c'',''a'') }',
    'Assert-Collect ''Sample_ByteOverflow_ReplaysOriginalsAndKeepsTheScopeNative'' ($global:SampleClosed -eq 0 -and $global:AdaptiveRows -eq 0 -and (Test-CollectOrder @(''budget-a'',''budget-b-'',''budget-c'')))',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''merged-a'',''a''); [DisplayHostContract.Row]::new(''merged-b'',''a''); Write-Warning ''SAMPLE_MERGED_WARNING''; [DisplayHostContract.Row]::new(''merged-c'',''a'') } *>&1',
    'Assert-Collect ''Sample_MergedBoundary_ClosesBeforeTheNativeRecord'' ($global:SampleClosed -eq 1 -and $global:AdaptiveSingles -eq 1 -and (Test-CollectOrder @(''CLOSED<merged-a,merged-b>'',''SAMPLE_MERGED_WARNING'',''SINGLE<merged-c>'')))',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''error-a'',''a''); Write-Error ''SAMPLE_UNMERGED_ERROR'' -ErrorAction Continue; [DisplayHostContract.Row]::new(''error-b'',''a'') }',
    'Assert-Collect ''Sample_ObservableUnmergedError_ClosesTheHeldBatchFirst'' ($global:AdaptiveSingles -eq 2 -and (Test-CollectOrder @(''SINGLE<error-a>'',''SAMPLE_UNMERGED_ERROR'',''SINGLE<error-b>'')))',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''warning-a'',''a''); Write-Warning ''SAMPLE_NATIVE_WARNING''; [DisplayHostContract.Row]::new(''warning-b'',''a'') }',
    'Assert-Collect ''Sample_IndependentWarning_RetainsNativeTiming'' ($global:SampleClosed -eq 1 -and (Test-CollectOrder @(''SAMPLE_NATIVE_WARNING'',''CLOSED<warning-a,warning-b>'')))',
    'Use-SampleView; $global:SampleThrow=$true; $VerbosePreference=''Continue''',
    '& { [DisplayHostContract.Row]::new(''failed-a'',''a''); [DisplayHostContract.Row]::new(''failed-b'',''a'') }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Sample_CollectedBuilderFailure_DelegatesEachOriginalOnce'' ($global:SampleClosed -eq 1 -and ([regex]::Matches($screen,''failed-a'').Count -eq 1) -and ([regex]::Matches($screen,''failed-b'').Count -eq 1) -and $screen -notmatch ''CLOSED<'')',
    'Use-SampleView',
    '& { [DisplayHostContract.Row]::new(''abort-a'',''a''); throw ''SAMPLE_ABORTED'' }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Sample_AbortedProducer_HasNoLateFrame'' ($global:SampleClosed -eq 0 -and $global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 0 -and $screen -match ''SAMPLE_ABORTED'' -and $screen -notmatch ''SINGLE<abort-a>'')',
    'Use-SampleView',
    ('Start-Transcript -LiteralPath '+(Quote-Sample ([IO.Path]::ChangeExtension($ResultsPath,'transcript.txt')))+'|Out-Null'),
    '& { [DisplayHostContract.Row]::new(''transcript-a'',''a''); [DisplayHostContract.Row]::new(''transcript-b'',''a'') }',
    'Stop-Transcript|Out-Null; Assert-Collect ''Sample_ActiveTranscript_DoesNotInvokeAnyRichBuilder'' ($global:SampleClosed -eq 0 -and $global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 0 -and $global:SampleHeaders -eq 0)',
    '$global:AdaptiveLease.Dispose()',
    ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath '+(Quote-Sample $ResultsPath)+' -Encoding UTF8'),
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned sample test exceeded 30 seconds.'}
$process.Refresh();$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed -AutoSize
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne 11 -or [DateTime]$report.RecordedUtc -lt $started){throw 'Sample host proof failed; see its result file.'}
