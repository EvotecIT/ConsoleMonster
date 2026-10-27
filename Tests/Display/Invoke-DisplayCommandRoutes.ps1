[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
function Quote-Route([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$textPath=[IO.Path]::ChangeExtension($ResultsPath,'console.txt')
$transcriptPath=[IO.Path]::ChangeExtension($ResultsPath,'transcript.txt')
$commands=@(
    ('. '+(Quote-Route (Join-Path $PSScriptRoot 'CommandRouteSetup.ps1'))+' -AssemblyDirectory '+(Quote-Route $AssemblyDirectory)),
    'Test-DisplayCommandRoute',
    'Assert-CommandRoute ''OwnedBroker_FormatCommandsHelpAndPaging_RecognizeDirectRoute'' ($global:RouteDirect -and $global:RoutePaged -and -not $global:RoutePagedAfter)',
    'Test-DisplayCommandRoute -HelpRoute',
    'Assert-CommandRoute ''OwnedBroker_HelpRoute_RecognizesDirectRoute'' ($global:RouteDirect -and $global:RoutePaged)',
    '$captured=Test-DisplayCommandRoute; $assignmentSafe=-not $global:RouteDirect; $text=Test-DisplayCommandRoute|Out-String; $pipelineSafe=-not $global:RouteDirect',
    'Test-DisplayCommandRoute -OutVariable routeCaptured',
    '$outVariableSafe=-not $global:RouteDirect',
    'Test-DisplayCommandRoute -PipelineVariable routePipeline',
    'Assert-CommandRoute ''OwnedBroker_AssignmentPipelineAndCaptureParameters_RemainNative'' ($assignmentSafe -and $pipelineSafe -and $outVariableSafe -and -not $global:RouteDirect -and $routeCaptured -eq ''route-output'')',
    ('Start-Transcript -LiteralPath '+(Quote-Route $transcriptPath)+'|Out-Null'),
    'Test-DisplayCommandRoute -HelpRoute',
    '$recordingSafe=-not $global:RouteDirect -and -not $global:RoutePaged; Stop-Transcript|Out-Null',
    'Test-DisplayCommandRoute -HelpRoute',
    'Assert-CommandRoute ''OwnedBroker_ActiveTranscript_BlocksFormatHelpAndPagingThenRecovers'' ($recordingSafe -and $global:RouteDirect -and $global:RoutePaged)',
    '$owned=(Get-Command Out-Default).ScriptBlock; Set-Item Function:global:Out-Default -Value ([scriptblock]::Create($owned.ToString()))',
    'Test-DisplayCommandRoute -HelpRoute',
    '$copySafe=-not $global:RouteDirect -and -not $global:RoutePaged; Set-Item Function:global:Out-Default -Value $owned',
    'Test-DisplayCommandRoute -HelpRoute',
    'Assert-CommandRoute ''CopiedBrokerBody_IsNotOwnedAndCannotEnableRichRoutes'' ($copySafe -and $global:RouteDirect)',
    '[pscustomobject]@{Name=''actual-format-table'';Value=42}|Format-Table Name,Value',
    '$screen=[DisplayHostContract.ConsoleText]::Read(); Assert-CommandRoute ''ActualFormatProxy_ProducesRichTableThroughOwnedBroker'' ($screen -match ''\|\s+actual-format-table\s+\|\s+42\s+\|'')',
    'Disable-ConsoleFormatting -Feature FormatCommands',
    'Test-DisplayCommandRoute',
    '$formatDisabled=-not $global:RouteDirect',
    'Test-DisplayCommandRoute -HelpRoute',
    'Assert-CommandRoute ''FeatureDisable_LeavesHelpEnabledAndDisablesFormatRoute'' ($formatDisabled -and $global:RouteDirect -and $global:RoutePaged)',
    ('$screen=[DisplayHostContract.ConsoleText]::Read(); $screen|Set-Content -LiteralPath '+(Quote-Route $textPath)+' -Encoding UTF8'),
    'Remove-Module $global:RouteModule.Name',
    ('[pscustomobject]@{RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Runtime=$PSVersionTable.PSVersion.ToString();Tests=$global:RouteResults.ToArray();Passed=@($global:RouteResults|Where-Object Passed).Count;Failed=@($global:RouteResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath '+(Quote-Route $ResultsPath)+' -Encoding UTF8'),
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned command route test exceeded 30 seconds.'}
$process.Refresh()
if(-not(Test-Path -LiteralPath $ResultsPath)){throw 'Command route proof did not produce its result file.'}
$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed -AutoSize
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne 7 -or [DateTime]$report.RecordedUtc -lt $started){throw 'Command route proof failed; see the result and console text files.'}
