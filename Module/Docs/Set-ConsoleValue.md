---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Set-ConsoleValue
## SYNOPSIS
Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.

Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.

## SYNTAX
### __AllParameterSets
```powershell
Set-ConsoleValue [-Key] <string> [-Value] <Object> [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.

Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleValue -Key 'Value' -Value 'Value'
```


## PARAMETERS

### -Key
The key, bar label or segment.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The widget name (default keyvalue).

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

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
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
The value.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
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
