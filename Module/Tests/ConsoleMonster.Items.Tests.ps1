BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force

    # The widget as plain text at a fixed width, the way it draws.
    function Get-WidgetText {
        param([Parameter(ValueFromPipeline)] $Widget, [int] $Width = 70)
        process { ($Widget | Export-ConsoleFrame -Format Text -Width $Width) -join "`n" }
    }
}

Describe 'One grammar for every widget' {
    Context 'New-ConsoleTree' {
        It 'builds nested nodes with statuses and details from New-ConsoleTreeNode' {
            $text = New-ConsoleTree 'evotec.xyz' {
                New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
                    New-ConsoleTreeNode 'mail.protection.outlook.com' -Detail 'priority 0'
                }
                New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
            } | Get-WidgetText
            $text | Should -Match 'evotec\.xyz'
            $text | Should -Match 'MX\s+2 records'
            $text | Should -Match 'mail\.protection\.outlook\.com\s+priority 0'
            $text | Should -Match 'DMARC\s+p=none'
        }

        It 'shows strings in the script block literally' {
            $text = New-ConsoleTree 'Members' { '[bold]not markup[/]' } | Get-WidgetText
            $text | Should -Match ([regex]::Escape('[bold]not markup[/]'))
        }

        It 'keeps the positional hashtable and -Data forms' {
            (New-ConsoleTree ([ordered] @{ A = @('x', 'y') }) | Get-WidgetText) | Should -Match 'y'
            $text = New-ConsoleTree -Title 'Root' -Data ([ordered] @{ A = 'note' }) | Get-WidgetText
            $text | Should -Match 'Root'
            $text | Should -Match 'A\s+note'
            (New-ConsoleTree -Title 'Root' ([ordered] @{ B = 1 }) | Get-WidgetText) | Should -Match 'B\s+1'
        }

        It 'follows hierarchical objects, including a single child object, with statuses and details' {
            $groups = [pscustomobject] @{
                Name = 'Domain Admins'; Health = 'Warn'; Note = '6 members'
                Members = [pscustomobject] @{ Name = 'Tier 0 operators'; Health = 'Pass'; Members = @([pscustomobject] @{ Name = 'anna.k' }) }
            }
            $text = $groups | New-ConsoleTree 'Groups' -ChildrenProperty Members -StatusProperty Health -DetailProperty Note | Get-WidgetText
            $text | Should -Match 'Domain Admins\s+6 members'
            $text | Should -Match 'Tier 0 operators'
            $text | Should -Match 'anna\.k'
        }

        It 'marks cycles, the depth limit and the item limit' {
            $a = [pscustomobject] @{ Name = 'A'; Kids = @() }
            $b = [pscustomobject] @{ Name = 'B'; Kids = @($a) }
            $a.Kids = @($b)
            ($a | New-ConsoleTree -Children { $_.Kids } | Get-WidgetText) | Should -Match 'A\s+\(cycle\)'
            $deep = [pscustomobject] @{ Name = 'L1'; Kids = @([pscustomobject] @{ Name = 'L2'; Kids = @([pscustomobject] @{ Name = 'L3' }) }) }
            ($deep | New-ConsoleTree -ChildrenProperty Kids -Depth 1 | Get-WidgetText) | Should -Match 'depth limit'
            $wide = [pscustomobject] @{ Name = 'Share'; Files = 1..30 | ForEach-Object { [pscustomobject] @{ Name = "file$_" } } }
            ($wide | New-ConsoleTree -ChildrenProperty Files -MaxItems 5 | Get-WidgetText) | Should -Match '\+25'
        }

        It 'takes nodes from the pipeline as they are' {
            $text = New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' { 'child' } | New-ConsoleTree 'Piped' | Get-WidgetText
            $text | Should -Match 'MX\s+2 records'
            $text | Should -Match 'child'
        }
    }

    Context 'Item commands in their containers' {
        It 'adds key/value items with a status and a presenter' {
            $text = New-ConsoleKeyValue ([ordered] @{ Module = 'ConsoleMonster' }) {
                New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
                New-ConsoleKeyValueItem 'Size' 1482113 -Presenter Bytes
            } | Get-WidgetText
            $text | Should -Match 'Module\s+ConsoleMonster'
            $text | Should -Match 'Signing'
            $text | Should -Match 'MB'
        }

        It 'adds status items from a script block, hashtables and objects in the pipeline' {
            $text = New-ConsoleStatusList { New-ConsoleStatusItem 'Restore' -Status Pass -Detail '1.6 s' } | Get-WidgetText
            $text | Should -Match 'Restore\s+1\.6 s'
            $piped = @{ Label = 'Build'; Status = 'Warning' }, [pscustomobject] @{ Name = 'Test'; Status = 'Error'; Detail = '2 failed' } | New-ConsoleStatusList | Get-WidgetText
            $piped | Should -Match 'Build'
            $piped | Should -Match 'Test\s+2 failed'
        }

        It 'rejects an unknown status word' {
            { @{ Label = 'Build'; Status = 'Passs' } | New-ConsoleStatusList } | Should -Throw '*not a status*'
        }

        It 'builds phases from status items and keeps -Phase' {
            $text = New-ConsolePhases { New-ConsoleStatusItem 'Build' -Status Pass -Detail '14s'; New-ConsoleStatusItem 'Test' -Status Running } | Get-WidgetText
            $text | Should -Match 'Build 14s'
            (New-ConsolePhases -Phase 'Restore', 'Pack' | Get-WidgetText) | Should -Match 'Restore.*Pack'
        }

        It 'colours bars and stacked segments with -Tone' {
            $stacked = New-ConsoleBarChart -Stacked { New-ConsoleBarItem 'Passed' 412 -Tone Pass; New-ConsoleBarItem 'Failed' 23 -Tone Fail }
            $stacked.Segments[0].Token | Should -Be 'Pass'
            (New-ConsoleBarChart -Tone Fail ([ordered] @{ DNS = 4 })).BarToken | Should -Be 'Fail'
            { @{ Label = 'DNS'; Value = 'four' } | New-ConsoleBarChart } | Should -Throw '*numeric Value*'
        }

        It 'adds chart series with values and an automatic or chosen tone' {
            $chart = New-ConsoleLineChart {
                New-ConsoleChartSeries 'completed' -Value 1, 2, 3 -Tone Accent
                4, 5 | New-ConsoleChartSeries 'failed'
            }
            $chart.Series.Count | Should -Be 2
            $chart.Series[0].Token | Should -Be 'Accent'
            $chart.Series[1].Values.Count | Should -Be 2
            $chart.Series[1].Token | Should -Be 'Fail'
        }

        It 'writes log entries with levels and times in seconds, units or TimeSpans' {
            $log = New-ConsoleLog -Height 4 {
                New-ConsoleLogEntry 'first' -At 2s
                New-ConsoleLogEntry 'second' -Level Warning -At 1.5m
                New-ConsoleLogEntry 'third' -Level Error -At (New-TimeSpan -Seconds 44)
            }
            @($log.Entries).Count | Should -Be 3
            @($log.Entries)[1].Level | Should -Be 'warn'
            @($log.Entries)[1].Time | Should -Be ([timespan]::FromSeconds(90))
        }

        It 'adds lanes with their own totals and progress' {
            $lanes = New-ConsoleLanes { New-ConsoleLane 'DC01' -Total 40 -Done 40 -Status Pass; New-ConsoleLane 'DC02' -Total 20 -Done 5 -Status Running }
            $lanes.Items.Count | Should -Be 2
            $lanes.Items[1].Total | Should -Be 20
            (New-ConsoleLanes -Name servers -Lane 'A', 'B' -Total 3).Items.Count | Should -Be 2
        }

        It 'pads and sets progress status, sparkline tone and latest value' {
            (New-ConsolePadding -Padding 2, 1 { 'padded' } | Get-WidgetText -Width 20) | Should -Match '^\s*\n  padded'
            (New-ConsoleProgress 'Docs' -Value 100 -Total 100 -Status Pass).Status | Should -Be 'Pass'
            $spark = 1, 2, 3 | New-ConsoleSparkline -Tone Fail -NoLatest
            $spark.Values.Count | Should -Be 3
            $spark.ShowLatest | Should -BeFalse
        }

        It 'draws a cell map and a summary bar' {
            $map = New-ConsoleCellMap -Status Pass, Pass, Fail -Count 5
            $map.Count | Should -Be 5
            (New-ConsoleSummaryBar -Done 3 -Total 4 -Counter ([ordered] @{ passed = 2; failed = 1 }) | Get-WidgetText) | Should -Match '3/4'
        }
    }

    Context 'Add-ConsoleItem' {
        It 'grows widgets made earlier and passes them on with -PassThru' {
            $tree = New-ConsoleTree 'Shares'
            $tree | Add-ConsoleItem { New-ConsoleTreeNode 'Reports' -Detail 'D:\Reports' }
            ($tree | Get-WidgetText) | Should -Match 'Reports\s+D:\\Reports'
            $spark = New-ConsoleSparkline | Add-ConsoleItem ([double[]] (3, 5, 8)) -PassThru
            $spark.Values.Count | Should -Be 3
            $table = New-ConsoleLiveTable -Column Job, State | Add-ConsoleItem @{ Job = 'nightly'; State = 'ok' }, ([pscustomobject] @{ Job = 'weekly'; State = 'running' }) -PassThru
            $table.Keys.Count | Should -Be 2
        }

        It 'explains what a widget takes when an item does not fit' {
            { New-ConsoleTree 'x' | Add-ConsoleItem 42 } | Should -Throw '*New-ConsoleTreeNode*'
        }
    }
}
