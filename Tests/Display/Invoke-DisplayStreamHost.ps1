[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$RuntimePath,[Parameter(Mandatory=$true)][string]$AssemblyDirectory,[Parameter(Mandatory=$true)][string]$ResultsPath,[int]$Width=120)
$ErrorActionPreference='Stop'
function Quote-StreamHost([string]$Text){"'"+$Text.Replace("'","''")+"'"}
$commands=New-Object 'System.Collections.Generic.List[string]'
$commands.Add('. '+(Quote-StreamHost (Join-Path $PSScriptRoot 'StreamHostSetup.ps1'))+' -AssemblyDirectory '+(Quote-StreamHost $AssemblyDirectory)+' -Width '+$Width)
foreach($glyph in 'Full','Ascii'){foreach($plain in $false,$true){
    $commands.Add('Use-StreamHost -Glyph '+$glyph+' -Plain $'+$plain.ToString().ToLowerInvariant())
    $commands.Add('& { [DisplayHostContract.Row]::new(''DFS'',''a''); [DisplayHostContract.Row]::new(''DFS Replication'',''a''); [DisplayHostContract.Row]::new(''Backups and capacity unknown on an exceptionally long volume label'',''a''); ''stream-native-tail'' }')
    $commands.Add('Assert-StreamHost '+(Quote-StreamHost ('Stream_RealHost_LaterLongerRows_'+$Width+'_'+$glyph+'_'+$(if($plain){'NoColor'}else{'Color'}))))
}}
$commands.Add('$global:StreamHostLease.Dispose()')
$commands.Add('[pscustomobject]@{Runtime=$PSVersionTable.PSVersion.ToString();RecordedUtc=[DateTime]::UtcNow.ToString(''o'');Tests=$global:DisplayLoopResults.ToArray();Failed=@($global:DisplayLoopResults|Where-Object{-not $_.Passed}).Count}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath '+(Quote-StreamHost $ResultsPath)+' -Encoding UTF8')
$commands.Add('exit')
$inputPath=[IO.Path]::ChangeExtension($ResultsPath,'input.ps1');[IO.File]::WriteAllLines($inputPath,$commands)
$started=[DateTime]::UtcNow
$process=Start-Process -FilePath $RuntimePath -ArgumentList @('-NoLogo','-NoProfile','-Command','-') -RedirectStandardInput $inputPath -WindowStyle Hidden -PassThru
[pscustomobject]@{OwnedPid=$process.Id;Results=$ResultsPath}|ConvertTo-Json -Compress
if(-not $process.WaitForExit(30000)){Stop-Process -Id $process.Id;throw 'The owned stream host exceeded 30 seconds.'}
$process.Refresh();$report=Get-Content -LiteralPath $ResultsPath -Raw|ConvertFrom-Json
$report.Tests|Format-Table Name,Passed,BufferWidth,ProfileWidth -AutoSize
if($process.ExitCode -ne 0 -or $report.Failed -or $report.Tests.Count -ne 4 -or [DateTime]$report.RecordedUtc -lt $started){throw 'Native stream host proof failed.'}
