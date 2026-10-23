Describe 'Application editing and renderer failure contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        $framework = if ($PSEdition -eq 'Desktop') { 'net472' } else { 'net8.0' }
        $hostProject = Join-Path $PSScriptRoot '../../ConsoleMonster.TestHost/ConsoleMonster.TestHost.csproj'
        $extension = if ($PSEdition -eq 'Desktop') { 'exe' } else { 'dll' }
        $assemblyPath = [IO.Path]::GetFullPath((Join-Path (Split-Path $hostProject) "bin/Debug/$framework/ConsoleMonster.TestHost.$extension"))
        if (-not (Test-Path -LiteralPath $assemblyPath)) {
            throw 'Build ConsoleMonster.TestHost in Debug before running the module tests.'
        }
        if ($PSEdition -eq 'Desktop') {
            $assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)
        } else {
            $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext([ConsoleMonster.Terminal].Assembly)
            $assembly = $context.LoadFromAssemblyPath($assemblyPath)
        }
        $script:FailureProbeType = $assembly.GetType('ConsoleMonster.TestHost.RendererFailureProbe', $true)
    }

    It 'keeps cleared text-area navigation and insertion valid without another frame' {
        $area = [ConsoleMonster.Interactive.TextArea]::new('Notes', "one`ntwo`nthree")
        $null = [ConsoleMonster.Testing.TestTerminal]::Render($area, 20)
        $area.Value = ''
        $context = [ConsoleMonster.Terminal]::CreateContext()
        $area.HandleInput([ConsoleMonster.Input.InputEvent]::Press([ConsoleKey]::DownArrow), $context) | Should -BeTrue
        $area.HandleInput([ConsoleMonster.Input.InputEvent]::Character('x'), $context) | Should -BeTrue
        $area.Value | Should -Be 'x'
    }

    It 'returns every keyboard-only edit through Show-ConsoleApp' {
        $inputSource = [ConsoleMonster.Input.ScriptedInput]::new('Ctrl+Home Ctrl+Delete*3 Down "x" Escape')
        $capture = [ConsoleMonster.Interactive.ScreenCapture]::new(80, 24)
        $result = Show-ConsoleApp {
            New-ConsoleTextArea -Name Notes -Value "one`ntwo`nthree"
        } -InputSource $inputSource -Capture $capture -NoMouse -PassThru -ErrorAction Stop
        $result.Notes | Should -Be 'x'
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
    }

    It 'propagates a renderer-thread failure without returning a successful partial state' {
        $probe = [Activator]::CreateInstance($script:FailureProbeType)
        try {
            $live = [ConsoleMonster.Live.LiveSessionOptions]::new()
            $live.Output = $probe
            $live.CaptureConsole = $false
            $live.TaskbarProgress = $false
            $output = [Collections.Generic.List[object]]::new()
            {
                Show-ConsoleApp { New-ConsoleTextInput -Name Query } -InputSource $probe -Profile $probe.Profile `
                    -Live $live -NoMouse -PassThru -ErrorAction Stop | ForEach-Object { $output.Add($_) }
            } | Should -Throw -ExpectedMessage '*Synthetic renderer failure*'
            $output.Count | Should -Be 0
            $probe.Ended | Should -BeTrue
            [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
        } finally { $probe.Dispose() }
    }
}
