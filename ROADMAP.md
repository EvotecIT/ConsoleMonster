# ConsoleMonster roadmap

This file holds decisions and the backlog. Granular engineering tasks (validation, review follow-ups, small fixes)
live in [TODO.md](TODO.md).

The aim: ConsoleMonster is complete enough that moving a product onto it never needs a ConsoleMonster change made for
that product. Gaps are closed with general extension points, not per-product shims.

## Done (2.0)

- A zero-dependency .NET library (netstandard2.0, net472, net8.0) and a thin binary PowerShell module for Windows
  PowerShell 5.1 and PowerShell 7, laid out library-first.
- Terminal detection (VT, colour depth, glyph tier, size, redirection, CI, light or dark background) with environment
  overrides, Unicode output through `WriteConsoleW`, Ctrl+C restore, and safe plain output for logs and CI.
- Markup with the 256-colour palette names, hex, `rgb()`, theme roles and explicit `console.` / `palette.` names; links
  as OSC 8 with a safe-scheme filter; graceful unknown tags; escape and interpolation helpers.
- Rich text everywhere text appears, with themes (dark, light, high contrast) and per-tier colour down-mapping.
- Widgets: tables with width negotiation, groups, totals and presenters; trees; panels; key/value lists; charts; logo;
  status lists; progress, lanes and phases; log regions; dashboards and layouts.
- Live sessions, full screen or inline below the cursor, with console capture, record and replay, resize handling and
  a `Write-Progress` bridge.
- `Terminal.Status`, screen and cursor helpers, and markup without a newline.
- Prompts, forms and wizards, menus and the app shell, a multi-line text area and an interactive grid.
- Exceptions and PowerShell errors, a dependency-free highlighting engine (`CodeView`, `Format-ConsoleCode`,
  `Format-ConsoleJson`), and Pester runs with a live dashboard.
- Export to HTML, SVG, text and asciicast; public test helpers (`TestTerminal`, `GoldenFile`, `ScreenCapture`,
  `Assert-ConsoleOutput`).
- `Write-ConsoleText` with the Write-Color parameters, log formats and correct log-file encoding handling.

## Next

### Round B

- An injectable console and output abstraction (profile, writer, theme and input in one object), accepted by
  `Terminal.Write`, prompts and live sessions, plus ambient redirection for a scope. It serves tests and redirection in
  code that writes globally today.
- Extensible progress columns: elapsed, remaining time and rate per row; a custom column delegate; a transient mode
  that hides finished rows.

### Round C

- Swapping live-region content: a slot whose child can be replaced from any thread.
- Row-level live table updates by key.
- Emoji and symbol fallback per glyph tier through the glyph set, so literal emoji degrade on consoles that can't show
  them.

## Decision pending: command-line parsing

Our own source-generated, AOT-safe command-line layer that integrates with ConsoleMonster:

- help rendered with the design system;
- when arguments are missing, open the matching interactive form, which already produces the equivalent command line;
- one definition drives parsing, help, completion and forms.

To weigh: the maintenance cost of owning a parser against the value of one definition for all four. Several product
executables use a command-line parser today.

## Backlog

- An object comparison view (`Compare-ConsoleObject`): side by side, unified diff, and property-level changes for
  objects and collections, with filters.
- Documentation viewers: rich rendering of help, Markdown and examples (`Show-Documentation`, `Get-Help` style).
- One shared highlighting engine for tools that keep their own today (PSPublishModule could consume ConsoleMonster's).
- Images through ChartForgeX.
- Mouse support.
- Per-log highlighting in log regions (today highlighting applies per line through `Write-ConsoleLog -Highlight`).
- Collapsing long JSON arrays.
- Logging provider and adapter packages, when a consumer needs them.

## Not planned

- A full windowing framework.
