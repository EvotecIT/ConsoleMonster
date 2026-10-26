---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Add-ConsoleRecordingMarker
## SYNOPSIS
Adds a chapter or an export hold to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

## SYNTAX
### __AllParameterSets
```powershell
Add-ConsoleRecordingMarker [[-Label] <string>] [-HoldSeconds <double>] [<CommonParameters>]
```

## DESCRIPTION
Adds a chapter or an export hold to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

## EXAMPLES

### EXAMPLE 1
```powershell
Add-ConsoleRecordingMarker 'Deploy'
```

Mark a deployment step

### EXAMPLE 2
```powershell
Add-ConsoleRecordingMarker -HoldSeconds 3
```

Hold this screen during export without waiting while recording

## PARAMETERS

### -HoldSeconds
Freezes the exported screen for this many seconds without sleeping during capture. The idle limit never trims this hold. Label is optional for a hold.

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

### -Label
Chapter label.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
