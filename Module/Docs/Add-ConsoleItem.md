---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Add-ConsoleItem
## SYNOPSIS
Adds items to a console widget made earlier: tree nodes, pairs, status items, bars, series, log lines, lanes or values.

The PowerShell way to grow a widget after it was created, instead of calling its .NET methods. Pipe the widget in and give the items as a script block or values; they are the same item commands the widget's own script block takes: Label/Children records for New-ConsoleTree, Key/Value records for New-ConsoleKeyValue, Key/Label/Status records for New-ConsoleStatusList and New-ConsolePhases, Label/Value records for New-ConsoleChart -Type Bar or Stacked, Key/Values records for New-ConsoleChart -Type Line or Scatter, Message/Level records for New-ConsoleLogRegion and Key/Label records for New-ConsoleLanes. Sparklines and histograms take numbers, a cell map takes status words, and a live table takes objects or hashtables using the table's selected properties and calculated columns. -PassThru writes the widget on, so it can go straight to Show-Console. For a widget inside a running dashboard, use Update-Console and Write-ConsoleLogEntry instead.

## SYNTAX
### __AllParameterSets
```powershell
Add-ConsoleItem [-Item] <Object> -InputObject <psobject> [-KeyProperty <string>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Adds items to a console widget made earlier: tree nodes, pairs, status items, bars, series, log lines, lanes or values.

The PowerShell way to grow a widget after it was created, instead of calling its .NET methods. Pipe the widget in and give the items as a script block or values; they are the same item commands the widget's own script block takes: Label/Children records for New-ConsoleTree, Key/Value records for New-ConsoleKeyValue, Key/Label/Status records for New-ConsoleStatusList and New-ConsolePhases, Label/Value records for New-ConsoleChart -Type Bar or Stacked, Key/Values records for New-ConsoleChart -Type Line or Scatter, Message/Level records for New-ConsoleLogRegion and Key/Label records for New-ConsoleLanes. Sparklines and histograms take numbers, a cell map takes status words, and a live table takes objects or hashtables using the table's selected properties and calculated columns. -PassThru writes the widget on, so it can go straight to Show-Console. For a widget inside a running dashboard, use Update-Console and Write-ConsoleLogEntry instead.

## EXAMPLES

### EXAMPLE 1
```powershell
$log = New-ConsoleLogRegion -Height 5
$log | Add-ConsoleItem { [pscustomobject]@{ Message = 'Discovered 4 domain controllers'; AtSeconds = 2 } }
$log | Add-ConsoleItem { [pscustomobject]@{ Message = 'DC02 lags by 42 minutes'; Level = 'Warning'; AtSeconds = 31 } }
$log | Show-Console
```

Add log lines as the script goes, then show the log

### EXAMPLE 2
```powershell
$tree = New-ConsoleTree 'Shares'
foreach ($share in Get-SmbShare) {
    $tree | Add-ConsoleItem { [pscustomobject]@{ Label = $share.Name; Detail = $share.Path } }
}
$tree | Show-Console
```

Grow a tree in a loop

### EXAMPLE 3
```powershell
New-ConsoleChart -Title 'req/s' -Type Sparkline | Add-ConsoleItem 3, 5, 8, 13, 9 -PassThru | Show-Console
```

Values for a sparkline, written on

## PARAMETERS

### -InputObject
The widget to add to (pipeline input): a tree or tree node, key/value list, status list, phases, bar chart, stacked bar, line chart, log, lanes, cell map, sparkline, histogram or live table.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Item
The items: a script block that emits them, or the items themselves.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -KeyProperty
For a live table: the property whose value is the row key (default the first column).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Writes the widget to the pipeline after adding.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
