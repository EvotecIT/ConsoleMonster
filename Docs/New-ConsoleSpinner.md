---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleSpinner
## SYNOPSIS
Creates a spinner: a one-line activity indicator for work without a known total.

While running it animates (braille on modern fonts, | / - \ on Basic and ASCII) and shows the label, a muted detail and the elapsed time. Once finished it shows the status marker in the status colour and the final duration. Update it live with Update-ConsoleSpinner.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSpinner [[-Label] <string>] [-Detail <string>] [-Status <StatusKind>] [-Name <string>] [-NoElapsed] [<CommonParameters>]
```

## DESCRIPTION
Creates a spinner: a one-line activity indicator for work without a known total.

While running it animates (braille on modern fonts, | / - \ on Basic and ASCII) and shows the label, a muted detail and the elapsed time. Once finished it shows the status marker in the status colour and the final duration. Update it live with Update-ConsoleSpinner.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleSpinner -Name 'Name'
```


## PARAMETERS

### -Detail
Muted detail after the label, for example the current item.

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
The live name (default spinner).

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

### -NoElapsed
Hide the elapsed time.

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

### -Status
The initial status (default Running).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.Spinner`

## RELATED LINKS

- None
