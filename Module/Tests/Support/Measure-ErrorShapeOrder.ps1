[CmdletBinding()]
param([string] $ModulePath, [string] $ReceiptPath, [int] $Width)
$ErrorActionPreference = 'Stop'
try {
    Import-Module $ModulePath
    . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
    . (Join-Path $PSScriptRoot 'New-ErrorShapeFixture.ps1')
    $invocation = New-ErrorShapeFixture -Directory (Split-Path $ReceiptPath) -MixedOutput
    [ConsoleMonster.Terminal]::Profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($Width, 100)
    foreach ($enabled in $false, $true) {
        Clear-Host
        if ($enabled) { Enable-ConsoleFormatting -Feature Errors }
        else { Disable-ConsoleFormatting }
        if ($enabled) { & $invocation mixed-output 2>&1 | Out-ConsoleDefault -Shim -TopLevel }
        else { & $invocation mixed-output 2>&1 | Microsoft.PowerShell.Core\Out-Default }
        Write-Host ''
        $text = [ConsoleMonsterTestConsoleFrame]::LastFrame([Console]::CursorTop, [Console]::BufferWidth)
        $last = -1
        foreach ($token in 'before error', 'First synthetic failure.', 'between errors', 'Second synthetic failure.', 'after errors') {
            $position = $text.IndexOf($token, [StringComparison]::Ordinal)
            if ($position -le $last) { throw "Live output order changed with enabled=$enabled`: $token`n$text" }
            $last = $position
        }
        if ($enabled -and $text -notmatch 'Error: First synthetic failure\.') { throw 'Compact error routing did not activate.' }
    }
    @{Passed=$true; Width=$Width; Runtime=$PSVersionTable.PSVersion.ToString()} | ConvertTo-Json | Set-Content $ReceiptPath
} catch {
    @{Passed=$false; Error=($_ | Out-String)} | ConvertTo-Json | Set-Content $ReceiptPath
    exit 1
} finally { Disable-ConsoleFormatting }
