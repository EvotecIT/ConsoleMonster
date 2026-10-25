---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsolePanel
## SYNOPSIS
Creates a bordered panel with a title around other console widgets.

-Content is a scriptblock, a widget, text or any objects (objects are shown as a record or a table; several pieces are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsolePanel [[-Content] <Object>] [-Widget <IWidget>] [-Title <Object>] [-HeaderRight <Object>] [-Caption <Object>] [-Border <BoxStyle>] [-Weight <double>] [-Height <int>] [-Fill] [-Verdict] [-Status <StatusKind>] [<CommonParameters>]
```

## DESCRIPTION
Creates a bordered panel with a title around other console widgets.

-Content is a scriptblock, a widget, text or any objects (objects are shown as a record or a table; several pieces are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePanel -Title 'Targets' -Weight 2 -Content { New-ConsoleLanes -WidgetId targets }
```

A panel holding a lanes widget, taking two shares of a row

## PARAMETERS

### -Border
Border style: Rounded, Single, Double, Heavy, Ascii or None.

```yaml
Type: BoxStyle
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rounded, Single, Double, Heavy, Ascii, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
Text on the right of the bottom border.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
The panel content: a scriptblock that emits widgets, text or objects, a widget, a string, or objects (one object is shown as labelled fields, several as a table, through the same rules as Show-ConsoleList and Show-ConsoleTable).

```yaml
Type: Object
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
Take the remaining height of a live dashboard (for example a log).

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

### -HeaderRight
Text on the right of the top border, for example a count.

```yaml
Type: Object
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
Fixed height in rows, including the border.

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

### -Status
Status colour and title glyph for a verdict.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The title shown in the top border (Spectre-style markup allowed).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Verdict
Double-bordered verdict with a centred title.

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

### -Weight
Share of the width (in columns) or height (in a live dashboard) this panel takes.

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
A widget to use as content instead of a scriptblock.

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
