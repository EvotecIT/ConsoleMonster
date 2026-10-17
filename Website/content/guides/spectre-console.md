---
title: Coming from Spectre.Console
description: ConsoleMonster markup follows the Spectre.Console dialect, so markup moves over unchanged. Colour names, escaping, status and matching calls.
slug: spectre-console
order: 2
---

ConsoleMonster is not a wrapper around Spectre.Console; it has its own parser and renderer and no dependencies. Its markup follows the Spectre.Console dialect, so strings such as `[bold red]text[/]` move over unchanged.

{{< cm-frame id="markup" script="true" caption="Write-ConsoleMarkup in PowerShell 7: palette names, hex and rgb() colours, theme roles as backgrounds, escaped brackets, an unknown tag and a link." >}}

## The dialect

- **Styles.** `[bold]`, `[dim]`, `[italic]`, `[underline]`, `[strikethrough]` and `[invert]`, combined as `[bold red on blue]`. `[/]` closes the innermost tag; `[[` and `]]` are literal brackets.
- **Colours.** The 256 xterm names of Spectre markup (`[cadetblue]`, `[grey50]`, `[deepskyblue3]`; a name that appears twice in the xterm list takes `_1`, `_2`, as in `[blue3_1]`), `[#ff8800]`, `[rgb(1,2,3)]`, `[color(208)]` or `[208]`, and `[default]`. They are reduced per tier like any colour.
- **Theme roles.** `[pass]`, `[fail]`, `[warn]`, `[muted]`, `[accent]` and the other roles, also as backgrounds (`[white on fail]`).
- **Links.** `[link=https://example.com]text[/]` and `[link]https://example.com[/]` become OSC 8 hyperlinks in Windows Terminal and VS Code and plain text elsewhere. Only absolute `http`, `https`, `mailto` and `ftp` targets link.
- **Unknown tags.** A tag that isn't a known style is removed and its text kept when a `[/]` closes it. An unknown tag that is never closed stays literal, because it is most likely text such as `[x]`. `Markup.Strict = true` throws a `FormatException` instead, and `Markup.Validate` reports the first problem without throwing.

## One rule for colliding names

In markup, a colour name is the 256-colour palette name, so `[darkred]` is palette colour 52 and `[grey]` palette colour 8. In `-Color`, `-BackgroundColor` and `ConsoleColor` parameters a name keeps its Write-Host meaning. Either meaning can be asked for in either place: `console.<name>` is the ConsoleColor as Write-Host shows it (`[console.darkred]` in markup) and `palette.<name>` the palette colour (`-Color palette.darkred`).

| Name | Markup `[name]` | `-Color name` |
|---|---|---|
| `red` / `blue` | bright red / bright blue (ANSI 9 / 12) | the same |
| `darkred` / `darkblue` | palette 52 / 18 | ANSI red / blue (1 / 4) |
| `grey`, `gray` | ANSI 8 | ANSI 7 (Write-Host Gray) |
| `darkgray` | ANSI 8 | ANSI 8 |

## Calls that map

| Spectre.Console | ConsoleMonster |
|---|---|
| `AnsiConsole.Markup(...)` | `Terminal.WriteMarkup(...)`, which does not end the line |
| `AnsiConsole.MarkupLine(...)` | `Terminal.WriteMarkupLine(...)` |
| `Markup.Escape(value)` | `Markup.Escape(value)` or `value.EscapeMarkup()` |
| `AnsiConsole.MarkupInterpolated($"...")` | `Markup.Interpolate($"...")`, which escapes every argument after its format specifier |
| `AnsiConsole.Status().Start(...)` | `Terminal.Status("...", ctx => ...)` and `Terminal.StatusAsync` |
| `AnsiConsole.Clear()` | `Terminal.Clear()` |

```csharp
Terminal.WriteMarkupLine(Markup.Interpolate($"[pass]{server}[/] answered in [muted]{elapsed:N1}s[/]"));
Terminal.Status("Resolving…", ctx => {
    foreach (string dc in controllers) {
        ctx.Update(Markup.Interpolate($"Querying [accent]{dc}[/]"));
        Query(dc);
    }
});
```

`Terminal.Status` draws a one-line spinner as an inline live area: console output written meanwhile scrolls above it, and the line is removed when the work ends, even when it throws. Redirected output, CI logs and plain mode get each new status text once as a plain line.

Tables, trees, panels and charts are separate widgets rather than Spectre renderables; the [widget gallery](/widgets/) shows each with its C# source.