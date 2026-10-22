---
title: Charts from the pipeline
description: Turn numeric properties into bars, distributions and grouped trends with Out-ConsoleChart.
slug: pipeline-charts
order: 15
group: hosts
card: pipeline-charts-60
chips: [PowerShell, Bars, Histograms, Trends]
apis: [Out-ConsoleChart, PipelineChart]
summary: Numeric pipeline properties as bars, distributions and grouped trends.
---

`Out-ConsoleChart` turns numbers or a numeric property into a chart. Start with horizontal bars, with the values beside them and colours from the current theme:

Install and import the module in PowerShell 5.1 or 7 before running the examples:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
```

```powershell
Get-Process | Out-ConsoleChart -Property CPU -Label ProcessName -Sort Descending -Top 10 -Unit ' s'
```

`-Sort Ascending` or `-Sort Descending` orders bars by value before `-Top` takes the first N. Ties retain input order. Without `-Sort`, bars keep input order. The label defaults to `Name`, then the row number; `-Label` selects another property. Repeated labels remain separate observations. Negative bars extend to the left of a shared zero baseline.

{{< cm-frame id="pipeline-charts-120" script="true" >}}

The figure uses fixed illustrative samples. Compose a chart with other widgets using `-AsWidget` inside a script block:

```powershell
New-ConsolePanel -Title 'CPU time' {
    Get-Process | Out-ConsoleChart -Property CPU -Sort Descending -Top 5 -Unit ' s' -AsWidget
} | Write-ConsoleWidget
```

Property selection uses the same binding as the formatting commands: one name, a wildcard matching one property, or a calculated-property hashtable. For example, show file sizes in kilobytes:

```powershell
Get-ChildItem -File | Out-ConsoleChart -Property @{
    Name = 'KB'
    Expression = { $_.Length / 1KB }
} -Label Name -Sort Descending -Top 8 -Unit ' KB'
```

## Distributions

Pipe numbers directly or select a property. `-Bins` requests 1 through 128 buckets; the default is 10.

```powershell
$latencies | Out-ConsoleChart -Type Histogram -Bins 8 -Unit ' ms'
$requests | Out-ConsoleChart -Property Duration -Type Histogram -Bins 6
```

Buckets include their lower edge and exclude their upper edge, except the final bucket, which includes the maximum. Integral input uses whole-number edges; a small integral range or constant input uses fewer buckets. Every valid observation contributes to the distribution: `-Top` and `-Sort` do not filter histogram input. The count axis uses integers.

## Trends by group

For sparklines, `-Label` identifies the groups. Each group gets one line, in the order its label first appears. Samples within a group retain arrival order. Sort timestamped data before piping it:

```powershell
$samples | Sort-Object Time |
    Out-ConsoleChart -Property Latency -Label Server -Type Sparkline -Unit ' ms'
```

Without a label property, scalar values form one unnamed trend. `-Sort` orders groups by their latest value; `-Top` then limits the number of groups. Neither option changes the order of samples inside a group. The existing sparkline renderer scales from zero, so a group containing negative values is shown as numerical text to preserve its signs.

{{< cm-frame id="pipeline-charts-60" >}}

## Plain text and invalid values

Without colour, the output lists bar values, histogram bucket counts or every sample in each trend. It stays useful in redirected output and SSH sessions. Use `-AsString` to capture one text block reliably on Windows PowerShell 5.1 and PowerShell 7:

```powershell
$text = $samples | Out-ConsoleChart -Property Latency -Label Server -AsString
$samples | Out-ConsoleChart -Property Latency -Label Server | Out-File ./latency.txt
```

Null or missing properties, malformed values, booleans, NaN and infinity produce a non-terminating error and are skipped. Numeric strings use invariant notation such as `12.5` or `1e3`. Add `-ErrorAction Stop` when an invalid sample should stop the command. Empty input produces no output. The command collects its input before drawing; it does not follow a live stream or infer timestamps.

## C# fluent API

The cmdlet binds PowerShell properties and calls the same core `PipelineChart` used by C#:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var chart = new PipelineChart()
    .WithSort(PipelineChartSort.Descending)
    .WithTop(3)
    .WithUnit(" s")
    .Add("web", 34)
    .Add("jobs", 72)
    .Add("api", 18);
Terminal.Write(chart);

var histogram = new PipelineChart()
    .WithType(PipelineChartType.Histogram)
    .WithBins(4)
    .Add(null, 12)
    .Add(null, 18)
    .Add(null, 25);
Terminal.Write(histogram);
```

`Add` accepts finite doubles and rejects NaN or infinity. `TryNumber` checks numeric primitives, decimals and invariant numeric strings before adding them. `Build()` returns the existing bar, histogram or sparkline widgets for composition; `Build(textOnly: true)` returns their numerical fallback. Rendering the `PipelineChart` itself chooses that fallback from terminal capabilities.

Next, [compose widgets in the gallery](/widgets/), [choose a theme](/docs/themes/), or read the [Out-ConsoleChart reference](/powershell/commands/out-consolechart/).
