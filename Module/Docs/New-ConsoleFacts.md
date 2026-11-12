---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolefacts/
schema: 2.0.0
---
# New-ConsoleFacts
## SYNOPSIS
Creates a facts line with bold counts, semantic colours and shared separators.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFacts [[-InputObject] <psobject>] [[-Content] <scriptblock>] [<CommonParameters>]
```

## DESCRIPTION
Each fact is a value or a record with Value, Label, Caption, Presenter, Role and NewLine. Counts are bold, status words use the same colour, and neutral facts are muted. Facts wrap between items. ASCII uses | instead of the middle dot.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleFacts -Content { @{ Value = 3; Label = 'passed'; Role = 'Pass' }; @{ Value = 1; Label = 'warning'; Role = 'Warn' }; @{ Value = 5; Label = 'checks' } } | Show-Console
```

A report summary

### EXAMPLE 2
```powershell
$summary = [pscustomobject]@{ Value = 4; Label = 'servers online'; Role = 'Pass' }
$summary | New-ConsoleFacts | Show-Console
```

Show the number of online servers

## PARAMETERS

### -Content
A script block emitting values or records with Value, Label and an optional Role, Caption, Presenter and NewLine.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Values or fact records, in display order.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.ViewFacts`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolefacts/](https://consolemonster.dev/powershell/commands/new-consolefacts/)
