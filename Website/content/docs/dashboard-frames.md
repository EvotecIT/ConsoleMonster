---
title: Dashboard frames
description: Build a one-screen summary with New-ConsoleDashboardFrame or DashboardFrame, a title line with facts, meters, a needs-attention list, sections, a legend and a footer, laid out for the terminal's width.
slug: dashboard-frames
order: 15
group: apps
summary: "One frame for a summary: facts, meters, needs attention, sections, legend and footer."
apis: [New-ConsoleDashboardFrame, DashboardFrame, Ui.DashboardFrame, Meter, AttentionItem, FrameSection]
chips: [Reports, Width tiers, PowerShell, C#]
card: dashboard-frame-110
---

A dashboard frame is a summary that prints once and says how something is doing: the title line with the scope's facts and its state, a row of meters, a "Needs attention" list with the worst problems first, summary sections packed beside it, detail sections below, a legend and a footer. You describe the parts; the frame decides where they go for the width it gets, so every summary built on it reads the same way. `New-ConsoleDashboardFrame` takes the parts as records, `DashboardFrame` takes them as objects in C#, and `Ui.DashboardFrame()` chains them in C# fluent.

## Try it

The estate and its numbers are fictional and nothing is queried. The three programs print the same cells.

{{< cm-try key="dashboard" ids="server-estate-frame" project="ServerEstate" caption="At 110 columns the attention list and the sites share a row, and the backups, a detail section, follow below." >}}

## How it works

Each part is a record in PowerShell and an object in C#. Meters pick their kind from the fields they carry: `Healthy` with `Warning` and `Failing` is a composition bar, `Healthy` with `Whole` is a ratio, a number `Value` is a count and a text `Value` is a fact.

```powershell
New-ConsoleDashboardFrame -Title 'Server estate' -Fact 'contoso.example', '48 servers' -Meter @(
    @{ Label = 'Servers healthy'; Healthy = 44; Warning = 3; Failing = 1 }
    @{ Label = 'Backups current'; Healthy = 46; Whole = 48 }
) -Attention @{ State = 'Failing'; Identity = 'SQL-03'; Reason = 'Disk D: 97% full'; Values = '97%' } -AttentionNoun server -AttentionOutOf 48 -Section @{
    Title = 'Sites'; Content = { $sites | New-ConsoleTable }
} | Show-Console
```

The fluent builder names the same parts:

```csharp
Terminal.Write(Ui.DashboardFrame("Server estate")
    .Fact("contoso.example", "48 servers")
    .Meter(Meter.Composition("Servers healthy", 44, 3, 1))
    .AttentionOf("server", 48)
    .Attention(HealthState.Failing, "SQL-03", "Disk D: 97% full", "97%")
    .Section("Sites", c => c.Add(sites)));
```

- **Title line.** Facts follow the title and states (`State`, `Count`, `Word`) sit at the right edge. When every state counts zero and nothing needs attention, the line says "all healthy". `-ObservedAt` adds how long ago the data was taken.
- **Meters** print in the order given; a narrow row keeps the first ones.
- **Needs attention** prints first in the summary rows whenever it has items, worst state first, then by `Order`, with "3 of 48 servers" from `-AttentionNoun` and `-AttentionOutOf`.
- **Sections** build their content once. Summary sections pack side by side while their minimum widths fit, and a later one drops into the shortest column; `Placement = 'Detail'` prints a section below at full width. `MinWidth`, `MaxWidth`, `Grow` and `FullWidth` shape the row.
- **Legend and footer.** The legend lists the states you name; the footer prints the counts. A frame the script builds has no footer hint; module views use `EveryRow` ("pipe to Format-Table for every row").

## Variations

The same program at 60 columns: the facts move under the title, meters stack two to a row and each section takes the whole width.

{{< cm-frame id="dashboard-frame-60" script="false" caption="The server estate program at 60 columns." >}}

- `-Length Summary` leaves out the detail sections and `Full` lifts the row caps; without it the frame follows the session's preference.
- On the ASCII tier every cell inside the frame, including your own widgets, is folded to printable ASCII without changing the layout.
- A module that owns summary objects declares its frame in C#, next to its data model, and registers it as their default display; `DashboardFrame.Compact` then gives it a narrow view of its own below 80 columns.

## Related

The [server estate demo](/demos/server-estate/) is the program above with its download and run lines, and the [readiness report](/demos/readiness-report/) builds a similar summary from ordinary widgets. [Tables](/tables/) covers the tables inside the sections.

## Next

[Pipeline charts](/docs/pipeline-charts/) draws numeric properties as charts you can put in a section, and [host overview](/docs/host-overview/) is a finished read-only summary for a login.
