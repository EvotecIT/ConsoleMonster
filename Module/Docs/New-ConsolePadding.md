---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsolePadding
## SYNOPSIS
Adds empty space around console widgets.

-Padding takes one, two or four numbers of cells: all sides; left and right, then top and bottom; or left, top, right and bottom (the order of the .NET Padding widget). Strings in the script block are markup text. Use New-ConsoleAlign to place content inside its space instead.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsolePadding [[-Content] <scriptblock>] [-Widget <IWidget>] [-Padding <int[]>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Adds empty space around console widgets.

-Padding takes one, two or four numbers of cells: all sides; left and right, then top and bottom; or left, top, right and bottom (the order of the .NET Padding widget). Strings in the script block are markup text. Use New-ConsoleAlign to place content inside its space instead.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePanel -Title 'Padding' { New-ConsolePadding -Padding 2, 1 { 'two cells of padding' } } | Write-ConsoleWidget
```

Two cells left and right, one row above and below

### EXAMPLE 2
```powershell
New-ConsolePadding -Padding 4, 0, 0, 0 { New-ConsoleText -Markup '[muted]indented note[/]' } | Write-ConsoleWidget
```

Indent a block by four cells

## PARAMETERS

### -Content
Script block that emits the content (several widgets are stacked vertically).

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

### -Padding
The space in cells: one value for all sides; two for left and right, then top and bottom; four for left, top, right and bottom (default 1, 0: one cell left and right).

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
A widget to pad instead of a script block.

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
