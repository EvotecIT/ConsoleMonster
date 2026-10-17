---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsolePhases
## SYNOPSIS
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

The active phase is emphasised and finished phases show their duration. Update phases live with Update-ConsolePhase.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsolePhases [-Phase] <string[]> [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

The active phase is emphasised and finished phases show their duration. Update phases live with Update-ConsolePhase.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePhases -Phase @('Value')
```


## PARAMETERS

### -Name
The live name (default phases).

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

### -Phase
The phases in order.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

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

- `ConsoleMonster.Widgets.Phases`

## RELATED LINKS

- None
