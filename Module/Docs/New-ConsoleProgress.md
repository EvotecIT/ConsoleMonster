---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleProgress
## SYNOPSIS
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. -Status marks a finished bar (Pass, Fail, Warn, Skip) with its status colour. Update it live with Update-ConsoleProgress.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleProgress [[-WidgetId] <string>] [[-Label] <Object>] [-Value <double>] [-Total <double>] [-Status <StatusKind>] [-Shape <BarShape>] [-Column <string[]>] [-CountColumn <ColumnFormat>] [<CommonParameters>]
```

## DESCRIPTION
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. -Status marks a finished bar (Pass, Fail, Warn, Skip) with its status colour. Update it live with Update-ConsoleProgress.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleProgress -Label 'Upload' -Value 64 -Total 100 | Show-Console
```

A static progress bar

### EXAMPLE 2
```powershell
New-ConsoleRows -Content {
    New-ConsoleProgress -Label 'Build' -Value 64 -Total 100
    New-ConsoleProgress -Label 'Docs' -Value 100 -Total 100 -Status Pass
    New-ConsoleProgress -Label 'Upload' -Value 3 -Total 8 -Shape Block
} | Show-Console
```

Several bars, one of them finished

## PARAMETERS

### -Column
The columns of the row, in order: Standard, or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Rate and Remaining are smoothed over the last 10 seconds. Without it the classic label, bar, percentage and count show.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Standard, Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results, Status

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountColumn
Shared count-column settings from New-ConsoleColumnFormat.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The label (markup allowed).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shape
Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.

```yaml
Type: BarShape
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Block, Line

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The status (default Running): Pass, Fail, Warn or Skip mark a finished bar in that colour (PowerShell's words such as Success and Error work too).

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

### -Total
The total value represented by a full bar (default 100).

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

### -Value
The current value.

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
The live name (default progress).

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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.ProgressBar`

## RELATED LINKS

- None
