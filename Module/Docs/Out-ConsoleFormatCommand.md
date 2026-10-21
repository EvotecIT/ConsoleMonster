---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Out-ConsoleFormatCommand
## SYNOPSIS
Renders native PowerShell formatting records through ConsoleMonster.

The host-only sink used by Enable-ConsoleFormatting -Feature FormatCommands. PowerShell still resolves properties, calculated expressions, grouping and named views. Unrecognized formatting records are passed to the native Out-Default. Use -AsWidget to export the same view with Export-ConsoleFrame.

## SYNTAX
### __AllParameterSets
```powershell
Out-ConsoleFormatCommand [-InputObject <psobject>] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Renders native PowerShell formatting records through ConsoleMonster.

The host-only sink used by Enable-ConsoleFormatting -Feature FormatCommands. PowerShell still resolves properties, calculated expressions, grouping and named views. Unrecognized formatting records are passed to the native Out-Default. Use -AsWidget to export the same view with Export-ConsoleFrame.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | Microsoft.PowerShell.Utility\Format-Table Name, Id | Out-ConsoleFormatCommand -AsWidget | Export-ConsoleFrame -Path processes.html
```

Export a native table using ConsoleMonster's frame exporter

## PARAMETERS

### -AsWidget
Returns the view widget for composition or export.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Native Format-Table, Format-List or Format-Wide output records.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
