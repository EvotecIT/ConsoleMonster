---
title: How it works
description: "The model behind every ConsoleMonster command: build a widget, compose widgets into a layout, then print it, run it live or export it."
slug: how-it-works
order: 3
group: start
card: how-it-works-80
apis: [New-ConsoleRows, Show-Console, Write-ConsoleText, Export-ConsoleFrame, Terminal.Write, Ui.Rows]
chips: [Widgets, Layout, Output]
summary: "Widgets are definitions you build, compose and then render, print live or export."
---

This page explains the one model behind every command and class: a widget is a definition that is built first and drawn later, so the same report can be printed, kept on screen while work runs, or saved as HTML. The example builds a small report from three invented services and prints it once.

## Try it

{{< cm-try key="how-it-works" ids="how-it-works-80" project="ServiceCheck" caption="A rule, a facts line and a panel holding a table, printed once at 80 columns." >}}

## Build, then render

Nothing is drawn while the report is being built. `New-ConsoleRows`, `New-ConsoleRule`, `New-ConsoleFacts`, `New-ConsolePanel` and `New-ConsoleTable` each return a widget object, and containers take their children in `-Content`. Only the last line draws:

<div class="cm-lang" data-lang="powershell">

```powershell
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[accent bold]Service check[/]'
    New-ConsolePanel -Title 'Services' -Content {
        $services | New-ConsoleTable -Property Name, Server, Status -StatusProperty Status
    }
}
$report | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Rows report = new Rows(
    new Rule("[accent bold]Service check[/]"),
    new Panel(table, "Services")) { Gap = 1 };
Terminal.Write(report);
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
Ui.Rows().Gap(1).Content(c =>
{
    c.Rule("[accent bold]Service check[/]");
    c.Panel(p => p.Title("Services").Content(inner => inner.Add(table)));
}).Show();
```

</div>

Because a widget is only a definition, it can be kept in a variable, passed to a function, put inside another layout or rendered more than once. It measures itself against the width it is given when it is drawn, so the same object fits a 60-column window and a 200-column one.

## The command families

The PowerShell verbs say what a command does with a widget:

| Commands | What they do | In C# |
|---|---|---|
| `New-Console*` | Build a widget and return it; nothing is printed. | `new Table(...)`, `new Panel(...)`, or `Ui.Table()`, `Ui.Panel()` |
| `Show-Console*` | Print a widget, or objects piped in, once. | `Terminal.Write(widget)` or `widget.Show()` |
| `Write-Console*` | Write text: a line with markup (`Write-ConsoleText`), a log line (`Write-ConsoleLogEntry`). | `Terminal.WriteMarkupLine(...)` |
| `Invoke-Console*`, `Start-Console*` | Run work while a widget stays on screen and updates: a dashboard, a per-item run, a test run. | `LiveSession`, `ConsoleRun` |
| `Export-Console*` | Save a widget or a recording as HTML, SVG, Markdown, text or PNG. | `ConsoleExporter.Save(...)` or `widget.Export(path)` |

`Show-Console` also takes ordinary objects: `Get-Service | Show-Console` picks a table, a record or a tree from the shape of the data. The [widgets](/widgets/) and [tables](/tables/) pages list everything that can be built, each with its PowerShell, C# and fluent forms.

## Composing layouts

Four containers arrange everything else, and they nest freely:

- `New-ConsoleRows` (`Rows`) puts widgets one below another; `-Gap` adds blank rows between them.
- `New-ConsoleColumns` (`Columns`) puts them side by side and stacks them again when the window is too narrow.
- `New-ConsolePanel` (`Panel`) draws a border with a title around one child.
- `New-ConsoleLayoutGrid` (`LayoutGrid`) fills equal columns row by row.

[Layout](/docs/layout/) covers sizing, alignment, padding, layers and scrolling.

## Markup and theme roles

Text takes Spectre-style markup: `[bold]`, `[accent]`, `[muted]`, `[pass]`, `[warn]`, `[fail]`, closed with `[/]`. The colour words are roles, not colours: `[pass]` is green in the dark theme, a darker green in the light theme, and still readable when colour is off, because status words keep their symbol. The same roles appear as `-Role Pass` in PowerShell and `ThemeToken.Pass` in C#, so a facts line, a status column and a bar all agree. [Themes](/docs/themes/) shows the roles and how to change them.

<div class="cm-lang" data-lang="powershell">

```powershell
Write-ConsoleText -Markup '[accent bold]Deploy[/] finished: [pass]3 passed[/], [fail]1 failed[/]'
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Terminal.WriteMarkupLine("[accent bold]Deploy[/] finished: [pass]3 passed[/], [fail]1 failed[/]");
```

</div>

## Where the output goes

By default a widget is drawn to the console with the colour depth and glyphs the terminal supports. Redirected output, CI logs and `CONSOLEMONSTER_MODE=plain` get plain lines instead, from the same widget. To send output somewhere else on purpose:

- `Export-ConsoleFrame` writes a widget to an HTML page, an SVG image, Markdown, text or PNG, with the format taken from the file extension. In C#, `ConsoleExporter.Save` (or `widget.Export(path)` in fluent code) writes HTML, SVG or text the same way.
- `Use-ConsoleTarget` runs a script block with all ConsoleMonster output going to a file or returned as a string; `Terminal.Use(target)` does the same in C#.

<div class="cm-lang" data-lang="powershell">

```powershell
$report | Export-ConsoleFrame -Path ./service-check.html
$text = Use-ConsoleTarget -Width 80 { $report | Show-Console }
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
ConsoleExporter.Save(report, "service-check.html");
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
report.Export("service-check.html");
```

</div>

[Your first report](/guides/first-report/) builds a complete report step by step and exports it.

## Next

- [Three ways to write C#](/docs/csharp-styles/): initializers, fluent builders and how they map to PowerShell.
- [PowerShell conventions](/docs/powershell-conventions/): parameter names, pipeline input and live updates across the module.
- [Layout](/docs/layout/): sizing, alignment and padding.
- [Your first report](/guides/first-report/): a script from data to an exported report.
