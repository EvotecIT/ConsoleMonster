# Interactive applications

Use `ConsoleApp` to place editable controls and dependent displays in an ordinary layout. A named input writes its
value into `AppState`. An `AppBinding` builds a new display from that state after an edit, so a query can update a table
and its matching count without waiting for Enter.

The [live-filter C# example](../ConsoleMonster.Examples/Showcases/LiveFilterApp.cs) demonstrates this with six synthetic
hosts. The query matches literal text in the host name, role, site or status, ignoring case. Type `sql` to reduce the
inventory to SQL01 and SQL02. Backspace broadens the result again. The count and table stay below the input at both
120 and 60 columns. The input fills the available width while the table keeps its content width.

## PowerShell

The [PowerShell example](../Website/examples/app/live-filter.ps1) uses `Show-ConsoleApp`, `New-ConsoleTextInput` and
`New-ConsoleBinding`. Its scriptblock filters native objects and passes them to the ordinary `New-ConsoleTable`
builder. Bindings run on the owning PowerShell input thread and receive `$State` as a hashtable.

```powershell
Import-Module ConsoleMonster

$hosts = @(
    [pscustomobject] @{ Host = 'WEB01'; Role = 'Web'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'SQL01'; Role = 'Database'; Status = 'Pass' }
    [pscustomobject] @{ Host = 'SQL02'; Role = 'Database'; Status = 'Fail' }
)

Show-ConsoleApp -Content {
    New-ConsoleTextInput -Key Query -Label Query -Placeholder 'Host name'
    New-ConsoleBinding -Content {
        param($State)
        $query = [string] $State.Query
        $matching = @($hosts | Where-Object {
            $_.Host.IndexOf($query, [StringComparison]::OrdinalIgnoreCase) -ge 0
        })
        "$($matching.Count) of $($hosts.Count) hosts"
        $matching | New-ConsoleTable -Property Host, Role, Status -StatusProperty Status -Expand
    }
}
```

Run the complete six-host example from the repository root after importing the module:

```powershell
./Website/examples/app/live-filter.ps1
```

## C#

Build the stable input once. Put the count and table in a binding so they read the same query snapshot. The example
uses the existing typed table and status presenter; the shared input control supplies editing, focus and the caret.

```csharp
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Examples.Showcases;

IWidget layout = new Rows(
    new TextInput("Query") {
        Label = "Query",
        Placeholder = "Host, role, site or status"
    },
    new AppBinding(state => LiveFilterApp.Results(state.Get<string>("Query", string.Empty)))) {
    Gap = 1
};

AppState state = ConsoleApp.Run(layout);
string finalQuery = state.Get<string>("Query", string.Empty);
```

`LiveFilterApp.Results` is the example's count-and-table factory. In your application, replace it with a builder over
your own data. Keep editable controls in the stable outer layout and build display widgets inside the binding.

To run the complete example from a console application referencing the examples project:

```csharp
AppState state = LiveFilterApp.Run();
```

## Keyboard and empty results

Typing and Backspace update the query immediately. Tab moves to the next input, Shift+Tab moves back, and Escape
closes the application. This example has one editable input, so Tab returns to that input. The shared editor also
handles caret movement; moving the caret leaves the query and results unchanged.

A query with no matches shows `0 of 6 hosts`, the table headings and a message to broaden the query with Backspace.
Queries use literal substring matching: brackets, asterisks and other characters are ordinary text.

## Capture and non-interactive output

Use `ScriptedInput` and `ScreenCapture` to exercise the same compiled application without a physical terminal:

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;

ScreenCapture capture = new ScreenCapture(60, 24);
using (ScriptedInput input = new ScriptedInput("\"sql\" Escape")) {
    AppState state = LiveFilterApp.Run(new InteractiveOptions {
        Input = input,
        Capture = capture,
        Mode = InteractionMode.Rich,
        Theme = Theme.Light
    });
    string text = ConsoleExporter.ToText(capture.LastFrame!);
}
```

The capture includes the initial inventory and the final filtered state. Intermediate rendering may coalesce several
edits, while the final state contains the complete query. Use the first and last frames for deterministic snapshots.

For one initialized frame in CI or redirected output, set `Mode = InteractionMode.Snapshot` in the app's
`InteractiveOptions`, or use `Show-ConsoleApp -Mode Snapshot` in PowerShell. `CONSOLEMONSTER_SNAPSHOT=1` selects the same behaviour when no input source is supplied. `CONSOLEMONSTER_INPUT` supplies scripted keys for an unchanged app. Explicit input wins. This draws once without reading input. A running interactive application
requires interactive input or an explicitly supplied input source.

Applications use a full-screen live session. C# hosts configure it through `ConsoleAppOptions.Live`;
PowerShell uses the app's full-screen defaults. Inline mode is rejected before state initialization or input acquisition because its moving screen origin
cannot provide reliable click coordinates.

Text-area edits and programmatic value changes are safe between renderer frames. Navigation uses the current text
and the latest layout width, so callbacks can clear or replace a field before the next key is read.
If the renderer or output fails, `Show-ConsoleApp` reports the failure and does not emit a successful partial state.
`ConsoleApp.Run` and `RunAsync` propagate the original exception after session cleanup. Windows output mode is saved
before first-use capability detection and restored on normal exit, startup failure and cancellation.

## Website frames

The site generator compiles and calls `LiveFilterApp.Run`; it does not maintain a separate mock screen. It writes
initial and filtered frames in both themes at widths 120 and 60. Its existing PowerShell host runner captures the
native script on PowerShell 7 and Windows PowerShell 5.1, then the renderer compares the terminal cells with C#.

From the repository root, after building the module for the capture hosts:

```powershell
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website app
dotnet run --project Website/tools/SiteFrames -c Release -- render Website
```

The generated app bundle is `Website/static/frames/app/live-filter.json`, with example sources and parity results in
`Website/data/app.json`. Generate these files through SiteFrames when updating the example.

For a focused local review without regenerating website assets, choose a scratch output directory:

```powershell
dotnet run --project Website/tools/SiteFrames -c Release -- app-review Website ./artifacts/app-review
```

This produces `app-review.json` and eight pairs of `.txt` and `.html` files named
`live-filter-{initial|filtered}-{120|60}-{dark|light}`. The HTML comes from `ConsoleExporter` and preserves the captured
cells for viewport and theme review.
