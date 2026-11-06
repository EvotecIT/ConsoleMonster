# ConsoleMonster.Graphics

Export ConsoleMonster widgets, frames and recordings as PNG, GIF and APNG with
ChartForgeX. All three formats run in-process without an external executable.
The core ConsoleMonster package stays dependency-free.

```bash
dotnet add package ConsoleMonster.Graphics
```

In PowerShell the module bundles this add-on: `Export-ConsoleRecording -Format Gif` and `-Format Png` use it.

Slide presets share a centred, rounded window on a theme, solid or gradient
background. Content fitting trims unused rows and columns in a still, keeping two
trailing columns of padding. Animations reserve the widest and tallest content
throughout the clip. `GraphicsFit.Terminal` keeps the full grid.
Both modes fit the window inside a 6% safe inset, up to a 72-pixel font by default.

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;

var recording = SessionRecording.Read("talk.cast");
recording.SaveGraphics("talk.gif", GraphicsFormat.Gif,
    new GraphicsExportOptions { Preset = GraphicsPreset.Slide16x9 });
recording.SavePng("slide.png", new GraphicsExportOptions {
    Preset = GraphicsPreset.Slide16x9
});
```

GIF is the animated slide format. `Slide16x9` renders directly at 1920×1080;
insert it at the slide's size without enlarging a documentation capture. `Docs`
uses the terminal's natural pixel size. Leading empty screens are skipped so the
first frame works as a poster; explicit opening holds are retained on that poster. The final frame holds for two seconds by default
(`Playback.HoldLastFrame`), and `LoopCount = 0` repeats forever; set it to `1` to
play once. `Playback.Pace` defaults to `RecordingPace.Normal`, adding bounded
reading time after output settles; `Slow` and `Fast` adjust that time. To keep
source timing, choose `RecordingPace.Source` and set `IdleLimit = TimeSpan.Zero`.
The idle cap never clips reading holds, marker holds or the final hold.

`Playback` also accepts `ChapterHold`, `TrimStart` / `TrimEnd` (source positions),
`TrimStartChapter` / `TrimEndChapter` (exclusive end), and `ChapterSpeeds` keyed by
unique chapter labels. Speed scales source and reading delays; explicit holds
keep their duration. These settings share the core timeline with HTML and SVG.
A running `SessionRecorder.AddMarker(TimeSpan.FromSeconds(3))` records an export
hold without sleeping. See the [recording guide](https://consolemonster.dev/docs/recording/#make-it-readable).

The encoder preserves exact source colours when a frame fits in its palette.
Richer frames reserve common colours and quantise without error diffusion, so
text has no dither speckles; gradients can show gentle bands. Unchanged screens
share a delay, and changed regions use GIF sub-frames with retained disposal.
APNG remains available for full-colour playback in applications that support it.

## Inline images

`ConsoleImage` draws a PNG, JPEG, GIF, BMP, TIFF or PPM image, or a ChartForgeX render, in the terminal at a size
in cells, keeping its aspect ratio. `Write()` uses Kitty graphics, iTerm2 inline images or Sixel when the terminal
supports them (`TerminalProfile.ImageProtocol`), half blocks on other terminals with 256 colours or more, and an
`[image: name WxH]` placeholder in redirected, CI and no-colour output. Inside layouts and live regions it draws
half blocks, so redraws stay exact.

```csharp
using ConsoleMonster.Graphics;

ConsoleImage image = ConsoleImage.FromFile("chart.png");
image.Width = 40;
image.Write();
```

In PowerShell, use `Show-ConsoleImage` and `New-ConsoleImage`. See [Inline images](https://consolemonster.dev/docs/inline-images/).

## Diagrams and use case playback

`ConsoleMonster.Widgets.Diagram` contains nodes, links, groups and use cases in
the dependency-free core package. Graphics adds boxes, routed connections,
group boundaries, keyboard navigation and use case playback through ChartForgeX.
Without Graphics, the same model renders as a grouped tree with connection and
status text.

Call `DiagramRendererProvider.Register()` once at application startup, before
creating or loading diagrams. This explicit registration is required on every
target framework, including .NET Framework 4.7.2 and .NET Standard 2.0 consumers.
A package reference alone does not load the Graphics assembly or register its
provider.

```csharp
using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register();

var diagram = Ui.Diagram("Home network")
    .Node("Laptop", n => n.Kind(DiagramNodeKind.Workstation))
    .Node("Router", n => n.Kind(DiagramNodeKind.Gateway)
        .Subtitle("192.168.1.1").Value("1 ms"))
    .Link("Laptop", "Router", l => l.Label("Wi-Fi"));
