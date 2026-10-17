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
`$d = New-ConsoleDashboard -Title 'Redirected' { New-ConsoleLanes -Name lanes; New-ConsoleLog }
Start-ConsoleDashboard `$d -RecordPath '$recording' {
    Write-ConsoleLog -Message 'first line' -Level info
    Start-Sleep -Milliseconds 400
    Write-ConsoleLog -Message 'second line' -Level warn
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

    It 'honours -WhatIf, -Force and -Confirm:$false in Show-ConsoleChanges' {
        $text = Invoke-ChangesChild @'
$before = @([pscustomobject]@{ Name = 'anna' }, [pscustomobject]@{ Name = 'bob' })
$after = @([pscustomobject]@{ Name = 'anna' })
"WHATIF=$(Show-ConsoleChanges -Before $before -After $after -Property Name -WhatIf)"
"FORCE=$(Show-ConsoleChanges -Before $before -After $after -Property Name -Force)"
"CONFIRMFALSE=$(Show-ConsoleChanges -Before $before -After $after -Property Name -Confirm:$false)"
"NONE=$(Show-ConsoleChanges -Before $after -After $after -Property Name -Force)"
try { Show-ConsoleChanges -Before $before -After $after -Property Name | Out-Null; 'ASKED' } catch { 'NONINTERACTIVE' }
'@
        $text | Should -Match 'WHATIF=False'
        $text | Should -Match 'FORCE=True'
        $text | Should -Match 'CONFIRMFALSE=True'
        $text | Should -Match 'NONE=False'
        $text | Should -Match 'No changes'
        $text | Should -Match 'NONINTERACTIVE'
    }

    It 'prints the markers of a recording with Show-ConsoleReplay outside an interactive console' {
        $recording = Join-Path $TestDrive 'replay.jsonl'
        $text = Invoke-ChangesChild @"
`$d = New-ConsoleDashboard -Title 'Recorded' { New-ConsoleLanes -Name lanes; New-ConsoleLog }
Start-ConsoleDashboard `$d -RecordPath '$recording' {
    Update-ConsoleLane -Key one -Status Running
    Send-ConsoleEvent -Target '`$marker' -Operation mark -Text 'Wave 2 started'
    Write-ConsoleLog -Message 'disk almost full' -Level warn
    Update-ConsoleLane -Key two -Result Fail
    Update-ConsoleLane -Key one -Complete
    Update-ConsoleLane -Key two -Complete
}
Show-ConsoleReplay `$d '$recording'
'REPLAY-DONE'
"AUTORECORD=`$((Get-Command Start-ConsoleDashboard).Parameters.ContainsKey('AutoRecord'))"
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
$phases = [ConsoleMonster.Widgets.Phases]::new()
$null = $phases.Set('discover', 'Running', 'Discover servers', '3 found')
$null = $phases.Set('discover', 'Pass', $null, '2.1s')
$status = [ConsoleMonster.Widgets.StatusList]::new()
$null = $status.Set('dc01', 'DC01 replication', 'Running', 'checking')
$null = $status.Set('dc01', '', 'Pass', $null)
Write-ConsoleWidget $phases
Write-ConsoleWidget $status
'@
        $text | Should -Match 'Discover servers'
        $text | Should -Match '2\.1s'
        $text | Should -Match 'DC01 replication'
        $text | Should -Match 'checking'
    }

    It 'shows identifier columns without thousands separators' {
        $text = Invoke-ChangesChild @'
[System.Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]'en-US'
[pscustomobject]@{ Name = 'svc'; ProcessId = 12345; LocalPort = 50123; WorkingSet = 1234567 } | Format-ConsoleTable
[pscustomobject]@{ Name = 'x'; Code = 40000 } | Format-ConsoleTable -Property Name, @{ Name = 'Code'; Expression = { $_.Code }; Identifier = $true }
'@
        $text | Should -Match '12345'
        $text | Should -Match '50123'
        $text | Should -Not -Match '12,345'
        $text | Should -Match '40000'
    }
}
