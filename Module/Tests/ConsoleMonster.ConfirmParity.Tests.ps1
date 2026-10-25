Describe 'Typed confirmation warning parity' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'renders the same warning and answer as TypedConfirmPrompt with <UseWarning> explicit warning' -ForEach @(
        @{ UseWarning = $true }
        @{ UseWarning = $false }
    ) {
        param($UseWarning)
        $saved = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default
        try {
            $target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal'))::Target(80, 16, 'Full', 'None', [cultureinfo]::InvariantCulture, "'inventory' Enter")
            $options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::new()
            $options.Target = $target
            $options.Mode = 'Rich'
            ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $options
            $parameters = @{ Title = 'Remove inventory?'; TypedConfirmation = 'inventory'; Description = 'Description fallback'; Mode = 'Rich' }
            if ($UseWarning) { $parameters.Warning = 'Explicit warning' }
            $actual = Read-ConsoleConfirm @parameters
            $actualFrames = @($target.Screens.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n"

            $expectedTarget = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Testing.TestTerminal'))::Target(80, 16, 'Full', 'None', [cultureinfo]::InvariantCulture, "'inventory' Enter")
            $expectedOptions = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::new()
            $expectedOptions.Target = $expectedTarget
            $expectedOptions.Mode = 'Rich'
            $prompt = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.TypedConfirmPrompt'))::new('Remove inventory?', 'inventory')
            $prompt.Warning = if ($UseWarning) { 'Explicit warning' } else { 'Description fallback' }
            $expected = $prompt.Show($expectedOptions)
            $expectedFrames = @($expectedTarget.Screens.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n"
            $actual | Should -BeTrue
            $actual | Should -Be $expected
            $actualFrames | Should -BeExactly $expectedFrames
            $actualFrames | Should -Match ([regex]::Escape($prompt.Warning))
            if ($UseWarning) { $actualFrames | Should -Not -Match 'Description fallback' }
        } finally {
            ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default = $saved
        }
    }
}
