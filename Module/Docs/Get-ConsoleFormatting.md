---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Get-ConsoleFormatting
## SYNOPSIS
Shows the formatting features enabled in this runspace.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleFormatting [<CommonParameters>]
```

## DESCRIPTION
Returns the enabled feature names, whether compact errors are enabled, and whether built-in views are included. A refused installation is not reported as enabled. AddOptIns shows how to add Help, Prompt, Progress or GridView.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting -Include Help; Get-ConsoleFormatting
```

Inspect the current session's formatting

### EXAMPLE 2
```powershell
Get-ConsoleFormatting | Select-Object Feature, Enabled | Show-ConsoleTable
```

Inspect which formatting features are enabled

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.Management.Automation.PSObject`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/get-consoleformatting/](https://consolemonster.dev/powershell/commands/get-consoleformatting/)
