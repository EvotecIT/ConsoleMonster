function New-Terminal {
    [cmdletBinding()]
    param(
        [ScriptBlock] $Application
    )

    [Terminal.Gui.Application]::Init()

    if ($Application) {
        $OutputApp = & $Application
        foreach ($O in $OutputApp) {
            [Terminal.Gui.Application]::Top.Add($O)
        }
    }

    # Show the window (takes over the whole screen).
    # Note: This is a blocking call.
    [Terminal.Gui.Application]::Run()

    [Terminal.Gui.Application]::Shutdown() # Clears the screen too; required for being able to rerun the application in the same session.
}