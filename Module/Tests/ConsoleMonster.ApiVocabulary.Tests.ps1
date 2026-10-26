Describe 'ConsoleMonster public API vocabulary' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
        $script:Commands = @(Get-Command -Module ConsoleMonster -CommandType Cmdlet)
    }

    It 'exports one command for each approved job and removes the old names and aliases' {
        $names = $script:Commands.Name
        $fixture = Get-Content (Join-Path $PSScriptRoot '../../Tests/Api/round2-commands.json') -Raw | ConvertFrom-Json
        $names.Count | Should -Be $fixture.ExpectedCount
        Compare-Object @($fixture.Commands) @($names) | Should -BeNullOrEmpty
        foreach ($name in $fixture.Retired) {
            $names | Should -Not -Contain $name
            Get-Command -Module ConsoleMonster -Name $name -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
        foreach ($name in 'Format-ConsoleTable', 'Format-ConsoleList', 'Format-ConsoleTree', 'Format-ConsoleCode', 'Format-ConsoleJson', 'Format-ConsoleError', 'Write-ConsoleTable', 'Write-ConsoleTree', 'Write-ConsoleKeyValue', 'Write-ConsoleWidget', 'Write-ConsoleMarkup', 'Write-ConsoleException', 'Write-ConsoleRule', 'Out-ConsoleChart', 'Start-ConsoleRecording', 'Start-ConsoleReplay', 'Start-ConsoleDemo', 'Invoke-ConsoleDemo', 'Update-ConsoleTable', 'New-ConsoleLog', 'Write-ConsoleLog', 'Write-ConsoleStatus', 'Update-ConsoleStatus', 'New-ConsoleRun', 'Start-ConsoleDashboard', 'Read-ConsoleSelection', 'Read-ConsoleMultiSelection', 'New-ConsoleClock', 'New-ConsoleRunResult') {
            $names | Should -Not -Contain $name
            Get-Command -Module ConsoleMonster -Name $name -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
        foreach ($name in 'fct', 'fcl', 'ForEach-ConsoleItem') {
            Get-Alias $name -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }

    It 'exports no aliases and declares no compatibility parameter aliases' {
        $module = Get-Module ConsoleMonster
        $module.ExportedAliases.Count | Should -Be 0
        $manifest = Import-PowerShellDataFile (Join-Path $PSScriptRoot '../ConsoleMonster.psd1')
        @($manifest.AliasesToExport).Count | Should -Be 0
        foreach ($command in $script:Commands) {
            foreach ($entry in $command.Parameters.GetEnumerator()) {
                $parameter = $entry.Value
                # Common parameters belong to PowerShell, including its native aliases.
                $property = $command.ImplementingType.GetProperty($parameter.Name)
                if ($null -eq $property -or $property.DeclaringType.Namespace -notlike 'ConsoleMonster*') { continue }
                $parameter.Aliases.Count | Should -Be 0 -Because "$($command.Name) -$($parameter.Name) has one public name"
            }
        }
    }

    It 'runs Table and List shortcuts through the same view implementation and output contract' {
        $rows = @([pscustomobject]@{ Name = 'first'; Count = 2 }, [pscustomobject]@{ Name = 'second'; Count = 5 })
        foreach ($view in 'Table', 'List') {
            $command = Get-Command "Show-Console$view"
            $command.ImplementingType.BaseType | Should -Be (Get-Command Show-Console).ImplementingType.BaseType
            $shortcut = $rows | & $command -AsString -Width 60
            $canonical = $rows | Show-Console -View $view -AsString -Width 60
            $shortcut | Should -Be $canonical
        }
    }

    It 'keeps exporting in Export-ConsoleFrame and the default sink limited to routing' {
        (Get-Command Out-ConsoleDefault).Parameters.ContainsKey('Format') | Should -BeFalse
        (Get-Command Out-ConsoleDefault).Parameters.ContainsKey('Width') | Should -BeFalse
        (Get-Command Export-ConsoleFrame).Parameters.Keys | Should -Contain Format
    }

    It 'uses the shared presentation options and removes the literal-text compatibility switch' {
        (Get-Command New-ConsoleText).Parameters.ContainsKey('Plain') | Should -BeFalse
        $lanes = New-ConsoleLanes -NoDetail -NoDimPending -CountSkipped -BoldActive -ShowRunningCount -MaxLabelWidth 17 -CollapseWhen Pass -RateWindowSeconds 0.5
        $lanes.ShowDetail | Should -BeFalse
        $lanes.DimPending | Should -BeFalse
        $lanes.CountSkipped | Should -BeTrue
        $lanes.BoldActive | Should -BeTrue
        $lanes.ShowRunningCount | Should -BeTrue
        $lanes.MaxLabelWidth | Should -Be 17
        $lanes.CollapseWhen.ToString() | Should -Be Pass
        $lanes.RateWindow.TotalSeconds | Should -Be 0.5
        $cells = New-ConsoleCellMap -Gap 2 -Window Active -NoSpinRunning
        $cells.Gap | Should -Be 2
        $cells.Window.ToString() | Should -Be Active
        $cells.SpinRunning | Should -BeFalse
        (New-ConsoleSummaryBar -NoElapsed -DoneLabel 'Checked').ShowElapsed | Should -BeFalse
        (New-ConsoleSummaryBar -DoneLabel 'Checked').DoneLabel | Should -Be Checked
        (New-ConsoleText 'literal [pass]value[/]' -Overflow Ellipsis).PlainText | Should -Be 'literal [pass]value[/]'
        foreach ($command in 'New-ConsoleProgress', 'New-ConsoleLanes') {
            $widget = & $command -Columns @{ Type = 'Percent'; Alignment = 'Right'; Priority = 17; MaxWidth = 6 }
            $widget.Columns.Count | Should -Be 1
            $widget.Columns[0].Alignment.ToString() | Should -Be Right
            $widget.Columns[0].Priority | Should -Be 17
            $widget.Columns[0].MaxWidth | Should -Be 6
            { & $command -Columns @{ Type = 'Percent'; Width = 6 } } | Should -Throw '*Unknown item field*'
            { & $command -Columns @{ Type = 'Percent'; MaxWidth = -1 } } | Should -Throw '*MaxWidth*'
        }
        $spinner = New-ConsoleProgress -Indeterminate -StartedSeconds 0.25 -FinishedSeconds 1.5 -Status Pass
        $spinner.Started.TotalSeconds | Should -Be 0.25
        $spinner.Finished.TotalSeconds | Should -Be 1.5
        { New-ConsoleProgress -Value 1 -StartedSeconds 0.25 } | Should -Throw
        $rule = New-ConsoleRule 'Build' -LineRole Accent -LineStyle Double
        $rule.LineRole.ToString() | Should -Be Accent
        $rule.LineStyle.ToString() | Should -Be Double
        (Show-Console -View Rule -Title Build -LineRole Accent -LineStyle Double -AsWidget).LineStyle | Should -Be $rule.LineStyle
        { Show-Console -View Json -LineRole Accent -InputObject '{}' } | Should -Throw '*does not support LineRole*'
    }

    It 'passes typed export options through the same C# serializers without changing the request' {
        $options = [Activator]::CreateInstance((Get-Command Export-ConsoleFrame).Parameters['Options'].ParameterType)
        $options.Width = 0
        $options.Padding = 7
        $options.Fragment = $true
        $options.LineHeight = 1.5
        $widget = New-ConsolePanel -Title Build -Content { New-ConsoleText Ready }
        $actual = $widget | Export-ConsoleFrame -Format Html -Options $options
        $exporter = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Export.ConsoleExporter')
        $expected = $exporter::ToHtml($widget, $options)
        $actual | Should -BeExactly $expected
        $options.Width | Should -Be 0
        ($widget | Export-ConsoleFrame -Format Text -Options @{ Width = 25 }) | Should -Match Ready
        $options.Padding | Should -Be 7
        { $widget | Export-ConsoleFrame -Format Html -Options $options -Width 40 } | Should -Throw '*Width*cannot be combined*'
        $cast = Join-Path $TestDrive 'sample.cast'
        [IO.File]::WriteAllText($cast, "{`"version`":2,`"width`":10,`"height`":2}`n[0,`"o`",`"Ready`"]`n")
        $playback = [Activator]::CreateInstance((Get-Command Export-ConsoleRecording).Parameters['Options'].ParameterType)
        $playback.Width = 0
        $playback.MaxFrames = 10
        $playback.FontSize = 12
        $actual = Export-ConsoleRecording -Path $cast -Format Svg -Options $playback
        $recordingType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecording')
        $recording = $recordingType::Read($cast)
        $format = [Enum]::Parse([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Export.ExportFormat'), 'Svg')
        $expected = $recording.Export($format, $playback)
        $actual | Should -BeExactly $expected
        $playback.Width | Should -Be 0
        { Export-ConsoleRecording -Path $cast -Options $playback -Speed 2 } | Should -Throw '*Speed*cannot be combined*'
        $form = New-ConsoleForm 'Build'
        { Read-ConsoleForm $form -Options ([Activator]::CreateInstance((Get-Command Read-ConsoleForm).Parameters['Options'].ParameterType)) -Inline } | Should -Throw '*Inline*cannot be combined*'
        $menu = New-ConsoleMenu 'Build'
        { Show-ConsoleMenu $menu -Options ([Activator]::CreateInstance((Get-Command Show-ConsoleMenu).Parameters['Options'].ParameterType)) -NoCommandPalette } | Should -Throw '*NoCommandPalette*cannot be combined*'
    }

    It 'uses the same chart controls and rejects them for mismatched kinds' {
        $bar = New-ConsoleChart -Type Bar -NoValues -Signed
        $bar.ShowValues | Should -BeFalse
        $bar.Signed | Should -BeTrue
        $stacked = New-ConsoleChart -Type Stacked -NoLegend -MaxLegendColumns 2
        $stacked.ShowLegend | Should -BeFalse
        $stacked.MaxLegendColumns | Should -Be 2
        (New-ConsoleChart -Type Line -Capacity 7).Capacity | Should -Be 7
        (New-ConsoleChart -Type Sparkline -Capacity 9).Capacity | Should -Be 9
        { New-ConsoleChart -Type Line -NoValues } | Should -Throw '*does not support ShowValues*'
        { New-ConsoleChart -Type Bar -NoLegend } | Should -Throw '*does not support ShowLegend*'
        { New-ConsoleChart -Type Bar -Capacity 7 } | Should -Throw '*does not support Capacity*'
    }

    It 'keeps shortcut metadata limited to the selected representation' {
        foreach ($name in 'Language', 'LineNumbers', 'PlaybackMode', 'Bins', 'ChildrenProperty', 'NoStack') {
            (Get-Command Show-ConsoleTable).Parameters.ContainsKey($name) | Should -BeFalse
            (Get-Command Show-ConsoleList).Parameters.ContainsKey($name) | Should -BeFalse
        }
        (Get-Command Show-Console).Parameters.Keys | Should -Contain Language
        (Get-Command Show-ConsoleTable).Parameters.Keys | Should -Contain Presentation
        (Get-Command Show-ConsoleList).Parameters.Keys | Should -Contain ShowEmpty
    }

    It 'rejects conflicting menu jobs during parameter binding' {
        { New-ConsoleMenuItem 'Conflict' -Action { } -Submenu (New-ConsoleMenu 'Nested') } | Should -Throw
        { New-ConsoleMenuItem 'Conflict' -Separator -Exit } | Should -Throw
        { New-ConsoleMenuItem 'Conflict' -Select 'One' -Minimum 1 } | Should -Throw
        { New-ConsoleMenuItem 'Conflict' -LoadChoices { 'One' } -Single -Maximum 2 } | Should -Throw '*Single selection*'
        (New-ConsoleMenuItem 'Tasks' -MultiSelect 'One', 'Two' -Minimum 1).Kind.ToString() | Should -Be MultiSelect
    }

    It 'reserves Name and Target for their approved resource meanings' {
        foreach ($command in $script:Commands) {
            if ($command.Parameters.ContainsKey('Name')) {
                $command.Name | Should -BeIn @('Show-ConsoleHelp')
            }
            if ($command.Parameters.ContainsKey('Target')) {
                $command.Name | Should -BeIn @('Use-ConsoleTarget', 'Start-ConsoleRun')
            }
        }
        (Get-Command New-ConsoleField).Parameters.Keys | Should -Contain Key
        (Get-Command New-ConsoleChart).Parameters.Keys | Should -Contain Type
        (Get-Command Set-ConsoleTheme).Parameters.Keys | Should -Contain Theme
        (Get-Command Invoke-ConsoleRecording).Parameters.Keys | Should -Contain Title
        (Get-Command Send-ConsoleEvent).Parameters.Keys | Should -Contain WidgetId
    }

    It 'keeps switches false by default and uses NoX for default-on behavior' {
        foreach ($command in $script:Commands) {
            $instance = [Activator]::CreateInstance($command.ImplementingType)
            foreach ($entry in $command.Parameters.GetEnumerator()) {
                $parameter = $entry.Value
                if ($parameter.ParameterType -ne [Management.Automation.SwitchParameter]) { continue }
                $property = $command.ImplementingType.GetProperty($parameter.Name)
                if ($null -ne $property) {
                    $property.GetValue($instance).IsPresent | Should -BeFalse -Because "$($command.Name) -$($parameter.Name) selects non-default behavior"
                }
            }
            $command.Parameters.Keys | Should -Not -Contain NoLiveView
        }
        foreach ($name in 'Show-ConsoleApp') {
            (Get-Command $name).Parameters.Keys | Should -Contain NoMouse
        }
        (Get-Command New-ConsoleLogRegion).Parameters.Keys | Should -Contain NoElapsed
        (Get-Command Show-ConsoleStatusLine).Parameters.Keys | Should -Contain ShowElapsed
    }

    It 'uses numeric units for PowerShell durations rather than implicit TimeSpan ticks' {
        foreach ($command in $script:Commands) {
            foreach ($entry in $command.Parameters.GetEnumerator()) {
                $parameter = $entry.Value
                $type = $parameter.ParameterType
                $inner = [Nullable]::GetUnderlyingType($type)
                if ($null -ne $inner) { $type = $inner }
                $type | Should -Not -Be ([TimeSpan]) -Because "$($command.Name) -$($parameter.Name) must not interpret a number as ticks"
                if ($parameter.Name -match '(Seconds|Milliseconds)$') {
                    $type | Should -BeIn @([double], [int], [long]) -Because "$($command.Name) -$($parameter.Name) has numeric units"
                }
            }
            foreach ($old in 'Timeout', 'StepTimeout', 'Interval', 'IdleLimit', 'ProgressSeconds', 'Budget') {
                $command.Parameters.Keys | Should -Not -Contain $old -Because $command.Name
            }
        }
        foreach ($name in 'Watch-Console', 'Watch-ConsoleLog') {
            (Get-Command $name).Parameters.Keys | Should -Contain PollIntervalSeconds
        }
    }

    It 'puts live widget addresses at position zero and preserves native progress identity' {
        foreach ($command in $script:Commands) {
            if (!$command.Parameters.ContainsKey('WidgetId')) { continue }
            if ($command.Verb -notin 'Update', 'Set', 'Add', 'New') { continue }
            foreach ($set in $command.ParameterSets) {
                $parameter = $set.Parameters | Where-Object Name -EQ WidgetId
                if ($null -ne $parameter) {
                    if ($command.Name -eq 'Update-Console') { $parameter.IsMandatory | Should -BeTrue }
                    $parameter.Position | Should -Be 0 -Because "$($command.Name) [$($set.Name)] addresses a widget first"
                }
                $positions = @($set.Parameters | Where-Object Position -GE 0 | Group-Object Position | Where-Object Count -GT 1)
                $positions.Count | Should -Be 0 -Because "$($command.Name) [$($set.Name)] has unambiguous positional arguments"
            }
        }
        $progress = Get-Command Write-Progress
        foreach ($name in 'Activity', 'Status', 'Id', 'ParentId', 'PercentComplete', 'SecondsRemaining', 'CurrentOperation', 'Completed', 'SourceId') {
            $progress.Parameters.Keys | Should -Contain $name
        }
        $progress.Parameters.Keys | Should -Not -Contain WidgetId
    }

    It 'resolves automatic widths consistently and rejects negative widths' {
        foreach ($command in $script:Commands) {
            if (!$command.Parameters.ContainsKey('Width')) { continue }
            $instance = [Activator]::CreateInstance($command.ImplementingType)
            $instance.Width | Should -Be 0 -Because $command.Name
            @($command.Parameters['Width'].Attributes | Where-Object { $_ -is [Management.Automation.ValidateRangeAttribute] -and $_.MinRange -eq 0 }).Count | Should -Be 1 -Because "$($command.Name) validates automatic and explicit widths"
        }
        $dashboard = New-ConsoleDashboard -Title 'Elapsed' -Content { New-ConsoleText 'ready' }
        $dashboard.ShowElapsed | Should -BeTrue
        (New-ConsoleDashboard -Title 'Elapsed' -NoElapsed -Content { New-ConsoleText 'ready' }).ShowElapsed | Should -BeFalse
    }

    It 'uses one spelling for semantic roles, timestamps and decoration' {
        foreach ($command in $script:Commands) {
            foreach ($old in 'Tone', 'ClockFormat', 'DateTimeFormat', 'ShowTime', 'LogTime') {
                $command.Parameters.Keys | Should -Not -Contain $old -Because $command.Name
            }
            if ($command.Verb -eq 'Show') { $command.Parameters.Keys | Should -Not -Contain As }
        }
    }

    It 'registers no unprefixed accelerators and preserves foreign registrations in a fresh process' {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $proof = Join-Path $PSScriptRoot 'Support/Assert-TypeAccelerators.ps1'
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $output = Invoke-TestChild -Runtime ([Diagnostics.Process]::GetCurrentProcess().Path) -Script "& '$proof' -ModulePath '$module'"
        $LASTEXITCODE | Should -Be 0 -Because ($output -join "`n")
        ($output -join "`n") | Should -Match ACCELERATORS-OK
    }

    It 'renders supplied widgets with the same geometry as the C# writer' {
        $widget = New-ConsolePanel -Title 'Frame' -Content { New-ConsoleText 'ready' }
        $actual = Use-ConsoleTarget -Width 60 { $widget | Show-Console }
        $expected = Use-ConsoleTarget -Width 60 { [ConsoleMonster.Terminal]::Write($widget) }
        $actual | Should -Be $expected
    }

    It 'keeps AsString plain and honors the current target and explicit narrow widths' {
        $profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(57, 24, 'TrueColor', 'Full')
        $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile)
        $previous = if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering }
        try {
            if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = 'Ansi' }
            Use-ConsoleTarget -Target $target {
                $text = Show-Console -Title 'Section' -AsString -View Rule
                $text.IndexOf([char]27) | Should -Be -1
                ($text -split "`n")[0].Length | Should -Be 57
                $narrow = Show-Console -AsString -Width 5 -View Rule
                ($narrow -split "`n")[0].Length | Should -Be 5
            }
        } finally {
            if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = $previous }
        }
    }

    It 'keeps addressed runs live by default and maps NoLive to append-only output' {
        $profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(60, 24, 'None', 'Ascii').WithMode('Interactive')
        $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile)
        Use-ConsoleTarget -Target $target {
            $run = Start-ConsoleRun -Items @('build') -ProgressIntervalSeconds 0
            try { $run.IsLive | Should -BeTrue } finally { Complete-ConsoleRun $run }
            $run = Start-ConsoleRun -Items @('build') -NoLive -ProgressIntervalSeconds 0
            try { $run.IsLive | Should -BeFalse } finally { Complete-ConsoleRun $run }
        }
    }

    It 'uses ParameterName on fields and builds dashboard footer hints from dictionaries or typed hints' {
        (New-ConsoleField -Key host -Label Host -ParameterName ComputerName).ParameterName | Should -Be ComputerName
        $dashboard = New-ConsoleDashboard -Title Test -FooterHints ([ordered]@{ q = 'Quit'; r = 'Refresh' }) -Content { New-ConsoleText ready }
        $dashboard.FooterHints.Count | Should -Be 2
        $dashboard.FooterHints[0].Label.ToString() | Should -Be Quit
        $hints = (New-ConsoleFooterBar -Hint @{ q = 'Quit' }).Hints
        (New-ConsoleDashboard -Title Test -FooterHints $hints -Content { New-ConsoleText ready }).FooterHints.Count | Should -Be 1
    }

    It 'writes consecutive rules with the same one-line geometry as the C# writer' {
        $first = New-ConsoleRule 'Section'
        $second = New-ConsoleRule
        $actual = Use-ConsoleTarget -Width 60 { Show-Console -Title 'Section' -View Rule; Show-Console -View Rule }
        $expected = Use-ConsoleTarget -Width 60 { [ConsoleMonster.Terminal]::Write($first); [ConsoleMonster.Terminal]::Write($second) }
        $actual | Should -Be $expected
    }

    It 'captures a dictionary in one call and exports ordinary rows with the automatic view' {
        $pairs = [ordered] @{ Zulu = 3; Alpha = 1 }
        $text = Show-ConsoleList $pairs -AsString -Width 60
        $text | Should -Match '(?s)Zulu.*3.*Alpha.*1'
        Show-Console -Title 'Section' -AsString -Width 60 -View Rule | Should -Match Section
        $rows = @([pscustomobject] @{ Label = 'first'; Count = 2 }, [pscustomobject] @{ Label = 'second'; Count = 5 })
        $direct = $rows | Export-ConsoleFrame -Format Markdown -Width 60
        $direct | Should -Match 'first'
        $direct | Should -Match 'second'
        $composed = $rows | Show-Console -AsWidget | Export-ConsoleFrame -Format Markdown -Width 60
        $composed | Should -Be $direct
    }
}
