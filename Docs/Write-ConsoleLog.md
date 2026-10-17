---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleLog
## SYNOPSIS
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug or pass. Only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleLog [-Message] <Object> [-Level <string>] [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug or pass. Only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleLog -Message 'Value'
```


## PARAMETERS

### -Level
The level: info, warn, err, verbose, debug or pass.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: info, warn, err, verbose, debug, pass

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Message
The message.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Name
The log name; defaults to the dashboard's log region.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
