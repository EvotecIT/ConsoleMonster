Import-Module .\ConsoleMonster.psd1 -Force

[Terminal.Gui.Application]::Init()

$win = [Terminal.Gui.Window] @{
    Title = 'Hello World'
}

$edt = [Terminal.Gui.TextField] @{
    X     = [Terminal.Gui.Pos]::Center()
    Y     = [Terminal.Gui.Pos]::Center()
    Width = 20
    Text  = 'This text will be returned'
}
$win.Add($edt)

for ($i = 0; $i -lt 30; $i++) {
    $Check = [Terminal.Gui.CheckBox] @{
        X    = [Terminal.Gui.Pos]::Center()
        Y    = [Terminal.Gui.Pos]::Center() + $i
        Text = "Option $i"
    }
    $win.Add($Check)
}

# $btn = [Terminal.Gui.Button] @{
#     X    = [Terminal.Gui.Pos]::Center()
#     Y    = [Terminal.Gui.Pos]::Center() + 1
#     Text = 'Option 1'
# }
# $win.Add($btn)

$btn = [Terminal.Gui.Button] @{
    X    = [Terminal.Gui.Pos]::Center() + 20
    Y    = [Terminal.Gui.Pos]::Center() + 2
    Text = 'Quit'
}
$win.Add($btn)


[Terminal.Gui.Application]::Top.Add($win)

# Attach an event handler to the button.
# Note: Register-ObjectEvent -Action is NOT an option, because
# the [Application]::Run() method used to display the window is blocking.
$btn.add_Clicked( {
        # Close the modal window.
        # This call is also necessary to stop printing garbage in response to mouse
        # movements later.
        [Terminal.Gui.Application]::RequestStop()
    })

# Show the window (takes over the whole screen).
# Note: This is a blocking call.
[Terminal.Gui.Application]::Run()

# As of 1.0.0-pre.4, at least on macOS, the following two statements
# are necessary on in order for the terminal to behave properly again.
[Terminal.Gui.Application]::Shutdown() # Clears the screen too; required for being able to rerun the application in the same session.

# Output something
$edt.Text.ToString()