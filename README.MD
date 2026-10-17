<p align="center">
  <a href="https://www.powershellgallery.com/packages/ConsoleMonster"><img src="https://img.shields.io/powershellgallery/v/ConsoleMonster.svg?style=flat-square"></a>
  <a href="https://www.nuget.org/packages/ConsoleMonster"><img src="https://img.shields.io/nuget/v/ConsoleMonster.svg?style=flat-square"></a>
  <a href="https://github.com/EvotecIT/ConsoleMonster"><img src="https://img.shields.io/github/license/EvotecIT/ConsoleMonster.svg?style=flat-square"></a>
</p>

<p align="center">
  <a href="https://twitter.com/PrzemyslawKlys"><img src="https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=flat-square&logo=twitter"></a>
  <a href="https://evotec.xyz/hub"><img src="https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg?style=flat-square"></a>
  <a href="https://www.linkedin.com/in/pklys"><img src="https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn&style=flat-square"></a>
</p>

# ConsoleMonster

ConsoleMonster is a zero-dependency console rendering library for .NET (netstandard2.0, net472, net8.0) and a
PowerShell module for Windows PowerShell 5.1 and PowerShell 7. It prints tables, trees, panels, key/value lists and
charts once, and runs live dashboards that many threads can update at the same time.

Version 2 replaces the Spectre.Console / Terminal.Gui based 0.0.x module (still available on the PowerShell Gallery).

## Layers

Each layer can be used without the ones above it.

| Layer | What it does |
|---|---|
| L0 Terminal | Capability detection (VT, colour depth, glyph tier from the console font, size, redirection, CI) with `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_MODE`, `CONSOLEMONSTER_THEME`, `CONSOLEMONSTER_CULTURE` and `NO_COLOR` overrides; light/dark background detection; UTF-16 output through `WriteConsoleW` on Windows consoles, UTF-8 when redirected; Ctrl+C restore. |
| L1 Text and style | Display-width measurement (Unicode 17), Spectre-compatible markup (`[bold red]text[/]`, `[pass]`), semantic themes mapped to 24-bit, 256 and 16 colours, glyph sets (Full, Basic, ASCII). |
| L2 Canvas | Cell buffer, braille and half-block pixel canvases, frame diff. |
| L3 Layout and widgets | `IWidget` / `IContainer`; Rows, Columns, Grid, Panel, Padding, Align, Stack; rich text, text, rule, table (width negotiation, cards, groups, totals), record view, object inspector, tree, key/value, status list, spinner, progress, lanes, phases, bar and stacked bar, sparkline, histogram, line/scatter chart, log region, logo, dashboard; cell presenters shared by tables, cards, key/value lists and trees. |
| L4 Live | `LiveSession`: lock-free update queue, ~15 fps coalesced diff rendering, resize, font and too-small handling, typed handles and names, ambient `TerminalScope`, Console.Out/Error capture into the log region, JSON-lines record and replay. |
| L5 Surfaces | The C# object model and a fluent builder; 47 thin PowerShell cmdlets with a plain `Console` noun. |

Every widget renders in every mode: a live dashboard becomes append-only lines when output is redirected or in CI,
and plain ASCII lines without colour with `CONSOLEMONSTER_MODE=plain`. Status is never shown by colour alone.
Text that reaches stream and plain output is sanitised: escape sequences (CSI, OSC) and control characters in log
messages, captured lines or data are removed, so Plain mode never emits an escape sequence.

## PowerShell

```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Write-ConsoleTable -Title 'Services' -StatusProperty Status

$dashboard = New-ConsoleDashboard -Title 'Patch' -Gap 1 {
    New-ConsolePanel -Title 'Servers' { New-ConsoleLanes }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}
'SRV1', 'SRV2', 'SRV3' | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 3 {
    param($Server)
    Update-ConsoleLane -Key $Server -Status Running -Total 5
    1..5 | ForEach-Object { Start-Sleep -Milliseconds 300; Update-ConsoleLane -Key $Server -Result Pass }
    Write-Host "$Server patched"   # lands in the log region, never on top of the dashboard
    Update-ConsoleLane -Key $Server -Complete
}
```

