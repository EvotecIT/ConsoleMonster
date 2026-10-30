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
$d = New-ConsoleDashboard -Title 'Swap' -Content { New-ConsolePanel -Title 'Main' -Content { New-ConsoleRegion -WidgetId main -Content { New-ConsoleProgress -Label 'waiting' -Indeterminate -WidgetId 'spinner' } } }
Invoke-ConsoleDashboard $d {
    Update-Console -WidgetId main -Content { New-ConsoleLanes -WidgetId servers }
    Update-Console -WidgetId servers -Key srv1 -Status Running
    Update-Console -WidgetId servers -Key srv1 -Result Pass
    Update-Console -WidgetId servers -Key srv1 -Complete
}
'@
        $text | Should -Match 'srv1 started'
        $text | Should -Match 'srv1 PASS'
    }

    It 'adds, updates and removes live table rows by key' {
        $text = Invoke-ContentChild -Script @'
$d = New-ConsoleDashboard -Title 'Jobs' -Content { New-ConsoleTable -WidgetId jobs -MaxRows 10 -Live -Property 'Job', 'State' }
Invoke-ConsoleDashboard $d {
    Update-Console -WidgetId jobs -Row ([pscustomobject]@{ Key = 1; Values = @('backup', 'running') })
    Update-Console -WidgetId jobs -Row ([pscustomobject]@{ Key = 1; Values = @('backup', 'done') })
    Update-Console -WidgetId jobs -Row ([pscustomobject]@{ Key = 2; Values = @('sync', 'running') })
    Update-Console -WidgetId jobs -Key 2 -RemoveRow
}
'@
        $text | Should -Match 'backup  running'
        $text | Should -Match 'backup  done'
        $text | Should -Match 'sync  running'
    }

    It 'reads caller variables with $using: and caller functions with -ImportFunction' {
        $text = Invoke-ContentChild -Script @'
$frames = @(1, 2, 3)
$map = @{ web01 = 'up' }
function Get-Double($n) { $n * 2 }
$d = New-ConsoleDashboard -Title 'Frames' -Content { New-ConsoleProgress p -Total 3 }
$seq = Invoke-ConsoleDashboard $d -ImportFunction Get-Double { foreach ($f in $using:frames) { Update-Console p -Value $f; Get-Double $f } }
"SEQ=$($seq -join ',')"
$par = 'a', 'b' | Invoke-ConsoleDashboard $d -Parallel -ImportFunction Get-Double { param($Item) "$Item$(Get-Double $using:frames.Count)$($using:map['web01'])" }
"PAR=$(($par | Sort-Object) -join ',')"
try { Invoke-ConsoleDashboard $d -ImportFunction Missing-Thing { } } catch { "MISSING=$($_.Exception.Message)" }
'@
        $text | Should -Match 'SEQ=2,4,6'
        $text | Should -Match 'PAR=a6up,b6up'
        $text | Should -Match "MISSING=ImportFunction found no function named 'Missing-Thing'"
    }

    It 'stops a dashboard with two widgets of one default id where it is built' {
        { New-ConsoleDashboard -Title 'Report' -Content { New-ConsoleChart -Type Bar; New-ConsoleChart -Type Bar } } | Should -Throw "*WidgetId 'bars'*-WidgetId*"
        { New-ConsoleDashboard -Title 'Report' -Content { New-ConsoleChart -Type Bar; New-ConsoleChart bars-2 -Type Bar } } | Should -Not -Throw
    }

    It 'upserts ordinary records into a live table through its KeyProperty and columns' {
        $text = Invoke-ContentChild -Script @'
$d = New-ConsoleDashboard -Title 'Health' -Content { New-ConsoleProgress build -Total 2; New-ConsoleTable results -Live -KeyProperty Server -Property Server, State, Ms }
Invoke-ConsoleDashboard $d {
    Update-Console results -Row ([pscustomobject] @{ Server = 'web01'; State = 'Pass'; Ms = 12 })
    Update-Console results -Row ([pscustomobject] @{ Server = 'web01'; State = 'Fail'; Ms = 99 })
    Update-Console results -Row @{ Server = 'web02'; State = 'Warn'; Ms = 50 }
    try { Update-Console results -Row ([pscustomobject] @{ Host = 'x' }) } catch { 'E1 ' + $_.Exception.Message }
    try { Update-Console build -Row ([pscustomobject] @{ Server = 'x' }) } catch { 'E2 ' + $_.Exception.Message }
}
'@
        $text | Should -Match 'web01  Pass  12'
        $text | Should -Match 'web01  Fail  99'
        $text | Should -Match 'web02  Warn  50'
        $text | Should -Match "E1 The row has no 'Server' value"
        $text | Should -Match 'E2 ProgressBar does not support Row'
    }

    It 'updates ordinary live rows with calculated and renamed columns intact' {
        $text = Invoke-ContentChild -Script @'
$table = [pscustomobject] @{ Server = 'web01'; Cpu = 3 } | New-ConsoleTable results -Live -KeyProperty Server -Property @(
    'Server'
    @{ Name = 'CPU %'; Expression = 'Cpu'; FormatString = 'F1' }
    @{ Name = 'Doubled'; Expression = { $_.Cpu * 2 }; FormatString = 'F1' }
)
$dashboard = New-ConsoleDashboard -Title 'Health' -Content { $table }
Invoke-ConsoleDashboard $dashboard {
    Update-Console results -Row ([pscustomobject] @{ Server = 'web01'; Cpu = 12 })
    Update-Console results -Row @{ Server = 'web02'; Cpu = 5 }
}
$table | Export-ConsoleFrame -Format Text -Width 70
'@
        $text | Should -Match 'web01\s+12\.0\s+24\.0'
        $text | Should -Match 'web02\s+5\.0\s+10\.0'
    }

    It 'updates calculated Property descriptors on a live table that starts empty' {
        $text = Invoke-ContentChild -Script @'
$table = New-ConsoleTable results -Live -KeyProperty Server -Property @(
    'Server'
    @{ Name = 'CPU %'; Expression = 'Cpu'; FormatString = 'F1' }
    @{ Name = 'Doubled'; Expression = { $_.Cpu * 2 }; FormatString = 'F1' }
)
$dashboard = New-ConsoleDashboard -Title 'Health' -Content { $table }
Invoke-ConsoleDashboard $dashboard {
    Update-Console results -Row ([pscustomobject] @{ Server = 'web01'; Cpu = 12 })
    Update-Console results -Row @{ Server = 'web02'; Cpu = 5 }
}
$table | Export-ConsoleFrame -Format Text -Width 70
'@
        $text | Should -Match 'web01\s+12\.0\s+24\.0'
        $text | Should -Match 'web02\s+5\.0\s+10\.0'
    }

    It 'returns the output of a script block as a string with Use-ConsoleTarget' {
        $text = Invoke-ContentChild -Script @'
$captured = Use-ConsoleTarget -Width 30 {
    Write-ConsoleText -Markup '[pass]inside[/]'
    [pscustomobject] @{ Name = 'alpha'; Count = 7 } | Show-ConsoleTable
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
        $null = Invoke-ContentChild -Script "Use-ConsoleTarget -Path '$file' -Width 40 { New-ConsoleRule 'Report' | Show-Console; Write-ConsoleText -Markup '[muted]end[/]' }"
        $content = Get-Content -LiteralPath $file
        $content[0] | Should -Match 'Report'
        $content[0].Length | Should -Be 40
        $content[-1] | Should -BeExactly 'end'
    }

    It 'shows emoji as ASCII on the ASCII glyph tier' {
        $text = Invoke-ContentChild -Environment @{ CONSOLEMONSTER_GLYPHS = 'ascii' } -Script @'
[pscustomobject] @{ Check = "$([char]0x2705) backup"; Result = 'ok' } | Show-ConsoleTable -Width 40
'@
        $text | Should -Match '\[OK\] backup'
    }

    It 'updates cells, the summary bar and activities by name, keeping the bar''s total' {
        $text = Invoke-ContentChild -Script @'
$map = New-ConsoleCellMap -WidgetId checks -Count 4 -Columns 2 -RowLabels 'SRV01', 'SRV02'
$bar = New-ConsoleSummaryBar -Total 4 -Counter ([ordered] @{ passed = 0; failed = 0 })
$list = New-ConsoleActivityList -WidgetId running
$d = New-ConsoleDashboard -Title 'Fleet' -Content { $bar; $map; $list }
Invoke-ConsoleDashboard $d {
    Update-Console -WidgetId running -Key 1 -Label 'SRV01 check'
    Update-Console -WidgetId running -Key 2 -Label 'SRV02 check' -Value 50 -Detail 'second try'
    Update-Console -WidgetId checks -Index 0, 1 -Status Pass
    Update-Console -WidgetId checks -Index 3 -Status Error
    Update-Console -WidgetId checks -Status Warn -AppendCells
    Update-Console -Counter ([ordered] @{ passed = 1 }) -WidgetId 'summary' -Value 1
    Update-Console -Counter ([ordered] @{ passed = 2; failed = 1 }) -WidgetId 'summary' -Value 3
    Update-Console -WidgetId running -Key 1 -Complete
}
"map=$($map[0]),$($map[1]),$($map[2]),$($map[3]),$($map[4])"
"bar=$($bar.Value)/$($bar.Total)"
"activities=$($list.Count)"
'@
        $text | Should -Match 'map=Pass,Pass,Pending,Fail,Warn'
        $text | Should -Match 'bar=3/4'
        $text | Should -Match 'activities=1'
        $text | Should -Match 'SRV02 check'
        $text | Should -Match '2 passed'
    }

    It 'shows the time of a simulated clock in the header and the log with Invoke-ConsoleDashboard -Clock' {
        $text = Invoke-ContentChild -Script @'
$clock = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.SimulatedClock'))::new(0)
$d = New-ConsoleDashboard -Title 'Replay' -Content { New-ConsoleLogRegion -Height 3 }
Invoke-ConsoleDashboard $d -Clock $clock -ArgumentList $clock {
    param($Clock)
    $Clock.Advance(83)
    Write-ConsoleLogEntry 'replayed'
}
'@
        $text | Should -Match '00:01:23'
        $text | Should -Match '01:23 +replayed'
    }
}
