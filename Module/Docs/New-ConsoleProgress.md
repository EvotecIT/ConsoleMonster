---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleProgress
## SYNOPSIS
Creates a progress bar or an indeterminate activity indicator.

The default bar represents a known total. Indeterminate creates a spinner. Update either through Update-Console. Structural parameter sets reject bar-only options on a spinner and spinner-only options on a bar; the shared C# Progress factory validates the same contract.

## SYNTAX
### Bar (Default)
```powershell
New-ConsoleProgress [[-WidgetId] <string>] [[-Label] <Object>] [-Status <StatusKind>] [-Value <double>] [-Total <double>] [-Shape <BarShape>] [-Columns <Object[]>] [-CountColumn <ColumnFormat>] [-NoCount] [-LabelWidth <Int32>] [-RateWindowSeconds <double>] [<CommonParameters>]
```

### Indeterminate
```powershell
New-ConsoleProgress [[-WidgetId] <string>] [[-Label] <Object>] -Indeterminate [-Status <StatusKind>] [-Detail <Object>] [-NoElapsed] [-StartedSeconds <Double>] [-FinishedSeconds <Double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a progress bar or an indeterminate activity indicator.

The default bar represents a known total. Indeterminate creates a spinner. Update either through Update-Console. Structural parameter sets reject bar-only options on a spinner and spinner-only options on a bar; the shared C# Progress factory validates the same contract.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleProgress -Label 'Upload' -Value 64 -Total 100 | Show-Console
```

Show completed work

### EXAMPLE 2
```powershell
New-ConsoleProgress resolve -Indeterminate -Label 'Resolving DNS' -Detail 'zone 4' | Show-Console
```

Show work with no known total

## PARAMETERS

### -Columns
Ordered builtin column names, Type/Alignment/Priority/MaxWidth records or typed ProgressColumn objects.

```yaml
Type: Object[]
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountColumn
The shared count column format.

```yaml
Type: ColumnFormat
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
The muted indeterminate activity detail.

```yaml
Type: Object
Parameter Sets: Indeterminate
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinishedSeconds
Initial indeterminate completion time on the session clock, in seconds.

```yaml
Type: Double
Parameter Sets: Indeterminate
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Indeterminate
Creates a spinner for work without a known total.

```yaml
Type: SwitchParameter
Parameter Sets: Indeterminate
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The activity label, including markup.

```yaml
Type: Object
Parameter Sets: Bar, Indeterminate
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LabelWidth
The fixed label width.

```yaml
Type: Int32
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoCount
Hides the default-on count in the classic bar layout.

```yaml
Type: SwitchParameter
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoElapsed
Hides the default-on indeterminate elapsed time.

```yaml
Type: SwitchParameter
Parameter Sets: Indeterminate
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RateWindowSeconds
Rate smoothing window in seconds; default ten.

```yaml
Type: Double
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shape
The progress bar shape; default Line.

```yaml
Type: BarShape
Parameter Sets: Bar
Aliases: None
Possible values: Block, Line

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartedSeconds
Initial indeterminate start time on the session clock, in seconds.

```yaml
Type: Double
Parameter Sets: Indeterminate
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The initial state; default Running.

```yaml
Type: StatusKind
Parameter Sets: Bar, Indeterminate
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Total
The planned total; default 100.

```yaml
Type: Double
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The completed value; default zero.

```yaml
Type: Double
Parameter Sets: Bar
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live identifier; default progress, or spinner with Indeterminate.

```yaml
Type: String
Parameter Sets: Bar, Indeterminate
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

- `ConsoleMonster.Widgets.ProgressBar`
- `ConsoleMonster.Widgets.Spinner`

## RELATED LINKS

- None
