---
title: Coming from Microsoft grid tools
description: Move Out-GridView, GraphicalTools and ConsoleGuiTools selection workflows into a terminal grid and object explorer.
slug: microsoft-grid-tools
order: 5
toc: false
card: object-explorer-120
---

Use ConsoleMonster when a selection workflow needs to stay in the terminal, including over SSH or on a host without Windows Desktop. `Show-ConsoleGrid` displays, filters and sorts ordinary object rows, then returns the selected objects. `Show-ConsoleObject` explores nested values. These are changes of interaction model, not drop-in replacements for every desktop-grid feature.

## Project status, checked 25 October 2026

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Project or command | Verified status | Source |
| --- | --- | --- |
| Built-in `Out-GridView` | Available in PowerShell 7 on Windows Desktop only; unavailable on Server Core/Nano Server. It is not described here as discontinued. | [Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/out-gridview?view=powershell-7.5) |
| `Microsoft.PowerShell.GraphicalTools` | Its Avalonia-based cross-platform Out-GridView is deprecated and unmaintained according to the repository README; development was paused pending a possible MAUI rewrite. Gallery version 0.2.0 was published 16 August 2019. | [Maintainer notice #101](https://github.com/PowerShell/ConsoleGuiTools/issues/101), [Gallery 0.2.0](https://www.powershellgallery.com/packages/Microsoft.PowerShell.GraphicalTools/0.2.0), [README](https://github.com/PowerShell/ConsoleGuiTools#readme) |
| `Microsoft.PowerShell.ConsoleGuiTools` | Repository archived 24 June 2026. README calls it feature complete; v0.7.7 is the last feature release, published 1 May 2024. It already supported Windows, macOS and Linux. | [Repository and archive banner](https://github.com/PowerShell/ConsoleGuiTools), [v0.7.7](https://github.com/PowerShell/ConsoleGuiTools/releases/tag/v0.7.7) |

</div>

ConsoleGuiTools points readers to a [community continuation, PSTui](https://github.com/PowerShell/ConsoleGuiTools#readme). Consider it if keeping the original command names is your priority. ConsoleMonster is a separate renderer with different selection, layout and pipeline contracts.

## Command map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Existing operation | ConsoleMonster | Note |
| --- | --- | --- |
| `Out-GridView` for viewing only | `Show-ConsoleGrid -OutputMode None` | Stays in the terminal; blocks until closed. |
| `Out-GridView -OutputMode Single` | `Show-ConsoleGrid -OutputMode Single` | Returns one original row. |
| `Out-GridView -PassThru` or `-OutputMode Multiple` | `Show-ConsoleGrid -OutputMode Multiple` | Multiple is ConsoleMonster's default. |
| `Out-ConsoleGridView -OutputMode ...` | `Show-ConsoleGrid -OutputMode ...` | Check filter grammar and key bindings. |
| `Out-ConsoleGridView -Filter` | `Show-ConsoleGrid -Filter` | Text looks similar but the grammars differ. |
| `Show-ObjectTree` | `Show-ConsoleObject` | Keyboard explorer; `-PassThru` explicitly returns a selection. |
| A fixed inventory report | `Show-ConsoleTable` | No interactive selection; `-AsString` for logs/tests. |

</div>

## Shared sample data

Run `Import-Module ConsoleMonster`, then define these rows. Use the left-hand grid examples in the host/module that supplies that command. No example changes a service or machine.

```powershell
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
```

## 1. Browse without returning selections

<div class="cm-migration">
<div>

**Before**

```powershell
$servers | Out-GridView -Title 'Patch inventory'
# ConsoleGuiTools:
$servers | Out-ConsoleGridView -Title 'Patch inventory' -OutputMode None
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | Show-ConsoleGrid -Title 'Patch inventory' -OutputMode None
```

</div>
</div>

Specify `None` when migrating a viewer. Otherwise ConsoleMonster's default multiple-selection mode can emit rows into the next pipeline command. Both grids need objects, not native `Format-Table` records.

{{< cm-try ids="gridview-fallback-120,gridview-fallback-60" project="ServicePicker" >}}

This related runnable service-picker program shows real grid output at two widths, including its own sample data and selection code.

## 2. Pick one target

<div class="cm-migration">
<div>

**Before**

```powershell
$server = $servers | Out-GridView -OutputMode Single -Title 'Review one server'
# ConsoleGuiTools:
$server = $servers | Out-ConsoleGridView -OutputMode Single
```

</div>
<div>

**ConsoleMonster**

```powershell
$server = $servers | Show-ConsoleGrid -OutputMode Single -Title 'Review one server'
if ($null -ne $server) {
    $server | Show-ConsoleList
}
```

</div>
</div>

## 3. Keep objects for a later report

<div class="cm-migration">
<div>

**Before**

```powershell
$selected = @($servers | Out-GridView -PassThru)
$selected | Select-Object Name, Site, Pending
```

</div>
<div>

**ConsoleMonster**

```powershell
$selected = @($servers | Show-ConsoleGrid -OutputMode Multiple)
$selected | Select-Object Name, Site, Pending
```

</div>
</div>

Check for an empty selection before invoking downstream operations. The example returns the original objects so numeric fields remain numeric; it does not scrape rendered text.

## 4. Filter and sort deliberately

<div class="cm-migration">
<div>

**Before**

```powershell
$servers | Out-ConsoleGridView -Filter 'SQL' -OutputMode Single
# Built-in Out-GridView has an interactive filter box, not a -Filter parameter.
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | Show-ConsoleGrid -Filter 'SQL' -SortBy Pending -Descending -OutputMode Single
```

</div>
</div>

ConsoleGuiTools documents regular-expression filtering. ConsoleMonster uses its own grid search syntax; start with literal text and check [grid behavior](/interactive/) before porting advanced expressions. Desktop column dragging, criteria dialogs and clipboard gestures do not map one-for-one to terminal controls.

## 5. Explore a nested record

<div class="cm-migration">
<div>

**Before**

```powershell
$inventory = [pscustomobject] @{ CollectedBy = 'Inventory'; Servers = $servers }
$inventory | Show-ObjectTree
```

</div>
<div>

**ConsoleMonster**

```powershell
$inventory = [pscustomobject] @{ CollectedBy = 'Inventory'; Servers = $servers }
$inventory | Show-ConsoleObject -Title 'Inventory object' -Depth 4
```

</div>
</div>

The default explorer is a viewer. Use `-PassThru` only when you want selected values; `-SelectionOutput Path` returns an inspection path instead. A path is descriptive output, not a command to evaluate. [Object exploration](/docs/object-explorer/) includes real captures and bounded-depth options.

## Differences to know

- Built-in Out-GridView opens a separate desktop window and can return immediately in view-only mode. ConsoleMonster occupies the terminal and owns input until it closes; there is no separate-window `-Wait` lifecycle.
- `Show-ConsoleGrid` defaults to `Multiple`. Explicit `None`, `Single` and `Multiple` make migrated pipelines easier to review.
- ConsoleMonster is not an `ogv` or `ocgv` alias. The optional GridView formatting feature is an unavailable-host adapter; prefer the explicit command when migrating scripts.
- Filter syntax, selection keys, hidden columns and sorting are ConsoleMonster behavior. Follow the on-screen key hints and keep object projection in `Select-Object` or `-Property`.
- An unattended host cannot make an interactive choice. Supply the selection in script parameters; use `Show-ConsoleTable -AsString` for a noninteractive report. A fallback or snapshot is not approval.

## Next

Try [object exploration](/docs/object-explorer/), [interactive screens](/interactive/) or [all migration guides](/guides/coming-from/).
