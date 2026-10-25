Describe 'ConsoleMonster module' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
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
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'exports the Console cmdlets on this runtime' {
        Import-Module $script:ModulePath -Force
        $commands = Get-Command -Module ConsoleMonster
        $commands.Count | Should -Be 141
        $manifest = Import-PowerShellDataFile -LiteralPath $script:ModulePath
        $cmdlets = @($commands | Where-Object CommandType -EQ Cmdlet)
        $cmdlets.Count | Should -Be 141
        ($cmdlets.Name | Sort-Object) -join ',' | Should -BeExactly (($manifest.CmdletsToExport | Sort-Object) -join ',')
        ($commands | Where-Object CommandType -EQ Alias) | Should -BeNullOrEmpty
        foreach ($name in 'Get-ConsoleDemo', 'Show-ConsoleDemo', 'New-ConsoleFlow', 'New-ConsoleFlowNode', 'New-ConsoleFlowEdge', 'New-ConsoleFlowBranch', 'Update-ConsoleFlow') { $cmdlets.Name | Should -Contain $name }
        $commands.Name | Should -Contain 'Show-ConsoleMarkdown'
        foreach ($name in 'Start-ConsoleRun', 'Update-ConsoleRun', 'Wait-ConsoleRun', 'Complete-ConsoleRun') { $commands.Name | Should -Contain $name }
        $commands.Name | Should -Contain 'Show-ConsoleObject'
        $commands.Name | Should -Contain 'Show-ConsoleOverview'
        $commands.Name | Should -Contain 'Compare-ConsoleText'
        $commands.Name | Should -Contain 'Export-ConsoleTheme'
        foreach ($name in 'New-ConsoleBinding', 'New-ConsoleButton', 'New-ConsoleMultiSelect', 'New-ConsoleSelect',
            'New-ConsoleTextArea', 'New-ConsoleTextInput', 'New-ConsoleToggle', 'Show-ConsoleApp', 'Watch-ConsoleLog',
            'Stop-ConsoleRecording', 'Invoke-ConsoleRecording', 'Add-ConsoleRecordingMarker', 'Invoke-ConsolePresentation', 'Show-ConsolePresentation') {
            $commands.Name | Should -Contain $name
        }
        $commands.Name | Should -Contain 'Show-ConsoleChart'
        $commands.Name | Should -Contain 'Copy-ConsoleOutput'
        $commands.Name | Should -Contain 'Out-ConsoleFormatCommand'
        $commands.Name | Should -Contain 'New-ConsoleTreeNode'
        $commands.Name | Should -Contain 'Add-ConsoleItem'
        $commands.Name | Should -Contain 'Invoke-ConsolePester'
        $commands.Name | Should -Contain 'Show-ConsoleTable'
        $commands.Name | Should -Contain 'Invoke-ConsoleDashboard'
        ($commands.Name | Where-Object { $_ -notlike '*-Console*' }) | Should -BeNullOrEmpty
    }

    It 'writes a table with right-aligned numbers and status markers' {
        $text = Invoke-ConsoleChild -Script @'
[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')
@(
    [pscustomobject] @{ Name = 'alpha'; Count = 7; Result = 'pass' }
    [pscustomobject] @{ Name = 'beta'; Count = 1234; Result = 'fail' }
) | Show-ConsoleTable -Border Minimal -StatusProperty Result
'@
        $text | Should -Match 'Name\s+Count\s+Result'
        $text | Should -Match 'alpha\s+7'
        # Show-ConsoleTable formats numbers in the culture, as Show-ConsoleTable does.
        $text | Should -Match 'beta\s+1,234'
        $text | Should -Match (([char] 0xD7) + ' fail')
    }

    It 'renders plain ASCII without escape sequences in plain mode' {
        $text = Invoke-ConsoleChild -Environment @{ CONSOLEMONSTER_MODE = 'plain'; CONSOLEMONSTER_COLOR = 'truecolor' } -Script @'
New-ConsolePanel -Title 'Plain' -Content { New-ConsoleStatusList -InputObject ([ordered] @{ SPF = 'pass'; DNSSEC = 'fail' }) } | Show-Console
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
$d = New-ConsoleDashboard -Title 'Test' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
$result = Invoke-ConsoleDashboard $d {
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

    It 'writes through the host in a background job, so the job completes and Receive-Job gets the text' {
        $job = Start-Job -ArgumentList $script:ModulePath -ScriptBlock {
            param($ModulePath)
            Import-Module $ModulePath -Force
            Write-ConsoleText -Markup '[pass]from the job[/]'
            [pscustomobject] @{ Name = 'alpha'; Count = 7 } | Show-ConsoleTable -Border Minimal
            $d = New-ConsoleDashboard -Title 'Job dashboard' -Content { New-ConsoleLanes -WidgetId lanes }
            Invoke-ConsoleDashboard $d { Update-ConsoleLane -Key srv1 -Result Pass; Update-ConsoleLane -Key srv1 -Complete }
            'pipeline output'
        }
        $null = Wait-Job $job -Timeout 180
        $state = $job.State
        # The job's streams are read directly: Receive-Job would also replay the job's host calls onto this console.
        $child = $job.ChildJobs[0]
        $received = @(@($child.Output) + @($child.Information) + @($child.Error) | ForEach-Object { "$_" })
        Remove-Job $job -Force
        $state | Should -Be 'Completed'
        $received | Should -Contain 'from the job'
        $received | Should -Contain 'pipeline output'
        ($received -join "`n") | Should -Match 'alpha\s+7'
        ($received -join "`n") | Should -Match 'srv1 PASS'
    }

    It 'is recorded by Start-Transcript and captured by 6>&1 like Write-Host, but not by assignment' {
        $transcript = Join-Path $TestDrive 'transcript.txt'
        $text = Invoke-ConsoleChild -Script @"
Start-Transcript -Path '$transcript' | Out-Null
Write-ConsoleText -Markup 'in the transcript'
Stop-Transcript | Out-Null
`$assigned = Write-ConsoleText -Markup 'assigned line'
"ASSIGNED=[`$assigned]"
`$captured = @(Write-ConsoleText -Markup 'captured line' 6>&1 | ForEach-Object { "`$_" })
"CAPTURED=[`$(`$captured -join '|')]"
"@
        Get-Content -LiteralPath $transcript -Raw | Should -Match 'in the transcript'
        $text | Should -Match 'ASSIGNED=\[\]'
        $text | Should -Match 'CAPTURED=\[captured line\]'
    }

    It 'colours host output in jobs with Write-Host style records: PASS green, FAIL red, WARN yellow' {
        $job = Start-Job -ArgumentList $script:ModulePath -ScriptBlock {
            param($ModulePath)
            foreach ($name in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_MODE', 'TERM') { [Environment]::SetEnvironmentVariable($name, $null) }
            Import-Module $ModulePath -Force
            $checks = @(
                [pscustomobject] @{ Check = 'SPF'; Status = 'pass' }
                [pscustomobject] @{ Check = 'MTA-STS'; Status = 'fail' }
                [pscustomobject] @{ Check = 'DKIM'; Status = 'warn' }
            )
            foreach ($case in @(@('COLOUR', $null, $null), @('NO_COLOR', 'NO_COLOR', '1'), @('NONE', 'CONSOLEMONSTER_COLOR', 'none'))) {
                if ($case[1]) { [Environment]::SetEnvironmentVariable($case[1], $case[2]) }
                $records = @($checks | Show-ConsoleTable -Property Check, @{ Name = 'Status'; Presenter = 'Status' } 6>&1)
                $records | Where-Object { $_.MessageData -is [System.Management.Automation.HostInformationMessage] } | ForEach-Object { $case[0] + '=' + $_.MessageData.Message.Trim() + '|' + $_.MessageData.ForegroundColor + '|' + $_.MessageData.NoNewLine }
                if ($case[1]) { [Environment]::SetEnvironmentVariable($case[1], $null) }
            }
        }
        $null = Wait-Job $job -Timeout 180
        $text = (@(Receive-Job $job) -join "`n")
        Remove-Job $job -Force
        $text | Should -Match 'COLOUR=\S+ PASS\|Green\|True'
        $text | Should -Match 'COLOUR=\S+ FAIL\|Red\|True'
        $text | Should -Match 'COLOUR=\S+ WARN\|Yellow\|True'
        # Without colour every line is one plain record.
        $text | Should -Match 'NO_COLOR=.*PASS.*\|\|False'
        $text | Should -Match 'NONE=.*PASS.*\|\|False'
        $text | Should -Not -Match '(NO_COLOR|NONE)=[^\n]*\|(Green|Red|Yellow|DarkGray)\|'
    }
    It 'keeps transcripts plain: whole lines, no colour codes' {
        $transcript = Join-Path $TestDrive 'coloured-transcript.txt'
        $null = Invoke-ConsoleChild -Script @"
Start-Transcript -Path '$transcript' | Out-Null
[pscustomobject] @{ Check = 'SPF'; Status = 'pass' } , [pscustomobject] @{ Check = 'DKIM'; Status = 'warn' } | Show-ConsoleTable -Property Check, @{ Name = 'Status'; Presenter = 'Status' }
Stop-Transcript | Out-Null
"@
        $content = Get-Content -LiteralPath $transcript -Raw
        $content.IndexOf([char] 27) | Should -Be -1
        @(Get-Content -LiteralPath $transcript | Where-Object { $_ -match 'SPF' -and $_ -match 'PASS' }).Count | Should -Be 1
        @(Get-Content -LiteralPath $transcript | Where-Object { $_ -match 'DKIM' -and $_ -match 'WARN' }).Count | Should -Be 1
    }

    It 'keeps the colours through Receive-Job' {
        $job = Start-Job -ArgumentList $script:ModulePath -ScriptBlock {
            param($ModulePath)
            foreach ($name in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_MODE', 'TERM') { [Environment]::SetEnvironmentVariable($name, $null) }
            Import-Module $ModulePath -Force
            [pscustomobject] @{ Check = 'SPF'; Status = 'pass' } , [pscustomobject] @{ Check = 'MTA-STS'; Status = 'fail' } | Show-ConsoleTable -Property Check, @{ Name = 'Status'; Presenter = 'Status' }
        }
        $null = Wait-Job $job -Timeout 180
        # The job's information records are what Receive-Job 6>&1 returns, read without replaying host calls onto this console.
        $records = @($job.ChildJobs[0].Information)
        Remove-Job $job -Force
        $segments = @($records | Where-Object { $_.MessageData.Message } | ForEach-Object { '{0}|{1}' -f $_.MessageData.Message.Trim(), $_.MessageData.ForegroundColor })
        ($segments -join "`n") | Should -Match '\S+ PASS\|Green'
        ($segments -join "`n") | Should -Match '\S+ FAIL\|Red'
    }

    It 'reports updates to an unknown widget name once, through the host, after the dashboard ends' {
        $text = Invoke-ConsoleChild -Script @'
$d = New-ConsoleDashboard -Title 'Typos' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
Invoke-ConsoleDashboard $d -WarningVariable dashboardWarnings -WarningAction SilentlyContinue {
    1..5 | ForEach-Object { Update-ConsoleLane -WidgetId lanez -Key one -Result Pass }
}
"WARNINGS=$(@($dashboardWarnings).Count)"
$dashboardWarnings | ForEach-Object { "W: $_" }
'@
        $text | Should -Match 'WARNINGS=1'
        $text | Should -Match "W: The console dashboard has no widget named 'lanez'; 5 update\(s\) for it were ignored\. Widget names: .*lanes"
    }

    It 'processes every input item with -Parallel' {
        $text = Invoke-ConsoleChild -Script @'
$d = New-ConsoleDashboard -Title 'Parallel' -Content { New-ConsoleLanes }
$results = 1..60 | Invoke-ConsoleDashboard $d -Parallel -ThrottleLimit 3 {
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
`$d = New-ConsoleDashboard -Title 'Recorded' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
Invoke-ConsoleDashboard `$d -RecordPath '$recording' {
    Update-ConsoleLane -Key one -Status Running
    Write-ConsoleLogEntry -Message 'checking one' -Level Info
    Update-ConsoleLane -Key one -Result Pass
    Update-ConsoleLane -Key two -Result Fail
    Update-ConsoleLane -Key one -Complete
    Update-ConsoleLane -Key two -Complete
}
`$r = New-ConsoleDashboard -Title 'Replayed' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
Show-ConsoleReplay -PlaybackMode Automatic `$r -Path '$recording' -Speed 1000
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
