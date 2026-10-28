[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
function Quote-Adaptive([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$commands=@(
    ('. '+(Quote-Adaptive (Join-Path $PSScriptRoot 'AdaptiveLoopSetup.ps1'))+' -AssemblyDirectory '+(Quote-Adaptive $AssemblyDirectory)),
    '[DisplayHostContract.Row]::new(''one'',''a'')',
    'Assert-Collect ''Adaptive_OneAtEnd_UsesSingleRecordBuilderOnly'' ($global:AdaptiveSingles -eq 1 -and $global:AdaptiveRows -eq 0 -and $global:AdaptiveFooters -eq 0 -and (Test-CollectOrder @(''SINGLE<one>'')))',
    'Use-AdaptiveView',
    '& { [DisplayHostContract.Row]::new(''many-a'',''computer-a''); [DisplayHostContract.Row]::new(''many-b'',''computer-b''); Start-Sleep -Milliseconds 300; [DisplayHostContract.Row]::new(''many-c'',''computer-c'') }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Adaptive_ManyWithoutScopeKey_StreamsAcrossDifferentComputers'' ($global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 3 -and $global:AdaptiveFooters -eq 1 -and ([regex]::Matches($screen,''AdaptiveName'').Count -eq 1) -and (Test-CollectOrder @(''many-a'',''many-b'',''many-c'',''ADAPTIVE_FOOTER<3>'')))',
    'Use-AdaptiveView -Grouped',
    '& { [DisplayHostContract.Row]::new(''scope-one'',''a''); [DisplayHostContract.Row]::new(''scope-two'',''b'') }',
    'Assert-Collect ''Adaptive_OnePerScope_ClosesEachAsSingle'' ($global:AdaptiveSingles -eq 2 -and $global:AdaptiveRows -eq 0 -and (Test-CollectOrder @(''SINGLE<scope-one>'',''SINGLE<scope-two>'')))',
    'Use-AdaptiveView',
    '& { [DisplayHostContract.Row]::new(''type-one'',''a''); ''adaptive-native-boundary''; [DisplayHostContract.Row]::new(''type-two'',''a'') }',
    'Assert-Collect ''Adaptive_NativeTypeBoundary_FlushesOneOriginalBeforeNative'' ($global:AdaptiveSingles -eq 2 -and (Test-CollectOrder @(''SINGLE<type-one>'',''adaptive-native-boundary'',''SINGLE<type-two>'')))',
    'Use-AdaptiveView',
    '& { [DisplayHostContract.Row]::new(''error-one'',''a''); Write-Error ''ADAPTIVE_ERROR'' -ErrorAction Continue; [DisplayHostContract.Row]::new(''error-two'',''a'') }',
    'Assert-Collect ''Adaptive_ObservableErrorBeforeSecond_FlushesSingleFirst'' ($global:AdaptiveSingles -eq 2 -and (Test-CollectOrder @(''SINGLE<error-one>'',''ADAPTIVE_ERROR'',''SINGLE<error-two>'')))',
    'Use-AdaptiveView',
    '& { [DisplayHostContract.Row]::new(''merge-one'',''a''); Write-Warning ''ADAPTIVE_WARNING''; [DisplayHostContract.Row]::new(''merge-two'',''a''); Write-Verbose ''ADAPTIVE_VERBOSE'' -Verbose; [DisplayHostContract.Row]::new(''merge-three'',''a''); Write-Information ''ADAPTIVE_INFORMATION'' -InformationAction Continue; [DisplayHostContract.Row]::new(''merge-four'',''a'') } *>&1',
    'Assert-Collect ''Adaptive_MergedBoundaries_PreserveProducerOrder'' ($global:AdaptiveSingles -eq 4 -and $global:AdaptiveRows -eq 0 -and (Test-CollectOrder @(''SINGLE<merge-one>'',''ADAPTIVE_WARNING'',''SINGLE<merge-two>'',''ADAPTIVE_VERBOSE'',''SINGLE<merge-three>'',''ADAPTIVE_INFORMATION'',''SINGLE<merge-four>'')))',
    'Use-AdaptiveView -View adaptive.single.failure; $global:AdaptiveThrowSingle=$true; $VerbosePreference=''Continue''',
    '[DisplayHostContract.Row]::new(''single-fallback'',''a'')',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Adaptive_SingleBuilderFailure_DelegatesOriginalWithoutPartialFrame'' ($screen -match ''single-fallback'' -and $screen -notmatch ''PARTIAL_RICH_MUST_NOT_APPEAR'' -and ([regex]::Matches($screen,''used native formatting'').Count -eq 1))',
    'Use-AdaptiveView -View adaptive.pair.failure; $global:AdaptiveThrowRow=''pair-second''',
    '& { [DisplayHostContract.Row]::new(''pair-first'',''a''); [DisplayHostContract.Row]::new(''pair-second'',''a'') }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Adaptive_SecondRowFailure_ReplaysBothOriginalsWithoutPartialFirst'' ($global:AdaptiveRows -eq 2 -and $screen -notmatch ''AdaptiveName'' -and ([regex]::Matches($screen,''pair-first'').Count -eq 1) -and ([regex]::Matches($screen,''pair-second'').Count -eq 1) -and (Test-CollectOrder @(''pair-first'',''pair-second'')))',
    'Use-AdaptiveView -View adaptive.later.failure; $global:AdaptiveThrowRow=''later-third''',
    '& { [DisplayHostContract.Row]::new(''later-first'',''a''); Start-Sleep -Milliseconds 300; [DisplayHostContract.Row]::new(''later-second'',''a''); [DisplayHostContract.Row]::new(''later-third'',''a''); [DisplayHostContract.Row]::new(''later-fourth'',''a'') }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-Collect ''Adaptive_LaterRowFailure_EmitsEachOriginalOnceWithoutFalseSummary'' ($global:AdaptiveRows -eq 3 -and $global:AdaptiveFooters -eq 0 -and (Test-CollectOrder @(''later-first'',''later-second'',''later-third'',''later-fourth'')) -and ([regex]::Matches($screen,''later-first'').Count -eq 1))',
    'Use-AdaptiveView',
    '& { [DisplayHostContract.Row]::new(''frozen-one'',''a''); $global:AdaptiveView.SingleRecordBuilder=[Func[object,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($value,$context) throw ''late replacement must not run''} }',
    'Assert-Collect ''Adaptive_SingleCallback_IsFrozenAtFirstRecord'' ($global:AdaptiveSingles -eq 1 -and (Test-CollectOrder @(''SINGLE<frozen-one>'')))',
    'Use-AdaptiveView',
    ('Start-Transcript -LiteralPath '+(Quote-Adaptive ([IO.Path]::ChangeExtension($ResultsPath,'transcript.txt')))+'|Out-Null'),
    '& { [DisplayHostContract.Row]::new(''transcript-one'',''a''); [DisplayHostContract.Row]::new(''transcript-two'',''a'') }',
    'Stop-Transcript|Out-Null; Assert-Collect ''Adaptive_ActiveTranscript_NeverInvokesEitherBuilder'' ($global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 0)',
    'Use-AdaptiveView; $original=[DisplayHostContract.Row]::new(''adaptive-capture'',''a''); $names=@($original.PSObject.TypeNames); $assigned=$original; $text=$original|Out-String',
    'Assert-Collect ''Adaptive_Capture_PreservesOriginalAndTypeNames'' ($global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 0 -and [object]::ReferenceEquals($assigned,$original) -and ($names -join ''|'') -ceq ($original.PSObject.TypeNames -join ''|'') -and $text -match ''adaptive-capture'')',
    'Use-AdaptiveView -Bytes 512',
    '& { [DisplayHostContract.Row]::new(''budget-first'',''a''); [DisplayHostContract.Row]::new((''budget-second-''+(''x''*300)),''a''); [DisplayHostContract.Row]::new(''budget-third'',''a'') }',
    'Assert-Collect ''Adaptive_PendingByteBudget_ReplaysBothAndContinuesNatively'' ($global:AdaptiveSingles -eq 0 -and $global:AdaptiveRows -eq 0 -and (Test-CollectOrder @(''budget-first'',''budget-second-'',''budget-third'')))',
    '$global:AdaptiveLease.Dispose()',
    ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath '+(Quote-Adaptive $ResultsPath)+' -Encoding UTF8'),
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned adaptive test exceeded 30 seconds.'}
$process.Refresh();$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed -AutoSize
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne 13 -or [DateTime]$report.RecordedUtc -lt $started){throw 'Adaptive host proof failed; see its result file.'}
