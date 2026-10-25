# ConsoleMonster FormatCommands proxy. Parameters come from this runtime's native cmdlet metadata.
begin {
    $cmNative = $ExecutionContext.InvokeCommand.GetCommand('Microsoft.PowerShell.Utility\__COMMAND__', [System.Management.Automation.CommandTypes]::Cmdlet)
    $cmCapture = $null
    $cmPipeline = $null
    $cmExplicitInput = $PSBoundParameters.ContainsKey('InputObject')
    if (((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.FormatCommandRouter'))::IsDirectHost($PSCmdlet)) {
        if ('__COMMAND__' -ne 'Format-Wide') {
            $cmCapture = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.NativeFormatCapture'))::new($PSCmdlet, '__COMMAND__'.Substring(7), $PSBoundParameters)
        }
        $cmTarget = { & $cmNative @PSBoundParameters | ConsoleMonster\Out-ConsoleFormatCommand }
    } else {
        $cmTarget = { & $cmNative @PSBoundParameters }
    }
    if ($null -eq $cmCapture) {
        $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
        $cmPipeline.Begin($PSCmdlet)
    }
}
process {
    if ($null -ne $cmCapture) {
        $cmSource = if ($cmExplicitInput) { $PSBoundParameters['InputObject'] } else { $_ }
        if ($null -eq $cmPipeline) {
            $cmParameters = $cmCapture.Prepare($cmSource)
            $cmTarget = { & $cmNative @cmParameters | ConsoleMonster\Out-ConsoleFormatCommand -Capture $cmCapture }
            $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
            $cmPipeline.Begin($PSCmdlet)
        }
        $cmCapture.BeginItem($cmSource)
    }
    $cmPipeline.Process($_)
}
end { if ($null -ne $cmPipeline) { $cmPipeline.End() } }
