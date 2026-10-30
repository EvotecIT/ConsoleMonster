# Runs inside successive disposable runspaces in the same process. Returns the actual rendered cells and titles.
$ErrorActionPreference = 'Stop'
Import-Module $modulePath
$member = 'synthetic' | Get-Member -Name Length, Substring | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160
$command = Get-Command Get-Date, Get-Random | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160
$moduleFixture = New-Module -Name Demo.RunspaceInventory -ScriptBlock {
    function Get-RunspaceItem { 'synthetic' }
    Export-ModuleMember -Function Get-RunspaceItem
}
$moduleFixture | Add-Member -NotePropertyName Path -NotePropertyValue ([IO.Path]::Combine([IO.Path]::GetTempPath(), 'Demo.RunspaceInventory.psm1')) -Force
$module = $moduleFixture | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160
$history = [pscustomobject]@{
    Id = 101
    StartExecutionTime = [datetime]'2026-10-22T10:00:00'
    EndExecutionTime = [datetime]'2026-10-22T10:00:02'
    ExecutionStatus = 'Completed'
    CommandLine = 'Get-Date'
}
$history.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.HistoryInfo')
$historyText = $history | New-ConsoleTable   | Export-ConsoleFrame -Format Text -Width 160
if ($member -notmatch 'TypeName: System.String' -or $member -notmatch 'Substring' -or $member -notmatch 'int Length') { throw 'Member title or expression failed.' }
if ($command -notmatch '\[Cmdlet\]' -or $command -notmatch 'Get-Date' -or $command -notmatch 'Get-Random' -or $command -notmatch '\d+\.\d+') { throw 'Command expressions failed.' }
if ($module -notmatch 'Demo.RunspaceInventory' -or $module -notmatch 'Commands\s+1') { throw 'Module expressions failed.' }
if ($historyText -notmatch '2[.,]0 s' -or $historyText -notmatch 'Completed' -or $historyText -notmatch '101') { throw 'History expressions failed.' }
@{ Member = $member; Command = $command; Module = $module; History = $historyText }
