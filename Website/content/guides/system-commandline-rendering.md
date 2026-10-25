---
title: Coming from System.CommandLine.Rendering
description: Replace the deprecated rendering package with widgets and live regions while keeping your command-line parser.
slug: system-commandline-rendering
order: 7
toc: false
card: migration-report-100
meta.seo_title: System.CommandLine.Rendering migration | ConsoleMonster
---

If your application used System.CommandLine.Rendering for terminal output, keep argument parsing in your chosen parser and replace the rendering layer independently. ConsoleMonster supplies widgets, layout, typed tables and live regions without a dependency on the command-line parser. This is an API migration, not a binary-compatible replacement.

## Package status

Microsoft's [19 June 2025 announcement](https://github.com/dotnet/command-line-api/issues/2576) lists System.CommandLine.Rendering among the packages being retired and removed from the repository. The [NuGet page](https://www.nuget.org/packages/System.CommandLine.Rendering) marks it deprecated and no longer maintained; its latest version is `0.4.0-alpha.25306.1`, last updated 19 June 2025. Checked 25 October 2026. This statement applies to the Rendering package, not System.CommandLine as a whole. Microsoft recommends Spectre.Console for rendering; ConsoleMonster is another option with its own API and tradeoffs.

The retired package targeted .NET Standard 2.0; it was not a Windows-only product. The migration reason here is package retirement, not a new cross-platform capability.

## API map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| System.CommandLine.Rendering | ConsoleMonster | Note |
| --- | --- | --- |
| `ConsoleRenderer` | `Terminal.Write`, `ConsoleExporter` | Print to a terminal or render a document explicitly. |
| `ContentView`, `TextSpan`, `TextSpanFormatter` | `TextBlock`, `RichText`, `Markup` | Different content/style model; rewrite formatters as presenters or rich text. |
| `TableView<T>` | `Table.For(rows)` / `Table<T>` | Typed columns and presenters, width negotiation and narrow cards. |
| `StackLayoutView`, `GridView` | `Rows`, `Columns`, `ConsoleMonster.Layout.Grid` | Widget composition rather than the old View tree. |
| `Region`, `RenderToRegion` | `RegionWidget` and `LiveSession.Region` for mutable content | Not a direct absolute-coordinate Region replacement. |
| `OutputMode` | `TerminalProfile` and capability detection | Choose an explicit fixed profile for reproducible tests. |
| `IConsole` injection | `ConsoleTarget`, `Terminal.Render`, `ConsoleExporter` | Different output boundary; keep parser injection separate. |

</div>

## Shared sample data

For the left snippets, reference the retired package in an old test application and import `System.CommandLine.Rendering`, `System.CommandLine.Rendering.Views` and `System.CommandLine.IO`. For the right snippets use `ConsoleMonster`, `ConsoleMonster.Widgets`, `ConsoleMonster.Layout` and `ConsoleMonster.Live`. Both use normal .NET collection/LINQ namespaces. These are operation snippets; the complete ConsoleMonster program below contains its imports.

```csharp
var servers = new[] {
    new { Name = "FS01", Site = "Warsaw", Pending = 4, Status = "Pass" },
    new { Name = "SQL01", Site = "Dublin", Pending = 12, Status = "Warn" },
    new { Name = "WEB01", Site = "Berlin", Pending = 0, Status = "Pass" }
};
```

## 1. Render a message

<div class="cm-migration">
<div>

**System.CommandLine.Rendering**

```csharp
var renderer = new ConsoleRenderer(new SystemConsole());
renderer.RenderToRegion("Inventory ready", new Region(0, 0, 80, 1));
```

</div>
<div>

**ConsoleMonster**

```csharp
Terminal.Write(new TextBlock("Inventory ready"));
```

</div>
</div>

Printing once is normally enough. Do not carry absolute screen coordinates into static output when the program only needs a report in scrollback.

## 2. Render typed rows

<div class="cm-migration">
<div>

**System.CommandLine.Rendering**

```csharp
var table = new TableView<(string Name, int Pending)>();
table.Items = servers.Select(s => (s.Name, s.Pending)).ToArray();
table.AddColumn(s => s.Name, "Name");
table.AddColumn(s => s.Pending, "Pending");
var renderer = new ConsoleRenderer(new SystemConsole());
table.Render(renderer, new Region(0, 0, 80, 12));
```

</div>
<div>

**ConsoleMonster**

```csharp
var table = Table.For(servers)
    .Column(s => s.Name)
    .Column(s => s.Pending);
Terminal.Write(table);
```

</div>
</div>

Both versions retain typed selectors and numeric values. The tuple projection gives the older TableView an explicit row type; a named inventory record works too.

{{< cm-try ids="migration-report-100,migration-report-48" project="InventoryReport" >}}

The complete report includes site/status columns as well as the two columns in the comparison.

## 3. Replace content in a live region

<div class="cm-migration">
<div>

**System.CommandLine.Rendering**

```csharp
var renderer = new ConsoleRenderer(new SystemConsole());
var region = new Region(0, 0, 80, 1);
foreach (var s in servers)
    renderer.RenderToRegion($"Reading {s.Name}", region);
```

</div>
<div>

**ConsoleMonster**

```csharp
var region = new RegionWidget("inventory", new TextBlock("Starting"));
using (var session = new LiveSession(region)) {
    session.Start();
    foreach (var s in servers)
        session.Region("inventory").Set(new TextBlock($"Reading {s.Name}"));
}
```

</div>
</div>

The live session owns redraws, output routing and cleanup. Updates can coalesce; a fast loop may only show the final state. A region identifies replaceable content in a layout, rather than a promise to write to a fixed desktop coordinate.

## Differences to know

- `View`, `TextSpan` and old formatter types do not implement ConsoleMonster `IWidget`. Migrate one presentation boundary at a time.
- Retain command parsing, help behavior and exit codes unless you separately intend to change them. ConsoleMonster does not replace System.CommandLine's parser.
- Capability detection selects color/glyph behavior. Use explicit dimensions and a fixed profile for output tests; redirected output should not rely on cursor movement.
- Typed selectors suit AOT applications. Reflection-driven object inspection needs trimming care; see [AOT limits](/docs/install/).
- If the old program depended on a custom formatter, viewport or coordinate scheme, port that behavior explicitly. There is no automatic View-to-widget adapter; consult the [roadmap](https://github.com/EvotecIT/ConsoleMonster/blob/master/ROADMAP.md) for gaps.

## Next

See [live sessions](/docs/live-sessions/), [tables](/tables/), [the Spectre comparison](/guides/spectre-console/) or [all migration guides](/guides/coming-from/).
