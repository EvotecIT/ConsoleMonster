---
title: Compare several objects
description: Compare two or more objects against a baseline, property by property, with drift highlighted and noise normalised away.
slug: object-drift
order: 17
group: apps
summary: "One row per property, one column per object, differences against a chosen baseline and a status for every row."
apis: [Compare-ConsoleObject, ObjectDiff.CompareBaseline, BaselineView]
chips: [Drift, Baseline]
card: object-drift-120
---

`Compare-ConsoleObject -View Baseline` puts two or more objects side by side: one row per property (nested properties by their path), one column per object, and a status for every row. One object is the baseline; every cell that differs from it is highlighted, and a property an object doesn't have says `(missing)` instead of looking empty. It suits servers that should be configured alike, such as the domain controllers of one forest.

## Try it

{{< cm-try key="object-drift" ids="object-drift-120" project="ObjectDrift" caption="Four domain controllers compared with dc01. Only the properties that drift are listed; the SPNs differ only by host name, so they don't count." >}}

## How it works

Pass the objects through the pipeline or `-InputObject`, or to `ObjectDiff.CompareBaseline` in C#, and render the result with `BaselineView`:

```powershell
$controllers | Compare-ConsoleObject -View Baseline -NameProperty Name -Baseline dc01 -Ignore Name `
    -Normalize @{ Spn = { $_ -replace '/[^.]+', '/HOST' } }
```

- **Columns.** Each column is named by `-NameProperty`, by `-Name` with one name per object, or by the usual key properties (`Name`, `Id` and similar). The first object is the baseline unless `-Baseline` names another column or gives its zero-based index.
- **Rows.** Properties are compared to `-Depth` levels, so `Security.Signing` is a row of its own. Matching rows are left out; `-IncludeEqual` shows them too. `-Property` and `-Ignore` choose the paths.
- **Case.** Strings compare case-insensitively, as `Compare-Object` does, and `-CaseSensitive` (`DiffOptions.CaseSensitive`) changes both values and names. A difference only in case is still shown, with its own status, so it never disappears silently.
- **Noise.** `-Normalize` maps a property path to a script block (`DiffOptions.Normalize` takes a delegate) whose result is compared instead of the value. The matrix still shows the original values.
- **Narrow windows.** When the columns don't fit, each property becomes a small card listing every object's value and the status, like other ConsoleMonster tables.

{{< cm-frame id="object-drift-60" script="false" caption="The same comparison at 60 columns: one card per property." >}}

## Two sets of objects

Without `-View Baseline`, `Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after` matches two collections by key and lists what was added, removed and changed, with `-View Summary`, `Table`, `SideBySide` or `Unified`. The same case rule, missing marker and `-Normalize` apply there, and `-PassThru` returns the change records instead of drawing them.

## In a layout or a report

`-AsWidget` returns the comparison as a widget instead of writing it, so it goes into `Show-ConsoleApp` layouts and `Export-ConsoleFrame` exports:

```powershell
$matrix = $controllers | Compare-ConsoleObject -View Baseline -NameProperty Name -Ignore Name -AsWidget
$matrix | Export-ConsoleFrame -Format Html -Width 120 -Path ./controller-drift.html
```

In C#, `BaselineView` is an ordinary widget; `BaselineComparison.Rows` gives each property's cells, presence and status for your own checks.

The [Compare-ConsoleObject reference](/powershell/commands/compare-consoleobject/) lists every parameter.

## Next

- [Explore an object](/docs/object-explorer/): navigate one object's members and search its values.
- [Compare text and files](/docs/text-diff/): compare strings and files line by line.
- [Live sessions](/docs/live-sessions/): dashboards that update while work runs.
