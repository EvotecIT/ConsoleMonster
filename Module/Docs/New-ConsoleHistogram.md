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
New-ConsoleHistogram -Edge <double[]> [-Value <double[]>] [-Height <int>] [-Unit <string>] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a histogram of values counted into buckets.

-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleHistogram -Edge @(1)
```


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

### -Name
The live name (default histogram).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.Histogram`

## RELATED LINKS

- None
