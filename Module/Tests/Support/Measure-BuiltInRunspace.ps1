# Runs inside successive disposable runspaces in the same process. Returns the actual rendered cells and titles.
$ErrorActionPreference = 'Stop'
Import-Module $modulePath
$member = 'synthetic' | Get-Member -Name Length, Substring | Show-ConsoleTable -AsString -Width 160
$command = Get-Command Get-Date, Get-Random | Show-ConsoleTable -AsString -Width 160
$module = Get-Module Microsoft.PowerShell.Utility | Show-ConsoleTable -AsString -Width 160
$history = [pscustomobject]@{
    Id = 101
    StartExecutionTime = [datetime]'2026-10-22T10:00:00'
    EndExecutionTime = [datetime]'2026-10-22T10:00:02'
    ExecutionStatus = 'Completed'
    CommandLine = 'Get-Date'
}
$history.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.HistoryInfo')
$historyText = $history | Show-ConsoleTable -AsString -Width 160
if ($member -notmatch 'TypeName: System.String' -or $member -notmatch 'Substring' -or $member -notmatch 'int Length') { throw 'Member title or expression failed.' }
if ($command -notmatch '\[Cmdlet\]' -or $command -notmatch 'Get-Date' -or $command -notmatch 'Get-Random' -or $command -notmatch '\d+\.\d+') { throw 'Command expressions failed.' }
if ($module -notmatch 'Microsoft.PowerShell.Utility' -or $module -notmatch 'Commands') { throw 'Module expressions failed.' }
if ($historyText -notmatch '2[.,]0 s' -or $historyText -notmatch 'Completed' -or $historyText -notmatch '101') { throw 'History expressions failed.' }
@{ Member = $member; Command = $command; Module = $module; History = $historyText }
