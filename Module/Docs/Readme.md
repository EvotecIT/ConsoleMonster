---
Module Name: ConsoleMonster
Module Guid: 7ef2133c-55a5-47e6-809e-6010f6e65e09
Download Help Link: https://consolemonster.dev/
Help Version: 2.4.0
Locale: en-US
---
# ConsoleMonster Module
## Description
Readable output for the commands you already run, and tables, trees, charts, live runs, dashboards, menus and forms when you need more. One engine for Windows PowerShell 5.1, PowerShell 7 and .NET, with no extra modules to install and plain text when output is redirected.

## ConsoleMonster Cmdlets
### [Add-ConsoleJobSummary](Add-ConsoleJobSummary.md)
Adds widgets or objects as Markdown to the CI job summary.

### [Add-ConsoleRecordingMarker](Add-ConsoleRecordingMarker.md)
Adds a chapter or an export hold to a recorded terminal session.

### [Assert-ConsoleOutput](Assert-ConsoleOutput.md)
Compares console output with a golden file (for Pester and other tests).

### [Compare-ConsoleObject](Compare-ConsoleObject.md)
Compares object sets or shows property drift across a baseline matrix.

### [Compare-ConsoleText](Compare-ConsoleText.md)
Compares text or files with numbered lines, highlighted words and collapsed unchanged regions.

### [Complete-ConsoleRun](Complete-ConsoleRun.md)
Finishes a run dashboard and prints its full summary into scrollback.

### [ConvertTo-ConsoleView](ConvertTo-ConsoleView.md)
Drafts a reusable console view from sample objects.

### [Disable-ConsoleFormatting](Disable-ConsoleFormatting.md)
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.

### [Enable-ConsoleFormatting](Enable-ConsoleFormatting.md)
Enables the everyday console views, native format commands and theme colours.

### [Export-ConsoleFrame](Export-ConsoleFrame.md)
Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.

### [Export-ConsoleRecording](Export-ConsoleRecording.md)
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

### [Export-ConsoleTheme](Export-ConsoleTheme.md)
Exports the current ConsoleMonster theme as a starting file for a user or team.

### [Get-ConsoleCapability](Get-ConsoleCapability.md)
Shows what ConsoleMonster detected about the current terminal.

### [Get-ConsoleDemo](Get-ConsoleDemo.md)
Lists gallery demos and returns their complete PowerShell source.

### [Get-ConsoleFormatting](Get-ConsoleFormatting.md)
Shows the formatting features enabled in this runspace.

### [Get-ConsoleSetting](Get-ConsoleSetting.md)
Shows ConsoleMonster's settings: the value of each and where it came from.

### [Import-ConsoleTheme](Import-ConsoleTheme.md)
Imports a terminal colour scheme as a version 3 ConsoleMonster theme.

### [Invoke-ConsoleDashboard](Invoke-ConsoleDashboard.md)
Runs a console dashboard live while a script does the work.

### [Invoke-ConsoleHeadless](Invoke-ConsoleHeadless.md)
Runs a script (usually an app) on a headless terminal with scripted input and returns the screens it drew.

### [Invoke-ConsolePester](Invoke-ConsolePester.md)
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

### [Invoke-ConsoleRecording](Invoke-ConsoleRecording.md)
Records a script, an app on a headless terminal, or an interactive child shell and saves the result.

### [Invoke-ConsoleRun](Invoke-ConsoleRun.md)
Runs a script block for every input item, optionally in parallel, with a live view of the run.

### [New-ConsoleAccordion](New-ConsoleAccordion.md)
Creates an accordion for Show-ConsoleApp: collapsible sections that keep one open.

### [New-ConsoleActivityList](New-ConsoleActivityList.md)
Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.

### [New-ConsoleBinding](New-ConsoleBinding.md)
Creates a display rebuilt from application state on the owning PowerShell input thread.

