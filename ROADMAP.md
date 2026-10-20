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
- Export to HTML, SVG, text and asciicast, with the HTML recording player as one native script and a data format that
  sites reuse; public test helpers (`TestTerminal`, `GoldenFile`, `ScreenCapture`,
  `Assert-ConsoleOutput`).
- `Write-ConsoleText` with the Write-Color parameters, log formats and correct log-file encoding handling.
- Targets (`ITerminalTarget`): console, recording, writer and null targets behind every write, live session, prompt
  and status helper, with a scoped ambient default and a documented pattern for libraries.
- Progress columns for lanes and progress bars: built-ins, custom columns, smoothed rate and remaining time,
  hide-completed, collapse-when-finished and transient modes, and column summaries in stream output.
- Built-in labels in English, Polish, German, Spanish, French and Italian, following the UI language, including the
  test-run view.
- Replaceable regions (`Slot`, `LiveSession.Region`, `New-ConsoleRegion`, `Set-ConsoleRegion`) and keyed live tables
  (`LiveTable`, `New-ConsoleLiveTable`, `Update-ConsoleTable`) with stable widths, a recent-rows window and sorting.
- Emoji and symbol fallback per glyph tier, extensible by apps, with opt-in `:shortcode:` emoji in markup and an
  `Emoji` cell presenter.
- `Use-ConsoleTarget` to send PowerShell output to a file, a writer, a target or a string.
- The terminal's progress indicator (OSC 9;4, the Windows Terminal tab and taskbar) driven by progress bars, lanes,
  dashboards, status lines, `Use-ConsoleProgress` and `Invoke-ConsolePester`, cleared on every exit path.
- `Show-Console` (the best view for any data, `ConsoleView` in C#) and opt-in default formatting
  (`Enable-ConsoleFormatting`, `Register-ConsoleView`) for files, processes, services and registered types.
- Rewinding runs: `Show-ConsoleReplay` (`ReplayPlayer` in C#), an in-terminal player for recordings with play/pause,
  speed, frame stepping, a timeline to scrub, jumps to markers, failures, warnings and log entries, and log search;
  opt-in automatic recording of the last runs (`LiveSessionOptions.AutoRecord`, `Start-ConsoleDashboard -AutoRecord`);
  region swaps recorded as pictures so they replay faithfully.
- Object comparison and the what-if pattern: `Compare-ConsoleObject` (`ObjectDiff`, `ChangeView`) matches objects by
  key and shows property-level changes with counts (summary, table, side by side, unified), and `Show-ConsoleChanges`
  (`ChangePreview`) previews changes and asks before applying them, with a typed confirmation for removals and
  `-WhatIf` support.
- Three equivalent C# styles on every widget: object initializers (the model), generated fluent `With...`/`Add...`
  methods with typed tables (`Table.For(items).Column(s => s.Name)`), and nested `using static ConsoleMonster.Ui;`
  factories. Decision: the fluent methods come from a build-time Roslyn source generator whose output is compiled
  into `ConsoleMonster.dll`. Nothing ships as an analyzer and nothing is committed as generated code, so consumers on
  every target framework and C# version get ordinary, reflection-free methods, and a coverage test keeps them complete.

## Next

The agreed order. Steps 1–4 shipped in 2.0.x (the everyday-path PR, followed by one PowerShell grammar for every widget and
the documentation and website round). Steps 5 and 6 are next; product trials in step 5 happen in the referenced
repositories.

1. **The everyday path.** Ordinary PowerShell output that is excellent without configuration: one value-shaping layer
   for every command, capturable output, labelled group headings, one status vocabulary with PowerShell's own words,
   one date format, short values that never break, `Enable-ConsoleFormatting` with views for the common commands, and
   Linux and macOS parity. (Done.)
2. **Live runs.** Generic widgets for running many items: an outcome grid (one cell per item, a shape per state), a
   summary bar, an activity list and lane options; the test-run view built from them; `Watch-Console` to re-run a
   script block as a live view; `ForEach-ConsoleItem -Parallel` with a live view and ordinary output objects
   (`ConsoleRun.ForEachAsync` in C#). (Done.)
3. **Better defaults.** `Enable-ConsoleFormatting` can also install the error view for `$ErrorView` and `Write-Progress`
   rendering, each opt-in; `Show-ConsoleHelp` renders PowerShell's own help objects. (Done.)
4. **.NET.** Hosting and logging packages (`ConsoleMonster.Hosting`, `ConsoleMonster.Extensions.Logging`) and a
   `dotnet new` template, outside the zero-dependency core; a decision paper on a command-line layer
   (`Docs/Proposals/Command-Line-Layer.md`, written as part of this step). (Done.)

   Shipped alongside steps 1–4: one PowerShell grammar for every widget (item commands such as `New-ConsoleTreeNode`,
   `New-ConsoleStatusItem` and `New-ConsoleLogEntry`, hierarchical objects into `New-ConsoleTree`, and
   `Add-ConsoleItem` for widgets made earlier; see `Docs/PowerShell-Conventions.md`), command help with real examples
   for every cmdlet, and website pages and demos for the everyday path, live runs, help and defaults, Linux and SSH, and
   the .NET packages.

Next:

5. **Trials in real products.** TestimoX.CLI and PSPublishModule adopt ConsoleMonster after the everyday path ships.
   The work happens in those repositories; gaps found there come back here as general extension points.
6. **One definition, every output.** The same view definition rendered to the terminal here and, through bridges in
   their own packages, to other outputs.

## Ownership

ConsoleMonster renders terminals. HTML belongs to HtmlForgeX, and Markdown, Word and Excel belong to OfficeIMO. Step 6
means bridges outside the core: ConsoleMonster never emits semantic HTML or Markdown itself. Its HTML and SVG frame
export stays, because it is a picture of the terminal, not a document.

## Decision pending: command-line parsing

Our own source-generated, AOT-safe command-line layer that integrates with ConsoleMonster:

- help rendered with the design system;
- when arguments are missing, open the matching interactive form, which already produces the equivalent command line;
- one definition drives parsing, help, completion and forms.

To weigh: the maintenance cost of owning a parser against the value of one definition for all four. Several product
executables use a command-line parser today. The decision paper written in step 4 lays out the options.

## Backlog

Grouped under the step they would join; the rest stays unscheduled.

- With live runs (step 2): watching a live run remotely, a local browser mirror built on the HTML exporter and
  `Watch-ConsoleSession` to attach from another terminal, including over SSH; per-log highlighting in log regions.
- With better defaults (step 3): collapsing long JSON arrays; one shared highlighting engine for tools that keep their
  own today (PSPublishModule could consume ConsoleMonster's).
- With .NET (step 4): an AOT-compiled `cmon` command-line tool.
- With one definition, every output (step 6): raster export through ChartForgeX as a bridge in a separate package (the
  cell grid of any widget, frame or recording rasterized to PNG and the other raster formats ChartForgeX supports).
- Unscheduled, terminal work:
  - images drawn into the cell grid through ChartForgeX, in the same separate package as raster export;
  - QR codes and barcodes in the terminal through CodeGlyphX;
  - interactive terminal charts through ChartForgeX (zoom and pan, a crosshair, charts linked to tables);
  - a documentation viewer for Markdown and examples in the terminal, with OfficeIMO reading the Markdown;
  - mouse support.

## Not planned

- A full windowing framework.
