Describe 'ConsoleMonster test helpers, progress and inline sessions' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        Import-Module $script:ModulePath -Force
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # Runs a script in a child process with redirected output (a stream terminal), with detection inputs cleared.
        function Invoke-ConsoleChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'Assert-ConsoleOutput creates a golden file, then passes and fails with a readable diff' {
        $golden = Join-Path $TestDrive 'panel.txt'
        { New-ConsolePanel -Title 'Box' -Content { New-ConsoleText 'hello' } | Assert-ConsoleOutput -GoldenPath $golden -Width 20 } | Should -Throw '*was created*'
        Get-Content -LiteralPath $golden -Raw | Should -Match 'hello'
        New-ConsolePanel -Title 'Box' -Content { New-ConsoleText 'hello' } | Assert-ConsoleOutput -GoldenPath $golden -Width 20
        $failure = { New-ConsolePanel -Title 'Box' -Content { New-ConsoleText 'hallo' } | Assert-ConsoleOutput -GoldenPath $golden -Width 20 } | Should -Throw -PassThru
        $failure.Exception.Message | Should -Match 'at line 2'
        $failure.Exception.Message | Should -Match 'CONSOLEMONSTER_UPDATE_GOLDEN=1'
        $text = New-ConsolePanel -Title 'Box' -Content { New-ConsoleText 'hello' } | Assert-ConsoleOutput -GoldenPath $golden -Width 20 -Glyphs Full -PassThru
        $text | Should -Match '╭─ Box'
    }

    It 'Show-ConsoleTable -AsWidget keeps -Culture' {
        $text = [pscustomobject] @{ Name = 'A'; Size = 1234.5 } , [pscustomobject] @{ Name = 'B'; Size = 2.25 } |
            Show-ConsoleTable -AsWidget -Culture ([cultureinfo] 'pl-PL') | Export-ConsoleFrame -Format Text -Width 40
        $text | Should -Match '1.234,5'
    }

    It 'shows Write-Progress from a dashboard runspace as an activity (stream lines when redirected)' {
        $text = Invoke-ConsoleChild -Script @'
$d = New-ConsoleDashboard -Title 'Copy' -Content { New-ConsoleLogRegion }
Invoke-ConsoleDashboard $d -Inline {
    Write-Progress -Activity 'Copying' -Status 'file 5 of 10' -PercentComplete 50
    Write-Progress -Activity 'Copying' -Completed
}
'DASHBOARD-DONE'
'@
        $text | Should -Match 'Copying 50% file 5 of 10'
        $text | Should -Match 'DASHBOARD-DONE'
    }

    It 'Use-ConsoleProgress runs the script with the caller''s variables and returns its output' {
        $text = Invoke-ConsoleChild -Script @'
$items = 1..3
$ConsoleSession = 'outer-session'
$result = Use-ConsoleProgress {
    if ($null -ne $ConsoleSession) { throw 'The redirected fallback must not borrow the outer session.' }
    foreach ($i in $items) { Write-Progress -Activity 'Work' -PercentComplete ($i * 33) }
    "processed $($items.Count)"
}
"RESULT:$result"
"RESTORED:$ConsoleSession"
'@
        $text | Should -Match 'RESULT:processed 3'
        $text | Should -Match 'RESTORED:outer-session'
    }

    It 'Use-ConsoleProgress exposes its own session for recorded waits and restores the caller scope' {
        $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal'))::Target(40, 12)
        $target.SkipDelays = $true
        $ConsoleSession = 'outer-session'
        Use-ConsoleTarget -Target $target {
            Use-ConsoleProgress {
                $ConsoleSession | Should -BeOfType ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveSession'))
                Write-Progress -Activity 'Work' -PercentComplete 50
                Wait-ConsoleDashboard -Seconds 0.11 -ErrorAction Stop
            }
        }
        $target.CheckpointCount | Should -Be 1
        $ConsoleSession | Should -BeExactly 'outer-session'
    }
}