Where cmdlet output goes:

- On a real local interactive console, output is drawn directly, with colour and live dashboards.
- On a real console while `Start-Transcript` runs, and in the ISE: coloured text through the host UI
  (`Host.UI.Write` with console colours). The transcript receives the same lines as plain text.
- In background jobs and remoting: `Write-Host`-style information records (tag `PSHOST`) with foreground and
  background colours. Windows PowerShell 5.1 shows them in colour after `Receive-Job`. PowerShell 7 keeps the colours
  in the records but prints received job output without colour, as it does for `Write-Host -ForegroundColor` in a job.
- Redirected output, thread jobs and other hosts get one plain record per line. So files, transcripts and `6>&1` get
  whole lines of plain text.
- Colours map to the 16 console colours by role: pass green, fail red, warn yellow, muted dark grey, accent cyan.
  The light theme uses the darker variants.
- `NO_COLOR`, `CONSOLEMONSTER_COLOR=none` and plain mode keep host output uncoloured.
- As with `Write-Host`, `$x = Write-ConsoleTable ...` captures nothing.
- A dashboard in those hosts narrates as append-only lines.
Dashboard problems are reported once the screen is restored:

- An update for a widget name the dashboard doesn't have warns once per name, listing the valid names.
- Failed updates, frames that couldn't be laid out, and a session that ended because drawing failed are written as
  warnings and errors.
- `LiveSession.GetCounters()` returns the counters, for example `$ConsoleSession.GetCounters()` inside the dashboard script.
- A dashboard that can't start fails with a clear error before it touches the screen. Examples are an unwritable
  `-RecordPath` or another dashboard already running.

## C#

```csharp
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Dashboard dashboard = new DashboardBuilder("Deploy")
    .Panel("Servers", new Lanes(), out Lanes lanes, caption: "4 workers")
    .Panel("Log", new LogRegion(), out LogRegion log, fill: true)
    .BuildDashboard();

using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => {
        LanesHandle servers = lanes.Handle(s);           // thread-safe from any thread
        Parallel.ForEach(new[] { "SRV1", "SRV2", "SRV3" }, server => {
            servers.Start(server);
            servers.Advance(server, StatusKind.Pass, "copying files");
            Console.WriteLine(server + " done");          // captured into the log region
            servers.Complete(server);
        });
    });
}
```

## Tables and complex data

Tables negotiate their width instead of overflowing it:

- Each column can set a `Priority`, `MinWidth`/`MaxWidth` and an `Overflow`: `Wrap` (with `MaxLines`), `Truncate`,
  `TruncateMiddle` (paths, DNS names) or `Never`.
