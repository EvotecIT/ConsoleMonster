---
Module Name: ConsoleMonster
Module Guid: 7ef2133c-55a5-47e6-809e-6010f6e65e09
Download Help Link: https://github.com/EvotecIT/ConsoleMonster
Help Version: 2.0.1
Locale: en-US
---
# ConsoleMonster Module
## Description
Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.

## ConsoleMonster Cmdlets
### [Add-ConsoleItem](Add-ConsoleItem.md)
Adds items to a console widget made earlier: tree nodes, pairs, status items, bars, series, log lines, lanes or values.

The PowerShell way to grow a widget after it was created, instead of calling its .NET methods. Pipe the widget in and give the items as a script block or values; they are the same item commands the widget's own script block takes: New-ConsoleTreeNode for New-ConsoleTree, New-ConsoleKeyValueItem for New-ConsoleKeyValue, New-ConsoleStatusItem for New-ConsoleStatusList and New-ConsolePhases, New-ConsoleBarItem for New-ConsoleBarChart, New-ConsoleChartSeries for New-ConsoleLineChart, New-ConsoleLogEntry for New-ConsoleLog and New-ConsoleLane for New-ConsoleLanes. Sparklines and histograms take numbers, a cell map takes status words, and a live table takes objects or hashtables with a property per column. -PassThru writes the widget on, so it can go straight to Write-ConsoleWidget. For a widget inside a running dashboard, use the live commands (Add-ConsoleValue, Set-ConsoleValue, Update-ConsoleStatus, Write-ConsoleLog...) instead.

### [Add-ConsoleRecordingMarker](Add-ConsoleRecordingMarker.md)
Adds a named playback chapter to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

### [Add-ConsoleValue](Add-ConsoleValue.md)
Adds a value to a chart in a running dashboard.

Appends to a sparkline or line-chart series (-Key names the series), counts a value in a histogram, or adds to a bar or stacked-bar segment (-Key names it).

### [Assert-ConsoleOutput](Assert-ConsoleOutput.md)
Compares console output with a golden file (for Pester and other tests).

Renders a widget at a fixed width, glyph tier and culture (invariant by default), so the result does not depend on the machine, and compares the text with a golden file. The first run writes the file and fails so it can be reviewed; a difference fails with the first differing line, both versions and the column where they part. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite golden files after an intended change. Strings are compared as they are. In Pester the thrown error fails the test; see the README for a Should operator built on it.

### [Clear-ConsoleScreen](Clear-ConsoleScreen.md)
Clears the console screen, safely.

On an interactive console the screen and its scrollback are cleared and the cursor goes to the top left, like Clear-Host. It does nothing where clearing would only lose output: redirected output, CI logs, plain mode (CONSOLEMONSTER_MODE=plain), background and thread jobs, and while a live dashboard owns the console. In the ISE and other hosts with a screen buffer it clears that buffer the way Clear-Host does.

### [Compare-ConsoleObject](Compare-ConsoleObject.md)
Compares two sets of objects and shows what was added, removed and changed, property by property.

Like Compare-Object, with a much better display: objects are matched by -Property (or by Id, Name, Key and similar members), then compared property by property, nested objects by path. The view shows the counts by kind and a table of the changes in the design-system tones (added in green, removed in red, changed in yellow), with line diffs of long and multi-line values; -View picks Summary, Table, SideBySide or Unified. -PassThru returns the structured changes (ConsoleMonster.Changes.ObjectChange) instead.

### [Compare-ConsoleText](Compare-ConsoleText.md)
Compares text or files with numbered lines, highlighted words and collapsed unchanged regions.

Shows the reference and difference side by side at 120 columns or more, and inline in narrower terminals. Text headings default to Before and After; file headings use the resolved filenames. ReferenceName and DifferenceName override these headings in both the widget and unified output. Minus and plus markers distinguish changes without colour. Context controls the unchanged lines around changes. CRLF, LF and CR endings compare equally; a missing final newline remains significant. Large inputs use bounded comparison work and linear auxiliary memory, so a large changed range may produce a non-minimal diff. AsUnified returns plain unified diff text instead of rendering.

### [Complete-ConsoleRun](Complete-ConsoleRun.md)
Finishes a run dashboard and prints its full summary into scrollback.

Marks unfinished items skipped, releases held log lines, restores the terminal and prints totals, groups, full wrapped problems and the report path. Safe to call more than once. If deferred host output cannot finish within five seconds, reports a timeout and retains it for a later call. Put it in finally so cancellation or errors restore the terminal. PassThru returns the structured snapshot; ordinary results and the script's exit code are unchanged.

### [Copy-ConsoleOutput](Copy-ConsoleOutput.md)
Copies widgets, frames, text or pipeline objects to the operating-system clipboard.

Serializes input with ConsoleExporter as Text (default), Markdown or Html and copies the source as Unicode text. Markdown uses GFM tables, property/value lists and nested tree bullets, including the shared value presenters and totals. Text preserves the console layout inside a text fence. A rendered frame or live-session snapshot retains a fenced fallback. Windows uses its native clipboard, macOS uses pbcopy, and Linux uses wl-copy (Wayland) or xclip (X11). SSH, a missing desktop/provider or a failing provider produces a non-terminating error; use -ErrorAction Stop to catch it. No output is written unless -PassThru succeeds. -WhatIf exports without accessing the clipboard.

