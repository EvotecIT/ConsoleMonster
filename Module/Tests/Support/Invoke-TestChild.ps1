function Invoke-TestChild {
    <#
    .SYNOPSIS
    Runs a script in a child process of a PowerShell runtime, with redirected output and a console of its own.

    .DESCRIPTION
    The child gets its own hidden console (CreateNoWindow), so output it writes straight to a console (coloured host
    writes, live areas) never lands on the console the tests run in, for example under an inline Invoke-ConsolePester
    view. Its output is UTF-8 whatever the code page of the parent console, and is returned as lines (standard output,
    then standard error). $LASTEXITCODE is set to the child's exit code.

    .PARAMETER Runtime
    The path of pwsh.exe or powershell.exe.

    .PARAMETER Script
    The script to run.

    .PARAMETER TimeoutSeconds
    How long to wait before the child is stopped (default 120).

    .EXAMPLE
    $lines = Invoke-TestChild -Runtime $runtime -Script "Import-Module '$module'; Write-ConsoleText 'hello'"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string] $Runtime,
        [Parameter(Mandatory)][string] $Script,
        [int] $TimeoutSeconds = 120
    )
    $text = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new(`$false)`n" + $Script
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($text))
    $info = [System.Diagnostics.ProcessStartInfo]::new($Runtime, "-NoLogo -NoProfile -NonInteractive -EncodedCommand $encoded")
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $info.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
    $process = [System.Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill()
            throw "The child process did not finish within $TimeoutSeconds seconds."
        }
        $process.WaitForExit()
        $global:LASTEXITCODE = $process.ExitCode
        # PowerShell writes its streams to a redirected standard error as CLIXML. As with 2>&1 on a native command, only the
        # error records join the output; information, progress and verbose records are dropped.
        $errorText = $stderr.Result
        if ($errorText.StartsWith('#< CLIXML')) {
            $errors = foreach ($document in [regex]::Split($errorText.Substring($errorText.IndexOf("`n") + 1), '(?=<Objs )')) {
                if (-not $document.Trim()) { continue }
                foreach ($item in @([System.Management.Automation.PSSerializer]::Deserialize($document))) {
                    if ($item.PSObject.TypeNames -match 'ErrorRecord') { "$item" }
                }
            }
            $errorText = if ($errors) { (@($errors) -join "`n") + "`n" } else { '' }
        }
        $all = ($stdout.Result + $errorText) -replace "`r", ''
        $all.TrimEnd("`n") -split "`n"
    } finally {
        $process.Dispose()
    }
}
