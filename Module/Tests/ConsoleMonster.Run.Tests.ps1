Describe 'Invoke-ConsoleRun (ForEach-ConsoleItem)' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # Runs a script in a child process (redirected output: the run prints a line per item) and returns its text.
        function Invoke-RunChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            (Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n") -TimeoutSeconds 180) -join "`n"
        }
    }

    It 'returns the output in input order in parallel, passes $using: values and writes failures as errors' {
        $text = Invoke-RunChild @'
$base = 10
$out = 1..12 | ForEach-ConsoleItem -Parallel -ThrottleLimit 4 { Start-Sleep -Milliseconds ((13 - $_) * 15); if ($_ -eq 5) { throw 'five failed' }; $_ * $using:base } -ErrorVariable failures -ErrorAction SilentlyContinue
"OUT=$($out -join ',')"
"ERRORS=$($failures.Count) $($failures[0].Exception.Message)"
'@
        $text | Should -Match 'OUT=10,20,30,40,60,70,80,90,100,110,120'
        $text | Should -Match 'ERRORS=1 five failed'
        $text | Should -Match '12 items: 11 passed, 1 failed'
    }

    It 'writes each output as it finishes with -Unordered' {
        $text = Invoke-RunChild @'
$out = 1..4 | ForEach-ConsoleItem -Parallel -ThrottleLimit 4 -Unordered { Start-Sleep -Milliseconds ((5 - $_) * 120); $_ }
"OUT=$($out -join ',')"
'@
        $text | Should -Match 'OUT=4,3,2,1'
    }

    It 'runs one at a time in the caller''s scope without -Parallel, with labels from -Name' {
        $text = Invoke-RunChild @'
$script:seen = [System.Collections.Generic.List[string]]::new()
$items = @([pscustomobject] @{ Host = 'alpha' }, [pscustomobject] @{ Host = 'beta' })
$out = $items | ForEach-ConsoleItem -Name Host { $script:seen.Add($_.Host); $_.Host.ToUpper() }
"OUT=$($out -join ',') SEEN=$($script:seen -join ',')"
'@
        $text | Should -Match 'OUT=ALPHA,BETA SEEN=alpha,beta'
        $text | Should -Match 'alpha \('
        $text | Should -Match 'beta \('
    }

    It 'keeps variables set in the body without -Parallel, as ForEach-Object does, and leaves no helper variables' {
        $text = Invoke-RunChild @'
function Get-Total { $total = 0; 1..4 | ForEach-ConsoleItem { $total += $_ } | Out-Null; $total }
"TOTAL=$(Get-Total)"
$progressBefore = $ProgressPreference
1 | ForEach-ConsoleItem { $_ } | Out-Null
"LEAKED=$(@(Get-Variable -Name '__cm*' -ErrorAction Ignore).Count) PROGRESS=$($ProgressPreference -eq $progressBefore)"
'@
        $text | Should -Match 'TOTAL=10'
        $text | Should -Match 'LEAKED=0 PROGRESS=True'
    }

    It 'prints nothing for no input, warns about -TimeoutSeconds without -Parallel and rejects $using: expressions before the run' {
        $text = Invoke-RunChild @'
$none = @() | ForEach-ConsoleItem -Title 'NOTHING' { $_ }
"NONE=$(@($none).Count)"
$out = 1 | ForEach-ConsoleItem -TimeoutSeconds 5 { $_ } -WarningVariable w -WarningAction SilentlyContinue
"WARNED=$($w.Count) OUT=$out"
$items = @{ a = 1 }
try { 1, 2 | ForEach-ConsoleItem -Parallel { $using:items['a'] } } catch { "REJECTED=$($_.Exception.Message -like '*$using:name*')" }
'@
        $text | Should -Match 'NONE=0'
        $text | Should -Not -Match 'NOTHING'
        $text | Should -Match 'WARNED=1 OUT=1'
        $text | Should -Match 'REJECTED=True'
        $text | Should -Not -Match '2 items: 0 passed'
    }

    It 'shows a native program''s stderr lines as text without failing the item, in both modes' {
        $text = Invoke-RunChild @'
$exe = [System.Diagnostics.Process]::GetCurrentProcess().Path
$out = 1 | ForEach-ConsoleItem { & $exe -NoProfile -NonInteractive -Command "[Console]::Error.WriteLine('progress note'); 'native out'" } -ErrorVariable e1 -ErrorAction SilentlyContinue
$par = 1 | ForEach-ConsoleItem -Parallel { & $using:exe -NoProfile -NonInteractive -Command "[Console]::Error.WriteLine('progress note'); 'native out'" } -ErrorVariable e2 -ErrorAction SilentlyContinue
"OUT=$out PAR=$par ERRORS=$($e1.Count + $e2.Count)"
'@
        $text | Should -Match 'OUT=native out PAR=native out ERRORS=0'
        $text | Should -Match '1 item: 1 passed, 0 failed'
        $text | Should -Match 'progress note'
    }

    It 'stops an item past -TimeoutSeconds and counts it as failed' {
        $text = Invoke-RunChild @'
$out = 1..2 | ForEach-ConsoleItem -Parallel -TimeoutSeconds 1 { if ($_ -eq 2) { Start-Sleep -Seconds 30 }; $_ } -ErrorVariable failures -ErrorAction SilentlyContinue
"OUT=$($out -join ',') TIMEOUT=$($failures[0].FullyQualifiedErrorId -like 'ItemTimedOut*')"
'@
        $text | Should -Match 'OUT=1 TIMEOUT=True'
        $text | Should -Match 'timed out after'
    }
}
