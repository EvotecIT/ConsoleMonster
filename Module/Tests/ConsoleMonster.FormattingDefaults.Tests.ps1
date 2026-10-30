Describe 'Everyday formatting defaults' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        function Invoke-FormattingScenario {
            param([string] $Script)
            $result = Invoke-TestChild -Runtime $script:Runtime -Script ("Import-Module '$script:ModulePath' -Force`n" + $Script)
            if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
            $result -join "`n"
        }
    }

    It 'enables the non-invasive set and restores every feature' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting
"ON=$((Get-ConsoleFormatting).Features -join ',')"
"LAYOUT=$((Get-ConsoleFormatting).ErrorLayout)"
Disable-ConsoleFormatting
"OFF=$((Get-ConsoleFormatting).Features.Count)"
'@
        $result | Should -Match 'ON=Output,FormatCommands,Errors,Streams,ReadLine'
        $result | Should -Match 'LAYOUT=Standard'
        $result | Should -Match 'OFF=0'
    }

    It 'includes help alongside defaults, supports exclusions and records the profile choice' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting -Include Help
$first = Get-ConsoleFormatting
"FIRST=$($first.Features -join ',')"
Enable-ConsoleFormatting -Include Help -Exclude FormatCommands
$state = Get-ConsoleFormatting
"ON=$($state.Features -join ',')"
"HOW=$($state.AddOptIns)"
Enable-ConsoleFormatting -Include Help -Exclude FormatCommands -ProfileSnippet
Enable-ConsoleFormatting -Include Help -Exclude Help
"EXCLUDED=$((Get-ConsoleFormatting).Features -contains 'Help')"
Disable-ConsoleFormatting
'@
        $result | Should -Match 'FIRST=Output,FormatCommands,Errors,Streams,ReadLine,Help'
        $result | Should -Match 'ON=Output,Errors,Streams,ReadLine,Help'
        $result | Should -Match 'HOW=Enable-ConsoleFormatting -Include Help'
        $result | Should -Match 'Enable-ConsoleFormatting -Include Help -Exclude FormatCommands'
        $result | Should -Match 'EXCLUDED=False'
    }

    It 'selects an exact subset and disables all defaults with Exclude' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting
Enable-ConsoleFormatting -Include Streams -Exclude Output, FormatCommands, Errors, ReadLine
"ON=$((Get-ConsoleFormatting).Features -join ',')"
Enable-ConsoleFormatting -Exclude Output, FormatCommands, Errors, Streams, ReadLine
"NONE=$((Get-ConsoleFormatting).Features.Count)"
Disable-ConsoleFormatting
'@
        $result | Should -Match 'ON=Streams'
        $result | Should -Match 'NONE=0'
    }

    It 'excludes selected defaults and disables a previously enabled feature' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting -Include Prompt -Exclude Output, FormatCommands, Errors, Streams, ReadLine -Force
Enable-ConsoleFormatting -Exclude FormatCommands, Prompt
"ON=$((Get-ConsoleFormatting).Features -join ',')"
Disable-ConsoleFormatting
'@
        $result | Should -Match 'ON=Output,Errors,Streams,ReadLine'
        $result | Should -Not -Match 'ON=.*(FormatCommands|Prompt)'
    }

    It 'accepts an explicit feature set and preserves it in the profile snippet' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact
"ON=$((Get-ConsoleFormatting).Features -join ',') LAYOUT=$((Get-ConsoleFormatting).ErrorLayout)"
Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact -ProfileSnippet
Enable-ConsoleFormatting -Exclude FormatCommands, Streams -ProfileSnippet
Disable-ConsoleFormatting
'@
        $result | Should -Match 'ON=Errors LAYOUT=Compact'
        $result | Should -Match 'Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact'
        $result | Should -Match 'Enable-ConsoleFormatting -Exclude FormatCommands, Streams'
    }

    It 'lets Exclude take precedence when the same feature is included' {
        $result = Invoke-FormattingScenario @'
Enable-ConsoleFormatting -Include Output -Exclude Output
"OUTPUT=$((Get-ConsoleFormatting).Features -contains 'Output')"
"ON=$((Get-ConsoleFormatting).Features.Count)"
'@
        $result | Should -Match 'OUTPUT=False'
        $result | Should -Match 'ON=4'
    }

    It 'keeps explicit format records and captured text native with the default set' {
        $result = Invoke-FormattingScenario @'
$items = @([pscustomobject]@{ Name = 'alpha'; Value = 12 }, [pscustomobject]@{ Name = 'beta'; Value = 34 })
$before = $items | Format-Table Name, Value | Out-String
$beforeTypes = @(($items | Format-Table Name, Value) | ForEach-Object { $_.GetType().FullName }) -join ','
Enable-ConsoleFormatting
$after = $items | Format-Table Name, Value | Out-String
$afterTypes = @(($items | Format-Table Name, Value) | ForEach-Object { $_.GetType().FullName }) -join ','
if ($before -cne $after -or $beforeTypes -cne $afterTypes) { throw 'Captured native formatting changed' }
Disable-ConsoleFormatting
'CAPTURES_IDENTICAL'
'@
        $result | Should -Match 'CAPTURES_IDENTICAL'
    }

    It 'preserves a custom Out-Default while applying independent colours' {
        $result = Invoke-FormattingScenario @'
function global:Out-Default { process { $_ | Microsoft.PowerShell.Core\Out-Default } }
Enable-ConsoleFormatting -WarningAction SilentlyContinue
"KEPT=$((Get-Command Out-Default -CommandType Function).Definition -notmatch 'ConsoleMonster')"
"ON=$((Get-ConsoleFormatting).Features -join ',')"
Disable-ConsoleFormatting
'@
        $result | Should -Match 'KEPT=True'
        $result | Should -Match 'ON=Streams,ReadLine'
    }

    It 'restores an installed progress proxy when a later feature fails to install' {
        $result = Invoke-FormattingScenario @'
Set-Item Function:global:Format-Table { 'read-only formatter' } -Options ReadOnly
try { Enable-ConsoleFormatting -Include Progress, FormatCommands -Exclude Output, Errors, Streams, ReadLine -Force -ErrorAction Stop; throw 'Expected installation to fail' }
catch { if ($_.Exception.Message -eq 'Expected installation to fail') { throw } }
if ((Get-Command Write-Progress).CommandType -ne 'Function') { throw 'Progress was not installed first' }
Disable-ConsoleFormatting
if ((Get-Command Write-Progress).CommandType -ne 'Cmdlet') { throw 'Progress was not restored' }
'PARTIAL_INSTALL_RESTORED'
'@
        $result | Should -Match 'PARTIAL_INSTALL_RESTORED'
    }
}
