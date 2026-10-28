---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Get-ConsoleFormatting
## SYNOPSIS
Shows the formatting features enabled in this runspace.

Returns the enabled feature names, whether compact errors are enabled, and whether built-in views are included. A refused installation is not reported as enabled. AddOptIns shows how to add Help, Prompt, Progress or GridView.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleFormatting [<CommonParameters>]
```

## DESCRIPTION
Shows the formatting features enabled in this runspace.

Returns the enabled feature names, whether compact errors are enabled, and whether built-in views are included. A refused installation is not reported as enabled. AddOptIns shows how to add Help, Prompt, Progress or GridView.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting -Include Help; Get-ConsoleFormatting
```

Inspect the current session's formatting

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Management.Automation.PSObject`

## RELATED LINKS

- None
