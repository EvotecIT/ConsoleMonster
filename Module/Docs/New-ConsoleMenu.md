---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleMenu
## SYNOPSIS
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleMenu [-Title] <string> [[-Content] <scriptblock>] [-Item <MenuItem[]>] [-Description <string>] [-Subtitle <string>] [-Layout <MenuLayout>] [<CommonParameters>]
```

## DESCRIPTION
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu.

## EXAMPLES

### EXAMPLE 1
```powershell
$menu = New-ConsoleMenu 'Backup helper' {
    New-ConsoleMenuItem 'Back up now' -Hotkey b -Action { Start-Backup }
    New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    New-ConsoleMenuItem -Exit
}
Show-ConsoleMenu $menu -Layout Classic
```

A small script menu

## PARAMETERS

### -Content
A scriptblock whose New-ConsoleMenuItem output becomes the items.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
A sentence shown above the items.

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

### -Item
Items created beforehand.

```yaml
Type: MenuItem[]
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
The default layout for Show-ConsoleMenu: Auto, Classic, Shell or Inline.

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

### -Subtitle
Muted text at the right of the footer (a version, an environment).

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

### -Title
The title (the app name for the top menu).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.Menu`

## RELATED LINKS

- None
