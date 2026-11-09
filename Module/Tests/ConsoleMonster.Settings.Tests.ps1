Describe 'Settings saved for every session' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A fresh session using its own settings file, so each run starts like a new terminal.
        function Invoke-SettingsChild {
            param([Parameter(Mandatory)][string] $File, [Parameter(Mandatory)][string] $Script, [hashtable] $Environment = @{})
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_THEME', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MOTION', 'CONSOLEMONSTER_NOMOUSE', 'CONSOLEMONSTER_LANGUAGE', 'CONSOLEMONSTER_CULTURE') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "`$env:CONSOLEMONSTER_SETTINGS = '$File'"
                foreach ($name in $Environment.Keys) { "`$env:$name = '$($Environment[$name])'" }
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    BeforeEach {
        $script:Folder = Join-Path ([System.IO.Path]::GetTempPath()) ('cm-settings-' + [guid]::NewGuid().ToString('N'))
        $null = New-Item -ItemType Directory -Path $script:Folder
        $script:File = Join-Path $script:Folder 'settings.json'
    }

    AfterEach {
        Remove-Item -LiteralPath $script:Folder -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'keeps a saved theme and formatting for the next session, below environment variables' {
        $null = Invoke-SettingsChild -File $script:File -Script @'
Set-ConsoleTheme ColorBlindSafe -Save
Enable-ConsoleFormatting -Include Help -Save -Confirm:$false
'@
        $next = Invoke-SettingsChild -File $script:File -Script @'
$theme = Get-ConsoleSetting Theme
"theme=$($theme.Value)/$($theme.Source)"
"preference=$((Get-ConsoleCapability).ThemePreference)"
Enable-ConsoleFormatting -Confirm:$false
"features=$((Get-ConsoleFormatting).Features -join ',')"
'@
        $next | Should -Match 'theme=ColorBlindSafe/File'
        $next | Should -Match 'preference=ColorBlind'
        $next | Should -Match 'features=.*Help'

        $overridden = Invoke-SettingsChild -File $script:File -Environment @{ CONSOLEMONSTER_THEME = 'light' } -Script @'
$theme = Get-ConsoleSetting Theme
"theme=$($theme.Value)/$($theme.Source)"
Set-ConsoleSetting Theme Dark
"session=$((Get-ConsoleSetting Theme).Source)/$((Get-ConsoleCapability).ThemePreference)"
'@
        $overridden | Should -Match 'theme=Light/Environment'
        $overridden | Should -Match 'session=Session/Dark'
    }

    It 'saves what is chosen in the settings picker, and Escape changes nothing' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
$null = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Escape' { Show-ConsoleSettings }
"cancelled=$(Test-Path $env:CONSOLEMONSTER_SETTINGS)/$((Get-ConsoleSetting Theme).Value)"
# Theme list: Auto, Dark, Light. Nine settings, then Save.
$screen = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Down Enter Tab Tab Tab Tab Tab Tab Tab Tab Tab Enter' { Show-ConsoleSettings }
"saved=$((Get-ConsoleSetting Theme).Value)/$((Get-ConsoleSetting Theme).Source)"
'@
        $text | Should -Match 'cancelled=False/Auto'
        $text | Should -Match 'saved=Light/File'
        (Get-Content -LiteralPath $script:File -Raw | ConvertFrom-Json).Theme | Should -Be 'Light'
    }
}
