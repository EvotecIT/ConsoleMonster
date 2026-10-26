Describe 'Invoke-ConsoleRun (Invoke-ConsoleRun)' {
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

    It 'retries failed items in both modes, keeping only the last attempt' {
        $text = Invoke-RunChild @'
$dir = Join-Path ([IO.Path]::GetTempPath()) ([guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $dir | Out-Null
$out = 1..4 | Invoke-ConsoleRun -Parallel -ThrottleLimit 2 -RetryCount 1 -RetryDelaySeconds 0.1 {
    $marker = Join-Path $using:dir ([string] $_)
    $attempt = 1 + @(Get-ChildItem -LiteralPath $using:dir -Filter "$_.*").Count
    New-Item -ItemType File -Path "$marker.$attempt" | Out-Null
    if ($_ -eq 2 -and $attempt -lt 2) { throw 'two flaked' }
    if ($_ -eq 3) { throw "three broke on $attempt" }
    "$_@$attempt"
} -ErrorVariable failures -ErrorAction SilentlyContinue
"PAR=$($out -join ',')"
"PARERR=$($failures.Count) $($failures[0].Exception.Message)"
$script:calls = 0
$seq = 'a' | Invoke-ConsoleRun -RetryCount 2 { $script:calls++; if ($script:calls -lt 3) { Write-Error "try $script:calls" }; "seq@$script:calls" } -ErrorVariable seqErrors -ErrorAction SilentlyContinue
"SEQ=$seq ERR=$($seqErrors.Count)"
'@
        $text | Should -Match 'PAR=1@1,2@2,4@1'
        $text | Should -Match 'PARERR=1 three broke on 2'
        $text | Should -Match '2 \(retry 1 of 1\): two flaked'
        $text | Should -Match 'SEQ=seq@3 ERR=0'
        $text | Should -Match 'a \(retry 2 of 2\): try 2'
    }

    It 'returns the output in input order in parallel, passes $using: values and writes failures as errors' {
        $text = Invoke-RunChild @'
$base = 10
$out = 1..12 | Invoke-ConsoleRun -Parallel -ThrottleLimit 4 { Start-Sleep -Milliseconds ((13 - $_) * 15); if ($_ -eq 5) { throw 'five failed' }; $_ * $using:base } -ErrorVariable failures -ErrorAction SilentlyContinue
"OUT=$($out -join ',')"
"ERRORS=$($failures.Count) $($failures[0].Exception.Message)"
'@
        $text | Should -Match 'OUT=10,20,30,40,60,70,80,90,100,110,120'
        $text | Should -Match 'ERRORS=1 five failed'
        $text | Should -Match '12 items: 11 passed, 1 failed'
    }

    It 'writes each output as it finishes with -Unordered' {
        $text = Invoke-RunChild @'
$release = [System.Threading.ManualResetEventSlim]::new($false)
try {
    $out = @(1, 2 | Invoke-ConsoleRun -Parallel -ThrottleLimit 2 -Unordered {
        $gate = $using:release
        if ($_ -eq 1 -and -not $gate.Wait(10000)) { throw 'Item 2 was not delivered while item 1 was waiting.' }
        $_
    } -ErrorVariable failures -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_ -eq 2) { $release.Set() }
        $_
    })
    "OUT=$($out -join ',') ERRORS=$($failures.Count)"
} finally {
    $release.Set()
    $release.Dispose()
}
'@
        $text | Should -Match 'OUT=2,1 ERRORS=0'
    }

    It 'keeps output and warnings emitted before a terminating error in both modes' {
        $text = Invoke-RunChild @'
$out = 1 | Invoke-ConsoleRun { 'before'; Write-Warning 'detail'; throw 'failed' } -ErrorVariable failures -WarningVariable warnings -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
"SEQ=$out ERRORS=$($failures.Count) WARNINGS=$($warnings -join ',')"
$out = 1 | Invoke-ConsoleRun -Parallel { 'before'; Write-Warning 'detail'; throw 'failed' } -ErrorVariable failures -WarningVariable warnings -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
"PAR=$out ERRORS=$($failures.Count) WARNINGS=$($warnings -join ',')"
'@
        $text | Should -Match 'SEQ=before ERRORS=1 WARNINGS=detail'
        $text | Should -Match 'PAR=before ERRORS=1 WARNINGS=detail'
    }

    It 'runs one at a time in the caller''s scope without -Parallel, with labels from -Label' {
        $text = Invoke-RunChild @'
$script:seen = [System.Collections.Generic.List[string]]::new()
$items = @([pscustomobject] @{ Host = 'alpha' }, [pscustomobject] @{ Host = 'beta' })
$out = $items | Invoke-ConsoleRun -Label Host { $script:seen.Add($_.Host); $_.Host.ToUpper() }
"OUT=$($out -join ',') SEEN=$($script:seen -join ',')"
'@
        $text | Should -Match 'OUT=ALPHA,BETA SEEN=alpha,beta'
        $text | Should -Match 'alpha \('
        $text | Should -Match 'beta \('
    }

    It 'keeps variables set in the body without -Parallel, as ForEach-Object does, and leaves no helper variables' {
        $text = Invoke-RunChild @'
function Get-Total { $total = 0; 1..4 | Invoke-ConsoleRun { $total += $_ } | Out-Null; $total }
"TOTAL=$(Get-Total)"
$progressBefore = $ProgressPreference
1 | Invoke-ConsoleRun { $_ } | Out-Null
"LEAKED=$(@(Get-Variable -Name '__cm*' -ErrorAction Ignore).Count) PROGRESS=$($ProgressPreference -eq $progressBefore)"
'@
        $text | Should -Match 'TOTAL=10'
        $text | Should -Match 'LEAKED=0 PROGRESS=True'
    }

    It 'prints nothing for no input, warns about -TimeoutSeconds without -Parallel and rejects $using: expressions before the run' {
        $text = Invoke-RunChild @'
$none = @() | Invoke-ConsoleRun -Title 'NOTHING' { $_ }
"NONE=$(@($none).Count)"
$out = 1 | Invoke-ConsoleRun -TimeoutSeconds 5 { $_ } -WarningVariable w -WarningAction SilentlyContinue
"WARNED=$($w.Count) OUT=$out"
$items = @{ a = 1 }
try { 1, 2 | Invoke-ConsoleRun -Parallel { $using:items['a'] } } catch { "REJECTED=$($_.Exception.Message -like '*$using:name*')" }
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
$out = 1 | Invoke-ConsoleRun { & $exe -NoProfile -NonInteractive -Command "[Console]::Error.WriteLine('progress note'); 'native out'" } -ErrorVariable e1 -ErrorAction SilentlyContinue
$par = 1 | Invoke-ConsoleRun -Parallel { & $using:exe -NoProfile -NonInteractive -Command "[Console]::Error.WriteLine('progress note'); 'native out'" } -ErrorVariable e2 -ErrorAction SilentlyContinue
"OUT=$out PAR=$par ERRORS=$($e1.Count + $e2.Count)"
'@
        $text | Should -Match 'OUT=native out PAR=native out ERRORS=0'
        $text | Should -Match '1 item: 1 passed, 0 failed'
        $text | Should -Match 'progress note'
    }

    It 'stops an item past -TimeoutSeconds and counts it as failed' {
        $text = Invoke-RunChild @'
$out = 1..2 | Invoke-ConsoleRun -Parallel -TimeoutSeconds 1 { if ($_ -eq 2) { 'partial'; Start-Sleep -Seconds 30 }; $_ } -ErrorVariable failures -ErrorAction SilentlyContinue
"OUT=$($out -join ',') TIMEOUT=$($failures[0].FullyQualifiedErrorId -like 'ItemTimedOut*')"
'@
        $text | Should -Match 'OUT=1,partial TIMEOUT=True'
        $text | Should -Match 'timed out after'
    }
}