diagram.Run();
```

Object initializers and the fluent `Ui.Diagram()` surface configure the same
model. `Subtitle` is the second node line; `Value` is a separate third line for
readings or other live text. A value update retains the subtitle. Node and link
health use the core `StatusKind` enum. Omitted status has no marker. A legend appears only when a supplied
status is not `Pass`, and lists only supplied statuses. The **Needs attention** list retains problem details
for every node and link with a supplied non-`Pass` status. Set `ShowSummary = false` or use `.Summary(false)` to hide it.

Small diagrams keep their natural terminal size. The renderer chooses the tree when the terminal is below
20 columns, projected text would be lost, a group label is overwritten, or a forest does not preserve its
parent-to-child reading direction. Wider windows use topology as soon as the engine preserves those contracts.
The tree nests forests, keeps cross-links and cycles as references, and places subtitle and value beside each label.

Finish configuring the model and its collections before sharing it with a live
session. Use its thread-safe `DiagramHandle` for concurrent updates; direct
collection edits and fluent configuration are sequential setup operations.

### Load a file or import a chart

After registration, `Diagram.Load(path, diagramId)` reads a Markdown file with
ChartForgeX or Mermaid visual fences, a Mermaid file (`.mmd`) or a plain
architecture file. Mermaid flowcharts, class, state, ER, architecture, mindmap,
C4 and requirement diagrams draw; other Mermaid types are reported by name.
Registration also lets ConsoleMonster.Markdown draw Mermaid fences in documents.
The optional second
argument selects a diagram when the file contains several:

```csharp
using ConsoleMonster.Graphics;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register(); // once at application startup
Diagram diagram = Diagram.Load("ad-replication.md", "replication");
diagram.Run();
```

For an existing ChartForgeX `TopologyChart`, use `chart.ToDiagram()` with
`using ConsoleMonster.Graphics;`. This typed extension lives in Graphics and accepts
only a topology chart. Core has no ChartForgeX type or dependency. Keep the same
explicit startup registration for rendering other diagrams and loading files.
Finish configuring the source chart before importing it and do not mutate that chart afterwards. Configure the returned Diagram through its own model.

When a diagram has use cases, the view shows the steps, the active step
highlighted on the diagram, and its request or response payload.

`Run()` uses the keyboard view in a rich console. When output is redirected,
the session runs in CI, or interaction is unavailable, it prints one static
snapshot without reading keys. The selected scenario's `AutoPlay` setting
(the architecture markup's `autoplay` attribute) starts playback when the
keyboard view opens. A static snapshot retains the current step and does not
start autoplay.

Keys: `p` or Space play and pause, `n` or Right next step, `b` or Left previous
step, `r` or Home restart, `e` or End last step, `x` clear the step, `s` or Tab
next use case (Shift+Tab previous), Up and Down focus a node, and `d` show or hide
its details. When the diagram is taller than the viewport, Up and Down scroll
through it. The app footer shows the configured exit key (Esc by default).

For dashboards, keep the diagram in a live session and update node values and
statuses from any thread:

```csharp
using ConsoleMonster.Graphics;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DiagramRendererProvider.Register(); // once at application startup
var diagram = new Diagram("Replication") {
    Nodes = {
        new DiagramNode("dc03") { Subtitle = "Branch controller", Value = "lag 0 min" }
    }
};
using var session = new LiveSession(diagram, new LiveSessionOptions());
session.Start();
var nodes = diagram.Handle(session);
nodes.Value("dc03", "lag 42 min", StatusKind.Fail);
```

`diagram.Values` returns a read-only snapshot of non-null readings keyed by
node id. `diagram.Scenario` returns the selected core `DiagramUseCase`, or null
when no use case is selected.

Nodes reserve ten columns for live values from the first frame, so value
updates keep boxes and routes in place. Longer values are clipped in the box;
the focused details retain the full text.

Full, Basic and ASCII glyph tiers are supported: Basic swaps rounded corners,
dashes and heavy lines for their WGL4 equivalents. When a topology cannot fit
without losing labels, the widget falls back to the grouped tree.
`diagram.ToPng()` exports the current widget through Graphics, including its
active step and live values.

The PowerShell module bundles Graphics and registers the provider for diagram
commands. Build a model with `New-ConsoleDiagram` and its node, link, group,
use case and step cmdlets, or pass connection rows or a file to
`Show-ConsoleDiagram`. Outside an interactive console it prints one static view:

```powershell
Import-Module ConsoleMonster

New-ConsoleDiagram -Title 'Home network' {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation -To Router
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Subtitle '192.168.1.1' -Value '1 ms'
} | Show-ConsoleDiagram

Show-ConsoleDiagram -Path ./ad-replication.md
Show-ConsoleDiagram -Path ./ad-replication.md -Scenario replicate -Step 6 -StaticSnapshot
```

`-AsWidget` returns a widget for static display or live updates in
`Show-ConsoleApp`. The consumer handles keys when it needs diagram navigation
or playback. `-AsWidget` cannot be combined with `-AsString` or `-StaticSnapshot`.

Use `-NoSummary` on `New-ConsoleDiagram` or `Show-ConsoleDiagram` to hide the
attention list. See the [eight diagram examples](../ConsoleMonster.Examples/Diagrams/README.md)
for equivalent initializer, fluent and PowerShell versions, the core tree runner,
and the Graphics runner with text, SVG and PNG capture.

JetBrains Mono is embedded under the SIL Open Font License; see
`licenses/JetBrainsMono-OFL.txt`. Installed font and emoji/CJK fallbacks depend
on the host. Use the embedded default and fixed inputs for repeatable slides.

[ConsoleMonster](https://consolemonster.dev/) · [Recording and replay](https://consolemonster.dev/docs/recording/) ·
[Add-ons](https://consolemonster.dev/docs/add-ons/)
