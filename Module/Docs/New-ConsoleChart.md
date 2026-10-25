---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleChart
## SYNOPSIS
Creates one numeric chart representation from records or samples.

Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts ordinary records; Content emits the same records. Use Update-Console for live changes.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleChart [[-WidgetId] <string>] [[-InputObject] <psobject>] -Type <ChartType> [-Unit <string>] [-Label <Object>] [-Height <Int32>] [-Minimum <Double>] [-Maximum <Double>] [-Role <ThemeToken>] [-Shape <BarShape>] [-Warning <Double>] [-Critical <Double>] [-NoLatest] [-Edges <double[]>] [-Values <double[]>] [-ValueColumn <ColumnFormat>] [-CountColumn <ColumnFormat>] [-PercentColumn <ColumnFormat>] [-LabelColumn <ColumnFormat>] [-Content <scriptblock>] [-Series <Object[]>] [<CommonParameters>]
```

## DESCRIPTION
Creates one numeric chart representation from records or samples.

Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts ordinary records; Content emits the same records. Use Update-Console for live changes.

## EXAMPLES

### EXAMPLE 1
```powershell
[pscustomobject] @{ Label = 'Cloudflare'; Value = 12 } | New-ConsoleChart -Type Bar -Unit ' ms' | Show-Console
```

Show resolver latency

## PARAMETERS

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
The sparkline label.

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

### -Role
The chart theme role.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

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

### -Type
The numeric chart kind: Bar, Stacked, Line, Scatter, Histogram or Sparkline.

```yaml
Type: ChartType
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Bar, Stacked, Line, Scatter, Histogram, Sparkline

Required: True
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
