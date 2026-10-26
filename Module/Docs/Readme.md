---
Module Name: ConsoleMonster
Module Guid: 7ef2133c-55a5-47e6-809e-6010f6e65e09
Download Help Link: https://github.com/EvotecIT/ConsoleMonster
Help Version: 2.0.2
Locale: en-US
---
# ConsoleMonster Module
## Description
Rich console output for PowerShell 5.1 and 7: tables, trees, panels, charts and live dashboards with thread-safe updates, captured script output and graceful fallback to plain text.

## ConsoleMonster Cmdlets
### [Add-ConsoleItem](Add-ConsoleItem.md)
Adds items to a console widget made earlier: tree nodes, pairs, status items, bars, series, log lines, lanes or values.

The PowerShell way to grow a widget after it was created, instead of calling its .NET methods. Pipe the widget in and give the items as a script block or values; they are the same item commands the widget's own script block takes: Label/Children records for New-ConsoleTree, Key/Value records for New-ConsoleKeyValue, Key/Label/Status records for New-ConsoleStatusList and New-ConsolePhases, Label/Value records for New-ConsoleBarChart, Key/Values records for New-ConsoleLineChart, Message/Level records for New-ConsoleLogRegion and Key/Label records for New-ConsoleLanes. Sparklines and histograms take numbers, a cell map takes status words, and a live table takes objects or hashtables with a property per column. -PassThru writes the widget on, so it can go straight to Show-Console. For a widget inside a running dashboard, use Update-Console or Write-ConsoleLogEntry instead.

### [Add-ConsoleRecordingMarker](Add-ConsoleRecordingMarker.md)
Adds a chapter or an export hold to a recorded terminal session.

Inside a recorded child shell, sends an OSC marker to the parent recorder. The marker becomes an asciicast v2 event and an HTML chapter. In-process fallback records the chapter directly.

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

Serializes input through the core or bundled Markdown add-on as Text (default), Markdown or Html and copies the source as Unicode text. Markdown uses GFM tables, property/value lists and nested tree bullets, including the shared value presenters and totals. Text is plain terminal output. A rendered frame or live-session snapshot retains a fenced fallback. Windows uses its native clipboard, macOS uses pbcopy, and Linux uses wl-copy (Wayland) or xclip (X11). SSH, a missing desktop/provider or a failing provider produces a non-terminating error; use -ErrorAction Stop to catch it. No output is written unless -PassThru succeeds. -WhatIf exports without accessing the clipboard.

### [Disable-ConsoleFormatting](Disable-ConsoleFormatting.md)
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.

Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress function is restored, and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.

### [Enable-ConsoleFormatting](Enable-ConsoleFormatting.md)
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), JSON/REST and CSV records (PSCustomObject), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), PowerShell 7 ping replies, .NET DNS entries and IP addresses, Windows network objects, event log records (Get-WinEvent), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; JSON/CSV records show at most 50 rows and 24 fields with omission hints and nested summaries; numeric and date-like CSV strings align right without conversion. Commands group by module from 12 rows and show alias targets and command-type chips; modules group repeated names and show versions, shortened paths and exported command counts; history shows id, start time, duration, status and highlighted command text. Windows network views register only on Windows; ping and .NET DNS/IP views work across platforms. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. A custom Out-Default is kept unless you pass -Force for a feature that uses it. Streams, ReadLine, GridView and Prompt do not replace Out-Default.

### [Export-ConsoleDemo](Export-ConsoleDemo.md)
Exports one embedded demo as a copyable PowerShell script.

Use Get-ConsoleDemo to list slugs. Saves the canonical script as UTF-8 with a BOM for PowerShell 5.1. The destination folder must exist. Existing files are preserved unless Force is explicit.

### [Export-ConsoleFrame](Export-ConsoleFrame.md)
Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Show-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Markdown uses the bundled ConsoleMonster.Markdown add-on lazily: tables, records and trees retain their semantics; FencedText preserves the terminal layout. Other objects use the automatic view. Several widgets are stacked. Without -Path the document is written to the pipeline.

### [Export-ConsoleRecording](Export-ConsoleRecording.md)
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations without an external executable. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.

