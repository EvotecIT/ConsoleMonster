# ConsoleMonster unavailable Out-GridView fallback. Rendering and selection belong to Show-ConsoleGrid.
<#
.SYNOPSIS
Shows objects in the terminal grid when the native Out-GridView is unavailable.
.DESCRIPTION
Installed only by Enable-ConsoleFormatting -Feature GridView. Delegates to Show-ConsoleGrid; working native commands stay
unchanged. The terminal screen is synchronous. In jobs, transcripts and unattended sessions the existing table fallback
prints rows with a warning and returns nothing. Disable-ConsoleFormatting or module removal restores saved commands.
.PARAMETER InputObject
The objects to show, supplied explicitly or through the pipeline.
.PARAMETER Title
The title of the terminal grid.
.PARAMETER PassThru
Returns the original marked rows, or the focused row when none are marked.
.PARAMETER OutputMode
None browses without output; Single returns the focused row; Multiple returns the marked rows.
.PARAMETER Wait
Waits until the grid closes. The terminal grid also waits when this switch is omitted.
.EXAMPLE
Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
.OUTPUTS
The original selected objects, or no output for browse, cancellation or an unattended session.
#>
[CmdletBinding(DefaultParameterSetName = 'Wait')]
param(
    [Parameter(ValueFromPipeline = $true)] [psobject] $InputObject,
    [Parameter()] [string] $Title,
    [Parameter(ParameterSetName = 'PassThru')] [switch] $PassThru,
    [Parameter(Mandatory = $true, ParameterSetName = 'OutputMode')]
    [ValidateSet('None', 'Single', 'Multiple')] [string] $OutputMode,
    [Parameter(ParameterSetName = 'Wait')] [switch] $Wait
)
begin {
    $cmParameters = @{} + $PSBoundParameters
    $null = $cmParameters.Remove('Wait')
    $null = $cmParameters.Remove('PassThru')
    $cmParameters.OutputMode = if ($PassThru) { 'Multiple' } elseif ($PSBoundParameters.ContainsKey('OutputMode')) { $OutputMode } else { 'None' }
    $cmParameters.UnavailableFallback = $true
    # A terminal screen is synchronous, including browse-only mode. -Wait therefore needs no separate window lifetime.
    $cmCommand = $ExecutionContext.InvokeCommand.GetCommand('ConsoleMonster\Show-ConsoleGrid', [System.Management.Automation.CommandTypes]::Cmdlet)
    $cmPipeline = { & $cmCommand @cmParameters }.GetSteppablePipeline($MyInvocation.CommandOrigin)
    $cmPipeline.Begin($PSCmdlet)
}
process { $cmPipeline.Process($_) }
end { $cmPipeline.End() }
