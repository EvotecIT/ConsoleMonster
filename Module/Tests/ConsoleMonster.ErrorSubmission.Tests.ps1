Describe 'Compact error same-submission fallback' {
    It 'keeps native error frames and transcripts when <Scenario> changes after BeginProcessing' -Skip:($env:OS -ne 'Windows_NT') -ForEach @(
        @{ Scenario = 'Transcript' }
        @{ Scenario = 'Disable' }
    ) {
        $runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $probe = Join-Path $PSScriptRoot 'Support/Measure-ErrorSubmission.ps1'
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $receipt = Join-Path $TestDrive "$Scenario.json"
        $info = [Diagnostics.ProcessStartInfo]::new($runtime)
        $info.Arguments = "-NoLogo -NoProfile -NonInteractive -File `"$probe`" -ModulePath `"$module`" -ReceiptPath `"$receipt`" -Scenario $Scenario"
        $info.UseShellExecute = $true
        $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $process = [Diagnostics.Process]::Start($info)
        try {
            if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Console error regression timed out.' }
            $result = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
            if ($process.ExitCode -ne 0 -or -not $result.Passed) { throw $result.Error }
        } finally { $process.Dispose() }
    }
}
