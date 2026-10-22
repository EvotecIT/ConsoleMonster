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

    It 'binds object values and labels then sorts before taking Top' {
        $items = @(
            [pscustomobject]@{ Name = 'a'; CPU = 1 }
            [pscustomobject]@{ Name = 'b'; CPU = 3 }
            [pscustomobject]@{ Name = 'c'; CPU = 2 }
        )
        $text = $items | Out-ConsoleChart -Property CPU -Label Name -Sort Descending -Top 2 -AsString
        $text.Trim() | Should -Be "b: 3`nc: 2"
    }

    It 'uses shared dictionary binding and calculated properties' {
        $chart = @{ Host = '[red]api'; Milliseconds = 250 } | Out-ConsoleChart -Property @{
            Name = 'Seconds'
            Expression = { $_.Milliseconds / 1000 }
        } -Label Host -AsWidget
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 0.25
        $chart.Samples[0].Label | Should -Be '[red]api'
    }

    It 'accepts numbers directly with deterministic row labels' {
        $text = 2, -1, 0 | Out-ConsoleChart -AsString
        $text.Trim() | Should -Be "1: 2`n2: -1`n3: 0"
    }

    It 'keeps group appearance and sample order in trends' {
        $items = @(
            [pscustomobject]@{ Host = 'b'; Value = 3 }
            [pscustomobject]@{ Host = 'a'; Value = 2 }
            [pscustomobject]@{ Host = 'b'; Value = 1 }
        )
        $text = $items | Out-ConsoleChart -Type Sparkline -Label Host -AsString
        $text.Trim() | Should -Be "b: 3, 1`na: 2"
    }

    It 'reports and skips malformed missing and nonfinite values without fabricating zeros' {
        $errors = @()
        $items = @(
            [pscustomobject]@{ Value = 'bad' }
            [pscustomobject]@{ Other = 9 }
            [pscustomobject]@{ Value = [double]::NaN }
            [pscustomobject]@{ Value = [double]::PositiveInfinity }
            [pscustomobject]@{ Value = $true }
            [pscustomobject]@{ Value = 4 }
        )
        $chart = $items | Out-ConsoleChart -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors
        $errors.Count | Should -Be 5
        $chart.Samples.Count | Should -Be 1
        $chart.Samples[0].Value | Should -Be 4
    }

    It 'treats an unreadable numeric getter as invalid data' {
        $item = [pscustomobject]@{ Name = 'broken' }
        $item | Add-Member -MemberType ScriptProperty -Name CPU -Value { throw 'unreadable' }
        $errors = @()
        $result = @($item | Out-ConsoleChart -Property CPU -AsWidget -ErrorAction SilentlyContinue -ErrorVariable errors)
        $errors.Count | Should -BeGreaterThan 0
        $result.Count | Should -Be 0
    }

    It 'produces no output for an empty pipeline' {
        @(@() | Out-ConsoleChart -AsString).Count | Should -Be 0
    }

    It 'counts every histogram observation including the last edge' {
        $text = -2, -1, 0, 1, 2 | Out-ConsoleChart -Type Histogram -Bins 4 -Top 1 -AsString
        $text.Trim() | Should -Be "[-2, -1): 1`n[-1, 0): 1`n[0, 1): 1`n[1, 2]: 2"
    }

    It 'emits plain strings to a downstream command through PSFormatting' {
        $lines = @(1, 2 | Out-ConsoleChart | ForEach-Object { $_ })
        $lines.Count | Should -BeGreaterThan 0
        foreach ($line in $lines) {
            $line | Should -BeOfType ([string])
            $line.Contains([char]27) | Should -BeFalse
        }
        ($lines -join "`n").Trim() | Should -Be "1: 1`n2: 2"
    }

    It 'rejects ambiguous wildcard selections' {
        { [pscustomobject]@{ CPU = 1; Count = 2 } | Out-ConsoleChart -Property 'C*' -AsString -ErrorAction Stop } | Should -Throw
    }

    It 'writes redirected output as readable text without ANSI sequences' {
        $path = Join-Path $TestDrive 'chart.txt'
        1, -2 | Out-ConsoleChart | Out-File -LiteralPath $path -Encoding utf8
        $text = (Get-Content -LiteralPath $path -Raw).Replace("`r`n", "`n").Trim()
        $text | Should -Be "1: 1`n2: -2"
        $text.Contains([char]27) | Should -BeFalse
    }

    It 'rejects invalid histogram bins and incompatible output choices' {
        { 1 | Out-ConsoleChart -Bins 0 -ErrorAction Stop } | Should -Throw
        { 1 | Out-ConsoleChart -Bins 129 -ErrorAction Stop } | Should -Throw
        { 1 | Out-ConsoleChart -AsString -AsWidget -ErrorAction Stop } | Should -Throw
    }
}
