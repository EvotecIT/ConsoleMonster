# ConsoleMonster.PowerShell.Display broker. Ownership is the installed ScriptBlock identity, not this comment.
[CmdletBinding()]
param(
    [switch] $Transcript,
    [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject
)
begin {
    $cmAutomaticTopLevel = $MyInvocation.CommandOrigin -eq 'Runspace'
    $cmDisplaySession = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Begin($PSCmdlet, $cmAutomaticTopLevel, $Transcript.IsPresent)
    $cmNativePipeline = $null
    $cmDispatch = {
        param($cmOperations)
        foreach ($cmOperation in $cmOperations) {
            if ($cmOperation.IsRich) {
                if ($null -ne $cmNativePipeline) {
                    $cmNativePipeline.End()
                    $cmNativePipeline.Dispose()
                    $cmNativePipeline = $null
                }
                if ($cmDisplaySession.Render($cmOperation)) { continue }
            }
            if ($null -eq $cmNativePipeline) {
                $cmNativePipeline = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::CreateNativeSink($PSCmdlet, $cmAutomaticTopLevel, $Transcript.IsPresent)
                $cmNativePipeline.Begin()
            }
            foreach ($cmOriginal in $cmOperation.Objects) { $cmNativePipeline.Process($cmOriginal) }
        }
    }
}
process {
    try { . $cmDispatch ($cmDisplaySession.Receive($InputObject)) }
    catch {
        $cmDisplaySession.Dispose()
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.Dispose() }
        throw
    }
}
end {
    try {
        . $cmDispatch ($cmDisplaySession.Complete())
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.End() }
    } finally {
        $cmDisplaySession.Dispose()
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.Dispose() }
    }
}
