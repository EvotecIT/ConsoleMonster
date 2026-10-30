---
title: Explore an object
description: Navigate nested object members, search their values and return a selection or its PowerShell path.
slug: object-explorer
order: 13
group: apps
card: object-explorer-120
chips: [Objects, Keyboard, PowerShell]
apis: [Show-ConsoleObject, ObjectExplorer]
summary: "Browse nested members, search values and return a selection or its PowerShell path."
---

`Show-ConsoleObject` opens an object in the terminal: move through its members, expand a branch or drill into it, and return a selected value, without flattening the object into a table first.

## Try it

{{< cm-try key="explorer" ids="object-explorer-120" file="server-explorer.ps1" project="ServerExplorer" caption="A server object with an owner, groups and services, opened in the explorer. Both programs define the same server." >}}

## Keys

Up and Down move the focus. Right expands a member and Left collapses it. Enter drills into the member and puts its path in the breadcrumb; Backspace returns to the previous object. Press `/`, type a search, then Enter to step through the matching names and values. Search covers the current level and expanded branches, without reading unopened descendants. F1 lists the keys.

F10 or Ctrl+Enter accepts, Esc cancels.

## Return a value or its path

Browsing alone returns nothing. `-PassThru` returns the selected value as it is, and `-PassThru Path` returns a quoted PowerShell expression that reaches it:

```powershell
$value = $server | Show-ConsoleObject -PassThru
$path = $server | Show-ConsoleObject -RootPath '$server' -PassThru Path
# Selecting the second group returns: $server.Owner.Groups[1]
```

`-RootPath` supplies the expression's root and defaults to `$x`; the explorer shows it without evaluating it. Dictionary keys and property names with punctuation are quoted. A selected collection comes back as one value instead of being enumerated into separate pipeline records.

In C#, `ObjectExplorer.Show()` returns the selected node, or null after cancellation or the static fallback. Its `Path` and `Value` properties expose the selection.

## Narrow terminals

{{< cm-frame id="object-explorer-60" script="false" caption="The same object at 60 columns." >}}

## What it reads

PowerShell objects, dictionaries, collections and `ConvertFrom-Json` output share the existing inspector adapters. A uniform collection shows a small inline table when you drill into it. Each getter is read once, on demand, and cycles stay leaves. `-Depth`, `-MaxItems` and `-MaxNodes` bound expansion; `-Exclude` skips getters that are expensive or have side effects. A synchronous getter that is already running cannot be interrupted by an inspection budget.

## Redirected output and CI

Redirected output, CI, `-NonInteractive`, line mode and accessible mode print the static inspector instead. They return no selected value and never wait for input, which also makes the command safe in SSH sessions without a terminal. On 16-colour and no-colour terminals the screen keeps its text markers and keyboard navigation.

`Show-ConsoleObject` needs a rich interactive terminal. For a static inspector, use `$server | Show-Console -View Tree`; use `New-ConsoleTree` to construct an explicit hierarchy for a layout.

## Related

[The interactive grid](/interactive/) picks rows from a collection, and [PowerShell conventions](/docs/powershell-conventions/) covers composition and capturing output.
