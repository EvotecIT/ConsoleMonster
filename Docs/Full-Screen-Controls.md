# Composed full-screen controls

`ConsoleApp` owns input and rendering. Retained controls keep their state when
focus moves, a tab changes, a dialog opens or a child process takes the terminal.
The controls are in the core package and use the same APIs on .NET Framework
4.7.2, .NET Standard 2.0, .NET 8 and .NET 10.

## Data grids

Create a listing, then embed a named `DataGrid`. Each instance owns its cursor,
viewport, prefix search and marks. `Selection` contains original objects; a row's
`Id` identifies it across refreshes independently of that object or its position.

```csharp
var listing = Ui.Grid().Title("Files");
listing.AddColumn("Name").AddColumn("Size");
listing.AddRow(new GridRow(file, file.Name,
    new DataGridCell(async cancellation =>
        (object?)await ReadSizeAsync(file, cancellation))) { Id = file.FullName });

using var left = Ui.DataGrid().Key("Left").Grid(listing).Build();
using var right = Ui.DataGrid().Key("Right").Grid(otherListing).Build();
var panels = Ui.LayoutRegions().Add("left", left).Add("right", right);
ConsoleApp.Run(panels);
```

Set column widths and priorities on `TableColumn` to control narrowing. The grid
measures at most 128 sample rows and the visible page. Only visible lazy cells
start work. Their factories run on workers, receive cancellation and show `…`
until completion. A failed cell retains its placeholder and exposes `Error`.
Use a fresh cell when its value changes. Pending values do not start work merely
because the user searches, marks or sorts a listing.

Call `grid.SetRows(rows)` with unique, non-null identities to refresh a listing.
Surviving marks and the focused identity are retained. `SearchPrefix`,
`MarkPattern`, `UnmarkPattern` and `InvertMarks` operate independently per grid.
Patterns use the shared `GlobPattern` grammar, including `*`, `?`, character
classes, exclusions and semicolon-separated alternatives; matching ignores case. Marks survive a filter, and
selection returns marked objects in sort order even when some are filtered out.
Call `RefreshRows` explicitly if newly completed cell values should affect an
existing filter or sort.

`Show-ConsoleGrid` uses this same control. Its substring filter, sort, column
chooser, details, original-object output and line fallback keep their contracts.
PowerShell can embed a prepared listing with
`New-ConsoleDataGrid -Key Left -Grid $listing`; use `-NoQuickSearch` when the app
registry owns printable keys.

For direct .NET interop in PowerShell, resolve a type through the module's
loaded assembly, as described in [PowerShell conventions](PowerShell-Conventions.md):

```powershell
$listing = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.Grid'))::new('Files')
$null = $listing.AddColumn('Name')
$null = $listing.AddRow($file, @($file.Name))
$left = New-ConsoleDataGrid -Key Left -Grid $listing
```

This preserves type identity in the module's assembly load context and keeps
the approved set of fully qualified type accelerators unchanged.

## Registry keys and footer

`ConsoleAppOptions.HandleInput` routes a normalized event to the app registry
before the focused control. Modal input and cancellation take precedence.
Release host gestures that belong to the registry:

```csharp
var options = new ConsoleAppOptions();
options.Keys.NextFocus = null;
options.Keys.CancelAction = null;
options.Keys.Actions = null;
options.Keys.Palette = null;
options.HandleInput = (state, input) => commands.Dispatch(input);
```

Ctrl+C cancels active work and otherwise exits the app. If a different gesture
is desired, set `Keys.CancelAction`, for example to Escape. The default gestures
remain Tab/Shift+Tab for focus, F6 for cancellation, F8 for action buttons and
Ctrl+K for the palette.

Supply `ConsoleAppOptions.Footer` with a `FooterBar` whose `HintsSource` reads
the current registry context. `new KeyHint(keys, label, available)` dims an
unavailable entry. `FixedSlots = true` retains all entries in their positions
and shortens labels to fit. The registry supplies F-key or Alt+number labels and
routes both gestures to the same command; changing focus changes its context.

## Child terminal and last output

Attach one `TerminalHandoff` to the app options and invoke it on the app thread:

```csharp
var terminal = new TerminalHandoff();
var options = new ConsoleAppOptions { TerminalHandoff = terminal, SignalKeys = false };
options.HandleInput = (state, input) => {
    if (input.Is(ConsoleKey.F4)) {
        var child = new TerminalChildOptions { FileName = editor, WorkingDirectory = folder };
        child.Arguments.Add(path);
        int exitCode = terminal.SuspendForChild(child);
        RefreshPanels();
        return true;
    }
    if (input.Is(ConsoleKey.O, KeyModifiers.Control)) {
        terminal.ShowOutput();
        return true;
    }
    return false;
};
ConsoleApp.Run(panels, options: options);
```

The host leaves its alternate screen, pauses drawing and mouse reporting, and
relays the physical terminal to a child ConPTY on Windows or a controlling Unix
PTY. The child receives terminal input, colours and window-size changes. Its
exit code is returned. Startup failure, timeout and cancellation restore the app
before escaping; the focused controls and their values stay intact.

