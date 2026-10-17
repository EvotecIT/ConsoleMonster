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
| L5 Surfaces | The C# object model and a fluent builder; 60 thin PowerShell cmdlets with a plain `Console` noun. |

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
- Very long tables print in blocks: above `Terminal.StreamingRowThreshold` rows (2,000) the column widths are still
  measured from every row, but rows are rendered and written 500 at a time. Printing 50,000 rows at 140 columns
  takes about 1.7 s, with peak memory about 76 MB above the starting point (510 MB before), and the output is
  identical to a single render.- The header row is separated from the data by one rule row (`├──┼──┤`), drawn by the font at mid-height. There is
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

## Prompts, menus and forms

Scripts and tools can ask questions, show menus and run wizards that look like the rest of ConsoleMonster.

- Every list filters as you type. Matches are highlighted, and groups, hotkeys, badges, descriptions and a preview
  pane are available.
- A footer always lists the keys that work right now; `?` (or F1 in prompts) shows them all.
- Answers come back as typed values or as the chosen objects themselves, not strings.
- Forms and reviews show the equivalent command line, so people learn how to automate what they just did.

### Prompts

Prompts draw below the cursor and leave a one-line answer in the scrollback:

```powershell
$environment = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging', 'Lab' -Default 'Staging' -ParameterName '-Environment'
$services = Get-Service | Read-ConsoleMultiSelection 'Services' -Display DisplayName -GroupBy Status -Minimum 1
$name = Read-ConsoleText 'Share name' -Pattern '[a-z][a-z0-9-]+' -PatternMessage 'Lower case letters, digits and dashes'
$port = Read-ConsoleNumber 'Port' -Integer -Minimum 1 -Maximum 65535 -Default 443
$token = Read-ConsoleSecret 'Token'
$file = Read-ConsolePath 'Import file' -Kind File -MustExist -Extension .csv
if (Read-ConsoleConfirm 'Remove FS01?' -TypedConfirmation 'FS01') { ... }
```

```csharp
string environment = Prompt.Select("Environment", new[] { "Production", "Staging", "Lab" });
IReadOnlyList<ToolTask> tasks = new MultiSelectPrompt<ToolTask>("Tasks", allTasks, t => t.Name, t => t.Category) { Minimum = 1, Preview = TaskPreview }.Show();
bool go = Prompt.Confirm("Continue?", true);
```

| Prompt | What it adds |
|---|---|
| Selection | Fuzzy filter, groups, hotkeys (`/` filters then), badges, descriptions, disabled choices with their reason, a preview pane beside or below the list. |
| Multi-selection | Tri-state group checkboxes (Space toggles a group; ←→ collapse it), Ctrl+A all, Ctrl+N none, Ctrl+T invert, Ctrl+O selected only, minimum and maximum, a live count. |
| Text | Default (Enter accepts it, Tab edits it), placeholder, required, length, pattern and scriptblock validation shown inline, history. |
| Secret | Masked or hidden, Ctrl+R reveals it; a SecureString in PowerShell unless `-AsPlainText`. |
| Number | Whole or decimal, minimum and maximum, Up and Down step. |
| Path | Folder entries listed while typing, Tab completes, must-exist and file/folder checks. |
| Confirm | Y/N answer at once, the default focused; `-TypedConfirmation` for destructive actions. |

Prompts asked while a live dashboard runs pause it and appear in front of it, and the dashboard resumes afterwards.
Prompts asked from a menu action appear in front of the app.

### When nobody can answer

| Situation | What prompts do |
|---|---|
| CI, redirected input, `pwsh -NonInteractive`, `CONSOLEMONSTER_NONINTERACTIVE=1` | Return the default, or fail: *Prompt 'Environment' needs a value in a non-interactive session; pass -Environment or set a default.* |
| Remoting, the ISE | Ask through the host: numbered choices and `PromptForChoice`. |
| `CONSOLEMONSTER_ACCESSIBLE=1` | Plain numbered questions with no redraws; every answer is confirmed as text. |

Esc and Ctrl+C end a prompt with a `PromptCancelled` error (a typed confirmation answers `$false` instead).

