---
title: Diagrams, zero to hero
description: Draw terminal diagrams in C#, fluent C# or PowerShell, one idea at a time, from three boxes to live values, request walkthroughs, hierarchies and Mermaid.
slug: diagrams
order: 18
group: apps
summary: "Nine steps from three boxes and two arrows to groups, status, data, live values, walkthroughs, hierarchies and Mermaid."
apis: [Diagram, Ui.Diagram, New-ConsoleDiagram, New-ConsoleDiagramNode, New-ConsoleDiagramLink, New-ConsoleDiagramGroup, Show-ConsoleDiagram]
chips: [Diagrams, Mermaid, C#, PowerShell]
card: diagram-4-120
---

A diagram should take a minute to type. Each step below adds one idea to the last, and each frame is what the program beside it prints, in C#, with the fluent builder or in PowerShell. The core package draws every diagram as a tree and needs nothing else; with ConsoleMonster.Graphics registered, as here, you get boxes, routed connections and groups, laid out by ChartForgeX.

The PowerShell commands follow PSWriteHTML's diagrams: `New-ConsoleDiagramNode -Label 'A' -To 'B'` is the whole first lesson, and `New-ConsoleDiagramLink` and `New-ConsoleDiagramGroup` come when you need them.

## 1. Three boxes, two arrows

A node can point at other nodes with `To`.

{{< cm-try key="level-1" ids="diagram-1-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 2. Kinds and labels

`Kind` says what a node is (and picks its icon in images and HTML). A link of its own, `New-ConsoleDiagramLink` or `DiagramLink`, can carry a label.

{{< cm-try key="level-2" ids="diagram-2-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 3. Groups

A group is a block around its nodes, the way a panel wraps content. Groups can nest.

{{< cm-try key="level-3" ids="diagram-3-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 4. Status

Status uses the same words as every other widget: `Pass`, `Warn`, `Fail`, `Info` and `Pending`. Put what is wrong in `Value`. A node without a status shows no mark, the legend appears only when something isn't fine, and **Needs attention** under the diagram lists every node and link that isn't, with its full text.

{{< cm-try key="level-4" ids="diagram-4-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 5. From a list

You rarely type every link. Nodes you never declared are created from the links, so a list of rows is enough: `Diagram.FromLinks` in C#, and in PowerShell a hashtable per row splatted into `New-ConsoleDiagramLink`. Rows with `From`, `To`, `Label`, `Status` and `Group` columns can also be piped straight into `Show-ConsoleDiagram`, for example from `Import-Csv`.

{{< cm-try key="level-5" ids="diagram-5-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 6. Live values

Give the diagram a `WidgetId` and update it while it shows; values and statuses change, the layout stays put. This frame is one moment of a ping monitor. The live version wraps the same diagram in a dashboard:

```powershell
$diagram = New-ConsoleDiagram -WidgetId ping -Title 'Ping monitor · every 5 s' { <# the nodes above #> }
Invoke-ConsoleDashboard $diagram {
    while ($true) {
        foreach ($name in 'Router', 'DNS', 'VPN', 'contoso.com') {
            $ping = Test-Connection $name -Count 1 -TimeoutSeconds 1 -ErrorAction Ignore
            $status = if (-not $ping) { 'Fail' } elseif ($ping.Latency -gt 100) { 'Warn' } else { 'Pass' }
            $value = if ($ping) { "$($ping.Latency) ms" } else { 'timeout' }
            Update-Console ping -Key $name -Value $value -Status $status
        }
        Start-Sleep -Seconds 5
    }
}
```

{{< cm-try key="level-6" ids="diagram-6-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 7. A request walkthrough

A use case walks a request through the diagram, step by step: requests, replies, messages nobody waits for and calls that fail. The active step lights up and the panel underneath lists the steps. In an interactive console, `Show-ConsoleDiagram` or `diagram.Run()` lets Space play it and the arrow keys step through it.

{{< cm-try key="level-7" ids="diagram-7-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 8. Hierarchy

Links that form a tree are a hierarchy: an org chart, a folder tree, a dependency tree. `Direction = Down` lays it out top to bottom.

{{< cm-try key="level-8" ids="diagram-8-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics" >}}

## 9. Mermaid

A diagram you already wrote in Mermaid draws as it is. In a Markdown document a ```` ```mermaid ```` fence becomes a diagram when Graphics is registered, and stays readable code when it isn't. The PowerShell module bundles Graphics, so `Show-Console -View Markdown` draws it.

{{< cm-try key="level-9" ids="diagram-9-120" project="Diagrams" packages="ConsoleMonster,ConsoleMonster.Graphics,ConsoleMonster.Markdown" >}}

A Mermaid file on its own, or a Markdown file with one Mermaid fence, loads like any other diagram file:

```powershell
Show-ConsoleDiagram -Path ./network.mmd -StaticSnapshot
```

Flowcharts (`flowchart` and `graph`, with subgraphs as groups), `classDiagram`, `stateDiagram`, `erDiagram`, `architecture-beta`, `mindmap`, C4 and `requirementDiagram` draw as diagrams. Sequence, pie, Gantt, timeline, gitGraph and the other chart types are not drawn in the terminal: in Markdown they stay code, and `Show-ConsoleDiagram` says which types it can draw.

## Next

- [Architecture diagrams](/docs/topology/): load ChartForgeX architecture files and play their use cases.
- [Inline images](/docs/inline-images/): chart renders and pictures in terminal pixels.
- [Live sessions](/docs/live-sessions/): how a diagram becomes part of a running dashboard.