### [New-ConsoleBreadcrumb](New-ConsoleBreadcrumb.md)
Creates a breadcrumb for Show-ConsoleApp: a path such as Settings › Network › Proxy, its own or that of an outline's current entry.

### [New-ConsoleButton](New-ConsoleButton.md)
Creates a named action button for Show-ConsoleApp.

### [New-ConsoleCalendar](New-ConsoleCalendar.md)
Creates a month or year calendar with dated operational outcomes.

### [New-ConsoleCellMap](New-ConsoleCellMap.md)
Creates a cell map: one status symbol per item of a run, with a legend of counts.

### [New-ConsoleChart](New-ConsoleChart.md)
Creates one numeric chart representation from records or samples.

### [New-ConsoleChartView](New-ConsoleChartView.md)
Makes a line chart explorable inside Show-ConsoleApp.

### [New-ConsoleCollapsible](New-ConsoleCollapsible.md)
Creates a collapsible section for Show-ConsoleApp.

### [New-ConsoleColumns](New-ConsoleColumns.md)
Places console widgets side by side.

### [New-ConsoleCommand](New-ConsoleCommand.md)
Defines an in-app action for buttons, palettes and grid rows.

### [New-ConsoleDashboard](New-ConsoleDashboard.md)
Defines a live console dashboard: a header with title and clock, the body and a footer.

### [New-ConsoleDashboardFrame](New-ConsoleDashboardFrame.md)
Creates a dashboard frame: a summary with a title line, meters, a needs-attention list, sections, a legend and a footer.

### [New-ConsoleDataGrid](New-ConsoleDataGrid.md)
Creates a named, embeddable data grid for Show-ConsoleApp.

### [New-ConsoleDatePicker](New-ConsoleDatePicker.md)
Creates a persistent civil-date or inclusive range picker.

### [New-ConsoleDiagram](New-ConsoleDiagram.md)
Builds a diagram from a script block or connection rows.

### [New-ConsoleDiagramGroup](New-ConsoleDiagramGroup.md)
Groups components and nested diagram groups.

### [New-ConsoleDiagramLink](New-ConsoleDiagramLink.md)
Connects two diagram components.

### [New-ConsoleDiagramNode](New-ConsoleDiagramNode.md)
Creates a diagram component.

### [New-ConsoleDiagramStep](New-ConsoleDiagramStep.md)
Creates a request, message, reply or failed interaction.

### [New-ConsoleDiagramUseCase](New-ConsoleDiagramUseCase.md)
Builds a playable sequence of diagram interactions.

### [New-ConsoleDialog](New-ConsoleDialog.md)
Creates a typed modal form for the running app's dialog service.

### [New-ConsoleFacts](New-ConsoleFacts.md)
Creates a facts line with bold counts, semantic colours and shared separators.

### [New-ConsoleField](New-ConsoleField.md)
Creates a form field for New-ConsoleForm.

### [New-ConsoleFlow](New-ConsoleFlow.md)
Builds a native flow strip of nodes, labelled edges and one side branch.

### [New-ConsoleForm](New-ConsoleForm.md)
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

### [New-ConsoleImage](New-ConsoleImage.md)
Creates an image to place inside panels, columns and dashboards.

### [New-ConsoleKeyValue](New-ConsoleKeyValue.md)
Creates an aligned key/value list from a hashtable, an object's properties or Key/Value records pairs.

### [New-ConsoleLanes](New-ConsoleLanes.md)
Creates a lanes widget: one row per worker, server or item of a parallel workload.

### [New-ConsoleLayoutGrid](New-ConsoleLayoutGrid.md)
Lays console widgets out in an equal-width grid.

### [New-ConsoleLogo](New-ConsoleLogo.md)
Creates banner text drawn with a pixel font and a colour gradient.

### [New-ConsoleLogRegion](New-ConsoleLogRegion.md)
Creates a log region: the newest lines of a log, bottom-aligned.

