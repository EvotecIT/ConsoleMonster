---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolemenu/
schema: 2.0.0
---
# New-ConsoleMenu
## SYNOPSIS
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleMenu [-Title] <string> [[-Content] <scriptblock>] [-InputObject <psobject>] [-Preset <Object[]>] [-Description <string>] [-Subtitle <string>] [-Layout <MenuLayout>] [<CommonParameters>]
```

## DESCRIPTION
The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.

## EXAMPLES

### EXAMPLE 1
```powershell
$formOptions = @{
    Title = 'Settings'
    Content = {
        $fieldOptions2 = @{
            Key = 'Target'
            Label = 'Target'
            Default = 'Weekly'
        }
        New-ConsoleField @fieldOptions2
        $fieldOptions = @{
            Key = 'Compress'
            Label = 'Compress'
            Kind = 'Toggle'
            Default = $true
        }
        New-ConsoleField @fieldOptions
    }
}
$settings = New-ConsoleForm @formOptions
$menuOptions2 = @{
    Title = 'Backup helper'
    Content = {
        $menuItemOptions2 = @{
            Label = 'Preview backup'
            Hotkey = 'b'
            Action = {
                $dashboardOptions = @{
                    Title = 'Backup preview'
                    Content = {
                        $statusListOptions = @{
                            WidgetId = 'steps'
                            Content = { 'Inspect plan' }
                        }
                        New-ConsoleStatusList @statusListOptions
                    }
                }
                $dashboard = New-ConsoleDashboard @dashboardOptions
                $null = $MenuContext.Run($dashboard, {
                        $updateConsoleOptions = @{
                            WidgetId = 'steps'
                            Key = 'Inspect plan'
                            Status = 'Pass'
                        }
                        Update-Console @updateConsoleOptions
                        @{ Title = 'Backup preview'; Status = 'Pass'; Summary = 'No files were copied.' }
                    })
            }
        }
        New-ConsoleMenuItem @menuItemOptions2
        $menuItemOptions = @{
            Label = 'Settings'
            Hotkey = 's'
            Form = $settings
        }
        New-ConsoleMenuItem @menuItemOptions
        New-ConsoleMenuItem -Exit
    }
}
$menu = New-ConsoleMenu @menuOptions2
$menuOptions = @{
    Menu = $menu
    Layout = 'Classic'
}
Show-ConsoleMenu @menuOptions
```

Build a small script menu

### EXAMPLE 2
```powershell
$formOptions = @{
    Title = 'Settings'
    Content = {
        $fieldOptions2 = @{
            Key = 'Target'
            Label = 'Target'
            Default = 'Weekly'
        }
        New-ConsoleField @fieldOptions2
        $fieldOptions = @{
            Key = 'Compress'
            Label = 'Compress'
            Kind = 'Toggle'
            Default = $true
        }
        New-ConsoleField @fieldOptions
    }
}
$settings = New-ConsoleForm @formOptions
$menuOptions2 = @{
    Title = 'Backup helper'
    Content = {
        [pscustomobject]@{ Name = 'Weekly'; Form = $settings; Values = @{ Target = 'Weekly'; Compress = $true }; Description = 'The Sunday backup.' }
        $menuItemOptions2 = @{
            Presets = $true
            Label = 'Presets'
            Hotkey = 'p'
        }
        New-ConsoleMenuItem @menuItemOptions2
        $menuItemOptions = @{
            Label = 'Settings'
            Hotkey = 's'
            Form = $settings
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

Offer a preset for a menu form

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'toolbox' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'fleet' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleMenu in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

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
Default value: Auto
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

- [https://consolemonster.dev/powershell/commands/new-consolemenu/](https://consolemonster.dev/powershell/commands/new-consolemenu/)