### [Disable-ConsoleFormatting](Disable-ConsoleFormatting.md)
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.

Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress alias goes (or the user's own alias comes back), and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.

### [Enable-ConsoleFormatting](Enable-ConsoleFormatting.md)
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; commands group by module with command-type chips; modules show version, linked path and exported counts; history shows id, duration, status and highlighted command text. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. A custom Out-Default is kept unless you pass -Force for a feature that uses it. Streams, ReadLine, GridView and Prompt do not replace Out-Default.

### [Export-ConsoleFrame](Export-ConsoleFrame.md)
Exports a widget, a rendered frame or a live-session snapshot to HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Format-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Other objects are shown as a table. Several widgets are stacked. Without -Path the document is written to the pipeline.

### [Export-ConsoleRecording](Export-ConsoleRecording.md)
Exports a terminal session or dashboard recording to a player, SVG or asciicast.

Without -Dashboard, reads a whole-session asciicast v2 from Start-ConsoleRecording or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Start-ConsoleDashboard widget updates into the same definition, preserving the existing dashboard HTML, SVG and asciicast v3 exports. Add #t=30000 to an HTML address to open at 30 seconds.

### [Export-ConsoleTheme](Export-ConsoleTheme.md)
Exports the current ConsoleMonster theme as a starting file for a user or team.

Writes every semantic token, foreground/background colour, explicit fallback tier and decoration as JSON or restricted PowerShell data. The current theme is unchanged. The file extension selects the format; the parent directory must already exist. JSON is UTF-8 without a BOM; PSD1 uses a UTF-8 BOM for Windows PowerShell 5.1.

### [Format-ConsoleCode](Format-ConsoleCode.md)
Shows source text with syntax highlighting, line numbers and highlighted lines.

Highlights JSON, XML/HTML, YAML, PowerShell, C#, SQL, INI/TOML, CSV/TSV, diffs, log lines and Markdown, or plain text with -Language Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings). -Language Auto (the default) picks the language from the file extension, then from the content, and falls back to plain text. Text comes from -Path or from pipeline strings (joined as lines). The text is always literal: markup is not read and control characters are dropped. -LineNumbers adds a gutter, -Highlight marks lines (10, '10-14'), long lines are truncated with an ellipsis or wrapped with -Wrap, and output stops after -MaxLines lines with "+n more lines". A file is shown on a panel titled with its name (-Title changes it, -NoTitle leaves the panel out). Redirected output is plain text.

### [Format-ConsoleError](Format-ConsoleError.md)
Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.

Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.

### [Format-ConsoleJson](Format-ConsoleJson.md)
Shows JSON with syntax highlighting: JSON text re-indented, or objects written as JSON.

A thin convenience over Format-ConsoleCode -Language Json. Pipeline strings are joined (so Get-Content works); when they are JSON they are re-indented, otherwise they are shown as a JSON string. Other objects are collected and written with ConsoleMonster's shared value rules to -Depth levels (default 4): dictionaries with any key type, cycles as "(cycle)", values past the depth as "…", secure strings masked and byte arrays as their size.

### [Format-ConsoleList](Format-ConsoleList.md)
Formats pipeline objects as record views (labelled values) and writes them to the host.

Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Format-ConsoleTable. Redirected output is plain text.

### [Format-ConsoleTable](Format-ConsoleTable.md)
Formats pipeline objects as a width-aware table and writes it to the host.

Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards. One object stays a one-row table; -View Record shows objects as labelled fields and -View Cards as cards. Redirected output is plain text without escape codes.

### [Format-ConsoleTree](Format-ConsoleTree.md)
Shows the structure of pipeline objects as a tree (or an inspector view) and writes it to the host.

Works for any object: PSCustomObjects, .NET objects, hashtables and dictionaries, nested to -Depth levels. Collections list up to -MaxItems items followed by "+N more"; reference cycles and the depth limit are marked instead of recursing forever. -Include and -Exclude filter member names (wildcards). With -AsInspector the object is shown as a record view: nested objects as indented sections and uniform collections as nested tables. Redirected output is plain text.

### [Get-ConsoleCapability](Get-ConsoleCapability.md)
Shows what ConsoleMonster detected about the current terminal.

Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).

### [Get-ConsoleDemo](Get-ConsoleDemo.md)
Lists the copyable ConsoleMonster gallery demos or exports one PowerShell script.

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. With -Path, an exact slug exports the canonical self-contained script as UTF-8; ConsoleMonster must be installed to run it. The parent directory must exist. Existing files are preserved unless -Force is supplied.

### [Invoke-ConsolePester](Invoke-ConsolePester.md)
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Invoke-ConsoleRecording](Invoke-ConsoleRecording.md)
Records a script in a child PowerShell terminal and exports the result.

Runs the script in an isolated child process. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

### [Invoke-ConsoleRun](Invoke-ConsoleRun.md)
Runs a script block for every input item, optionally in parallel, with a live view of the run.

ForEach-ConsoleItem is its alias. $_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Name (or -Label) sets each item's label: a property name or a script block. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.

### [New-ConsoleActivityList](New-ConsoleActivityList.md)
Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.

Each activity has an id, a title, an optional status, a percentage (or none, which spins) and an optional parent it is indented under; finished activities disappear, and an empty list takes no space. Update it with Update-ConsoleActivity from the script of a running dashboard or menu run. -MaxVisible caps the rows (more are summed up in one line). The default name, $progress, is also where Write-Progress goes in Start-ConsoleDashboard, so a list with that name shows the script's progress records; give another name for activities of your own.

### [New-ConsoleAlign](New-ConsoleAlign.md)
Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.

The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.

### [New-ConsoleBarChart](New-ConsoleBarChart.md)
Creates a horizontal bar chart, or a stacked bar with -Stacked.

Bars come from -Data (labels mapped to numbers), from a script block of New-ConsoleBarItem calls (when a bar needs its own tone) or from the pipeline. A stacked bar splits one bar into proportional segments with a legend showing values and shares. -Tone sets the colour of every bar as a theme role (Fail, Warn, Pass, Accent, Series1 to Series6...); -Warning and -Critical colour bars by threshold instead. To add bars later, pipe the chart to Add-ConsoleItem; in a live dashboard, update values with Add-ConsoleValue or Set-ConsoleValue.

### [New-ConsoleBarItem](New-ConsoleBarItem.md)
Creates one bar of a bar chart, or one segment of a stacked bar, with its own tone.

Use it inside the script block of New-ConsoleBarChart when a bar or a segment needs its own colour: -Tone takes a theme role (Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted, Series1 to Series6). Without -Tone the chart's colour is used, and a stacked bar picks the next series colour. The label is shown literally; rich text from New-ConsoleText keeps its colours. To add bars to a chart made earlier, pipe the chart to Add-ConsoleItem.

### [New-ConsoleBinding](New-ConsoleBinding.md)
Creates a display rebuilt from application state on the owning PowerShell input thread.

Content receives param($State) and injected $State as a native hashtable. Emit widgets or text using the ordinary layout commands. Input controls must remain in the stable outer layout. Added or assigned state keys write back through AppState; bindings should normally only read state.

