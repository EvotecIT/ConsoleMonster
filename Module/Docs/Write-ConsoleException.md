---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleException
## SYNOPSIS
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleException [[-InputObject] <Object>] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-Width <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleException -InputObject 'Value'
```


## PARAMETERS

### -InputObject
An ErrorRecord, an exception or a message (pipeline input).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -MaxFrames
The most app frames shown per error (default 8).

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

### -NoStack
Leaves out the stack frames: type, message and position only.

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

### -ShowFrameworkFrames
Shows framework frames (System, Microsoft, the PowerShell engine) instead of collapsing them.

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

### -Width
Lays the output out for this width instead of the terminal's.

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

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
