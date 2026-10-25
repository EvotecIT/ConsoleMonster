param(
    [Parameter(Mandatory)][object[]] $TypeCases
)
<#
.SYNOPSIS
Runs every input type of the type sweep through every everyday command (in the child process of ConsoleMonster.Types.Tests.ps1).

.DESCRIPTION
Each cell is written as '@@CELL <type>|<command>', its output, then '@@ERR <message>' when it threw and '@@MS <milliseconds>'.
A last cell times a table of objects whose getters stall.

.PARAMETER TypeCases
The cases from Support\TypeCases.ps1 (Name and Make, a script that creates the input).
#>
$commands = [ordered] @{
    'Show-ConsoleTable'   = { param($x, $y) $x | Show-ConsoleTable -Width 120 }
    'Show-ConsoleList'    = { param($x, $y) $x | Show-ConsoleList -Width 120 }
    'Show-ConsoleTree'    = { param($x, $y) $x | Show-Console -Width 120 -View Tree }
    'Show-ConsoleJson'    = { param($x, $y) $x | Show-Console -Width 120 -View Json }
    'Show-Console'          = { param($x, $y) $x | Show-Console -Width 120 }
    'Out-ConsoleDefault'    = { param($x, $y) $x | Out-ConsoleDefault -Force }
    'Compare-ConsoleObject' = { param($x, $y) Compare-ConsoleObject -ReferenceObject $x -DifferenceObject $y }
}
$ErrorActionPreference = 'Stop'
foreach ($case in $TypeCases) {
    $make = [scriptblock]::Create($case.Make)
    foreach ($command in $commands.Keys) {
        "@@CELL $($case.Name)|$command"
        $clock = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $x = & $make
            $y = & $make
            & $commands[$command] $x $y
        } catch {
            '@@ERR ' + ($_.Exception.Message -replace '\s+', ' ')
        }
        '@@MS ' + $clock.ElapsedMilliseconds
    }
}
# Getters that stall (remote objects): the table must not wait for all of them.
'@@CELL SlowGetters|Show-ConsoleTable'
$slow = [pscustomobject] @{ Name = 'slow' }
foreach ($i in 1..8) {
    $slow | Add-Member -MemberType ScriptProperty -Name ('P' + $i) -Value { Start-Sleep -Milliseconds 700; 1 }
}
$clock = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $slow | Show-ConsoleTable -Width 120
} catch {
    '@@ERR ' + $_.Exception.Message
}
'@@MS ' + $clock.ElapsedMilliseconds
'@@END'
