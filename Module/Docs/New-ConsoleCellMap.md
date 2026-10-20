---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleCellMap
## SYNOPSIS
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. To add cells later, pipe the map to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCellMap [[-Status] <Object[]>] [-Count <int>] [-MaxRows <int>] [-NoLegend] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. To add cells later, pipe the map to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
$states = @('Pass') * 28 + @('Fail') * 2 + @('Warn') + @('Running') * 4
New-ConsoleCellMap -Status $states -Count 40 | Write-ConsoleWidget
```

Forty items part-way through a run

### EXAMPLE 2
```powershell
$results | ForEach-Object Result | New-ConsoleCellMap | Write-ConsoleWidget
```

States from objects

## PARAMETERS

### -Count
The total number of items; cells beyond the given states are pending.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRows
The most rows of cells (0 = all); a last line counts the items not shown.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The live name (default cells).

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

### -NoLegend
Hide the legend line with each state's count.

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

### -Status
The item states, in order: Pass, Fail, Warn, Skip, Info, Running or Pending (pipeline input works too).

```yaml
Type: Object[]
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

- `System.Object[]`

## OUTPUTS

- `ConsoleMonster.Widgets.CellMap`

## RELATED LINKS

- None
