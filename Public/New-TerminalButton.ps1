function New-TerminalButton {
    [cmdletBinding()]
    param(
        [scriptblock] $Command,
        [string] $Text,
        [int] $X,
        [int] $Y,
        [switch] $DisableAutosize
    )

    $Button = [Terminal.Gui.Button]::new()
    $Button.X = $X
    $Button.Y = $Y
    $Button.Text = $Text
    $Button.AutoSize = -not $DisableAutosize.IsPresent
    $Button
}