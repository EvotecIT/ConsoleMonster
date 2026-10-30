---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleStack
## SYNOPSIS
Layers console widgets on top of each other in the same space.

Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleRegion to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleStack [[-Content] <Object>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Layers console widgets on top of each other in the same space.

Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleRegion to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleStack -Content {
    New-ConsoleChart -WidgetId latency -Type Line
    New-ConsoleRegion -Horizontal Right -Content { New-ConsoleText -Markup '[pass]LIVE[/]' }
}
```

A chart with a status badge in its top-right corner

## PARAMETERS

### -Content
Scriptblock that emits the layers, bottom first.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Fill
Take the remaining height of a live dashboard.

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
Fixed height in rows.

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

### -Weight
Share of the width (in columns) or height (in a live dashboard) this widget takes.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
