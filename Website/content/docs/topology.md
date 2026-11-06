---
title: Architecture diagrams
description: Draw ChartForgeX architecture and topology diagrams in the terminal, play their use cases step by step and update node values live.
slug: topology
order: 18
group: apps
card: app
chips: [ChartForgeX, Use cases, Live values]
summary: "Architecture and request-flow diagrams in terminal cells, with use case playback."
---

A `Diagram` is nodes, links, groups and use cases, written in C#, with the fluent builder or with PowerShell commands. The core package draws it as a tree and needs nothing else. Add ConsoleMonster.Graphics and register it, and the same diagram is drawn with the ChartForgeX layout: boxes with status, routed connections with arrows, dashed groups and a legend, the same picture ChartForgeX renders as SVG, PNG or an HTML player.

```terminal
dotnet add package ConsoleMonster.Graphics
```

`DiagramRendererProvider.Register()` turns on the Graphics drawing once per process; `Diagram.Load(path)` then reads a ChartForgeX architecture file, as the demo below does.

{{< cm-demo id="topology" heading="h2" >}}

## Write one

Nodes, links, groups and use cases are the same model in C#, the fluent builder and PowerShell. Without Graphics registered, the core draws this as a tree:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var diagram = Ui.Diagram("Home network")
    .Node("Laptop", n => n.Kind(DiagramNodeKind.Workstation))
    .Node("Router", n => n.Kind(DiagramNodeKind.Gateway)
        .Subtitle("192.168.1.1").Value("1 ms"))
    .Link("Laptop", "Router", l => l.Label("Wi-Fi"));
Terminal.Write(diagram);
```

The PowerShell module bundles Graphics and registers it for the diagram commands:

```powershell
Import-Module ConsoleMonster

New-ConsoleDiagram -Title 'Home network' {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Subtitle '192.168.1.1' -Value '1 ms'
    New-ConsoleDiagramLink 'Laptop' 'Router' -Label 'Wi-Fi'
} | Show-ConsoleDiagram -StaticSnapshot
```

## Use cases

A use case is an ordered list of steps over the diagram: a request, its answer, a message nobody waits for, or a call that fails. The panel under the diagram lists the steps, the active one lights up on the diagram (answers point back, failures are red) and its payload is shown. In an interactive console:

| Key | Does |
|---|---|
| Space or P | Play or pause |
| Right or N, Left or B | Next and previous step |
| Home or R, End or E | First and last step |
| X | Clear the active step |
| Tab or S | Next use case |
| Up and Down | Move the focus between nodes |
| D | Details of the focused node |

Playback follows the session clock, so a resize or a slow frame never skips steps.

## From PowerShell

`Show-ConsoleDiagram` reads a Markdown file with ChartForgeX or Mermaid visual fences, a Mermaid `.mmd` file, or a plain architecture file ([which Mermaid types draw](/docs/diagrams/)). In an interactive console it opens the same view; in CI, redirected output or with `-StaticSnapshot` it prints one step and returns.

```powershell
Show-ConsoleDiagram -Path ./ad-replication.cfx
Show-ConsoleDiagram -Path ./ad-replication.cfx -Scenario sitelink -Step 4 -StaticSnapshot
Show-ConsoleDiagram -Path ./ad-replication.cfx -Scenario sitelink -Step 4 -Width 120 -AsString
```

Markup errors stop the command with the file, line and diagnostic code, for example `ad-replication.cfx:20:1 CFXA002 ...`.

## Live values

A diagram can be a live dashboard. Values and statuses change, the layout stays put, and nodes keep room for a short value; longer text shows in the focused node's details.

```csharp
using var session = new LiveSession(diagram, new LiveSessionOptions());
session.Start();
DiagramHandle handle = diagram.Handle(session);
handle.Value("dc03", "lag 42 min", StatusKind.Fail);
```

In PowerShell, give the diagram a `-WidgetId` and update it with `Update-Console -WidgetId <id> -Key dc03 -Value 'lag 42 min' -Status Fail`.

## Glyphs and width

The Full glyph set uses rounded and dashed boxes and heavy lines for the active step; Basic maps them to WGL4 box drawing; ASCII stays plain ASCII. A small diagram keeps its natural size in a wide terminal. In a terminal narrower than the full text, subtitles go first, then labels are shortened, then connection labels are dropped until it fits.
