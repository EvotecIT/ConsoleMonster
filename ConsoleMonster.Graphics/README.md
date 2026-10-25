# ConsoleMonster.Graphics

Export ConsoleMonster widgets, frames and recordings as PNG, GIF and APNG with
ChartForgeX. All three formats run in-process without an external executable.
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

GIF is the animated slide format. `Slide16x9` renders directly at 1920×1080;
insert it at the slide's size without enlarging a documentation capture. `Docs`
uses the terminal's natural pixel size. Leading empty screens are skipped so the
first frame works as a poster. The final frame holds for two seconds by default
(`Playback.HoldLastFrame`), and `LoopCount = 0` repeats forever; set it to `1` to
play once. `IdleLimit` caps other unchanged-screen delays.

The encoder preserves exact source colours when a frame fits in its palette.
Richer frames reserve common colours and quantise without error diffusion, so
text has no dither speckles; gradients can show gentle bands. Unchanged screens
share a delay, and changed regions use GIF sub-frames with retained disposal.
APNG remains available for full-colour playback in applications that support it.

JetBrains Mono is embedded under the SIL Open Font License; see
`licenses/JetBrainsMono-OFL.txt`. Installed font and emoji/CJK fallbacks depend
on the host. Use the embedded default and fixed inputs for repeatable slides.