### [Export-ConsoleTheme](Export-ConsoleTheme.md)
Exports the current ConsoleMonster theme as a starting file for a user or team.

Writes every semantic token, foreground/background colour, explicit fallback tier and decoration as JSON or restricted PowerShell data. The current theme is unchanged. The file extension selects the format; the parent directory must already exist. JSON is UTF-8 without a BOM; PSD1 uses a UTF-8 BOM for Windows PowerShell 5.1.

### [Format-ConsoleCommand](Format-ConsoleCommand.md)
Builds a copyable command line from named values without executing it.

Formats strings, numbers, switches and lists through the same quoting rules as forms. Use an ordered dictionary to retain argument order. Null, empty strings and false switches are omitted. The result is a plain string, suitable for a review's equivalent command.

### [Get-ConsoleCapability](Get-ConsoleCapability.md)
Shows what ConsoleMonster detected about the current terminal.

Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).

### [Get-ConsoleDemo](Get-ConsoleDemo.md)
Lists the copyable ConsoleMonster gallery demos .

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. Use Export-ConsoleDemo to save a script.

### [Invoke-ConsoleDashboard](Invoke-ConsoleDashboard.md)
Runs a console dashboard live while a script does the work.

The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession, Update-Console and Write-ConsoleLogEntry update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.

### [Invoke-ConsolePester](Invoke-ConsolePester.md)
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Invoke-ConsolePresentation](Invoke-ConsolePresentation.md)
Types a scripted PowerShell demo into a real terminal and records it.

Runs plain PowerShell with optional chapter, export pace, export hold, recording pause, setup, type-only, narration, WPM and output-wait directives. Uses OSC 133 readiness marks independent of visible prompt text and operating system. Character jitter uses a fixed seed; use fixed data and environment for repeatable output.

### [Invoke-ConsoleRecording](Invoke-ConsoleRecording.md)
Records a script or an interactive child shell and saves the result.

Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

### [Invoke-ConsoleRun](Invoke-ConsoleRun.md)
Runs a script block for every input item, optionally in parallel, with a live view of the run.

$_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Label sets each item's label: a property name or a script block. -RetryCount runs a failed or timed-out item again (after -RetryDelaySeconds); only the last attempt's output and errors are kept, the item's cell shows the retry and the summary counts retried items. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.

### [New-ConsoleActivityList](New-ConsoleActivityList.md)
Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.

Each activity has an id, a title, an optional status, a percentage (or none, which spins) and an optional parent it is indented under; finished activities disappear, and an empty list takes no space. Update it with Update-Console from the script of a running dashboard or menu run. -MaxVisible caps the rows (more are summed up in one line). The default name, $progress, is also where Write-Progress goes in Invoke-ConsoleDashboard, so a list with that name shows the script's progress records; give another name for activities of your own.

### [New-ConsoleAlign](New-ConsoleAlign.md)
Aligns console widgets left, centre or right, and top, middle or bottom, inside their space.

The content keeps its measured size and is placed within the space the parent gives it. Horizontal alignment uses the full width; vertical alignment applies when the parent gives more rows than the content needs (-Height or -Fill). Widgets that already use the full width, such as progress bars, only move vertically.

### [New-ConsoleBinding](New-ConsoleBinding.md)
Creates a display rebuilt from application state on the owning PowerShell input thread.

Content receives param($State) and injected $State as a native hashtable. Emit widgets or text using the ordinary layout commands. Input controls must remain in the stable outer layout. Added or assigned state keys write back through AppState; bindings should normally only read state.

### [New-ConsoleButton](New-ConsoleButton.md)
Creates a named action button for Show-ConsoleApp.

Emits a stable core Button widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleCalendar](New-ConsoleCalendar.md)
Creates a month or year calendar with dated operational outcomes.

Returns a passive widget. Pipeline rows provide Date, Status and Label properties, with explicit property overrides. Duplicate dates retain every row; the displayed status uses Fail, Warn, Running, Pending, Pass, Info, Skip precedence. Dates retain calendar components without time-zone conversion. ISO week labels describe the Monday in each row, including with a Sunday-first display.

### [New-ConsoleCellMap](New-ConsoleCellMap.md)
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. -Columns and -RowLabels turn it into a labelled grid (one row per server, one column per check) with one legend below, which -LegendLines lets wrap. To add cells later, pipe the map to Add-ConsoleItem; in a live dashboard, update cells with Update-Console.

