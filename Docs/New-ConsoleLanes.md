---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLanes
## SYNOPSIS
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane or appear on their first Update-ConsoleLane.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLanes [[-Name] <string>] [-Lane <string[]>] [-Total <double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane or appear on their first Update-ConsoleLane.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLanes -Name 'Name'
```


## PARAMETERS

### -Lane
Lanes to declare up front (key and label).

```yaml
Type: String[]
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
The live name (default lanes).

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

### -Total
Planned steps per declared lane.

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

- `ConsoleMonster.Widgets.Lanes`

## RELATED LINKS

- None
