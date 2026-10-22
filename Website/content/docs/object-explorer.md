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

`Show-ConsoleObject` opens an object in the terminal. Move through its members, expand a branch or drill into it, and return a selected value without flattening the object into a table first.

1. Install and import the module in PowerShell 5.1 or 7:

   ```powershell
   Install-Module ConsoleMonster -Scope CurrentUser
   Import-Module ConsoleMonster
   ```

2. Run the complete PowerShell example beside the frame below. It defines the server and opens the explorer.

```powershell
$server = [pscustomobject]@{
    Name = 'web-01'
    Owner = [pscustomobject]@{
        Name = 'Ada'
        Groups = @('Operators', 'Readers')
    }
    Enabled = $true
}
$server | Show-ConsoleObject -Title 'Server configuration'
```

{{< cm-frame id="object-explorer-120" >}}

Up and Down move the focus. Right expands a member; Left collapses it. Enter drills into the member and puts its path in the breadcrumb. Backspace returns to the previous object. Press `/`, type a search, then Enter to navigate the matching names and values. Search covers the current level and expanded branches, preserving lazy reads of unopened descendants. F1 lists the available keys.

Use F10 or Ctrl+Enter to accept, or Esc to cancel. Browsing alone returns nothing. Add `-PassThru` to return the original selected value, or `-PassThru Path` to return a quoted PowerShell expression:

```powershell
$value = $server | Show-ConsoleObject -PassThru
$path = $server | Show-ConsoleObject -RootPath '$server' -PassThru Path
# Selecting the second group returns: $server.Owner.Groups[1]
```

`-RootPath` supplies the expression's root and defaults to `$x`. The explorer displays it without evaluating it. Dictionary keys and property names with punctuation are quoted. Collection selection returns the selected member as one value, preserving collections instead of enumerating them into separate pipeline records.

{{< cm-frame id="object-explorer-60" >}}

PowerShell objects, dictionaries, collections and `ConvertFrom-Json` output share the existing inspector adapters. Uniform collections have a small inline table when drilled into. Each getter is read once on demand. Cycles remain leaves. `-Depth`, `-MaxItems` and `-MaxNodes` bound expansion; `-Exclude` skips getters that are expensive or have side effects. An already executing synchronous getter cannot be interrupted by an inspection budget.

Redirected output, CI, `-NonInteractive`, line mode and accessible mode print the existing static inspector. They return no selected value and do not wait for input. This also makes the command useful in SSH sessions without a terminal. The screen keeps text markers and keyboard navigation in 16-colour and no-colour terminals.

For C#, create a console project with `dotnet new console`, add the package with `dotnet add package ConsoleMonster`, and replace `Program.cs` with the complete C# program beside the frame. Both examples define the same server. `ObjectExplorer.Show()` returns the selected node, or null after cancellation or static fallback. Its `Path` and `Value` properties expose the selection.

Try [the interactive grid](/interactive/) to pick rows from a collection, or [the PowerShell conventions](/docs/powershell-conventions/) for composition and output capture.
