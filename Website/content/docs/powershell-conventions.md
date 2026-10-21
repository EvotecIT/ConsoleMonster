---
title: PowerShell conventions
description: "One grammar for every widget: containers take script blocks, items are commands with shared parameters, and .NET methods still work."
slug: powershell-conventions
order: 14
group: hosts
card: shaping
chips: [Cmdlets, Script blocks]
summary: "One grammar for every widget, and each widget's .NET methods."
---

Every widget is a `New-Console*` command, and they read the same way, so a widget you have not used yet works like the last one.

- **Containers take a script block of children.** `New-ConsoleRows`, `New-ConsoleColumns`, `New-ConsoleGrid`, `New-ConsolePanel`, `New-ConsoleStack`, `New-ConsoleAlign`, `New-ConsolePadding` and `New-ConsoleRegion` lay out what their script block emits; strings are markup text.
- **Widgets with items take a script block of item commands**, with the same parameter names everywhere: `New-ConsoleTreeNode` for `New-ConsoleTree`, `New-ConsoleKeyValueItem` for `New-ConsoleKeyValue`, `New-ConsoleStatusItem` for `New-ConsoleStatusList` and `New-ConsolePhases`, `New-ConsoleBarItem` for `New-ConsoleBarChart`, `New-ConsoleChartSeries` for `New-ConsoleLineChart`, `New-ConsoleLogEntry` for `New-ConsoleLog` and `New-ConsoleLane` for `New-ConsoleLanes`. Items can also come from the pipeline, and simple data still works with `-Data` hashtables and `-Value` arrays.
- **Hierarchical objects become a tree.** `New-ConsoleTree` follows children with `-ChildrenProperty` or a `-Children` script block and keeps a large or cyclic hierarchy bounded with `-Depth`, `-MaxItems` and `-MaxNodes`.
- **A widget made earlier grows with `Add-ConsoleItem`**, which takes the same item commands; a widget in a running dashboard changes through the live commands (`Update-ConsoleStatus`, `Write-ConsoleLog`, `Add-ConsoleValue`...).

```powershell
New-ConsoleTree '[bold]evotec.xyz[/]' {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'SPF' -Status Warn -Detail '9 of 10 DNS lookups'
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
} | Write-ConsoleWidget

$groups | New-ConsoleTree 'Privileged groups' -ChildrenProperty Members -StatusProperty Health -Depth 3 | Write-ConsoleWidget
```

## Shared parameters

| Parameter | Meaning everywhere |
|---|---|
| `-Status` | `Pass`, `Fail`, `Warn`, `Skip`, `Info`, `Running`, `Pending`, plus PowerShell's words (`Success`, `Warning`, `Error`, `Failed`, `Skipped`...), with tab completion. |
| `-Level` | Log levels: `Information`, `Warning`, `Error`, `Verbose`, `Debug`, `Success`, or `info`, `warn`, `err`. |
| `-Detail` | A muted note beside the label: a duration, a count, a short reason. |
| `-Tone` | A colour as a theme role: `Accent`, `Pass`, `Fail`, `Warn`, `Muted`, `Series1` to `Series6`. |
| `-Value` and `-Presenter` | What an item shows, and how: `Bytes`, `Duration`, `Date`, `Number`, `Boolean`... |
| `-Markup` | Opt-in markup for data values; titles and labels take markup without it. |
| `-At` | A time: a `TimeSpan`, seconds, or `500ms`, `2s`, `1.5m`, `1h`. |

## Every widget, both ways

The cmdlet form is what the documentation shows first. Each widget is also an ordinary .NET object, so a script can keep it in a variable and call its methods, the way C# code does.

| Widget | Cmdlet form | Grow it later | In a dashboard | .NET methods |
|---|---|---|---|---|
| Tree | `New-ConsoleTree` + `New-ConsoleTreeNode` | `Add-ConsoleItem` | | `Add(label, status, note)` returns the node |
| Key/value list | `New-ConsoleKeyValue` + `New-ConsoleKeyValueItem` | `Add-ConsoleItem` | `Set-ConsoleValue` | `Add(key, value, status)`, `AddValue(key, value, presenter)` |
| Status list | `New-ConsoleStatusList` + `New-ConsoleStatusItem` | `Add-ConsoleItem` | `Update-ConsoleStatus` | `Add(label, status, detail)` |
| Phases | `New-ConsolePhases` + `New-ConsoleStatusItem` | `Add-ConsoleItem` | `Update-ConsolePhase` | `Set(key, status, label, detail)` |
| Bar chart, stacked bar | `New-ConsoleBarChart` + `New-ConsoleBarItem` | `Add-ConsoleItem` | `Add-ConsoleValue`, `Set-ConsoleValue` | `Add(label, value, token)` |
| Line and scatter chart | `New-ConsoleLineChart` + `New-ConsoleChartSeries` | `Add-ConsoleItem` | `Add-ConsoleValue -Key` | `AddSeries(name, token)`, `Add(series, value)` |
| Sparkline, histogram | `New-ConsoleSparkline`, `New-ConsoleHistogram` | `Add-ConsoleItem` | `Add-ConsoleValue` | `Add(value)` |
| Log | `New-ConsoleLog` + `New-ConsoleLogEntry` | `Add-ConsoleItem` | `Write-ConsoleLog` | `Write(text, level, time)` |
| Lanes | `New-ConsoleLanes` + `New-ConsoleLane` | `Add-ConsoleItem` | `Update-ConsoleLane` | `Add(key, label, total)` returns the lane |
| Progress, spinner | `New-ConsoleProgress`, `New-ConsoleSpinner` | | `Update-ConsoleProgress`, `Update-ConsoleSpinner` | `Value`, `Status`, `Detail` |
| Live table | `New-ConsoleLiveTable` | `Add-ConsoleItem` | `Update-ConsoleTable` | `Set(key, values)`, `Remove(key)` |
| Cell map, summary bar | `New-ConsoleCellMap`, `New-ConsoleSummaryBar` | `Add-ConsoleItem` (cell map) | drawn by `Invoke-ConsoleRun` | `Add(state)`, `SetProgress`, `SetCounter` |
| Region | `New-ConsoleRegion` | | `Set-ConsoleRegion` | `session.Region(name).Set(widget)` |

The same log written both ways:

```powershell
New-ConsoleLog -Height 5 {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
} | Write-ConsoleWidget
```

```powershell
# Using the object directly
$log = New-ConsoleLog -Height 5
$log.Write('Discovered 4 domain controllers in 2 domains', 'info', [timespan]::FromSeconds(2))
$log.Write('DC02: USN rollback check failed (event 2095)', 'err', [timespan]::FromSeconds(44))
$log | Write-ConsoleWidget
```

The full conventions, with every widget and parameter, are in [Docs/PowerShell-Conventions.md](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/PowerShell-Conventions.md), and every command is in the [command reference](/powershell/commands/).