Describe 'Settings saved for every session' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A fresh session using its own settings file, so each run starts like a new terminal.
        function Invoke-SettingsChild {
            param([Parameter(Mandatory)][string] $File, [Parameter(Mandatory)][string] $Script, [hashtable] $Environment = @{})
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_THEME', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_DENSITY', 'CONSOLEMONSTER_MOTION', 'CONSOLEMONSTER_NOMOUSE', 'CONSOLEMONSTER_LANGUAGE', 'CONSOLEMONSTER_CULTURE') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
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

    It 'round-trips the named choice <Stored> through settings.json and a fresh session' -ForEach @(
        @{ Command = 'Set-ConsoleTheme Dracula -Save'; Stored = 'Dracula'; Scheme = 'Dracula'; Accessible = 'False' }
        @{ Command = 'Set-ConsoleTheme Dracula -Accessible -Save'; Stored = 'Dracula · accessible'; Scheme = 'Dracula'; Accessible = 'True' }
        @{ Command = "Set-ConsoleSetting Theme 'cAtPpUcCiN lAtTe · AcCeSsIbLe' -Save"; Stored = 'Catppuccin Latte · accessible'; Scheme = 'Catppuccin Latte'; Accessible = 'True' }
        @{ Command = "Set-ConsoleTheme 'Tokyo Night' -Accessible -Save"; Stored = 'Tokyo Night · accessible'; Scheme = 'Tokyo Night Dark'; Accessible = 'True' }
    ) {
        $null = Invoke-SettingsChild -File $script:File -Script $Command
        (Get-Content -LiteralPath $script:File -Raw -Encoding UTF8 | ConvertFrom-Json).Theme | Should -BeExactly $Stored
        $next = Invoke-SettingsChild -File $script:File -Script '"value=$((Get-ConsoleSetting Theme).Value);source=$((Get-ConsoleSetting Theme).Source);scheme=$([ConsoleMonster.Terminal]::Theme.Scheme.Name);accessible=$([ConsoleMonster.Terminal]::Theme.IsAccessible)"'
        $next | Should -Match ([regex]::Escape("value=$Stored;source=File;scheme=$Scheme;accessible=$Accessible"))
        $overridden = Invoke-SettingsChild -File $script:File -Environment @{ CONSOLEMONSTER_THEME = 'nord' } -Script '"scheme=$([ConsoleMonster.Terminal]::Theme.Scheme.Name);source=$((Get-ConsoleSetting Theme).Source)"'
        $overridden | Should -Match 'scheme=Nord;source=Environment'
    }

    It 'keeps Terminal and an accessible scheme choice across fresh sessions' {
        $null = Invoke-SettingsChild -File $script:File -Script 'Set-ConsoleTheme Terminal -Save'
        $next = Invoke-SettingsChild -File $script:File -Script '"theme=$((Get-ConsoleSetting Theme).Value);preference=$((Get-ConsoleCapability).ThemePreference)"'
        $next | Should -Match 'theme=Terminal;preference=Terminal'
        $source = Join-Path $script:Folder 'scheme.json'
        $theme = Join-Path $script:Folder 'scheme.theme.json'
        $data = Get-Content (Join-Path $PSScriptRoot '../../ConsoleMonster/Styling/Schemes/Dracula.json') -Raw | ConvertFrom-Json
        $data.schemes[0].name = 'Session scheme'
        $data.schemes[0] | ConvertTo-Json | Set-Content -LiteralPath $source -Encoding UTF8
        $null = Invoke-SettingsChild -File $script:File -Script "Import-ConsoleTheme -Path '$source' -OutFile '$theme'; Set-ConsoleTheme -Path '$theme' -Accessible -Save"
        $next = Invoke-SettingsChild -File $script:File -Script '"accessible=$([ConsoleMonster.Terminal]::Theme.IsAccessible);value=$((Get-ConsoleSetting Theme).Value)"'
        $next | Should -Match 'accessible=True'
        $next | Should -Match 'value=.*scheme.theme.json · accessible'
        (Get-Content -LiteralPath $theme -Raw | ConvertFrom-Json).Variant | Should -BeNullOrEmpty
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

    It 'exports in the chosen theme unless -Theme says otherwise' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
$chosen = New-ConsoleText 'ok' -Role Pass | Export-ConsoleFrame -Format Html -Width 20
"auto=$($chosen -match 'cm-light' -and $chosen -match 'cm-dark')"
Set-ConsoleSetting Theme ColorBlindSafeLight
$chosen = New-ConsoleText 'ok' -Role Pass | Export-ConsoleFrame -Format Html -Width 20
$explicit = New-ConsoleText 'ok' -Role Pass | Export-ConsoleFrame -Format Html -Width 20 -Theme Auto
"chosen=$($chosen -match 'cm-dark')/$($chosen -match '#ffffff')"
"explicit=$($explicit -match 'cm-light' -and $explicit -match 'cm-dark')"
'@
        $text | Should -Match 'auto=True'
        $text | Should -Match 'chosen=False/True'
        $text | Should -Match 'explicit=True'
    }

    It 'keeps authored spacing while Compact changes unbound PowerShell defaults and current exports' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
