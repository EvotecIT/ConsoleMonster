---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Set-ConsoleTheme
## SYNOPSIS
Selects the theme used by every ConsoleMonster widget.

Themes map semantic tokens (pass, fail, warn, muted, accent, border...) to colours for every colour tier. Dark suits dark terminals (the default), Light suits light backgrounds, HighContrast uses saturated primaries.

## SYNTAX
### __AllParameterSets
```powershell
Set-ConsoleTheme [-Name] <string> [<CommonParameters>]
```

## DESCRIPTION
Selects the theme used by every ConsoleMonster widget.

Themes map semantic tokens (pass, fail, warn, muted, accent, border...) to colours for every colour tier. Dark suits dark terminals (the default), Light suits light backgrounds, HighContrast uses saturated primaries.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleTheme -Name Light
```

Use the light theme

## PARAMETERS

### -Name
The theme: Dark, Light or HighContrast.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Dark, Light, HighContrast

Required: True
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

- `None`

## RELATED LINKS

- None
