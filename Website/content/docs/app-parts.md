---
title: App parts, one by one
description: Splitter, Accordion, Outline and Breadcrumb, Tabs, ChartView with LinkKey and the find bar, each as a small app in PowerShell and C# with the screen it drew.
slug: app-parts
order: 12
group: apps
summary: "Panes, folding sections, outlines, tabs, linked charts and the find bar, each on its own."
apis: [New-ConsoleSplitter, New-ConsoleAccordion, New-ConsoleCollapsible, New-ConsoleOutline, New-ConsoleBreadcrumb, New-ConsoleTabs, New-ConsoleChartView, LogViewport]
chips: [Apps, Mouse, PowerShell, C#]
card: app-chartview
---

The [incident workbench](/demos/incident-workbench/) and the other app demos are built from a handful of parts. Each part below is a whole app on its own: the PowerShell script, the C# program and the screen it drew after the keys or clicks under the frame. Every part works with the keyboard and the mouse, lists its keys in the footer, and keeps its value in app state under its `Key`, so a binding or a callback can read it. Run any of them with `Install-Module ConsoleMonster` and the script, or `dotnet add package ConsoleMonster` and the program as `Program.cs`.

## Splitter

Two panes side by side, or top and bottom with `-Vertical` (`Vertical = true`). Drag the divider, or press Ctrl+Shift+Left/Right while an input in either pane has focus. With a `Key`, app state holds the first pane's share, so a screen keeps its ratio when a binding rebuilds it.

{{< cm-frame id="app-splitter" caption="drag:24,2,36,2 then Escape: the divider moved twelve columns to the right." >}}

## Accordion and Collapsible

A `Collapsible` is a section with a header row; Enter, Space or a click folds it, and its value is whether it is open. While it is folded the header shows its `Summary`, and the inputs inside leave the focus order. An `Accordion` keeps one section open, and `+` and `-` open or fold them all.

{{< cm-frame id="app-accordion" caption="Tab, then Enter: Parameters opens and the accordion folds Synopsis." >}}

## Outline and Breadcrumb

An `Outline` lists entries with levels; Enter or a click makes an entry current, and an entry with a `Target` opens that control. A `Breadcrumb` given the outline shows where you are, after its `Ancestors`, and a click on a segment goes back to it.

{{< cm-frame id="app-outline" caption="Down, Down, Enter: Linux is current and the breadcrumb reads Guide › Install › Linux." >}}

## Tabs

Pages under a strip of titles. Left/Right or Ctrl+PageDown/PageUp change page, a number key opens that page, and a click on a title opens it. Each page keeps its own state and focus while another page is showing, and the value of `Key` is the active page's title.

{{< cm-frame id="app-tabs" caption="A click on the Disks title." >}}

## ChartView with LinkKey

`ChartView` puts a line chart in an app as a focusable control: Left/Right or a click moves the crosshair and the row above the plot reads every series there, a number key or a click on the legend hides a series, `+`/`-` or the wheel zoom and Home shows the whole range. Give a chart and a `DataGrid` the same `LinkKey` and moving one moves the other: the state key holds the position of the selected point or row.

{{< cm-frame id="app-chartview" caption="Down four times in the grid: the crosshair follows to 09:20." >}}

## The find bar

`LogViewport`, a multiline text input and the document pager have a find bar. Press `/` in a log or the pager, or Ctrl+F in a text input where `/` is typing, then type: every match is highlighted and the bar counts them. Enter and Shift+Enter move to the next and previous match and Escape closes the bar; in a log or the pager the matches stay highlighted, so `n` and `N` keep moving, and a second Escape clears them. A query with a capital letter matches case.

`LogViewport` has no cmdlet, so the script takes its type from the module's assembly; the C# program uses the class.

{{< cm-frame id="app-find" caption="/ \"timeout\" Enter: the second of two matches, with both highlighted." >}}

The [log tail demo](/demos/log-tail/) uses the text input's find bar after a live run, in PowerShell, C# and C# fluent.

## Next

Put the parts together in the [incident workbench](/demos/incident-workbench/), the [chart and table](/demos/chart-table/), [help sections](/demos/help-sections/) and [settings tabs](/demos/settings-tabs/) demos. [Interactive apps](/docs/interactive-apps/) covers app state, bindings and screens, the [prompts, menus and forms](/interactive/) page drives a tree, a grid and a wizard by keys and clicks, and [test, record and drive apps](/docs/testing/) runs any of these on a headless terminal.
