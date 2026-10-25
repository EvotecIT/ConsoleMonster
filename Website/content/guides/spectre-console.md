---
title: Coming from Spectre.Console
description: Compare C# markup, widgets, live sessions and prompts, with the behavior and gaps to check before migrating.
slug: spectre-console
order: 3
toc: false
card: migration-report-100
---

Keep your inventory collection and application logic, then replace the rendering and input boundaries. ConsoleMonster provides typed tables, composable widgets, prompts and event-driven live sessions in a core package with no package dependencies. The useful differences are adaptive object presentation, per-item runs and recording/export workflows; visual styles, Figlet fonts and renderable types are not interchangeable.

## API map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Spectre.Console | ConsoleMonster | Difference |
| --- | --- | --- |
| `AnsiConsole.MarkupLine`, `Markup.Escape` | `Terminal.WriteMarkupLine`, `Markup.Escape` | Similar tag dialect; escape collected data before interpolation. |
| `AnsiConsole.Write` | `Terminal.Write` | Accepts ConsoleMonster `IWidget`, not Spectre `IRenderable`. |
| `Table` | `Table`, `Table.For(rows)` | Typed presenters, priority columns and narrow-window cards. |
| `Tree`, `Panel`, `Rule` | `Tree`, `Panel`, `Rule` | New objects; no conversion of existing renderables. |
| `AnsiConsole.Progress()` | `ProgressBar`, `Lanes`, `LiveSession` | Update handles rather than Spectre task contexts. |
| `AnsiConsole.Status()` | `Terminal.Status`, `Terminal.StatusAsync` | Scoped status; context updates change the text. |
| `AnsiConsole.Live()` | `LiveSession`, `RegionWidget` | Named regions and typed handles; dispose the session. |
| `SelectionPrompt<T>`, `MultiSelectionPrompt<T>` | `SelectPrompt<T>`, `MultiSelectPrompt<T>` | Return selected values; configurable grouping and previews. |
| `TextPrompt<T>` | `TextPrompt`, `NumberPrompt`, `PathPrompt` | Pick the prompt for the value type; not one generic conversion API. |
| `ConfirmationPrompt` | `ConfirmPrompt`, `TypedConfirmPrompt` | Typed confirmation is available for consequential actions. |
| `BarChart`, `BreakdownChart` | `BarChart`, `StackedBar` | A stacked total is the nearest breakdown equivalent. |
| `FigletText` | `Logo` | Built-in pixel lettering; no `.flf` font loader equivalent. |
| `Grid`, `Columns`, `Rows` | `ConsoleMonster.Layout.Grid`, `Columns`, `Rows` | Grid layout is distinct from the interactive grid. |
| `Layout` | `DashboardBuilder`, `Rows`, `Columns`, `RegionWidget` | Compose the layout explicitly; no named split-tree API clone. |
| `Calendar`, emoji shortcodes, `Spectre.Console.Cli` | No direct equivalent | Use literal Unicode for emoji; keep a separate command-line parser. |

</div>

## Shared sample data

Use these rows for each comparison. The complete program under **Try it** includes its imports. For the snippets, import `Spectre.Console` on the left, and `ConsoleMonster`, `ConsoleMonster.Widgets`, `ConsoleMonster.Layout`, `ConsoleMonster.Live`, `ConsoleMonster.Interactive`, `ConsoleMonster.Building` and `ConsoleMonster.Text` on the right. Run the two versions separately to avoid type-name collisions. `Thread`, `Task`, LINQ and collection types use the ordinary .NET namespaces.

```csharp
var servers = new[] {
    new { Name = "FS01", Site = "Warsaw", Pending = 4, Status = "Pass" },
    new { Name = "SQL01", Site = "Dublin", Pending = 12, Status = "Warn" },
    new { Name = "WEB01", Site = "Berlin", Pending = 0, Status = "Pass" }
};
```

