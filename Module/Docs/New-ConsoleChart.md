---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleChart
## SYNOPSIS
Creates one numeric chart representation from records or samples.

Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts chart records (Label and Value; Key, Label, Values and Role for series); Content emits the same records. For Bar, Histogram and Sparkline, -Property picks the number from ordinary objects and -Label (Bar only, default Name) their label. Use Update-Console for live changes.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleChart [[-WidgetId] <string>] [[-InputObject] <psobject>] [-Type <ChartType>] [-Unit <string>] [-Title <Object>] [-Property <Object[]>] [-Label <Object>] [-Height <Int32>] [-Minimum <Double>] [-Maximum <Double>] [-Role <ThemeToken>] [-Shape <BarShape>] [-Warning <Double>] [-Critical <Double>] [-NoLatest] [-Edges <double[]>] [-Values <double[]>] [-ValueColumn <ColumnFormat>] [-CountColumn <ColumnFormat>] [-PercentColumn <ColumnFormat>] [-LabelColumn <ColumnFormat>] [-Content <scriptblock>] [-Series <Object[]>] [-NoValues] [-Signed] [-NoLegend] [-MaxLegendColumns <int>] [-Capacity <Int32>] [<CommonParameters>]
```

## DESCRIPTION
Creates one numeric chart representation from records or samples.

Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts chart records (Label and Value; Key, Label, Values and Role for series); Content emits the same records. For Bar, Histogram and Sparkline, -Property picks the number from ordinary objects and -Label (Bar only, default Name) their label. Use Update-Console for live changes.

## EXAMPLES

### EXAMPLE 1
```powershell
$servers = [pscustomobject]@{ Name = 'web01'; Cpu = 42 }, [pscustomobject]@{ Name = 'web02'; Cpu = 27 }; $servers | New-ConsoleChart -Property Cpu -Label Name -Unit '%'
```

Chart a property of ordinary objects inside a report

### EXAMPLE 2
```powershell
[pscustomobject] @{ Label = 'Cloudflare'; Value = 12 } | New-ConsoleChart -Type Bar -Unit ' ms' | Show-Console
```

Show resolver latency

## PARAMETERS

### -Capacity
Retained Line, Scatter or Sparkline samples; omitted uses the selected kind default.

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

### -Content
A script block emitting the same records accepted by InputObject.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountColumn
Stacked count-column settings.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Critical
The critical threshold for Bar.

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

### -Edges
Strictly ascending Histogram bucket edges.

```yaml
Type: Double[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Plot rows; omitted uses the selected kind default.

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
Records, numeric samples or a dictionary of labels to values/series.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Label
For Bar with -Property: the label property or expression; default Name.

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

### -LabelColumn
Stacked legend-label settings.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Maximum
The upper numeric limit.

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

### -MaxLegendColumns
Maximum Stacked legend columns; zero fits the available width.

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

### -Minimum
The lower numeric limit.

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

### -NoLatest
Hides the latest Sparkline value.

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

### -NoLegend
Hides the Stacked-chart legend.

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

### -NoValues
Hides Bar-chart numeric value labels.

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

### -PercentColumn
Stacked percent-column settings.

```yaml
Type: ColumnFormat
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
For Bar, Histogram or Sparkline: the numeric property of ordinary input objects (a name, pattern or calculated property).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Role
The chart theme role.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus, Strong, Low, Critical, Gap

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Series
Initial Line/Scatter series names, or records with Key, Label, Role and Values.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shape
The bar shape, Block by default.

```yaml
Type: BarShape
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Block, Line

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Signed
Shows signed Bar values on both sides of zero.

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

### -Title
The Sparkline caption shown before the trend.

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

### -Type
The numeric chart kind: Bar, Stacked, Line, Scatter, Histogram or Sparkline.

```yaml
Type: ChartType
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Bar, Stacked, Line, Scatter, Histogram, Sparkline

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
The unit on numeric labels.

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

### -ValueColumn
Bar numeric-column settings.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Values
Initial numeric samples for Histogram or Sparkline.

```yaml
Type: Double[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Warning
The warning threshold for Bar.

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

### -WidgetId
The live identifier; omitted uses the selected kind default.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
