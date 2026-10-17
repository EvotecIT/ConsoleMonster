### 2.0.0 - prototype (unreleased)
#### What's new
- Rebuilt as a zero-dependency .NET library (netstandard2.0, net472, net8.0) plus a thin binary PowerShell module for Windows PowerShell 5.1 and PowerShell 7.
- Terminal capability detection with `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_MODE` and `NO_COLOR` overrides; Unicode-safe output through `WriteConsoleW`.
- Spectre-compatible markup, semantic themes (dark, light, high contrast) and Full/Basic/ASCII glyph tiers.
- Widgets: text, rule, panel, table, tree, key/value, status list, spinner, progress, lanes, phases, bar and stacked bar, sparkline, histogram, braille line/scatter chart, log region, logo and dashboard; layouts Rows, Columns, Grid, Padding, Align and Stack.
- Live sessions: thread-safe updates coalesced at about 15 frames per second, console and PowerShell stream capture, Ctrl+C restore, JSON-lines record and replay (the replayed clock follows the recorded timeline), optional `[mm:ss]` prefixes on stream-mode lines (on by default in CI).
- Bars come in `Line` (progress and lanes default) and `Block` (bar and stacked chart default) shapes.
- Table width negotiation: column priority, minimum and maximum widths, overflow (wrap with a line limit, truncate, middle truncation, never), hidden-column footer, stable widths in live tables; record cards below a configurable width and a record view for single records.
- Groups with subtotals, totals (sum, count, average, minimum, maximum) and "Showing N of M" top-N.
- Cell presenters shared by tables, cards, key/value lists and trees: status, number, bytes, duration, date (absolute, relative, both; time zone), boolean, in-cell data bar, sparkline and progress, OSC 8 links; tone rules by threshold; readable without colour.
- Object inspector for .NET objects, PSObjects, dictionaries and `JsonElement` (net8.0) with depth and item limits, cycle detection, include/exclude and ordering; reflection isolated in one member source.
- `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree`: Format-Table style `-Property` (calculated properties, column option hashtables), DefaultDisplayPropertySet, `-GroupBy`, `-Total`, `-Top`, `-Width`, `-PassThru`.
- Live sessions handle window resizes (full redraw, coalesced while dragging), viewport scrolling, font changes (conhost font polling, width re-probe after Windows Terminal resizes) and windows that are too small (reduced layout by panel priority, then a one-line status).
- Light/dark background detection in conhost and `CONSOLEMONSTER_THEME=light|dark|auto`.
- Wrapping breaks after spaces, then after separators (path, list and query separators before dots, dashes and colons), then by character; `LogRegion.Wrap` wraps long log entries.
- Explicit culture for numbers and dates: `CONSOLEMONSTER_CULTURE`, `TerminalProfile.WithCulture`, `LiveSessionOptions.Culture` and `-Culture` on the Format cmdlets (default: the current culture).
- Outside a real local console (jobs, remoting, SSH, the ISE, redirected output, transcripts) cmdlets write through the PowerShell host like Write-Host, so jobs complete, transcripts record and `6>&1` captures the output.
- Host output keeps colour: coloured host UI writes on a real console with a transcript and in the ISE (plain lines in the transcript), coloured Write-Host-style records in jobs and remoting; redirected output stays plain.
- Rich text: multicolour spans (ConsoleColors exactly as Write-Host shows them, theme roles, hex/RGB, decorations, links, gradients) accepted in every widget, safe by default (data stays literal), with `RichText`/`RichTextBuilder` in C# and `New-ConsoleText` in PowerShell.
- `Write-ConsoleText` with Write-Color parameter parity plus markup, styles, gradients, padding, wrapping, split lines and Text/CSV/JSON-lines logs; `Write-ConsoleStatus` for an updating status line.
- Print-once output reads the window size on every write.
- Sentinel dates show as "never" and unbounded durations or non-finite numbers as a dash, instead of throwing; charts skip non-finite values.
- Stream and plain output sanitise escape sequences and control characters.
- Dashboards report unknown widget names, failed updates, failed frames and early ends through the host; `LiveSession.GetCounters()`; a dashboard that cannot start leaves the screen untouched.
- The object inspector has a node budget (`MaxNodes`, `-MaxNodes`), skips blocking getters and lets Ctrl+C through.
- Input core: Windows console keys through `ReadConsoleInputW` (Ctrl/Alt combinations, Shift+Tab, function keys, AltGr, pastes, resizes, focus), VT input decoding, scripted input for tests and demos, and one keyboard owner shared by prompts and live sessions.
- Prompts: confirm (with typed confirmation), text, secret, number, selection, multi-selection and path, with fuzzy filtering, groups, hotkeys, badges, previews, inline validation, typed results, line-based and accessible fallbacks, and defaults or a clear error when nobody can answer.
- Forms and wizards: pages with a progress strip, conditional and dependent fields with async loading, a review page with the equivalent command line, named presets and opt-in remembered choices.
- Menus and the app shell: Shell, Classic and Inline layouts, breadcrumb navigation, a command palette with providers, a key map and footer generated from the active keys, toasts, prompts over the app, and runs into a live dashboard that return to a results summary.
- Menu actions: console output, Write-Host, warnings and errors are collected and shown as a toast or an output dialog instead of drawing over the app; async actions ask through the menu's thread; a blocked Run shows its problem count and opens the item that fixes it; leaving a form with unsaved changes asks to discard them.
- Export: widgets, rendered frames and live-session snapshots to self-contained HTML (light and dark), SVG (box, block and braille drawn as shapes) and text; recordings to an HTML player, an animated SVG and asciicast v3 (ConsoleExporter, Recording, Export-ConsoleFrame, Export-ConsoleRecording, Format-ConsoleTable -AsWidget); ScreenCapture draws interactive screens into memory.
- Inline live area (`LiveSessionOptions.Mode = Inline`, `Start-ConsoleDashboard -Inline`/`-Transient`): a live region below the cursor with output scrolling above it; stream lines in CI and redirected output.
- `Write-Progress` from dashboard runspaces shows as nested activities (`ActivityList`); `Use-ConsoleProgress` renders a script's `Write-Progress` inline.
- Public test helpers: `TestTerminal`, `GoldenFile` (`CONSOLEMONSTER_UPDATE_GOLDEN=1`, readable diffs), `ScreenCapture` and `Assert-ConsoleOutput`.
- Long tables print in blocks (`Terminal.StreamingRowThreshold`, `Terminal.RenderChunks`): 50,000 rows use about 76 MB instead of 510 MB.
- SVG exports batch shapes into one path per colour; `ExportOptions.Elapsed` sets the clock of widget exports; `Format-ConsoleTable -AsWidget` keeps `-Culture` (`CultureScope`).
- A Windows CI workflow runs xUnit (net472, net8.0) and Pester (5.1, 7).
- 60 cmdlets with a plain `Console` noun (`Write-ConsoleTable`, `Format-ConsoleTable`, `New-ConsoleDashboard`, `Start-ConsoleDashboard`, `Read-ConsoleSelection`, `Show-ConsoleMenu`, `Read-ConsoleForm`, ...).

