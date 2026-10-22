<#
.SYNOPSIS
Checks Wide fitting through the direct-host FormatCommands proxy in an owned native console.
.PARAMETER ResultPath
The JSON receipt path. The caller must launch this script in an isolated console.
.EXAMPLE
pwsh -NoProfile -File ./Assert-ConcurrencyHost.ps1 -ResultPath ./wide-host.json
#>
[CmdletBinding()]
param([Parameter(Mandatory)][string] $ResultPath)
$ErrorActionPreference = 'Stop'
try {
    if ([Console]::IsOutputRedirected) { throw 'A native console is required.' }
    Import-Module (Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')
    . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
    Enable-ConsoleFormatting -Feature FormatCommands
    $count = 0
    foreach ($width in 40, 41, 80) {
        $profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width, 40, 'None')
        [ConsoleMonster.Terminal]::Profile = $profile
        $values = 1..4 | ForEach-Object { [pscustomobject]@{ Name = "$_-" + ('x' * 18) } }
        foreach ($layout in @(@{Column = 2}, @{Column = 3}, @{AutoSize = $true})) {
            $native = ($values | Microsoft.PowerShell.Utility\Format-Wide Name @layout | Microsoft.PowerShell.Utility\Out-String -Width $width)
            $wanted = ((($native -replace "`r", '').Trim("`n") -split "`n") | ForEach-Object { $_.TrimEnd() }) -join "`n"
            $lineCount = @($wanted -split "`n").Count
            $values | Format-Wide Name @layout
            $actual = [ConsoleMonsterTestConsoleFrame]::LastFrame($lineCount, $width).TrimEnd("`n")
            if ($actual -cne $wanted) { throw "Direct-host Wide differs at width $width :`nNative:`n$wanted`nActual:`n$actual" }
            $count++
        }
    }
    [pscustomobject]@{ Runtime = $PSVersionTable.PSVersion.ToString(); Cases = $count; DirectHostWide = 'Passed' } |
        ConvertTo-Json | Set-Content -LiteralPath $ResultPath -Encoding UTF8
} catch {
    $_ | Out-String | Set-Content -LiteralPath $ResultPath -Encoding UTF8
    exit 1
} finally {
    Disable-ConsoleFormatting
}
