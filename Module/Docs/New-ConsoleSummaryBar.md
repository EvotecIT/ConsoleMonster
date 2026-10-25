---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleSummaryBar
## SYNOPSIS
Creates a summary bar: one line with progress, done of total, the time left and result counters.

The line Invoke-ConsoleRun ends its view with, as a widget of its own: a thin bar, done of total, the percentage, the time left (estimated from -ElapsedSeconds and the share done) and counters in a fixed order with zeros dimmed rather than hidden. -Counter maps labels to counts; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour. Without -Total only the count done shows.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSummaryBar [[-WidgetId] <string>] [[-Value] <double>] [[-Total] <double>] [-Counter <IDictionary>] [-ElapsedSeconds <Double>] [-BarWidth <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates a summary bar: one line with progress, done of total, the time left and result counters.

The line Invoke-ConsoleRun ends its view with, as a widget of its own: a thin bar, done of total, the percentage, the time left (estimated from -ElapsedSeconds and the share done) and counters in a fixed order with zeros dimmed rather than hidden. -Counter maps labels to counts; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour. Without -Total only the count done shows.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleSummaryBar -Done 30 -Total 40 -ElapsedSeconds 42 -Counter ([ordered] @{ passed = 27; failed = 2; skipped = 1 }) | Show-Console
```

A run three quarters through

## PARAMETERS

### -BarWidth
The bar width in cells (default 20; narrow space shrinks it).

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

### -Counter
Labels mapped to counts, in the order to show them; status words (passed, failed, skipped...) take their status symbol and colour.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ElapsedSeconds
Elapsed seconds used to estimate remaining time. Fractional seconds are accepted.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Total
Items in total (0 = unknown: no bar, percentage or time left).

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
Items done.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live name (default summary).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.SummaryBar`

## RELATED LINKS

- None
