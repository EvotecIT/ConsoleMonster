---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleSparkline
## SYNOPSIS
Creates a sparkline: a compact trend of recent values.

One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Values come from -Value or the pipeline. -Tone sets the colour, -Maximum the top of the scale and -NoLatest hides the latest value. To add values later, pipe it to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSparkline [[-Name] <string>] [-Value <double[]>] [-Label <Object>] [-Height <int>] [-Unit <string>] [-InputObject <double[]>] [-Tone <ThemeToken>] [-Maximum <Double>] [-NoLatest] [<CommonParameters>]
```

## DESCRIPTION
Creates a sparkline: a compact trend of recent values.

One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Values come from -Value or the pipeline. -Tone sets the colour, -Maximum the top of the scale and -NoLatest hides the latest value. To add values later, pipe it to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleSparkline -Label 'req/s' -Value 3, 5, 8, 13, 9, 7, 12 | Write-ConsoleWidget
```

A labelled trend

## PARAMETERS

### -Height
Rows (default 1).

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

### -InputObject
Values from the pipeline, oldest first.

```yaml
Type: Double[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Label
A muted label on the left.

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

### -Maximum
The value at the top of the scale (default: the largest value).

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

### -Name
The live name (default sparkline).

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

### -NoLatest
Hide the latest value after the trend.

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

### -Tone
The colour as a theme role: Accent (default), Accent2, Pass, Fail, Warn or Series1 to Series6.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
A unit appended to the latest value.

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

### -Value
Initial values, oldest first.

```yaml
Type: Double[]
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

- `System.Double[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Sparkline`

## RELATED LINKS

- None
