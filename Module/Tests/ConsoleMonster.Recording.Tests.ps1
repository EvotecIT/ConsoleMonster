Describe 'Whole-session recording' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
        if (-not ('RecordingOutputObserver' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'Support/RecordingOutputObserver.cs') }
        if (-not ('RecordingRelayProbe' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'Support/RecordingRelayProbe.cs') }
        if (-not ('RecordingCleanupProbe' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'Support/RecordingCleanupProbe.cs') }
    }

    It 'bounds a slow live relay, yields between drain passes and releases blocked output on cancellation' {
        $assembly = (Get-Command Start-ConsoleRecording).ImplementingType.Assembly
        { [RecordingRelayProbe]::Validate($assembly.GetType('ConsoleMonster.PowerShell.RecordingOutputRelay', $true)) } | Should -Not -Throw
    }

    It 'restores input before finalization and attempts every cleanup after a disposal failure' {
        $command = (Get-Command Start-ConsoleRecording).ImplementingType.BaseType
        { [RecordingCleanupProbe]::Validate($command) } | Should -Not -Throw
    }

    It 'relays native Unix signals, resizes and no-echo input and restores the terminal and title' -Skip:([Environment]::OSVersion.Platform -eq 'Win32NT') {
        $probe = Join-Path $PSScriptRoot 'Support/recording_terminal_pty.py'
        $module = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $output = & python3 $probe $module (Join-Path $TestDrive 'native-terminal') 2>&1
        $LASTEXITCODE | Should -Be 0 -Because ($output -join "`n")
    }

    It 'keeps the recording title on a final frame and honors an explicit override' {
        $cast = Join-Path $TestDrive 'named.cast'
        [IO.File]::WriteAllLines($cast, @('{"version":2,"width":40,"height":4,"title":"Build report"}', '[0,"o","Complete"]'))
        [xml] $named = Export-ConsoleRecording -Path $cast -FinalFrame -WindowChrome
        [string] $named.svg.title | Should -Be 'Build report'
        [xml] $custom = Export-ConsoleRecording -Path $cast -FinalFrame -WindowChrome -Title 'Custom report'
        [string] $custom.svg.title | Should -Be 'Custom report'
    }

    It 'uses a visible redaction label without losing surrounding text' {
        $html = Join-Path $TestDrive 'redaction-label.html'
        $mask = ([char] 0x2022).ToString() * 7
        Invoke-ConsoleRecording {
            Write-Host 'API_TOKEN=synthetic-value (redacted)' -ForegroundColor DarkGray
        } -Destination $html -Width 60 -Height 4 -Redact 'synthetic-value' -RedactionText $mask | Out-Null
        $cast = [IO.Path]::ChangeExtension($html, '.cast')
        Get-Content $cast -Raw | Should -Not -Match 'synthetic-value'
        $text = Export-ConsoleRecording -Path $cast -Format Text
        $text | Should -Match ([regex]::Escape("API_TOKEN=$mask (redacted)"))
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
        $windows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
        # A headless Unix PTY has no emulator to answer ConsoleHost/PSReadLine cursor-position queries.
        $shell = if ($windows) { '' } else { " -Shell '/bin/sh'" }
        $script = "Import-Module '$($module.Replace("'", "''"))'; Start-ConsoleRecording -Path '$($inner.Replace("'", "''"))'$shell -Width 50 -Height 10; [Console]::WriteLine('OUTER-DONE')"
        $options = [ConsoleMonster.Sessions.SessionRecordingOptions]::new()
        $options.Path = $outer
        $options.Shell = $runtime
        $options.Width = 50
        $options.Height = 10
        $options.Timeout = [TimeSpan]::FromSeconds(30)
        $options.Environment['PS1'] = 'CM-READY> '
        $options.Arguments.Add('-NoProfile')
        $options.Arguments.Add('-EncodedCommand')
        $options.Arguments.Add([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script)))
        $observer = [RecordingOutputObserver]::new()
        $relay = [Delegate]::CreateDelegate([Action[string]], $observer, 'Receive')
        $recorder = [ConsoleMonster.Sessions.SessionRecorder]::Start($options, $relay)
        try {
            # Wait for the nested shell to finish startup rather than typing into its input-mode setup.
            $prompt = if ($windows) { '>' } else { 'CM-READY> ' }
            $observer.WaitFor($prompt, [TimeSpan]::FromSeconds(20)) | Should -BeTrue -Because $observer.Text
            if ($windows) {
                $commands = @(
                    @{ Text = "[Console]::WriteLine(); [Console]::WriteLine(('INTERACTIVE' + '-CHECK'))"; Output = 'INTERACTIVE-CHECK' },
                    @{ Text = "Add-ConsoleRecordingMarker 'Interactive'; Stop-ConsoleRecording"; Output = $null }
                )
                foreach ($command in $commands) {
                    foreach ($key in $command.Text.ToCharArray()) {
                        $recorder.SendInput([string] $key)
                        Start-Sleep -Milliseconds 10
                    }
                    $recorder.SendInput("`r")
                    # Command echo cannot satisfy this: the expected token is assembled only during execution.
                    # Stop after ConPTY has painted the result, rather than terminating it in the same command.
                    if ($command.Output) { $observer.WaitFor($command.Output, [TimeSpan]::FromSeconds(20)) | Should -BeTrue -Because $observer.Text }
                }
            } else {
                $label = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('Interactive'))
                $recorder.SendInput("printf '\033]777;ConsoleMonster;marker;$label\007'; printf '\r\nINTERACTIVE-CHECK\r\n'; exit`r")
            }
            $recorder.WaitForExit()
        } finally {
            $recorder.Dispose()
        }
        $recorder.ExitCode | Should -Be 0
        Export-ConsoleRecording -Path $outer -Format Text | Should -Match 'OUTER-DONE'
        $text = Export-ConsoleRecording -Path $inner -Format Text
        $text | Should -Match 'INTERACTIVE-CHECK' -Because ([IO.File]::ReadAllText($inner))
        $text | Should -Not -Match 'OUTER-DONE'
        (Export-ConsoleRecording -Path $inner -Format PlayerData | ConvertFrom-Json).markers[0][1] | Should -Be 'Interactive'
    }

    It 'surfaces a child failure and retains its recording' {
        $cast = Join-Path $TestDrive 'failed.cast'
        { Invoke-ConsoleRecording { Write-Host 'Failure recorded'; exit 7 } -Destination $cast -ErrorAction Stop } | Should -Throw '*code 7*'
        Export-ConsoleRecording -Path $cast -Format Text | Should -Match 'Failure recorded'
    }

    It 'surfaces a fallback script failure and retains its recording' {
        $cast = Join-Path $TestDrive 'fallback-failed.cast'
        { Invoke-ConsoleRecording { throw 'Fallback failure recorded' } -Destination $cast -InProcess -WarningAction SilentlyContinue -ErrorAction Stop } | Should -Throw '*Fallback failure recorded*'
        Export-ConsoleRecording -Path $cast -Format Text | Should -Match 'Fallback failure recorded'
    }

    It 'surfaces a fallback nonzero exit after finalizing its recording' {
        $cast = Join-Path $TestDrive 'fallback-exit.cast'
        { Invoke-ConsoleRecording { Write-Host 'Before fallback exit'; exit 7 } -Destination $cast -InProcess -WarningAction SilentlyContinue -ErrorAction Stop } | Should -Throw '*code 7*'
        Test-Path -LiteralPath ($cast + '.json') | Should -BeTrue
        Export-ConsoleRecording -Path $cast -Format Text | Should -Match 'Before fallback exit'
    }

    It 'stops a fallback script cleanly without recording later output' {
        $cast = Join-Path $TestDrive 'fallback-stop.cast'
        Invoke-ConsoleRecording {
            Write-Host 'Before fallback stop'
            Stop-ConsoleRecording
            Start-Sleep -Seconds 20
            throw 'After fallback stop'
        } -Destination $cast -InProcess -TimeoutSeconds 5 -WarningAction SilentlyContinue -ErrorAction Stop | Out-Null
        $text = Export-ConsoleRecording -Path $cast -Format Text
        $text | Should -Match 'Before fallback stop'
        $text | Should -Not -Match 'After fallback stop'
    }

    It 'applies custom redaction and the byte cap' {
        $cast = Join-Path $TestDrive 'capped.cast'
        Invoke-ConsoleRecording {
            Write-Host 'private-sample'
            Write-Host ('x' * 3000)
            Start-Sleep -Milliseconds 100
            Stop-ConsoleRecording
            Start-Sleep -Seconds 60
            throw 'Stop after cap failed'
        } -Destination $cast -MaxBytes 1024 -Redact 'private-sample' -TimeoutSeconds 15 -WarningAction SilentlyContinue | Out-Null
        (Get-Item -LiteralPath $cast).Length | Should -BeLessOrEqual 1024
        Get-Content -LiteralPath $cast -Raw | Should -Not -Match 'private-sample'
        (Get-Content -LiteralPath ($cast + '.json') -Raw | ConvertFrom-Json).sizeLimitReached | Should -BeTrue
    }

    It 'keeps redacted names out of every Invoke export title' {
        foreach ($format in 'html', 'json', 'svg', 'final') {
            $destination = Join-Path $TestDrive ('title-' + $format + $(if ($format -eq 'final') { '.svg' } else { '.' + $format }))
            $parameters = @{
                Destination = $destination
                Name = 'private-project'
                Redact = 'private-project'
                InProcess = $true
                WarningAction = 'SilentlyContinue'
            }
            if ($format -eq 'final') { $parameters.FinalFrame = $true }
            Invoke-ConsoleRecording { Write-Host 'Visible output' } @parameters | Out-Null
            Get-Content -LiteralPath $destination -Raw | Should -Not -Match 'private-project'
            Get-Content -LiteralPath $destination -Raw | Should -Match '\[REDACTED\]'
        }
    }
}
