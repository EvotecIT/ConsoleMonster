Describe 'Show-ConsoleObject compiled command' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $script:SavedInteractive = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default
    }

    BeforeEach {
        $script:Capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 20)
        $options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::new()
        $options.Mode = 'Rich'
        $options.Capture = $script:Capture
        $options.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Down Enter F10')
        $options.Input.Paced = $true
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $options
    }

    AfterEach {
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $script:SavedInteractive
    }

    It 'returns the selected original PowerShell member value' {
        $value = [pscustomobject]@{ Name = 'web-01'; Owner = [pscustomobject]@{ Name = 'Ada' } }
        $selected = $value | Show-ConsoleObject -Mode Rich -PassThru
        $selected | Should -Be 'Ada'
        $script:Capture.LastFrame.ToPlainText() | Should -Match '\$x.Owner'
    }

    It 'labels drilled PowerShell records without changing their indexed path' {
        $services = @(
            [pscustomobject]@{ Name = 'web'; Status = 'Running' }
            [pscustomobject]@{ Name = 'metrics'; Status = 'Running' }
        )
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Enter F10')
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input.Paced = $true
        $x = [pscustomobject]@{ Services = $services }
        $path = $x | Show-ConsoleObject -Mode Rich -PassThru Path
        $path | Should -Be '$x.Services[0]'
        $script:Capture.LastFrame.ToPlainText() | Should -Match '\[0\]\s+web'
        $script:Capture.LastFrame.ToPlainText() | Should -Match '\[1\]\s+metrics'
        $script:Capture.LastFrame.ToPlainText() | Should -Match 'Name\s+Status'
        [object]::ReferenceEquals((& ([scriptblock]::Create($path))), $services[0]) | Should -BeTrue
    }

    It 'opens one rich explorer for pipeline items and returns their indexed path' {
        $x = @([pscustomobject]@{ Name = 'web' }, [pscustomobject]@{ Name = 'metrics' })
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('F10')
        $path = $x | Show-ConsoleObject -Mode Rich -PassThru Path
        $path | Should -Be '$x[0]'
        [object]::ReferenceEquals((& ([scriptblock]::Create($path))), $x[0]) | Should -BeTrue
        $script:Capture.LastFrame.ToPlainText() | Should -Match '\[0\]\s+web'
        $script:Capture.LastFrame.ToPlainText() | Should -Match '\[1\]\s+metrics'
    }

    It 'binds the bare PassThru switch followed by Path' {
        $value = [pscustomobject]@{ Name = 'web-01'; Owner = [pscustomobject]@{ Name = 'Ada' } }
        $selected = $value | Show-ConsoleObject -Mode Rich -RootPath '$config' -PassThru Path
        $selected | Should -Be '$config.Owner.Name'
    }

    It 'explores ConvertFrom-Json records with the same adapters' {
        $value = Get-Content (Join-Path $PSScriptRoot 'Fixtures/ObjectExplorer.json') -Raw | ConvertFrom-Json
        $selected = $value | Show-ConsoleObject -Mode Rich -PassThru
        $selected | Should -Be 'Ada'
    }

    It 'returns no selection while browsing or cancelling' {
        $value = [pscustomobject]@{ Name = 'web-01'; Owner = [pscustomobject]@{ Name = 'Ada' } }
        @($value | Show-ConsoleObject -Mode Rich).Count | Should -Be 0
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Escape')
        @($value | Show-ConsoleObject -Mode Rich -PassThru).Count | Should -Be 0
    }

    It 'prints a static inspector without prompting or returning a selected node' {
        $warned = @()
        $result = @([pscustomobject]@{ Name = 'web-01' } | Show-ConsoleObject -Mode None -PassThru -WarningVariable warned)
        $result.Count | Should -Be 0
        $warned.Count | Should -Be 0
    }

    It 'exports the command and documents all its parameters' {
        $command = Get-Command Show-ConsoleObject -Module ConsoleMonster
        $command.Parameters.ContainsKey('InputObject') | Should -BeTrue
        $command.Parameters.ContainsKey('PassThru') | Should -BeTrue
        $help = Get-Help Show-ConsoleObject -Full
        "$($help.Synopsis)" | Should -Match 'keyboard'
        @($help.parameters.parameter | Where-Object { $_.name -eq 'RootPath' }).Count | Should -Be 1
    }

    It 'does not read a wrapped pending Task result or force a wrapped Lazy value' {
        $completion = [System.Threading.Tasks.TaskCompletionSource[int]]::new()
        $lazy = [System.Lazy[string]]::new([System.Func[string]] { throw 'The factory must stay uncreated.' })
        try {
            ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('F10')
            $selected = Show-ConsoleObject -InputObject $completion.Task -Mode Rich -PassThru
            [object]::ReferenceEquals($selected, $completion.Task) | Should -BeTrue
            $completion.Task.IsCompleted | Should -BeFalse
            ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('F10')
            $null = Show-ConsoleObject -InputObject $lazy -Mode Rich -PassThru
            $lazy.IsValueCreated | Should -BeFalse
        } finally {
            $completion.SetResult(42)
        }
    }

    It 'returns distinct evaluable paths for integer and string dictionary keys' {
        $map = @{}
        $map.Add(1, 'integer')
        $map.Add('1', 'string')
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('F10')
        $first = $map | Show-ConsoleObject -RootPath '$map' -Mode Rich -PassThru Path
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Down F10')
        $second = $map | Show-ConsoleObject -RootPath '$map' -Mode Rich -PassThru Path
        $first | Should -Not -Be $second
        $values = @(& ([scriptblock]::Create($first)); & ([scriptblock]::Create($second)))
        $values | Should -Contain 'integer'
        $values | Should -Contain 'string'
    }

    It 'finds complex dictionary keys when Keys and intrinsic-looking names are shadowed' {
        $key = [object]::new()
        $map = @{ Keys = 'wrong'; get_Keys = 'wrong'; PSBase = 'wrong'; GetEnumerator = 'wrong' }
        $map.Add($key, 'target')
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('/ "target" Enter F10')
        $expression = $map | Show-ConsoleObject -RootPath '$map' -Mode Rich -PassThru Path
        (& ([scriptblock]::Create($expression))) | Should -Be 'target'
    }
}
