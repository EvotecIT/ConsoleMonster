function Invoke-GridViewSession {
    <# .SYNOPSIS
    Invokes module contracts in a fresh initial command namespace; removing an entry never changes the parent session.
    #>
    param([string] $Script, [switch] $Native, [string] $ModulePath)
    $initial = [Management.Automation.Runspaces.InitialSessionState]::CreateDefault()
    if (-not $Native) {
        $initial.Commands.Remove('Out-GridView', [Management.Automation.Runspaces.SessionStateCmdletEntry])
    }
    $initial.Variables.Add([Management.Automation.Runspaces.SessionStateVariableEntry]::new('PSModuleAutoLoadingPreference', 'None', 'Isolated missing-command namespace'))
    $runspace = [runspacefactory]::CreateRunspace($Host, $initial)
    $runspace.Open()
    $ps = [powershell]::Create()
    $ps.Runspace = $runspace
    try {
        $module = if ($ModulePath) { $ModulePath } else { [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../ConsoleMonster.psd1')) }
        $null = $ps.AddScript('param($path, $body) $ErrorActionPreference = ''Stop''; Import-Module $path; & ([scriptblock]::Create($body))').AddArgument($module).AddArgument($Script)
        $result = @($ps.Invoke())
        if ($ps.HadErrors) { throw ($ps.Streams.Error | Out-String) }
        $result
    } finally {
        $ps.Dispose()
        $runspace.Dispose()
    }
}
