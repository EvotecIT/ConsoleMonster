# ConsoleMonster.Graphics

Export ConsoleMonster widgets, frames and recordings as PNG, GIF and APNG with
ChartForgeX. MP4 uses an installed `ffmpeg`; it is never a package dependency.
The core ConsoleMonster package stays dependency-free.

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

JetBrains Mono is embedded under the SIL Open Font License; see
`licenses/JetBrainsMono-OFL.txt`. Installed font and emoji/CJK fallbacks depend
on the host. Use the embedded default and fixed inputs for repeatable slides.
