param($__cmInvoke, $__cmAdvanced)
{
    __BINDING__
    param(__PARAMETERS__)
    # Capture the caller's success before any pipeline runs. The native prompt engine
    # preserves $? around evaluation; no command or PSReadLine handler is replaced.
    $__cmSucceeded = $?
    $__cmFrame = $__cmInvoke.Invoke($__cmSucceeded, $MyInvocation, $Host, @(Microsoft.PowerShell.Utility\Get-PSCallStack))
    try {
        if ($null -ne $__cmFrame.Text) {
            $__cmFrame.Text
        } else {
            $__cmParameters = @{} + $PSBoundParameters
            $__cmArguments = $args
            if ($__cmAdvanced) {
                # The wrapper's native runtime owns caller variable captures. Forwarding these again
                # would append each record twice when the caller uses a +variable capture.
                foreach ($__cmVariable in 'ErrorVariable', 'OutVariable', 'WarningVariable', 'InformationVariable', 'PipelineVariable') {
                    $__cmParameters.Remove($__cmVariable)
                }
            }
            # Native function dispatch carries caller stream redirections and the original error identity.
            # Evaluate the status setter in the command expression: a separate statement
            # would reset $? before the original function's first statement can capture it.
            & ($__cmFrame.AtCaller({ & ($__cmFrame.Original()) @__cmParameters __ARGUMENTS__ }))
        }
    } finally {
        $__cmFrame.Restore()
    }
}.GetNewClosure()