### [New-ConsoleButton](New-ConsoleButton.md)
Creates a named action button for Show-ConsoleApp.

Emits a stable core Button widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleCellMap](New-ConsoleCellMap.md)
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. -Columns and -RowLabels turn it into a labelled grid (one row per server, one column per check) with one legend below, which -LegendLines lets wrap. To add cells later, pipe the map to Add-ConsoleItem; in a live dashboard, update cells with Update-ConsoleCell.

### [New-ConsoleChartSeries](New-ConsoleChartSeries.md)
Creates one series of a line or scatter chart: a name, values and an optional tone.

Use it inside the script block of New-ConsoleLineChart. Values come from -Value or from the pipeline, oldest first, and are spaced evenly. -Tone takes a theme role (Accent, Accent2, Pass, Fail, Warn, Series1 to Series6); without it the chart picks the next series colour, and a series named with a status word (failed, passed, warnings) takes that status colour. To add a series to a chart made earlier, pipe the chart to Add-ConsoleItem; in a running dashboard, append values with Add-ConsoleValue -Key.

### [New-ConsoleClock](New-ConsoleClock.md)
Creates a run clock (ConsoleMonster.Live.SimulatedClock) the script moves forward, for simulated runs whose screen shows simulated time.

A live session reads the clock for the header clock, the summary bar's elapsed time and time left, the times of log lines and activities, and the run result's duration, so a demo or a replay of logged work shows its own time instead of the wall clock. Give it to a menu run as the third argument of $MenuContext.Run($dashboard, { ... }, $clock), or to Start-ConsoleDashboard -Clock. In the script, $clock.Wait(seconds) and $clock.WaitUntil(seconds) move it forward in small steps, sleeping 1/-Speed of each simulated second (never at speed 0, which runs at once); $clock.Advance(seconds) jumps without waiting and $clock.Hold(seconds) waits real time with the clock standing still. $clock.Elapsed is the time on the clock. Once the dashboard is stopped (Ctrl+C) waits stop sleeping, so the script reaches its end at once.

### [New-ConsoleColumns](New-ConsoleColumns.md)
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.

### [New-ConsoleDashboard](New-ConsoleDashboard.md)
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Start-ConsoleDashboard to run it live, or to Write-ConsoleWidget to print it once.

### [New-ConsoleField](New-ConsoleField.md)
Creates a form field for New-ConsoleForm.

A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validate and the type's own checks) and the command-line parameter it supplies (-Parameter) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values). Choices are built from any objects: -Display, -GroupBy, -Describe, -Badge, -Disabled and -Selected read each object (a property name, or a scriptblock for choices given with -Choices; loaded choices take property names), and -CommandValue picks the text the equivalent command shows and the field returns.

### [New-ConsoleForm](New-ConsoleForm.md)
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back. Presets the form comes with (New-ConsoleFormPreset, in the scriptblock or -Preset) are listed before the saved ones.

### [New-ConsoleFormPreset](New-ConsoleFormPreset.md)
Creates a preset a form comes with: field values under one name, with a description and a preview.

Give the presets to New-ConsoleForm (-Preset, or emit them in its scriptblock next to the fields). They are listed before the presets people save, marked as built in, and cannot be overwritten or deleted; applying one (Read-ConsoleForm -Preset, or the menu's command palette) sets the fields it names the way a saved preset does. -Values maps field names to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice, and a value for a field whose choices load is chosen once they arrive.

### [New-ConsoleGrid](New-ConsoleGrid.md)
Lays console widgets out in an equal-width grid.

Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

### [New-ConsoleHistogram](New-ConsoleHistogram.md)
Creates a histogram of values counted into buckets.

-Edge defines ascending bucket edges. Bars grow with eighth-block precision on modern fonts. Add values live with Add-ConsoleValue.

### [New-ConsoleKeyValue](New-ConsoleKeyValue.md)
Creates an aligned key/value list from a hashtable, an object's properties or New-ConsoleKeyValueItem pairs.

Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of New-ConsoleKeyValueItem calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Set-ConsoleValue.

### [New-ConsoleKeyValueItem](New-ConsoleKeyValueItem.md)
Creates one key/value pair, with an optional status or presenter, for New-ConsoleKeyValue.

Use it inside the script block of New-ConsoleKeyValue or Write-ConsoleKeyValue when a pair needs more than a hashtable gives: a status marker before the value (-Status), markup in the value (-Markup), or a value formatted by a presenter (-Presenter Bytes, Duration, Date...). Values are shown literally unless -Markup is used. To add pairs to a list made earlier, pipe the list to Add-ConsoleItem.

### [New-ConsoleLane](New-ConsoleLane.md)
Creates one lane (a worker, server or item of a parallel workload) for New-ConsoleLanes.

Use it inside the script block of New-ConsoleLanes when lanes differ: each has its own total, progress, status, result counts and detail. -Key names the lane for Update-ConsoleLane; the label defaults to the key and accepts rich text from New-ConsoleText. To add lanes to a widget made earlier, pipe it to Add-ConsoleItem.

### [New-ConsoleLanes](New-ConsoleLanes.md)
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane (names sharing one -Total), with a script block of New-ConsoleLane calls (each with its own total, progress, status and counts), from the pipeline, or appear on their first Update-ConsoleLane in a live dashboard. To add lanes later, pipe the widget to Add-ConsoleItem.

### [New-ConsoleLineChart](New-ConsoleLineChart.md)
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

Series come from a script block of New-ConsoleChartSeries calls (each with its values and an optional tone), from the pipeline, or from -Data (series names mapped to arrays of values). Values are spaced evenly. -Series declares empty series for a live chart. To add a series later, pipe the chart to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue -Key series.

### [New-ConsoleLiveTable](New-ConsoleLiveTable.md)
Creates a table whose rows change while a dashboard runs.

Rows have stable keys and are added, updated, removed or cleared with Update-ConsoleTable. Column widths only grow, so rows never jitter. -Column takes header texts or column hashtables (Alignment, Width, Flexible, Markup and the other column options of Format-ConsoleTable). -MaxRows keeps only the most recent rows; -SortBy keeps rows sorted by a column.

### [New-ConsoleLog](New-ConsoleLog.md)
Creates a log region: the newest lines of a log, bottom-aligned.

Printed once, the lines come from a script block of New-ConsoleLogEntry calls (each with a level and a time) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLog. To add lines to a log made earlier, pipe it to Add-ConsoleItem.

### [New-ConsoleLogEntry](New-ConsoleLogEntry.md)
Creates one log line with a level and a time, for New-ConsoleLog.

Use it inside the script block of New-ConsoleLog to show a log printed once, or pipe a log made earlier to Add-ConsoleItem with these entries. -Level takes info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). -At is the time shown at the start of the line: a TimeSpan, a number of seconds, or a number with a unit (500ms, 2s, 1.5m, 1h). The message is shown literally; rich text from New-ConsoleText keeps its colours. In a running dashboard, write to the log with Write-ConsoleLog instead.

