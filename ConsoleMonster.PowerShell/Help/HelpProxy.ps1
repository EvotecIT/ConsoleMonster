# ConsoleMonster Help proxy. Parameters come from this runtime's native help function.
begin {
    try {
        $cmNative = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting'))::NativeHelp()
        if (((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.FormatCommandRouter'))::IsDirectHost($PSCmdlet, ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.ConsoleFormattingFeature'))::Help) -and
            -not $PSBoundParameters.ContainsKey('Online') -and -not $PSBoundParameters.ContainsKey('ShowWindow')) {
            if ($PSCmdlet.ParameterSetName -eq 'AllUsersView' -and -not $Full) { $PSBoundParameters['Full'] = $true }
            ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting'))::BeginPaging($PSCmdlet)
            $cmGetHelp = $ExecutionContext.InvokeCommand.GetCommand('Microsoft.PowerShell.Core\Get-Help', [System.Management.Automation.CommandTypes]::Cmdlet)
            $cmTarget = { & $cmGetHelp @PSBoundParameters }
        } else {
            $cmTarget = { & $cmNative @PSBoundParameters }
        }
        $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
        $cmPipeline.Begin($PSCmdlet)
    } catch {
        ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting'))::EndPaging($PSCmdlet)
        throw
    }
}
process {
    try { $cmPipeline.Process($_) }
    catch {
        ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting'))::EndPaging($PSCmdlet)
        throw
    }
}
end {
    try { $cmPipeline.End() }
    finally { ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.HelpHostFormatting'))::EndPaging($PSCmdlet) }
}
<#
.FORWARDHELPTARGETNAME Get-Help
.FORWARDHELPCATEGORY Cmdlet
#>
