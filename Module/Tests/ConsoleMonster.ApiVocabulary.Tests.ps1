Describe 'ConsoleMonster public API vocabulary' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
        $script:Commands = @(Get-Command -Module ConsoleMonster -CommandType Cmdlet)
    }

    It 'exports one command for each approved job and removes the old names and aliases' {
        $names = $script:Commands.Name
        foreach ($name in 'Show-ConsoleTable', 'Show-ConsoleList', 'Show-ConsoleRule', 'Export-ConsoleDemo', 'Show-ConsolePresentation', 'Invoke-ConsolePresentation', 'Unregister-ConsoleView', 'New-ConsoleHeaderBar', 'New-ConsoleFooterBar', 'New-ConsoleColumnFormat') {
            $names | Should -Contain $name
        }
        foreach ($name in 'Format-ConsoleTable', 'Format-ConsoleList', 'Format-ConsoleTree', 'Format-ConsoleCode', 'Format-ConsoleJson', 'Format-ConsoleError', 'Write-ConsoleTable', 'Write-ConsoleTree', 'Write-ConsoleKeyValue', 'Write-ConsoleWidget', 'Write-ConsoleMarkup', 'Write-ConsoleException', 'Write-ConsoleRule', 'Start-ConsoleRecording', 'Start-ConsoleReplay', 'Start-ConsoleDemo', 'Invoke-ConsoleDemo') {
            $names | Should -Not -Contain $name
        }
        foreach ($name in 'fct', 'fcl', 'ForEach-ConsoleItem') {
            Get-Alias $name -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }

    It 'reserves Name and Target for their approved resource meanings' {
        foreach ($command in $script:Commands) {
            if ($command.Parameters.ContainsKey('Name')) {
                $command.Name | Should -BeIn @('New-ConsoleFormPreset', 'New-ConsoleMenuPreset', 'Show-ConsoleHelp')
            }
            if ($command.Parameters.ContainsKey('Target')) {
                $command.Name | Should -BeIn @('Use-ConsoleTarget', 'Start-ConsoleRun')
            }
        }
        (Get-Command New-ConsoleField).Parameters.Keys | Should -Contain Key
        (Get-Command New-ConsoleChartSeries).Parameters.Keys | Should -Contain Key
        (Get-Command Set-ConsoleTheme).Parameters.Keys | Should -Contain Theme
        (Get-Command Invoke-ConsoleRecording).Parameters.Keys | Should -Contain Title
        (Get-Command Send-ConsoleEvent).Parameters.Keys | Should -Contain WidgetId
    }

    It 'keeps switches false by default and uses NoX for default-on behavior' {
        foreach ($command in $script:Commands) {
            $instance = [Activator]::CreateInstance($command.ImplementingType)
            foreach ($parameter in $command.Parameters.Values) {
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
            foreach ($parameter in $command.Parameters.Values) {
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
                    $parameter.Position | Should -Be 0 -Because "$($command.Name) [$($set.Name)] addresses a widget first"
                }
                $positions = @($set.Parameters | Where-Object Position -GE 0 | Group-Object Position | Where-Object Count -GT 1)
                $positions.Count | Should -Be 0 -Because "$($command.Name) [$($set.Name)] has unambiguous positional arguments"
            }
        }
        $progress = Get-Command Write-ConsoleProgress
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

    It 'owns only the explicit accelerator list and preserves foreign registrations in a fresh process' {
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
                $text = Show-ConsoleRule 'Section' -AsString
                $text.IndexOf([char]27) | Should -Be -1
                ($text -split "`n")[0].Length | Should -Be 57
                $narrow = Show-ConsoleRule -AsString -Width 5
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

    It 'captures a dictionary in one call and exports ordinary rows with the automatic view' {
        $pairs = [ordered] @{ Zulu = 3; Alpha = 1 }
        $text = Show-ConsoleList $pairs -AsString -Width 60
        $text | Should -Match '(?s)Zulu.*3.*Alpha.*1'
        Show-ConsoleRule 'Section' -AsString -Width 60 | Should -Match Section
        $rows = @([pscustomobject] @{ Label = 'first'; Count = 2 }, [pscustomobject] @{ Label = 'second'; Count = 5 })
        $direct = $rows | Export-ConsoleFrame -Format Markdown -Width 60
        $direct | Should -Match 'first'
        $direct | Should -Match 'second'
        $composed = $rows | Show-Console -AsWidget | Export-ConsoleFrame -Format Markdown -Width 60
        $composed | Should -Be $direct
    }
}