### [New-ConsoleLogo](New-ConsoleLogo.md)
Creates banner text drawn with a pixel font and a colour gradient.

Letters A-Z, digits and - . : ! / _ are drawn with half blocks (four rows tall) on modern and Consolas fonts, and with # in ASCII mode. Colours accept names, #hex or rgb().

### [New-ConsoleMenu](New-ConsoleMenu.md)
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenuPreset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.

### [New-ConsoleMenuItem](New-ConsoleMenuItem.md)
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -State can be scriptblocks, recomputed every time the menu draws; -Disabled returns why the item is unavailable.

### [New-ConsoleMenuPreset](New-ConsoleMenuPreset.md)
Creates a preset a menu comes with: form values and selections under one name, with a description and a preview.

Give the presets to New-ConsoleMenu (-Preset, or emit them in its scriptblock) and add a New-ConsoleMenuItem -Presets item: it lists them first, marked as built in, before the presets people save; they cannot be overwritten or deleted. Applying one sets the values it names and keeps the rest. -Form with -Values fills in a form opened from the menu (a list for a multi-choice field; a value for a field whose choices load is chosen once they arrive). -Item with -Selection selects values in a selection item. -Preview is shown beside the list while the preset has focus: a scriptblock (run once, now), widgets, text or objects.

### [New-ConsoleMultiSelect](New-ConsoleMultiSelect.md)
Creates a named multiselect input for Show-ConsoleApp.

Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.

### [New-ConsolePadding](New-ConsolePadding.md)
Adds empty space around console widgets.

-Padding takes one, two or four numbers of cells: all sides; left and right, then top and bottom; or left, top, right and bottom (the order of the .NET Padding widget). Strings in the script block are markup text. Use New-ConsoleAlign to place content inside its space instead.

### [New-ConsolePanel](New-ConsolePanel.md)
Creates a bordered panel with a title around other console widgets.

-Content is a scriptblock, a widget, text or any objects (objects are shown as a record or a table; several pieces are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.

### [New-ConsolePhases](New-ConsolePhases.md)
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

Phases come from -Phase (names, all pending) or from a script block of New-ConsoleStatusItem calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-ConsolePhase, which also measures their duration.

### [New-ConsoleProgress](New-ConsoleProgress.md)
Creates a labelled progress bar.

Shows a bar with sub-cell precision for the glyph tier, the percentage and value/total. -Status marks a finished bar (Pass, Fail, Warn, Skip) with its status colour. Update it live with Update-ConsoleProgress.

### [New-ConsoleRegion](New-ConsoleRegion.md)
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Set-ConsoleRegion -Name with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

### [New-ConsoleRows](New-ConsoleRows.md)
Stacks console widgets vertically.

Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

### [New-ConsoleRule](New-ConsoleRule.md)
Creates a horizontal rule, optionally with a title.

The line uses the border colour and the glyph tier's line character.

### [New-ConsoleRun](New-ConsoleRun.md)
Starts a bounded run dashboard for work scheduled by your script.

Declares the queued items and returns a LiveRun handle. Use Update-ConsoleRun as workers start or finish and Complete-ConsoleRun in a finally block. Finished items stream above the live dashboard; failures and warnings remain pinned. The dashboard uses at most 40 percent of terminal height by default. CI and redirected hosts use append-only lines with periodic totals. This command does not execute work or change its results or exit code. Name, Target and Group accept property names or script blocks evaluated once, before the session starts. Interactive keys are opt-in: p pauses the log, f filters it to problems, g toggles groups, and ? shows help.

### [New-ConsoleRunResult](New-ConsoleRunResult.md)
Creates the result a menu run returns: a title, a status, a summary, figures and next actions.

Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -FigureStatus colours the figures that need attention (Pass, Warn, Fail by label). -Status takes the shared status vocabulary (default Pass). The time shown with the result is the run's time on its session clock (real time, or a simulated clock given to the run); -Elapsed overrides it.

### [New-ConsoleSelect](New-ConsoleSelect.md)
Creates a named select input for Show-ConsoleApp.

Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.

### [New-ConsoleSparkline](New-ConsoleSparkline.md)
Creates a sparkline: a compact trend of recent values.

One row by default (▁▂▄▆█ on modern fonts, shades on Consolas, an ASCII ramp in ASCII mode); with -Height above 1 it becomes a small area chart. Values come from -Value or the pipeline. -Tone sets the colour, -Maximum the top of the scale and -NoLatest hides the latest value. To add values later, pipe it to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue.

### [New-ConsoleSpinner](New-ConsoleSpinner.md)
Creates a spinner: a one-line activity indicator for work without a known total.

While running it animates (braille on modern fonts, | / - \ on Basic and ASCII) and shows the label, a muted detail and the elapsed time. Once finished it shows the status marker in the status colour and the final duration. Update it live with Update-ConsoleSpinner.

### [New-ConsoleStack](New-ConsoleStack.md)
Layers console widgets on top of each other in the same space.

Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleAlign to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.

### [New-ConsoleStatusItem](New-ConsoleStatusItem.md)
Creates one status item, a step with a status and a detail, for New-ConsoleStatusList and New-ConsolePhases.

Use it inside the script block of New-ConsoleStatusList (a checklist) or New-ConsolePhases (a strip of stages). -Status is the shared status vocabulary: Pass, Fail, Warn, Skip, Info, Running or Pending, and PowerShell's words such as Success, Warning and Error. -Detail is a muted note on the right, such as a duration. The label is shown literally; rich text from New-ConsoleText keeps its colours. -Key names the item for live updates (Update-ConsoleStatus, Update-ConsolePhase) when it differs from the label. To add items to a list made earlier, pipe the list to Add-ConsoleItem.

### [New-ConsoleStatusList](New-ConsoleStatusList.md)
Creates a checklist of items with status markers.

Items come from a script block of New-ConsoleStatusItem calls (each with its own status and detail), from the pipeline, from -Data (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-ConsoleStatus.

### [New-ConsoleSummaryBar](New-ConsoleSummaryBar.md)
Creates a summary bar: one line with progress, done of total, the time left and result counters.

The line Invoke-ConsoleRun ends its view with, as a widget of its own: a thin bar, done of total, the percentage, the time left (estimated from -Elapsed and the share done) and counters in a fixed order with zeros dimmed rather than hidden. -Counter maps labels to counts; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour. Without -Total only the count done shows.

### [New-ConsoleTable](New-ConsoleTable.md)
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

### [New-ConsoleText](New-ConsoleText.md)
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Style adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLog and Update-ConsoleLane, and with Write-ConsoleWidget. Without colour (NO_COLOR, redirected output) the text stays plain.

### [New-ConsoleTextArea](New-ConsoleTextArea.md)
Creates a named multiline text input for Show-ConsoleApp.

Emits a stable core TextArea widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleTextInput](New-ConsoleTextInput.md)
Creates a named single-line text input for Show-ConsoleApp.

Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleToggle](New-ConsoleToggle.md)
Creates a named Boolean toggle for Show-ConsoleApp.

