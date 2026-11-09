Describe 'Theme files through the compiled PowerShell commands' {
    BeforeAll {
        $assembly = $env:CONSOLEMONSTER_THEME_TEST_ASSEMBLY
        if (-not $assembly) {
            $assembly = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        }
        Import-Module $assembly -Force
        $script:Fixtures = Join-Path $PSScriptRoot 'Fixtures/Themes'
    }

    AfterEach {
        # Back to the detected theme, so later tests in this session see no chosen theme.
        Reset-ConsoleSetting Theme
    }

    It 'uses the same public C# writer and reader for <Extension> themes' -ForEach @(
        @{ Extension = 'json' }
        @{ Extension = 'psd1' }
    ) {
        $path = Join-Path $TestDrive "csharp.$Extension"
        $exported = Join-Path $TestDrive "powershell.$Extension"
        $model = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Styling.ThemeFile')
        $bridge = (Get-Command Export-ConsoleTheme).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.ConsoleThemeDataFile')
        $document = $model.GetMethod('FromTheme').Invoke($null, @([ConsoleMonster.Terminal]::Theme))
        $bridge.GetMethod('Save').Invoke($null, @($document.PSObject.BaseObject, $path.PSObject.BaseObject))
        Export-ConsoleTheme -Path $exported
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($path)) | Should -BeExactly ([Convert]::ToBase64String([IO.File]::ReadAllBytes($exported)))
        $loaded = $bridge.GetMethod('Load').Invoke($null, @($path.PSObject.BaseObject))
        $loaded.ToJson() | Should -BeExactly $document.ToJson()
    }

    It 'selects each built-in preset including both ColorBlindSafe variants' {
        foreach ($preset in 'Dark', 'Light', 'HighContrast', 'ColorBlindSafeLight') {
            Set-ConsoleTheme -Theme $preset
            [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly $preset
        }
        Set-ConsoleTheme -Theme ColorBlindSafeDark
        [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly 'ColorBlindSafe'
        Set-ConsoleTheme -Theme ColorBlindSafe
        [ConsoleMonster.Terminal]::Theme.Name | Should -BeIn 'ColorBlindSafe', 'ColorBlindSafeLight'
    }

    It 'round trips Compact density through JSON and PSD1 style files' {
        $path = Join-Path $TestDrive 'compact.psd1'
        "@{ SchemaVersion = 2; Name = 'Team'; BaseTheme = 'Dark'; Style = @{ Density = 'Compact' }; Tokens = @{} }" | Set-Content -LiteralPath $path -Encoding UTF8
        Set-ConsoleTheme -Path $path
        [ConsoleMonster.Terminal]::Theme.Appearance.Density.ToString() | Should -BeExactly Compact
        foreach ($extension in 'json', 'psd1') {
            $exported = Join-Path $TestDrive "compact.$extension"
            Export-ConsoleTheme -Path $exported
            Set-ConsoleTheme -Theme Dark
            Set-ConsoleTheme -Path $exported
            [ConsoleMonster.Terminal]::Theme.Appearance.Density.ToString() | Should -BeExactly Compact
        }
    }

    It 'adds the selected theme to the profile snippet until the theme is replaced' {
        Set-ConsoleTheme -Theme ColorBlindSafeLight
        $snippet = @(Enable-ConsoleFormatting -ProfileSnippet)
        $snippet | Should -Contain 'Set-ConsoleTheme -Theme ColorBlindSafeLight'
        [Array]::IndexOf($snippet, 'Set-ConsoleTheme -Theme ColorBlindSafeLight') | Should -BeLessThan ([Array]::IndexOf($snippet, 'Enable-ConsoleFormatting'))
        [ConsoleMonster.Terminal]::Theme = [ConsoleMonster.Styling.Theme]::Dark
        @(Enable-ConsoleFormatting -ProfileSnippet) | Should -Not -Contain 'Set-ConsoleTheme -Theme ColorBlindSafeLight'
    }

    It 'quotes a theme file path with straight and typographic apostrophes in the profile snippet' {
        $path = Join-Path $TestDrive ("Team's " + [char]0x2019 + "theme.json")
        Export-ConsoleTheme -Path $path
        Set-ConsoleTheme -Path $path
        $line = @(Enable-ConsoleFormatting -ProfileSnippet) | Where-Object { $_ -like 'Set-ConsoleTheme -Path *' }
        $tokens = $null; $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($line, [ref]$tokens, [ref]$errors)
        $errors | Should -BeNullOrEmpty
        $command = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
        $command.CommandElements[2].Value | Should -BeExactly $path
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
        Set-ConsoleTheme -Theme Light
        Set-ConsoleTheme -Path $path
        [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly $before.Name
        foreach ($token in [Enum]::GetValues([ConsoleMonster.Styling.ThemeToken])) {
            foreach ($depth in [Enum]::GetValues([ConsoleMonster.Capabilities.ColorDepth])) {
                [ConsoleMonster.Terminal]::Theme.Resolve($token, $depth).Equals($before.Resolve($token, $depth)) | Should -BeTrue -Because "$token at $depth round trips"
            }
            $expected = $before.Get($token)
            $actual = [ConsoleMonster.Terminal]::Theme.Get($token)
            [object]::Equals($actual.Color, $expected.Color) | Should -BeTrue
            [object]::Equals($actual.BackgroundColor, $expected.BackgroundColor) | Should -BeTrue
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
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Pass).Color.TrueColor.ToString() | Should -BeExactly '#123456'
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Pass).Color.Ansi16 | Should -Be 14
        $theme.Get([ConsoleMonster.Styling.ThemeToken]::Focus).BackgroundColor | Should -BeNullOrEmpty
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
        Set-ConsoleTheme -Theme HighContrast
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

    It 'checks the active and named themes without changing the selection' {
        Set-ConsoleTheme ColorBlindSafeLight
        $before = [ConsoleMonster.Terminal]::Theme
        $active = Test-ConsoleTheme
        $active.Passed | Should -BeTrue
        $dark = Test-ConsoleTheme -Theme Dark
        $dark.Passed | Should -BeFalse
        @($dark.Results | Where-Object { $_.Check -eq 'ColourVision' -and $_.Outcome -eq 'Fail' }).Count | Should -BeGreaterThan 0
        [object]::ReferenceEquals($before, [ConsoleMonster.Terminal]::Theme) | Should -BeTrue
    }

    It 'loads a <Extension> theme for checking and makes failures terminating in Strict mode' -ForEach @(
        @{ Extension = 'json' }
        @{ Extension = 'psd1' }
    ) {
        Set-ConsoleTheme Dark
        $path = Join-Path $TestDrive "check.$Extension"
        Export-ConsoleTheme -Path $path
        Set-ConsoleTheme ColorBlindSafeLight
        $report = Test-ConsoleTheme -Path $path
        $report.Passed | Should -BeFalse
        $caught = $null
        try {
            Test-ConsoleTheme -Path $path -Strict
        } catch {
            $caught = $_
        }
        $caught.FullyQualifiedErrorId | Should -BeLike 'ConsoleThemeCheckFailed,*'
        @($caught.TargetObject.Results | Where-Object Outcome -EQ Fail).Count | Should -BeGreaterThan 0
        [ConsoleMonster.Terminal]::Theme.Name | Should -BeExactly ColorBlindSafeLight
        (Test-ConsoleTheme -Theme ColorBlindSafeLight -Background White -Strict).Passed | Should -BeTrue
    }

    It 'leads with the verdict and shows only problems and unknowns at 80 columns' {
        $oldColumns = $env:COLUMNS
        try {
            $env:COLUMNS = '80'
            $report = Test-ConsoleTheme -Theme Dark
            $text = $report | Out-String -Width 80
        } finally {
            $env:COLUMNS = $oldColumns
        }
        $report.ThemeName | Should -BeExactly Dark
        $report.Results.Count | Should -Be 28
        $lines = @($text -split '\r?\n' | Where-Object { $_.Trim().Length -gt 0 })
        $lines[0] | Should -BeExactly 'Dark: 2 problems, 1 unknown'
        $text | Should -Match 'Pass and fail look alike with deuteranopia \(9.2, needs 25\)'
        $text | Should -Match 'Pending text is too faint: 4.0:1, needs 4.5:1'
        $text | Should -Match "Text uses the terminal's own colour, so its contrast can't be measured"
        $lines[-1] | Should -BeExactly '25 other checks pass'
        $lines.Count | Should -Be 5
        @($lines | Where-Object Length -GT 80).Count | Should -Be 0
        (Test-ConsoleTheme -Theme ColorBlindSafeDark | Out-String).Trim() | Should -BeExactly 'ColorBlindSafe: passes every check'
    }

    It 'keeps every measurement and uses one decimal and structural words in Detailed mode' {
        $report = Test-ConsoleTheme -Theme Dark -Detailed
        $report.Results.Count | Should -Be 28
        $text = $report | Out-String -Width 100
        $text | Should -Match '80.6, needs 25'
        $text | Should -Match '112.5, needs 25'
        $text | Should -Match '69.0, needs 25'
        $text | Should -Match 'Pass and warning markers are different'
        $text | Should -Match 'Critical is filled, unlike ordinary text'
        $text | Should -Not -Match '1.0 / 1|RGB|other checks pass'
        ((Test-ConsoleTheme -Theme ColorBlindSafeDark -Detailed) | Out-String -Width 100) | Should -Match 'Critical is filled'
        (Get-Help Test-ConsoleTheme).Synopsis | Should -Match 'contrast'
    }

    It 'groups a failing pair across vision conditions without dropping measurements' {
        $path = Join-Path $TestDrive 'same-statuses.json'
        @{
            Name = 'Same statuses'
            BaseTheme = 'ColorBlindSafe'
            Tokens = @{
                Fail = @{
                    Foreground = @{
                        TrueColor = '#6cb6ff'
                        Ansi16 = 14
                    }
                }
            }
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding UTF8
        $report = Test-ConsoleTheme -Path $path
        @($report.Results | Where-Object Outcome -EQ Fail).Count | Should -Be 4
        $text = $report | Out-String -Width 120
        $text | Should -Match 'Same statuses: 1 problem'
        ([regex]::Matches($text, 'Pass and fail look alike')).Count | Should -Be 1
        ($text -replace '\s+', ' ') | Should -Match 'in normal vision and with protanopia, deuteranopia and tritanopia'
        $text | Should -Match '0.0, needs 25'
    }

}
