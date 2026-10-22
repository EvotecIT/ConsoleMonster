Describe 'Theme files through the compiled PowerShell commands' {
    BeforeAll {
        $assembly = $env:CONSOLEMONSTER_THEME_TEST_ASSEMBLY
        if (-not $assembly) {
            $assembly = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        }
        Import-Module $assembly -Force
        $script:OriginalTheme = [ConsoleMonster.Terminal]::Theme
        $script:Fixtures = Join-Path $PSScriptRoot 'Fixtures/Themes'
    }

    AfterEach {
        [ConsoleMonster.Terminal]::Theme = $script:OriginalTheme
    }

    It 'selects each built-in preset including ColorBlindSafe' {
        foreach ($preset in 'Dark', 'Light', 'HighContrast', 'ColorBlindSafe') {
            Set-ConsoleTheme -Name $preset
            [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly $preset
        }
    }

    It 'exports and reloads every style and tier as <Extension>' -ForEach @(
        @{ Extension = 'json' }
        @{ Extension = 'psd1' }
    ) {
        $path = Join-Path $TestDrive "Team's theme.$Extension"
        $before = [ConsoleMonster.Styling.Theme]::ColorBlindSafe.Rename("Team's theme – Zażółć テーマ")
        [ConsoleMonster.Terminal]::Theme = $before
        $file = Export-ConsoleTheme -Path $path -PassThru
        $file | Should -BeOfType System.IO.FileInfo
        $file.FullName | Should -BeExactly $path
        [object]::ReferenceEquals($before, [ConsoleMonster.Terminal]::Theme) | Should -BeTrue
        Set-ConsoleTheme -Name Light
        Set-ConsoleTheme -Path $path
        [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly $before.Name
        foreach ($token in [Enum]::GetValues([ConsoleMonster.Styling.ThemeToken])) {
            foreach ($depth in [Enum]::GetValues([ConsoleMonster.Capabilities.ColorDepth])) {
                [ConsoleMonster.Terminal]::Theme.Resolve($token, $depth).Equals($before.Resolve($token, $depth)) | Should -BeTrue -Because "$token at $depth round trips"
            }
            $expected = $before.Get($token)
            $actual = [ConsoleMonster.Terminal]::Theme.Get($token)
            [object]::Equals($actual.Foreground, $expected.Foreground) | Should -BeTrue
            [object]::Equals($actual.Background, $expected.Background) | Should -BeTrue
            $actual.Decoration | Should -Be $expected.Decoration
        }
        if ($Extension -eq 'psd1') {
            (Import-PowerShellDataFile -LiteralPath $path).Name | Should -BeExactly $before.Name
        } else {
            (Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json).Name | Should -BeExactly $before.Name
        }
    }

    It 'imports a relative literal PSD1 file with partial inheritance and explicit clearing' {
        Push-Location $script:Fixtures
        try {
            Set-ConsoleTheme -Path './team.psd1'
        } finally {
            Pop-Location
        }
        $theme = [ConsoleMonster.Terminal]::Theme
        $theme.Name | Should -BeExactly 'Team'
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Pass).Foreground.TrueColor.ToString() | Should -BeExactly '#123456'
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Pass).Foreground.Ansi16 | Should -Be 14
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Focus).Background | Should -BeNullOrEmpty
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Header).Equals([ConsoleMonster.Styling.Theme]::Light.Get([ConsoleMonster.Styling.ThemeToken]::Header)) | Should -BeTrue
    }

    It 'keeps the current theme unchanged when validation fails for <Case>' -ForEach @(
        @{ Case = 'unknown-key'; File = 'unknown-key.json'; Message = '*unknown key*' }
        @{ Case = 'unknown-token'; File = 'unknown-token.json'; Message = '*unknown theme token*' }
        @{ Case = 'invalid-colour'; File = 'invalid-colour.json'; Message = '*invalid colour*' }
        @{ Case = 'invalid-index'; File = 'invalid-index.psd1'; Message = '*Ansi16*' }
        @{ Case = 'malformed-json'; File = 'malformed.json'; Message = '*JSON*' }
        @{ Case = 'duplicate-key'; File = 'duplicate.json'; Message = '*Duplicate*' }
    ) {
        Set-ConsoleTheme -Name HighContrast
        $before = [ConsoleMonster.Terminal]::Theme
        { Set-ConsoleTheme -Path (Join-Path $script:Fixtures $File) -ErrorAction Stop } | Should -Throw -ExpectedMessage $Message
        [object]::ReferenceEquals($before, [ConsoleMonster.Terminal]::Theme) | Should -BeTrue
    }

    It 'rejects executable PSD1 content without creating its marker or changing the theme' {
        $marker = Join-Path $TestDrive 'executed.txt'
        $path = Join-Path $TestDrive 'unsafe.psd1'
        $text = [IO.File]::ReadAllText((Join-Path $script:Fixtures 'unsafe.psd1')).Replace('__MARKER__', $marker.Replace("'", "''"))
        [IO.File]::WriteAllText($path, $text)
        $before = [ConsoleMonster.Terminal]::Theme
        { Set-ConsoleTheme -Path $path -ErrorAction Stop } | Should -Throw
        Test-Path -LiteralPath $marker | Should -BeFalse
        [object]::ReferenceEquals($before, [ConsoleMonster.Terminal]::Theme) | Should -BeTrue
    }

    It 'honours export WhatIf without creating or replacing a file' {
        $newPath = Join-Path $TestDrive 'new.json'
        Export-ConsoleTheme -Path $newPath -WhatIf
        Test-Path -LiteralPath $newPath | Should -BeFalse
        $existing = Join-Path $TestDrive 'existing.psd1'
        [IO.File]::WriteAllText($existing, 'keep me')
        Export-ConsoleTheme -Path $existing -WhatIf
        [IO.File]::ReadAllText($existing) | Should -BeExactly 'keep me'
    }

    It 'reports a missing path and unsupported extensions without changing the theme or writing files' {
        $before = [ConsoleMonster.Terminal]::Theme
        { Set-ConsoleTheme -Path (Join-Path $TestDrive 'missing.json') -ErrorAction Stop } | Should -Throw
        $unsupported = Join-Path $TestDrive 'theme.ps1'
        { Export-ConsoleTheme -Path $unsupported -ErrorAction Stop } | Should -Throw -ExpectedMessage '*extension*'
        Test-Path -LiteralPath $unsupported | Should -BeFalse
        [object]::ReferenceEquals($before, [ConsoleMonster.Terminal]::Theme) | Should -BeTrue
    }
}
