---
title: Coming from Spectre.Console
description: Spectre.Console concepts and their ConsoleMonster counterparts, with the markup dialect the two share and where it differs.
slug: spectre-console
card: three-fluent
order: 3
---

Spectre.Console is a widely used .NET library for rich console output, and its markup syntax is familiar to many .NET developers. ConsoleMonster is a separate library with its own parser and renderer. Its markup follows the same dialect, so a reader who knows one can read the other. This page maps the concepts from one to the other and notes where they behave differently.

{{< cm-frame id="markup" script="true" caption="`Write-ConsoleText -Markup` in PowerShell 7: palette names, hex and `rgb()` colours, theme roles as backgrounds, escaped brackets, an unknown tag and a link." >}}

## Markup

| Spectre.Console | ConsoleMonster | Notes |
|---|---|---|
| `[bold]`, `[dim]`, `[italic]`, `[underline]`, `[strikethrough]`, `[invert]` | the same | Combined the same way, as in `[bold red on blue]`. |
| `[/]`, `[[` and `]]` | the same | `[/]` closes the innermost tag. |
| Colour names, `[#ff8800]`, `[rgb(1,2,3)]`, `[color(208)]` | the same, plus `[208]` and `[default]` | Names are the 256 xterm names. A name that appears twice in the xterm list takes `_1`, `_2`, as in `[blue3_1]`. Colours are reduced to what the console can show. |
| `[link=https://example.com]text[/]`, `[link]…[/]` | the same | OSC 8 hyperlinks in terminals that support them, plain text elsewhere. Only absolute `http`, `https`, `mailto` and `ftp` targets link. |
| `:emoji:` shortcodes | not supported | Write the emoji character itself; it is measured as two cells. |
| An unknown style is an error | removed, its text kept | An unknown tag that is never closed stays literal, since it is most likely text such as `[x]`. `Markup.Strict = true` throws a `FormatException` instead, and `Markup.Validate` reports the first problem without throwing. |
| — | theme roles: `[pass]`, `[fail]`, `[warn]`, `[muted]`, `[accent]` and others | Roles follow the active theme and also work as backgrounds (`[white on fail]`). |

In markup, a colour name is the 256-colour palette name, so `[darkred]` is palette colour 52 and `[grey]` palette colour 8. ConsoleMonster's PowerShell cmdlets also take `-Color` and `-BackgroundColor` parameters, where a name keeps its Write-Host meaning. Either meaning can be asked for in either place: `console.<name>` is the ConsoleColor as Write-Host shows it (`[console.darkred]` in markup), and `palette.<name>` is the palette colour (`-Color palette.darkred`).

| Name | Markup `[name]` | `-Color name` |
|---|---|---|
| `red` / `blue` | bright red / bright blue (ANSI 9 / 12) | the same |
| `darkred` / `darkblue` | palette 52 / 18 | ANSI red / blue (1 / 4) |
| `grey`, `gray` | ANSI 8 | ANSI 7 (Write-Host Gray) |
| `darkgray` | ANSI 8 | ANSI 8 |

## Writing output

| Spectre.Console | ConsoleMonster | Notes |
|---|---|---|
| `AnsiConsole.Write(renderable)` | `Terminal.Write(widget)` | Widgets measure themselves for the console width. |
| `AnsiConsole.Markup(...)` / `MarkupLine(...)` | `Terminal.WriteMarkup(...)` / `Terminal.WriteMarkupLine(...)` | `WriteMarkup` does not end the line. |
| `Markup.Escape(value)` | `Markup.Escape(value)` or `value.EscapeMarkup()` | |
| `AnsiConsole.MarkupInterpolated($"...")` | `Markup.Interpolate($"...")` | Escapes every argument after its format specifier. |
| `AnsiConsole.Clear()` | `Terminal.Clear()` | |
| `AnsiConsole.WriteException(ex)` | `Terminal.Write(new ExceptionView(ex))` | |
| `AnsiConsole.Profile.Capabilities` | `CapabilityDetector` and `TerminalProfile` | Colour depth, glyph tier, links and interactivity; see [capability detection](/docs/capabilities/). |

