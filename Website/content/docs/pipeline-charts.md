---
title: Charts from the pipeline
description: Turn numeric properties into bars, distributions and grouped trends with Show-Console -View Chart.
slug: pipeline-charts
order: 15
group: hosts
card: pipeline-charts-60
chips: [PowerShell, Bars, Histograms, Trends]
apis: [Show-Console -View Chart, PipelineChart]
summary: Numeric pipeline properties as bars, distributions and grouped trends.
---

`Show-Console -View Chart` turns numbers, or a numeric property of the objects you pipe, into horizontal bars, a distribution or one trend per group, with the values beside them and colours from the current theme.

## Try it

{{< cm-try key="charts" ids="pipeline-charts-120" project="PipelineCharts" caption="Bars, a distribution and grouped trends from fixed illustrative samples." >}}

## Bars

On real data it is one pipeline:

```powershell
Get-Process | Show-Console -Property CPU -Label ProcessName -Sort Descending -MaxRows 10 -Unit ' s' -View Chart
```

`-Sort Ascending` or `-Sort Descending` orders bars by value before `-MaxRows` takes the first N; ties keep their input order, and without `-Sort` bars keep input order. The label defaults to `Name`, then the row number; `-Label` picks another property. Repeated labels stay separate observations, and negative bars extend to the left of a shared zero baseline. With `-AsWidget` the chart composes with other widgets:

```powershell
New-ConsolePanel -Title 'CPU time' -Content {
    Get-Process | Show-Console -Property CPU -Sort Descending -MaxRows 5 -Unit ' s' -AsWidget -View Chart
} | Show-Console
```

Property selection uses the same binding as the formatting commands: one name, a wildcard matching one property, or a calculated-property hashtable. For example, show file sizes in kilobytes:

```powershell
Get-ChildItem -File | Show-Console -Property @{
    Name = 'KB'
    Expression = { $_.Length / 1KB }
} -Label Name -Sort Descending -MaxRows 8 -Unit ' KB' -View Chart
```

## Distributions

Pipe numbers directly or select a property. `-Bins` requests 1 through 128 buckets; the default is 10.

```powershell
$latencies | Show-Console -Type Histogram -Bins 8 -Unit ' ms' -View Chart
$requests | Show-Console -Property Duration -Type Histogram -Bins 6 -View Chart
```

Buckets include their lower edge and exclude their upper edge, except the final bucket, which includes the maximum. Integral input uses whole-number edges; a small integral range or constant input uses fewer buckets. Every valid observation contributes to the distribution: `-MaxRows` and `-Sort` do not filter histogram input. The count axis uses integers.

## Trends by group

For sparklines, `-Label` identifies the groups. Each group gets one line, in the order its label first appears. Samples within a group retain arrival order. Sort timestamped data before piping it:

```powershell
$samples | Sort-Object Time |
    Show-Console -Property Latency -Label Server -Type Sparkline -Unit ' ms' -View Chart
```

Without a label property, scalar values form one unnamed trend. `-Sort` orders groups by their latest value; `-MaxRows` then limits the number of groups. Neither option changes the order of samples inside a group. The existing sparkline renderer scales from zero, so a group containing negative values is shown as numerical text to preserve its signs.

{{< cm-frame id="pipeline-charts-60" script="false" caption="The same samples at 60 columns." >}}

## Plain text and invalid values

Without colour, the output lists bar values, histogram bucket counts or every sample in each trend. It stays useful in redirected output and SSH sessions. Use `-AsString` to capture one text block reliably on Windows PowerShell 5.1 and PowerShell 7:

```powershell
$text = $samples | Show-Console -Property Latency -Label Server -AsString -View Chart
$samples | Show-Console -Property Latency -Label Server -View Chart | Out-File ./latency.txt
```

Null or missing properties, malformed values, booleans, NaN and infinity produce a non-terminating error and are skipped. Numeric strings use invariant notation such as `12.5` or `1e3`. Add `-ErrorAction Stop` when an invalid sample should stop the command. Empty input produces no output. The command collects its input before drawing; it does not follow a live stream or infer timestamps.

## C# fluent API

The cmdlet binds PowerShell properties and calls the same core `PipelineChart` used by C#:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var chart = new PipelineChart()
    .Sort(PipelineChartSort.Descending)
    .MaxRows(3)
    .Unit(" s")
    .Add("web", 34)
    .Add("jobs", 72)
    .Add("api", 18);
Terminal.Write(chart);

var histogram = new PipelineChart()
    .Type(PipelineChartType.Histogram)
    .Bins(4)
    .Add(null, 12)
    .Add(null, 18)
    .Add(null, 25);
Terminal.Write(histogram);
```

`Add` accepts finite doubles and rejects NaN or infinity. `TryNumber` checks numeric primitives, decimals and invariant numeric strings before adding them. `Build()` returns the existing bar, histogram or sparkline widgets for composition; `Build(textOnly: true)` returns their numerical fallback. Rendering the `PipelineChart` itself chooses that fallback from terminal capabilities.

## Related

[Compose widgets in the gallery](/widgets/), [choose a theme](/docs/themes/), or read the [Show-Console -View Chart reference](/powershell/commands/show-consolechart/).
