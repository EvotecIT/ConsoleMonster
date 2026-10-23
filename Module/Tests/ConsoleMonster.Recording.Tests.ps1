Describe 'Whole-session recording' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
    }

    It 'records a real child and exports every session format' {
        $html = Join-Path $TestDrive 'native.html'
        Invoke-ConsoleRecording {
            Add-ConsoleRecordingMarker 'Native program'
            git --version
            Write-Host 'password=synthetic-test-value'
            Write-Host 'Session complete' -ForegroundColor Green
        } -Destination $html -Width 40 -Height 8 -Name 'Test recording' | Out-Null

        $cast = [IO.Path]::ChangeExtension($html, '.cast')
        $lines = @(Get-Content -LiteralPath $cast)
        ($lines[0] | ConvertFrom-Json).version | Should -Be 2
        ($lines -join "`n") | Should -Not -Match 'synthetic-test-value'
        ($lines -join "`n") | Should -Match '\[REDACTED\]'
        foreach ($line in $lines | Select-Object -Skip 1) {
            $recordedEvent = ConvertFrom-Json -InputObject $line
            $recordedEvent.Count | Should -Be 3
        }
        Get-Content -LiteralPath $html -Raw | Should -Match 'ConsoleMonsterPlayer'
        $data = Export-ConsoleRecording -Path $cast -Format PlayerData | ConvertFrom-Json
        $data.width | Should -Be 40
        $data.markers[0][1] | Should -Be 'Native program'
        Export-ConsoleRecording -Path $cast -Format Text | Should -Match 'Session complete'
        Export-ConsoleRecording -Path $cast -Format Markdown | Should -Match '```'
        Export-ConsoleRecording -Path $cast -Format Svg | Should -Match '@keyframes cm-play'
        Export-ConsoleRecording -Path $cast -FinalFrame | Should -Match '<svg'
        $exported = Export-ConsoleRecording -Path $cast -Format Asciicast
        (($exported -split "`n")[0] | ConvertFrom-Json).version | Should -Be 2
    }

    It 'labels the limited fallback and records PowerShell streams and chapters' {
        $destination = Join-Path $TestDrive 'fallback.html'
        Invoke-ConsoleRecording {
            Add-ConsoleRecordingMarker 'Fallback chapter'
            Write-Host 'Host output'
            Write-Warning 'Warning output'
            'Pipeline output'
            Write-Host 'token=synthetic-fallback-secret'
        } -Destination $destination -InProcess -WarningAction SilentlyContinue | Out-Null
        $cast = [IO.Path]::ChangeExtension($destination, '.cast')
        $raw = Get-Content -LiteralPath $cast -Raw
        $raw | Should -Match 'in-process PowerShell output only'
        $raw | Should -Not -Match 'synthetic-fallback-secret'
        $text = Export-ConsoleRecording -Path $cast -Format Text
        $text | Should -Match 'Host output'
        $text | Should -Match 'Warning output'
        $text | Should -Match 'Pipeline output'
        (Export-ConsoleRecording -Path $cast -Format PlayerData | ConvertFrom-Json).markers[0][1] | Should -Be 'Fallback chapter'
    }

    It 'stops from inside the recorded child and leaves a valid cast' {
        $cast = Join-Path $TestDrive 'stop.cast'
        Invoke-ConsoleRecording {
            Write-Host 'Before stopping'
            Stop-ConsoleRecording
            Start-Sleep -Seconds 10
            Write-Host 'After stopping'
        } -Destination $cast | Out-Null
        $text = Export-ConsoleRecording -Path $cast -Format Text
        $text | Should -Match 'Before stopping'
        $text | Should -Not -Match 'After stopping'
        Test-Path -LiteralPath ($cast + '.json') | Should -BeTrue
    }

    It 'refuses an existing destination before executing the script' {
        $destination = Join-Path $TestDrive 'existing.html'
        Set-Content -LiteralPath $destination -Value 'Keep this file'
        { Invoke-ConsoleRecording { throw 'Script must not run' } -Destination $destination } | Should -Throw '*already exists*'
        Get-Content -LiteralPath $destination -Raw | Should -Match 'Keep this file'
        Test-Path -LiteralPath ([IO.Path]::ChangeExtension($destination, '.cast')) | Should -BeFalse
    }

    It 'relays an interactive Start session through the real terminal input host' {
        $module = (Get-Module ConsoleMonster | Select-Object -First 1).Path
        $runtime = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
        $inner = Join-Path $TestDrive 'interactive.cast'
        $outer = Join-Path $TestDrive 'interactive-host.cast'
        $script = "Import-Module '$($module.Replace("'", "''"))'; Start-ConsoleRecording -Path '$($inner.Replace("'", "''"))' -Width 50 -Height 10; [Console]::WriteLine('OUTER-DONE')"
        $options = [ConsoleMonster.Sessions.SessionRecordingOptions]::new()
        $options.Path = $outer
        $options.Shell = $runtime
        $options.Width = 50
        $options.Height = 10
        $options.Timeout = [TimeSpan]::FromSeconds(30)
        $options.Arguments.Add('-NoProfile')
        $options.Arguments.Add('-EncodedCommand')
        $options.Arguments.Add([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script)))
        $recorder = [ConsoleMonster.Sessions.SessionRecorder]::Start($options)
        try {
            Start-Sleep -Milliseconds 1200
            $recorder.SendInput("[Console]::WriteLine('INTERACTIVE-CHECK'); Add-ConsoleRecordingMarker 'Interactive'; Stop-ConsoleRecording`r")
            $recorder.WaitForExit()
        } finally {
            $recorder.Dispose()
        }
        $recorder.ExitCode | Should -Be 0
        $text = Export-ConsoleRecording -Path $inner -Format Text
        $text | Should -Match 'INTERACTIVE-CHECK'
        $text | Should -Not -Match 'OUTER-DONE'
        (Export-ConsoleRecording -Path $inner -Format PlayerData | ConvertFrom-Json).markers[0][1] | Should -Be 'Interactive'
    }

    It 'surfaces a child failure and retains its recording' {
        $cast = Join-Path $TestDrive 'failed.cast'
        { Invoke-ConsoleRecording { Write-Host 'Failure recorded'; exit 7 } -Destination $cast -ErrorAction Stop } | Should -Throw '*code 7*'
        Export-ConsoleRecording -Path $cast -Format Text | Should -Match 'Failure recorded'
    }

    It 'applies custom redaction and the byte cap' {
        $cast = Join-Path $TestDrive 'capped.cast'
        Invoke-ConsoleRecording {
            Write-Host 'private-sample'
            Write-Host ('x' * 3000)
        } -Destination $cast -MaxBytes 1024 -Redact 'private-sample' -WarningAction SilentlyContinue | Out-Null
        (Get-Item -LiteralPath $cast).Length | Should -BeLessOrEqual 1024
        Get-Content -LiteralPath $cast -Raw | Should -Not -Match 'private-sample'
        (Get-Content -LiteralPath ($cast + '.json') -Raw | ConvertFrom-Json).sizeLimitReached | Should -BeTrue
    }
}
