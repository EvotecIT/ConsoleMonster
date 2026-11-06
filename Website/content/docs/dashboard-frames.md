---
title: Dashboard frames
description: Declare a summary view with DashboardFrame in C#, a title line with facts, meters, a needs-attention list, sections, a legend and a footer, laid out for the terminal's width.
slug: dashboard-frames
order: 15
group: hosts
summary: "One frame for summary output: facts, meters, needs attention, sections, legend and footer."
apis: [DashboardFrame, Meter, AttentionList, FrameSection, FrameLegend, FrameFooter]
chips: [C#, Reports, Width tiers]
card: dashboard-frame-110
---

`DashboardFrame` (in `ConsoleMonster.Frames`) is the frame a product module declares for a summary view: the title line with the estate's facts and its state, a row of meters, a "Needs attention" list, summary sections packed beside it, detail sections, a legend and a footer. It prints once and scrolls like any output. The frame applies the width tiers, the ordering and the footer, so every summary built on it reads the same way.

Frames are declared in C#, next to the data model of the module that owns the objects. PowerShell shows them through that module's registered default display, so there is no `New-Console*` command for a frame and no fluent builder; the parity catalogue lists it as C# only.

## The program

`dotnet new console`, `dotnet add package ConsoleMonster`, then this as `Program.cs`. The estate and its numbers are fictional and nothing is queried.

{{< cm-frame id="dashboard-frame-110" caption="At 110 columns the attention list and the summary sections share a row; Disk use drops into the shorter column, and Backups, a detail section, follows." >}}

## How it lays out

- **Title line.** `Header.Facts` follow the title, and `Header.States` sit at the right edge: `FrameState.Of(HealthState.Failing, 1, "failing")`. When every state has a count of zero and nothing needs attention, the line says "all healthy" instead.
- **Meters.** `Meter.Composition`, `Meter.Ratio`, `Meter.Fact`, `Meter.Count` and `Meter.Missing`, in priority order. A row keeps as many as fit, first ones first.
- **Needs attention.** Each `AttentionItem` has a state, an identity, a reason, an order and values. It prints first in the summary rows whenever it has items, worst first, with "3 of 48 servers" from `Noun` and `OutOf`.
- **Sections.** A `FrameSection` builds its content for the width it gets. Summary sections pack side by side while their minimum widths fit; a later section drops into the shortest column. `Placement = FramePlacement.Detail` prints below, full width. `MinWidth`, `MaxWidth`, `Grow` and `FullWidth` shape the row.
- **Legend and footer.** The legend lists the symbols you add; the footer prints `Counts` and a hint: `FrameHint.EveryRow` ("pipe to Format-Table for every row") for a batch of pipeline objects, `EveryProperty` for one object, `None` for a frame that does not come from the pipeline, as here.

## Narrow terminals

The same program at 60 columns: the facts move under the title, meters stack two to a row and each section takes the whole width. Below 80 columns a frame with a `Compact` view prints that view instead, with the meter counts and the footer.

{{< cm-frame id="dashboard-frame-60" script="false" caption="The same program at 60 columns." >}}

On the ASCII tier every cell inside the frame, including your own widgets, is folded to printable ASCII without changing the layout.

## Next

[Pipeline charts](/docs/pipeline-charts/) draws numeric properties as charts for the sections, [tables](/tables/) covers the tables inside them, and the [readiness report](/demos/readiness-report/) builds a similar summary from ordinary widgets in PowerShell, C# and C# fluent.