### [New-ConsoleChart](New-ConsoleChart.md)
Creates one numeric chart representation from records or samples.

Type selects Bar, Stacked, Line, Scatter, Histogram or Sparkline. The shared C# Chart factory rejects options that do not apply to the selected kind. InputObject accepts chart records (Label and Value; Key, Label, Values and Role for series); Content emits the same records. For Bar, Histogram and Sparkline, -Property picks the number from ordinary objects and -Label (Bar only, default Name) their label, as Show-Console -View Chart does. Use Update-Console for live changes.

### [New-ConsoleColumnFormat](New-ConsoleColumnFormat.md)
Creates shared numeric or detail-column layout settings.

Pass the result to a widget's CountColumn, PercentColumn, ValueColumn or DetailColumn parameter. MinWidth is measured in cells; ReserveDigits reserves room for numbers before they grow. Widths remain stable during live updates unless -AllowShrink is specified.

### [New-ConsoleColumns](New-ConsoleColumns.md)
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.

### [New-ConsoleCommand](New-ConsoleCommand.md)
Defines an in-app action for buttons, palettes and grid rows.

Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 requests cancellation while the app stays open. After a two-second grace period the app becomes available even when a native call is still stopping. That command cannot start again until its previous runspace exits; other commands remain available. Cancellation cannot undo effects already performed by an external operation.

### [New-ConsoleDashboard](New-ConsoleDashboard.md)
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Invoke-ConsoleDashboard to run it live, or to Show-Console to print it once.

### [New-ConsoleDataGrid](New-ConsoleDataGrid.md)
Creates a named, embeddable data grid for Show-ConsoleApp.

The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control.

### [New-ConsoleDatePicker](New-ConsoleDatePicker.md)
Creates a persistent civil-date or inclusive range picker.

Uses Gregorian arithmetic with culture-aware labels. Arrows move the cursor, PageUp/PageDown change month, Ctrl+PageUp/PageDown change year, and Enter commits. In range mode Space anchors a range and Shift+arrows extend it. Bounds and unavailable dates reject a whole invalid range.

### [New-ConsoleDialog](New-ConsoleDialog.md)
Creates a typed modal form for the running app's dialog service.

Construction does not read input. AppDialogs.Show hosts the form on the owning application thread and returns DialogResult of AppState through its completion callback.

### [New-ConsoleField](New-ConsoleField.md)
Creates a form field for New-ConsoleForm.

A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validator and the type's own checks) and the command-line parameter it supplies (-ParameterName) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values). Choices are built from any objects: -Display, -GroupBy, -Describe, -Badge, -DisabledReason and -Selected read each object (a property name, or a scriptblock for choices given with -Choices; loaded choices take property names), and -CommandValue picks the text the equivalent command shows and the field returns.

### [New-ConsoleFlow](New-ConsoleFlow.md)
Builds a native flow strip of nodes, labelled edges and one side branch.

Plain strings are steps, connected in order. Emit nodes first, then edges and an optional branch. Pipeline rows use Kind (Node, Edge or Branch), Key, Label, Status, and the edge/branch From and To fields. Kind can be left out for nodes and for edges with From and To; a node's Key defaults to its Label and an edge's Key to From->To. Labels are literal. The strip becomes vertical in a narrow terminal.

### [New-ConsoleFooterBar](New-ConsoleFooterBar.md)
Creates a reusable footer with key hints, messages and linked segments.

Hint maps key names to their labels. Segment accepts FooterSegment values or hashtables with Text, Link and Role. The footer is a widget and does not read input or start a session.

### [New-ConsoleForm](New-ConsoleForm.md)
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Use -PageDescriptions to give each page its own sentence under the title. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back. Presets the form comes with (New-ConsoleForm -Preset, in the scriptblock or -Preset) are listed before the saved ones.

### [New-ConsoleHeaderBar](New-ConsoleHeaderBar.md)
Creates a header with a title, breadcrumbs, detail and optional elapsed time.

Returns a reusable HeaderBar widget without opening a session. Status is a semantic outcome; Detail is the text beside it. HeaderRight supplies independent right-aligned content.

