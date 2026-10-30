Describe 'ConsoleMonster changes, replay and engine fixes' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = (Resolve-Path (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1')).Path
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        function Invoke-ChangesChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'COLORTERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'records each log line once when a dashboard runs with redirected output' {
        $recording = Join-Path $TestDrive 'redirected.jsonl'
        $text = Invoke-ChangesChild @"
`$d = New-ConsoleDashboard -Title 'Redirected' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
Invoke-ConsoleDashboard `$d -RecordingPath '$recording' {
    Write-ConsoleLogEntry -Message 'first line' -Level info
    Start-Sleep -Milliseconds 400
    Write-ConsoleLogEntry -Message 'second line' -Level warn
    Start-Sleep -Milliseconds 400
} | Out-Null
"@
        $events = @(Get-Content -LiteralPath $recording | Select-Object -Skip 1 | ConvertFrom-Json)
        @($events | Where-Object { $_.target -eq '$log' }).Count | Should -Be 2
        @($events | Where-Object { $_.state -eq 'out' }).Count | Should -Be 0
        # The narration still reaches the redirected output.
        $text | Should -Match 'second line'
    }

    It 'returns structured changes with Compare-ConsoleObject -PassThru' {
        $text = Invoke-ChangesChild @'
$before = @([pscustomobject]@{ Name = 'web01'; Role = 'IIS'; Ram = 8 }, [pscustomobject]@{ Name = 'db01'; Role = 'SQL'; Ram = 32 })
$after = @([pscustomobject]@{ Name = 'web01'; Role = 'IIS'; Ram = 16 }, [pscustomobject]@{ Name = 'app01'; Role = 'App'; Ram = 8 })
$changes = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -PassThru
foreach ($c in $changes) { "CHANGE=$($c.Kind)|$($c.Key)|$(@($c.Properties | ForEach-Object Path) -join ',')" }
'@
        $text | Should -Match 'CHANGE=Removed\|db01\|'
        $text | Should -Match 'CHANGE=Changed\|web01\|Ram'
        $text | Should -Match 'CHANGE=Added\|app01\|'
    }

    It 'shows the change view with tones and counts' {
        $text = Invoke-ChangesChild @'
$before = @([pscustomobject]@{ Name = 'web01'; Ram = 8 })
$after = @([pscustomobject]@{ Name = 'web01'; Ram = 16 }, [pscustomobject]@{ Name = 'app01'; Ram = 8 })
Compare-ConsoleObject $before $after -Property Name -Title 'Servers'
$after | Compare-ConsoleObject -ReferenceObject $before -Property Name -View Summary
'@
        $text | Should -Match 'Servers'
        $text | Should -Match '\+1 added'
        $text | Should -Match '~1 changed'
        $text | Should -Match 'Ram'
    }

    It 'honours WhatIf Force and Confirm in Compare-ConsoleObject with Question' {
        $text = Invoke-ChangesChild @'
$before = @([pscustomobject]@{ Name = 'anna' }, [pscustomobject]@{ Name = 'bob' })
$after = @([pscustomobject]@{ Name = 'anna' })
"WHATIF=$(Compare-ConsoleObject -Question 'Apply these changes?' -ReferenceObject $before -DifferenceObject $after -Property Name -WhatIf)"
"FORCE=$(Compare-ConsoleObject -Question 'Apply these changes?' -ReferenceObject $before -DifferenceObject $after -Property Name -Force)"
"CONFIRMFALSE=$(Compare-ConsoleObject -Question 'Apply these changes?' -ReferenceObject $before -DifferenceObject $after -Property Name -Confirm:$false)"
"NONE=$(Compare-ConsoleObject -Question 'Apply these changes?' -ReferenceObject $after -DifferenceObject $after -Property Name -Force)"
try { Compare-ConsoleObject -Question 'Apply these changes?' -ReferenceObject $before -DifferenceObject $after -Property Name | Out-Null; 'ASKED' } catch { 'NONINTERACTIVE' }
'@
        $text | Should -Match 'WHATIF=False'
        $text | Should -Match 'FORCE=True'
        $text | Should -Match 'CONFIRMFALSE=True'
        $text | Should -Match 'NONE=False'
        $text | Should -Match '\+0 added\s+-0 removed\s+~0 changed'
        $text | Should -Match 'NONINTERACTIVE'
        foreach ($answer in @(@{ Key = 'y'; Expected = 'True' }, @{ Key = 'n'; Expected = 'False' })) {
            $interactive = Invoke-ChangesChild (@'
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::new()
$options.Mode = 'Rich'
$options.Capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(100, 20)
$inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('__KEY__')
$options.Input = $inputSource
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $options
$before = @([pscustomobject]@{ Name = 'one'; Count = 1 })
$after = @([pscustomobject]@{ Name = 'one'; Count = 2 })
$decisions = @(Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -Title PREVIEW_ONCE -Question 'Accept the change?' -ErrorAction Stop)
"DECISIONS=$($decisions.Count) VALUE=$($decisions[0]) REMAINING=$($inputSource.Remaining)"
"QUESTION=$($options.Capture.LastFrame.ToPlainText())"
'@.Replace('__KEY__', $answer.Key))
            ([regex]::Matches($interactive, 'PREVIEW_ONCE')).Count | Should -Be 1
            $interactive | Should -Match ("DECISIONS=1 VALUE=" + $answer.Expected + ' REMAINING=0')
            $interactive | Should -Match 'Accept the change\?'
        }
    }

    It 'prints the markers of a recording with Show-ConsoleReplay outside an interactive console' {
        $recording = Join-Path $TestDrive 'replay.jsonl'
        $text = Invoke-ChangesChild @"
`$d = New-ConsoleDashboard -Title 'Recorded' -Content { New-ConsoleLanes -WidgetId lanes; New-ConsoleLogRegion }
Invoke-ConsoleDashboard `$d -RecordingPath '$recording' {
    Update-Console -WidgetId lanes -Key one -Status Running
    `$ConsoleSession.Post(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveEvent'))::new('`$marker', 'mark', `$null, 'Wave 2 started'))
    Write-ConsoleLogEntry -Message 'disk almost full' -Level warn
    Update-Console -WidgetId lanes -Key two -Result Fail
    Update-Console -WidgetId lanes -Key one -Complete
    Update-Console -WidgetId lanes -Key two -Complete
}
Show-ConsoleReplay `$d '$recording'
'REPLAY-DONE'
"AUTORECORD=`$((Get-Command Invoke-ConsoleDashboard).Parameters.ContainsKey('AutoRecord'))"
"@
        $text | Should -Match 'REPLAY-DONE'
        $text | Should -Not -Match 'no widget named'
        $text | Should -Match 'marker\s+Wave 2 started'
        $text | Should -Match 'warning\s+disk almost full'
        $text | Should -Match 'failure\s+lanes two'
        $text | Should -Match 'AUTORECORD=True'
    }

    It 'keeps labels when $null or empty strings are passed' {
        $text = Invoke-ChangesChild @'
$phases = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.Phases'))::new()
$null = $phases.Set('discover', 'Running', 'Discover servers', '3 found')
$null = $phases.Set('discover', 'Pass', $null, '2.1s')
$status = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.StatusList'))::new()
$null = $status.Set('dc01', 'DC01 replication', 'Running', 'checking')
$null = $status.Set('dc01', '', 'Pass', $null)
Show-Console $phases
Show-Console $status
'@
        $text | Should -Match 'Discover servers'
        $text | Should -Match '2\.1s'
        $text | Should -Match 'DC01 replication'
        $text | Should -Match 'checking'
    }

    It 'shows identifier columns without thousands separators' {
        $text = Invoke-ChangesChild @'
[System.Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
[pscustomobject]@{ Name = 'svc'; ProcessId = 12345; LocalPort = 50123; WorkingSet = 1234567 } | Show-ConsoleTable
[pscustomobject]@{ Name = 'x'; Code = 40000 } | Show-ConsoleTable -Property Name, @{ Name = 'Code'; Expression = { $_.Code }; Identifier = $true }
'@
        $text | Should -Match '12345'
        $text | Should -Match '50123'
        $text | Should -Not -Match '12,345'
        $text | Should -Match '40000'
    }
}
