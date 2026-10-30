Describe 'Recording export pacing' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'exports the same trim, holds and chapter speeds through the player and SVG' {
        $cast = Join-Path $TestDrive 'timing.cast'
        [IO.File]::WriteAllLines($cast, @(
            '{"version":2,"width":30,"height":4}',
            '[0,"o","A"]',
            '[0,"m","Install"]',
            '[4,"cm","{\"label\":\"\",\"hold\":3,\"pace\":null,\"chapter\":false}"]',
            '[6,"o","\rB"]',
            '[6,"m","Results"]',
            '[12,"o","\rC"]',
            '[14,"m","End"]'
        ))
        $settings = @{
            RecordingPath = $cast
            Pace = 'Source'
            Speed = 2
            ChapterSpeeds = @{ Install = 2 }
            IdleLimitSeconds = 2
            ChapterHoldSeconds = 1
            HoldLastFrameSeconds = 2
            TrimStartSeconds = 2
            TrimEndChapter = 'End'
        }
        $data = Export-ConsoleRecording @settings -Format PlayerData | ConvertFrom-Json
        $data.duration | Should -Be 10000
        $data.markers[0][0] | Should -Be 4000
        $data.markers[0][1] | Should -Be 'Results'
        Export-ConsoleRecording @settings -Format Svg | Should -Match 'animation:cm-play 10s'
        { Export-ConsoleRecording @settings -TrimStartChapter Results } | Should -Throw '*time or a chapter*'


    }

    It 'preserves fractional source boundaries through numeric second parameters' {
        $cast = Join-Path $TestDrive 'fractional.cast'
        [IO.File]::WriteAllLines($cast, @(
            '{"version":2,"width":30,"height":4}',
            '[0,"o","BEFORE-CUT"]',
            '[0.0508,"o","\rAT-CUT    "]',
            '[1,"o","\rAFTER-CUT "]'
        ))
        $data = Export-ConsoleRecording $cast -Format PlayerData -Pace Source -IdleLimitSeconds 0 -HoldLastFrameSeconds 0 -TrimEndSeconds 0.0508 | ConvertFrom-Json
        ($data.rows -join '') | Should -Match 'BEFORE-CUT'
        ($data.rows -join '') | Should -Not -Match 'AT-CUT'
        $data.duration | Should -Be 51
    }

    It 'adds an unnamed hold in an ordinary recorded script without sleeping' {
        $cast = Join-Path $TestDrive 'hold.cast'
        Invoke-ConsoleRecording {
            Write-Host 'Read this'
            Add-ConsoleRecordingMarker -HoldSeconds 3
            Write-Host 'Continue'
        } -Path $cast -InProcess -WarningAction SilentlyContinue | Out-Null
        $recording = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecording'))::Read($cast)
        $recording.Chapters.Count | Should -Be 0
        $recording.Markers.Count | Should -Be 1
        $recording.Markers[0].Hold.TotalSeconds | Should -Be 3
        $data = Export-ConsoleRecording $cast -Format PlayerData -Pace Source -IdleLimitSeconds 0.01 -HoldLastFrameSeconds 0 | ConvertFrom-Json
        $data.duration | Should -BeGreaterOrEqual 3000
        $roundTrip = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Sessions.SessionRecording'))::Read([IO.StringReader]::new($recording.ToAsciicast()))
        $roundTrip.Markers[0].Hold.TotalSeconds | Should -Be 3
    }
}
