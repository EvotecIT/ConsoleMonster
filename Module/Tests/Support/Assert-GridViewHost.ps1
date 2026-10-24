<#
.SYNOPSIS
Checks unavailable-grid native input and command restoration in a real terminal.
.DESCRIPTION
Runs in an isolated initial command namespace; the host's native Out-GridView is retained. The SiteFrames driver sends
keys only after GRIDVIEW_READY. Synthetic rows are checked by identity and order; redirected launches fail.
.PARAMETER Case
The native-key contract to execute.
#>
[CmdletBinding()]
param([ValidateSet('Single', 'Multiple', 'Wait', 'Browse', 'Captures')] [string] $Case)
$ErrorActionPreference = 'Stop'
if ([Console]::IsOutputRedirected -or [Console]::IsInputRedirected) { throw 'A native console is required.' }
. (Join-Path $PSScriptRoot 'Invoke-GridViewSession.ps1')
$script = @'
$rows = @([pscustomobject]@{Name='alpha';Id=1}, [pscustomobject]@{Name='beta';Id=2})
Enable-ConsoleFormatting -Feature GridView
if ((Get-Command Out-GridView).CommandType -ne 'Function') { throw 'Fallback missing.' }
[Console]::Out.WriteLine('GRIDVIEW_READY')
$picked = @($rows | Out-GridView -Title 'Native keyboard proof' __ARGS__)
if (($picked.Name -join ',') -ne '__EXPECTED__') { throw ('Wrong native-key selection: ' + ($picked.Name -join ',')) }
foreach ($row in $picked) { if (-not [object]::ReferenceEquals($row, $rows[$row.Id-1])) { throw 'Identity lost.' } }
Disable-ConsoleFormatting
if (Get-Command Out-GridView -ErrorAction Ignore) { throw 'Fallback remained after disable.' }
'HOST_OK'
'@
$arguments = switch ($Case) { Single { '-OutputMode Single' }; Multiple { '-PassThru' }; Wait { '-Wait' }; Browse { '' } }
$expected = switch ($Case) { Single { 'beta' }; Multiple { 'alpha,beta' }; default { '' } }
if ($Case -ne 'Captures') {
    Invoke-GridViewSession ($script.Replace('__ARGS__', $arguments).Replace('__EXPECTED__', $expected))
    return
}

$scratch = Join-Path ([IO.Path]::GetTempPath()) ('cm-grid-host-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $scratch
try {
    # The native branch needs no input: zero input validates command resolution and capture routes without opening a GUI.
    Invoke-GridViewSession -Native @'
$before = Get-Command Out-GridView
$empty = @()
$original = @($empty | Out-GridView -PassThru | Out-String)
Enable-ConsoleFormatting -Feature GridView -Force
if ((Get-Command Out-GridView) -ne $before) { throw 'Native command replaced.' }
$after = @($empty | Out-GridView -PassThru | Out-String)
if (($original -join '') -cne ($after -join '')) { throw 'Native capture changed.' }
Disable-ConsoleFormatting
'NATIVE_CAPTURE_OK'
'@
    $body = @'
Enable-ConsoleFormatting -Feature GridView
$rows = @([pscustomobject]@{Name='alpha';Id=1}, [pscustomobject]@{Name='beta';Id=2})
$file = '__PATH__/empty.txt'
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.NonInteractive = $true
$rows | Out-GridView -PassThru -WarningAction SilentlyContinue > $file
if ((Get-Item $file).Length -ne 0) { throw 'Display leaked into file success stream.' }
$text = $rows | Out-GridView -PassThru -WarningAction SilentlyContinue | Out-String
if ($text) { throw 'Display text leaked into success output.' }
Start-Transcript -LiteralPath '__PATH__/transcript.txt' | Out-Null
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.NonInteractive = $false
try { $picked = @($rows | Out-GridView -OutputMode Single -WarningAction SilentlyContinue) }
finally { Stop-Transcript | Out-Null }
if ($picked.Count -ne 0) { throw 'Transcript fabricated a selection.' }
$transcript = [IO.File]::ReadAllText('__PATH__/transcript.txt')
if ($transcript -notmatch 'alpha' -or $transcript -match ([string][char]27 + '\[')) { throw 'Transcript lacks plain table output.' }
Disable-ConsoleFormatting
'CAPTURE_OK'
'@
    Invoke-GridViewSession ($body.Replace('__PATH__', $scratch.Replace('\','/')))
    $job = Start-Job -ArgumentList (Join-Path $PSScriptRoot 'Invoke-GridViewSession.ps1') -ScriptBlock {
        param($helper)
        . $helper
        Invoke-GridViewSession @'
Enable-ConsoleFormatting -Feature GridView
$rows = @([pscustomobject]@{Name='job-alpha';Id=1})
$result = @($rows | Out-GridView -PassThru -WarningAction SilentlyContinue)
if ($result.Count -ne 0) { throw 'Job fabricated a selection.' }
Disable-ConsoleFormatting
'JOB_OK'
'@
    }
    try {
        $null = Wait-Job $job -Timeout 30
        if ($job.State -ne 'Completed') { throw ('Job state: ' + $job.State) }
        $output = @(Receive-Job $job)
        if ($output -notcontains 'JOB_OK') { throw 'Job receipt missing.' }
    } finally {
        if ($job.State -in @('Completed','Failed','Stopped')) { Remove-Job $job }
    }
    'HOST_OK'
} finally {
    # Only the three files made by this assertion are removed. The task's containing directory is retained.
    foreach ($name in 'empty.txt','transcript.txt') {
        $file = Join-Path $scratch $name
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $scratch
}
