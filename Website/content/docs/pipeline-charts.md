---
title: Charts from the pipeline
description: Build numeric charts from pipeline properties, records and samples with New-ConsoleChart.
slug: pipeline-charts
order: 15
group: hosts
card: pipeline-charts-60
chips: [PowerShell, Bars, Histograms, Trends]
apis: [New-ConsoleChart, Chart, ChartOptions]
summary: Numeric pipeline properties, records and samples as reusable charts.
---

`New-ConsoleChart` constructs Bar, Stacked, Line, Scatter, Histogram or Sparkline widgets. Pipe the result to `Show-Console`, place it inside a panel, or export it without drawing.

## Try it

{{< cm-try key="charts" ids="pipeline-charts-120" project="PipelineCharts" caption="Bars, a distribution and grouped trends from fixed illustrative samples." >}}

## Bars from ordinary objects

Sort and select input before constructing the chart:

```powershell
Get-Process | Sort-Object CPU -Descending | Select-Object -First 10 |
    New-ConsoleChart -Type Bar -Property CPU -Label ProcessName -Unit ' s' | Show-Console
```

`-Property` selects the numeric value using a name, wildcard or calculated-property record. `-Label` selects the Bar label; it defaults to `Name`. Use `-Signed` for bars extending to either side of zero.

```powershell
New-ConsolePanel -Title 'CPU time' -Content {
    Get-Process | Sort-Object CPU -Descending | Select-Object -First 5 |
        New-ConsoleChart -Type Bar -Property CPU -Label ProcessName -Unit ' s'
} | Show-Console

Get-ChildItem -File | Sort-Object Length -Descending | Select-Object -First 8 |
    New-ConsoleChart -Type Bar -Property @{
        Name = 'KB'
        Expression = { $_.Length / 1KB }
    } -Label Name -Unit ' KB' | Show-Console
```

Explicit bar records use `Label` and `Value`; a dictionary maps labels to values. Numeric-column options, such as `-ValueColumn`, accept format records.

## Distributions

Histogram samples come from numeric pipeline input, `-Values`, or `-Property` on ordinary objects. Supply strictly ascending bucket edges:

```powershell
$latencies | New-ConsoleChart -Type Histogram -Edges 0, 10, 25, 50, 100 -Unit ' ms' | Show-Console
$requests | New-ConsoleChart -Type Histogram -Property Duration -Edges 0, 50, 100, 250, 500 | Show-Console
```

Use `-Height` for the plot height. Options are validated for the selected chart kind; a histogram does not accept bar-only label or threshold options.

## Trends and series

A Sparkline takes one ordered sequence. Group samples before construction when each server needs its own trend:

```powershell
$samples | Group-Object Server | ForEach-Object {
    $_.Group | Sort-Object Time |
        New-ConsoleChart -Type Sparkline -Property Latency -Title $_.Name -Unit ' ms'
} | Show-Console
```

Line and Scatter accept series records with `Key`, `Label`, `Values` and an optional `Role`. Stacked charts accept labelled values and support legend and numeric-column settings. For live samples, give the chart a `WidgetId` and use `Update-Console` inside a live session.

{{< cm-frame id="pipeline-charts-60" script="false" caption="The same samples at 60 columns." >}}

## Capture text

Export the constructed widget for assignment or a file on Windows PowerShell 5.1 and PowerShell 7:

```powershell
$text = $latencies | New-ConsoleChart -Type Sparkline -Unit ' ms' | Export-ConsoleFrame -Format Text
$latencies | New-ConsoleChart -Type Sparkline -Unit ' ms' | Export-ConsoleFrame -Format Text -Path ./latency.txt
```

Chart values must be finite numbers. Property binding reports invalid samples; use `-ErrorAction Stop` when they should stop construction. Constructors collect their input before returning a widget.

## C#

C# uses the same validated factory and kind-specific options:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var options = new ChartOptions { Type = ChartType.Bar, Unit = " s" };
options.Items.Add(new ChartItem("web", 34));
options.Items.Add(new ChartItem("jobs", 72));
options.Items.Add(new ChartItem("api", 18));
Terminal.Write(Chart.Create(options));

var histogram = Chart.Create(new ChartOptions {
    Type = ChartType.Histogram,
    Edges = new double[] { 0, 10, 20, 30 },
    Values = new double[] { 12, 18, 25 }
});
Terminal.Write(histogram);
```

## Related

[Compose widgets in the gallery](/widgets/), [choose a theme](/docs/themes/), or read the [New-ConsoleChart reference](/powershell/commands/new-consolechart/).
