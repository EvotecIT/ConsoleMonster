# ConsoleMonster FormatCommands proxy. Parameters come from this runtime's native cmdlet metadata.
begin {
    $cmNative = $ExecutionContext.InvokeCommand.GetCommand('Microsoft.PowerShell.Utility\__COMMAND__', [System.Management.Automation.CommandTypes]::Cmdlet)
    if ([ConsoleMonster.PowerShell.FormatCommandRouter]::IsDirectHost($PSCmdlet)) {
        $cmTarget = { & $cmNative @PSBoundParameters | ConsoleMonster\Out-ConsoleFormatCommand }
    } else {
        $cmTarget = { & $cmNative @PSBoundParameters }
    }
    $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
    $cmPipeline.Begin($PSCmdlet)
}
process { $cmPipeline.Process($_) }
end { $cmPipeline.End() }
