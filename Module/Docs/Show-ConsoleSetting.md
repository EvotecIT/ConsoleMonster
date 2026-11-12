---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolesetting/
schema: 2.0.0
---
# Show-ConsoleSetting
## SYNOPSIS
Chooses ConsoleMonster's settings in the terminal with a live preview.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleSetting [<CommonParameters>]
```

## DESCRIPTION
A full-screen picker: each setting on the left, and on the right a preview with a table, statuses, progress, a chart and an error drawn as the highlighted choice would draw them, so themes and glyph sets can be compared before choosing. Save keeps the choices for every session (Set-ConsoleSetting -Save), "Use now" applies them to this session only, and Escape or Cancel changes nothing. A setting that an environment variable sets is shown locked with the variable's name. In a non-interactive session it lists the settings and how to change them instead.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleSetting; Get-ConsoleSetting
```

Choose with the preview, then list what applies now and where each value came from

### EXAMPLE 2
```powershell
Show-ConsoleSetting
            Get-ConsoleSetting -Name Theme | Show-ConsoleTable
```

Choose settings and inspect the resulting theme

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.ConsoleSetting`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/show-consolesetting/](https://consolemonster.dev/powershell/commands/show-consolesetting/)
