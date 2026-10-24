---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleCommand
## SYNOPSIS
Builds a copyable command line from named values without executing it.

Formats strings, numbers, switches and lists through the same quoting rules as forms. Use an ordered dictionary to retain argument order. Null, empty strings and false switches are omitted. The result is a plain string, suitable for a review's equivalent command.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleCommand -Command <string> -Arguments <IDictionary> [-Style <CommandStyle>] [<CommonParameters>]
```

## DESCRIPTION
Builds a copyable command line from named values without executing it.

Formats strings, numbers, switches and lists through the same quoting rules as forms. Use an ordered dictionary to retain argument order. Null, empty strings and false switches are omitted. The result is a plain string, suitable for a review's equivalent command.

## EXAMPLES

### EXAMPLE 1
```powershell
Format-ConsoleCommand -Command Invoke-FleetCheck -Arguments ([ordered]@{ ComputerName = @('WEB01', 'SQL01'); ThrottleLimit = 3; WhatIf = $true })
```

Build an equivalent command from selected servers

## PARAMETERS

### -Arguments
Parameter names and values. Use an ordered dictionary when order matters.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Command
The command text, which is never executed.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Style
PowerShell, Posix or Windows argument quoting (default PowerShell).

```yaml
Type: CommandStyle
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: PowerShell, Posix, Windows

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

- `System.String`

## RELATED LINKS

- None
