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
$screen = Invoke-ConsoleHeadless -Width 110 -Height 30 -InputScript 'Escape' { Show-ConsoleSettings -WarningAction SilentlyContinue }
"picker=$($screen.Text -match 'Density.*Comfortable \(default\)')/$($screen.Text -match 'bogus')"
'@
        $text | Should -Match 'density=Comfortable/Default'
        $text | Should -Match 'problem=.*CONSOLEMONSTER_DENSITY.*bogus'
        $text | Should -Match 'picker=True/False'
    }

    It 'fits choice labels and theme verdicts at 110 columns and still renders below their natural width' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
Set-ConsoleTheme Dark
Set-ConsoleSetting Glyphs Full
Set-ConsoleSetting Density Comfortable
$wide = Invoke-ConsoleHeadless -Width 110 -Height 30 -InputScript 'Escape' { Show-ConsoleSettings }
$narrow = Invoke-ConsoleHeadless -Width 38 -Height 30 -InputScript 'Escape' { Show-ConsoleSettings }
"wide=$($wide.Text -match 'Density.*Comfortable \(default\)')"
"dark=$($wide.Text -match '(Statuses apart.*|Colour-blind: )no')/$($wide.Text -match 'deuteranopia')/$($wide.Text -match 'Pending text too faint: 4.0:1')"
Set-ConsoleTheme ColorBlindSafeDark
$safe = Invoke-ConsoleHeadless -Width 110 -Height 30 -InputScript 'Escape' { Show-ConsoleSettings }
"safe=$($safe.Text -match '(Statuses apart.*|Colour-blind: )yes')/$($safe.Text -match 'too faint')"
"narrow=$($narrow.Text -match 'Density')/$($narrow.Text -match 'Comfortable \(default\)')"
'@
        $text | Should -Match 'wide=True'
        $text | Should -Match 'dark=True/True/True'
        $text | Should -Match 'safe=True/False'
        $text | Should -Match 'narrow=True/False'
    }

    It 'saves what is chosen in the settings picker, and Escape changes nothing' {
        $text = Invoke-SettingsChild -File $script:File -Script @'
$null = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Escape' { Show-ConsoleSettings }
"cancelled=$(Test-Path $env:CONSOLEMONSTER_SETTINGS)/$((Get-ConsoleSetting Theme).Value)"
# Theme list: Auto, Dark, Light. Twelve settings, then Save.
$screen = Invoke-ConsoleHeadless -Width 100 -Height 24 -InputScript 'Enter Down Down Enter Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Tab Enter' { Show-ConsoleSettings }
"saved=$((Get-ConsoleSetting Theme).Value)/$((Get-ConsoleSetting Theme).Source)"
'@
        $text | Should -Match 'cancelled=False/Auto'
        $text | Should -Match 'saved=Light/File'
        (Get-Content -LiteralPath $script:File -Raw | ConvertFrom-Json).Theme | Should -Be 'Light'
    }
}