Emits a stable core Toggle widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleTree](New-ConsoleTree.md)
Creates a console tree from nested New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of New-ConsoleTreeNode calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -NameProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -Data maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

### [New-ConsoleTreeNode](New-ConsoleTreeNode.md)
Creates a tree node with an optional status, a muted detail and child nodes, for New-ConsoleTree.

Use it inside the script block of New-ConsoleTree, or inside another New-ConsoleTreeNode to nest. The label accepts markup or rich text from New-ConsoleText. -Status shows a status marker before the label (Pass, Fail, Warn, Skip, Info, Running, Pending, or PowerShell's words such as Success, Warning and Error). -Detail is a muted note after the label, and -Value a value formatted by a presenter (sizes, dates, durations). The script block emits the children: more New-ConsoleTreeNode calls, or strings for leaves (shown literally). To add nodes to a tree made earlier, pipe the tree to Add-ConsoleItem.

### [Out-ConsoleChart](Out-ConsoleChart.md)
Draws horizontal bars, a histogram or grouped sparklines from pipeline objects.

Select one numeric property, including a calculated property, or pipe numbers directly.
Bars show value labels in theme colours. Sorting is stable and happens before Top. Sparkline labels identify groups;
samples within each group retain arrival order, so sort timestamped input before piping it. Histogram uses every
valid sample regardless of Sort or Top, and integral data has whole-number bucket edges. Null, malformed, boolean
and non-finite values produce a non-terminating error and are skipped; an empty input produces no output.
No-colour output lists numeric values, bucket counts or all trend samples. Signed trends also use numerical text
because the existing sparkline renderer scales from zero. Output uses the normal ConsoleMonster formatting route.

### [Out-ConsoleDefault](Out-ConsoleDefault.md)
Shows objects of registered types through ConsoleMonster and passes everything else to PowerShell's Out-Default.

This is what Enable-ConsoleFormatting puts behind Out-Default. Consecutive objects of registered types (Register-ConsoleView; after Enable-ConsoleFormatting also files and folders, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries) are collected and shown as one ConsoleMonster view; any other object, format data from Format-*, errors and strings go to PowerShell's Out-Default unchanged. It only takes over when the output is displayed on a real local console: redirected output, jobs, remoting and other hosts get PowerShell's default output (-Force renders anyway). The objects themselves are never changed.

### [Out-ConsoleFormatCommand](Out-ConsoleFormatCommand.md)
Renders native PowerShell formatting records through ConsoleMonster.

The host-only sink used by Enable-ConsoleFormatting -Feature FormatCommands. PowerShell still resolves properties, calculated expressions, grouping and named views. Unrecognized formatting records are passed to the native Out-Default. Use -AsWidget to export the same view with Export-ConsoleFrame.

### [Read-ConsoleConfirm](Read-ConsoleConfirm.md)
Asks a yes/no question and returns $true or $false.

Shows the question with the answers inline (the default focused); Y and N answer at once, arrows move, Enter takes the focused answer and Esc answers no. With -TypedConfirmation the user must type a text (a server name, a domain) before a destructive action; Esc then returns $false. In remoting and the ISE the host asks (PromptForChoice); without a user the default is returned.

### [Read-ConsoleForm](Read-ConsoleForm.md)
Shows a form or wizard and returns the answers.

The form opens on the alternate screen (or in the scrollback with -Inline). Fields are edited in place; choice fields open a filterable list; dependent options load with a spinner. The review page shows every answer and the equivalent command (copy it, save the answers as a named preset, or run). The answers come back as one object with a property per visible field (secrets as SecureString unless -AsPlainText); -PassThru returns the full result (Submitted, Values, CommandLine) instead. Cancelling returns nothing. -AppName stores presets, and with -RememberLastChoices the last answers, per user under %APPDATA%\ConsoleMonster\<app>. In remoting and the ISE the fields are asked one by one; without a user the defaults are used, or an error names the first field that needs a value.

### [Read-ConsoleMultiSelection](Read-ConsoleMultiSelection.md)
Asks the user to pick any number of the choices and returns the chosen objects.

Everything Read-ConsoleSelection does, plus checkboxes: Space toggles (on a group it toggles the whole group, shown as [–] when partly selected; Left and Right collapse and expand groups), Ctrl+A selects all shown, Ctrl+N none, Ctrl+T inverts, Ctrl+O shows only the selected ones. -Minimum and -Maximum are enforced, and a live count shows how many are selected. The chosen objects are written to the pipeline in list order.

### [Read-ConsoleNumber](Read-ConsoleNumber.md)
Asks for a number.

Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).

