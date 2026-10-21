function Get-EverydayValue {
    <#
    .SYNOPSIS
    Returns synthetic module data with no ConsoleMonster dependency.
    .EXAMPLE
    Get-EverydayValue
    #>
    [CmdletBinding()]
    param()
    [pscustomobject]@{ PSTypeName = 'EverydayViews.Server'; Name = 'alpha'; State = 'Pass'; Site = 'A'; HiddenDetail = 'private-detail' }
    [pscustomobject]@{ PSTypeName = 'EverydayViews.Server'; Name = 'beta'; State = 'Warning'; Site = 'B'; HiddenDetail = 'private-detail' }
}
Export-ModuleMember -Function Get-EverydayValue