- Wrapped text breaks after spaces first, then after separators, and only then by character. Path, list and query
  separators (`\ / ; , | & ?`) win over the dots, dashes and colons inside a name (`. - : =`) when the next segment fits
  on one line, so a line ends with its separator: `evotec-xyz.mail.protection.` / `outlook.com`,
  `Artefacts\Packed\` / `ConsoleMonster.2.0.0.nupkg`. Tables, cards, record views, key/value lists, text blocks and
  log regions (with `LogRegion.Wrap`) all use the same rules.
- The header row is separated from the data by one rule row (`├──┼──┤`), drawn by the font at mid-height. There is
  no padding row. `HeaderSeparator = false` (`-NoHeaderSeparator`) underlines the header instead.
- When the columns don't fit, the lowest-priority columns hide first and a footer names them:
  `+2 columns (TTL, Cache file)`. The key column never hides.
- Column widths only grow in a live table, so values don't jump between frames.
- Below `CardsBelow` (60 cells by default), when the minimum widths still don't fit, rows turn into record cards with
  the key value as the card title.
- A single record renders as a record view: sections, groups, empty fields hidden and a summary header line.
- Groups (`AddGroup`) can show subtotals; `ShowTotals` adds a totals row (Sum, Count, Average, Min, Max per column).
  `MaxRows` with `TotalRowCount` prints "Showing 50 of 1,234".

Cell presenters turn values into text and are shared by tables, cards, key/value lists and trees:

| Presenter | Shows |
|---|---|
| Status | the status glyph and label (`√ PASS`, `▲ WARN`, `✗ FAIL`) |
| Number | right-aligned, with culture separators and an optional unit |
| Bytes | `1,41 MB` |
| Duration | `48 s`, `1 m 04 s` |
| Date | absolute, relative (`12 m ago`) or both, in a chosen time zone |
| Boolean | `√ yes` / `— no`, either value can be marked as the problem |
| Bar, Sparkline, Progress | a data bar, sparkline or progress bar inside the cell |
| Link | an OSC 8 hyperlink in Windows Terminal and VS Code, plain text elsewhere |

Tone rules (`ToneRule.Above(1, ThemeToken.Warn)` and similar) colour a value by threshold. Without colour (16-colour
fallbacks, `NO_COLOR`, redirected output and `CONSOLEMONSTER_MODE=plain`), a status tone adds its symbol, so the
output stays readable.

```csharp
Table table = new Table { Title = "Artifacts", ShowTotals = true };
table.AddColumn(new TableColumn("Artifact") { IsKey = true, Overflow = ColumnOverflow.Never });
table.AddColumn(new TableColumn("Size") { Priority = 9, Presenter = CellPresenters.Bytes, Aggregate = ColumnAggregate.Sum });
table.AddColumn(new TableColumn("Built in") { Priority = 6, Presenter = CellPresenters.Duration, Aggregate = ColumnAggregate.Sum });
table.AddColumn(new TableColumn("Path") { Priority = 1, Overflow = ColumnOverflow.TruncateMiddle, MinWidth = 20 });
table.AddValues("ConsoleMonster.2.0.0.nupkg", 1_482_113L, TimeSpan.FromSeconds(48.2), @"C:\Artefacts\Packed\ConsoleMonster.2.0.0.nupkg");
Terminal.Write(table);
```

The object inspector (`InspectorView`, `ObjectInspector.Inspect`) shows .NET objects, PSObjects, dictionaries and JSON:

- Nested objects become indented sections and collections become nested tables.
- `InspectorOptions` sets the depth limit, the item limit (with "+N more"), include/exclude and ordering.
- `MaxNodes` (default 5,000; `Format-ConsoleTree -MaxNodes`) caps one inspection, so a wide object at a large depth
  ends with a "node limit reached" marker instead of millions of nodes.
- Cycles are detected and marked.
- Getters that would block or force work are not called. An incomplete `Task` or `ValueTask` and an uncreated
  `Lazy<T>` show their state instead. Excluded members are never read, so `Exclude` doubles as a skip list.
- Ctrl+C (and any `OperationCanceledException`) stops the inspection instead of becoming an error node.
- `JsonElement` is supported on net8.0, where System.Text.Json is in-box. On net472 and netstandard2.0, parse JSON
  into dictionaries first.
- Reflection is used only for objects that are not dictionaries, JSON or PSObjects, and lives in one member source.
  On net8.0 it is marked `RequiresUnreferencedCode`. For trimmed or AOT builds, set `UseReflection = false` and pass
  dictionaries or your own `IMemberSource`.

From PowerShell, `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree` take pipeline input like the
built-in Format cmdlets:

- `-Property` accepts names, wildcards and calculated properties, as Format-Table does. A hashtable with column options
  (`Priority`, `Key`, `Overflow`, `MaxLines`, `MinWidth`, `MaxWidth`, `Presenter`, `Aggregate`, `WarnAbove`, ...)
  configures that column.
- Without `-Property`, an object's DefaultDisplayPropertySet is used.
- `-GroupBy`, `-Total`, `-Top N`, `-Width` and `-Culture` work as their names say. `-PassThru` also writes the input
  objects to the pipeline.
- Redirected output is plain text.

```powershell
Get-ChildItem C:\Windows\Logs -Recurse -File | Format-ConsoleTable -GroupBy Extension -Total -Top 20 -Property @(
    @{ Name = 'Name'; Key = $true }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Priority = 9 }
    @{ Name = 'LastWriteTime'; Header = 'Changed'; Presenter = 'DateRelative'; Priority = 5 }
    @{ Name = 'FullName'; Header = 'Path'; Overflow = 'TruncateMiddle'; Priority = 1 }
)
Get-Item C:\Windows\explorer.exe | Format-ConsoleTree -AsInspector -Depth 2
```

`Examples/Example-Tables.ps1 -Width 80` and `ConsoleMonster.Examples tables 80` show DnsClientX, DomainDetective,
TestimoX and PSPublishModule style data at any width.

## Rich text

Rich text is multicolour text that works anywhere text appears:

- table cells and headers, cards, key/value lists and record views;
- tree labels, panel and rule titles, the dashboard header and footer, the logo tagline;
- lane names and lane detail, status list items, log lines, and chart labels and legends.

It's built from spans, and each span has:

- a foreground and a background: a ConsoleColor, a theme role (`Pass`, `Fail`, `Warn`, `Info`, `Muted`, `Accent`),
  `#RRGGBB`, `rgb(r,g,b)` or `r,g,b`;