```csharp
using ConsoleMonster;
using ConsoleMonster.Text;

string server = "dc01";
double elapsed = 1.25;
Terminal.WriteMarkupLine(Markup.Interpolate($"[pass]{server}[/] answered in [muted]{elapsed:N1}s[/]"));
```

## Widgets

| Spectre.Console | ConsoleMonster | Notes |
|---|---|---|
| `Table` | `Table` | Columns carry a priority, a minimum and maximum width and an overflow rule. Tables fit the console width, and below a set width rows become cards; see [tables](/tables/). |
| `Tree` | `Tree` | |
| `Panel` | `Panel` | |
| `Rule` | `Rule` | |
| `BarChart` / `BreakdownChart` | `BarChart` / `StackedBar` | ConsoleMonster also has `Sparkline`, `Histogram` and `LineChart`. |
| `Columns`, `Rows`, `Grid`, `Padder`, `Align` | `Columns`, `Rows`, `Grid`, `Stack`, `Align` | |
| `Layout` | `Dashboard` and `DashboardBuilder` | Named panels in a header, body and footer, sized to the window. |
| `FigletText` | `Logo` | Pixel lettering in a built-in font. |
| `Canvas` | `PixelCanvas`, `BrailleCanvas`, `HalfBlockCanvas`, `CharacterCanvas` | |
| `JsonText` (Spectre.Console.Json) | `CodeView` with `JsonHighlighter`, `Show-ConsoleJson` in PowerShell | |
| `Calendar` | — | No counterpart. |

The [widget gallery](/widgets/) shows each ConsoleMonster widget with its PowerShell and C# source. Its C# uses the fluent style (`new Table().WithTitle(...)`, `Table.For(items).Column(...)`); [Three ways to write it in C#](/docs/csharp-styles/) shows it next to object initializers and the `Ui` factories.

## Status, progress and live output

| Spectre.Console | ConsoleMonster | Notes |
|---|---|---|
| `AnsiConsole.Status().Start(...)` | `Terminal.Status(text, action)`, `Terminal.StatusAsync` | |
| `AnsiConsole.Progress().Start(...)` | `Lanes` and `ProgressBar`, usually in a `LiveSession` | Progress columns are a column model; see [live sessions](/docs/live-sessions/). |
| `AnsiConsole.Live(renderable).Start(action)` | `LiveSession` | Updates are events addressed to widgets by WidgetId and can come from any thread. |
| `AnsiConsole.Record()` / `ExportHtml()` | `ConsoleExporter.ToHtml(...)`, `LiveRecorder` | Exports a frame, or records a session's events for [replay and export](/docs/recording/). |

```csharp
using ConsoleMonster;
using ConsoleMonster.Text;

string[] controllers = { "dc01", "dc02", "dc03" };
Terminal.Status("Resolving…", ctx => {
    foreach (string dc in controllers) {
        ctx.Update(Markup.Interpolate($"Querying [accent]{dc}[/]"));
        Thread.Sleep(500);    // the work for this controller
    }
});
```

`Terminal.Status` draws a one-line spinner as an inline live area. Console output written meanwhile scrolls above it, and the line is removed when the work ends, also when it throws. Redirected output, CI logs and plain mode get each new status text once, as a plain line.

## Prompts

| Spectre.Console | ConsoleMonster |
|---|---|
| `TextPrompt<T>` | `TextPrompt`, `NumberPrompt`, `PathPrompt` |
| `SelectionPrompt<T>` | `SelectPrompt<T>` |
| `MultiSelectionPrompt<T>` | `MultiSelectPrompt<T>` |
| `ConfirmationPrompt` | `ConfirmPrompt`, `TypedConfirmPrompt` |

ConsoleMonster also has forms, menus and a grid; the [interactive page](/interactive/) shows them.

## Outside this mapping

- **Spectre.Console.Cli** parses command lines and maps them to commands. ConsoleMonster has no command-line parser and can be used next to one.
- **Testing.** Spectre.Console.Testing provides a `TestConsole`. ConsoleMonster renders to a fixed profile in memory: `TerminalProfile.Fixed` with `ConsoleExporter.ToText`, and `ScreenCapture` with scripted keys for prompts.

## Related

Every widget is in the [widget gallery](/widgets/) with its source. The fluent, object and `Ui` styles are compared in [three ways to write it in C#](/docs/csharp-styles/).
