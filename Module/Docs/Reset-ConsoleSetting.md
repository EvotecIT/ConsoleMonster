---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/reset-consolesetting/
schema: 2.0.0
---
# Reset-ConsoleSetting
## SYNOPSIS
Undoes Set-ConsoleSetting for this session, and with -Save removes the value from the settings file.

## SYNTAX
### __AllParameterSets
```powershell
Reset-ConsoleSetting [[-Name] <string[]>] [-Save] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Without -Name every setting is reset. Afterwards the next source applies: an environment variable, the settings file, or detection.

## EXAMPLES

### EXAMPLE 1
```powershell
Reset-ConsoleSetting Theme
```

Go back to the saved theme

### EXAMPLE 2
```powershell
Reset-ConsoleSetting -Save
```

Remove every saved setting

## PARAMETERS

### -Name
The settings to reset; all of them when omitted.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Save
Also removes the values from the settings file.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
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

- [https://consolemonster.dev/powershell/commands/reset-consolesetting/](https://consolemonster.dev/powershell/commands/reset-consolesetting/)
