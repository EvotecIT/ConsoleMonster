param($__cmInvoke)
{
    # Capture the caller's success before any pipeline runs. The native prompt engine
    # preserves $? around evaluation; no command or PSReadLine handler is replaced.
    $__cmSucceeded = $?
    $__cmInvoke.Invoke($__cmSucceeded, $MyInvocation, $Host, @(Microsoft.PowerShell.Utility\Get-PSCallStack))
}.GetNewClosure()
