---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleProgress
## SYNOPSIS
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. Update it live with Update-ConsoleProgress.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleProgress [[-Label] <string>] [-Value <double>] [-Total <double>] [-Name <string>] [-Shape <BarShape>] [<CommonParameters>]
```

## DESCRIPTION
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. Update it live with Update-ConsoleProgress.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleProgress -Name 'Name'
```


## PARAMETERS

### -Label
The label (markup allowed).

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

### -Name
The live name (default progress).

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

### -Total
The total (default 100).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.ProgressBar`

## RELATED LINKS

- None
