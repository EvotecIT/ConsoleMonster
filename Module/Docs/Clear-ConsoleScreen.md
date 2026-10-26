---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Clear-ConsoleScreen
## SYNOPSIS
Clears the console screen, safely.

On an interactive console the screen and its scrollback are cleared and the cursor goes to the top left, like Clear-Host. It does nothing where clearing would only lose output: redirected output, CI logs, plain mode (CONSOLEMONSTER_MODE=plain), background and thread jobs, and while a live dashboard owns the console. In the ISE and other hosts with a screen buffer it clears that buffer the way Clear-Host does.

## SYNTAX
### __AllParameterSets
```powershell
Clear-ConsoleScreen [<CommonParameters>]
```

## DESCRIPTION
Clears the console screen, safely.

On an interactive console the screen and its scrollback are cleared and the cursor goes to the top left, like Clear-Host. It does nothing where clearing would only lose output: redirected output, CI logs, plain mode (CONSOLEMONSTER_MODE=plain), background and thread jobs, and while a live dashboard owns the console. In the ISE and other hosts with a screen buffer it clears that buffer the way Clear-Host does.

## EXAMPLES

### EXAMPLE 1
```powershell
Clear-ConsoleScreen; Show-ConsoleMenu -Title 'Tools' -Items $items
```

Start a menu screen from a clean console

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
