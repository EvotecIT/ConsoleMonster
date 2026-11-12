# ConsoleMonster.PowerShell.Display broker. Ownership is the installed ScriptBlock identity, not this comment.
[CmdletBinding()]
param(
    [switch] $Transcript,
    [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject
)
begin {
    $cmAutomaticTopLevel = $MyInvocation.CommandOrigin -eq 'Runspace'
    # The owning installation captures its exact Type, including private assembly load contexts.
    # A copied definition has no captured owner and delegates straight to native formatting.
    $cmOwnerType = $ExecutionContext.SessionState.PSVariable.GetValue('cmDisplayType')
    $cmDisplaySession = if ($null -ne $cmOwnerType) { $cmOwnerType::Begin($PSCmdlet, $cmAutomaticTopLevel, $Transcript.IsPresent) } else { $null }
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
                $cmNativePipeline = $cmOwnerType::CreateNativeSink($PSCmdlet, $cmAutomaticTopLevel, $Transcript.IsPresent)
                $cmNativePipeline.Begin()
            }
            foreach ($cmOriginal in $cmOperation.Objects) { $cmNativePipeline.Process($cmOriginal) }
        }
    }
}
process {
    try {
        if ($null -eq $cmDisplaySession) {
            if ($null -eq $cmNativePipeline) {
                $cmNativePipeline = { Microsoft.PowerShell.Core\Out-Default -Transcript:$Transcript }.GetSteppablePipeline($MyInvocation.CommandOrigin)
                $cmNativePipeline.Begin($PSCmdlet)
            }
            $cmNativePipeline.Process($InputObject)
        } else { . $cmDispatch ($cmDisplaySession.Receive($InputObject)) }
    }
    catch {
        if ($null -ne $cmDisplaySession) { $cmDisplaySession.Dispose() }
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.Dispose() }
        throw
    }
}
end {
    # The host appends Out-Default to each command it runs and then reads $? (pwsh -Command turns it into the
    # exit code). Upstream has finished here, so $? is still the caller's status, but every statement below
    # resets it. Hand a failure back with an error that is neither shown nor recorded.
    $cmSucceeded = $?
    try {
        if ($null -ne $cmDisplaySession) { . $cmDispatch ($cmDisplaySession.Complete()) }
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.End() }
    } finally {
        if ($null -ne $cmDisplaySession) { $cmDisplaySession.Dispose() }
        if ($null -ne $cmNativePipeline) { $cmNativePipeline.Dispose() }
    }
    if (-not $cmSucceeded) {
        Microsoft.PowerShell.Utility\Write-Error -Message 'The command before Out-Default failed.' -ErrorAction Ignore
    }
}
