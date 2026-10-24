Describe 'Talks and demos' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'exports binary PNG through the frame cmdlet and infers graphics from extensions' {
        $path = Join-Path $TestDrive 'slide.png'
        'Talk demo' | Export-ConsoleFrame -Format Png -Path $path -Preset Social -Theme HighContrast -Fit Content -Background Solid -BackgroundColor '#25364a' -PassThru | Should -BeOfType ([IO.FileInfo])
        $bytes = [IO.File]::ReadAllBytes($path)
        [Convert]::ToBase64String($bytes[0..7]) | Should -Be 'iVBORw0KGgo='
        $raw = 'Pipeline PNG' | Export-ConsoleFrame -Format Png
        ($raw -is [byte[]]) | Should -BeTrue
    }

    It 'writes chapter slides, source-time PNG, GIF and APNG through the installed export surface' {
        $cast = Join-Path $TestDrive 'input.cast'
        [IO.File]::WriteAllLines($cast, @(
            '{"version":2,"width":30,"height":4,"title":"Talk demo"}',
            '[0,"m","First"]',
            '[0.1,"o","one"]',
            '[0.2,"o","\r\nready"]',
            '[1,"m","Second"]',
            '[1.1,"o","\r\ntwo"]'
        ))
        $slides = Join-Path $TestDrive 'slides'
        $files = @(Export-ConsoleRecording $cast -Format Png -PerChapter -Destination $slides -Preset Slide16x9 -PassThru)
        $files.Count | Should -Be 2
        $files[0].Name | Should -Be 'slide-001.png'
        Export-ConsoleRecording $cast -Format Png -At '00:00:00.2' -Preset Social -Fit Terminal -Background Gradient -BackgroundColor '#142238' -BackgroundEndColor '#090e17' -Destination (Join-Path $TestDrive 'at.png')
        Export-ConsoleRecording $cast -Format Png -Chapter First -Destination (Join-Path $TestDrive 'chapter.png')
        foreach ($format in @('Gif', 'Apng')) {
            $target = Join-Path $TestDrive ('talk.' + $format.ToLowerInvariant())
            Export-ConsoleRecording $cast -Format $format -Destination $target -LoopCount 2 -IdleLimit 0.5
            (Get-Item $target).Length | Should -BeGreaterThan 100
        }
        { Export-ConsoleRecording $cast -Format Gif -At '00:00:01' -Destination (Join-Path $TestDrive 'bad.gif') } | Should -Throw '*PNG selector*'
        { Export-ConsoleRecording $cast -Format Png -At '00:00:01' -PerChapter -Destination $slides } | Should -Throw '*Choose one*'
    }

    It 'records typed commands in a real PowerShell child with a custom prompt' -Skip:([Environment]::OSVersion.Platform -eq 'Win32NT') {
        $demo = Join-Path $TestDrive 'demo.ps1'
        [IO.File]::WriteAllText($demo, @'
# demo: setup
function prompt { '任意> ' }; $secretSetup = 'do-not-record-setup'
# demo: wpm 1000
# demo: chapter Answer
Write-Host ('answer=' + (6 * 7))
# demo: wait answer=42
'@)
        $cast = Join-Path $TestDrive 'typed.cast'
        Invoke-ConsoleDemo -Path $demo -Destination $cast -Width 50 -Height 8 | Should -BeOfType ([IO.FileInfo])
        $text = Export-ConsoleRecording $cast -Format Text
        $text | Should -Match 'answer=42'
        $text | Should -Match '任意>'
        Get-Content $cast -Raw | Should -Not -Match 'do-not-record-setup'
        { Start-ConsoleDemo -Path $demo } | Should -Throw '*needs a terminal*'
    }
}
