# ConsoleMonster\Out-ConsoleDefault: reversible host shim.
[CmdletBinding()]
param([switch] $Transcript, [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject)
begin {
    $cmTarget = if (Get-Command -Name 'ConsoleMonster\Out-ConsoleDefault' -CommandType Cmdlet -ErrorAction Ignore) {
        $cmHelpSink = $PSCmdlet
        { ConsoleMonster\Out-ConsoleDefault -Transcript:$Transcript -Shim -TopLevel:($MyInvocation.CommandOrigin -eq 'Runspace') -HelpSink $cmHelpSink }
    } else { { Microsoft.PowerShell.Core\Out-Default -Transcript:$Transcript } }
    $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
    $cmPipeline.Begin($PSCmdlet)
}
process { $cmPipeline.Process($InputObject) }
end { $cmPipeline.End() }
