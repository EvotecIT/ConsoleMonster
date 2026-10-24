# Build a three-step installer preview with editable fields and buttons.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0,
    [hashtable] $AppOptions = @{}
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
$AppOptions.StaticSnapshot = $StaticSnapshot -or ([Console]::IsInputRedirected -and -not $AppOptions.InputSource)

# Data: fictional inputs kept in memory.
$installFolder = './toolbox-agent'
$components = @('Core', 'Core + reports')
$wizard = [pscustomobject] @{ Page = $Step }
# Layout: construct the widgets shown on screen.
Show-ConsoleApp {
    New-ConsoleRows -Gap 1 {
        '[bold]Install Toolbox Agent[/] [muted]· Toolbox Agent[/]'
        New-ConsoleTextInput -Name Folder -Label 'Install folder' -Value $installFolder
        New-ConsoleSelect -Name Component -Label Component -Choices $components -Value Core
        New-ConsoleButton -Name Back -Label Back -OnClick { $wizard.Page = [Math]::Max(0, $wizard.Page - 1) }
        New-ConsoleButton -Name Next -Label 'Next / preview' -OnClick { $wizard.Page = [Math]::Min(2, $wizard.Page + 1) }
        # Live updates: rebuild this section when an input or button changes state.
        New-ConsoleBinding {
            param($State)
            New-ConsolePanel -Title (@('1 / 3 · Location', '2 / 3 · Components', '3 / 3 · Review')[$wizard.Page]) {
                switch ($wizard.Page) {
                    0 { 'Choose a writable folder for the agent.' }
                    1 { 'Core runs local jobs. Reports adds an HTML summary.' }
                    2 {
                        # Plain text widgets escape user-entered values automatically.
                        New-ConsoleText "Folder: $($State.Folder)"
                        New-ConsoleText "Component: $($State.Component)"
                        '[pass]Ready to install. Review the folder and components.[/]'
                    }
                }
            }
        }
    }
} @AppOptions
