---
title: Interactive apps
description: Build a live inventory filter with a named text input, shared application state and a table that updates after every edit.
slug: interactive-apps
order: 11
group: apps
card: app-live-filter-filtered-60
apis: [Show-ConsoleApp, ConsoleApp, AppState, AppBinding]
chips: [Inputs, Live filtering]
summary: "Named inputs update shared state and redraw dependent widgets after every edit."
---

A named input and a binding turn an ordinary layout into an interactive application. This one filters six synthetic hosts: type `sql` in Query and the count and table narrow to SQL01 and SQL02. Nothing is queried.

## Try it

{{< cm-try key="live-filter" ids="app-live-filter-initial-60,app-live-filter-filtered-60" project="HostFilter" caption="The program before typing and after `sql`, at 60 columns. The count drops from six hosts to two." >}}

Query has focus when the application opens. Type to filter, Backspace broadens the query and Escape quits. Matching is literal and case-insensitive across host, role, site and status; a query with no matches keeps the column headers and explains why the table is empty. Tab and Shift+Tab move between controls when a layout has more than one input.

## How it works

The input has a name, and that name is the key in the application's state. The binding runs after every edit, reads the current value and builds the widgets that depend on it:

<div class="cm-lang" data-lang="powershell">

```powershell
New-ConsoleTextInput -Key Query -Label Query -Placeholder 'Host, role, site or status'
New-ConsoleBinding -Content {
    param($State)
    $matches = @(& $matchingHosts $State)
    "[bold]$($matches.Count)[/] of $($hosts.Count) hosts"
    $matches | New-ConsoleTable -Property Host, Role, Site, Status -StatusProperty Status -NoHeaderSeparator
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
new TextInput("Query") {
    Label = "Query",
    Placeholder = "Host, role, site or status"
},
new AppBinding(state => Results(state.Get<string>("Query", string.Empty)))
```

</div>

- **State.** `New-ConsoleTextInput -Key Query` stores the text in `$State.Query`; in C#, `TextInput("Query")` writes it to `AppState`, and `state.Get<string>("Query", string.Empty)` reads it.
- **Dependent widgets.** The count and the table come from one snapshot of the query, so they always agree. The input stays where it is while the binding rebuilds what is below it, and the Status column uses the built-in status presenter.
- **The loop.** `Show-ConsoleApp` and `ConsoleApp.Run` drive the existing live renderer: keys go to the focused control, the binding runs, the screen redraws only the cells that changed. The layout itself is ordinary composition (`New-ConsoleRows` or `Rows`).
- **The result.** `ConsoleApp.Run` returns the final state when the application ends; in PowerShell, `Show-ConsoleApp -PassThru` returns the named values.

## On a wide terminal

The input stretches to the width of the terminal; the table keeps its natural width. This is the same program at 120 columns:

{{< cm-frame ids="app-live-filter-initial-120,app-live-filter-filtered-120" script="false" caption="The same program at 120 columns. Both widths are rendered in light and dark, and the saved Windows PowerShell 5.1 and PowerShell 7 output is checked against the C# program cell by cell." >}}

## Redirected output and CI

An interactive application needs a keyboard. Without an interactive terminal, `Show-ConsoleApp` and `ConsoleApp.Run` report that prompting is unavailable, unless the caller asks for a static snapshot. The script passes its `-AppOptions` to `Show-ConsoleApp`, so `./live-filter.ps1 -AppOptions @{ StaticSnapshot = $true }` prints the first screen once and exits. In C#, pass `new ConsoleAppOptions { StaticSnapshot = true }` as the `options` argument of `ConsoleApp.Run`. A snapshot draws the layout once; it does not read a query.

## Related

[Interactive prompts](/interactive/) ask for one answer at a time, [live sessions](/docs/live-sessions/) show dashboards updated by running work, and [threads and concurrency](/docs/threads/) explains the live engine's threading rules.
