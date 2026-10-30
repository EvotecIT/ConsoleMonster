---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleRegion
## SYNOPSIS
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRegion [[-WidgetId] <string>] [[-Content] <Object>] [-Weight <double>] [-Height <int>] [-Fill] [-Horizontal <HorizontalAlignment>] [-Vertical <VerticalAlignment>] [-Padding <int[]>] [<CommonParameters>]
```

## DESCRIPTION
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePanel -Title 'Result' -Content { New-ConsoleRegion -WidgetId main -Content { New-ConsoleProgress -Label 'Querying' -Indeterminate -WidgetId 'spinner' } }
```

A panel whose content switches from a spinner to a table

### EXAMPLE 2
```powershell
New-ConsoleRegion -Horizontal Center -Padding 1 -Content { New-ConsoleText 'Ready' } | Show-Console
```

Align content inside a padded region

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
Default value: None
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
Default value: None
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
Default value: None
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
Default value: None
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
Default value: None
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

- None
