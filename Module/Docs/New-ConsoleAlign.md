---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleAlign
## SYNOPSIS
Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.

The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleAlign [[-Content] <scriptblock>] [-Widget <IWidget>] [-Horizontal <HorizontalAlignment>] [-Vertical <VerticalAlignment>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.

The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleAlign -Horizontal Center { New-ConsoleLogo -Text 'Monster' }
```

A centred logo above a dashboard

## PARAMETERS

### -Content
Scriptblock that emits the content (several widgets are stacked vertically).

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
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

### -Horizontal
Horizontal placement: Left, Center (default) or Right.

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

### -Vertical
Vertical placement: Top (default), Middle or Bottom.

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

### -Widget
A widget to align instead of a scriptblock.

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `ConsoleMonster.Layout.IWidget`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
