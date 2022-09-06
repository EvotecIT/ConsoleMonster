function New-TerminalText {
    [cmdletBinding()]
    param(
        [int] $X,
        [int] $Y,
        [int] $Width,
        [Parameter(Mandatory)][string] $Text
    )

    $TextField = [Terminal.Gui.TextField]::new()
    $TextField.X = $X
    $TextField.Y = $Y
    if ($Width) {
        $TextField.Width = $Width
    }
    if ($Text) {
        $TextField.Text = $Text
    }
    $TextField
}