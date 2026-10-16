---
Module Name: ConsoleMonster
Module Guid: 7ef2133c-55a5-47e6-809e-6010f6e65e09
Download Help Link: https://github.com/EvotecIT/ConsoleMonster
Help Version: 2.0.0
Locale: en-US
---
# ConsoleMonster Module
## Description
Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.

## ConsoleMonster Cmdlets
### [Add-ConsoleValue](Add-ConsoleValue.md)
Adds a value to a chart in a running dashboard.

Appends to a sparkline or line-chart series (-Key names the series), counts a value in a histogram, or adds to a bar or stacked-bar segment (-Key names it).

### [Get-ConsoleCapability](Get-ConsoleCapability.md)
Shows what ConsoleMonster detected about the current terminal.

Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).

### [New-ConsoleAlign](New-ConsoleAlign.md)
Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.

The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.

### [New-ConsoleBarChart](New-ConsoleBarChart.md)
Creates a horizontal bar chart, or a stacked bar with -Stacked.

-Data maps labels to numbers. A stacked bar splits one bar into proportional segments with a legend showing values and shares. Update values live with Add-ConsoleValue or Set-ConsoleValue.

### [New-ConsoleColumns](New-ConsoleColumns.md)
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content.

### [New-ConsoleDashboard](New-ConsoleDashboard.md)
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Start-ConsoleDashboard to run it live, or to Write-ConsoleWidget to print it once.

### [New-ConsoleGrid](New-ConsoleGrid.md)
Lays console widgets out in an equal-width grid.

Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

### [New-ConsoleHistogram](New-ConsoleHistogram.md)
Creates a histogram of values counted into buckets.

-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.

### [New-ConsoleKeyValue](New-ConsoleKeyValue.md)
Creates an aligned key/value list.

Keys come from a hashtable (use [ordered] to keep order) or from an object's properties. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. In a live dashboard, update pairs with Set-ConsoleValue.

### [New-ConsoleLanes](New-ConsoleLanes.md)
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane or appear on their first Update-ConsoleLane.

### [New-ConsoleLineChart](New-ConsoleLineChart.md)
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

-Data maps series names to arrays of values; values are spaced evenly. Append values live with Add-ConsoleValue -Key series.

### [New-ConsoleLog](New-ConsoleLog.md)
Creates a log region: the newest lines of a log, bottom-aligned.

In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen. Write to it explicitly with Write-ConsoleLog.

### [New-ConsoleLogo](New-ConsoleLogo.md)
Creates banner text drawn with a pixel font and a colour gradient.

Letters A-Z, digits and - . : ! / _ are drawn with half blocks (four rows tall) on modern and Consolas fonts, and with # in ASCII mode. Colours accept names, #hex or rgb().

### [New-ConsolePanel](New-ConsolePanel.md)
Creates a bordered panel with a title around other console widgets.

