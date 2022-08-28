function New-TerminalWindow {
    [cmdletBinding()]
    param(
        [scriptblock] $WindowContent,
        [string] $Title
    )

    $Window = [Terminal.Gui.Window]::new()
    if ($Title) {
        $Window.Title = $Title
    }

    if ($WindowContent) {
        $Output = & $WindowContent

        foreach ($O in $Output) {
            $Window.Add($O)
        }
    }

    $Window.Add($btn)
    $Window
}