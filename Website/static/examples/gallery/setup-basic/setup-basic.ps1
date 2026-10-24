# Build a three-step installer preview with editable fields and buttons.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$installFolder = './toolbox-agent'
$components = @('Core', 'Core + reports')
$wizard = [pscustomobject] @{ Page = 0 }
# Layout: construct the widgets shown on screen.
$content = {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Install Toolbox Agent[/] [muted]· Toolbox Agent[/]'
        New-ConsoleTextInput -Name Folder -Label 'Install folder' -Value $installFolder
        New-ConsoleSelect -Name Component -Label Component -Choices $components -Value Core
        New-ConsoleButton -Name Back -Label Back -OnClick { $wizard.Page = [Math]::Max(0, $wizard.Page - 1) }
        New-ConsoleButton -Name Next -Label 'Next / preview' -OnClick { $wizard.Page = [Math]::Min(2, $wizard.Page + 1) }
        # Rebuild this section when an input or button changes state.
        New-ConsoleBinding -Content {
            param($State)
            New-ConsolePanel -Title (@('1 / 3 · Location', '2 / 3 · Components', '3 / 3 · Review')[$wizard.Page]) -Content {
                switch ($wizard.Page) {
                    0 {
                        'Choose a writable folder for the agent.'
                    }
                    1 {
                        'Core runs local jobs. Reports adds an HTML summary.'
                    }
                    2 {
                        # Plain text widgets escape user-entered values automatically.
                        New-ConsoleText -Text "Folder: $($State.Folder)"
                        New-ConsoleText -Text "Component: $($State.Component)"
                        '[pass]Ready to install. Review the folder and components.[/]'
                    }
                }
            }
        }
    }
}

# Behaviour: edit controls to update the view; Escape exits the app.
Show-ConsoleApp -Content $content