### [New-ConsoleKeyValue](New-ConsoleKeyValue.md)
Creates an aligned key/value list from a hashtable, an object's properties or Key/Value records pairs.

Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of Key/Value records calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Update-Console.

### [New-ConsoleLanes](New-ConsoleLanes.md)
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane (names sharing one -Total), with a script block of Key/Label records calls (each with its own total, progress, status and counts), from the pipeline, or appear on their first Update-Console in a live dashboard. To add lanes later, pipe the widget to Add-ConsoleItem.

### [New-ConsoleLayoutGrid](New-ConsoleLayoutGrid.md)
Lays console widgets out in an equal-width grid.

Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

### [New-ConsoleLogo](New-ConsoleLogo.md)
Creates banner text drawn with a pixel font and a colour gradient.

Letters A-Z, digits and - . : ! / _ are drawn with half blocks (four rows tall) on modern and Consolas fonts, and with # in ASCII mode. Colours accept names, #hex or rgb().

### [New-ConsoleLogRegion](New-ConsoleLogRegion.md)
Creates a log region: the newest lines of a log, bottom-aligned.

Printed once, the lines come from a script block of Message/Level records calls (each with a level and a time) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Invoke-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLogEntry. To add lines to a log made earlier, pipe it to Add-ConsoleItem.

### [New-ConsoleMenu](New-ConsoleMenu.md)
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.

### [New-ConsoleMenuItem](New-ConsoleMenuItem.md)
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -Detail can be scriptblocks, recomputed every time the menu draws; -DisabledReason returns why the item is unavailable.

### [New-ConsoleMenuRunResult](New-ConsoleMenuRunResult.md)
Creates the result a menu run returns: a title, a status, a summary, figures and next actions.

Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -FigureStatus colours the figures that need attention (Pass, Warn, Fail by label). -Status takes the shared status vocabulary (default Pass). The time shown with the result is the run's time on its session clock (real time, or a simulated clock given to the run); -ElapsedSeconds overrides it.

### [New-ConsolePadding](New-ConsolePadding.md)
Adds empty space around console widgets.

-Padding takes one, two or four numbers of cells: all sides; left and right, then top and bottom; or left, top, right and bottom (the order of the .NET Padding widget). Strings in the script block are markup text. Use New-ConsoleAlign to place content inside its space instead.

### [New-ConsolePanel](New-ConsolePanel.md)
Creates a bordered panel with a title around other console widgets.

-Content is a scriptblock, a widget, text or any objects (objects are shown as a record or a table; several pieces are stacked vertically). Borders follow the detected glyph tier: rounded on modern fonts, single lines on Consolas, +-| in ASCII mode. Inside New-ConsoleColumns, panels share the row height.

### [New-ConsolePhases](New-ConsolePhases.md)
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

Phases come from -Phase (names, all pending) or from a script block of Key/Label/Status records calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-Console, which also measures their duration.

### [New-ConsoleProgress](New-ConsoleProgress.md)
Creates a progress bar or an indeterminate activity indicator.

The default bar represents a known total. Indeterminate creates a spinner. Update either through Update-Console. Structural parameter sets reject bar-only options on a spinner and spinner-only options on a bar; the shared C# Progress factory validates the same contract.

### [New-ConsoleRegion](New-ConsoleRegion.md)
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

### [New-ConsoleRows](New-ConsoleRows.md)
Stacks console widgets vertically.

Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

### [New-ConsoleRule](New-ConsoleRule.md)
Creates a horizontal rule, optionally with a title.

The line uses the border colour and the glyph tier's line character.

### [New-ConsoleScrollView](New-ConsoleScrollView.md)
Creates a clipped vertical viewport for a small widget tree inside an app.

Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.

### [New-ConsoleSelect](New-ConsoleSelect.md)
Creates a named select input for Show-ConsoleApp.

Choices use the shared prompt conversion and retain original objects as values. Keep the input in the stable outer layout. Disabled choices cannot be selected.

### [New-ConsoleSimulatedClock](New-ConsoleSimulatedClock.md)
Creates a run clock (ConsoleMonster.Live.SimulatedClock) the script moves forward, for simulated runs whose screen shows simulated time.