### [Read-ConsolePath](Read-ConsolePath.md)
Asks for a file or folder path, with completion.

Matching entries of the typed folder are listed under the box (folders first); Tab completes the common part or the focused entry, Up and Down move through them. -Kind File or Folder and -MustExist are checked on Enter. Returns the path as typed (environment variables and ~ are expanded only when checking).

### [Read-ConsoleSecret](Read-ConsoleSecret.md)
Asks for a secret and returns a SecureString (or a string with -AsPlainText).

The typed text is masked (or not shown at all with -Hidden); Ctrl+R reveals it while held open. The answer never appears in the scrollback summary. In remoting and the ISE the host reads it (ReadLineAsSecureString).

### [Read-ConsoleSelection](Read-ConsoleSelection.md)
Asks the user to pick one of the choices and returns the chosen object.

Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-Disabled) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.

### [Read-ConsoleText](Read-ConsoleText.md)
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validate check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

### [Register-ConsoleView](Register-ConsoleView.md)
Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Format-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). -Remove deletes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.

### [Send-ConsoleEvent](Send-ConsoleEvent.md)
Sends a raw live event to any widget of a running dashboard.

The general form behind the Update-* cmdlets, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

### [Set-ConsoleFormValue](Set-ConsoleFormValue.md)
Sets field values of a form, whether or not it is shown.

Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Value maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Value also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.

### [Set-ConsoleRegion](Set-ConsoleRegion.md)
Replaces the content of a region (New-ConsoleRegion) in the running dashboard.

The new widget is shown from the next frame on; the surrounding frame stays, and live widgets inside the new content (lanes, logs, live tables) can be updated by name right away. Use it from the dashboard script, like the Update-Console* commands.

### [Set-ConsoleTheme](Set-ConsoleTheme.md)
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

### [Set-ConsoleValue](Set-ConsoleValue.md)
Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.

Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.

### [Show-Console](Show-Console.md)
Shows pipeline input in the view that fits it best.

One object becomes an inspector; objects of one shape a table (cards when the window is narrow), using their DefaultDisplayPropertySet and presenters by type (bytes for Length and Size, dates, durations, Yes/No, status markers for enums such as Running/Stopped); hierarchical objects (a child collection of the same type, or -ChildProperty) a tree; a numeric series a bar chart; strings highlighted code when a language is detected, otherwise text. -View overrides the choice; Grid opens the interactive Show-ConsoleGrid and returns what you pick.

### [Show-ConsoleApp](Show-ConsoleApp.md)
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -StaticSnapshot to capture or print initialized values without interactive input. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

### [Show-ConsoleChanges](Show-ConsoleChanges.md)
Shows the changes an action would make and asks before making them: the what-if pattern.

Compares -Before with -After (as Compare-ConsoleObject does), shows the preview, then asks for confirmation and returns $true or $false; act only on $true. Changes that remove objects need the number of removals typed (or the -TypedConfirmation text). It honours the caller's -WhatIf: the preview is shown and $false returned without asking. -Force (or -Confirm:$false) skips the question and returns $true when there are changes. Use it inside your own functions: if (Show-ConsoleChanges -Before $current -After $planned -Property Name) { apply }.

### [Show-ConsoleDemo](Show-ConsoleDemo.md)
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

Executes the same self-contained PowerShell source offered by Get-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot and -Step are passed to the demo; omitted parameters retain the script's defaults. Demo output and errors follow the normal PowerShell streams.

### [Show-ConsoleGrid](Show-ConsoleGrid.md)
Shows pipeline objects in an interactive grid and returns the rows you choose.

An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Format-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list. Without an interactive user (redirected output, a job, CI) the rows are printed as a table with a warning and nothing is returned; -Mode None fails with an error instead.

### [Show-ConsoleHelp](Show-ConsoleHelp.md)
Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.

Reads the help PowerShell has for the command (Get-Help -Full): cmdlet help files, comment-based help of functions and scripts, or the help PowerShell generates when there is none. It shows the name and synopsis, the syntax of each parameter set (parameter names, value types and optional parts told apart), the description, a parameter table (type, required, position, pipeline input, default, description), the examples with highlighted PowerShell code, inputs, outputs, notes and related links. Piped on (| more, | Out-Host -Paging, | Out-File) it writes plain lines, so pagers and files get readable text. About topics use the shared Markdown reader, retaining literal HTML and safe link handling. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Show-ConsoleMarkdown](Show-ConsoleMarkdown.md)
Reads Markdown with native console tables, highlighted code and interactive paging.

Reads CommonMark with pipe tables through the optional OfficeIMO-backed add-on. Headings use accent rules, lists have hanging indents, and block quotes have a vertical bar. Code uses the registered highlighter and its language label. Links use OSC 8 when supported and show a muted URL otherwise. HTML stays literal, images become labelled links, and document content is never executed or fetched. Long documents page only when output goes directly to an interactive host. Piped, redirected, recorded and non-interactive output never acquires input. Pipeline strings are joined before parsing, preserving reference links and block structure. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Show-ConsoleMenu](Show-ConsoleMenu.md)
Shows a menu until the user exits.

Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.

### [Show-ConsoleObject](Show-ConsoleObject.md)
Explores an object's members with the keyboard.

Arrows move, Right and Left expand and collapse, Enter drills into a member, Backspace returns to its parent, and / searches displayed names and values. Ctrl+Enter or F10 accepts the selected node; Esc cancels. Nested values are read lazily and cycles, depth and collection sizes are bounded. In redirected, CI, line and non-interactive sessions, a static inspector is printed and no selection is returned. Objects parsed by ConvertFrom-Json use the same PowerShell member adapter as other objects.

### [Show-ConsoleOverview](Show-ConsoleOverview.md)
Shows a compact, read-only summary of the local host.

Collects CPU, memory, local fixed disks, network addresses and safely available services, update/reboot and security observations. Unavailable panels are hidden. Slow sections show a pending marker after the caller budget; each probe receives a two-second cooperative timeout. At 80 columns or more panels use two columns and at most 24 rows, capped at 120 columns. Below 80 columns panels stack at their natural height, preserving volumes and login rows; terminal scrolling is allowed. No elevation, child processes, remote access or system changes are used. Security data requires existing read access. Linux update and security support depends on local update-notifier and auth.log facilities; macOS optional service, update and security panels are hidden. Register application sections through OverviewSectionRegistry.Default.Register. Custom providers must honor cancellation and release their resources.

