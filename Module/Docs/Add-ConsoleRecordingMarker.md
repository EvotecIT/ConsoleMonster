---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Add-ConsoleRecordingMarker
## SYNOPSIS
Adds a named playback chapter to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

## SYNTAX
### __AllParameterSets
```powershell
Add-ConsoleRecordingMarker [-Label] <string> [<CommonParameters>]
```

## DESCRIPTION
Adds a named playback chapter to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

## EXAMPLES

### EXAMPLE 1
```powershell
Add-ConsoleRecordingMarker 'Deploy'
```

Mark a deployment step

## PARAMETERS

### -Label
Chapter label.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
