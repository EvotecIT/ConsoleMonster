Describe 'ConsoleMonster module' {
    BeforeAll {
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # Runs a script in a child process of the same runtime with redirected output, so ConsoleMonster detects a
        # redirected (Stream) terminal and the output can be inspected.
        function Invoke-ConsoleChild {
            param(
                [Parameter(Mandatory)][string] $Script,
                [hashtable] $Environment = @{}
            )
            $lines = [System.Collections.Generic.List[string]]::new()
            # Clear every capability-detection input so ambient settings (NO_COLOR, CI, TERM...) cannot change the result.
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'BUILD_BUILDID', 'JENKINS_URL', 'GITLAB_CI', 'TEAMCITY_VERSION', 'TERM', 'COLORTERM', 'TERM_PROGRAM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            foreach ($key in $Environment.Keys) {
                $lines.Add("`$env:$key = '$($Environment[$key])'")
            }
            $lines.Add($Script)
            $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(($lines -join "`n")))
            $output = & $script:Runtime -NoLogo -NoProfile -NonInteractive -EncodedCommand $encoded 2>&1
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'exports the Console cmdlets on this runtime' {
        Import-Module $script:ModulePath -Force
        $commands = Get-Command -Module ConsoleMonster
        $commands.Count | Should -Be 42
        $commands.Name | Should -Contain 'Write-ConsoleTable'
        $commands.Name | Should -Contain 'Start-ConsoleDashboard'
        ($commands.Name | Where-Object { $_ -notlike '*-Console*' }) | Should -BeNullOrEmpty
    }

    It 'writes a table with right-aligned numbers and status markers' {
        $text = Invoke-ConsoleChild -Script @'
@(
    [pscustomobject] @{ Name = 'alpha'; Count = 7; Result = 'pass' }
    [pscustomobject] @{ Name = 'beta'; Count = 1234; Result = 'fail' }
) | Write-ConsoleTable -Border Minimal -StatusProperty Result
'@
        $text | Should -Match 'Name\s+Count\s+Result'
        $text | Should -Match 'alpha\s+7'
        $text | Should -Match 'beta\s+1234'
        $text | Should -Match (([char] 0xD7) + ' fail')
    }

    It 'renders plain ASCII without escape sequences in plain mode' {
        $text = Invoke-ConsoleChild -Environment @{ CONSOLEMONSTER_MODE = 'plain'; CONSOLEMONSTER_COLOR = 'truecolor' } -Script @'
New-ConsolePanel -Title 'Plain' { New-ConsoleStatusList -Data ([ordered] @{ SPF = 'pass'; DNSSEC = 'fail' }) } | Write-ConsoleWidget
'@
        $text | Should -Match '\[PASS\] SPF'
        $text | Should -Match '\[FAIL\] DNSSEC'
        $text | Should -Not -Match ([char] 27)
        ($text.ToCharArray() | Where-Object { [int] $_ -gt 126 }) | Should -BeNullOrEmpty
    }

    It 'applies capability overrides from environment variables' {
        $text = Invoke-ConsoleChild -Environment @{ CONSOLEMONSTER_COLOR = '16'; CONSOLEMONSTER_GLYPHS = 'ascii' } -Script @'
$p = Get-ConsoleCapability
"$($p.ColorDepth)|$($p.Glyphs)|$($p.Mode)"
'@
        $text | Should -Match 'Ansi16\|Ascii\|Stream'
    }

    It 'runs a dashboard in redirected output, capturing Write-Host and returning script output' {
        $text = Invoke-ConsoleChild -Script @'
$d = New-ConsoleDashboard -Title 'Test' { New-ConsoleLanes -Name lanes; New-ConsoleLog }
$result = Start-ConsoleDashboard $d {
    Update-ConsoleLane -Key one -Status Running
    Write-Host 'hello from the runspace'
    Update-ConsoleLane -Key one -Result Pass
    Update-ConsoleLane -Key one -Complete
    'script output'
}
"RESULT=$result"
'@
        $text | Should -Match 'hello from the runspace'
        $text | Should -Match 'one PASS: 1 passed'
        $text | Should -Match 'RESULT=script output'
    }

    It 'processes every input item with -Parallel' {
        $text = Invoke-ConsoleChild -Script @'
$d = New-ConsoleDashboard -Title 'Parallel' { New-ConsoleLanes }
$results = 1..60 | Start-ConsoleDashboard $d -Parallel -ThrottleLimit 3 {
    param($Item)
    Update-ConsoleLane -Key "item$Item" -Result Pass
    $Item * 10
}
"SUM=$(($results | Measure-Object -Sum).Sum)"
'@
        $text | Should -Match 'SUM=18300'
    }

    It 'records a dashboard run with -RecordPath and replays the recording' {
        $recording = Join-Path $TestDrive 'run.jsonl'
        $text = Invoke-ConsoleChild -Script @"
`$d = New-ConsoleDashboard -Title 'Recorded' { New-ConsoleLanes -Name lanes; New-ConsoleLog }
Start-ConsoleDashboard `$d -RecordPath '$recording' {
    Update-ConsoleLane -Key one -Status Running
    Write-ConsoleLog -Message 'checking one' -Level Info
    Update-ConsoleLane -Key one -Result Pass
    Update-ConsoleLane -Key two -Result Fail
    Update-ConsoleLane -Key one -Complete
    Update-ConsoleLane -Key two -Complete
}
`$r = New-ConsoleDashboard -Title 'Replayed' { New-ConsoleLanes -Name lanes; New-ConsoleLog }
Start-ConsoleReplay `$r -Path '$recording' -Speed 1000
'REPLAY-DONE'
"@
        Test-Path -LiteralPath $recording | Should -BeTrue
        $lines = @(Get-Content -LiteralPath $recording -Encoding UTF8 | Where-Object { $_ })
        $lines[0] | Should -Be '{"consolemonster":"recording","version":1}'
        $events = @($lines | Select-Object -Skip 1 | ForEach-Object { $_ | ConvertFrom-Json })
        $events.Count | Should -BeGreaterThan 4
        @($events | Where-Object { $_.target -eq 'lanes' -and $_.key -eq 'one' }).Count | Should -BeGreaterThan 0
        @($events | Where-Object { $_.target -eq 'lanes' -and $_.key -eq 'two' -and $_.state -eq 'fail' }).Count | Should -Be 1
        # Timestamps are milliseconds since Start and never go backwards.
        $previous = 0
        foreach ($event in $events) {
            [double] $event.t | Should -BeGreaterOrEqual $previous
            $previous = [double] $event.t
        }
        # The replay narrates the same run in stream mode.
        $text | Should -Match 'two FAIL'
        $text | Should -Match 'REPLAY-DONE'
    }

    It 'reports an error when updating without a running dashboard' {
        Import-Module $script:ModulePath -Force
        { Update-ConsoleLane -Key x -Result Pass -ErrorAction Stop } | Should -Throw '*No console dashboard is running*'
    }
}
