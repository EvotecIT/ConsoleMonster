---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/get-consolecapability/
schema: 2.0.0
---
# Get-ConsoleCapability
## SYNOPSIS
Shows what ConsoleMonster detected about the current terminal.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleCapability [-Refresh] [<CommonParameters>]
```

## DESCRIPTION
Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).

## EXAMPLES

### EXAMPLE 1
```powershell
Get-ConsoleCapability | Select-Object -ExpandProperty Decisions
```

Show the profile and the decisions

### EXAMPLE 2
```powershell
Get-ConsoleCapability | Select-Object Width, Height, Glyphs, ColorDepth | Show-ConsoleTable
```

Inspect the current terminal dimensions and output tiers

## PARAMETERS

### -Refresh
Detect again instead of returning the cached profile.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Capabilities.TerminalProfile`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/get-consolecapability/](https://consolemonster.dev/powershell/commands/get-consolecapability/)