### Menus

A menu runs as an app (`Shell`), a centred menu (`Classic`) or a list in the scrollback (`Inline`):

```powershell
$menu = New-ConsoleMenu 'Backup helper' {
    New-ConsoleMenuItem 'Back up now' -Hotkey b -Action { if ($MenuContext.Confirm('Start now?', $true)) { Start-Backup } }
    New-ConsoleMenuItem 'Restore' -Hotkey r -Action { $backup = Read-ConsoleSelection 'Backup' -Choices (Get-Backup) -Display Name }
    New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings -OnSubmit { $MenuContext.Toast("Saved: $($Values.Keep) days") }
    New-ConsoleMenuItem -Exit
}
Show-ConsoleMenu $menu -Layout Classic
```

```csharp
Menu menu = new Menu("Server toolbox")
    .Item("Targets", i => i.Hotkey('t').LoadChoices(LoadServersAsync))
    .Item("Tasks", i => i.Hotkey('k').MultiSelect(tasks, t => t.Name, t => t.Category, TaskPreview))
    .Item("Options", i => i.Form(optionsForm))
    .Item("Presets", i => i.Presets())
    .Item("Review & run", i => i.Review(BuildReview))
    .Exit("Quit");
menu.Run(new MenuOptions { Layout = MenuLayout.Shell, AppName = "Server toolbox", RememberLastChoices = true });
```

- Items run actions (scriptblocks run on the pipeline thread) or open a submenu, a form, a selection, a page, a review
  (summary, equivalent command, Run, Copy command, Save preset) or the presets list. They can have hotkeys, icons,
  badges, state text and a reason when unavailable. Icons are semantic names that follow the glyph tier
  (`New-ConsoleMenuItem -IconName Targets`, `WithIcon(GlyphIcon.Targets)`: ● in modern fonts, o in ASCII), or free text.
- The Shell layout shows a breadcrumb title bar, a sidebar, the content pane and a key footer. Below 84 columns the
  sidebar folds into the breadcrumb.
- Keys: Esc goes back, Ctrl+Home goes home and Ctrl+Q leaves, asking first when a form has unsaved changes. `?` shows
  every key, and Ctrl+K (or `:`) opens the command palette with every item, preset and the app's own providers.
- `$MenuContext.Run($dashboard, { param($run) ... })` runs work with a live dashboard on the app's screen. The menu
  comes back with a results summary and next actions.