#### Breaking changes
- The Spectre.Console / Terminal.Gui functions of 0.0.x (`New-SpectreImage`, `New-SpectreRule`, `New-Terminal*`) are removed; 0.0.4 remains on the PowerShell Gallery.

### 0.0.4 - 2024.10.26
#### What's new
- Bump dependencies
- Small fixes

#### What's Changed
* Bump SixLabors.ImageSharp from 2.1.3 to 2.1.7 in /Sources/ConsoleMonster by @dependabot in https://github.com/EvotecIT/ConsoleMonster/pull/2
* Bump SixLabors.ImageSharp from 2.1.7 to 2.1.8 in /Sources/ConsoleMonster by @dependabot in https://github.com/EvotecIT/ConsoleMonster/pull/3

#### New Contributors
* @dependabot made their first contribution in https://github.com/EvotecIT/ConsoleMonster/pull/2

**Full Changelog**: https://github.com/EvotecIT/ConsoleMonster/compare/v0.0.3...v0.0.4
### 0.0.3 - 2023.01.01
- Removed Libraries from GIT, you will have to build them using PSPublishModule in the builds folder if you want to play with GIT version
- Updated versions of libraries:
  - Spectre.Console 0.45.0
  - Terminal.Gui 1.8.2

### 0.0.2 - 2022.09.06
- Improve `New-SpectreImage` to take URL as input
- Small improvements for other cmdlets but really WIP

### 0.0.1 - 2022.08.28
- Initial release to test things out
