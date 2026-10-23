# ConsoleMonster Help proxy. Parameters come from this runtime's native help function.
begin {
    try {
        $cmNative = [ConsoleMonster.PowerShell.HelpHostFormatting]::NativeHelp()
        if ([ConsoleMonster.PowerShell.FormatCommandRouter]::IsDirectHost($PSCmdlet, [ConsoleMonster.PowerShell.ConsoleFormattingFeature]::Help) -and
            -not $PSBoundParameters.ContainsKey('Online') -and -not $PSBoundParameters.ContainsKey('ShowWindow')) {
            if ($PSCmdlet.ParameterSetName -eq 'AllUsersView' -and -not $Full) { $PSBoundParameters['Full'] = $true }
            [ConsoleMonster.PowerShell.HelpHostFormatting]::BeginPaging($PSCmdlet)
            $cmGetHelp = $ExecutionContext.InvokeCommand.GetCommand('Microsoft.PowerShell.Core\Get-Help', [System.Management.Automation.CommandTypes]::Cmdlet)
            $cmTarget = { & $cmGetHelp @PSBoundParameters }
        } else {
            $cmTarget = { & $cmNative @PSBoundParameters }
        }
        $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
        $cmPipeline.Begin($PSCmdlet)
    } catch {
        [ConsoleMonster.PowerShell.HelpHostFormatting]::EndPaging($PSCmdlet)
        throw
    }
}
process {
    try { $cmPipeline.Process($_) }
    catch {
        [ConsoleMonster.PowerShell.HelpHostFormatting]::EndPaging($PSCmdlet)
        throw
    }
}
end {
    try { $cmPipeline.End() }
    finally { [ConsoleMonster.PowerShell.HelpHostFormatting]::EndPaging($PSCmdlet) }
}
<#
.FORWARDHELPTARGETNAME Get-Help
.FORWARDHELPCATEGORY Cmdlet
#>