Set-ConsoleSetting Density Compact -Save
"density=$((Get-ConsoleSetting Density).Value)/$((Get-ConsoleSetting Density).Source)/$([ConsoleMonster.Terminal]::Profile.Density)"
$default = New-ConsolePanel -NoExpand -Content { New-ConsoleText 'x' } | Export-ConsoleFrame -Format Text -Width 5 -Theme Current
$explicit = New-ConsolePanel -NoExpand -PaddingLeft 1 -PaddingRight 1 -Content { New-ConsoleText 'x' } | Export-ConsoleFrame -Format Text -Width 5 -Theme Current
"panels=$($default -match '│x')/$($explicit -match '│ x │')"
$default = New-ConsoleKeyValue -InputObject @{ Key = 'value' } | Export-ConsoleFrame -Format Text -Width 20 -Theme Current
$explicit = New-ConsoleKeyValue -InputObject @{ Key = 'value' } -Gap 2 | Export-ConsoleFrame -Format Text -Width 20 -Theme Current
"lists=$($default.Trim() -eq 'Key value')/$($explicit.Trim() -eq 'Key  value')"
$default = New-ConsoleLayoutGrid -Columns 2 -MinColumnWidth 1 -Content { New-ConsoleText 'a'; New-ConsoleText 'b' } | Export-ConsoleFrame -Format Text -Width 3 -Theme Current
$explicit = New-ConsoleLayoutGrid -Columns 2 -MinColumnWidth 1 -ColumnGap 1 -Content { New-ConsoleText 'a'; New-ConsoleText 'b' } | Export-ConsoleFrame -Format Text -Width 3 -Theme Current
"grids=$($default.Trim() -eq 'a b')/$($explicit.Trim() -eq 'a b')"
Reset-ConsoleSetting Density
"restored=$((Get-ConsoleSetting Density).Value)/$((Get-ConsoleSetting Density).Source)"
'@
        $text | Should -Match 'density=Compact/File/Compact'
        $text | Should -Match 'panels=True/True'
        $text | Should -Match 'lists=True/True'
        $text | Should -Match 'grids=True/True'
        $text | Should -Match 'restored=Compact/File'
        (Get-Content -LiteralPath $script:File -Raw | ConvertFrom-Json).Density | Should -Be 'Compact'
    }

    It 'reports an invalid environment density and keeps the picker usable' {
        $text = Invoke-SettingsChild -File $script:File -Environment @{ CONSOLEMONSTER_DENSITY = 'bogus' } -Script @'
$setting = Get-ConsoleSetting Density -WarningVariable problems -WarningAction SilentlyContinue
"density=$($setting.Value)/$($setting.Source)"
"problem=$($problems -match 'CONSOLEMONSTER_DENSITY.*bogus')"
$screen = Invoke-ConsoleHeadless -Width 110 -Height 30 -InputScript 'Escape' { Show-ConsoleSetting -WarningAction SilentlyContinue }
"picker=$($screen.Text -match 'Density.*Comfortable \(default\)')/$($screen.Text -match 'bogus')"
'@
        $text | Should -Match 'density=Comfortable/Default'
        $text | Should -Match 'problem=.*CONSOLEMONSTER_DENSITY.*bogus'
        $text | Should -Match 'picker=True/False'
    }

    It 'keeps a table and complete theme verdict beside capped closed controls at <Width> columns' -ForEach @(
        @{ Width = 110 }
        @{ Width = 90 }
    ) {
        $text = Invoke-SettingsChild -File $script:File -Script @"
Set-ConsoleSetting Glyphs Full
Set-ConsoleSetting Density Comfortable
foreach (`$theme in 'Dark', 'ColorBlindSafeDark') {
    Set-ConsoleTheme `$theme
    `$screen = Invoke-ConsoleHeadless -Width $Width -Height 30 -InputScript 'Escape' { Show-ConsoleSetting }
    `$lines = `$screen.Text -split "``n"
    `$panel = `$lines | Where-Object { `$_ -match '^╭─ Settings' } | Select-Object -First 1
    `$boundary = `$panel.IndexOf('╮') + 1
    "`$theme/pane=`$(`$boundary -gt 0 -and `$boundary -le [Math]::Floor($Width * 0.45))"
    "`$theme/table=`$(`$screen.Text -match 'Check.*State.*Size')/`$(`$screen.Text -match '3 records')"
    "`$theme/dropped=`$(`$screen.Text -match '\+1 column \(Changed\)')"
    "`$theme/labels=`$(`$screen.Text -match 'Density.*Comfortable \(default\)')"
    "`$theme/verdict=`$(`$screen.Text -match 'deuteranopia')/`$(`$screen.Text -match 'Pending text too faint: 4.0:1')/`$(`$screen.Text -match '(Statuses apart.*|Colour-blind: )yes')"
}
`$narrow = Invoke-ConsoleHeadless -Width 38 -Height 30 -InputScript 'Escape' { Show-ConsoleSetting }
"narrow=`$(`$narrow.Text -match 'Density')/`$(`$narrow.Text -match 'Comfortable \(default\)')"
"@
        foreach ($theme in 'Dark', 'ColorBlindSafeDark') {
            $text | Should -Match "$theme/pane=True"
            $text | Should -Match "$theme/table=True/False"
        }
        if ($Width -eq 110) { $text | Should -Match 'Dark/labels=True' }
        if ($Width -eq 90) { $text | Should -Match 'Dark/dropped=True' }
        $text | Should -Match 'Dark/verdict=True/True/False'
        $text | Should -Match 'ColorBlindSafeDark/verdict=False/False/True'
        $text | Should -Match 'narrow=True/False'
    }

    It 'saves what is chosen in the settings picker, and Escape changes nothing' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
$null = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Escape' { Show-ConsoleSetting }
"cancelled=$(Test-Path $env:CONSOLEMONSTER_SETTINGS)/$((Get-ConsoleSetting Theme).Value)"
# Theme list: Auto, Dark, Light. Twelve settings, then Save.
$screen = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Down Enter Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Enter' { Show-ConsoleSetting }
"saved=$((Get-ConsoleSetting Theme).Value)/$((Get-ConsoleSetting Theme).Source)"
'@
        $text | Should -Match 'cancelled=False/Auto'
        $text | Should -Match 'saved=Light/File'
        (Get-Content -LiteralPath $script:File -Raw | ConvertFrom-Json).Theme | Should -Be 'Light'
    }
}