A live session reads the clock for the header clock, the summary bar's elapsed time and time left, the times of log lines and activities, and the run result's duration, so a demo or a replay of logged work shows its own time instead of the wall clock. Give it to a menu run as the third argument of $MenuContext.Run($dashboard, { ... }, $clock), or to Invoke-ConsoleDashboard -Clock. In the script, $clock.Wait(seconds) and $clock.WaitUntil(seconds) move it forward in small steps, sleeping 1/-Speed of each simulated second (never at speed 0, which runs at once); $clock.Advance(seconds) jumps without waiting and $clock.Hold(seconds) waits real time with the clock standing still. $clock.Elapsed is the time on the clock. Once the dashboard is stopped (Ctrl+C) waits stop sleeping, so the script reaches its end at once.

### [New-ConsoleStack](New-ConsoleStack.md)
Layers console widgets on top of each other in the same space.

Each widget the scriptblock emits is a layer, bottom first; later layers paint over earlier ones and replace only the cells they write. Combine with New-ConsoleAlign to place an overlay, such as a badge in a corner of a chart. To place widgets one below another, use New-ConsoleRows.

### [New-ConsoleStatusList](New-ConsoleStatusList.md)
Creates a checklist of items with status markers.

Items come from a script block of Key/Label/Status records calls (each with its own status and detail), from the pipeline, from -InputObject (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-Console.

### [New-ConsoleSummaryBar](New-ConsoleSummaryBar.md)
Creates a summary bar: one line with progress, done of total, the time left and result counters.

The line Invoke-ConsoleRun ends its view with, as a widget of its own: a thin bar, done of total, the percentage, the time left (estimated from -ElapsedSeconds and the share done) and counters in a fixed order with zeros dimmed rather than hidden. -Counter maps labels to counts; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour. Without -Total only the count done shows.

### [New-ConsoleTab](New-ConsoleTab.md)
Creates a retained tab page for New-ConsoleTabs.

Collects the page widgets once and preserves their input values when another tab is selected. Pass the page to New-ConsoleTabs to mount it in a full-screen application.

### [New-ConsoleTable](New-ConsoleTable.md)
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Property descriptors accept presenter names or configured C# ICellPresenter instances. Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

### [New-ConsoleTabs](New-ConsoleTabs.md)
Creates retained tabs with keyboard and pointer navigation.

Combines pages from New-ConsoleTab in display order. Only the selected page receives input; switching pages retains each page's values and focus for the next visit.

### [New-ConsoleText](New-ConsoleText.md)
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Decoration adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLogEntry and Update-Console, and with Show-Console. Without colour (NO_COLOR, redirected output) the text stays plain.

### [New-ConsoleTextInput](New-ConsoleTextInput.md)
Creates a named single-line text input for Show-ConsoleApp.

Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleToggle](New-ConsoleToggle.md)
Creates a named Boolean toggle for Show-ConsoleApp.

Emits a stable core Toggle widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

### [New-ConsoleTree](New-ConsoleTree.md)
Creates a console tree from nested Label/Children records calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of Label/Children records calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -LabelProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -InputObject maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

### [New-ConsoleTreeSelect](New-ConsoleTreeSelect.md)
Creates an embeddable, lazy object tree for Show-ConsoleApp.

Uses the same member adapters, paths, cycle guards and expansion budget as Show-ConsoleObject. Arrows expand and collapse; Enter drills into the selected subtree.

### [Out-ConsoleDefault](Out-ConsoleDefault.md)
Shows objects of registered types through ConsoleMonster and passes everything else to PowerShell's Out-Default.

This is what Enable-ConsoleFormatting puts behind Out-Default. Consecutive objects of registered types (Register-ConsoleView; after Enable-ConsoleFormatting also files and folders, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries) are collected and shown as one ConsoleMonster view; any other object, format data from Format-*, errors and strings go to PowerShell's Out-Default unchanged. It only takes over when the output is displayed on a real local console: redirected output, jobs, remoting and other hosts get PowerShell's default output (-Force renders anyway). The objects themselves are never changed.

### [Out-ConsoleFormatCommand](Out-ConsoleFormatCommand.md)
Renders native PowerShell formatting records through ConsoleMonster.

