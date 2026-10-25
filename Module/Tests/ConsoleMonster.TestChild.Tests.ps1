Describe 'Child process stream diagnostics' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
    }

    It 'preserves plain stderr between serialized documents and retains the child exit code' {
        $script = @'
$info = [Management.Automation.InformationRecord]::new('progress-only', 'fixture')
$errorRecord = [Management.Automation.ErrorRecord]::new([InvalidOperationException]::new('serialized failure'), 'FixtureFailure', 'InvalidOperation', $null)
[Console]::Error.Write("#< CLIXML`n" + [Management.Automation.PSSerializer]::Serialize($info))
[Console]::Error.WriteLine('plain failure between documents')
[Console]::Error.Write("#< CLIXML`n" + [Management.Automation.PSSerializer]::Serialize($errorRecord))
[Console]::Error.WriteLine('plain failure after documents')
exit 7
'@
        $text = (Invoke-TestChild -Runtime ([Diagnostics.Process]::GetCurrentProcess().Path) -Script $script) -join "`n"
        $LASTEXITCODE | Should -Be 7
        $text | Should -Match 'serialized failure'
        $text | Should -Match 'plain failure between documents'
        $text | Should -Match 'plain failure after documents'
        $text | Should -Not -Match 'progress-only'
    }
}

Describe 'Owned test child cleanup' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:ChildRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
    }

    It 'stops a timed-out child and its descendant' {
        $receipt = Join-Path $TestDrive 'owned-child.json'
        $sleep = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes('Start-Sleep -Seconds 30'))
        $runtime = $script:ChildRuntime.Replace("'", "''")
        $path = $receipt.Replace("'", "''")
        $body = @"
`$info = [Diagnostics.ProcessStartInfo]::new('$runtime', '-NoLogo -NoProfile -NonInteractive -EncodedCommand $sleep')
`$info.UseShellExecute = `$false
`$info.CreateNoWindow = `$true
`$child = [Diagnostics.Process]::Start(`$info)
[IO.File]::WriteAllText('$path', (@{ Id = `$child.Id; StartTicks = `$child.StartTime.ToUniversalTime().Ticks.ToString() } | ConvertTo-Json -Compress))
Start-Sleep -Seconds 30
"@
        $owned = $null
        try {
            { Invoke-TestChild -Runtime $script:ChildRuntime -Script $body -TimeoutSeconds 5 } | Should -Throw '*did not finish within 5 seconds*'
            Test-Path -LiteralPath $receipt | Should -BeTrue -Because 'the owned descendant must have been started before the deadline'
            $owned = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
            $deadline = [DateTime]::UtcNow.AddSeconds(5)
            do {
                $remaining = Get-Process -Id $owned.Id -ErrorAction SilentlyContinue
                if ($null -eq $remaining -or $remaining.HasExited) { break }
                Start-Sleep -Milliseconds 50
            } while ([DateTime]::UtcNow -lt $deadline)
            ($null -eq $remaining -or $remaining.HasExited) | Should -BeTrue -Because 'a test timeout must not leave its project or service running'
        } finally {
            if ($null -ne $owned) {
                $remaining = Get-Process -Id $owned.Id -ErrorAction SilentlyContinue
                if ($null -ne $remaining -and -not $remaining.HasExited -and $remaining.StartTime.ToUniversalTime().Ticks.ToString() -eq $owned.StartTicks) {
                    Stop-TestChildProcess -Process $remaining
                }
            }
        }
    }
}
