Describe 'ConsoleMonster pipeline charts' {
    BeforeAll {
        # A parent-owned module stage may supply this path; otherwise use the ordinary built module.
        $module = if ($env:CONSOLEMONSTER_CHART_TEST_ASSEMBLY) {
            $env:CONSOLEMONSTER_CHART_TEST_ASSEMBLY
        } else {
            Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        }
        Import-Module $module -Force -ErrorAction Stop
    }

    It 'uses dictionary binding and calculated numeric properties without interpreting labels as markup' {
        $chart = @{ Host = '[red]api'; Milliseconds = 250 } | New-ConsoleChart -Property @{
            Name = 'Seconds'
            Expression = { $_.Milliseconds / 1000 }
        } -Label Host
        $chart.Items.Count | Should -Be 1
        $chart.Items[0].Value | Should -Be 0.25
        $chart.Items[0].Label | Should -Be '[red]api'
        ($chart | Export-ConsoleFrame -Format Text -Width 60) | Should -Match '\[red\]api'
    }

    It 'uses raw calculated numeric values despite <FormatString> formatting in <Culture>' -TestCases @(
        @{ Culture = 'en-US'; FormatString = 'N2' }
        @{ Culture = 'pl-PL'; FormatString = 'F2' }
    ) {
        param($Culture, $FormatString)
        $thread = [System.Threading.Thread]::CurrentThread
        $previousCulture = $thread.CurrentCulture
        try {
            $thread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo($Culture)
            $chart = [pscustomobject]@{ Value = 1234.567 } | New-ConsoleChart -Property @{
                Name = 'Doubled'; Expression = { $_.Value * 2 }; FormatString = $FormatString
            }
            $chart.Items.Count | Should -Be 1
            $chart.Items[0].Value | Should -Be (1234.567 * 2)
        } finally { $thread.CurrentCulture = $previousCulture }
    }

    It 'reports malformed missing and nonfinite property values without fabricating zeros' {
        $warnings = @()
        $items = @(
            [pscustomobject]@{ Value = 'bad' }
            [pscustomobject]@{ Other = 9 }
            [pscustomobject]@{ Value = [double]::NaN }
            [pscustomobject]@{ Value = [double]::PositiveInfinity }
            [pscustomobject]@{ Value = $true }
            [pscustomobject]@{ Value = 4 }
        )
        $chart = $items | New-ConsoleChart -Property Value -WarningAction SilentlyContinue -WarningVariable warnings
        $warnings.Count | Should -Be 1
        $warnings[0] | Should -Match '5 rows without chartable values skipped'
        $chart.Items.Count | Should -Be 1
        $chart.Items[0].Value | Should -Be 4
        ($chart | Export-ConsoleFrame -Format Text -Width 60) | Should -Match '4'
    }

    It 'reports unreadable numeric getters and keeps the empty chart mutable' {
        $item = [pscustomobject]@{ Name = 'broken' }
        $item | Add-Member ScriptProperty CPU { throw 'unreadable' }
        $warnings = @()
        $chart = $item | New-ConsoleChart -Property CPU -WarningAction SilentlyContinue -WarningVariable warnings
        $chart.Items.Count | Should -Be 0
        $warnings.Count | Should -Be 1
        $warnings[0] | Should -Match '1 rows without chartable values skipped'
        $null = $chart.Add('later', 4)
        $chart.Items.Count | Should -Be 1
        $chart.Items[0].Value | Should -Be 4
    }

    It 'counts null records without losing valid values or changing the bar chart type' {
        $warnings = @()
        $chart = $null, [pscustomobject]@{ Name = 'first'; CPU = 1 }, [pscustomobject]@{ Name = 'second'; CPU = 2 } |
            New-ConsoleChart -Property CPU -WarningAction SilentlyContinue -WarningVariable warnings
        $chart.GetType().Name | Should -Be BarChart
        @($chart.Items.Value) | Should -Be @(1, 2)
        @($chart.Items.Label) | Should -Be @('first', 'second')
        $warnings.Count | Should -Be 1
        $warnings[0] | Should -Match '1 rows without chartable values skipped'
    }

    It 'creates empty mutable charts with no input and warns for an explicit null' {
        $warnings = @()
        $empty = New-ConsoleChart -WarningVariable warnings
        $empty.Items.Count | Should -Be 0
        $warnings.Count | Should -Be 0
        $explicit = New-ConsoleChart -InputObject $null -WarningAction SilentlyContinue -WarningVariable warnings
        $explicit.Items.Count | Should -Be 0
        $warnings.Count | Should -Be 1
        $null = $empty.Add('later', 4)
        $empty.Items.Count | Should -Be 1
    }

    It 'preserves histogram boundary observations and never evaluates unused labels' {
        $items = -2, -1, 0, 1, 2 | ForEach-Object { [pscustomobject] @{ Value = $_ } }
        foreach ($item in $items) { $item | Add-Member ScriptProperty Name { throw 'label must not run' } }
        $histogram = $items | New-ConsoleChart -Type Histogram -Property Value -Edges -2, -1, 0, 1, 2
        @($histogram.Counts) | Should -Be @(1, 1, 1, 2)
        ($histogram | Export-ConsoleFrame -Format Text -Width 60) | Should -Match '2'
    }

    It 'retains signed trend values and their order' {
        $spark = -1, 2, 0 | New-ConsoleChart -Type Sparkline
        @($spark.Values) | Should -Be @(-1, 2, 0)
        $text = $spark | Export-ConsoleFrame -Format Text -Width 60
        $text | Should -Not -BeNullOrEmpty
        $text.Contains([char]27) | Should -BeFalse
    }

    It 'preserves exact double values and reads calculated Value once' {
        $value = [BitConverter]::Int64BitsToDouble(0x3ff0000000000001L)
        $script:chartValueReads = 0
        $item = [pscustomobject] @{ Name = 'sample' }
        $item | Add-Member ScriptProperty Value { $script:chartValueReads++; $value }
        $chart = $item | New-ConsoleChart -Property Value
        $chart.Items.Count | Should -Be 1
        $chart.Items[0].Value | Should -Be $value
        $script:chartValueReads | Should -Be 1
        $null = $chart | Export-ConsoleFrame -Format Text -Width 60
        $script:chartValueReads | Should -Be 1
    }

    It 'exports chart text to a downstream command and file without ANSI sequences' {
        $chart = [pscustomobject]@{ Label = 'first'; Value = 1 }, [pscustomobject]@{ Label = 'second'; Value = -2 } | New-ConsoleChart
        $text = @($chart | Export-ConsoleFrame -Format Text -Width 60 | ForEach-Object { $_ })
        $text.Count | Should -Be 1
        $text[0] | Should -BeOfType string
        $text[0] | Should -Match 'first'
        $text[0] | Should -Match 'second'
        $text[0].Contains([char]27) | Should -BeFalse
        $path = Join-Path $TestDrive 'chart.txt'
        $chart | Export-ConsoleFrame -Format Text -Width 60 -Path $path
        (Get-Content $path -Raw) | Should -BeExactly $text[0]
    }

    It 'rejects ambiguous property selections and invalid histogram edges' {
        { [pscustomobject]@{ CPU = 1; Count = 2 } | New-ConsoleChart -Property 'C*' } | Should -Throw '*exactly one column*'
        { 1, 2 | New-ConsoleChart -Type Histogram -Edges 2, 1 } | Should -Throw '*strictly increasing*'
    }

    It 'builds charts from ordinary objects with -Property and -Label' {
        $servers = @([pscustomobject] @{ Host = 'web01'; Cpu = 42 }, [pscustomobject] @{ Host = 'db01'; Cpu = 97 })
        $bar = $servers | New-ConsoleChart -Type Bar -Property Cpu -Label Host -Unit '%'
        @($bar.Items.Label) | Should -Be @('web01', 'db01')
        $text = New-ConsolePanel -Title Report -Content { $servers | New-ConsoleChart -Type Bar -Property Cpu -Label Host } | Export-ConsoleFrame -Format Text -Width 60
        $text | Should -Match 'web01'
        $text | Should -Match '97'
        $named = [pscustomobject] @{ Name = 'fs01'; Cpu = 5 } | New-ConsoleChart -Type Bar -Property Cpu
        $named.Items[0].Label | Should -BeExactly 'fs01'
        $spark = $servers | New-ConsoleChart -Type Sparkline -Property Cpu -Title 'CPU'
        $spark.Title | Should -BeExactly 'CPU'
        @($spark.Values) | Should -Be @(42, 97)
        { $servers | New-ConsoleChart -Type Line -Property Cpu } | Should -Throw '*Bar, Histogram and Sparkline*'
        { $servers | New-ConsoleChart -Type Sparkline -Property Cpu -Label Host } | Should -Throw '*Label applies to Bar*'
        { [pscustomobject] @{ Label = 'a'; Value = 1 } | New-ConsoleChart -Type Bar -Label Host } | Should -Throw '*use it with -Property*'
        (Get-Command New-ConsoleChart).Parameters['Title'].ParameterType | Should -Be ([object])
    }

    It 'points ordinary records at the property-based chart view' {
        { [pscustomobject] @{ Name = 'web01'; Cpu = 42 } | New-ConsoleChart -Type Bar } | Should -Throw '*-Property <number> -Label <name>*'
    }

    It 'preserves duplicate labels and uses bars when Type is omitted' {
        $rows = @([pscustomobject]@{Name='alpha';Value=50}, [pscustomobject]@{Name='alpha';Value=40})
        $chart = $rows | New-ConsoleChart -Property Value -Label Name
        @($chart.Items.Label) | Should -Be @('alpha', 'alpha (2)')
        @($chart.Items.Value) | Should -Be @(50, 40)
    }

    It 'preserves duplicate chart records and presents byte properties with units' {
        $records = @([pscustomobject]@{Label='disk';Value=2}, [pscustomobject]@{Label='disk';Value=3})
        $chart = $records | New-ConsoleChart
        @($chart.Items.Label) | Should -Be @('disk', 'disk (2)')
        @($chart.Items.Value) | Should -Be @(2, 3)
        $bytes = [pscustomobject]@{Name='file';Length=[long] 1048576} | New-ConsoleChart -Property Length
        $text = $bytes | Export-ConsoleFrame -Format Text -Width 80
        $text | Should -Match '1 MB'
    }
}
