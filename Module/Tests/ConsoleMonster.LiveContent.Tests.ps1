Describe 'Live regions, live tables, targets and symbol fallback' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (stream mode, plain text) and en-US culture.
        function Invoke-ContentChild {
            param([Parameter(Mandatory)][string] $Script, [hashtable] $Environment = @{})
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            foreach ($name in $Environment.Keys) {
                $lines.Add("[Environment]::SetEnvironmentVariable('$name', '$($Environment[$name])')")
            }
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'swaps a region and updates the live widgets inside the new content' {
        $text = Invoke-ContentChild -Script @'
$d = New-ConsoleDashboard -Title 'Swap' { New-ConsolePanel -Title 'Main' { New-ConsoleRegion -Name main { New-ConsoleSpinner -Label 'waiting' } } }
Start-ConsoleDashboard $d {
    Set-ConsoleRegion -Name main { New-ConsoleLanes -Name servers }
    Update-ConsoleLane -Name servers -Key srv1 -Status Running
    Update-ConsoleLane -Name servers -Key srv1 -Result Pass
    Update-ConsoleLane -Name servers -Key srv1 -Complete
}
'@
        $text | Should -Match 'srv1 started'
        $text | Should -Match 'srv1 PASS'
    }

    It 'adds, updates and removes live table rows by key' {
        $text = Invoke-ContentChild -Script @'
$d = New-ConsoleDashboard -Title 'Jobs' { New-ConsoleLiveTable -Name jobs -Column 'Job', 'State' -MaxRows 10 }
Start-ConsoleDashboard $d {
    Update-ConsoleTable -Name jobs -Key 1 -Value 'backup', 'running'
    Update-ConsoleTable -Name jobs -Key 1 -Value 'backup', 'done'
    Update-ConsoleTable -Name jobs -Key 2 -Value 'sync', 'running'
    Update-ConsoleTable -Name jobs -Key 2 -Remove
}
'@
        $text | Should -Match 'backup  running'
        $text | Should -Match 'backup  done'
        $text | Should -Match 'sync  running'
    }

    It 'returns the output of a script block as a string with Use-ConsoleTarget' {
        $text = Invoke-ContentChild -Script @'
$captured = Use-ConsoleTarget -Width 30 {
    Write-ConsoleMarkup '[pass]inside[/]'
    [pscustomobject] @{ Name = 'alpha'; Count = 7 } | Format-ConsoleTable
    'pipeline output'
}
'OUTER-START'
$captured
'OUTER-END'
'@
        $lines = @($text -split "`n")
        $lines[0] | Should -BeExactly 'OUTER-START'
        $lines | Should -Contain 'inside'
        $lines | Should -Contain 'pipeline output'
        ($lines | Where-Object { $_ -match '^\| alpha' }) | Should -Not -BeNullOrEmpty
        $lines[-1] | Should -BeExactly 'OUTER-END'
    }

    It 'writes to a file with Use-ConsoleTarget -Path' {
        $file = Join-Path $TestDrive 'report.txt'
        $null = Invoke-ContentChild -Script "Use-ConsoleTarget -Path '$file' -Width 40 { Write-ConsoleRule -Title 'Report'; Write-ConsoleMarkup '[muted]end[/]' }"
        $content = Get-Content -LiteralPath $file
        $content[0] | Should -Match 'Report'
        $content[0].Length | Should -Be 40
        $content[-1] | Should -BeExactly 'end'
    }

    It 'shows emoji as ASCII on the ASCII glyph tier' {
        $text = Invoke-ContentChild -Environment @{ CONSOLEMONSTER_GLYPHS = 'ascii' } -Script @'
[pscustomobject] @{ Check = "$([char]0x2705) backup"; Result = 'ok' } | Format-ConsoleTable -Width 40
'@
        $text | Should -Match '\[OK\] backup'
    }

    It 'updates cells, the summary bar and activities by name, keeping the bar''s total' {
        $text = Invoke-ContentChild -Script @'
$map = New-ConsoleCellMap -Name checks -Count 4 -Columns 2 -RowLabels 'SRV01', 'SRV02'
$bar = New-ConsoleSummaryBar -Total 4 -Counter ([ordered] @{ passed = 0; failed = 0 })
$list = New-ConsoleActivityList -Name running
$d = New-ConsoleDashboard -Title 'Fleet' { $bar; $map; $list }
Start-ConsoleDashboard $d {
    Update-ConsoleActivity -Name running -Id 1 -Activity 'SRV01 check'
    Update-ConsoleActivity -Name running -Id 2 -Activity 'SRV02 check' -Percent 50 -Status 'second try'
    Update-ConsoleCell -Name checks -Index 0, 1 -Status Pass
    Update-ConsoleCell -Name checks -Index 3 -Status Error
    Update-ConsoleCell -Name checks -Add -Status Warn
    Update-ConsoleSummaryBar -Done 1 -Counter ([ordered] @{ passed = 1 })
    Update-ConsoleSummaryBar -Done 3 -Counter ([ordered] @{ passed = 2; failed = 1 })
    Update-ConsoleActivity -Name running -Id 1 -Complete
}
"map=$($map[0]),$($map[1]),$($map[2]),$($map[3]),$($map[4])"
"bar=$($bar.Done)/$($bar.Total)"
"activities=$($list.Count)"
'@
        $text | Should -Match 'map=Pass,Pass,Pending,Fail,Warn'
        $text | Should -Match 'bar=3/4'
        $text | Should -Match 'activities=1'
        $text | Should -Match 'SRV02 check'
        $text | Should -Match '2 passed'
    }

    It 'shows the time of a simulated clock in the header and the log with Start-ConsoleDashboard -Clock' {
        $text = Invoke-ContentChild -Script @'
$clock = New-ConsoleClock -Speed 0
$d = New-ConsoleDashboard -Title 'Replay' { New-ConsoleLog -Height 3 }
Start-ConsoleDashboard $d -Clock $clock -ArgumentList $clock {
    param($Clock)
    $Clock.Advance(83)
    Write-ConsoleLog 'replayed'
}
'@
        $text | Should -Match '00:01:23'
        $text | Should -Match '01:23 +replayed'
    }
}
