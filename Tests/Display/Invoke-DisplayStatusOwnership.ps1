[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath)
$ErrorActionPreference='Stop'
function Quote-Status([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1')
$commands=@(
    ('. '+(Quote-Status (Join-Path $PSScriptRoot 'CollectLoopSetup.ps1'))+' -AssemblyDirectory '+(Quote-Status $AssemblyDirectory)),
    'function global:Test-StatusReplacement { $cells=$Host.UI.RawUI.GetBufferContents($global:ReplacementArea); for($x=0;$x -lt $cells.GetLength(1);$x++){if($cells[0,$x].Character -ne ''X'' -or $cells[0,$x].ForegroundColor -ne [ConsoleColor]::Yellow -or $cells[0,$x].BackgroundColor -ne [ConsoleColor]::DarkBlue){return $false}}; return $true }',
    '& { [DisplayHostContract.Row]::new(''ownership-a'',''a''); Start-Sleep -Milliseconds 1100; [DisplayHostContract.Row]::new(''ownership-b'',''a''); $global:StatusWasVisible=Read-CollectStatus; $raw=$Host.UI.RawUI; $global:ReplacementArea=[Management.Automation.Host.Rectangle]::new(0,$raw.CursorPosition.Y-1,(''collecting 2 rows...'').Length-1,$raw.CursorPosition.Y-1); $raw.SetBufferContents($global:ReplacementArea,[Management.Automation.Host.BufferCell]::new(''X'',[ConsoleColor]::Yellow,[ConsoleColor]::DarkBlue,[Management.Automation.Host.BufferCellType]::Complete)); $cursor=$raw.CursorPosition; [DisplayHostContract.Row]::new(''ownership-c'',''a''); $global:CurrentArrivalSafe=(Test-StatusReplacement) -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and $raw.CursorPosition.X -eq $cursor.X -and $raw.CursorPosition.Y -eq $cursor.Y; [DisplayHostContract.Row]::new(''ownership-d'',''a''); $global:LaterArrivalSafe=(Test-StatusReplacement) -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and $raw.CursorPosition.X -eq $cursor.X -and $raw.CursorPosition.Y -eq $cursor.Y; [DisplayHostContract.Row]::new(''ownership-e'',''a''); $global:SubsequentArrivalSafe=(Test-StatusReplacement) -and ([DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting'') -and $raw.CursorPosition.X -eq $cursor.X -and $raw.CursorPosition.Y -eq $cursor.Y }',
    '$global:FinalReplacementSafe=Test-StatusReplacement; $global:FinalStatusAbsent=[DisplayHostContract.ConsoleText]::Read() -notmatch ''collecting''; Assert-Collect ''Collect_StatusOwnershipLost_PreservesReplacementAndDisablesCurrentAndLaterArrivals'' ($global:StatusWasVisible -and $global:CurrentArrivalSafe -and $global:LaterArrivalSafe -and $global:SubsequentArrivalSafe -and $global:FinalReplacementSafe -and $global:FinalStatusAbsent -and $global:CollectBatches.Count -eq 1 -and $global:CollectBatches[0] -eq ''5:ownership-a:ownership-e'')',
    '$global:CollectLease.Dispose()',
    ('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');StatusWasVisible=$global:StatusWasVisible;CurrentArrivalSafe=$global:CurrentArrivalSafe;LaterArrivalSafe=$global:LaterArrivalSafe;SubsequentArrivalSafe=$global:SubsequentArrivalSafe;FinalReplacementSafe=$global:FinalReplacementSafe;FinalStatusAbsent=$global:FinalStatusAbsent;Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath '+(Quote-Status $ResultsPath)+' -Encoding UTF8'),
    'exit'
)
[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(15000)){Stop-Process -Id $process.Id;throw 'The owned status proof exceeded 15 seconds.'}
$process.Refresh()
$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report|ConvertTo-Json -Depth 8
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne 1 -or [DateTime]$report.RecordedUtc -lt $started){throw 'Status ownership proof failed; see its result file.'}
