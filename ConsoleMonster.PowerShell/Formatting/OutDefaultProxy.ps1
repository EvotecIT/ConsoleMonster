# ConsoleMonster\Out-ConsoleDefault: reversible host shim.
[CmdletBinding()]
param([switch] $Transcript, [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject)
begin {
    $cmCommand = Get-Command -Name 'ConsoleMonster\Show-Console' -CommandType Cmdlet -ErrorAction Ignore
    $cmTarget = if ($null -ne $cmCommand) {
        $cmAdapter = [System.Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', $cmCommand.ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))
        $cmHelpSink = $PSCmdlet
        { & $cmAdapter -Transcript:$Transcript -Shim -TopLevel:($MyInvocation.CommandOrigin -eq 'Runspace') -HelpSink $cmHelpSink }
    } else { { Microsoft.PowerShell.Core\Out-Default -Transcript:$Transcript } }
    $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
    $cmPipeline.Begin($PSCmdlet)
}
process { $cmPipeline.Process($InputObject) }
end {
    # The host appends Out-Default to each command it runs and then reads $? (pwsh -Command turns it into the
    # exit code; prompts show it). Upstream has finished here, so $? is still the caller's status, but every
    # statement in this block resets it. Hand a failure back with an error that is neither shown nor recorded.
    $cmSucceeded = $?
    $cmPipeline.End()
    if (-not $cmSucceeded) {
        Microsoft.PowerShell.Utility\Write-Error -Message 'The command before Out-Default failed.' -ErrorAction Ignore
    }
}
