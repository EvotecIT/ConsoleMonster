# ConsoleMonster FormatCommands proxy. Parameters come from this runtime's native cmdlet metadata.
begin {
    $cmNative = $ExecutionContext.InvokeCommand.GetCommand('Microsoft.PowerShell.Utility\__COMMAND__', [System.Management.Automation.CommandTypes]::Cmdlet)
    $cmCapture = $null
    $cmPipeline = $null
    $cmExplicitInput = $PSBoundParameters.ContainsKey('InputObject')
    $cmAdapter = [System.Management.Automation.CmdletInfo]::new('Out-ConsoleFormatCommand', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleFormatCommand'))
    if (((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.FormatCommandRouter'))::IsDirectHost($PSCmdlet)) {
        if ('__COMMAND__' -ne 'Format-Wide') {
            $cmCaptureType = (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.NativeFormatCapture', $true)
            $cmCapture = [Activator]::CreateInstance($cmCaptureType, [object[]] @($PSCmdlet, '__COMMAND__'.Substring(7), $PSBoundParameters))
        }
        $cmTarget = { & $cmNative @PSBoundParameters | & $cmAdapter }
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
            $cmTarget = { & $cmNative @cmParameters | & $cmAdapter -Capture $cmCapture }
            $cmPipeline = $cmTarget.GetSteppablePipeline($MyInvocation.CommandOrigin)
            $cmPipeline.Begin($PSCmdlet)
        }
        $cmCapture.BeginItem($cmSource)
    }
    $cmPipeline.Process($_)
}
end { if ($null -ne $cmPipeline) { $cmPipeline.End() } }
