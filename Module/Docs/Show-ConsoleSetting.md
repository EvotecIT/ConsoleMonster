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
Settings picker
---------------

The list on the left shows settings and their available values. The preview on the right redraws sample tables, statuses, progress, charts and errors with the current choices. An environment-controlled setting displays its variable name and is locked.

Choose Use now to apply the choices to this session. Choose Save to apply and persist them for future sessions. Escape and Cancel discard changes. The picker closes without returning setting objects.

Settings list
-------------

When a full-screen picker is unavailable, the command returns the current setting objects and shows how to change a setting with Set-ConsoleSetting.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleSetting; Get-ConsoleSetting
```

Choose with the preview, then list what applies now and where each value came from

### EXAMPLE 2
```powershell
Show-ConsoleSetting
$settingOptions = @{
    Name = @('Theme')
}
Get-ConsoleSetting @settingOptions | Show-ConsoleTable
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
