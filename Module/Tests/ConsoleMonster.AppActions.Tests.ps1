Describe 'In-app command actions' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        function Invoke-ActionGrid {
            param($Command, [string] $Keys, [scriptblock] $Refresh)
            $profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(100, 30)
            $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(100, 30)
            $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new($Keys)
            $inputSource.Paced = $true
            $inputSource.RealTime = $true
            $previous = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default
            $options = $previous.Clone()
            $options.Mode = 'Rich'
            $options.Profile = $profile
            $options.Input = $inputSource
            $options.Capture = $capture
            try {
                ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $options
                $rows = @([pscustomobject] @{ Name = 'One' }, [pscustomobject] @{ Name = 'Two' })
                $null = $rows | Show-ConsoleGrid -Property Name -Title Services -Command $Command -RefreshRow $Refresh -ErrorAction Stop
                return ($capture.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n"
            } finally { ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $previous; $inputSource.Dispose() }
        }
    }

    It 'treats an explicit null module list as no imports' {
        $command = New-ConsoleCommand -Key NoImports -ImportModule $null -ScriptBlock { 'Done' }
        $command.Key | Should -Be NoImports
    }

    It 'preserves a command label on a button and accepts an explicit label override' {
        $command = New-ConsoleCommand -Key restart -Label 'Restart service' -ScriptBlock { 'Done' }
        $button = New-ConsoleButton -Key action -Command $command
        $button.Label | Should -BeExactly 'Restart service'
        $button.Command | Should -Be $command
        (New-ConsoleButton -Key action -Command $command -Label 'Restart now').Label | Should -BeExactly 'Restart now'
        { New-ConsoleButton -Key action -Command $command -OnClick { 'Duplicate' } } | Should -Throw '*not both*'
    }

    It 'collects pipeline content into one scroll container' {
        $scroll = @('First', 'Second') | New-ConsoleScrollView -Height 5
        @($scroll).Count | Should -Be 1
        $text = $scroll | Show-Console -AsString -Width 40
        $text | Should -Match 'First'
        $text | Should -Match 'Second'
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
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
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
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

    It 'bounds cancellation of a native call and prevents another runspace for that command until it exits' {
        $command = New-ConsoleCommand -Key Native -Shortcut F5 -ScriptBlock {
            param($Row)
            if ($Row.Name -eq 'One') {
                'Native operation started'
                [Threading.Thread]::Sleep(8000)
                'Late native output must stay suppressed'
            } else { 'Second row completed' }
        }
        $text = Invoke-ActionGrid $command 'F5 wait:2500 F6 wait:2500 F5 wait:4500 Down F5 wait:1000 Escape'
        $text | Should -Match 'Native operation started'
        $text | Should -Match 'native operation is still stopping'
        $text | Should -Match 'Native: cancelled'
        $text | Should -Match 'previous invocation of this command is still stopping'
        $text | Should -Match 'Second row completed'
        $text | Should -Match 'Native: completed'
        $text | Should -Not -Match 'Late native output must stay suppressed'
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

    It 'refreshes the acted-on row on the caller thread after successful isolated work' {
        $script:RefreshCalls = 0
        $script:RefreshThread = 0
        $script:RefreshKey = ''
        $thread = [Threading.Thread]::CurrentThread.ManagedThreadId
        $command = New-ConsoleCommand -Key Restart -Shortcut F5 -ScriptBlock { param($Row) "Restarted $($Row.Name)" }
        $text = Invoke-ActionGrid $command 'F5 wait:4000 Escape' {
            param($Row, $CommandKey)
            $script:RefreshCalls++
            $script:RefreshThread = [Threading.Thread]::CurrentThread.ManagedThreadId
            $script:RefreshKey = $CommandKey
            $Row.Name = $Row.Name + '-refreshed'
            $Row
        }
        $text | Should -Match 'Restarted One'
        $text | Should -Match 'One-refreshed'
        $script:RefreshCalls | Should -Be 1
        $script:RefreshThread | Should -Be $thread
        $script:RefreshKey | Should -Be Restart
    }
}
