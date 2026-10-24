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
