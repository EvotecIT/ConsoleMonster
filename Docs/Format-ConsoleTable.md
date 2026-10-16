---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleTable
## SYNOPSIS
Formats pipeline objects as a width-aware table and writes it to the host.

Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards; a single object becomes a record view. Redirected output is plain text without escape codes.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleTable [[-Property] <Object[]>] [-InputObject <psobject>] [-GroupBy <Object>] [-Total] [-Top <int>] [-Title <string>] [-Caption <string>] [-Border <TableBorder>] [-CardsBelow <int>] [-Width <int>] [-NoHeaderSeparator] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Formats pipeline objects as a width-aware table and writes it to the host.

Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards; a single object becomes a record view. Redirected output is plain text without escape codes.

## EXAMPLES

### EXAMPLE 1
```powershell
Format-ConsoleTable -Border 'Value'
```


## PARAMETERS

### -Border
Border style: Rounded, Square, Double, Minimal or None.

```yaml
Type: TableBorder
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
A caption below the table (markup, muted).

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

### -CardsBelow
Below this width (in cells) rows become record cards when the grid cannot fit; 0 disables cards. Default 60.

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

### -GroupBy
Groups rows under headings by a property name or a scriptblock; with -Total each group gets a subtotal.

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

### -InputObject
The objects to show (pipeline input).

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

### -NoHeaderSeparator
Underlines the header instead of drawing a separator row.

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

### -PassThru
Also writes the input objects to the pipeline.

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

### -Property
Properties (names, wildcards, calculated properties and column option hashtables).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title above the table (markup).

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

### -Top
Shows only the first N rows followed by "Showing N of M" (totals still cover every row).

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

### -Total
Adds a totals row; numeric columns without an Aggregate option are summed.

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

### -Width
Lays the table out for this width instead of the terminal's (for files, logs and previews).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
