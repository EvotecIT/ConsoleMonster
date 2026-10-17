---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleWidget
## SYNOPSIS
Writes console widgets once (tables, trees, panels, charts, dashboards).

Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleWidget [-Widget] <Object[]> [-Width <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes console widgets once (tables, trees, panels, charts, dashboards).

Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleWidget -Widget @('Value')
```


## PARAMETERS

### -Widget
The widgets to write; strings are treated as markup.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Width
Render at this width instead of the terminal width.

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

- `System.Object[]`

## OUTPUTS

- `None`

## RELATED LINKS

- None
