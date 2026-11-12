Describe 'In-app command actions' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        function Invoke-ActionGrid {
            param($Command, [scriptblock] $Script, [scriptblock] $Refresh)
            $profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(100, 30)
            $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(100, 30)
            $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('')
            $inputSource.Paced = $true
            $previous = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default
            $options = $previous.Clone()
            $options.Mode = 'Rich'
            $options.Profile = $profile
            $options.Input = $inputSource
            $options.Capture = $capture
            $driver = $null
            try {
                $driver = & $Script $inputSource $capture
                if ($null -ne $driver) { $options.Input = $driver }
                ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $options
                $rows = @([pscustomobject] @{ Name = 'One' }, [pscustomobject] @{ Name = 'Two' })
                $null = $rows | Show-ConsoleGrid -Property Name -Title Services -Command $Command -RefreshRow $Refresh -ErrorAction Stop
                return ($capture.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n"
            } finally {
                ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $previous
                if ($null -ne $driver) { $driver.Dispose() }
                $inputSource.Dispose()
            }
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
        $text = $scroll | Export-ConsoleFrame -Format Text -Width 40
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
        $text = Invoke-ActionGrid $command {
            param($inputSource, $capture)
            $inputSource.Append('Space Down Space F5')
            $inputSource.WaitForOutput($capture, 'Check services?', [timespan]::FromSeconds(30))
            $inputSource.Append('Left Enter')
            $inputSource.WaitForOutput($capture, 'Check: completed', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('Escape')
        }
        $text | Should -Match 'Row=One;Argument=Lab;Caller='
        $text | Should -Match 'Row=Two;Argument=Lab;Caller='
        $text | Should -Not -Match 'must not cross runspaces'
        $text | Should -Match 'Streamed warning'
        $text | Should -Match 'Check: completed'
        $text | Should -Match 'Selected row'
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

    It 'confirms hashtable shorthand before running the selected row through the palette' {
        $text = Invoke-ActionGrid @{ Inspect = { param($Row) "Inspected $($Row.Name)" } } {
            param($inputSource, $capture)
            $inputSource.Append('Ctrl+K')
            $inputSource.WaitForOutput($capture, 'Inspect', [timespan]::FromSeconds(30))
            $inputSource.Append('Enter')
            $inputSource.WaitForOutput($capture, 'Inspect the selected rows?', [timespan]::FromSeconds(30))
            $inputSource.Append('Left Enter')
            $inputSource.WaitForOutput($capture, 'Inspect: completed', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('Escape')
        }
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
        $text = Invoke-ActionGrid $command {
            param($inputSource, $capture)
            $inputSource.Append('F5')
            $inputSource.WaitForOutput($capture, 'Operation started', [timespan]::FromSeconds(30))
            $inputSource.Append('F6')
            $inputSource.WaitForOutput($capture, 'Wait: cancelled', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('Escape')
        }
        $text | Should -Match 'Operation started'
        $text | Should -Match 'Wait: cancelled'
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

    It 'bounds cancellation of a native call and prevents another runspace for that command until it exits' {
        $script:NativeInputType = [AppDomain]::CurrentDomain.GetAssemblies() |
            ForEach-Object { $_.GetType('NativeActionInput', $false) } | Select-Object -First 1
        if ($null -eq $script:NativeInputType) {
            $references = @([ConsoleMonster.Terminal].Assembly.Location, [powershell].Assembly.Location)
            $compile = @{}
            if ($PSEdition -eq 'Core') {
                $references += @(Get-ChildItem (Join-Path $PSHOME 'ref') -Filter *.dll | ForEach-Object FullName)
                $compile.CompilerOptions = '/nowarn:1701'
            } else { $references += 'System.Core' }
            $assemblyPath = Join-Path $TestDrive 'NativeActionInput.dll'
            Add-Type -Path (Join-Path $PSScriptRoot 'Support/NativeActionInput.cs') -ReferencedAssemblies $references -OutputAssembly $assemblyPath -ErrorAction Stop @compile
            $bytes = [IO.File]::ReadAllBytes($assemblyPath)
            if ($PSEdition -eq 'Core') {
                $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([ConsoleMonster.Terminal].Assembly)
                $stream = [IO.MemoryStream]::new($bytes)
                try { $assembly = $context.LoadFromStream($stream) } finally { $stream.Dispose() }
            } else { $assembly = [Reflection.Assembly]::Load($bytes) }
            $script:NativeInputType = $assembly.GetType('NativeActionInput', $true)
        }
        $gateId = [guid]::NewGuid().ToString('N')
        $command = New-ConsoleCommand -Key Native -Shortcut F5 -ArgumentList $gateId, $script:NativeInputType.Assembly.FullName -ScriptBlock {
            param($Row, $GateId, $FixtureAssembly)
            if ($Row.Name -eq 'One') {
                'Native operation started'
                $assembly = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object FullName -EQ $FixtureAssembly | Select-Object -First 1
                ($assembly.GetType('NativeActionInput', $true))::Hold($GateId)
                'Late native output must stay suppressed'
            } else { 'Second row completed' }
        }
        $script:NativeDriver = $null
        $text = Invoke-ActionGrid $command {
            param($inputSource, $capture)
            $inputSource.Append('F5')
            $inputSource.WaitForOutput($capture, 'Native operation started', [timespan]::FromSeconds(30))
            $inputSource.Append('F6')
            $inputSource.WaitForOutput($capture, 'native operation is still stopping', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Native: cancelled', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('F5')
            $inputSource.WaitForOutput($capture, 'previous invocation of this command is still stopping', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('Down')
            $inputSource.WaitForOutput($capture, 'Second row completed', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Native: completed', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'Action output', [timespan]::FromSeconds(30))
            $inputSource.Append('Escape')
            $script:NativeDriver = $script:NativeInputType::new($gateId, $inputSource, $capture)
            $script:NativeDriver
        }
        $script:NativeDriver.HeldThroughRejectedRestart | Should -BeTrue
        $script:NativeDriver.RunspaceClosed | Should -BeTrue
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
        $text = Invoke-ActionGrid $command {
            param($inputSource, $capture)
            $inputSource.Append('F5')
            $inputSource.WaitForOutput($capture, 'Restart: completed', [timespan]::FromSeconds(30))
            $inputSource.WaitForOutput($capture, 'One-refreshed', [timespan]::FromSeconds(30))
            $inputSource.Append('Escape')
        } {
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