### [New-ConsoleMenu](New-ConsoleMenu.md)
Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.

### [New-ConsoleMenuItem](New-ConsoleMenuItem.md)
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

### [New-ConsoleOutline](New-ConsoleOutline.md)
Creates an outline for Show-ConsoleApp: a list of headings to jump between, such as settings groups or the sections of a long page.

### [New-ConsolePanel](New-ConsolePanel.md)
Creates a bordered panel with a title around other console widgets.

### [New-ConsoleProgress](New-ConsoleProgress.md)
Creates a progress bar or an indeterminate activity indicator.

### [New-ConsoleRegion](New-ConsoleRegion.md)
Creates a named region whose content can be replaced while a dashboard runs.

### [New-ConsoleRows](New-ConsoleRows.md)
Stacks console widgets vertically.

### [New-ConsoleRule](New-ConsoleRule.md)
Creates a horizontal rule, optionally with a title.

### [New-ConsoleScreen](New-ConsoleScreen.md)
Creates a screen for Show-ConsoleApp: its content, its breadcrumb title and the keys that work on it.

### [New-ConsoleScrollView](New-ConsoleScrollView.md)
Creates a clipped vertical viewport for a small widget tree inside an app.

### [New-ConsoleSelect](New-ConsoleSelect.md)
Creates a named select input for Show-ConsoleApp.

### [New-ConsoleSplitter](New-ConsoleSplitter.md)
Creates two panes with a divider that the mouse and keyboard can move.

### [New-ConsoleStack](New-ConsoleStack.md)
Layers console widgets on top of each other in the same space.

### [New-ConsoleStatusList](New-ConsoleStatusList.md)
Creates a checklist of items with status markers.

### [New-ConsoleSummaryBar](New-ConsoleSummaryBar.md)
Creates a summary bar: one line with progress, done of total, the time left and result counters.

### [New-ConsoleTable](New-ConsoleTable.md)
Creates a console table from pipeline objects.

### [New-ConsoleTabs](New-ConsoleTabs.md)
Creates retained tabs with keyboard and pointer navigation.

### [New-ConsoleText](New-ConsoleText.md)
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

### [New-ConsoleTextInput](New-ConsoleTextInput.md)
Creates a named single-line text input for Show-ConsoleApp.

### [New-ConsoleToggle](New-ConsoleToggle.md)
Creates a named Boolean toggle for Show-ConsoleApp.

### [New-ConsoleTree](New-ConsoleTree.md)
Creates a console tree from nested Label/Children records calls, hierarchical objects or nested hashtables.

### [New-ConsoleTreeSelect](New-ConsoleTreeSelect.md)
Creates an embeddable, lazy object tree for Show-ConsoleApp.

### [Read-ConsoleConfirm](Read-ConsoleConfirm.md)
Asks a yes/no question and returns $true or $false.

### [Read-ConsoleDate](Read-ConsoleDate.md)
Asks for one civil date or inclusive date range.

### [Read-ConsoleForm](Read-ConsoleForm.md)
Shows a form or wizard and returns the answers.

### [Read-ConsoleNumber](Read-ConsoleNumber.md)
Asks for a number.

### [Read-ConsolePath](Read-ConsolePath.md)
Asks for a file or folder path, with completion.

### [Read-ConsoleSelect](Read-ConsoleSelect.md)
Asks the user to pick one of the choices and returns the chosen object.

### [Read-ConsoleText](Read-ConsoleText.md)
Asks for a line of text.

### [Register-ConsoleView](Register-ConsoleView.md)
Registers how a type is shown by Enable-ConsoleFormatting.

### [Reset-ConsoleSetting](Reset-ConsoleSetting.md)
Undoes Set-ConsoleSetting for this session, and with -Save removes the value from the settings file.

### [Set-ConsoleFormValue](Set-ConsoleFormValue.md)
Sets field values of a form, whether or not it is shown.