## 1. Markup, separators and a heading

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
AnsiConsole.MarkupLine($"[green]{Markup.Escape(servers[0].Name)}[/] ready");
AnsiConsole.Write(new Rule("Patch inventory"));
AnsiConsole.Write(new FigletText("Ops"));
```

</div>
<div>

**ConsoleMonster**

```csharp
Terminal.WriteMarkupLine($"[pass]{Markup.Escape(servers[0].Name)}[/] ready");
Terminal.Write(new Rule("Patch inventory"));
Terminal.Write(new Logo("Ops"));
```

</div>
</div>

`Logo` is an alternative heading style, not a Figlet font interpreter. ConsoleMonster supports `[[`/`]]`, nested tags, hex/rgb colors and theme roles. Unknown tags are lenient by default; set `Markup.Strict = true` for exceptions or use `Markup.Validate`. `:emoji:` shortcodes are not expanded. OSC 8 links depend on terminal support.

## 2. A table, a panel and a tree

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
var table = new Table().AddColumn("Name").AddColumn("Pending");
foreach (var s in servers)
    table.AddRow(Markup.Escape(s.Name), s.Pending.ToString());
AnsiConsole.Write(new Panel(table).Header("Patch inventory"));
var tree = new Tree("Sites");
foreach (var s in servers) tree.AddNode($"{s.Site}: {s.Name}");
AnsiConsole.Write(tree);
```

</div>
<div>

**ConsoleMonster**

```csharp
var table = Table.For(servers)
    .Column(s => s.Name)
    .Column(s => s.Pending);
Terminal.Write(new Panel(table, "Patch inventory"));
var tree = new Tree("Sites");
foreach (var s in servers) tree.Add($"{s.Site}: {s.Name}");
Terminal.Write(tree);
```

</div>
</div>

Use typed columns to retain numbers and choose presenters, aggregates and width priorities. Apply `Markup.Escape` to external text when constructing markup labels; do not assume table cells, prompts and markup writers all interpret strings identically.

{{< cm-try ids="migration-report-100,migration-report-48" project="InventoryReport" >}}

The full report above also includes site and status columns. Its wide and narrow output comes from that exact program, rather than from either comparison snippet.

## 3. Progress, a status line and replaceable live content

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
AnsiConsole.Progress().Start(ctx => {
    var task = ctx.AddTask("Inventory", maxValue: servers.Length);
    foreach (var s in servers) { Thread.Sleep(100); task.Increment(1); }
});
AnsiConsole.Status().Start("Resolving inventory", ctx => {
    foreach (var s in servers) ctx.Status($"Resolving {s.Name}");
});
AnsiConsole.Live(new Spectre.Console.Text("Starting")).Start(ctx => {
    foreach (var s in servers) {
        ctx.UpdateTarget(new Spectre.Console.Text(s.Name));
        ctx.Refresh();
    }
});
```

</div>
<div>

**ConsoleMonster**

```csharp
var bar = new ProgressBar("Inventory", "inventory") { Total = servers.Length };
using (var session = new LiveSession(bar)) {
    session.Start();
    var handle = bar.Handle(session);
    foreach (var s in servers) { Thread.Sleep(100); handle.Advance(); }
    handle.Complete();
}
Terminal.Status("Resolving inventory", ctx => {
    foreach (var s in servers) ctx.Update($"Resolving {s.Name}");
});
var region = new RegionWidget("current", new TextBlock("Starting"));
using (var session = new LiveSession(region)) {
    session.Start();
    foreach (var s in servers) session.Region("current").Set(new TextBlock(s.Name));
}
```

</div>
</div>

These loops simulate short collection steps; put your actual work there. ConsoleMonster handles post events to a session, whose render loop coalesces redraws. Do not expect one visible frame per update. Use unique widget IDs and dispose sessions in failure paths. A status scope removes its spinner when work exits; redirected output gets plain status updates. See [live sessions](/docs/live-sessions/) for output routing and [runs](/docs/live-runs/) for bounded per-item execution.

## 4. Selection, text and confirmation

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
string server = AnsiConsole.Prompt(new SelectionPrompt<string>()
    .Title("Target server").AddChoices(servers.Select(s => s.Name)));
string ticket = AnsiConsole.Prompt(new TextPrompt<string>("Change ticket"));
bool proceed = AnsiConsole.Prompt(new ConfirmationPrompt($"Review {server} for {ticket}?"));
```

</div>
<div>

**ConsoleMonster**

```csharp
string server = new SelectPrompt<string>("Target server", servers.Select(s => s.Name)).Show();
string ticket = new TextPrompt("Change ticket") { Required = true }.Show();
bool proceed = new ConfirmPrompt($"Review {server} for {ticket}?").Show();
```

</div>
</div>

Handle `PromptCanceledException` when the user cancels. For unattended execution pass values through your own application options; ConsoleMonster can use defaults or report a missing value instead of waiting indefinitely. [Interactive screens](/interactive/) show complete selection, text and typed-confirmation programs with recorded input.

