---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Stop-ConsoleRecording
## SYNOPSIS
Stops recording from inside the recorded child shell.

Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Start-ConsoleRecording session.

## SYNTAX
### __AllParameterSets
```powershell
Stop-ConsoleRecording [<CommonParameters>]
```

## DESCRIPTION
Stops recording from inside the recorded child shell.

Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Start-ConsoleRecording session.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-Host 'Deployment finished'
            Stop-ConsoleRecording
```

Finish from inside the recorded child shell

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
