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

### [Assert-ConsoleOutput](Assert-ConsoleOutput.md)
Compares console output with a golden file (for Pester and other tests).

Renders a widget at a fixed width, glyph tier and culture (invariant by default), so the result does not depend on the machine, and compares the text with a golden file. The first run writes the file and fails so it can be reviewed; a difference fails with the first differing line, both versions and the column where they part. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite golden files after an intended change. Strings are compared as they are. In Pester the thrown error fails the test; see the README for a Should operator built on it.

### [Export-ConsoleFrame](Export-ConsoleFrame.md)
Exports a widget, a rendered frame or a live-session snapshot to HTML, SVG or text.

Pipe any New-Console* widget (or Format-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Other objects are shown as a table. Several widgets are stacked. Without -Path the document is written to the pipeline.

### [Export-ConsoleRecording](Export-ConsoleRecording.md)
Exports a dashboard recording to an HTML player, an animated SVG or asciicast.

Recordings (written by Start-ConsoleDashboard -RecordPath) hold updates, not pictures, so they are played into the same dashboard definition at a fixed size and frame rate. Html writes a self-contained page with a small player (play/pause, speed, scrubbing; add #t=30000 to the address to open at 30 seconds). Svg writes an animated image with CSS keyframes and no script, for READMEs. Asciicast writes asciicast v3 for asciinema and other players (output events only). Identical frames are merged; a long recording is sampled less often.

### [Format-ConsoleCode](Format-ConsoleCode.md)
Shows source text with syntax highlighting, line numbers and highlighted lines.

Highlights JSON, XML/HTML, YAML, PowerShell, C#, SQL, INI/TOML, CSV/TSV, diffs, log lines and Markdown, or plain text with -Language Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings). -Language Auto (the default) picks the language from the file extension, then from the content, and falls back to plain text. Text comes from -Path or from pipeline strings (joined as lines). The text is always literal: markup is not read and control characters are dropped. -LineNumbers adds a gutter, -Highlight marks lines (10, '10-14'), long lines are truncated with an ellipsis or wrapped with -Wrap, and output stops after -MaxLines lines with "+n more lines". A file is shown on a panel titled with its name (-Title changes it, -NoTitle leaves the panel out). Redirected output is plain text.

### [Format-ConsoleError](Format-ConsoleError.md)
Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.

Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.

### [Format-ConsoleJson](Format-ConsoleJson.md)
Shows JSON with syntax highlighting: JSON text, or objects converted with ConvertTo-Json.

A thin convenience over Format-ConsoleCode -Language Json. Strings are taken as JSON text (pipeline strings are joined, so Get-Content works); other objects are collected and converted with ConvertTo-Json -Depth (default 4) first.

### [Format-ConsoleList](Format-ConsoleList.md)
Formats pipeline objects as record views (labelled values) and writes them to the host.

Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Format-ConsoleTable. Redirected output is plain text.

### [Format-ConsoleTable](Format-ConsoleTable.md)
Formats pipeline objects as a width-aware table and writes it to the host.

Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards; a single object becomes a record view. Redirected output is plain text without escape codes.

### [Format-ConsoleTree](Format-ConsoleTree.md)
Shows the structure of pipeline objects as a tree (or an inspector view) and writes it to the host.

Works for any object: PSCustomObjects, .NET objects, hashtables and dictionaries, nested to -Depth levels. Collections list up to -MaxItems items followed by "+N more"; reference cycles and the depth limit are marked instead of recursing forever. -Include and -Exclude filter member names (wildcards). With -AsInspector the object is shown as a record view: nested objects as indented sections and uniform collections as nested tables. Redirected output is plain text.

### [Get-ConsoleCapability](Get-ConsoleCapability.md)
Shows what ConsoleMonster detected about the current terminal.

Returns the terminal profile: kind, mode (Interactive, Stream, Plain), colour depth, glyph tier, size, VT support, console font and the reasons behind each decision. -Refresh detects again (after a resize or font change).

### [Invoke-ConsolePester](Invoke-ConsolePester.md)
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.

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

### [New-ConsoleField](New-ConsoleField.md)
Creates a form field for New-ConsoleForm.

A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validate and the type's own checks) and the command-line parameter it supplies (-Parameter) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values).

### [New-ConsoleForm](New-ConsoleForm.md)
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back.

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

### [New-ConsoleMenu](New-ConsoleMenu.md)
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu.

### [New-ConsoleMenuItem](New-ConsoleMenuItem.md)
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -State can be scriptblocks, recomputed every time the menu draws; -Disabled returns why the item is unavailable.

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
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Style adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLog and Update-ConsoleLane, and with Write-ConsoleWidget. Without colour (NO_COLOR, redirected output) the text stays plain.

### [New-ConsoleTree](New-ConsoleTree.md)
Creates a console tree from nested hashtables.

Each key becomes a node. A nested hashtable (or ordered dictionary) becomes child nodes; an array becomes leaf nodes; any other value is shown as a muted note after the key. Use [ordered] to keep order.

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

### [Send-ConsoleEvent](Send-ConsoleEvent.md)
Sends a raw live event to any widget of a running dashboard.

The general form behind the Update-* cmdlets, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

### [Set-ConsoleTheme](Set-ConsoleTheme.md)
Selects the theme used by every ConsoleMonster widget.

Themes map semantic tokens (pass, fail, warn, muted, accent, border...) to colours for every colour tier. Dark suits dark terminals (the default), Light suits light backgrounds, HighContrast uses saturated primaries.

### [Set-ConsoleValue](Set-ConsoleValue.md)
Sets a value in a running dashboard: a key/value pair, a bar or a stacked-bar segment.

Sends a "set" update to the named widget. For a key/value list the value is shown as text; for bar charts and stacked bars it must be numeric.

### [Show-ConsoleGrid](Show-ConsoleGrid.md)
Shows pipeline objects in an interactive grid and returns the rows you choose.

An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Format-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list; without an interactive user the cmdlet fails with a clear error.

### [Show-ConsoleMenu](Show-ConsoleMenu.md)
Shows a menu until the user exits.

Shell is an app: a title bar with the breadcrumb, a sidebar (it folds into the breadcrumb in narrow windows), the content pane with the open item's list, form, page or review, and a footer generated from the keys that work right now. Classic is one centred menu; Inline is a list in the scrollback for scripts. Everywhere: Esc goes back, Ctrl+K (or :) opens the command palette, ? shows every key, Ctrl+Q leaves (asking first when a form has unsaved changes). Item actions run on the pipeline thread. Selections and form values survive navigation; -AppName stores presets, and with -RememberLastChoices the last choices.

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

### [Use-ConsoleProgress](Use-ConsoleProgress.md)
Runs a script and shows its Write-Progress calls as an inline progress area.

The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. Works on Windows PowerShell 5.1 and PowerShell 7.

### [Write-ConsoleException](Write-ConsoleException.md)
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

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

### [Write-ConsoleStatus](Write-ConsoleStatus.md)
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

Each call replaces the line (Write-Color style pieces, colours, -Markup, -Style and -Role as in Write-ConsoleText). -Spinner, -ShowClock and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).

### [Write-ConsoleTable](Write-ConsoleTable.md)
Writes pipeline objects as a console table.

Same parameters as New-ConsoleTable; the table is written immediately in the detected output mode (live terminal, redirected stream or plain ASCII).

### [Write-ConsoleText](Write-ConsoleText.md)
Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.

The parameters and aliases of Write-Color (PSWriteColor) work the same way, so most scripts migrate with a rename: -Text pieces get -Color and -BackGroundColor per piece (pieces beyond the colours use the first colour), -StartTab, -StartSpaces, -LinesBefore, -LinesAfter, -ShowTime with -DateTimeFormat, -NoNewLine, -HorizontalCenter, -LogFile with -LogTime, -LogRetry and -Encoding, and -NoConsoleOutput. ConsoleColor names render exactly as Write-Host shows them. Beyond Write-Color: -Markup, -Style, -Link, -Role, hex and RGB colours, -Gradient, -PadLeft/-PadRight/-PadCenter with -PadCharacter (measured in terminal cells, so emoji and CJK pad correctly), -Wrap with a hanging indent under the time and prefix, -SplitLines with the following lines aligned under the first, and -LogFormat Text, Csv or JsonLines with -LogSeparator, -Level and -Category. Differences: the default colour is the terminal's own (Write-Color uses White), and the log file is UTF-8 without a byte order mark by default (Write-Color writes UTF-16). In transcripts each call is one line; in jobs and remoting the colours travel with the output; redirected output is plain text. There is deliberately no Write-Color or wc alias.

### [Write-ConsoleTree](Write-ConsoleTree.md)
Writes nested hashtables as a console tree.

Same parameters as New-ConsoleTree; the tree is written immediately.

### [Write-ConsoleWidget](Write-ConsoleWidget.md)
Writes console widgets once (tables, trees, panels, charts, dashboards).

Renders each widget at the terminal width in the detected mode: VT colours and Unicode glyphs in a terminal, append-only lines when redirected, plain ASCII with CONSOLEMONSTER_MODE=plain. Override detection with CONSOLEMONSTER_COLOR, CONSOLEMONSTER_GLYPHS, CONSOLEMONSTER_MODE or NO_COLOR.
