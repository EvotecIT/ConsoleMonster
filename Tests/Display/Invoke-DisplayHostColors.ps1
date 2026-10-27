[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
function Quote-HostColors([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$commands=@(
    ('. '+(Quote-HostColors (Join-Path $PSScriptRoot 'HostColorsLoopSetup.ps1'))+' -AssemblyDirectory '+(Quote-HostColors $AssemblyDirectory)),
    '[DisplayHostContract.Row]::new(''SINGLE-ROW'',''a'')',
    'Assert-HostColors ''HostColors_Single_OrdinaryLightRetainsBodyAndCurrentPair'' ((Test-ColorContexts 1) -and (Test-ColorCells ''LIGHT-FRAME'') -and (Test-ColorCells ''SINGLE-ROW'' -BlackText))',
    'Use-ColorView -Batch Collect',
    '& { [DisplayHostContract.Row]::new(''COLLECT-A'',''a''); [DisplayHostContract.Row]::new(''COLLECT-B'',''a'') }',
    'Assert-HostColors ''HostColors_Collect_OrdinaryLightRetainsWholeFrame'' ((Test-ColorContexts 1) -and (Test-ColorCells ''COLLECT-A'' -BlackText) -and (Test-ColorCells ''COLLECT-B'' -BlackText))',
    'Use-ColorView -Batch Stream',
    '& { [DisplayHostContract.Row]::new(''STREAM-A'',''a''); $global:ColorAfterOpening=Read-HostColorPair; Write-Warning ''LIGHT-NATIVE-WARNING''; Write-Verbose ''LIGHT-NATIVE-VERBOSE'' -Verbose; Write-Information ''LIGHT-NATIVE-INFORMATION'' -InformationAction Continue; [DisplayHostContract.Row]::new(''STREAM-B'',''a'') }',
    'Assert-HostColors ''HostColors_Stream_OpeningPairSurvivesNativeMessagesAndFooter'' ((Test-ColorContexts 3) -and $global:ColorAfterOpening.ConsoleAttributes -eq 240 -and (Test-ColorCells ''LIGHT-HEADER'') -and (Test-ColorCells ''STREAM-A'' -BlackText) -and (Test-ColorCells ''STREAM-B'' -BlackText) -and (Test-ColorCells ''LIGHT-FOOTER'' -BlackText) -and [DisplayHostContract.ConsoleText]::Read().Contains(''LIGHT-NATIVE-WARNING'') -and [DisplayHostContract.ConsoleText]::Read().Contains(''LIGHT-NATIVE-VERBOSE'') -and [DisplayHostContract.ConsoleText]::Read().Contains(''LIGHT-NATIVE-INFORMATION''))',
    'Use-ColorView -Batch Stream -Adaptive',
    '[DisplayHostContract.Row]::new(''ADAPTIVE-ONE'',''a'')',
    'Assert-HostColors ''HostColors_AdaptiveOne_UsesCurrentLightPair'' ((Test-ColorContexts 1) -and $global:ColorContexts[0].Role -eq ''Single'' -and (Test-ColorCells ''LIGHT-ONE'') -and (Test-ColorCells ''ADAPTIVE-ONE'' -BlackText))',
    'Use-ColorView -Batch Stream -Adaptive',
    '& { [DisplayHostContract.Row]::new(''ADAPTIVE-A'',''a''); [DisplayHostContract.Row]::new(''ADAPTIVE-B'',''b'') }',
    'Assert-HostColors ''HostColors_AdaptiveMany_RowsAndFooterRetainOpeningPair'' ((Test-ColorContexts 3) -and (Test-ColorCells ''ADAPTIVE-A'' -BlackText) -and (Test-ColorCells ''ADAPTIVE-B'' -BlackText) -and (Test-ColorCells ''LIGHT-FOOTER'' -BlackText))',
    'Use-ColorView; Set-OrdinaryHostColors -Foreground Yellow -Background DarkBlue',
    '[DisplayHostContract.Row]::new(''NEW-HOST-PAIR'',''a'')',
    'Assert-HostColors ''HostColors_NewFrame_ResnapshotsOrdinaryHostCustomization'' ((Test-ColorContexts 1 -Foreground Yellow -Background DarkBlue) -and (Test-ColorCells ''NEW-HOST-PAIR'' -Background DarkBlue)) -ExpectedAttributes 30',
    'Use-ColorView -Batch Collect -Records 1',
    '& { [DisplayHostContract.Row]::new(''NATIVE-FIRST'',''a''); [DisplayHostContract.Row]::new(''NATIVE-LAST'',''a'') }',
    'Assert-HostColors ''HostColors_NativeBudgetFooter_RetainsCurrentLightPair'' ($global:ColorContexts.Count -eq 0 -and (Test-ColorCells ''collection budget exceeded'') -and [DisplayHostContract.ConsoleText]::Read().Contains(''NATIVE-FIRST'') -and [DisplayHostContract.ConsoleText]::Read().Contains(''NATIVE-LAST''))',
    'Use-ColorView -Batch Collect; $global:ColorBroken=$true',
    '& { [DisplayHostContract.Row]::new(''FALLBACK-A'',''a''); [DisplayHostContract.Row]::new(''FALLBACK-B'',''a'') }',
    'Assert-HostColors ''HostColors_ThrowingWidget_NativeOriginalsWithoutPartialFrame'' ((Test-ColorContexts 1) -and [DisplayHostContract.ConsoleText]::Read().Contains(''FALLBACK-A'') -and [DisplayHostContract.ConsoleText]::Read().Contains(''FALLBACK-B'') -and -not [DisplayHostContract.ConsoleText]::Read().Contains(''PARTIAL_RICH_MUST_NOT_APPEAR''))',
    'Use-ColorView; $global:ColorLease.Mode=''Plain''',
    '[DisplayHostContract.Row]::new(''PLAIN-ROW'',''a'')',
    'Assert-HostColors ''HostColors_Plain_NoColorSnapshotOrDecoration'' ((Test-ColorContexts 1 -Plain) -and (Test-ColorCells ''PLAIN-ROW'' -BlackText))',
    'Use-ColorView; $env:NO_COLOR=''1''; [ConsoleMonster.Terminal]::Refresh()',
    '[DisplayHostContract.Row]::new(''NO-COLOR-ROW'',''a'')',
    'Assert-HostColors ''HostColors_NoColor_NoColorSnapshotOrDecoration'' ((Test-ColorContexts 1 -Plain) -and (Test-ColorCells ''NO-COLOR-ROW'' -BlackText))',
    'Use-ColorView; $original=[DisplayHostContract.Row]::new(''CAPTURE-ROW'',''a''); $names=@($original.PSObject.TypeNames); $assigned=$original; $text=$original|Out-String; $formatted=$original|Format-Table Name|Out-String; $json=$original|ConvertTo-Json -Compress',
    'Assert-HostColors ''HostColors_CaptureAndFormat_PreserveNativeObjectAndTypeNames'' ($global:ColorContexts.Count -eq 0 -and [object]::ReferenceEquals($assigned,$original) -and ($original.PSObject.TypeNames -join ''|'') -ceq ($names -join ''|'') -and $text.Contains(''CAPTURE-ROW'') -and $formatted.Contains(''CAPTURE-ROW'') -and $json.Contains(''CAPTURE-ROW''))',
    'Use-ColorView',
    ('Start-Transcript -LiteralPath '+(Quote-HostColors ([IO.Path]::ChangeExtension($ResultsPath,'transcript.txt')))+'|Out-Null'),
    '[DisplayHostContract.Row]::new(''TRANSCRIPT-ROW'',''a'')',
    'Stop-Transcript|Out-Null; Assert-HostColors ''HostColors_ActiveTranscript_LeavesNativeRoute'' ($global:ColorContexts.Count -eq 0 -and -not [DisplayHostContract.ConsoleText]::Read().Contains(''LIGHT-FRAME''))',
    'if($PSVersionTable.PSVersion.Major -ge 7){Use-ColorView; $saved=$PSStyle.OutputRendering; $PSStyle.OutputRendering=''PlainText''}',
    'if($PSVersionTable.PSVersion.Major -ge 7){[DisplayHostContract.Row]::new(''PSSTYLE-PLAIN'',''a'')}',
    'if($PSVersionTable.PSVersion.Major -ge 7){Assert-HostColors ''HostColors_PSStylePlain_NoColorSnapshotOrDecoration'' ((Test-ColorContexts 1 -Plain) -and (Test-ColorCells ''PSSTYLE-PLAIN'' -BlackText)); $PSStyle.OutputRendering=$saved}',
    '$global:ColorLease.Dispose()',
    ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath '+(Quote-HostColors $ResultsPath)+' -Encoding UTF8'),
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned host-color proof exceeded 30 seconds.'}
$process.Refresh();$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed -AutoSize
$expected=if([version]$report.Runtime -ge [version]'7.0'){13}else{12}
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne $expected -or [DateTime]$report.RecordedUtc -lt $started){throw 'Native host-color proof failed; see its result file.'}