- decorations: bold, dim, italic, underline, strikethrough, invert;
- an optional link.

How it renders:

- Colours are down-mapped per tier (24-bit, 256, 16, none), and theme roles follow the light or dark theme.
- ConsoleColor names render exactly as PowerShell shows them. PowerShell's `DarkRed` is ANSI red and `Red` is bright
  red, so a script looks the same as with `Write-Host -ForegroundColor`.
- Wrapping, truncation (also in the middle), padding and alignment keep the styles.
- Without colour (`NO_COLOR`, CI, redirected output) the text and status symbols stay readable.

It's safe by default. Plain strings from data are always literal: no markup interpretation, and control characters
are removed. Rich text is opt-in:

- a `RichText` object, including one returned by a calculated property;
- the column option `Markup = $true`;
- a presenter.

```powershell
Get-Service | Select-Object -First 5 | Format-ConsoleTable -Property Name, @{
    Name = 'Status'; Expression = {
        if ($_.Status -eq 'Running') { New-ConsoleText '√ ', 'Running' -Role Pass } else { New-ConsoleText '× ', $_.Status -Role Fail -Style Bold }
    }
}
New-ConsolePanel -Title (New-ConsoleText 'SRV', '01' -Color Cyan, Yellow) { New-ConsoleText 'body' } | Write-ConsoleWidget
```

```csharp
RichText status = new RichTextBuilder().Append("√ ", ThemeToken.Pass).Append("PASS", ThemeToken.Pass, TextDecoration.Bold).Build();
table.AddValues(new RichTextBuilder().Append("SRV", ConsoleColor.Cyan).Append("01", ConsoleColor.Yellow).Build(), status);
lanes.Add("srv01", RichText.Gradient("SRV01", RichColor.FromRgb(0, 198, 255), RichColor.FromRgb(255, 0, 170)));
```

`Write-ConsoleText` writes multicolour lines and optionally a log. `Write-ConsoleStatus` shows one line that
updates in place:

- It can show a spinner, a clock and the elapsed time.
- It stops refreshing when anything else writes.
- It ends with `-Complete`.
- Outside a real console it writes plain lines.

### Coming from Write-Color

