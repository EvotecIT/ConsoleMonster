---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Out-ConsoleChart
## SYNOPSIS
Draws horizontal bars, a histogram or grouped sparklines from pipeline objects.

Select one numeric property, including a calculated property, or pipe numbers directly.
Bars show value labels in theme colours. Sorting is stable and happens before Top. Sparkline labels identify groups;
samples within each group retain arrival order, so sort timestamped input before piping it. Histogram uses every
valid sample regardless of Sort or Top, and integral data has whole-number bucket edges. Null, malformed, boolean
and non-finite values produce a non-terminating error and are skipped; an empty input produces no output.
No-colour output lists numeric values, bucket counts or all trend samples. Signed trends also use numerical text
because the existing sparkline renderer scales from zero. Output uses the normal ConsoleMonster formatting route.

## SYNTAX
### __AllParameterSets
```powershell
Out-ConsoleChart [[-Property] <Object>] [-InputObject <psobject>] [-Label <Object>] [-Type <PipelineChartType>] [-Top <int>] [-Sort <PipelineChartSort>] [-Bins <int>] [-Unit <string>] [-Width <int>] [-AsString] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Draws horizontal bars, a histogram or grouped sparklines from pipeline objects.

Select one numeric property, including a calculated property, or pipe numbers directly.
Bars show value labels in theme colours. Sorting is stable and happens before Top. Sparkline labels identify groups;
samples within each group retain arrival order, so sort timestamped input before piping it. Histogram uses every
valid sample regardless of Sort or Top, and integral data has whole-number bucket edges. Null, malformed, boolean
and non-finite values produce a non-terminating error and are skipped; an empty input produces no output.
No-colour output lists numeric values, bucket counts or all trend samples. Signed trends also use numerical text
because the existing sparkline renderer scales from zero. Output uses the normal ConsoleMonster formatting route.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | Out-ConsoleChart -Property CPU -Label ProcessName -Sort Descending -Top 10
```

The ten busiest processes, highest CPU first

### EXAMPLE 2
```powershell
Get-ChildItem -File | Out-ConsoleChart -Property @{ Name = 'KB'; Expression = { $_.Length / 1KB } } -Type Histogram -Bins 8
```

Distribution of file sizes with a calculated property

### EXAMPLE 3
```powershell
$samples | Sort-Object Time | Out-ConsoleChart -Property Latency -Label Server -Type Sparkline
```

One trend per server, in timestamp order

### EXAMPLE 4
```powershell
1, 3, 2, 5 | Out-ConsoleChart -Type Histogram -Bins 4 -AsString
```

Capture a chart as plain text on Windows PowerShell 5.1 or PowerShell 7

## PARAMETERS

### -AsString
Return one plain-text string for reliable assignment and redirection on every supported PowerShell version. Cannot be combined with AsWidget.

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

### -AsWidget
Return the PipelineChart widget for layout composition or frame export. Cannot be combined with AsString.

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

### -Bins
Requested histogram bucket count, 1 through 128 (default 10). Small integral ranges and identical values use fewer buckets to avoid fractional or repeated edges.

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

### -InputObject
Numbers or objects containing the numeric property, supplied directly or through the pipeline. Explicit null samples produce a non-terminating invalid-data error; no input produces no output.

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

### -Label
A property name or calculated-property hashtable for bar labels and sparkline group names. Defaults to Name, then row number for bars or one unnamed trend for scalars. Labels are literal text.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
One numeric property name, wildcard matching one property, or calculated-property hashtable. Uses the raw numeric expression result regardless of FormatString. Defaults to Value for scalar input.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sort
None (default), Ascending or Descending by numeric value; trends use the latest sample. Ties retain input order. Ignored for Histogram.

```yaml
Type: PipelineChartSort
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Ascending, Descending

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Top
Show this many bars or sparkline groups after sorting. Zero (default) shows all. Histogram always uses every valid sample.

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

### -Type
Bar (default), Histogram or Sparkline. Each sparkline group is one line and preserves sample arrival order.

```yaml
Type: PipelineChartType
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Bar, Histogram, Sparkline

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
Unit suffix appended to numerical values and histogram edges, for example ' ms'.

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

### -Width
Layout width in terminal cells; zero uses the current terminal width.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.String`
- `ConsoleMonster.Widgets.PipelineChart`

## RELATED LINKS

- None
