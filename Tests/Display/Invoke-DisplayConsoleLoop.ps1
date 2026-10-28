[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
$setup=Join-Path $PSScriptRoot 'ConsoleLoopSetup.ps1'
function Quote-Loop([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$textPath=[IO.Path]::ChangeExtension($ResultsPath,'console.txt')
$transcriptPath=[IO.Path]::ChangeExtension($ResultsPath,'transcript.txt')
$commands=@(
    ('. '+(Quote-Loop $setup)+' -AssemblyDirectory '+(Quote-Loop $AssemblyDirectory)+' -ResultsPath '+(Quote-Loop $ResultsPath)),
    '[DisplayHostContract.Row]::new(''single-host'',''a'')',
    'Assert-ConsoleLoop ''AutomaticSubmission_Single_Renders'' ($global:DisplayLoopBuilds -eq 1) ''No forced TopLevel flag; actual ConsoleHost command loop.''',
    '$global:BeforeNative=$global:DisplayLoopBuilds; $r=[DisplayHostContract.Row]::new(''native-data'',''a''); $names=@($r.PSObject.TypeNames); $assigned=$r; $txt=$r|Out-String; $ft=$r|Format-Table Name|Out-String; $json=$r|ConvertTo-Json -Compress',
    'Assert-ConsoleLoop ''NativeSinksAndIdentity_ArePreserved'' (($global:DisplayLoopBuilds -eq $global:BeforeNative) -and [object]::ReferenceEquals($r,$assigned) -and ($txt -match ''native-data'') -and ($ft -match ''native-data'') -and ($json -match ''native-data'') -and (($names -join ''|'') -ceq ($r.PSObject.TypeNames -join ''|''))) ''Assignment, Out-String, explicit Format-Table and JSON stay native.''',
    ('Start-Transcript -LiteralPath '+(Quote-Loop $transcriptPath)+'|Out-Null'),
    '[DisplayHostContract.Row]::new(''transcript-native'',''a'')',
    'Stop-Transcript|Out-Null',
    ('$transcript=Get-Content -LiteralPath '+(Quote-Loop $transcriptPath)+' -Raw'),
    'Assert-ConsoleLoop ''ActiveTranscript_IsNative'' (($global:DisplayLoopBuilds -eq $global:BeforeNative) -and ($transcript -match ''transcript-native'') -and ($transcript -notmatch ''RICH transcript-native'')) ''Actual Start-Transcript lifecycle with automatic output.''',
    '$env:NO_COLOR=''1''; [ConsoleMonster.Terminal]::Refresh()',
    '[DisplayHostContract.Row]::new(''plain-host'',''a'')',
    'Assert-ConsoleLoop ''NoColor_PhysicalPlainProfile_KeepsLayout'' (($global:DisplayLoopBuilds -eq 2) -and ($global:DisplayLoopLastMode -eq ''Plain'')) ''Physical console handle remains eligible in Plain mode.''',
    '$global:DisplayStreamBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($objects,$context) $t=[ConsoleMonster.Widgets.Table]::new(); $t.Title=''stream-scope''; $c=[ConsoleMonster.Widgets.TableColumn]::new(''StreamColumn''); $c.Width=22; $null=$t.AddColumn($c); $null=$t.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name))); $t}',
    '$global:DisplayLoopView=$global:DisplayLoopLease.Register(''DisplayHostContract.Row'',''loop.row'',[version]''1.1.0'',''Stream'',[string[]]@(''Scope''),$global:DisplayStreamBuilder,$null)',
    '$global:DisplayLoopView.Streaming=[ConsoleMonster.PowerShell.Display.DefaultDisplayStreaming]::new([Func[object]]{0},[Func[object,object,object]]{param($state,$row) ([int]$state)+1},[Func[object,long,ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($state,$count,$context) $global:DisplayLoopSummaries++; [ConsoleMonster.Widgets.TextBlock]::Plain(''closed-scope ''+$count)})',
    '& { [DisplayHostContract.Row]::new(''stream-before'',''scope-a''); Start-Sleep -Milliseconds 300; [DisplayHostContract.Row]::new(''stream-open'',''scope-a''); Write-Warning ''STREAM_WARNING''; Write-Verbose ''STREAM_VERBOSE'' -Verbose; [DisplayHostContract.Row]::new(''stream-after'',''scope-a'') }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); $a=$screen.IndexOf(''stream-before''); $w=$screen.IndexOf(''STREAM_WARNING''); $v=$screen.IndexOf(''STREAM_VERBOSE''); $b=$screen.IndexOf(''stream-after''); Assert-ConsoleLoop ''Stream_UnmergedHostStreams_OrderedAndOneSummary'' (($a -ge 0) -and ($a -lt $w) -and ($w -lt $v) -and ($v -lt $b) -and ($global:DisplayLoopSummaries -eq 1) -and ([regex]::Matches($screen,''StreamColumn'').Count -eq 1)) ''Owned console buffer: row, warning, verbose, row; one header and one group closure.''',
    '$ebefore=[DisplayHostContract.Row]::new(''error-before'',''scope-e''); $eafter=[DisplayHostContract.Row]::new(''error-after'',''scope-e'')',
    '& { $ebefore; Write-Error ''STREAM_ERROR'' -ErrorAction Continue; $eafter }',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); $a=$screen.IndexOf(''error-before''); $e=$screen.IndexOf(''STREAM_ERROR''); $b=$screen.IndexOf(''error-after''); Assert-ConsoleLoop ''Stream_AutomaticErrorBoundary_PreservesProducerOrder'' (($a -ge 0) -and ($a -lt $e) -and ($e -lt $b) -and ($global:DisplayLoopSummaries -eq 1)) ''Owned console buffer: row, native error, row; quick samples close on each side without invoking open-stream summaries.''',
    '$global:DisplayLoopView=$global:DisplayLoopLease.Register(''DisplayHostContract.Row'',''loop.row'',[version]''1.2.0'',''Collect'',[string[]]@(''Scope''),$global:DisplayLoopBuilder,$null)',
    '[DisplayHostContract.Row]::new(''collect-native'',''a'')',
    'Assert-ConsoleLoop ''Collect_AcceptedNativeTiming_RendersAtEnd'' ($global:DisplayLoopBuilds -eq 3) ''Collect renders at the normal end; independent native stream timing is retained.''',
    '$global:FailureBuilder=[Func[System.Collections.Generic.IReadOnlyList[object],ConsoleMonster.PowerShell.Display.DefaultDisplayContext,ConsoleMonster.Layout.IWidget]]{param($objects,$context) [DisplayHostContract.BrokenWidget]::new()}; $global:DisplayLoopView=$global:DisplayLoopLease.Register(''DisplayHostContract.Row'',''loop.row'',[version]''1.3.0'',''Single'',$null,$global:FailureBuilder,$null); $VerbosePreference=''Continue''',
    '[DisplayHostContract.Row]::new(''failed-widget-one'',''a'')',
    '[DisplayHostContract.Row]::new(''failed-widget-two'',''a'')',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-ConsoleLoop ''RenderFailure_NoPartialWrite_OnceVerboseAndNativeFallback'' (($screen -notmatch ''PARTIAL_RICH_MUST_NOT_APPEAR'') -and ($screen -match ''failed-widget-one'') -and ($screen -match ''failed-widget-two'') -and ([regex]::Matches($screen,''used native formatting'').Count -eq 1)) ''A widget draws then throws during preparation; only native rows and one verbose diagnostic reach the host.''',
    '$global:DisplayLoopLease.Dispose(); $global:DisplayLoopLease=[ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule(''Selector'',[version]''1.0.0'',[string[]]@(''DisplayHostContract.Row'')); $selector=[Func[object,string]]{param($value) throw ''missing-run-context''}; $null=$global:DisplayLoopLease.Register(''DisplayHostContract.Row'',''scope.selector'',[version]''1.0.0'',''Single'',$null,$global:DisplayLoopBuilder,$null,$selector)',
    '[DisplayHostContract.Row]::new(''scope-native-one'',''a'')',
    '[DisplayHostContract.Row]::new(''scope-native-two'',''a'')',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-ConsoleLoop ''ScopeSelectorFailure_IsNative_OnceVerbose'' (($screen -match ''scope-native-one'') -and ($screen -match ''scope-native-two'') -and ([regex]::Matches($screen,''missing-run-context'').Count -eq 1) -and ($global:DisplayLoopBuilds -eq 3)) ''Selector failure preserves both original records and emits one view diagnostic.''',
    ('$screen | Set-Content -LiteralPath '+(Quote-Loop $textPath)+' -Encoding UTF8'),
    '$global:DisplayLoopLease.Dispose()',
    '[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();PhysicalProfile=[ConsoleMonster.Terminal]::GetCurrentProfile();OutputRedirected=[Console]::IsOutputRedirected;ErrorRedirected=[Console]::IsErrorRedirected;InputRedirected=[Console]::IsInputRedirected;Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $global:DisplayLoopResultsPath -Encoding UTF8',
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned console test exceeded 30 seconds.'}
$process.Refresh()
if(-not(Test-Path -LiteralPath $ResultsPath)){throw 'The console test did not produce its result file.'}
$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed -AutoSize
if($process.ExitCode -ne 0 -or $report.Failed){throw 'Console host contracts failed; see the result and console text files.'}
