Describe 'ConsoleMonster window styles' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
    }

    It 'exports <Style> frames in <Scheme> without needing the WindowChrome switch' -ForEach @(
        @{ Style = 'MacOS'; Scheme = 'Dark'; Bar = 28 }
        @{ Style = 'MacOS'; Scheme = 'Light'; Bar = 28 }
        @{ Style = 'Windows'; Scheme = 'Dark'; Bar = 32 }
        @{ Style = 'Windows'; Scheme = 'Light'; Bar = 32 }
        @{ Style = 'Linux'; Scheme = 'Dark'; Bar = 36 }
        @{ Style = 'Linux'; Scheme = 'Light'; Bar = 36 }
        @{ Style = 'None'; Scheme = 'Dark'; Bar = 0 }
        @{ Style = 'None'; Scheme = 'Light'; Bar = 0 }
    ) {
        $html = New-ConsolePanel -Title 'Build' -Content { New-ConsoleText '8 of 8 checks passed' } |
            Export-ConsoleFrame -Format Html -Width 36 -Theme $Scheme -ChromeStyle $Style -Title 'Build report'
        [xml] $svg = New-ConsolePanel -Title 'Build' -Content { New-ConsoleText '8 of 8 checks passed' } |
            Export-ConsoleFrame -Format Svg -Width 36 -Theme $Scheme -ChromeStyle $Style
        [double] $svg.svg.height | Should -Be (3 * 16.8 + 24 + $Bar)
        if ($Style -eq 'None') {
            $html | Should -Not -Match 'cm-titlebar'
        } else {
            $html | Should -Match ('cm-chrome-' + $Style.ToLowerInvariant())
            $html | Should -Match ('height:' + $Bar + 'px')
            $html | Should -Match 'Build report'
        }
    }

    It 'preserves explicit false and the existing MacOS default' {
        $hidden = New-ConsoleText 'Status' | Export-ConsoleFrame -ChromeStyle Windows -WindowChrome:$false
        $hidden | Should -Not -Match 'cm-titlebar'
        $default = New-ConsoleText 'Status' | Export-ConsoleFrame -WindowChrome
        $default | Should -Match 'cm-chrome-macos'
        $none = New-ConsoleText 'Status' | Export-ConsoleFrame -ChromeStyle None -WindowChrome
        $none | Should -Not -Match 'cm-titlebar'
    }

    It 'exports <Style> dashboard and cast playback with the same style' -ForEach @(
        @{ Style = 'MacOS'; Bar = 28 }; @{ Style = 'Windows'; Bar = 32 }
        @{ Style = 'Linux'; Bar = 36 }; @{ Style = 'None'; Bar = 0 }
    ) {
        $cast = Join-Path $TestDrive ($Style + '.cast')
        [IO.File]::WriteAllLines($cast, @('{"version":2,"width":36,"height":3}', '[0.02,"o","Build complete"]'))
        $data = Export-ConsoleRecording -Path $cast -Format PlayerData -ChromeStyle $Style | ConvertFrom-Json
        $data.chromeStyle | Should -Be $Style.ToLowerInvariant()
        $data.chrome | Should -Be ($Style -ne 'None')
        [xml] $final = Export-ConsoleRecording -Path $cast -FinalFrame -ChromeStyle $Style
        [double] $final.svg.height | Should -Be (16.8 + 24 + $Bar)
        (Export-ConsoleRecording -Path $cast -Format PlayerData -ChromeStyle $Style -WindowChrome:$false | ConvertFrom-Json).chrome | Should -BeFalse
        $recording = Join-Path $TestDrive ($Style + '.jsonl')
        [IO.File]::WriteAllLines($recording, @('{"consolemonster":"recording","version":1}', '{"t":20,"target":"build","op":"advance","value":1}'))
        $dashboard = New-ConsoleDashboard -Title 'Build' -Content { New-ConsoleProgress -WidgetId build -Label 'Build' -Total 1 }
        $dashboardData = Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Format PlayerData -ChromeStyle $Style | ConvertFrom-Json
        $dashboardData.chromeStyle | Should -Be $Style.ToLowerInvariant()
        $dashboardData.chrome | Should -Be ($Style -ne 'None')
        $animated = Export-ConsoleRecording -Path $cast -Format Svg -ChromeStyle $Style
        $animated | Should -Match ('<svg x="0" y="' + $Bar + '"')
    }

    It 'invokes a recording and final frame with <Style> chrome' -ForEach @(
        @{ Style = 'MacOS'; Bar = 28 }; @{ Style = 'Windows'; Bar = 32 }
        @{ Style = 'Linux'; Bar = 36 }; @{ Style = 'None'; Bar = 0 }
    ) {
        $path = Join-Path $TestDrive ($Style + '-invoke.json')
        Invoke-ConsoleRecording { Write-Host 'Build complete' } -Destination $path -ChromeStyle $Style -InProcess -WarningAction SilentlyContinue | Out-Null
        $data = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        $data.chromeStyle | Should -Be $Style.ToLowerInvariant()
        $data.chrome | Should -Be ($Style -ne 'None')
        $finalPath = Join-Path $TestDrive ($Style + '-invoke-final.svg')
        Invoke-ConsoleRecording { Write-Host 'Build complete' } -Destination $finalPath -FinalFrame -ChromeStyle $Style -Width 36 -Height 3 -InProcess -WarningAction SilentlyContinue | Out-Null
        [xml] $svg = Get-Content -LiteralPath $finalPath -Raw
        # Windows PowerShell can also emit module-initialization progress into the fallback recording.
        # Compare the same cast so this checks chrome geometry independently of the run's occupied rows.
        [xml] $plain = Export-ConsoleRecording -Path ([IO.Path]::ChangeExtension($finalPath, '.cast')) -FinalFrame -ChromeStyle None
        [double] $svg.svg.height | Should -Be ([double] $plain.svg.height + $Bar)
    }

    It 'keeps Invoke chrome on by default and lets NoWindowChrome hide it' {
        $default = Join-Path $TestDrive 'default-invoke.json'
        Invoke-ConsoleRecording { Write-Host 'Build complete' } -Destination $default -InProcess -WarningAction SilentlyContinue | Out-Null
        $data = Get-Content -LiteralPath $default -Raw | ConvertFrom-Json
        $data.chrome | Should -BeTrue
        $data.chromeStyle | Should -Be 'macos'
        $hidden = Join-Path $TestDrive 'hidden-invoke.json'
        Invoke-ConsoleRecording { Write-Host 'Build complete' } -Destination $hidden -ChromeStyle Windows -NoWindowChrome -InProcess -WarningAction SilentlyContinue | Out-Null
        $data = Get-Content -LiteralPath $hidden -Raw | ConvertFrom-Json
        $data.chrome | Should -BeFalse
        $data.chromeStyle | Should -Be 'none'
    }

    It 'documents MacOS as the ChromeStyle default on all three cmdlets' {
        foreach ($name in 'Export-ConsoleFrame', 'Export-ConsoleRecording', 'Invoke-ConsoleRecording') {
            $help = Get-Help -Name $name -Full
            $parameter = @($help.parameters.parameter | Where-Object Name -eq 'ChromeStyle')
            $parameter.Count | Should -Be 1
            $parameter[0].defaultValue | Should -Match '^(?:\[ConsoleMonster\.Export\.WindowChromeStyle\]::)?MacOS$'
        }
    }
}
