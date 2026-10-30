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

## Topology diagrams and use case playback

`TopologyDiagram` draws a ChartForgeX topology in the terminal: boxes, routed
edges, dashed group boundaries and a status legend. ChartForgeX lays the diagram
out and projects it into cells (`TopologyCellLayout`); the widget only styles and
draws those cells, so the terminal shows the same diagram as the SVG and PNG
exports. A diagram wider than the terminal drops subtitles, then shortens labels,
then drops edge labels until it fits.

When the topology has use cases (ChartForgeX scenarios, for example from a
`chartforgex architecture v1` file), the widget shows a player under the diagram:
the steps, the active step highlighted on the diagram, and its request or
response payload.

```csharp
using ConsoleMonster.Graphics;

var diagram = new TopologyDiagram(chart);   // a ChartForgeX TopologyChart
diagram.Run();
```

Keys: `p` or Space play and pause, `n` or Right next step, `b` or Left previous
step, `r` or Home restart, `e` or End last step, `x` clear the step, `s` or Tab
next use case (Shift+Tab previous), Up and Down focus a node, `d` show or hide its
details, Esc quit.

For dashboards, keep the diagram in a live session and update node values and
statuses from any thread:

```csharp
using ChartForgeX.Topology;
using ConsoleMonster.Graphics;
using ConsoleMonster.Live;

var diagram = new TopologyDiagram(chart).SelectScenario(null);
using var session = new LiveSession(diagram, new LiveSessionOptions());
session.Start();
var nodes = diagram.Handle(session);
nodes.Value("dc03", "lag 42 min", TopologyHealthStatus.Critical);
```

Nodes keep room for values of up to eight cells, so typical values do not move
the layout; a longer value lays the diagram out again once.

Full, Basic and ASCII glyph tiers are covered: Basic swaps rounded corners,
dashes and heavy lines for their WGL4 equivalents. `diagram.ToPng()` renders the
diagram with the active step through ChartForgeX, for terminals that can show
images; live values and focus stay in the cell drawing.

A small diagram keeps its natural size, about one cell per ten layout pixels,
instead of stretching across a wide terminal.

From PowerShell, `Show-ConsoleDiagram` reads a Markdown file with ChartForgeX
fences, or a plain architecture file, and opens the same view with its keys.
Outside an interactive console it prints one static view:

```powershell
Show-ConsoleDiagram -Path ./ad-replication.md
Show-ConsoleDiagram -Path ./ad-replication.md -Scenario replicate -Step 6 -StaticSnapshot
```

JetBrains Mono is embedded under the SIL Open Font License; see
`licenses/JetBrainsMono-OFL.txt`. Installed font and emoji/CJK fallbacks depend
on the host. Use the embedded default and fixed inputs for repeatable slides.

[ConsoleMonster](https://consolemonster.dev/) · [Recording and replay](https://consolemonster.dev/docs/recording/) ·
[Add-ons](https://consolemonster.dev/docs/add-ons/)
