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
end { $cmPipeline.End() }
