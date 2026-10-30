---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleMenu
## SYNOPSIS
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleMenu [-Title] <string> [[-Content] <scriptblock>] [-InputObject <psobject>] [-Preset <Object[]>] [-Description <string>] [-Subtitle <string>] [-Layout <MenuLayout>] [<CommonParameters>]
```

## DESCRIPTION
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.

## EXAMPLES

### EXAMPLE 1
```powershell
$settings = New-ConsoleForm 'Settings' -Content {
    New-ConsoleField Target 'Target' -Default Weekly
    New-ConsoleField Compress 'Compress' -Type Toggle -Default $true
}
$menu = New-ConsoleMenu 'Backup helper' -Content {
    New-ConsoleMenuItem 'Preview backup' -Hotkey b -Action {
        $dashboard = New-ConsoleDashboard -Title 'Backup preview' -Content { New-ConsoleStatusList -WidgetId steps -Content { 'Inspect plan' } }
        $null = $MenuContext.Run($dashboard, {
            Update-Console steps -Key 'Inspect plan' -Status Pass
            @{ Title = 'Backup preview'; Status = 'Pass'; Summary = 'No files were copied.' }
        })
    }
    New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    New-ConsoleMenuItem -Exit
}
Show-ConsoleMenu $menu -Layout Classic
```

Build a small script menu

### EXAMPLE 2
```powershell
$settings = New-ConsoleForm 'Settings' -Content {
    New-ConsoleField Target 'Target' -Default Weekly
    New-ConsoleField Compress 'Compress' -Type Toggle -Default $true
}
$menu = New-ConsoleMenu 'Backup helper' -Content {
    [pscustomobject]@{ Name = 'Weekly'; Form = $settings; Values = @{ Target = 'Weekly'; Compress = $true }; Description = 'The Sunday backup.' }
    New-ConsoleMenuItem 'Presets' -Hotkey p -Presets
    New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    New-ConsoleMenuItem -Exit
}
Show-ConsoleMenu $menu -Layout Shell
```

Offer a preset for a menu form

## PARAMETERS

### -Content
A scriptblock whose New-ConsoleMenuItem output becomes the items (and New-ConsoleMenu -Preset output the built-in presets).

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

### -InputObject
Menu items or preset records from the pipeline.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
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

### -Preset
Presets the app comes with (New-ConsoleMenu -Preset), listed before the saved ones by a -Presets item.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.Menu`

## RELATED LINKS

- None
