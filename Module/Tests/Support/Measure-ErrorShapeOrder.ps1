[CmdletBinding()]
param([string] $ModulePath, [string] $ReceiptPath, [int] $Width)
$ErrorActionPreference = 'Stop'
try {
    # This owned physical console exercises the interactive route, independently of its CI parent.
    foreach ($name in 'CI', 'TF_BUILD', 'GITHUB_ACTIONS', 'GITLAB_CI', 'CODEX_CI', 'CONSOLEMONSTER_DEFAULT_VIEWS') {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }
    Import-Module $ModulePath
    . (Join-Path $PSScriptRoot 'Read-TestConsoleFrame.ps1')
    . (Join-Path $PSScriptRoot 'New-ErrorShapeFixture.ps1')
    $invocation = New-ErrorShapeFixture -Directory (Split-Path $ReceiptPath) -MixedOutput
    [ConsoleMonster.Terminal]::Profile = [ConsoleMonster.Terminal]::GetCurrentProfile().WithSize($Width, 100)
    foreach ($enabled in $false, $true) {
        Clear-Host
        if ($enabled) { Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact }
        else { Disable-ConsoleFormatting }
        if ($enabled) { & $invocation mixed-output 2>&1 | & ([Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))) -Shim -TopLevel }
        else { & $invocation mixed-output 2>&1 | Microsoft.PowerShell.Core\Out-Default }
        Write-Host ''
        $text = [ConsoleMonsterTestConsoleFrame]::LastFrame([Console]::CursorTop, [Console]::BufferWidth)
        $last = -1
        foreach ($token in 'before error', 'First synthetic failure.', 'between errors', 'Second synthetic failure.', 'after errors') {
            $position = $text.IndexOf($token, [StringComparison]::Ordinal)
            if ($position -le $last) { throw "Live output order changed with enabled=$enabled`: $token`n$text" }
            $last = $position
        }
        if ($enabled -and $text -notmatch 'Test-SyntheticErrorShape: First synthetic failure\.') { throw 'Compact error routing did not activate.' }
    }
    @{Passed=$true; Width=$Width; Runtime=$PSVersionTable.PSVersion.ToString()} | ConvertTo-Json | Set-Content $ReceiptPath
} catch {
    @{Passed=$false; Error=($_ | Out-String)} | ConvertTo-Json | Set-Content $ReceiptPath
    exit 1
} finally { Disable-ConsoleFormatting }
