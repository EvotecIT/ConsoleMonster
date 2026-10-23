Describe 'Native log tails' {
    BeforeAll {
        $script:TailModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        Import-Module $script:TailModule -Force
        $null = Get-Command Watch-ConsoleLog -CommandType Cmdlet -ErrorAction Stop
        $script:TailIsWindows = $PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows
        function New-TailFixtureEvent {
            param([long] $Record, [int] $Level = 4, [string] $Message = 'entry')
            [pscustomobject]@{
                RecordId = $Record
                Id = 42
                ProviderName = 'Synthetic'
                Level = $Level
                TimeCreated = [datetime]::SpecifyKind([datetime]'2026-10-22T12:00:00', [DateTimeKind]::Utc).AddSeconds($Record)
                Message = $Message
            }
        }
        function Invoke-TailFixtureEvents {
            param([object[]] $Replies, [int] $Tail, [int] $Count)
            $responses = [Collections.Generic.Queue[object]]::new()
            foreach ($reply in $Replies) { $responses.Enqueue($reply) }
            $queries = [Collections.Generic.List[object]]::new()
            $previous = Get-Item -LiteralPath Function:global:Get-WinEvent -ErrorAction SilentlyContinue
            $source = {
                [CmdletBinding()]
                param([string] $LogName, [int] $MaxEvents, [switch] $Oldest, [string] $FilterXPath)
                $queries.Add([pscustomobject]@{ LogName = $LogName; MaxEvents = $MaxEvents; Oldest = $Oldest.IsPresent; FilterXPath = $FilterXPath })
                foreach ($record in $responses.Dequeue()) { $record }
            }.GetNewClosure()
            try {
                Set-Item -LiteralPath Function:global:Get-WinEvent -Value $source
                $lines = @(Watch-ConsoleLog -LogName 'ConsoleMonster-Synthetic' -Tail $Tail -Count $Count -Interval 0.05 -Plain)
                [pscustomobject]@{ Lines = $lines; Queries = $queries.ToArray(); Remaining = $responses.Count }
            } finally {
                if ($previous) { Set-Item -LiteralPath Function:global:Get-WinEvent -Value $previous.ScriptBlock }
                else { Remove-Item -LiteralPath Function:global:Get-WinEvent }
            }
        }
    }

    It 'returns the requested completed history as ordinary strings without interpreting markup' {
        $path = Join-Path $TestDrive 'literal [name].log'
        [IO.File]::WriteAllText($path, "INFO first`nWARN [red]literal[/] | data`nERROR last`nunfinished", [Text.UTF8Encoding]::new($true))
        $lines = @(Watch-ConsoleLog -Path $path -Tail 2 -Count 1 -Plain)
        $lines | Should -Be @('WARN [red]literal[/] | data', 'ERROR last')
        @($lines | Where-Object { $_ -isnot [string] }) | Should -BeNullOrEmpty
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
        [IO.File]::Delete($path)
        Test-Path -LiteralPath $path | Should -BeFalse
    }

    It 'filters literally and does not add highlighting or terminal controls to piped output' {
        $path = Join-Path $TestDrive 'filter.log'
        [IO.File]::WriteAllText($path, "INFO [db] ready`nWARN [DB] retry`nERROR unrelated`n", [Text.UTF8Encoding]::new($false))
        $lines = @(Watch-ConsoleLog -LiteralPath $path -Filter '[db]' -Highlight ready -Count 1 | ForEach-Object { $_ })
        $lines | Should -Be @('INFO [db] ready', 'WARN [DB] retry')
        ($lines -join '') | Should -Not -Match ([regex]::Escape([string][char]27))
    }

    It 'renders an explicit timestamp format at both widths without changing original plain source lines' {
        $path = Join-Path $TestDrive 'timestamp.log'
        $original = '2026-10-22T09:31:09+02:00 WARN [red]literal[/]'
        [IO.File]::WriteAllText($path, $original + "`n", [Text.UTF8Encoding]::new($false))
        @(Watch-ConsoleLog -Path $path -TimestampFormat 'yyyy-MM-dd HH:mm:ss zzz' -Count 1 -Plain) | Should -Be @($original)
        foreach ($set in (Get-Command Watch-ConsoleLog).ParameterSets) {
            $set.Parameters.Name | Should -Contain 'TimestampFormat'
        }
        foreach ($width in 60, 120) {
            $profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width, 4)
            $target = [ConsoleMonster.Output.RecordingTarget]::new($profile, '')
            Use-ConsoleTarget -Target $target {
                Watch-ConsoleLog -Path $path -TimestampFormat 'yyyy-MM-dd HH:mm:ss zzz' -Count 1
            }
            $target.Output | Should -Match '2026-10-22 09:31:09 \+02:00'
            $target.Output | Should -Match '\[red\]literal\[/\]'
        }
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
    }

    It 'uses the shared encoding names for a BOMless file and gives an existing BOM precedence' {
        $path = Join-Path $TestDrive 'encoding.log'
        [IO.File]::WriteAllText($path, "INFO Zażółć`n", [Text.UnicodeEncoding]::new($false, $false))
        @(Watch-ConsoleLog -Path $path -Encoding unicode -Count 1 -Plain) | Should -Be @('INFO Zażółć')
        [IO.File]::WriteAllText($path, "WARN 日本語`n", [Text.UTF32Encoding]::new($true, $true))
        @(Watch-ConsoleLog -Path $path -Encoding ascii -Count 1 -Plain) | Should -Be @('WARN 日本語')
    }

    It 'does not acquire a configured keyboard source in plain mode, including empty snapshots' {
        $path = Join-Path $TestDrive 'empty.log'
        [IO.File]::WriteAllText($path, '')
        $previous = [ConsoleMonster.Input.ConsoleInput]::Override
        $keys = [ConsoleMonster.Input.ScriptedInput]::new('q')
        try {
            [ConsoleMonster.Input.ConsoleInput]::Override = $keys
            @(Watch-ConsoleLog -Path $path -Count 2 -Interval 0.05 -Plain) | Should -BeNullOrEmpty
            $keys.Remaining | Should -Be 1
            [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
        } finally { [ConsoleMonster.Input.ConsoleInput]::Override = $previous }
    }

    It 'rejects a non-file provider before acquiring keyboard input' {
        { Watch-ConsoleLog -Path 'Variable:PSVersionTable' -Count 1 -Plain -ErrorAction Stop } |
            Should -Throw -ExpectedMessage '*FileSystem provider*'
        [ConsoleMonster.Input.ConsoleInput]::IsAcquired | Should -BeFalse
    }

    It 'reads synthetic event history oldest first and follows each newer record once' {
        if (-not $script:TailIsWindows) { Set-ItResult -Skipped -Because 'Windows event-log source'; return }
        $one = New-TailFixtureEvent 1 -Message 'first'
        $two = New-TailFixtureEvent 2 -Level 3 -Message 'second'
        $three = New-TailFixtureEvent 3 -Level 2 -Message '[red]third[/]'
        $result = Invoke-TailFixtureEvents -Replies @(@($two), @($two, $one), @($three), @($three)) -Tail 2 -Count 2
        $lines = $result.Lines
        $lines.Count | Should -Be 3
        $lines[0] | Should -Match '\[INFO\] \[Synthetic#42\] first$'
        $lines[1] | Should -Match '\[WARN\] \[Synthetic#42\] second$'
        $lines[2] | Should -Match '\[ERR\] \[Synthetic#42\] \[red\]third\[/\]$'
        $result.Remaining | Should -Be 0
        $result.Queries.Count | Should -Be 4
        @($result.Queries | Where-Object { $_.Oldest -and $_.FilterXPath -eq '*[System[EventRecordID > 2]]' }).Count | Should -Be 1
    }

    It 'does not discard the first event arriving after an initially empty zero-history tail' {
        if (-not $script:TailIsWindows) { Set-ItResult -Skipped -Because 'Windows event-log source'; return }
        $one = New-TailFixtureEvent 1 -Message 'arrived'
        $result = Invoke-TailFixtureEvents -Replies @(@(), @($one), @($one)) -Tail 0 -Count 2
        $lines = $result.Lines
        $lines.Count | Should -Be 1
        $lines[0] | Should -Match 'arrived$'
        $result.Remaining | Should -Be 0
    }

    It 'follows records after an observed clear without skipping them as initial history' {
        if (-not $script:TailIsWindows) { Set-ItResult -Skipped -Because 'Windows event-log source'; return }
        $old = New-TailFixtureEvent 10 -Message 'old'
        $new = New-TailFixtureEvent 1 -Message 'after-clear'
        $result = Invoke-TailFixtureEvents -Replies @(@($old), @($new), @($new)) -Tail 0 -Count 2
        $lines = $result.Lines
        $lines.Count | Should -Be 1
        $lines[0] | Should -Match 'after-clear$'
        @($result.Queries | Where-Object { $_.Oldest -and $_.FilterXPath -eq '*[System[EventRecordID > 0]]' }).Count | Should -Be 1
    }

    It 'rejects event-log wildcards rather than mixing unrelated record ID sequences' {
        $failure = $null
        try { Watch-ConsoleLog -LogName '*' -Count 1 -Plain -ErrorAction Stop }
        catch { $failure = $_ }
        $failure.FullyQualifiedErrorId | Should -BeLike 'ParameterArgumentValidationError,*'
    }

    It 'restores the exact native console mode in fresh hosts for plain, live and source construction failure' {
        if (-not $script:TailIsWindows) { Set-ItResult -Skipped -Because 'An isolated native Windows console is required'; return }
        $repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
        $hostPath = Join-Path $repository 'ConsoleMonster.TestHost/bin/Debug/net472/ConsoleMonster.TestHost.exe'
        if (-not (Test-Path -LiteralPath $hostPath)) {
            $hostPath = Join-Path $repository 'ConsoleMonster.TestHost/bin/Release/net472/ConsoleMonster.TestHost.exe'
        }
        if (-not (Test-Path -LiteralPath $hostPath)) { throw 'Build ConsoleMonster.TestHost for net472 before the native tail mode contract.' }
        $runtime = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
        $fixture = Join-Path $PSScriptRoot 'Support/Assert-TailModeHost.ps1'
        foreach ($scenario in 'PlainCount', 'LiveCount', 'SourceConstructionFailure') {
            $receipt = Join-Path $TestDrive "$scenario.json"
            $command = "& '{0}' -ResultPath '{1}' -Scenario '{2}'" -f $fixture.Replace("'", "''"), $receipt.Replace("'", "''"), $scenario
            $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))
            $arguments = @('host', $runtime, '-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand', $encoded)
            $quoted = foreach ($argument in $arguments) {
                [ConsoleMonster.Interactive.CommandLine]::Quote($argument, [ConsoleMonster.Interactive.CommandStyle]::Windows)
            }
            $info = [Diagnostics.ProcessStartInfo]::new($hostPath, ($quoted -join ' '))
            $info.UseShellExecute = $false
            $info.CreateNoWindow = $true
            $info.RedirectStandardOutput = $true
            $info.RedirectStandardError = $true
            $child = [Diagnostics.Process]::Start($info)
            try {
                $stdout = $child.StandardOutput.ReadToEndAsync()
                $stderr = $child.StandardError.ReadToEndAsync()
                if (-not $child.WaitForExit(45000)) { throw "Native tail host timed out: $scenario" }
                $details = if (Test-Path -LiteralPath $receipt) { Get-Content -LiteralPath $receipt -Raw } else { 'No receipt.' }
                $child.ExitCode | Should -Be 0 -Because "$details $($stdout.Result) $($stderr.Result)"
                $result = $details | ConvertFrom-Json
                $result.Status | Should -Be 'Passed'
                $result.Scenario | Should -Be $scenario
                $result.ModeAfter | Should -Be $result.ModeBefore
                $result.SourceFailed | Should -Be ($scenario -eq 'SourceConstructionFailure')
            } finally {
                if (-not $child.HasExited) { $child.Kill() }
                $child.Dispose()
            }
        }
    }
}
