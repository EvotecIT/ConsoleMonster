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

A named input and a binding turn an ordinary layout into an interactive application. This example filters six synthetic hosts: type `sql` in Query and the count and status table update to show SQL01 and SQL02. Matching is literal and case-insensitive across host, role, site and status. No machines or services are queried.

## Run the filter

1. Install ConsoleMonster for PowerShell:

   ```powershell
   Install-Module ConsoleMonster -Scope CurrentUser
   ```

2. Copy the full PowerShell script below into `live-filter.ps1`, then run it in an interactive terminal:

   ```powershell
   ./live-filter.ps1
   ```

3. For C#, create a console project and add the ConsoleMonster package:

   ```shell
   dotnet new console -n HostFilter
   cd HostFilter
   dotnet add package ConsoleMonster
   ```

4. Replace `Program.cs` with the full C# program below and run it:

   ```shell
   dotnet run
   ```

Query has focus when the application opens. Type to filter, use Backspace to broaden the query, and press Escape to quit. Tab and Shift+Tab move between controls in layouts with more than one input. A query with no matches keeps the column headers and shows an explanation beneath the empty table.

{{< cm-frame ids="app-live-filter-initial-120,app-live-filter-filtered-120" caption="The same program at 120 columns, before typing and after `sql`. The count changes from six hosts to two. Full program includes the synthetic inventory and all setup code." >}}

## State and dependent widgets

In PowerShell, `New-ConsoleTextInput -Name Query` stores the current text in `$State.Query`. `New-ConsoleBinding` runs after each edit and builds the matching count and table from that value. The layout remains native PowerShell: `New-ConsoleRows`, the text input and `New-ConsoleTable` compose the screen inside `Show-ConsoleApp`.

In C#, `TextInput("Query")` writes to `AppState`, and `AppBinding` reads it with `state.Get<string>("Query", string.Empty)`. `ConsoleApp.Run` drives the existing live renderer and returns the final state when the application ends. Use `Show-ConsoleApp -PassThru` when a PowerShell caller needs the final named values.

The input stays in place while the binding rebuilds its dependent widgets. The count and table use the same query snapshot, and the status column uses the built-in status presenter.

## At 60 columns

{{< cm-frame ids="app-live-filter-initial-60,app-live-filter-filtered-60" caption="The same inventory and query at 60 columns. Both widths are rendered in light and dark themes, and the saved Windows PowerShell 5.1 and PowerShell 7 output is checked against the C# program cell by cell." >}}

## Redirected output and CI

An interactive application needs input. Without an interactive terminal, `ConsoleApp.Run` and `Show-ConsoleApp` report that prompting is unavailable unless the caller explicitly requests a static snapshot. To print the initial PowerShell layout without reading keys, run the same script with its snapshot option:

```powershell
./live-filter.ps1 -AppOptions @{ StaticSnapshot = $true }
```

For a C# snapshot, pass `new ConsoleAppOptions { StaticSnapshot = true }` as the `options` argument to `ConsoleApp.Run`. A snapshot initializes the layout once; it does not read a query.

Try [interactive prompts](/interactive/) for one answer at a time, [live sessions](/docs/live-sessions/) for dashboards updated by running work, or [threads and concurrency](/docs/threads/) for the live engine's threading rules.
