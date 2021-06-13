function New-TerminalText {
    [cmdletBinding()]
    param(
        [int] $X,
        [int] $Y,
        [int] $Width,
        [string] $Text
    )

    $TextField = [Terminal.Gui.TextField]::new()
    #if ($X) {
        $TextField.X = [Terminal.Gui.Pos]::Center()
    #}
    #if ($Y) {
        $TextField.Y = [Terminal.Gui.Pos]::Center()
    #}
    if ($Width) {
        $TextField.Width = 20
    }
    if ($Text) {
        $TextField.Text = 'This text will be returned'
    }
    $TextField
}