`LastOutput` retains at most `OutputCapacity` characters in memory. No child
input or command history is recorded. `ShowOutput` displays the last terminal
screen until a key is pressed, then redraws the app. Handoff requires physical
TTY input and output; scripted or redirected runs fail clearly.

`SignalKeys = false` keeps Ctrl+C and Ctrl+backslash as app input. Enable signal
keys only when the app needs the terminal to generate those signals. The child
uses its own terminal modes independently of this setting.

## Tabs and regions

`Ui.Tabs().Add("Files", files).Add("Output", output)` keeps stable subtrees and
mounts only the active tab. Ctrl+PageUp/PageDown and header clicks switch tabs;
each tab remembers its last focused input. Hidden inputs are excluded from
keyboard traversal and pointer hit testing.

`LayoutRegions` places regions side by side at `NarrowBelow` columns and above.
Below that width it shows the region containing focus, or `ActiveRegion` when
none owns focus. `Activate(key)` shows a region and requests its first enabled
input; `Swap(first, second)` exchanges existing subtrees and retains their state.
Configure the set of inputs before starting the app.

PowerShell composes the same pages with `New-ConsoleTab -Title Files -Content
{ ... }` and `$pages | New-ConsoleTabs -SelectedIndex 0`.

## Forms and background operations

`AppDialogs.Show` opens a `FormDialog<T>` inside the running host. Its content
is an ordinary input subtree. The result mapper and optional validator run on
the app thread. Cancel is the initial button unless `DefaultAccept` or
`InitialFocus` chooses another target. Tab stays inside the form; Escape cancels
and restores the underlying focus. Ctrl+Enter accepts from a field. In terminals
that send Ctrl+Enter as plain Enter, Tab to the affirmative button and press Enter.

```csharp
var dialogs = new AppDialogs();
var form = Ui.Dialog().Title("Destination")
    .Content(c => c.Add(Ui.TextInput().Key("Path").Value("backup").Build()))
    .InitialFocus("Path").Build();
// From an app-thread command, with options.Dialogs = dialogs:
dialogs.Show(form, answer => {
    if (answer.Accepted) { destination = answer.Value!.Get<string>("Path"); }
});
```

`New-ConsoleDialog` builds the same named-value form for PowerShell; construction
does not start another input loop. C# callers can use `FormDialog<T>` directly
when a domain-specific result is preferable to `AppState`.

Set `AppCommand.ProgressDialog` (PowerShell: `New-ConsoleCommand
-ProgressDialog`) to show the existing action worker in a cancellable dialog.
Report immutable `AppProgress` values through `AppActionContext.Report`.
Updates coalesce; they do not create an unbounded queue. Background closes the
progress overlay while work continues. Completion, cancellation and failure
show a final summary. `Summarize(new AppOperationSummary(...))` can include
failed items and an explicit retry command; retry runs that command, so the
backend decides which failed items are safe to repeat. Skip closes the summary.
The host never silently replays a whole partially completed operation.

Attach an `AppActions` service through `ConsoleAppOptions.Actions` when the
registry creates an operation after a dialog result. On the app thread,
`actions.Invoke(command)` captures selection and uses the same confirmation,
worker, progress and summary path as a registered shortcut. It returns false
while another operation or modal is active. `actions.Cancel()` requests
cooperative cancellation. PowerShell passes the same service with
`Show-ConsoleApp -Actions`. This lets an `ICommanderHost.Start` implementation
adapt its backend operation without another worker or synthetic input.
Long summaries scroll with Alt+PageUp/PageDown or the mouse wheel while their
Retry/Skip buttons retain focus.

## Dates, trees and logs

`New-ConsoleTreeSelect` and `Ui.TreeSelect()` embed the same lazy object explorer
used by `Show-ConsoleObject`, including traversal budgets and safe member access.
The standalone explorer retains its value/path output contracts.

`New-ConsoleSelect -Radio` presents the existing scalar selector as radio choices;
`New-ConsoleToggle` remains the checkbox presentation.

`DatePicker`, `New-ConsoleDatePicker`, `DatePrompt` and `Read-ConsoleDate` share
the static calendar's Gregorian month geometry, culture labels, ISO weeks and
marked days. Scalar answers are midnight `DateTime` values with unspecified
kind. Range answers are one inclusive `DateRange`. Arrows move without committing;
Space anchors a range and Enter commits it. Shift+arrows also extend a range.
Wheel changes month; dragging beyond the month navigates at most four times per
second. Minimum, maximum and unavailable days reject invalid commits visibly,
including an unavailable interior day. Unattended prompts require a valid default.

`LogViewport` retains a capped `LogRegion` with severity, elapsed time and literal
messages. Up/PageUp pause following, End resumes it, and Left/Right scroll long
lines. `CopyText()` returns retained messages for an app-owned copy/export action.
`Write` is safe from a worker thread. No terminal output bypasses the live frame.

Run `dotnet run --project ConsoleMonster.Examples --framework net8.0 --
fullscreen-controls` for the service-browser and operations-calendar example.
It uses fictional records and does not modify files or services. At 80 columns
the two regions are side by side; below that only the focused region is shown.
Below its configured 40-by-12 minimum, the host asks for a larger window.
