---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolemenu/
schema: 2.0.0
---
# Show-ConsoleMenu
## SYNOPSIS
Shows a menu until the user exits.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleMenu [[-Menu] <psobject>] [-Layout <MenuLayout>] [-AppName <string>] [-RememberLastChoices] [-StorageRoot <string>] [-NoCommandPalette] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Menu actions run on the pipeline thread. Selections and form values survive navigation. AppName stores presets, and RememberLastChoices also stores the last choices.

Shell menu
----------

The title bar shows the breadcrumb. The sidebar lists items and folds into the breadcrumb in narrow windows. The content pane shows the open list, form, page or review. The footer lists the current keys. Use Escape to go back and Ctrl+Q to exit.

Classic menu
------------

Classic shows a centred menu. Escape returns from a submenu and exits from the root. Both full-screen layouts show the key map with ?. When enabled, Ctrl+K opens the command palette. A colon also opens it when focus is outside a text input. Ctrl+Q asks before discarding unsaved form edits.

Inline menu
-----------

Inline displays one selection prompt at a time in the scrollback. In line mode, respond with the numbered choice. Actions write their output in the scrollback and the menu returns after the action. Use Back in a submenu or choose an Exit item to leave.

Static menu
-----------

Redirected output, CI, Mode None and non-interactive sessions print the current menu once. The static view executes no actions.

## EXAMPLES

### EXAMPLE 1
```powershell
$menuOptions2 = @{
    Title = 'Server toolbox'
    Content = {
        $menuItemOptions = @{
            Label = 'Current time'
            Action = { $MenuContext.Toast((Get-Date).ToString('u')) }
        }
        New-ConsoleMenuItem @menuItemOptions
        New-ConsoleMenuItem -Exit
    }
}
$menu = New-ConsoleMenu @menuOptions2
$menuOptions = @{
    Menu = $menu
    Layout = 'Shell'
}
Show-ConsoleMenu @menuOptions
```

Run a menu as an application

### EXAMPLE 2
```powershell
$menuOptions2 = @{
    Title = 'Inventory'
    Content = {
        $menuItemOptions = @{
            Label = 'List servers'
            Action = { [pscustomobject]@{ Name = 'web01'; State = 'Online' } | Show-ConsoleTable }
        }
        New-ConsoleMenuItem @menuItemOptions
        New-ConsoleMenuItem -Exit
    }
}
$menu = New-ConsoleMenu @menuOptions2
$menuOptions = @{
    Menu = $menu
}
Show-ConsoleMenu @menuOptions
```

Choose an inventory action from a menu

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'toolbox' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'fleet' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see Show-ConsoleMenu in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -AppName
The app name presets and remembered choices are stored under (default: the menu title).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Layout
The layout: Auto (the menu's own, or Shell when items open content), Classic, Shell or Inline.

```yaml
Type: MenuLayout
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Auto, Classic, Shell, Inline

Required: False
Position: named
Default value: Auto
Accept pipeline input: False
Accept wildcard characters: False
```

### -Menu
The menu from New-ConsoleMenu.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Mode
Forces how to interact: Rich, Line or Accessible.

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None, Snapshot

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoCommandPalette
Turns the command palette off.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -RememberLastChoices
Remembers selections and form answers between runs.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -StorageRoot
The folder that holds the app's storage folder (default: per user).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/show-consolemenu/](https://consolemonster.dev/powershell/commands/show-consolemenu/)