The scriptblock emits the panel content (several widgets are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.

### [New-ConsolePhases](New-ConsolePhases.md)
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

The active phase is emphasised and finished phases show their duration. Update phases live with Update-ConsolePhase.

### [New-ConsoleProgress](New-ConsoleProgress.md)
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. Update it live with Update-ConsoleProgress.

### [New-ConsoleRows](New-ConsoleRows.md)
Stacks console widgets vertically.

Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

### [New-ConsoleRule](New-ConsoleRule.md)
Creates a horizontal rule, optionally with a title.

The line uses the border colour and the glyph tier's line character.

### [New-ConsoleSparkline](New-ConsoleSparkline.md)
Creates a sparkline: a compact trend of recent values.

One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Append values live with Add-ConsoleValue.

### [New-ConsoleSpinner](New-ConsoleSpinner.md)
Creates a spinner: a one-line activity indicator for work without a known total.

While running it animates (braille on modern fonts, | / - \ on Basic and ASCII) and shows the label, a muted detail and the elapsed time. Once finished it shows the status marker in the status colour and the final duration. Update it live with Update-ConsoleSpinner.

### [New-ConsoleStack](New-ConsoleStack.md)
Layers console widgets on top of each other in the same space.

Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleAlign to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.

### [New-ConsoleStatusList](New-ConsoleStatusList.md)
Creates a checklist of items with status markers.

-Data maps item labels to statuses (pass, fail, warn, skip, running, pending, info). Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. Update items live with Update-ConsoleStatus.

### [New-ConsoleTable](New-ConsoleTable.md)
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

### [New-ConsoleText](New-ConsoleText.md)
Creates a text block from Spectre-compatible markup.

Markup such as [bold], [red], [#ff8800 on navy] and theme tokens such as [pass], [warn] or [muted] is supported. Use -Plain to show brackets literally. Text wraps to the available width.

### [New-ConsoleTree](New-ConsoleTree.md)
Creates a console tree from nested hashtables.

Each key becomes a node. A nested hashtable (or ordered dictionary) becomes child nodes; an array becomes leaf nodes; any other value is shown as a muted note after the key. Use [ordered] to keep order.

### [Send-ConsoleEvent](Send-ConsoleEvent.md)
Sends a raw live event to any widget of a running dashboard.

The general form behind the Update-* cmdlets, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

### [Set-ConsoleTheme](Set-ConsoleTheme.md)
Selects the theme used by every ConsoleMonster widget.

Themes map semantic tokens (pass, fail, warn, muted, accent, border...) to colours for every colour tier. Dark suits dark terminals (the default), Light suits light backgrounds, HighContrast uses saturated primaries.

### [Set-ConsoleValue](Set-ConsoleValue.md)
Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.

Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.

### [Start-ConsoleDashboard](Start-ConsoleDashboard.md)
Runs a console dashboard live while a script does the work.

The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession and the Update-ConsoleLane, Update-ConsolePhase, Write-ConsoleLog and related cmdlets update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.

### [Start-ConsoleReplay](Start-ConsoleReplay.md)
Replays a recorded dashboard session (JSON lines) into a dashboard.

Recordings are written by Start-ConsoleDashboard -RecordPath. Replays keep the original timing, scaled by -Speed (0 plays as fast as possible). Useful for demos, documentation captures, tests and bug reports.

### [Update-ConsoleLane](Update-ConsoleLane.md)
Updates a lane of a running dashboard: progress, step results, status and detail.

With -Result, counts one completed step (or -Steps steps) as passed, failed, warning or skipped and advances the bar. With -Status, sets the lane state (Running, Pass, Fail, Warn, Skip). -Complete finishes the lane (Fail if any step failed, Warn if any warned, otherwise Pass). Safe to call from any runspace or thread.

### [Update-ConsolePhase](Update-ConsolePhase.md)
Updates a phase of a running dashboard's phase strip.

Set -Status Running when a phase starts; its duration is measured until it gets a final status (Pass, Fail, Warn, Skip). -Detail replaces the shown detail (for example a percentage).

### [Update-ConsoleProgress](Update-ConsoleProgress.md)
Updates a progress bar in a running dashboard.

-Value sets the value, -Increment adds to it, -Label renames the bar and -Status sets a final state that recolours it.

### [Update-ConsoleSpinner](Update-ConsoleSpinner.md)
Updates a spinner in a running dashboard.

-Label renames it, -Detail changes the muted detail, -Restart restarts its clock and returns it to running, and -Status sets a final state that replaces the animation with the status marker.

### [Update-ConsoleStatus](Update-ConsoleStatus.md)
Updates an item of a status list in a running dashboard.

Adds the item when it does not exist. -Detail shows a right-aligned muted note such as a duration.

### [Write-ConsoleKeyValue](Write-ConsoleKeyValue.md)
Writes an aligned key/value list.

Same parameters as New-ConsoleKeyValue; the list is written immediately.

### [Write-ConsoleLog](Write-ConsoleLog.md)
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug or pass. Only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

### [Write-ConsoleMarkup](Write-ConsoleMarkup.md)
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], colours ([red], [#ff8800], [rgb(1,2,3)], [white on navy]) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them: [[ and ]]. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

### [Write-ConsoleRule](Write-ConsoleRule.md)
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

### [Write-ConsoleTable](Write-ConsoleTable.md)
Writes pipeline objects as a console table.

Same parameters as New-ConsoleTable; the table is written immediately in the detected output mode (live terminal, redirected stream or plain ASCII).

### [Write-ConsoleTree](Write-ConsoleTree.md)
Writes nested hashtables as a console tree.

Same parameters as New-ConsoleTree; the tree is written immediately.

### [Write-ConsoleWidget](Write-ConsoleWidget.md)
Writes console widgets once (tables, trees, panels, charts, dashboards).

Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.
