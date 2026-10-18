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

No round is scheduled. The next work comes from the pending decision below or from the backlog.

## Decision pending: command-line parsing

Our own source-generated, AOT-safe command-line layer that integrates with ConsoleMonster:

- help rendered with the design system;
- when arguments are missing, open the matching interactive form, which already produces the equivalent command line;
- one definition drives parsing, help, completion and forms.

To weigh: the maintenance cost of owning a parser against the value of one definition for all four. Several product
executables use a command-line parser today.

## Backlog

- Documentation viewers: rich rendering of help, Markdown and examples (`Show-Documentation`, `Get-Help` style).
- One shared highlighting engine for tools that keep their own today (PSPublishModule could consume ConsoleMonster's).
- Images and raster export through ChartForgeX, as one bridge in a separate package so the core stays dependency-free:
  images in (pictures drawn into the cell grid) and rasters out (the cell grid of any widget, frame or recording
  rasterized to PNG and the other raster formats ChartForgeX supports).
- Mouse support.
- Per-log highlighting in log regions (today highlighting applies per line through `Write-ConsoleLog -Highlight`).
- Collapsing long JSON arrays.
- Logging provider and adapter packages, when a consumer needs them.
- QR codes and barcodes in the terminal through CodeGlyphX.
- Watching a live run remotely: a local browser mirror built on the HTML exporter, and `Watch-ConsoleSession` to attach
  from another terminal, including over SSH.
- One definition, every output: the same view rendered to the terminal, an HtmlForgeX report, an HtmlForgeX.Email
  message, and OfficeIMO Word and Excel documents.
- Interactive terminal charts through ChartForgeX: zoom and pan, a crosshair, and charts linked to tables.

## Not planned

- A full windowing framework.
