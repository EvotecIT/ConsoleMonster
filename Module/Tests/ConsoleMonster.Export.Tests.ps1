Describe 'ConsoleMonster exports' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
    }

    It 'exports a widget to a self-contained HTML page with both themes' {
        $path = Join-Path $TestDrive 'panel.html'
        New-ConsolePanel -Title 'Status' { New-ConsoleText 'All <good> & "fine"' } | Export-ConsoleFrame -Path $path -Width 40
        $html = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $html | Should -Match '<!DOCTYPE html>'
        $screens = [regex]::Matches($html, '(?s)<pre\b[^>]*\bclass="([^"]*)"[^>]*>(.*?)</pre>')
        foreach ($theme in 'cm-dark', 'cm-light') {
            $screen = @($screens | Where-Object {
                $classes = $_.Groups[1].Value -split '\s+'
                $classes -contains 'cm-screen' -and $classes -contains $theme
            })
            $screen.Count | Should -Be 1
            $screen[0].Groups[2].Value | Should -Match 'All &lt;good&gt; &amp; &quot;fine&quot;'
        }
        $html | Should -Match 'prefers-color-scheme: light'
        $html | Should -Match 'All &lt;good&gt; &amp; &quot;fine&quot;'
        $html | Should -Not -Match '<script'
        $html | Should -Not -Match 'https?://(?!www\.w3\.org)'
    }

    It 'exports SVG and text, and picks the format from the extension' {
        $svg = Join-Path $TestDrive 'bar.svg'
        $file = New-ConsolePanel -Title 'Failures' { New-ConsoleBarChart -Data @{ DNS = 3; Kerberos = 1 } } | Export-ConsoleFrame -Path $svg -Width 50 -Theme Dark -Title 'Chart' -WindowChrome -PassThru
        $file | Should -BeOfType System.IO.FileInfo
        $content = Get-Content -LiteralPath $svg -Raw -Encoding UTF8
        $content | Should -Match '^<svg xmlns="http://www.w3.org/2000/svg"'
        $content | Should -Match '<title>Chart</title>'
        $content | Should -Match 'class="cm-t'
        $text = New-ConsolePanel -Title 'Failures' { New-ConsoleBarChart -Data @{ DNS = 3 } } | Export-ConsoleFrame -Format Text -Width 40
        $text | Should -Match 'Failures'
        $text | Should -Match 'DNS'
    }

    It 'exports Format-ConsoleTable -AsWidget and plain objects as tables' {
        $rows = @([pscustomobject] @{ Name = 'FS01'; Free = 41 }, [pscustomobject] @{ Name = 'WEB01'; Free = 12 })
        $widget = $rows | Format-ConsoleTable -AsWidget
        $widget.GetType().Name | Should -Be 'Table'
        $text = $widget | Export-ConsoleFrame -Format Text -Width 40
        $text | Should -Match 'Name'
        $text | Should -Match 'WEB01'
        $objects = $rows | Export-ConsoleFrame -Format Text -Width 40
        $objects | Should -Match 'FS01'
    }

    It 'drops javascript: links and keeps web links' {
        $safe = New-ConsoleText -Text 'docs' -Link 'https://example.org/docs' | Export-ConsoleFrame -Format Html -Width 20
        $safe | Should -Match '<a href="https://example.org/docs"'
        $bad = New-ConsoleText -Text 'click' -Link 'javascript:alert(1)' | Export-ConsoleFrame -Format Html -Width 20
        $bad | Should -Not -Match 'javascript'
        $bad | Should -Match 'click'
    }

    It 'exports a recording as an HTML player, an animated SVG and asciicast v3' {
        $recording = Join-Path $TestDrive 'run.jsonl'
        $lines = @(
            '{"consolemonster":"recording","version":1}'
            '{"t":100,"target":"build","op":"advance","value":2}'
            '{"t":600,"target":"build","op":"advance","value":3}'
            '{"t":1200,"target":"build","op":"advance","value":5}'
        )
        [IO.File]::WriteAllLines($recording, $lines)
        $dashboard = New-ConsoleDashboard -Title 'Build' { New-ConsoleProgress -Name build -Label 'Build' -Total 10 }

        $html = Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Format Html -Width 60 -Height 8
        $html | Should -Match '<script>'
        $html | Should -Match '"frames":\[\[0,'
        $html | Should -Match 'ConsoleMonsterPlayer'
        $html | Should -Not -Match 'src="http'

        # Markdown is a frame format. Recording destinations keep their existing HTML fallback for other extensions.
        foreach ($extension in 'txt', 'md', 'markdown') {
            $fallbackPath = Join-Path $TestDrive ('run.' + $extension)
            Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Destination $fallbackPath -Width 60 -Height 8
            Get-Content -LiteralPath $fallbackPath -Raw -Encoding UTF8 | Should -Match 'ConsoleMonsterPlayer'
        }

        $dataPath = Join-Path $TestDrive 'run.json'
        Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Destination $dataPath -Width 60 -Height 8
        $data = Get-Content -LiteralPath $dataPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $data.format | Should -Be 'consolemonster-player'
        $data.width | Should -Be 60
        @($data.frames).Count | Should -BeGreaterThan 1
        (Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Format PlayerData -Width 60 -Height 8 | ConvertFrom-Json).height | Should -Be 8

        $svgPath = Join-Path $TestDrive 'run.svg'
        Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Destination $svgPath -Width 60 -Height 8
        $svg = Get-Content -LiteralPath $svgPath -Raw -Encoding UTF8
        $svg | Should -Match '@keyframes cm-play'
        $svg | Should -Not -Match '<script'

        $cast = Export-ConsoleRecording -Dashboard $dashboard -Path $recording -Format Asciicast -Width 60 -Height 8
        $castLines = @($cast -split "`n" | Where-Object { $_ })
        $header = $castLines[0] | ConvertFrom-Json
        $header.version | Should -Be 3
        $header.term.cols | Should -Be 60
        $header.term.rows | Should -Be 8
        foreach ($line in $castLines | Select-Object -Skip 1) {
            $line | Should -Match '^\[\d+(\.\d+)?,"o",".*"\]$'
        }
    }
}
