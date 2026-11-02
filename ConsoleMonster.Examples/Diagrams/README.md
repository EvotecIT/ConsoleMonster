# Diagram examples

Build a network, application flow or team hierarchy with the core
`ConsoleMonster.Widgets.Diagram` model. Each of the eight examples has an object
initializer, a fluent C# version and a
[PowerShell script](../../Module/Examples/Diagrams/).

| Level | Example | Adds |
| --- | --- | --- |
| 1 | Hello | Laptop, router and internet connections |
| 2 | Labels | Node subtitles and connection labels |
| 3 | Groups | Office, DMZ and cloud groups |
| 4 | Status | Node and link health with an attention list |
| 5 | WebApp | Application, data and background processing paths |
| 6 | LivePing | Separate subtitles, readings and health |
| 7 | UseCase | Checkout requests, replies and payload fields |
| 8 | OrgChart | A team hierarchy |

From the repository root, run a core example as a tree snapshot:

```sh
dotnet run --project ConsoleMonster.Examples -f net8.0 -- diagram 1
dotnet run --project ConsoleMonster.Examples -f net8.0 -- diagram 6 fluent
```

Choose any level from 1 to 8. Omit `fluent` for the initializer version. The core
runner uses only ConsoleMonster; it does not register a Graphics renderer.

The extensions runner registers Graphics at startup and opens the same example
as a topology with keyboard navigation and use case playback:

```sh
dotnet run --project ConsoleMonster.Examples.Extensions -f net8.0 -- diagram 1
dotnet run --project ConsoleMonster.Examples.Extensions -f net8.0 -- diagram 7 fluent
dotnet run --project ConsoleMonster.Examples.Extensions -f net8.0 -- diagram --capture ./Artefacts/DiagramExamples
```

`--capture` writes text, SVG and PNG files for all eight levels in both C# forms,
plus narrow status and reading views. Without an interactive terminal, the
Graphics runner prints a static snapshot.

## One model, three construction surfaces

For an application, install `ConsoleMonster` and construct a `Diagram` directly:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var diagram = new Diagram("Home network") {
    Nodes = {
        new DiagramNode("Laptop") { Kind = DiagramNodeKind.Workstation },
        new DiagramNode("Router") {
            Kind = DiagramNodeKind.Gateway,
            Subtitle = "192.168.1.1",
            Value = "1 ms"
        }
    },
    Links = {
        new DiagramLink("Laptop", "Router") { Label = "Wi-Fi" }
    }
};
Terminal.Write(diagram);
```

The fluent form produces the same widget through `.Build`:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var diagram = Ui.Diagram("Home network")
    .Node("Laptop", n => n.Kind(DiagramNodeKind.Workstation))
    .Node("Router", n => n.Kind(DiagramNodeKind.Gateway)
        .Subtitle("192.168.1.1").Value("1 ms"))
    .Link("Laptop", "Router", l => l.Label("Wi-Fi"))
    .Build;
Terminal.Write(diagram);
```

In PowerShell, `New-ConsoleDiagram` returns the core widget. The module bundles
Graphics and registers its renderer for diagram commands:

```powershell
Import-Module ConsoleMonster

$diagram = New-ConsoleDiagram -Title 'Home network' {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Subtitle '192.168.1.1' -Value '1 ms'
    New-ConsoleDiagramLink 'Laptop' 'Router' -Label 'Wi-Fi'
}
$diagram | Show-ConsoleDiagram
$diagram | Show-ConsoleDiagram -AsString -Width 100
```

Use `New-ConsoleDiagramGroup` to nest nodes and groups, and
`New-ConsoleDiagramUseCase` with `New-ConsoleDiagramStep` to describe interactions.
For example, a step can use `-Reply`, `-Failed`, or an explicit `-Kind`, and
`-Payload @{ sku = 'MUG-01'; qty = 2 }` supplies fields for the active step.

A node's label is its identity unless `Id` is set. Connections refer to node
identities; an undeclared endpoint creates a node labelled with that identity.
Use `To` on a node for unlabelled outgoing connections, or explicit links when
you need labels, health, stroke style or arrows at both ends.

For connection data, `Diagram.FromLinks(rows, row => row.Source,
row => row.Destination)` builds a core model. PowerShell accepts property names
or selectors using `$_`:

```powershell
$rows = @(
    [pscustomobject]@{ Source = 'Laptop'; Destination = 'Router'; Protocol = 'Wi-Fi' }
    [pscustomobject]@{ Source = 'Router'; Destination = 'Internet'; Protocol = 'WAN' }
)
$rows | New-ConsoleDiagram -Title 'Home network' -From Source -To Destination -Label Protocol |
    Show-ConsoleDiagram -StaticSnapshot
```

Run a complete PowerShell example from the repository root after importing the
module:

```powershell
./Module/Examples/Diagrams/Example-Diagram-6-LivePing.ps1 |
    Show-ConsoleDiagram -AsString -Width 120
./Module/Examples/Diagrams/Example-Diagram-7-UseCase.ps1 |
    Show-ConsoleDiagram -Step 3 -StaticSnapshot
```

## Readings, health and live updates

`Subtitle` is the second node line and `Value` is the independent third line.
An update to a reading leaves the subtitle intact. Nodes and links use the core
`StatusKind` values. By default, **Needs attention** lists every node and link
whose status is not `Pass`, with full labels, subtitles and readings at narrow
widths as well. It is absent when everything passes.
Hide it with `ShowSummary = false`, fluent `.Summary(false)`, or PowerShell
`-NoSummary` on `New-ConsoleDiagram` or `Show-ConsoleDiagram`.

Configure the model, its collections and fluent settings before sharing the
widget with a session. Use a thread-safe handle to post updates from worker
threads:

```csharp
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

using var session = new LiveSession(diagram, new LiveSessionOptions());
session.Start();
var handle = diagram.Handle(session);
handle.Value("Router", "180 ms", StatusKind.Warn);
```

Direct collection edits are sequential setup operations. Handles also post
scenario, step, play, pause and focus changes when Graphics is registered.
The C# step index is zero-based; PowerShell `Show-ConsoleDiagram -Step` is
one-based. Level 6 supplies sample readings; it does not run network probes.

## Choose the renderer

Core stays dependency-free and draws a grouped tree when no Graphics provider
is registered. Install `ConsoleMonster.Graphics` for topology layout, keyboard
navigation, playback, file loading and chart import. Call
`DiagramRendererProvider.Register()` once at application startup, before
creating diagrams, on every target framework. This includes .NET Framework
4.7.2 and .NET Standard 2.0 consumers: referencing the package alone does not
load the assembly or register the provider.

After registration, `Diagram.Load(path, diagramId)` loads a supported file, and
`Diagram.From(chart)` imports a ChartForgeX `TopologyChart` with `TChart` inferred
by the compiler. These are provider factories on the core model; core does not
reference ChartForgeX. See the [Graphics guide](../../ConsoleMonster.Graphics/README.md#diagrams-and-use-case-playback)
for startup registration, file loading, live updates and keyboard controls.
