Describe 'Native console applications' {
    BeforeAll {
        $script:AppModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        Import-Module $script:AppModulePath -Force
    }

    It 'reports invalid native state keys with the application diagnostic and releases its input' {
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"x" Escape')
        {
            Show-ConsoleApp -Content {
                New-ConsoleTextInput -Key Query -OnChange { $State[17] = 'invalid' }
            } -InputSource $inputSource -Capture $capture -ErrorAction Stop
        } | Should -Throw -ExpectedMessage '*Application state keys must be strings*'
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

    It 'calls back once per committed edit before the next read and returns one ordinary hashtable' {
        $edits = [System.Collections.Generic.List[object]]::new()
        $thread = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"ab" Left Right Backspace "c" Escape')
        $output = @(Show-ConsoleApp -Content {
            New-ConsoleTextInput -Key Query -OnChange {
                param($Value, $State)
                $edits.Add([pscustomobject]@{
                    Value = $Value
                    Current = $State.Query
                    Control = $Control.Key
                    NativeState = $State -is [hashtable]
                    Thread = [System.Threading.Thread]::CurrentThread.ManagedThreadId
                })
                $State.Edits = 1 + $State.Edits
                'callback output must stay local'
            }
        } -InputSource $inputSource -Capture $capture -NoMouse -PassThru)

        $output.Count | Should -Be 1
        $output[0].GetType() | Should -Be ([hashtable])
        $output[0].Query | Should -Be 'ac'
        $output[0].Edits | Should -Be 4
        ($edits.Value -join ',') | Should -Be 'a,ab,a,ac'
        ($edits.Current -join ',') | Should -Be 'a,ab,a,ac'
        @($edits | Where-Object { $_.Thread -ne $thread -or -not $_.NativeState -or $_.Control -ne 'Query' }).Count | Should -Be 0
    }

    It 'rebuilds canonical tables and general widget bindings from native state on the caller thread' {
        $items = @(
            [pscustomobject]@{ Name = 'alpha'; Status = 'pass' }
            [pscustomobject]@{ Name = 'beta'; Status = 'fail' }
        )
        $seen = [System.Collections.Generic.List[string]]::new()
        $thread = [System.Threading.Thread]::CurrentThread.ManagedThreadId
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"bet" Escape')
        $result = Show-ConsoleApp -Content {
            New-ConsoleTextInput -Key Query
            New-ConsoleTable -Bind {
                param($State)
                if ($State -isnot [hashtable]) { throw 'Expected native state' }
                if ([System.Threading.Thread]::CurrentThread.ManagedThreadId -ne $thread) { throw 'Wrong binding thread' }
                $seen.Add($State.Query)
                $items | Where-Object Name -Like "*$($State.Query)*"
            } -Property Name, Status -StatusProperty Status -Title Matches -Caption Filtered -Border Minimal -Expand -NoHeaderSeparator
            New-ConsoleBinding -WidgetId Summary -Content {
                param($State)
                "Current query: $($State.Query)"
                'Second widget'
            }
        } -InputSource $inputSource -Capture $capture -PassThru

        $result.Query | Should -Be 'bet'
        $seen[0] | Should -Be ''
        $seen[$seen.Count - 1] | Should -Be 'bet'
        $text = $capture.LastFrame.ToPlainText()
        $text | Should -Match 'Matches'
        $text | Should -Match 'Filtered'
        $text | Should -Match 'beta'
        $text | Should -Not -Match 'alpha'
        $text | Should -Match 'Current query: bet'
        $text | Should -Match 'Second widget'
    }

    It 'dispatches toggle text-area and selection edits through the shared OnChange contract' {
        $edits = [System.Collections.Generic.List[object]]::new()
        $changed = {
            param($Value, $State)
            $edits.Add([pscustomobject]@{ Name = $Control.Key; Value = $Value })
            $State.LastEdited = $Control.Key
        }
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 40)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Space Tab "note" Tab Down Enter Tab Space Escape')
        $result = Show-ConsoleApp -Content {
            New-ConsoleToggle -Key Enabled -OnChange $changed
            New-ConsoleTextInput -Key Notes -Height 3 -OnChange $changed -Multiline
            New-ConsoleSelect -Key Site -Choices 'Warsaw', 'Berlin' -OnChange $changed
            New-ConsoleSelect -Key Sites -Choices 'Warsaw', 'Berlin' -OnChange $changed -Multiple
        } -InputSource $inputSource -Capture $capture -PassThru

        $result.Enabled | Should -BeTrue
        $result.Notes | Should -Be 'note'
        $result.Site | Should -Be 'Berlin'
        ($result.Sites -join ',') | Should -Be 'Warsaw'
        $result.LastEdited | Should -Be 'Sites'
        @($edits | Where-Object Name -EQ Enabled).Count | Should -Be 1
        @($edits | Where-Object Name -EQ Notes).Count | Should -Be 4
        @($edits | Where-Object Name -EQ Site).Count | Should -Be 1
        @($edits | Where-Object Name -EQ Sites).Count | Should -Be 1
    }

    It 'captures initialized controls and bound objects without consuming input in static mode' {
        $choice = [pscustomobject]@{ Name = 'Warsaw'; Id = 12 }
        $other = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.Choice'))::new('Berlin', 'Berlin')
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(100, 50)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"untouched" Escape')
        $remaining = $inputSource.Remaining
        $calls = [System.Collections.Generic.List[string]]::new()
        $result = Show-ConsoleApp -Content {
            New-ConsoleTextInput -Key Query -Label Filter -Value seed -Placeholder Search -MaxLength 20 -OnChange { $calls.Add('change') }
            New-ConsoleTextInput -Key Notes -Value 'Draft' -Height 3 -MaxLength 80 -Placeholder Notes -Multiline
            New-ConsoleToggle -Key Enabled -Value $true -Disabled -TabIndex 2
            New-ConsoleSelect -Key Site -Choices $choice, $other -Display Name -Value $choice
            New-ConsoleSelect -Key Sites -Choices 'Warsaw', 'Berlin' -Value Warsaw -Multiple
            New-ConsoleButton -Key Apply -OnClick { $calls.Add('click') }
            New-ConsoleTable -Bind { param($State) [pscustomobject]@{ Query = $State.Query } } -Property Query
        } -Mode Snapshot -InputSource $inputSource -Capture $capture -PassThru

        $result.GetType() | Should -Be ([hashtable])
        $result.Count | Should -Be 6
        $result.Query | Should -Be 'seed'
        $result.Notes | Should -Be 'Draft'
        $result.Enabled | Should -BeTrue
        [object]::ReferenceEquals($result.Site, $choice) | Should -BeTrue
        ($result.Sites -join ',') | Should -Be 'Warsaw'
        $result.Apply | Should -Be 0
        $calls.Count | Should -Be 0
        $inputSource.Remaining | Should -Be $remaining
        $capture.LastFrame.ToPlainText() | Should -Match 'seed'
    }

    It 'dispatches external controls only while attached and isolates repeated runs' {
        $changes = [System.Collections.Generic.List[string]]::new()
        $clicks = [System.Collections.Generic.List[int]]::new()
        $query = New-ConsoleTextInput -Key Query -OnChange { $changes.Add($Value); $State.Edits = 1 + $State.Edits }
        $button = New-ConsoleButton -Key Apply -OnClick {
            param($Value, $State)
            $clicks.Add($Value)
            $State.Applied = $State.Query
            'discard click output'
        }
        $firstCapture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $firstInput = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"a" Tab Enter Escape')
        $first = @(Show-ConsoleApp -Content { $query; $button } -InputSource $firstInput -Capture $firstCapture -PassThru)
        $first.Count | Should -Be 1
        $first[0].Edits | Should -Be 1
        $first[0].Applied | Should -Be 'a'
        $first[0].Apply | Should -Be 1
        $query.Value = ''
        $changes.Count | Should -Be 1

        $secondCapture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $secondInput = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"b" Tab Enter Escape')
        $second = Show-ConsoleApp -Content { $query; $button } -InputSource $secondInput -Capture $secondCapture -PassThru
        $second.Edits | Should -Be 1
        $second.Applied | Should -Be 'b'
        $second.Apply | Should -Be 2
        ($changes -join ',') | Should -Be 'a,b'
        ($clicks -join ',') | Should -Be '1,2'
        $first[0].Query | Should -Be 'a'
    }

    It 'detaches subscriptions after a callback fails and allows a fresh app run' {
        $calls = [System.Collections.Generic.List[string]]::new()
        $query = New-ConsoleTextInput -Key Query -OnChange { $calls.Add($Value); throw 'callback failed' }
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"a" Escape')
        { Show-ConsoleApp -Content { $query } -InputSource $inputSource -Capture $capture -ErrorAction Stop } | Should -Throw '*callback failed*'
        $query.Value = 'outside'
        $calls.Count | Should -Be 1
        $freshCapture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $fresh = Show-ConsoleApp -Content { New-ConsoleTextInput -Key Other -Value clean } -Mode Snapshot -Capture $freshCapture -PassThru
        $fresh.Count | Should -Be 1
        $fresh.Other | Should -Be 'clean'
    }

    It 'emits no success output without PassThru and rejects inputs inside a dependent binding' {
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        @(Show-ConsoleApp -Content { New-ConsoleTextInput -Key Query } -Mode Snapshot -Capture $capture).Count | Should -Be 0
        { Show-ConsoleApp -Content { New-ConsoleBinding -Content { New-ConsoleToggle -Key Nested } } -Mode Snapshot -Capture $capture -ErrorAction Stop } | Should -Throw
        { Show-ConsoleTable -Bind { [pscustomobject]@{ Name = 'host' } } -ErrorAction Stop } |
            Should -Throw '*parameter*Bind*'
    }

    It 'attaches controls and bindings created in an earlier pipeline of the same runspace' {
        $runspace = [runspacefactory]::CreateRunspace()
        $runspace.ThreadOptions = [System.Management.Automation.Runspaces.PSThreadOptions]::UseNewThread
        $runspace.Open()
        $shell = [powershell]::Create()
        $shell.Runspace = $runspace
        try {
            $setup = {
                param($ModulePath)
                Import-Module $ModulePath -Force
                $script:Query = New-ConsoleTextInput -Key Query -OnChange { $State.Edits = 1 + $State.Edits }
                $script:Summary = New-ConsoleBinding -Content { "Current: $($State.Query)" }
                $script:Table = New-ConsoleTable -Bind { [pscustomobject]@{ Query = $State.Query } } -Property Query
            }.ToString()
            $null = $shell.AddScript($setup).AddArgument($script:AppModulePath)
            $null = $shell.Invoke()
            $shell.HadErrors | Should -BeFalse
            $shell.Commands.Clear()
            $run = {
                $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
                $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"z" Escape')
                Show-ConsoleApp -Content { $script:Query; $script:Summary; $script:Table } -InputSource $inputSource -Capture $capture -PassThru
            }.ToString()
            $null = $shell.AddScript($run)
            $output = @($shell.Invoke())
            $shell.HadErrors | Should -BeFalse
            $output.Count | Should -Be 1
            $output[0].Query | Should -Be 'z'
            $output[0].Edits | Should -Be 1
        } finally {
            $shell.Dispose()
            $runspace.Dispose()
        }
    }

    It 'keeps separate runspaces free of callback or state registrations from earlier runs' {
        $scriptText = {
            param($ModulePath, $Text)
            Import-Module $ModulePath -Force
            $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
            $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new(('"{0}" Escape' -f $Text))
            Show-ConsoleApp -Content {
                New-ConsoleTextInput -Key Query -OnChange { $State.Edits = 1 + $State.Edits }
            } -InputSource $inputSource -Capture $capture -PassThru
        }.ToString()
        foreach ($text in 'a', 'xy') {
            $shell = [powershell]::Create()
            try {
                $null = $shell.AddScript($scriptText).AddArgument($script:AppModulePath).AddArgument($text)
                $output = @($shell.Invoke())
                $shell.HadErrors | Should -BeFalse
                $output.Count | Should -Be 1
                $output[0].Query | Should -Be $text
                $output[0].Edits | Should -Be $text.Length
            } finally {
                $shell.Dispose()
            }
        }
    }
}