The host-only sink used by Enable-ConsoleFormatting -Feature FormatCommands. PowerShell still resolves properties, calculated expressions, grouping and named views. Unrecognized formatting records are passed to the native Out-Default. Use -AsWidget to export the same view with Export-ConsoleFrame.

### [Read-ConsoleConfirm](Read-ConsoleConfirm.md)
Asks a yes/no question and returns $true or $false.

Shows the question with the answers inline (the default focused); Y and N answer at once, arrows move, Enter takes the focused answer and Esc answers no. With -TypedConfirmation the user must type a text (a server name, a domain) before a destructive action; Esc then returns $false. In remoting and the ISE the host asks (PromptForChoice); without a user the default is returned.

### [Read-ConsoleDate](Read-ConsoleDate.md)
Asks for one civil date or inclusive date range.

Returns one DateTime at midnight with unspecified kind, or one DateRange with -Range. Escape follows the normal prompt cancellation policy. Line and accessible modes accept yyyy-MM-dd and culture-aware dates; non-interactive mode requires a valid explicit default.

### [Read-ConsoleForm](Read-ConsoleForm.md)
Shows a form or wizard and returns the answers.

The form opens on the alternate screen (or in the scrollback with -Inline). Fields are edited in place; choice fields open a filterable list; dependent options load with a spinner. The review page shows every answer and the equivalent command (copy it, save the answers as a named preset, or run). The answers come back as one object with a property per visible field (secrets as SecureString unless -AsPlainText); -PassThru returns the full result (Submitted, Values, CommandLine) instead. Cancelling returns nothing. -AppName stores presets, and with -RememberLastChoices the last answers, per user under %APPDATA%\ConsoleMonster\<app>. In remoting and the ISE the fields are asked one by one; without a user the defaults are used, or an error names the first field that needs a value.

### [Read-ConsoleNumber](Read-ConsoleNumber.md)
Asks for a number.

Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).

### [Read-ConsolePath](Read-ConsolePath.md)
Asks for a file or folder path, with completion.

Matching entries of the typed folder are listed under the box (folders first); Tab completes the common part or the focused entry, Up and Down move through them. -Kind File or Folder and -MustExist are checked on Enter. Returns the path as typed (environment variables and ~ are expanded only when checking).

### [Read-ConsoleSelect](Read-ConsoleSelect.md)
Asks the user to pick one of the choices and returns the chosen object.

Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-DisabledReason) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.

### [Read-ConsoleText](Read-ConsoleText.md)
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validator check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

### [Register-ConsoleView](Register-ConsoleView.md)
Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Show-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). Unregister-ConsoleView removes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.

### [Send-ConsoleEvent](Send-ConsoleEvent.md)
Sends a raw live event to any widget of a running dashboard.

The general form behind Update-Console, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

### [Set-ConsoleFormValue](Set-ConsoleFormValue.md)
Sets field values of a form, whether or not it is shown.

Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Values maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Values also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.

### [Set-ConsoleTheme](Set-ConsoleTheme.md)
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

### [Show-Console](Show-Console.md)
Displays pipeline input in its automatic or explicit representation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

### [Show-ConsoleApp](Show-ConsoleApp.md)
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -StaticSnapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

### [Show-ConsoleChanges](Show-ConsoleChanges.md)
Shows the changes an action would make and asks before making them: the what-if pattern.

Compares -ReferenceObject with -DifferenceObject (as Compare-ConsoleObject does), shows the preview, then asks for confirmation and returns $true or $false; act only on $true. Changes that remove objects need the number of removals typed (or the -TypedConfirmation text). It honours the caller's -WhatIf: the preview is shown and $false returned without asking. -Force (or -Confirm:$false) skips the question and returns $true when there are changes. Use it inside your own functions: if (Show-ConsoleChanges -ReferenceObject $current -DifferenceObject $planned -Property Name) { apply }.

### [Show-ConsoleDemo](Show-ConsoleDemo.md)
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

Executes the same self-contained PowerShell source offered by Export-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot selects a recorded state using scripted input or queued live updates controlled by the runner. The copied source has no capture parameters. Demo output and errors follow the normal PowerShell streams.

