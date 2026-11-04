# Fold help sections and resize the pane beside them.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Layout: construct the widgets shown on screen.
$content = {
    New-ConsoleSplitter -Key Panes -Ratio 0.6 -Fill -First {
        New-ConsoleCollapsible -Accordion -Content {
            New-ConsoleCollapsible -Key Synopsis -Expanded -Content {
                'Lists servers and their roles from an inline inventory.'
            }
            New-ConsoleCollapsible -Key Parameters -Summary '(3)' -Content {
                '-Name    server name or wildcard'
                '-Role    web, db or cache'
                '-Online  only servers that answer'
            }
            New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
                'Get-Inventory -Role web'
                'Get-Inventory -Name db* -Online'
            }
        }
    } -Second {
        New-ConsolePanel -Title Related -Content {
            'Get-Service'
            'Test-Connection'
        }
    }
}

# Behaviour: Enter folds a section, + expands all, Ctrl+Shift+Left/Right resizes; Escape exits.
Show-ConsoleApp -Content $content