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
        It 'builds nested nodes with statuses and details from records' {
            $text = New-ConsoleTree 'evotec.xyz' -Content {
                [pscustomobject]@{ Label = 'MX'; Status = 'Pass'; Detail = '2 records'; Children = @(& {
                    [pscustomobject]@{ Label = 'mail.protection.outlook.com'; Detail = 'priority 0' }
                }) }
                [pscustomobject]@{ Label = 'DMARC'; Status = 'Fail'; Detail = 'p=none' }
            } | Get-WidgetText
            $text | Should -Match 'evotec\.xyz'
            $text | Should -Match 'MX\s+2 records'
            $text | Should -Match 'mail\.protection\.outlook\.com\s+priority 0'
            $text | Should -Match 'DMARC\s+p=none'
        }

        It 'keeps a single nested custom-object child in a node record' {
            $node = [pscustomobject]@{ Label = 'Root'; Children = [pscustomobject]@{ Label = 'Only child'; Detail = 'Ready' } }
            $text = New-ConsoleTree -Content { $node } | Get-WidgetText
            $text | Should -Match 'Root'
            $text | Should -Match 'Only child\s+Ready'
        }

        It 'shows strings in the script block literally' {
            $text = New-ConsoleTree 'Members' -Content { '[bold]not markup[/]' } | Get-WidgetText
            $text | Should -Match ([regex]::Escape('[bold]not markup[/]'))
        }

        It 'accepts dictionaries through InputObject' {
            (New-ConsoleTree -InputObject ([ordered] @{ A = @('x', 'y') }) | Get-WidgetText) | Should -Match 'y'
            $text = New-ConsoleTree -Title 'Root' -InputObject ([ordered] @{ A = 'note' }) | Get-WidgetText
            $text | Should -Match 'Root'
            $text | Should -Match 'A\s+note'
            (New-ConsoleTree -Title 'Root' -InputObject ([ordered] @{ B = 1 }) | Get-WidgetText) | Should -Match 'B\s+1'
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

        It 'bounds dictionary cycles and item budgets without rejecting a shared child' {
            $shared = [ordered]@{ Leaf = 'ready' }
            $data = [ordered]@{ First = $shared; Second = $shared }
            $text = New-ConsoleTree -InputObject $data | Get-WidgetText
            [regex]::Matches($text, 'Leaf').Count | Should -Be 2
            $text | Should -Not -Match 'cycle'
            $cycle = [ordered]@{ Label = 'root' }
            $cycle.Self = $cycle
            (New-ConsoleTree -InputObject $cycle | Get-WidgetText) | Should -Match 'cycle'
            $wide = [ordered]@{ Items = 1..30 }
            (New-ConsoleTree -InputObject $wide -MaxItems 3 | Get-WidgetText) | Should -Match '\+27'
            (New-ConsoleTree -InputObject $data -MaxNodes 2 | Get-WidgetText) | Should -Match 'node limit'
        }

        It 'rejects cyclic definition content in a child process and keeps repeated acyclic content' {
            . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
            $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
            $body = @'
Import-Module '__MODULE__'
$array = [object[]]::new(1)
$array[0] = $array
$count = 0
foreach ($command in 'New-ConsoleTree', 'New-ConsoleRows') {
    try { & $command -Content { ,$array } -ErrorAction Stop | Out-Null }
    catch { if ($_.Exception.Message -notmatch 'recursive or excessively nested') { throw }; $count++ }
}
try { New-ConsoleTree -Content { [pscustomobject]@{ Label = 'root'; Children = $array } } -ErrorAction Stop | Out-Null }
catch { if ($_.Exception.Message -notmatch 'recursive or excessively nested') { throw }; $count++ }
try { Show-Console -View Tree -Content { ,$array } -ErrorAction Stop | Out-Null }
catch { if ($_.Exception.Message -notmatch 'recursive or excessively nested') { throw }; $count++ }
$shared = @('leaf')
$tree = New-ConsoleTree -Content { ,@($shared, $shared) }
$text = $tree | Export-ConsoleFrame -Format Text
if ([regex]::Matches($text, 'leaf').Count -ne 2) { throw 'Shared content was rejected or lost.' }
"GUARDED=$count"
'@
            $text = Invoke-TestChild -Runtime ([Diagnostics.Process]::GetCurrentProcess().Path) -Script $body.Replace('__MODULE__', $module.Replace("'", "''"))
            $LASTEXITCODE | Should -Be 0 -Because ($text -join "`n")
            ($text -join "`n") | Should -Match 'GUARDED=4'
        }

        It 'takes nodes from the pipeline as they are' {
            $text = [pscustomobject]@{ Label = 'MX'; Status = 'Pass'; Detail = '2 records'; Children = @(& { 'child' }) } | New-ConsoleTree 'Piped' | Get-WidgetText
            $text | Should -Match 'MX\s+2 records'
            $text | Should -Match 'child'
        }
    }

    Context 'Item commands in their containers' {
        It 'distinguishes ordinary Key properties from explicit Key and Value descriptors' {
            foreach ($record in @([ordered] @{ Key = 'rsa'; Owner = 'build' }, [pscustomobject] @{ Key = 'rsa'; Owner = 'build' })) {
                $list = $record | New-ConsoleKeyValue
                $list.Items.Count | Should -Be 2
                ($list | Get-WidgetText) | Should -Match 'Owner\s+build'
                $list = New-ConsoleKeyValue -Content { $record }
                $list.Items.Count | Should -Be 2
                $list = New-ConsoleKeyValue | Add-ConsoleItem $record -PassThru
                $list.Items.Count | Should -Be 2
            }
            $pair = [pscustomobject] @{ Key = 'Signing'; Value = $null } | New-ConsoleKeyValue
            $pair.Items.Count | Should -Be 1
            $pair.Items[0].Key | Should -Be Signing
            { [pscustomobject] @{ Key = 'Size'; Value = 1; Tone = 'Accent' } | New-ConsoleKeyValue } | Should -Throw '*Unknown item field*Tone*'
        }

        It 'seeds live tables with the same evaluated calculated columns and formatting' {
            $script:liveExpressionReads = 0
            $rows = @([pscustomobject] @{ Id = 'first'; Size = 3 }, [pscustomobject] @{ Id = 'second'; Size = 5 })
            $table = $rows | New-ConsoleTable -Live -WidgetId jobs -KeyProperty Id -Property @(
                @{ Name = 'Doubled'; Expression = { $script:liveExpressionReads++; $_.Size * 2 }; FormatString = 'F1' }
                @{ Name = 'Size'; Presenter = 'Number' }
            )
            $script:liveExpressionReads | Should -Be 2
            $text = $table | Get-WidgetText
            $table.Keys | Should -Be @('first', 'second')
            $text | Should -Match '6\.0'
            $text | Should -Match '10\.0'
            $script:liveExpressionReads | Should -Be 2
        }

        It 'keeps default live-row identities separate from rounded display values without evaluating twice' {
            $script:liveIdentityReads = 0
            $table = @([pscustomobject] @{ Id = 1.1 }, [pscustomobject] @{ Id = 1.2 }) | New-ConsoleTable -Live -Property @(
                @{ Name = 'Id'; Expression = { $script:liveIdentityReads++; $_.Id }; FormatString = 'F0' }
            )
            $null = $table | Get-WidgetText
            $table.Keys | Should -Be @('1.1', '1.2')
            $table.Table.Rows.Count | Should -Be 2
            $script:liveIdentityReads | Should -Be 2
        }

        It 'keeps record values literal unless a presenter is explicit' {
            $literal = [pscustomobject]@{ Key = 'Install folder'; Value = './toolbox-agent' } | New-ConsoleKeyValue
            $literal.Items[0].Cell | Should -BeNullOrEmpty
            $literal.Items[0].Value | Should -Be './toolbox-agent'
            $typed = [pscustomobject]@{ Key = 'Size'; Value = 1024; Presenter = 'Bytes' } | New-ConsoleKeyValue
            $typed.Items[0].Cell | Should -Not -BeNullOrEmpty
        }

        It 'adds key/value items with a status and a presenter' {
            $text = New-ConsoleKeyValue -InputObject ([ordered] @{ Module = 'ConsoleMonster' }) -Content {
                [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
                [pscustomobject]@{ Key = 'Size'; Value = 1482113; Presenter = 'Bytes' }
            } | Get-WidgetText
            $text | Should -Match 'Module\s+ConsoleMonster'
            $text | Should -Match 'Signing'
            $text | Should -Match 'MB'
        }

        It 'adds status items from a script block, hashtables and objects in the pipeline' {
            $text = New-ConsoleStatusList -Content { [pscustomobject]@{ Label = 'Restore'; Status = 'Pass'; Detail = '1.6 s' } } | Get-WidgetText
            $text | Should -Match 'Restore\s+1\.6 s'
            $piped = @{ Label = 'Build'; Status = 'Warning' }, [pscustomobject] @{ Label = 'Test'; Status = 'Error'; Detail = '2 failed' } | New-ConsoleStatusList | Get-WidgetText
            $piped | Should -Match 'Build'
            $piped | Should -Match 'Test\s+2 failed'
        }

        It 'rejects an unknown status word' {
            { @{ Label = 'Build'; Status = 'Passs' } | New-ConsoleStatusList } | Should -Throw '*not a status*'
        }

        It 'builds phases from status items and keeps -Phase' {
            $text = New-ConsolePhases -Content { [pscustomobject]@{ Label = 'Build'; Status = 'Pass'; Detail = '14s' }; [pscustomobject]@{ Label = 'Test'; Status = 'Running' } } | Get-WidgetText
            $text | Should -Match 'Build 14s'
            (New-ConsolePhases -Phase 'Restore', 'Pack' | Get-WidgetText) | Should -Match 'Restore.*Pack'
        }

        It 'colours bars and stacked segments with -Role' {
            $stacked = New-ConsoleChart -Content { [pscustomobject]@{ Label = 'Passed'; Value = 412; Role = 'Pass' }; [pscustomobject]@{ Label = 'Failed'; Value = 23; Role = 'Fail' } } -Type Stacked
            $stacked.Segments[0].Role | Should -Be 'Pass'
            (New-ConsoleChart -Role Fail -InputObject ([ordered] @{ DNS = 4 }) -Type Bar).Role | Should -Be 'Fail'
            { @{ Label = 'DNS'; Value = 'four' } | New-ConsoleChart -Type Bar } | Should -Throw '*numeric Value*'
        }

        It 'adds chart series with values and an automatic or chosen role' {
            $chart = New-ConsoleChart -Content {
                [pscustomobject]@{ Key = 'completed'; Role = 'Accent'; Values = 1, 2, 3 }
                [pscustomobject]@{ Key = 'failed'; Values = 4, 5 }
            } -Type Line
            $chart.Series.Count | Should -Be 2
            $chart.Series[0].Role | Should -Be 'Accent'
            $chart.Series[1].Values.Count | Should -Be 2
            $chart.Series[1].Role | Should -Be 'Fail'
        }

        It 'writes log entries with levels and fractional source-relative seconds' {
            $log = New-ConsoleLogRegion -Height 4 -Content {
                [pscustomobject]@{ Message = 'first'; AtSeconds = 2 }
                [pscustomobject]@{ Message = 'second'; Level = 'Warning'; AtSeconds = 90.0 }
                [pscustomobject]@{ Message = 'third'; Level = 'Error'; AtSeconds = 44 }
            }
            @($log.Entries).Count | Should -Be 3
            @($log.Entries)[1].Level | Should -Be 'warn'
            @($log.Entries)[1].Time | Should -Be ([timespan]::FromSeconds(90))
        }

        It 'adds lanes with their own totals and progress' {
            $lanes = New-ConsoleLanes -Content { [pscustomobject]@{ Key = 'DC01'; Total = 40; Status = 'Pass'; Value = 40 }; [pscustomobject]@{ Key = 'DC02'; Total = 20; Status = 'Running'; Value = 5 } }
            $lanes.Items.Count | Should -Be 2
            $lanes.Items[1].Total | Should -Be 20
            (New-ConsoleLanes -WidgetId servers -Lane 'A', 'B' -Total 3).Items.Count | Should -Be 2
        }

        It 'preserves lane time boundaries through containers and Add-ConsoleItem' {
            $lane = [pscustomobject]@{ Key = 'build'; Total = 5; Status = 'Pass'; Passed = 5; StartedSeconds = 2; FinishedSeconds = 9; Value = 5 }
            $lanes = $lane | New-ConsoleLanes -Columns Description, Elapsed, Status
            $lanes.Items[0].Started.TotalSeconds | Should -Be 2
            $lanes.Items[0].Finished.TotalSeconds | Should -Be 9
            $lanes.Items[0].Passed | Should -Be 5
            $text = $lanes | Get-WidgetText
            $text | Should -Match '7(?:\.0)?s'
            $more = New-ConsoleLanes
            $more | Add-ConsoleItem $lane
            $more.Items[0].Started.TotalSeconds | Should -Be 2
            $more.Items[0].Finished.TotalSeconds | Should -Be 9
        }

        It 'pads and sets progress status, sparkline role and latest value' {
            (New-ConsolePadding -Padding 2, 1 -Content { 'padded' } | Get-WidgetText -Width 20) | Should -Match '^\s*\n  padded'
            (New-ConsoleProgress -Label 'Docs' -Value 100 -Total 100 -Status Pass).Status | Should -Be 'Pass'
            $spark = 1, 2, 3 | New-ConsoleChart -Role Fail -NoLatest -Type Sparkline
            $spark.Values.Count | Should -Be 3
            $spark.ShowLatest | Should -BeFalse
        }

        It 'draws a cell map and a summary bar' {
            $map = New-ConsoleCellMap -Status Pass, Pass, Fail -Count 5
            $map.Count | Should -Be 5
            (New-ConsoleSummaryBar -Total 4 -Counter ([ordered] @{ passed = 2; failed = 1 }) -Value 3 | Get-WidgetText) | Should -Match '3/4'
        }

        It 'draws a cell map as a labelled grid with a wrapping legend' {
            $map = New-ConsoleCellMap -Status Pass, Fail, Warn, Pass, Skip -Count 6 -Columns 3 -RowLabels 'SRV01', 'SRV02' -LegendLines 2
            $map.Columns | Should -Be 3
            $map.RowLabels -join ',' | Should -Be 'SRV01,SRV02'
            $map.LegendLines | Should -Be 2
            $lines = @(($map | Get-WidgetText -Width 30) -split "`n" | Where-Object { $_.Trim() })
            $lines[0] | Should -Match '^SRV01 '
            $lines[1] | Should -Match '^SRV02 '
            # Four states and the pending one do not fit one line of 30 cells: the legend takes a second line.
            $lines.Count | Should -Be 4
        }

        It 'gives columns fixed widths by position and a width to stack below' {
            $columns = New-ConsoleColumns -FixedWidth 0, 12 -CollapseBelow 40 -Content {
                New-ConsolePanel -Title 'Wide' -Weight 2 -Content { 'left' }
                New-ConsolePanel -Title 'Fixed' -Weight 5 -Content { 'right' }
            }
            $columns.CollapseBelow | Should -Be 40
            $columns.Sizes[1].Kind | Should -Be 'Fixed'
            $columns.Sizes[1].Value | Should -Be 12
            # The fixed width replaces the child's own share.
            $columns.Children[1].GetType().Name | Should -Be 'Panel'
            $side = @(($columns | Get-WidgetText -Width 60) -split "`n")
            $side[0].TrimEnd().Length | Should -Be 60
            $side[0].TrimEnd() | Should -Match 'Fixed\W{2,4}$'
            $side[0].TrimEnd().Substring(48) | Should -Match 'Fixed'
            $stacked = @(($columns | Get-WidgetText -Width 39) -split "`n" | Where-Object { $_.Trim() })
            $stacked.Count | Should -BeGreaterThan @($side | Where-Object { $_.Trim() }).Count
        }

        It 'takes live table columns with options and an activity list with its settings' {
            $table = New-ConsoleTable -WidgetId slowest -Live -Columns 'Check', @{ Name = 'Took'; Alignment = 'Right' }, @{ Name = 'Actual'; Flexible = $true; Markup = $true }
            $table.Table.Columns[0].Alignment | Should -Not -Be 'Right'
            $table.Table.Columns[1].Alignment | Should -Be 'Right'
            $table.Table.Columns[2].Flexible | Should -BeTrue
            { New-ConsoleTable -WidgetId bad -ErrorAction Stop -Live -Columns @{ Name = 'Took'; Align = 'Right' } } | Should -Throw '*Align*'
            $list = New-ConsoleActivityList -MaxVisible 3 -ShowIds -NoElapsed
            $list.WidgetId | Should -Be '$progress'
            $list.MaxVisible | Should -Be 3
            $list.ShowIds | Should -BeTrue
            $list.ShowElapsed | Should -BeFalse
            $list.Count | Should -Be 0
        }
    }

    Context 'Add-ConsoleItem' {
        It 'copies rich log entries once and preserves a selected physical line' {
            $source = New-ConsoleLogRegion
            $source.Write([ConsoleMonster.Text.RichText]::FromMarkup("[pass]first`nsecond[/]"))
            $entries = @($source.Entries)
            $all = New-ConsoleLogRegion | Add-ConsoleItem $entries -PassThru
            (@($all.Entries).Text -join '|') | Should -Be 'first|second'
            $selected = New-ConsoleLogRegion | Add-ConsoleItem $entries[1] -PassThru
            @($selected.Entries).Count | Should -Be 1
            @($selected.Entries)[0].Text | Should -Be 'second'
            ($selected | Get-WidgetText) | Should -Match 'second'
            ($selected | Get-WidgetText) | Should -Not -Match 'first'
            $message = [pscustomobject]@{ Message = "third`nfourth" }
            $expanded = New-ConsoleLogRegion | Add-ConsoleItem $message -PassThru
            (@($expanded.Entries).Text -join '|') | Should -Be 'third|fourth'
        }

        It 'grows widgets made earlier and passes them on with -PassThru' {
            $tree = New-ConsoleTree 'Shares'
            $tree | Add-ConsoleItem { [pscustomobject]@{ Label = 'Reports'; Detail = 'D:\Reports' } }
            ($tree | Get-WidgetText) | Should -Match 'Reports\s+D:\\Reports'
            $spark = New-ConsoleChart -Type Sparkline | Add-ConsoleItem ([double[]] (3, 5, 8)) -PassThru
            $spark.Values.Count | Should -Be 3
            $table = New-ConsoleTable -Live -Columns Job, State | Add-ConsoleItem @{ Job = 'nightly'; State = 'ok' }, ([pscustomobject] @{ Job = 'weekly'; State = 'running' }) -PassThru
            $table.Keys.Count | Should -Be 2
        }

        It 'explains what a widget takes when an item does not fit' {
            { New-ConsoleTree 'x' | Add-ConsoleItem 42 } | Should -Throw '*records with Label and Children*'
        }
    }
    Context 'Validation and plain objects' {
        It 'ends a large hierarchy at -MaxNodes with a note' {
            $root = [pscustomobject] @{ Name = 'Root'; Kids = 1..10 | ForEach-Object { [pscustomobject] @{ Name = "child$_" } } }
            ($root | New-ConsoleTree -ChildrenProperty Kids -MaxNodes 4 | Get-WidgetText) | Should -Match 'node limit'
        }

        It 'rejects unknown roles and statuses, and non-numeric values' {
            { @{ Label = 'DNS'; Value = 4; Role = 'Pinkish' } | New-ConsoleChart -Type Bar } | Should -Throw '*not a role*'
            { New-ConsoleStatusList -InputObject ([ordered] @{ Build = 'Passs' }) } | Should -Throw '*not a status*'
            { New-ConsoleChart -InputObject ([ordered] @{ DNS = 'four' }) -Type Bar } | Should -Throw '*finite numeric Value*'
        }

        It 'takes items from arrays emitted as one object and from plain objects' {
            (New-ConsoleStatusList -Content { , @(([pscustomobject]@{ Label = 'a' }), ([pscustomobject]@{ Label = 'b' })) }).Items.Count | Should -Be 2
            ([pscustomobject] @{ Label = 'DNS'; Value = 4; Role = 'Fail' } | New-ConsoleChart -Type Bar).Items[0].Role | Should -Be 'Fail'
            $pairs = New-ConsoleKeyValue | Add-ConsoleItem ([pscustomobject] @{ Domain = 'evotec.xyz'; Records = 14 }) -PassThru
            ($pairs | Get-WidgetText) | Should -Match 'Records\s+14'
        }
    }
}