### [Show-ConsoleReplay](Show-ConsoleReplay.md)
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

Plays a recording (written by Start-ConsoleDashboard -RecordPath or -AutoRecord) into the same dashboard and lets you move through it: space plays and pauses, left and right step frame by frame, + and - change the speed, PgUp and PgDn scrub, Home and End jump to the ends, n/p jump to the next or previous marker, f/F to failures, w/W to warnings, l/L to log entries, and / searches the log text (Enter for the next match). Markers are added while recording with Send-ConsoleEvent -Target '$marker' -Operation mark -Text 'label'. Without -Path the newest automatic recording is played. Without an interactive console the markers are printed as lines.

### [Start-ConsoleDashboard](Start-ConsoleDashboard.md)
Runs a console dashboard live while a script does the work.

The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession and the Update-ConsoleLane, Update-ConsolePhase, Write-ConsoleLog and related cmdlets update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.

### [Start-ConsoleRecording](Start-ConsoleRecording.md)
Opens a recorded child shell in a pseudo-console.

Records terminal output and resizes to local asciicast v2. Exit the child shell or run Stop-ConsoleRecording inside it to finalize. Input bytes are never stored; output secret patterns are redacted before writing. The title shows a REC indicator. Use -InProcess for explicitly limited PowerShell output capture when pseudo-consoles are unavailable.

### [Start-ConsoleReplay](Start-ConsoleReplay.md)
Replays a recorded dashboard session (JSON lines) into a dashboard.

Recordings are written by Start-ConsoleDashboard -RecordPath. Replays keep the original timing, scaled by -Speed (0 plays as fast as possible). Useful for demos, documentation captures, tests and bug reports.

### [Stop-ConsoleRecording](Stop-ConsoleRecording.md)
Stops recording from inside the recorded child shell.

Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Start-ConsoleRecording session.

### [Update-ConsoleActivity](Update-ConsoleActivity.md)
Starts, updates or finishes an activity of an activity list (New-ConsoleActivityList) in the running dashboard.

-Id identifies the activity: a new id adds a row, an existing one updates it, and -Complete removes it (with the activities under it). -Activity is the title, -Status a short text after it, -Percent the share done (leave it out for work of unknown length, which spins) and -SecondsRemaining an estimate shown as time left. -ParentId indents the activity under another one. The time each activity has taken follows the session clock, so a simulated clock (New-ConsoleClock) drives it too. Safe to call from any runspace or thread.

### [Update-ConsoleCell](Update-ConsoleCell.md)
Sets the state of cells of a cell map (New-ConsoleCellMap) in the running dashboard.

-Index names the cells by position, from 0 (in a grid with -Columns, row * columns + column); -Status is their new state: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success and Error work too). An index past the end grows the map. -Add appends cells in the -Status state instead, and -Count sets the number of cells (new ones are pending). Safe to call from any runspace or thread.

### [Update-ConsoleLane](Update-ConsoleLane.md)
Updates a lane of a running dashboard: progress, step results, status and detail.

With -Result, counts one completed step (or -Steps steps) as passed, failed, warning or skipped and advances the bar. With -Status, sets the lane state (Running, Pass, Fail, Warn, Skip). -Complete finishes the lane (Fail if any step failed, Warn if any warned, otherwise Pass). Safe to call from any runspace or thread.

### [Update-ConsolePhase](Update-ConsolePhase.md)
Updates a phase of a running dashboard's phase strip.

Set -Status Running when a phase starts; its duration is measured until it gets a final status (Pass, Fail, Warn, Skip). -Detail replaces the shown detail (for example a percentage).

### [Update-ConsoleProgress](Update-ConsoleProgress.md)
Updates a progress bar in a running dashboard.

-Value sets the value, -Increment adds to it, -Label renames the bar and -Status sets a final state that recolours it.

### [Update-ConsoleRun](Update-ConsoleRun.md)
Reports an item's running phase or final outcome to a run dashboard.

Index is the zero-based position in New-ConsoleRun's Items. Running starts the item or updates its phase without resetting elapsed time. Pass, Fail, Warn and Skip finish an item once; repeated completions do not print duplicate lines. Updates do not produce pipeline output or change the caller's error handling. For direct worker-thread use the handle exposes Started and Finished methods with the same contracts.

### [Update-ConsoleSpinner](Update-ConsoleSpinner.md)
Updates a spinner in a running dashboard.

-Label renames it, -Detail changes the muted detail, -Restart restarts its clock and returns it to running, and -Status sets a final state that replaces the animation with the status marker.

### [Update-ConsoleStatus](Update-ConsoleStatus.md)
Updates an item of a status list in a running dashboard.

Adds the item when it does not exist. -Detail shows a right-aligned muted note such as a duration.

### [Update-ConsoleSummaryBar](Update-ConsoleSummaryBar.md)
Updates a summary bar (New-ConsoleSummaryBar) in the running dashboard: progress and counters.

-Done sets the items done (-Total changes the total; without it the bar keeps its total) and -Counter sets counts by label, in one call; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour, and new labels are added after the existing ones. The elapsed time and the time left follow the session clock, so a simulated clock (New-ConsoleClock) drives them too. Safe to call from any runspace or thread.

### [Update-ConsoleTable](Update-ConsoleTable.md)
Adds, updates, removes or clears rows of a live table (New-ConsoleLiveTable) in the running dashboard.

-Key identifies the row: a new key adds a row, an existing key replaces its values. -Remove deletes the row and -Clear deletes them all; the column widths stay.

### [Use-ConsoleProgress](Use-ConsoleProgress.md)
Runs a script and shows its Write-Progress calls as an inline progress area.

The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. If another runspace owns the terminal's progress area, the script also runs with PowerShell's native progress. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Use-ConsoleTarget](Use-ConsoleTarget.md)
Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.

Inside the script block every ConsoleMonster command (Write-ConsoleText, Format-ConsoleTable, Write-ConsoleMarkup, live dashboards, prompts) writes to the target instead of the console. With -Path the output goes to a UTF-8 file, with -Writer to a TextWriter, with -Target to a target object (ConsoleMonster.Output.ITerminalTarget); with none of them it is returned as one string. -Width, -Glyphs and -Colors lay the output out for the file or string; prompts there have nobody to answer and use their defaults. The script block's own output is passed through.

