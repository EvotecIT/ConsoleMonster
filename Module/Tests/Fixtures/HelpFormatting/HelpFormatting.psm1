function Get-HelpFormattingValue {
    <#
    .SYNOPSIS
    Reads a synthetic formatting value.
    .DESCRIPTION
    HELP_DESCRIPTION is deliberately distinct from the examples and parameter descriptions.
    .PARAMETER Name
    HELP_NAME_DESCRIPTION identifies the synthetic value to read.
    .PARAMETER Count
    HELP_COUNT_DESCRIPTION limits the number of values.
    .EXAMPLE
    Get-HelpFormattingValue -Name alpha

    HELP_EXAMPLE_REMARKS describes the first example.
    .EXAMPLE
    Get-HelpFormattingValue -Name beta -Count 2

    HELP_SECOND_EXAMPLE describes the second example.
    .INPUTS
    System.String
    .OUTPUTS
    System.String
    .NOTES
    HELP_NOTES is included in full help.
    .LINK
    https://example.invalid/help-formatting
    #>
    [CmdletBinding()]
    param([Parameter(Position = 0, ValueFromPipelineByPropertyName)][string] $Name, [int] $Count = 1)
    process { throw 'The fixture command must never execute when help is requested.' }
}

function Get-HelpFormattingShort {
    <# .SYNOPSIS
    A short synthetic command. #>
    [CmdletBinding()]
    param()
}

Export-ModuleMember -Function Get-HelpFormattingValue, Get-HelpFormattingShort
