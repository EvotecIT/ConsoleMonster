function Stop-TestChildProcess {
    <#
    .SYNOPSIS
    Stops a process started and retained by the test, including its descendants.
    .DESCRIPTION
    Use only with the Process object returned when this test started its child. Modern runtimes kill the tree directly;
    Windows PowerShell 5.1 uses Windows taskkill for the same owned PID. The function never searches by process name.
    .PARAMETER Process
    The test-owned live child process.
    .OUTPUTS
    None. Throws if the owned process cannot be stopped.
    .EXAMPLE
    Stop-TestChildProcess -Process $child
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)] [Diagnostics.Process] $Process)

    if ($Process.HasExited) { return }
    $treeMethod = $Process.GetType().GetMethod('Kill', [type[]] @([bool]))
    if ($null -ne $treeMethod) {
        $Process.Kill($true)
    } else {
        $info = [Diagnostics.ProcessStartInfo]::new('taskkill.exe', "/PID $($Process.Id) /T /F")
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $killer = [Diagnostics.Process]::Start($info)
        try {
            $output = $killer.StandardOutput.ReadToEndAsync()
            $errors = $killer.StandardError.ReadToEndAsync()
            if (-not $killer.WaitForExit(10000)) { throw 'Stopping the owned test process tree timed out.' }
            if ($killer.ExitCode -ne 0 -and -not $Process.HasExited) {
                throw "Could not stop the owned test process tree: $($output.Result)$($errors.Result)"
            }
        } finally { $killer.Dispose() }
    }
    if (-not $Process.WaitForExit(10000)) { throw 'The owned test process tree did not exit.' }
}
