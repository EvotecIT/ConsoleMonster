---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Disable-ConsoleFormatting
## SYNOPSIS
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting).

Removes the session's Out-Default function when it is ConsoleMonster's, so PowerShell's own Out-Default is used again. Registrations stay for the next Enable-ConsoleFormatting.

## SYNTAX
### __AllParameterSets
```powershell
Disable-ConsoleFormatting [<CommonParameters>]
```

## DESCRIPTION
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting).

Removes the session's Out-Default function when it is ConsoleMonster's, so PowerShell's own Out-Default is used again. Registrations stay for the next Enable-ConsoleFormatting.

## EXAMPLES

### EXAMPLE 1
```powershell
Disable-ConsoleFormatting
```


## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
