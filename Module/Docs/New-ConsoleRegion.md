---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consoleregion/
schema: 2.0.0
---
# New-ConsoleRegion
## SYNOPSIS
Creates a named region whose content can be replaced while a dashboard runs.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRegion [[-WidgetId] <string>] [[-Content] <Object>] [-Weight <double>] [-Height <int>] [-Fill] [-Horizontal <HorizontalAlignment>] [-Vertical <VerticalAlignment>] [-Padding <int[]>] [<CommonParameters>]
```

## DESCRIPTION
Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

## EXAMPLES

### EXAMPLE 1
```powershell
$panelOptions = @{
    Title = 'Result'
    Content = {
        $regionOptions = @{
            WidgetId = 'main'
            Content = {
                $progressOptions = @{
                    Indeterminate = $true
                    Label = 'Querying'
                    WidgetId = 'spinner'
                }
                New-ConsoleProgress @progressOptions
            }
        }
        New-ConsoleRegion @regionOptions
    }
}
New-ConsolePanel @panelOptions
```

Build a panel with an indeterminate spinner as its initial content. WidgetId main identifies the region for a later Update-Console -Content call. WidgetId spinner identifies the progress widget currently inside it.

### EXAMPLE 2
```powershell
$regionOptions = @{
    Horizontal = 'Center'
    Padding = @(1)
    Content = {
        $textOptions = @{
            Text = @('Ready')
        }
        New-ConsoleText @textOptions
    }
}
New-ConsoleRegion @regionOptions | Show-Console
```

Center Ready and leave one cell of padding on each side. Padding is an integer array. The one-entry form uses that entry for all four sides. With two entries the order is horizontal then vertical. With four entries it is left, top, right, bottom.

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'build-steps' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'log-tail' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleRegion in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Content
A script block whose widgets are the first content.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Fill
Takes the remaining height.

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

### -Height
A fixed height in rows.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -Horizontal
Horizontal placement within the available space.

```yaml
Type: HorizontalAlignment
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Center, Right

Required: False
Position: named
Default value: Left
Accept pipeline input: False
Accept wildcard characters: False
```

### -Padding
Padding as one value for all sides, two for horizontal and vertical, or four for left, top, right and bottom.

```yaml
Type: Int32[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Vertical
Vertical placement within the available space.

```yaml
Type: VerticalAlignment
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Top, Middle, Bottom

Required: False
Position: named
Default value: Top
Accept pipeline input: False
Accept wildcard characters: False
```

### -Weight
The share of the remaining space in a row or column layout.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The region name used by Update-Console.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoleregion/](https://consolemonster.dev/powershell/commands/new-consoleregion/)
