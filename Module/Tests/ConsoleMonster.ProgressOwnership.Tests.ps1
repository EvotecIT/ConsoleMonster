Describe 'Runspace ownership of progress and the shared terminal area' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:ProbePath = Join-Path $PSScriptRoot 'Support/Invoke-ProgressOwnership.ps1'
    }

    It 'keeps overlapping activity IDs isolated while another runspace completes, disables and closes' {
        $scriptText = "& '$($script:ProbePath.Replace("'", "''"))' -ModulePath '$($script:ModulePath.Replace("'", "''"))'"
        $text = (Invoke-TestChild -Runtime $script:Runtime -Script $scriptText) -join "`n"
        $LASTEXITCODE | Should -Be 0
        $text | Should -Match 'FIRST_HANDLED=True'
        $text | Should -Match 'SECOND_HANDLED=False'
        $text | Should -Match 'SECOND_NATIVE=True'
        $text | Should -Match 'FIRST_AFTER_SECOND=1'
        $text | Should -Match 'FIRST_UPDATED=True'
        $text | Should -Match 'FIRST_LABEL=True'
        $text | Should -Match 'FIRST_COMPLETED=0'
        $text | Should -Match 'DISABLED_HANDLED=False DISABLED_ACTIVITIES=0'
        $text | Should -Match 'SECOND_AFTER_RELEASE=True SECOND_ACTIVITIES=1'
        $text | Should -Match 'SECOND_SURVIVED=1 SECOND_LABEL=True'
        $text | Should -Match 'NATIVE_STARTED=False'
        $text | Should -Match 'NATIVE_UPDATE_AFTER_RELEASE=False NATIVE_COMPLETION_AFTER_RELEASE=False NATIVE_CUSTOM_ACTIVITIES=0'
        $text | Should -Match 'AFTER_OWNER_CLOSED=True'
        $text | Should -Match 'SCOPE_BLOCKED=True'
        $text | Should -Match 'SCOPE_AFTER_CLOSE=True'
        $text | Should -Match 'SUCCESSOR_AFTER_OLD_SCOPE=True'
        $text | Should -Match 'STALE_SCOPE_KEPT_SUCCESSOR=True'
    }

    It 'reserves the terminal through Closing until the owner has finished shutting down' {
        $scriptText = "& '$($script:ProbePath.Replace("'", "''"))' -ModulePath '$($script:ModulePath.Replace("'", "''"))' -ClosingHandoff"
        $text = (Invoke-TestChild -Runtime $script:Runtime -Script $scriptText) -join "`n"
        $LASTEXITCODE | Should -Be 0
        $text | Should -Match 'OWNER_RESERVED=True'
        $text | Should -Match 'CLOSING_OWNER_BLOCKED=True'
        $text | Should -Match 'CLOSED_OWNER_RELEASED=True'
    }

    It 'bounds standalone fallback to its pipeline without requiring the formatting shim' {
        $scriptText = "& '$($script:ProbePath.Replace("'", "''"))' -ModulePath '$($script:ModulePath.Replace("'", "''"))' -StandaloneHandoff"
        $text = (Invoke-TestChild -Runtime $script:Runtime -Script $scriptText) -join "`n"
        $LASTEXITCODE | Should -Be 0
        $text | Should -Match 'STANDALONE_BLOCKED=True'
        $text | Should -Match 'STANDALONE_NEXT_PIPELINE=True'
        $text | Should -Match 'DISABLED_REPORT=False'
        $text | Should -Match 'STANDALONE_AFTER_DISABLED=True'
    }
}