## 5. Bars and proportions

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
var bars = new BarChart();
var breakdown = new BreakdownChart();
foreach (var s in servers) {
    bars.AddItem(s.Name, s.Pending, Spectre.Console.Color.Yellow);
    breakdown.AddItem(s.Name, s.Pending, Spectre.Console.Color.Yellow);
}
AnsiConsole.Write(bars);
AnsiConsole.Write(breakdown);
```

</div>
<div>

**ConsoleMonster**

```csharp
var bars = new BarChart();
var breakdown = new StackedBar();
foreach (var s in servers) {
    bars.Add(s.Name, s.Pending);
    breakdown.Add(s.Name, s.Pending);
}
Terminal.Write(bars);
Terminal.Write(breakdown);
```

</div>
</div>

Bars compare values; a stacked bar shows parts of a total. Review legend, zero-value and width behavior rather than expecting identical pixels. ConsoleMonster also offers `LineChart`, `Histogram` and `Sparkline`.

## 6. Grid, columns and application layout

<div class="cm-migration">
<div>

**Spectre.Console**

```csharp
var grid = new Grid().AddColumn().AddColumn();
grid.AddRow("Warsaw", "Dublin");
AnsiConsole.Write(grid);
AnsiConsole.Write(new Columns(new Panel("FS01"), new Panel("SQL01")));
var layout = new Spectre.Console.Layout("root")
    .SplitColumns(new Spectre.Console.Layout("inventory"), new Spectre.Console.Layout("notes"));
layout["inventory"].Update(new Panel("FS01"));
layout["notes"].Update(new Panel("12 pending on SQL01"));
AnsiConsole.Write(layout);
```

</div>
<div>

**ConsoleMonster**

```csharp
Terminal.Write(new ConsoleMonster.Layout.Grid(2,
    new TextBlock("Warsaw"), new TextBlock("Dublin")));
Terminal.Write(new Columns(
    new Panel(new TextBlock("FS01")), new Panel(new TextBlock("SQL01"))));
var dashboard = new DashboardBuilder("Inventory")
    .Panel("Inventory", new TextBlock("FS01"))
    .Panel("Notes", new TextBlock("12 pending on SQL01"))
    .BuildDashboard();
Terminal.Write(dashboard);
```

</div>
</div>

`DashboardBuilder` is a dashboard composition API, not a translation of Spectre's named split tree. Use `Rows`/`Columns` for exact local arrangements and named `RegionWidget` objects when content changes.

## Differences to know

- Keep collection separate from display. A widget is a definition; `Terminal.Write` draws it and `LiveSession` controls its live lifetime.
- Similar markup does not make the parsers interchangeable. Escape input and check strict-mode behavior, supported links, colors and emoji before moving a shared markup string.
- ConsoleMonster chooses color depth and glyph tier for the host, including ASCII and plain output. A Nerd Font is not required; literal emoji still depend on your terminal and font.
- Spectre renderables, custom renderers and extensions need rewriting against `IWidget`. Calendar and custom Figlet-font use have no direct migration. Check the [roadmap](https://github.com/EvotecIT/ConsoleMonster/blob/master/ROADMAP.md) for planned capabilities; an item there is not an implemented feature.

## What you get beyond this API mapping

**Everyday output:** the PowerShell module can present ordinary objects and opt into native output formatting; C# has typed presenters and object views. **Live runs:** track keyed item outcomes, progress, summaries and scrollback without managing each redraw. **Recording and export:** render frames to text, Markdown, HTML or SVG; record dashboard events or whole terminal sessions for their respective export/replay paths. **Demo scripts:** use [copyable demos](/demos/) and the [presenter tools](/docs/talks-and-demos/).

The **core** has zero package dependencies. Markdown display and export belong to the `ConsoleMonster.Markdown` add-on: C# uses `MarkdownExporter.Export`, while PowerShell loads the bundled add-on when `Export-ConsoleFrame -Format Markdown` is requested. The Markdown and Graphics add-ons have their own dependencies. The .NET 8/10 core targets are AOT-compatible, with reflection-based inspection requiring explicit trimming care; this is not a claim that every integration or the PowerShell module is a native AOT executable. See [installation and AOT limits](/docs/install/) and [recording formats](/docs/recording/).

## Sources and next steps

API comparisons use the [Spectre.Console documentation](https://spectreconsole.net/console) and its [widget reference](https://spectreconsole.net/widgets/table), checked 25 October 2026. No maintenance-status argument is needed to use this guide.

Next: [widget gallery](/widgets/), [three C# styles](/docs/csharp-styles/), [.NET reference](/api/) or [all migration guides](/guides/coming-from/).
