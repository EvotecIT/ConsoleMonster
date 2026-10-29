Describe 'ConsoleMonster tree golden output' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Get-TreeGoldenFixture.ps1')
        $script:PreviousCulture = [System.Threading.Thread]::CurrentThread.CurrentCulture
        [System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')
    }

    AfterAll { [System.Threading.Thread]::CurrentThread.CurrentCulture = $script:PreviousCulture }

    It 'renders <Kind> at <Width> columns through the tree view' -TestCases @(
        @{ Kind = 'service'; Width = 80 }; @{ Kind = 'service'; Width = 120 }
        @{ Kind = 'process'; Width = 80 }; @{ Kind = 'process'; Width = 120 }
        @{ Kind = 'file'; Width = 80 }; @{ Kind = 'file'; Width = 120 }
        @{ Kind = 'record'; Width = 80 }; @{ Kind = 'record'; Width = 120 }
    ) {
        param($Kind, $Width)
        $fixture = Get-TreeGoldenFixture $Kind
        $golden = [IO.File]::ReadAllText((Join-Path $PSScriptRoot "Fixtures/TreeGolden/$Kind-$Width.txt"), [Text.Encoding]::UTF8).Replace("`r`n", "`n").TrimEnd("`n")
        $policy = [ConsoleMonster.Terminal]::UseProfilePolicy({ param($profile) $profile.WithGlyphs('Ascii') })
        try {
            $actual = $fixture | Show-Console -View Tree -Depth 1 -Width $Width -AsString
            $actual.Replace("`r`n", "`n").TrimEnd("`n") | Should -BeExactly $golden
        } finally { $policy.Dispose() }
    }

    It 'uses the shared Unicode boolean presenter without an extra separator' {
        $widget = [pscustomobject]@{ Name = 'worker'; Ready = $true; Disabled = $false } | Show-Console -View Tree -Depth 1 -AsWidget
        $profileType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile')
        $profile = $profileType::Fixed(80, 24, 'None', 'Full', 'Stream')
        $text = [ConsoleMonster.Terminal]::RenderText($widget, $profile)
        $text | Should -Match ("Ready\s+" + [char]0x2713 + ' yes')
        $text | Should -Match ("Disabled\s+" + [char]0x2013 + ' no')
        $text | Should -Not -Match ("Disabled\s+-\s+" + [char]0x2013)
    }
}
