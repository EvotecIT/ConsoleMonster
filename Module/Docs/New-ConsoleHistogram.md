---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleHistogram
## SYNOPSIS
Creates a histogram of values counted into buckets.

-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleHistogram [[-WidgetId] <string>] -Edge <double[]> [-Value <double[]>] [-Height <int>] [-Unit <string>] [-Role <ThemeToken>] [<CommonParameters>]
```

## DESCRIPTION
Creates a histogram of values counted into buckets.

-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleHistogram -Edge 0, 0.25, 0.5, 1, 2, 4 -Unit s -Value (1..200 | ForEach-Object { Get-Random -Maximum 4.0 }) | Show-Console
```

Request durations in seconds

## PARAMETERS

### -Edge
Ascending bucket edges (at least two).

```yaml
Type: Double[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Rows for the bars (default 6).

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

### -Role
The colour as a theme role: Series1 (default), Accent, Pass, Fail, Warn and the other series colours.

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
A unit for bucket labels.

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
Values to count.

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

### -WidgetId
The live name (default histogram).

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

- `ConsoleMonster.Widgets.Histogram`

## RELATED LINKS

- None
