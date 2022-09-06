function New-TerminalCheckBox {
    [CmdletBinding()]
    param(
        [string[]] $Text,
        [int] $X,
        [int] $Y
    )
    [Terminal.Gui.CheckBox]::new($X, $Y, $Text)
}