`Write-ConsoleText` takes the parameters and aliases of `Write-Color` from
[PSWriteColor](https://github.com/EvotecIT/PSWriteColor), so most scripts migrate with a rename. There is deliberately
no `Write-Color` or `wc` alias.

| Write-Color | Write-ConsoleText | Notes |
|---|---|---|
| `-Text` (`-T`) | `-Text` (`-T`) | Pieces are literal. Rich text from `New-ConsoleText` is accepted too. |
| `-Color` (`-C`, `-ForegroundColor`, `-FGC`) | same | Also theme roles, `#RRGGBB`, `rgb()`, `r,g,b`. Extra pieces take the first colour, as in Write-Color. Without `-Color`, the terminal's own colour is used; Write-Color uses White. |
| `-BackGroundColor` (`-B`, `-BGC`) | same | Counts don't have to match: extra pieces take the first background. |
| `-StartTab` (`-Indent`) | same | Eight cells per tab. |
| `-StartSpaces`, `-LinesBefore`, `-LinesAfter` | same | |
| `-ShowTime`, `-DateTimeFormat` (`-DateFormat`, `-TimeFormat`) | same | |
| `-NoNewLine` | same | Works in transcripts, jobs and the host route too. |
| `-HorizontalCenter` | same | Measured in terminal cells, so emoji and CJK centre correctly. |
| `-LogFile` (`-L`), `-LogTime` (`-LogTimeStamp`), `-LogRetry` | same | Appends under a file lock, with retries and warnings as in Write-Color. |
| `-Encoding` | same | Defaults to UTF-8 without BOM; Write-Color defaults to UTF-16 "Unicode". Also takes `utf8BOM` and `utf8NoBOM`. |
| `-NoConsoleOutput` (`-HideConsole`) | same | |
| | `-Markup`, `-Style`, `-Link`, `-Role`, `-Gradient` | Markup after the pieces, decorations, hyperlinks, theme roles, colour gradients. |
| | `-PadLeft`, `-PadRight`, `-PadCenter`, `-PadCharacter` | Padding in terminal cells (PSWriteColor #12). |
| | `-Wrap`, `-Width` | Wraps with a hanging indent under the time and prefix. |
| | `-SplitLines` | Splits at a separator and aligns the following lines under the first (PSWriteColor #7). |
| | `-LogFormat Text\|Csv\|JsonLines`, `-LogSeparator`, `-Level`, `-Category` | Structured logs (PSWriteColor #11). |

Behaviour that differs because of the host routes:

- In transcripts, each call is one line (PSWriteColor #8–#10).
- In jobs and remoting, the colours travel with the output.
- Redirected output is plain text.

For an updating line (PSWriteColor #15), use `Write-ConsoleStatus 'Copying ', $file -Color Gray, Cyan -Spinner
-ShowElapsed` and end it with `-Complete`.
## Culture

Numbers and dates follow the current culture by default, as PowerShell's own formatting does, so on a Polish system a
table shows `71,5 %`, `1,41 MB` and `Showing 5 of 1 234` next to English labels. To fix the culture:

- `CONSOLEMONSTER_CULTURE=en-US` (any culture name, `invariant`, or `current`). An unknown name is ignored and noted in
  the profile's decisions.
- `TerminalProfile.WithCulture(culture)` for `Terminal.Write`, `LiveSessionOptions.Culture` for a live session, and
  `PresentationOptions.Culture` for one table, record view, key/value list or tree.
- `-Culture en-US` on `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree`.

The most specific setting wins: the widget's `PresentationOptions.Culture`, then `-Culture` or the session option, then
the profile (`CONSOLEMONSTER_CULTURE`), then the current culture. Labels stay English. Live dashboard counters (lanes,
rates, clocks) and the inspector's item counts use invariant digits regardless, because they are compact status text
rather than data.

## Window size, fonts and themes

- Live dashboards follow the window: dragging, maximize/restore, snapping, Windows Terminal pane splits and font zoom.
  A size change clears the screen and redraws it fully. During a drag, redraws are coalesced to at most one every
  100 ms, and the final size always gets a frame.
- Narrower panels drop optional parts first: lane counts, then percentages. Table columns hide by priority, and
  tables turn into cards.
- A widget declares its minimum size (`ICompactWidget.MinimumSize`, or `Prioritized` around any widget).
- When the window is smaller than a dashboard's `MinimumSize`, the dashboard keeps its highest-priority panels. When
  even those don't fit, it shows one status line, for example
  `TestimoX · 218/332 · ✗12 ▲25 · 00:24 · window too small: 50×12, needs 60×20`.
- A widget that fails at an unusual size is replaced by a one-line note for that frame. It never ends the session.
- In conhost, layout uses the visible window, not the screen buffer. Nothing is written past the window or into its
  bottom-right cell. This was verified with "Wrap text output on resize" both on and off.
- Scrolling during a live session: frames are drawn on the alternate screen. When the conhost viewport moves (the
  scroll bar or the mouse wheel), the next frame is redrawn completely at the new position.
- Font changes: in conhost, the console font is polled about once a second, and a new face or size re-derives the
  glyph tier and the emoji/ambiguous widths. In Windows Terminal, widths are measured again after every resize, which
  also covers Ctrl+= and Ctrl+0 zoom.
- Theme: `CONSOLEMONSTER_THEME=light|dark|auto` (default `auto`), or `TerminalProfile.WithThemePreference` in code. `auto` reads
  the console background colour in conhost. Windows Terminal doesn't expose its colour scheme to console programs
  (ConPTY reports the default dark colour table), and ConsoleMonster never reads console input to ask the terminal
  (OSC 11). So in Windows Terminal with a light scheme, `auto` picks the dark theme; set `CONSOLEMONSTER_THEME=light`.
  The light theme meets WCAG contrast on white, and the dark theme keeps titles readable on a light background.
- Print-once output (tables, trees and lists written with `Terminal.Write` or the Format cmdlets) has no trailing
  spaces or padding to the right edge. Each write reads the window size again (`Terminal.GetCurrentProfile`), so
  output printed after a resize uses the new width. Once printed, it is laid out for the width at that moment. A later resize can't
  re-lay it out: in conhost with "Wrap text output on resize" on, a narrower window re-wraps wide lines into
  fragments, and with the option off, wide lines are clipped behind a horizontal scroll bar. Use `-Width` (or a
  `TerminalProfile.Fixed` profile) when output must fit a known width.

## Edge values

- `DateTime.MinValue` and `DateTime.MaxValue` are common "never" values in directory data. They show as `never` in
  every time zone instead of throwing.
- `TimeSpan.MinValue`, `TimeSpan.MaxValue`, durations beyond the TimeSpan range, and NaN or infinite numbers show as a
  dash (`-` in the ASCII tier).
- Charts skip non-finite values, so a rate computed over zero elapsed time never prints `Infinity` or `NaN`.

## Repository

- `ConsoleMonster.psd1` / `ConsoleMonster.psm1` / `ConsoleMonster.Tests.ps1`: the module (built by `Build/Build-Module.ps1`).
- `Sources/ConsoleMonster`: the library; `Sources/ConsoleMonster.PowerShell`: the cmdlets; `Sources/ConsoleMonster.Tests`: xUnit;
  `Sources/ConsoleMonster.Examples`: showcases (`query`, `gallery`, `pipeline`, `testimox`, `record`, `stress`, `tables`, `theme`, `widths`); `Sources/ConsoleMonster.Benchmarks`.
- `Examples/*.ps1`: PowerShell showcases; `Tests/*.Tests.ps1`: Pester (Windows PowerShell 5.1 and PowerShell 7).
- `Build/Build-Module.ps1` (PSPublishModule), `Build/Build-Project.ps1` and `Build/project.build.json` (PowerForge, NuGet package).
