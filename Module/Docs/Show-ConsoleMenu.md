---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleMenu
## SYNOPSIS
Shows a menu until the user exits.

Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleMenu [-Menu] <Menu> [-Layout <MenuLayout>] [-AppName <string>] [-RememberLastChoices] [-StorageRoot <string>] [-NoCommandPalette] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Shows a menu until the user exits.

Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleMenu $menu -Layout Shell -AppName 'ServerToolbox' -RememberLastChoices
```

Run a menu as an app

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
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Menu
The menu from New-ConsoleMenu.

```yaml
Type: Menu
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
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
Possible values: Rich, Line, Accessible, None

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
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RememberLastChoices
Remembers selections and form answers between runs.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: Remember
Possible values:

Required: False
Position: named
Default value: None
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

- `ConsoleMonster.Interactive.Menu`

## OUTPUTS

- `None`

## RELATED LINKS

- None
