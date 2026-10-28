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

    It 'binds object values and labels then sorts before taking MaxRows' {
        $items = @(
            [pscustomobject]@{ Name = 'a'; CPU = 1 }
            [pscustomobject]@{ Name = 'b'; CPU = 3 }
            [pscustomobject]@{ Name = 'c'; CPU = 2 }
        )
        $text = $items | Show-Console -Property CPU -Label Name -Sort Descending -MaxRows 2 -AsString -View Chart
        $text.Trim() | Should -Be "b: 3`nc: 2"
    }

    It 'uses shared dictionary binding and calculated properties' {
        $chart = @{ Host = '[red]api'; Milliseconds = 250 } | Show-Console -Property @{
            Name = 'Seconds'
            Expression = { $_.Milliseconds / 1000 }
        } -Label Host -AsWidget -View Chart
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 0.25
        $chart.Samples[0].Label | Should -Be '[red]api'
    }

    It 'accepts numbers directly with deterministic row labels' {
        $text = 2, -1, 0 | Show-Console -AsString -View Chart
        $text.Trim() | Should -Be "1: 2`n2: -1`n3: 0"
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
            $errors = @()
            $chart = [pscustomobject]@{ Value = 1234.567 } | Show-Console -Property @{
                Name = 'Doubled'
                Expression = { $_.Value * 2 }
                FormatString = $FormatString
            } -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart
            $errors.Count | Should -Be 0
            $chart.Samples.Count | Should -Be 1
            $chart.Samples[0].Value | Should -Be (1234.567 * 2)
        } finally {
            $thread.CurrentCulture = $previousCulture
        }
    }

    It 'keeps group appearance and sample order in trends' {
        $items = @(
            [pscustomobject]@{ Host = 'b'; Value = 3 }
            [pscustomobject]@{ Host = 'a'; Value = 2 }
            [pscustomobject]@{ Host = 'b'; Value = 1 }
        )
        $text = $items | Show-Console -Type Sparkline -Label Host -AsString -View Chart
        $text.Trim() | Should -Be "b: 3, 1`na: 2"
    }

    It 'counts malformed missing and nonfinite values without fabricating zeros' {
        $errors = @()
        $items = @(
            [pscustomobject]@{ Value = 'bad' }
            [pscustomobject]@{ Other = 9 }
            [pscustomobject]@{ Value = [double]::NaN }
            [pscustomobject]@{ Value = [double]::PositiveInfinity }
            [pscustomobject]@{ Value = $true }
            [pscustomobject]@{ Value = 4 }
        )
        $chart = $items | Show-Console -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart
        $errors.Count | Should -Be 0
        $chart.SkippedCount | Should -Be 5
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 4
        $text = $items | Show-Console -AsString -View Chart
        $text | Should -Match '5 rows without chartable values skipped'
    }

    It 'treats an unreadable numeric getter as invalid data' {
        $item = [pscustomobject]@{ Name = 'broken' }
        $item | Add-Member -MemberType ScriptProperty -Name CPU -Value { throw 'unreadable' }
        $errors = @()
        $result = @($item | Show-Console -Property CPU -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart)
        $errors.Count | Should -Be 0
        $result.Count | Should -Be 1
        $result[0].SkippedCount | Should -Be 1
    }

    It 'produces no output for an empty pipeline' {
        @(@() | Show-Console -AsString -View Chart).Count | Should -Be 0
    }

    It 'counts an explicitly bound null sample without an error' {
        $errors = @()
        $result = @(Show-Console -InputObject $null -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart)
        $result.Count | Should -Be 1
        $result[0].SkippedCount | Should -Be 1
        $errors.Count | Should -Be 0
    }

    It 'counts a pipeline null and keeps valid samples' {
        $errors = @()
        $chart = 1, $null, 2 | Show-Console -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart
        $errors.Count | Should -Be 0
        $chart.SkippedCount | Should -Be 1
        $chart.Samples.Count | Should -Be 2
        $chart.Samples[0].Value | Should -Be 1
        $chart.Samples[1].Value | Should -Be 2
    }

    It 'does not validate pipeline rows beyond the unsorted bar limit' {
        $errors = @()
        $chart = 1, 2, $null, 'bad' | Show-Console -MaxRows 2 -AsWidget -View Chart -ErrorVariable errors
        $chart.Samples.Count | Should -Be 2
        $chart.SkippedCount | Should -Be 0
        $errors.Count | Should -Be 0
    }

    It 'produces no output or errors when invoked without input' {
        $errors = @()
        @(Show-Console -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart).Count | Should -Be 0
        $errors.Count | Should -Be 0
    }

    It 'counts every histogram observation including the last edge' {
        $text = -2, -1, 0, 1, 2 | Show-Console -Type Histogram -Bins 4 -MaxRows 1 -AsString -View Chart
        $text.Trim() | Should -Be "[-2, -1): 1`n[-1, 0): 1`n[0, 1): 1`n[1, 2]: 2"
    }

    It 'counts histogram values without evaluating unreadable or calculated labels' {
        $item = [pscustomobject]@{ Value = 4 }
        $item | Add-Member -MemberType ScriptProperty -Name Name -Value { throw 'label must not run' }
        $errors = @()
        $chart = $item | Show-Console -Type Histogram -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors -View Chart
        $errors.Count | Should -Be 0
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 4
        $text = $item | Show-Console -Type Histogram -Label @{
            Name = 'Unused'
            Expression = { throw 'calculated label must not run' }
        } -AsString -ErrorAction Stop -View Chart
        $text.Trim() | Should -Be '[2, 4]: 1'
    }

    It 'omits the separator for unnamed positive and signed trends' {
        (1, 2 | Show-Console -Type Sparkline -AsString -View Chart).Trim() | Should -Be '1, 2'
        (-1, 2 | Show-Console -Type Sparkline -AsString -View Chart).Trim() | Should -Be '-1, 2'
    }

    It 'preserves the same exact double value on both PowerShell runtimes' {
        $value = [BitConverter]::Int64BitsToDouble(0x3ff0000000000001L)
        $chart = $value | Show-Console -AsWidget -View Chart
        $chart.Samples[0].Value | Should -Be $value
        ($value | Show-Console -AsString -View Chart).Trim() | Should -Be '1: 1.0000000000000002'
    }

    It 'emits plain strings to a downstream command through PSFormatting' {
        $lines = @(1, 2 | Show-Console -View Chart | ForEach-Object { $_ })
        $lines.Count | Should -BeGreaterThan 0
        foreach ($line in $lines) {
            $line | Should -BeOfType ([string])
            $line.Contains([char]27) | Should -BeFalse
        }
        ($lines -join "`n").Trim() | Should -Be "1: 1`n2: 2"
    }

    It 'rejects ambiguous wildcard selections' {
        { [pscustomobject]@{ CPU = 1; Count = 2 } | Show-Console -Property 'C*' -AsString -ErrorAction Stop -View Chart } | Should -Throw
    }

    It 'writes redirected output as readable text without ANSI sequences' {
        $path = Join-Path $TestDrive 'chart.txt'
        1, -2 | Show-Console -View Chart | Out-File -LiteralPath $path -Encoding utf8
        $text = (Get-Content -LiteralPath $path -Raw).Replace("`r`n", "`n").Trim()
        $text | Should -Be "1: 1`n2: -2"
        $text.Contains([char]27) | Should -BeFalse
    }

    It 'rejects invalid histogram bins and incompatible output choices' {
        { 1 | Show-Console -Bins 0 -ErrorAction Stop -View Chart } | Should -Throw
        { 1 | Show-Console -Bins 129 -ErrorAction Stop -View Chart } | Should -Throw
        { 1 | Show-Console -AsString -AsWidget -ErrorAction Stop -View Chart } | Should -Throw
    }

    It 'names the numeric properties once when objects have no Value' {
        $servers = @([pscustomobject] @{ Name = 'web01'; Cpu = 42; Uptime = 3 }, [pscustomobject] @{ Name = 'db01'; Cpu = 97; Uptime = 1 })
        $errors = @()
        { $servers | Show-Console -View Chart -ErrorVariable errors -ErrorAction SilentlyContinue } | Should -Throw '*-Property Cpu*numeric properties: Cpu, Uptime*'
        $errors.Where({ $_.FullyQualifiedErrorId -like 'ConsoleMonsterChartValue*' }).Count | Should -Be 0
        ($servers | Show-Console -View Chart -Property Cpu -AsWidget).Samples.Count | Should -Be 2
    }

    It 'charts the first reading of a Value script property exactly once' {
        $script:chartValueReads = 0
        $item = [pscustomobject] @{ Name = 'sample' }
        $item | Add-Member ScriptProperty Value { $script:chartValueReads++; $script:chartValueReads }
        $chart = $item | Show-Console -View Chart -AsWidget
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 1
        $script:chartValueReads | Should -Be 1
    }

    It 'offers only the chart kinds a single property can draw' {
        (Get-Command Show-Console).Parameters['Type'].Attributes.Where({ $_ -is [ValidateSet] }).ValidValues | Should -Be @('Bar', 'Histogram', 'Sparkline')
        { 1, 2 | Show-Console -View Chart -Type Line } | Should -Throw '*Line*does not belong to the set*'
    }

    It 'builds charts from ordinary objects with -Property and -Label' {
        $servers = @([pscustomobject] @{ Host = 'web01'; Cpu = 42 }, [pscustomobject] @{ Host = 'db01'; Cpu = 97 })
        $bar = $servers | New-ConsoleChart -Type Bar -Property Cpu -Label Host -Unit '%'
        @($bar.Items.Label) | Should -Be @('web01', 'db01')
        $text = New-ConsolePanel -Title Report -Content { $servers | New-ConsoleChart -Type Bar -Property Cpu -Label Host } | Show-Console -AsString -Width 60
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
}
