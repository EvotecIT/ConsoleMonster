function Get-ErrorShapeRecords {
    <#
    .SYNOPSIS
    Obtains real compiled-cmdlet failures or a worker-created record without invocation metadata.
    .PARAMETER Shape
    The synthetic scenario.
    .PARAMETER Invocation
    Parsed source whose stable file name and extent are supplied by PowerShell.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string] $Shape, [Parameter(Mandatory)][scriptblock] $Invocation)
    if ($Shape -eq 'detached-record') { return [ConsoleMonsterErrorShapes.ErrorShapeFactory]::Detached() }
    if ($Shape -eq 'mixed-output') {
        & $Invocation $Shape 2>&1
        return
    }
    try { & $Invocation $Shape } catch { $_ }
}