### [Show-ConsoleGrid](Show-ConsoleGrid.md)
Shows pipeline objects in an interactive grid and returns the rows you choose.

An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Show-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list. Without an interactive user (redirected output, a job, CI) the rows are printed as a table with a warning and nothing is returned; -Mode None fails with an error instead.

### [Show-ConsoleHelp](Show-ConsoleHelp.md)
Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.

Reads the help PowerShell has for the command (Get-Help -Full): cmdlet help files, comment-based help of functions and scripts, or the help PowerShell generates when there is none. It shows the name and synopsis, the syntax of each parameter set (parameter names, value types and optional parts told apart), the description, a parameter table (type, required, position, pipeline input, default, description), the examples with highlighted PowerShell code, inputs, outputs, notes and related links. Piped on (| more, | Out-Host -Paging, | Out-File) it writes plain lines, so pagers and files get readable text. About topics use the shared Markdown reader, retaining literal HTML and safe link handling. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Show-ConsoleList](Show-ConsoleList.md)
Displays the list representation through the shared Show-Console implementation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

### [Show-ConsoleMenu](Show-ConsoleMenu.md)
Shows a menu until the user exits.

Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.

### [Show-ConsoleObject](Show-ConsoleObject.md)
Explores an object's members with the keyboard.

Arrows move, Right and Left expand and collapse, Enter drills into a member, Backspace returns to its parent, and / searches displayed names and values. Ctrl+Enter or F10 accepts the selected node; Esc cancels. Nested values are read lazily and cycles, depth and collection sizes are bounded. In redirected, CI, line and non-interactive sessions, a static inspector is printed and no selection is returned. Objects parsed by ConvertFrom-Json use the same PowerShell member adapter as other objects.

### [Show-ConsoleOverview](Show-ConsoleOverview.md)
Shows a compact, read-only summary of the local host.

Collects CPU, memory, local fixed disks, network addresses and safely available services, update/reboot and security observations. Unavailable panels are hidden. Slow sections show a pending marker after the caller budget; each probe receives a two-second cooperative timeout. At 80 columns or more panels use two columns and at most 24 rows. Below 80 columns panels stack at their natural height, preserving volumes and login rows; terminal scrolling is allowed. No elevation, child processes, remote access or system changes are used. Security data requires existing read access. Linux update and security support depends on local update-notifier and auth.log facilities; macOS optional service, update and security panels are hidden. Register application sections through OverviewSectionRegistry.Default.Register. Custom providers must honor cancellation and release their resources.

### [Show-ConsolePresentation](Show-ConsolePresentation.md)
Presents a scripted demo one command at a time in a real shell.

Enter types and runs; Space types without running; b moves back without undoing side effects; s skips; q quits. The terminal title shows the step counter. Optional -RecordingPath saves the same child output as asciicast; without it temporary capture files are removed on exit. Stop or cancellation terminates only the owned child and restores the title.

### [Show-ConsoleReplay](Show-ConsoleReplay.md)
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

Plays a recording (written by Invoke-ConsoleDashboard -RecordPath or -AutoRecord) into the same dashboard and lets you move through it: space plays and pauses, left and right step frame by frame, + and - change the speed, PgUp and PgDn scrub, Home and End jump to the ends, n/p jump to the next or previous marker, f/F to failures, w/W to warnings, l/L to log entries, and / searches the log text (Enter for the next match). Markers are added while recording with Send-ConsoleEvent -WidgetId '$marker' -Operation mark -Text 'label'. Without -Path the newest automatic recording is played. Without an interactive console the markers are printed as lines.

### [Show-ConsoleStatusLine](Show-ConsoleStatusLine.md)
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

Each call replaces the line (Write-Color style pieces, colours, -Markup, -Decoration and -Role as in Write-ConsoleText). -Spinner, -ShowTimestamp and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).

### [Show-ConsoleTable](Show-ConsoleTable.md)
Displays the table representation through the shared Show-Console implementation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

### [Start-ConsoleRun](Start-ConsoleRun.md)
Starts a bounded run dashboard for work scheduled by your script.

