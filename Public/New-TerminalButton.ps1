function New-TerminalButton {
    [cmdletBinding()]
    param(
        [string] $Text
    )

    $Button = [Terminal.Gui.Button]::new()
    $Button.X = [Terminal.Gui.Pos]::Center()
    $Button.Y = [Terminal.Gui.Pos]::Center() + 1
    $Button.Text = $Text
    $Button
}