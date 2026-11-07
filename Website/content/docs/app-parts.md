---
title: App parts, one by one
description: Explore panes, folding sections, outlines, tabs, linked charts, trees and searchable logs in small PowerShell, C# and C# fluent apps.
slug: app-parts
order: 12
group: apps
summary: "Panes, folding sections, outlines, tabs, linked charts, trees and the find bar, each on its own."
apis: [New-ConsoleSplitter, New-ConsoleAccordion, New-ConsoleCollapsible, New-ConsoleOutline, New-ConsoleBreadcrumb, New-ConsoleTabs, New-ConsoleChartView, New-ConsoleTreeSelect, New-ConsoleLogRegion]
chips: [Apps, Mouse, PowerShell, C#]
card: app-chartview
---

The [incident workbench](/demos/incident-workbench/) and the other app demos are built from a handful of parts. Here each part is a whole app on its own, in PowerShell, C# and C# fluent, with the screen it drew after the keys or clicks in its caption. Every part works with the keyboard and the mouse, lists its keys in the footer, and keeps its value in app state under its `Key`, so a binding or a callback can read it.

## Try it

Pick a part, then a language. The frame under the code is what that program drew; the three languages draw the same cells.

{{< cm-try key="parts" scenarios="Splitter:app-splitter|Accordion:app-accordion|Outline:app-outline|Tabs:app-tabs|ChartView:app-chartview|TreeSelect:tree|Find bar:app-find" project="AppParts" >}}

## How each part works

### Splitter

Two panes side by side, or top and bottom with `-Vertical` (`Vertical = true`). Drag the divider, or press Ctrl+Shift+Left/Right while an input in either pane has focus. With a `Key`, app state holds the first pane's share, so a screen keeps its ratio when a binding rebuilds it. The frame is after `drag:24,2,36,2`.

```powershell
New-ConsoleSplitter -Key Panes -Ratio 0.3 -First {
    New-ConsolePanel -Title Servers -Content { New-ConsoleSelect -Key Server -Choices WEB01, SQL01, FS01 }
} -Second {
    New-ConsolePanel -Title Notes -Content { 'WEB01 serves the public site.' }
}
```

### Accordion and Collapsible

A `Collapsible` is a section with a header row; Enter, Space or a click folds it, and its value is whether it is open. While it is folded the header shows its `Summary`, and the inputs inside leave the focus order. An `Accordion` keeps one section open, and `+` and `-` open or fold them all. The frame is after Tab and Enter: Parameters opens and Synopsis folds.

```powershell
New-ConsoleAccordion -Content {
    New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Lists servers and their roles.' }
    New-ConsoleCollapsible -Key Parameters -Summary '(2)' -Content { '-Name    server name or wildcard' }
}
```

### Outline and Breadcrumb

An `Outline` lists entries with levels; Enter or a click makes an entry current, and an entry with a `Target` opens that control. A `Breadcrumb` given the outline shows where you are, after its `Ancestors`. The frame is after Down, Down, Enter: Linux is current and the breadcrumb reads Guide › Install › Linux.

```powershell
$sections = New-ConsoleOutline -Key Sections -SelectedIndex 0 -Items @(
    'Install'
    @{ Title = 'Linux'; Level = 2 }
)
New-ConsoleBreadcrumb -Key Where -Ancestors 'Guide' -Outline $sections
```

### Tabs

Pages under a strip of titles. Left/Right or Ctrl+PageDown/PageUp change page, a number key opens that page, and a click on a title opens it. Each page keeps its own state and focus while another page is showing, and the value of `Key` is the active page's title. The frame is after a click on Disks.

```powershell
New-ConsoleTabs -Key Page -Content {
    @{ Title = 'Overview'; Content = { 'SQL01 · Database · Warsaw' } }
    @{ Title = 'Disks'; Content = { 'C:  41% used'; 'D:  97% used' } }
}
```

### ChartView with LinkKey

`ChartView` puts a line chart in an app as a focusable control: Left/Right or a click moves the crosshair and the row above the plot reads every series there, a number key or a click on the legend hides a series, `+`/`-` or the wheel zoom and Home shows the whole range. Give a chart and a `DataGrid` the same `LinkKey` and moving one moves the other: the state key holds the position of the selected point or row. The frame is after Down four times in the grid; the crosshair followed to 09:20.

```powershell
New-ConsoleDataGrid -Key Samples -InputObject $samples -LinkKey Sample
New-ConsoleChartView -Key Cpu -Chart $chart -PointLabels $times -LinkKey Sample -Height 9
```

### TreeSelect

An object tree inside an app, with the explorer's budgets and safe member access. Right expands and Left folds, typing filters the open branches, and Enter or a double-click opens a row as its own tree; Backspace goes back. Its value is the selected node, whose `Path` the line under the tree prints. The frame is after Right, Down×4, Right, `sql`, a double-click on SQL11 and Down twice. The [interactive page](/interactive/#tree) plays the whole run.

```powershell
New-ConsoleTreeSelect -Key Tree -Label Servers -InputObject $estate -RootPath '$estate' -Height 10
New-ConsoleBinding -Content {
    param($State)
    New-ConsoleText -Text "Selected: $($State.Tree.Path)"
}
```

### The find bar

`LogViewport`, a multiline text input and the document pager have a find bar. Press `/` in a log or the pager, or Ctrl+F in a text input where `/` is typing, then type: every match is highlighted and the bar counts them. Enter and Shift+Enter move to the next and previous match and Escape closes the bar; in a log or the pager the matches stay highlighted, so `n` and `N` keep moving, and a second Escape clears them. A query with a capital letter matches case. The frame is after `/`, `timeout` and Enter.

```powershell
Show-ConsoleApp -Content {
    New-ConsoleLogRegion -WidgetId deploy.log -NoElapsed -Height 8 -Content { $lines }
}
```

Inside `Show-ConsoleApp` content, `New-ConsoleLogRegion` makes the retained, scrollable log (`LogViewport` in C#, `Ui.LogViewport()` in fluent C#), and `Write-ConsoleLogEntry -WidgetId deploy.log` adds lines while the app runs. `Watch-ConsoleLog` and a multiline text input (`New-ConsoleTextInput -Multiline`) have the same find bar.

## Variations

- Put any part inside another: a splitter of an outline and an accordion is the incident workbench's runbook, and tabs hold whole screens.
- Pin a value at start: `-Ratio`, `-Expanded`, `-SelectedIndex` and `-Value` (or the C# properties of the same names) set the opening state.
- Read the parts from code: each `Key` is a field of `$State` in a binding or callback, so a line can follow the selected tab, outline entry or tree node.
- Turn the mouse off for one app with `Show-ConsoleApp -NoMouse`; every pointer action still has its key.

## Related

[Interactive apps](/docs/interactive-apps/) covers app state, bindings and screens. The [log tail demo](/demos/log-tail/) uses the text input's find bar after a live run, and [test, record and drive apps](/docs/testing/) runs any of these on a headless terminal.

## Next

Put the parts together in the [incident workbench](/demos/incident-workbench/), the [chart and table](/demos/chart-table/), [help sections](/demos/help-sections/) and [settings tabs](/demos/settings-tabs/) demos, then drive a tree, a grid and a wizard by keys and clicks on [prompts, menus and forms](/interactive/).