Declares the queued items and returns a LiveRun handle. Use Update-Console as workers start or finish and Complete-ConsoleRun in a finally block. Finished items stream above the live dashboard; failures and warnings remain pinned. The dashboard uses at most 40 percent of terminal height by default. CI and redirected hosts use append-only lines with periodic totals. This command does not execute work or change its results or exit code. Name, Target and Group accept property names or script blocks evaluated once, before the session starts. Interactive keys are opt-in: p pauses the log, f filters it to problems, g toggles groups, and ? shows help.

### [Stop-ConsoleRecording](Stop-ConsoleRecording.md)
Stops recording from inside the recorded child shell.

Sends a private terminal control to the parent recorder, which finalizes local files and closes this recorded child terminal. Run it inside a Invoke-ConsoleRecording -Interactive session.

### [Unregister-ConsoleView](Unregister-ConsoleView.md)
Removes registered ConsoleMonster views and value shapers.

Names the registered types directly or reads their names from a validated data-only .consoleviews.psd1 file. Does not remove or execute the file.

### [Update-Console](Update-Console.md)
Updates a live widget or a caller-managed run through one validated operation.

WidgetId is position zero. Fields are checked against the resolved widget before any events are posted. Structural sets separate table rows, cell batches, content replacement and run items. Unknown targets and unsupported fields are terminating errors.

### [Use-ConsoleProgress](Use-ConsoleProgress.md)
Runs a script and shows its Write-Progress calls as an inline progress area.

The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. If another runspace owns the terminal's progress area, the script also runs with PowerShell's native progress. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Use-ConsoleTarget](Use-ConsoleTarget.md)
Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.

Inside the script block every ConsoleMonster command (Write-ConsoleText, Show-ConsoleTable, Write-ConsoleText -Markup, live dashboards, prompts) writes to the target instead of the console. With -Path the output goes to a UTF-8 file, with -Writer to a TextWriter, with -Target to a target object (ConsoleMonster.Output.ITerminalTarget); with none of them it is returned as one string. -Width, -Glyphs and -ColorDepth lay the output out for the file or string; prompts there have nobody to answer and use their defaults. The script block's own output is passed through.

### [Wait-ConsoleDashboard](Wait-ConsoleDashboard.md)
Pauses dashboard work, or captures its next checkpoint during a recording.

Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.

### [Watch-Console](Watch-Console.md)
Runs a script block again and again and shows its latest output as a live view.

Like watch: every -PollIntervalSeconds seconds the script block runs and its output replaces the previous one, shaped as Show-ConsoleTable and the other commands shape it (objects as a table, one dictionary as a record, text as it is). The heading shows the interval, -Title, the run number and the time. -Differences highlights the cells that changed since the previous run. A run that fails shows its error and the watch goes on. Warnings, errors and Write-Host text from the script block are shown under its output in the view (an error marks the run in the heading) instead of interrupting it. Ctrl+C ends the watch and the last frame stays in the scrollback; -Count ends it after that many runs. Piped into another command (Out-File, Out-String), or without an interactive console (redirected output, CI, a job), every run is printed instead, so use -Count there; assigning the result to a variable at the prompt does not capture the live view, so pipe it instead.

### [Watch-ConsoleLog](Watch-ConsoleLog.md)
Follows a log file or a local Windows event log with a live, scrollable log view.

File lines are decoded incrementally and followed across observed truncation and rotation. Event logs use Get-WinEvent on Windows. The view follows new entries; scrolling pauses following and End resumes it. Filter and Highlight are literal case-insensitive substrings. Piped, redirected, CI and -NoLive output is ordinary literal text and never reads keyboard input. Count is source polls, as in Watch-Console: one takes a snapshot, zero follows until q, Escape or Ctrl+C. Capacity bounds retained history. The default interval is 0.25 seconds.

### [Write-ConsoleLogEntry](Write-ConsoleLogEntry.md)
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

### [Write-ConsoleText](Write-ConsoleText.md)
Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.

Writes literal text pieces with Color and BackgroundColor, optional Role and Decoration, links, gradients and terminal-cell padding. ShowTimestamp adds a wall-clock timestamp formatted with TimestampFormat. NoNewLine continues the current line. LogFile records text, CSV or JSON Lines; LogTimestamp and LogFormat control that destination independently. Redirected output is plain text, and jobs and remoting retain the structured rendering. New log files use UTF-8 without a byte order mark; existing logs keep their encoding.