### [Wait-ConsoleRun](Wait-ConsoleRun.md)
Waits for worker updates while pumping run progress to the PowerShell host.

Use after starting background workers that call the handle's Started and Finished methods. Waits until all items have finished and receives queued host output on the pipeline thread, including timed CI progress while workers are slow. It does not schedule work or print the final summary. Call Complete-ConsoleRun in finally after joining the workers. Ctrl+C signals the handle's Cancellation token so workers can drain and report their last outcomes.

### [Watch-Console](Watch-Console.md)
Runs a script block again and again and shows its latest output as a live view.

Like watch: every -Interval seconds the script block runs and its output replaces the previous one, shaped as Format-ConsoleTable and the other commands shape it (objects as a table, one dictionary as a record, text as it is). The heading shows the interval, -Title, the run number and the time. -Differences highlights the cells that changed since the previous run. A run that fails shows its error and the watch goes on. Warnings, errors and Write-Host text from the script block are shown under its output in the view (an error marks the run in the heading) instead of interrupting it. Ctrl+C ends the watch and the last frame stays in the scrollback; -Count ends it after that many runs. Piped into another command (Out-File, Out-String), or without an interactive console (redirected output, CI, a job), every run is printed instead, so use -Count there; assigning the result to a variable at the prompt does not capture the live view, so pipe it instead.

### [Watch-ConsoleLog](Watch-ConsoleLog.md)
Follows a log file or a local Windows event log with a live, scrollable log view.

File lines are decoded incrementally and followed across observed truncation and rotation. Event logs use Get-WinEvent on Windows. The view follows new entries; scrolling pauses following and End resumes it. Filter and Highlight are literal case-insensitive substrings. Piped, redirected, CI and -Plain output is ordinary literal text and never reads keyboard input. Count is source polls, as in Watch-Console: one takes a snapshot, zero follows until q, Escape or Ctrl+C. Capacity bounds retained history. The default interval is 0.25 seconds.

### [Write-ConsoleException](Write-ConsoleException.md)
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

### [Write-ConsoleKeyValue](Write-ConsoleKeyValue.md)
Writes an aligned key/value list.

Same parameters as New-ConsoleKeyValue; the list is written immediately.

### [Write-ConsoleLog](Write-ConsoleLog.md)
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

### [Write-ConsoleMarkup](Write-ConsoleMarkup.md)
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], the 256-colour palette names ([red], [cadetblue], [grey50], [deepskyblue3]; in markup [darkred] is the palette colour, [console.darkred] the ConsoleColor as Write-Host shows it), [#ff8800], [rgb(1,2,3)], [white on navy], links ([link=https://example.com]text[/] or [link]https://example.com[/], clickable in Windows Terminal and VS Code) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them ([[ and ]]) or with [ConsoleMonster.Text.Markup]::Escape($value). A tag that is not a known style is removed and its text kept when a [/] closes it; an unclosed unknown tag stays literal. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

### [Write-ConsoleProgress](Write-ConsoleProgress.md)
Writes a progress update, drawn by ConsoleMonster when Enable-ConsoleFormatting -Feature Progress is on.

Takes the same parameters as Write-Progress. With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias of this command for the session, so scripts and functions that call Write-Progress get ConsoleMonster's progress area: one row per activity below the cursor (nested with -ParentId), with a bar, the percentage, the status and the elapsed time. Output and errors written in between appear above the area, warnings and Write-Host lines too, and the area disappears when every activity completes or the command ends. $ProgressPreference and -ProgressAction are honoured. Without the feature, in redirected output, jobs, remoting and other hosts, it writes PowerShell's own progress record. Progress written by compiled cmdlets (Copy-Item, Invoke-WebRequest) is still shown by PowerShell.

### [Write-ConsoleRule](Write-ConsoleRule.md)
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

### [Write-ConsoleStatus](Write-ConsoleStatus.md)
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

Each call replaces the line (Write-Color style pieces, colours, -Markup, -Style and -Role as in Write-ConsoleText). -Spinner, -ShowClock and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).

### [Write-ConsoleTable](Write-ConsoleTable.md)
Writes pipeline objects as a console table.

Same parameters as New-ConsoleTable; the table is written immediately in the detected output mode (live terminal, redirected stream or plain ASCII).

### [Write-ConsoleText](Write-ConsoleText.md)
Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.

The parameters and aliases of Write-Color (PSWriteColor) work the same way: -Text pieces get -Color and -BackGroundColor per piece (pieces beyond the colours use the first colour), -StartTab, -StartSpaces, -LinesBefore, -LinesAfter, -ShowTime with -DateTimeFormat, -NoNewLine, -HorizontalCenter, -LogFile with -LogTime, -LogRetry and -Encoding, and -NoConsoleOutput. ConsoleColor names render exactly as Write-Host shows them. Beyond Write-Color: -Markup, -Style, -Link, -Role, hex and RGB colours, -Gradient, -PadLeft/-PadRight/-PadCenter with -PadCharacter (measured in terminal cells, so emoji and CJK pad correctly), -Wrap with a hanging indent under the time and prefix, -SplitLines with the following lines aligned under the first, and -LogFormat Text, Csv or JsonLines with -LogSeparator, -Level and -Category. Differences: the default colour is the terminal's own (Write-Color uses White), and a new log file is UTF-8 without a byte order mark by default (Write-Color writes UTF-16); appending to an existing log keeps its encoding, so Write-Color's UTF-16 logs stay UTF-16. In transcripts each call is one line; in jobs and remoting the colours travel with the output; redirected output is plain text. There is deliberately no Write-Color or wc alias.

### [Write-ConsoleTree](Write-ConsoleTree.md)
Writes a console tree from New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Same parameters as New-ConsoleTree (nodes in a script block, hierarchical objects from the pipeline with -ChildrenProperty or -Children, or -Data hashtables); the tree is written immediately instead of returned.

### [Write-ConsoleWidget](Write-ConsoleWidget.md)
Writes console widgets once (tables, trees, panels, charts, dashboards).

Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.
