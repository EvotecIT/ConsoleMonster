Describe 'In-app command actions' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        function Invoke-ActionGrid {
            param($Command, [string] $Keys)
            $profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed(100, 30)
            $capture = [ConsoleMonster.Interactive.ScreenCapture]::new(100, 30)
            $inputSource = [ConsoleMonster.Input.ScriptedInput]::new($Keys)
            $inputSource.Paced = $true
            $inputSource.RealTime = $true
            $previous = [ConsoleMonster.Interactive.InteractiveOptions]::Default
            $options = $previous.Clone()
            $options.Mode = 'Rich'
            $options.Profile = $profile
            $options.Input = $inputSource
            $options.Capture = $capture
            try {
                [ConsoleMonster.Interactive.InteractiveOptions]::Default = $options
                $rows = @([pscustomobject] @{ Name = 'One' }, [pscustomobject] @{ Name = 'Two' })
                $null = $rows | Show-ConsoleGrid -Property Name -Title Services -Action $Command -ErrorAction Stop
                return ($capture.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n"
            } finally { [ConsoleMonster.Interactive.InteractiveOptions]::Default = $previous; $inputSource.Dispose() }
        }
    }

    It 'runs explicit arguments and modules in isolation for marked rows and keeps the grid open' {
        $CallerOnly = 'must not cross runspaces'
        $command = New-ConsoleCommand -Key Check -Label Check -Shortcut F5 -ConfirmPrompt 'Check services?' `
            -ArgumentList 'Lab' -ImportModule Microsoft.PowerShell.Utility -ScriptBlock {
                param($Row, $Environment)
                "Row=$($Row.Name);Argument=$Environment;Caller=$CallerOnly"
                Write-Warning 'Streamed warning'
            }
        $text = Invoke-ActionGrid $command 'Space Down Space F5 Left Enter wait:4000 Escape'
        $text | Should -Match 'Row=One;Argument=Lab;Caller='
        $text | Should -Match 'Row=Two;Argument=Lab;Caller='
        $text | Should -Not -Match 'must not cross runspaces'
        $text | Should -Match 'Streamed warning'
        $text | Should -Match 'Check: completed'
        $text | Should -Match 'Selected row'
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
    }

    It 'confirms hashtable shorthand before running the selected row through the palette' {
        $text = Invoke-ActionGrid @{ Inspect = { param($Row) "Inspected $($Row.Name)" } } 'Ctrl+K Enter Left Enter wait:4000 Escape'
        $text | Should -Match 'Inspect the selected rows\?'
        $text | Should -Match 'Inspected One'
        $text | Should -Match 'Inspect: completed'
    }

    It 'cancels an isolated action while retaining its streamed output' {
        $command = New-ConsoleCommand -Key Wait -Label Wait -Shortcut F5 -ScriptBlock {
            param($Row)
            'Operation started'
            Start-Sleep -Seconds 60
        }
        $text = Invoke-ActionGrid $command 'F5 wait:2500 F6 wait:1000 Escape'
        $text | Should -Match 'Operation started'
        $text | Should -Match 'Wait: cancelled'
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
    }
}
