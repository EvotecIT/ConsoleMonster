Describe 'Large run progress' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
    }

    It 'maps property and script-block metadata, preserves pipeline results and returns a structured final snapshot' {
        $code = @'
Import-Module '__MODULE__' -Force
$items = @([pscustomobject]@{Name='Alpha';Target='host-1';Group='DNS'}, [pscustomobject]@{Name='Beta';Target='host-2';Group='DNS'}, [pscustomobject]@{Name='Gamma';Target='host-3';Group='Storage'})
$run = New-ConsoleRun -Items $items -Name { $_.Name } -Target Target -Group Group -Title 'Batch checks' -NoLive -ProgressEvery 2 -ProgressSeconds 0 -ReportPath 'reports/checks.html'
try {
    Update-ConsoleRun $run -Index 0 -State Running -Phase 'collecting'
    Update-ConsoleRun $run -Index 0 -State Pass
    Update-ConsoleRun $run -Index 1 -State Warn -Message 'Partial response [literal]'
    Update-ConsoleRun $run -Index 1 -State Fail -Message 'duplicate ignored'
    $value = 42
} finally { $summary = Complete-ConsoleRun $run -PassThru }
"COUNTS=$($summary.Passed),$($summary.Warned),$($summary.Failed),$($summary.Skipped)"
"METADATA=$($summary.Items[0].Item.Name),$($summary.Items[1].Item.Target),$($summary.Items[2].Item.Group)"
"VALUE=$value"
Complete-ConsoleRun $run
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'COUNTS=1,1,0,1'
        $output | Should -Match 'METADATA=Alpha,host-2,Storage'
        $output | Should -Match 'VALUE=42'
        $output | Should -Match 'reports/checks.html'
        $output | Should -Not -Match 'duplicate ignored'
        $output | Should -Not -Match ([char]27)
    }

    It 'accepts an empty run and completes without inventing work' {
        $code = @'
Import-Module '__MODULE__' -Force
$run = New-ConsoleRun -Items @() -NoLive -ProgressSeconds 0
$summary = Complete-ConsoleRun $run -PassThru
"EMPTY=$($summary.Total),$($summary.Done),$($summary.IsFinished)"
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match 'EMPTY=0,0,True'
    }
    It 'receives timed host progress while a background worker is running' {
        $code = @'
Import-Module '__MODULE__' -Force
$run = New-ConsoleRun -Items @('slow check') -NoLive -ProgressEvery 0 -ProgressSeconds 0.1
$worker = [powershell]::Create()
$null = $worker.AddScript('param($run) Start-Sleep -Milliseconds 800; $run.Finished(0, [ConsoleMonster.Styling.StatusKind]::Pass, "worker done")').AddArgument($run)
try {
    $pending = $worker.BeginInvoke()
    Wait-ConsoleRun $run
    'WAIT_DONE'
    $null = $worker.EndInvoke($pending)
} finally {
    $worker.Dispose()
    Complete-ConsoleRun $run
}
'@
        $output = (Invoke-TestChild -Runtime $script:Runtime -Script $code.Replace('__MODULE__', $script:ModulePath)) -join "`n"
        $output | Should -Match '(?s)0/1.*?worker done.*?WAIT_DONE.*?1 / 1 finished'
        $output | Should -Not -Match ([char]27)
    }
}