### [Set-ConsoleSetting](Set-ConsoleSetting.md)
Changes a ConsoleMonster setting for this session, and with -Save for every session.

### [Set-ConsoleTheme](Set-ConsoleTheme.md)
Selects the theme used by every ConsoleMonster widget.

### [Show-Console](Show-Console.md)
Shows objects, text and widgets in a readable view that fits the terminal.

### [Show-ConsoleApp](Show-ConsoleApp.md)
Runs native input controls and dependent displays in a console application.

### [Show-ConsoleDemo](Show-ConsoleDemo.md)
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

### [Show-ConsoleDiagram](Show-ConsoleDiagram.md)
Shows a diagram from connection rows, a Diagram widget or a ChartForgeX file, with use case playback.

### [Show-ConsoleGrid](Show-ConsoleGrid.md)
Shows pipeline objects in an interactive grid and returns the rows you choose.

### [Show-ConsoleHelp](Show-ConsoleHelp.md)
Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.

### [Show-ConsoleImage](Show-ConsoleImage.md)
Shows an image in the console, at the size you ask for in cells.

### [Show-ConsoleMenu](Show-ConsoleMenu.md)
Shows a menu until the user exits.

### [Show-ConsoleObject](Show-ConsoleObject.md)
Explores an object's members with the keyboard.

### [Show-ConsoleOverview](Show-ConsoleOverview.md)
Shows a compact, read-only summary of the local host.

### [Show-ConsolePresentation](Show-ConsolePresentation.md)
Presents a scripted demo one command at a time in a real shell.

### [Show-ConsoleReplay](Show-ConsoleReplay.md)
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

### [Show-ConsoleSetting](Show-ConsoleSetting.md)
Chooses ConsoleMonster's settings in the terminal with a live preview.

### [Show-ConsoleStatusLine](Show-ConsoleStatusLine.md)
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

### [Show-ConsoleTable](Show-ConsoleTable.md)
Shows objects as a table with fitted columns, readable values and optional grouping.

### [Start-ConsoleRun](Start-ConsoleRun.md)
Starts a bounded run dashboard for work scheduled by your script.

### [Stop-ConsoleRecording](Stop-ConsoleRecording.md)
Stops recording from inside the recorded child shell.

### [Test-ConsoleTheme](Test-ConsoleTheme.md)
Checks a theme's text contrast, status colours, markers and critical fill.

### [Unregister-ConsoleView](Unregister-ConsoleView.md)
Removes registered ConsoleMonster views and value shapers.

### [Update-Console](Update-Console.md)
Updates a live widget or a caller-managed run through one validated operation.

### [Use-ConsoleGroup](Use-ConsoleGroup.md)
Runs a script inside a collapsible CI log group or beneath a muted terminal heading.

### [Use-ConsoleProgress](Use-ConsoleProgress.md)
Runs a script and shows its Write-Progress calls as an inline progress area.

### [Use-ConsoleTarget](Use-ConsoleTarget.md)
Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.

### [Wait-ConsoleDashboard](Wait-ConsoleDashboard.md)
Pauses dashboard work, or captures its next checkpoint during a recording.

### [Watch-Console](Watch-Console.md)
Runs a script block again and again and shows its latest output as a live view.

### [Watch-ConsoleLog](Watch-ConsoleLog.md)
Follows a log file or a local Windows event log with a live, scrollable log view.

### [Write-ConsoleAnnotation](Write-ConsoleAnnotation.md)
Writes a CI error, warning or notice, or a styled terminal status line.

### [Write-ConsoleLogEntry](Write-ConsoleLogEntry.md)
Writes a line to the log region of a running dashboard.

### [Write-ConsoleText](Write-ConsoleText.md)
Writes coloured text with optional markup, alignment, wrapping and a text log.

## About Topics

### [about_ConsoleMonster](About/about_ConsoleMonster.md)
