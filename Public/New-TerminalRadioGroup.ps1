function New-TerminalRadioGroup {
    [cmdletBinding()]
    param(
        [string[]] $Text,
        [int] $X,
        [int] $Y,
        [int] $Selected
    )
    <#
    Terminal.Gui.RadioGroup new()
    Terminal.Gui.RadioGroup new(NStack.ustring[] radioLabels, int selected = 0)
    Terminal.Gui.RadioGroup new(Terminal.Gui.Rect rect, NStack.ustring[] radioLabels, int selected = 0)
    Terminal.Gui.RadioGroup new(int x, int y, NStack.ustring[] radioLabels, int selected = 0)
    #>
    [Terminal.Gui.RadioGroup]::new($X, $Y, $Text, $Selected)
}