- Selections and form values survive navigation and can be saved as presets. With `-RememberLastChoices` they are kept
  between runs (`%APPDATA%\ConsoleMonster\<app>\`, or the XDG config folder on Linux and macOS).
- A form opened from a menu changes nothing until you press its Save button. Leaving it with changes asks
  "Discard the changes to Options?"; Discard puts back the values it had when it was opened or last saved, so unsaved
  edits never reach a review or a run.
- While an action runs, what it writes (`Write-Host`, warnings, errors, `Console.Out` and `Console.Error`) is collected
  instead of drawn over the app. One line comes back as a toast, more lines in an output dialog. `$MenuContext.Log()`
  adds a line yourself. Programs that write to the console handle directly are not captured.
- A review blocks Run while it has problems: the button reads "Run · 1 problem", the problems sit above the buttons,
  and Enter opens the item that fixes the first one (`review.Problem("Pick a target", targetsItem)` in C#).
- Async C# actions (`ActionAsync`) run on a worker. Their prompts, runs, toasts and navigation are passed to the menu's
  thread, which owns the keyboard, and the worker waits for the answer. Calls from any other thread throw a clear
  `InvalidOperationException` instead of racing the menu for the console.

### Forms and wizards

```powershell
$form = New-ConsoleForm 'Install agent' -Command 'Install-Agent' {
    New-ConsoleField Path 'Install folder' -Type Path -Kind Folder -Default 'C:\Program Files\Agent' -Page Location -Parameter Path
    New-ConsoleField Region 'Region' -Type Select -Choices 'eu', 'us' -Page Connection -Parameter Region
    New-ConsoleField Server 'Server' -Type Select -DependsOn Region -LoadChoices { param($Values) Get-AgentServer -Region $Values.Region } -Page Connection -Parameter Server
    New-ConsoleField Proxy 'Use a proxy' -Type Toggle -Page Connection -Parameter UseProxy
    New-ConsoleField ProxyAddress 'Proxy address' -VisibleWhen { $Values.Proxy } -Page Connection -Parameter Proxy
}
$answers = Read-ConsoleForm $form -AppName 'Agent setup' -RememberLastChoices
```

Fields with `-Page` make a wizard with a progress strip. `-VisibleWhen` shows a field only while a condition holds.
`-DependsOn` resets a field when another changes, and its options load again in their own runspace, with a spinner
that Esc cancels. The review page shows every answer and the equivalent command line, built from each field's
`-Parameter`: PowerShell, POSIX or Windows style, with defaults left out and secrets as a placeholder.

Every built-in word comes from `PromptLabels`, so the interactive layer can be translated.

One design system covers every surface, described in [Docs/Design-System.md](Docs/Design-System.md): the header bar, the key-hint footer, dialogs, the focus row, the status vocabulary and the formatting rules. A run started from a menu, or from a form through `FormOptions.AfterSubmit`, draws inside the same frame as the menu or form. Its header, sidebar and footer stay in place; only the content changes, and the screen is never cleared between steps.

## Inline live area and Write-Progress

A live session can draw inline instead of on the alternate screen: an area as tall as its content sits below the
cursor, output and log lines scroll above it, and the final state stays in the scrollback.

```powershell
Start-ConsoleDashboard $dashboard -Inline { ... }          # -Transient leaves nothing behind
Use-ConsoleProgress { foreach ($i in 1..50) { Write-Progress -Activity 'Copying' -PercentComplete ($i * 2) } }
```

```csharp
using (LiveSession session = new LiveSession(new Rows(progress, lanes), new LiveSessionOptions { Mode = LiveMode.Inline })) { ... }
```

- It works in Windows Terminal and conhost. It redraws only changed rows, clears the rows it leaves when it shrinks,
  and copes with a narrower window after a resize. Ctrl+C restores the screen. In CI and redirected output it falls
  back to the usual stream lines.
- Console output, and log lines when the area has no log region, are printed above the area while it runs.
- `Write-Progress` in a dashboard script shows as nested activities under the dashboard, with Activity, Status,
  PercentComplete, SecondsRemaining and Id/ParentId nesting. They come from an `ActivityList`, added automatically
  when the dashboard has none; an empty list takes no space. In stream mode they print a line at every 10%.
- `Use-ConsoleProgress` runs an existing script unchanged, with the caller's variables. Its own `Write-Progress` and
  `Write-Host` calls are drawn inline, and its output is returned at the end. Commands inside other modules keep their
  own `Write-Progress`, which PowerShell shows as usual.
## Pester test runs

`Invoke-ConsolePester` runs Pester 5 or 6 with a live dashboard. It takes the parameters of `Invoke-Pester` (or a
`-Configuration`), imports Pester the first time it is needed, and returns Pester's own result object with `-PassThru`.

```powershell
Invoke-ConsolePester .\Tests                                    # inline: files as lanes, the running test, failures
Invoke-ConsolePester -Configuration $config -FullScreen -ExportHtml .\pester.html -PassThru
```

- The header bar shows the totals and the clock, the files are lanes with passed, failed and skipped counts, and the
  running test is shown under them. On the full screen there are also the failures (expected against actual, with the
  differing characters underlined, and the file and line as a link), the slowest tests and a histogram of durations.
- When the run ends, a table of results by file, the failures and the totals go to the scrollback. `-ExportHtml` saves
  the final dashboard. In CI, jobs and redirected output there is one line per failure and a summary line.
- Write-Host, warnings, errors and other records the tests write appear above the inline area, and one-shot
  ConsoleMonster output (tables, panels, markup) is printed there too instead of over the frame.
- Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter.
  It checks for every internal it uses, and when one is missing (or with `Run.Parallel` or `-Output None`) Pester runs
  unchanged with its own output, followed by the summary table. Tested with Pester 5.7, 5.8, 6.0, 6.1 and 6.2 on
  Windows PowerShell 5.1 and PowerShell 7. [Docs/Pester-Upstream-Proposal.md](Docs/Pester-Upstream-Proposal.md) describes the public
  hook that would replace the adapter.
- The view is a plain widget (`TestRunView` over a `TestRun`), so other test frameworks can feed it from C#.
## Errors and exceptions

```powershell
try { Get-Item C:\missing -ErrorAction Stop } catch { $_ | Format-ConsoleError }
$Error[0] | Write-ConsoleException -ShowFrameworkFrames
```

`ExceptionView` (C#) and `Format-ConsoleError` / `Write-ConsoleException` show an exception or ErrorRecord with its
type and message, the PowerShell category, error id and position message, the source lines around the failing line
(highlighted, the line marked), the script stack, .NET frames as `Type.Method()` with paths relative to the current
folder and file:line links, framework frames collapsed into "+n framework frames", and inner exceptions as "Caused by".
Failed runs in menus and forms show their exception the same way, and Pester failures that are not assertions name
their exception type.

## Syntax highlighting

```powershell
Format-ConsoleCode -Path .\deploy.ps1 -LineNumbers -Highlight '12-13'
Get-Content .\agent.log -Tail 20 | Format-ConsoleCode -Language Log
Get-Process -Id $PID | Select-Object Name, Id | Format-ConsoleJson
$hosts | Format-ConsoleTable -Property Name, @{ Name = 'Address'; Highlight = 'Smart' }
Write-ConsoleLog 'GET https://example.com took 412ms' -Highlight Smart
```

```csharp
Terminal.Write(new CodeView(File.ReadAllText(path), Highlighters.ForPath(path)) { Title = "deploy.ps1", LineNumbers = true }.HighlightLines(12, 13));
Highlighters.Register(new RegexHighlighter("ini2").Rule(@"^\s*;.*$", HighlightRole.Comment).Words(HighlightRole.Keyword, "include"), "ini2");
```

- One engine without dependencies: `ITextHighlighter` turns literal text into tokens with roles (keyword, string,
  number, comment, key, type, variable, error, added, removed, timestamp, link and more) that map onto theme roles, so
  the colours follow the theme and the colour depth.
- Languages: JSON, XML/HTML, YAML, PowerShell, log lines and diffs, CSV/TSV (column colours) with hand-written lexers;
  C#, SQL, INI/TOML and Markdown with rule highlighters (`RegexHighlighter`, with a time limit) that you can extend.
- `Smart` highlights values in plain text: numbers, booleans and null, GUIDs, IPv4 and IPv6, URLs and e-mail addresses
  (as links), paths, dates and times, distinguished names and quoted strings.
- `-Language Auto` picks the language from the file extension, then from the content, and falls back to plain text.
- Text is literal: markup is not read and control characters are dropped. `CodeView` tokenizes only the lines it
  shows (`MaxLines`, default 2000), and long lines are truncated with an ellipsis or wrapped.

## Multi-line text

```powershell
$reason = Read-ConsoleText 'Change reason' -Multiline -MaxLines 10
New-ConsoleField Reason 'Change reason' -Type TextArea -MaxLines 10
```

`TextAreaPrompt` and `TextAreaField` take notes and change reasons: soft wrap, Enter for a new line, Ctrl+Enter (or
Ctrl+S) to save, paste with line breaks kept, a line and length limit, and validation with the inline error. In a form
the field shows the first line and opens the text in a dialog. In line mode the answer ends with an empty line.

## Interactive grid

```powershell
Get-Service | Show-ConsoleGrid -Property Name, Status, StartType -Title 'Services' | Restart-Service -WhatIf
```

`Grid` (C#) and `Show-ConsoleGrid` are an Out-GridView for the console on 5.1 and 7: typing filters, F3/Shift+F3 sort
by column and F4 reverses, Space and Ctrl+A mark rows, Enter shows a row with every column, F2 chooses columns, and
Ctrl+Enter returns the original objects (`-OutputMode Multiple`, `Single` or `None`). Columns use the table's width
negotiation, presenters and tone rules, and the screen has the same header bar, panel and footer as menus and forms.
## Export to HTML, SVG, text and asciicast

Any widget, rendered frame or live-session snapshot can be saved as a self-contained file:

```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path .\services.html
$dashboard | Export-ConsoleFrame -Path .\run.svg -Width 120 -Height 36 -Title 'Nightly run' -WindowChrome
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Destination .\run.svg      # animated, no script
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Destination .\run.html     # player with scrubbing
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Destination .\run.cast     # asciicast v3
```

```csharp
string html = ConsoleExporter.ToHtml(widget, new ExportOptions { Width = 120, Title = "Nightly run", WindowChrome = true });
string svg = ConsoleExporter.ToSvg(session);            // what a live session shows now
string text = ConsoleExporter.ToText(frame);
string player = Recording.ExportHtml(dashboard, File.OpenText("run.jsonl"));
```

- **HTML**: one page with inline CSS. The text can be selected and copies as lines. The page holds a dark and a light
  rendering and shows the one the viewer prefers (`prefers-color-scheme`); use `-Theme Dark` or `Light` for one.
  Glyphs a fallback font might draw at another width are boxed to their cell, so the grid never drifts. Block elements
  and braille are painted as cell backgrounds, so bars and charts have no seams.
- **SVG**: box drawing, block and braille characters are drawn as shapes, so borders join and charts line up in any
  font. Every other glyph is centred in its cell and stays selectable. There are no external assets.
- **Recordings** are played into the same dashboard definition at a fixed size and frame rate, and identical frames
  are merged; `FramesPerSecond = 2` roughly halves the size of an animated SVG. The animated SVG uses CSS keyframes only. The HTML player has play/pause, speed and a scrubber; add
  `#t=30000` to the address to open it at 30 seconds. Asciicast v3 files contain output events only.
- **Links**: OSC 8 links become links for http, https, mailto and ftp only. `javascript:` and every other scheme
  render as plain text, all text is escaped, and nothing is fetched from the network.
- **Fonts**: the font stack starts with Cascadia Mono, DejaVu Sans Mono and Menlo, which draw box drawing on the cell
  grid. Braille in plain text needs a font that has it; Consolas does not and falls back to a system font. The HTML
  and SVG exports paint braille themselves.
- **Interactive screens**: `InteractiveOptions.Capture = new ScreenCapture(120, 36)` with a scripted `Input` draws
  prompts, menus and forms into memory, ready for `ConsoleExporter`.

## Testing your output

The same helpers the library's tests use are public (`ConsoleMonster.Testing`):

```csharp
string text = TestTerminal.RenderText(widget, width: 80, glyphs: GlyphTier.Ascii, culture: CultureInfo.InvariantCulture);
GoldenFile.Match("Golden/report.txt", text);      // first run writes the file; CONSOLEMONSTER_UPDATE_GOLDEN=1 rewrites
ScreenCapture capture = new ScreenCapture(80, 20);
bool ok = new ConfirmPrompt("Deploy?").Show(TestTerminal.Interactive("y", capture));
```

```powershell
It 'renders the report' {
    $rows | Format-ConsoleTable -AsWidget | Assert-ConsoleOutput -GoldenPath "$PSScriptRoot\Golden\report.txt" -Width 80
}
```

- Rendering uses a fixed size, glyph tier and culture (invariant by default), so results don't depend on the machine.
- A mismatch fails with the first differing line, the expected and actual versions, and the column where they part.
- To get a `Should` operator, register one in `BeforeAll`:
  `Add-ShouldOperator -Name MatchConsoleGolden -Test { param($ActualValue, $Path) try { $ActualValue | Assert-ConsoleOutput -GoldenPath $Path; [pscustomobject] @{ Succeeded = $true; FailureMessage = '' } } catch { [pscustomobject] @{ Succeeded = $false; FailureMessage = $_.Exception.Message } } }`,
  then write `$widget | Should -MatchConsoleGolden "$PSScriptRoot\Golden\x.txt"`.
- `.github/workflows/test.yml` runs xUnit on net472 and net8.0 and Pester on Windows PowerShell 5.1 and PowerShell 7
  on every push and pull request.
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
