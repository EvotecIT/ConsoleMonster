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

JetBrains Mono is embedded under the SIL Open Font License; see
`licenses/JetBrainsMono-OFL.txt`. Installed font and emoji/CJK fallbacks depend
on the host. Use the embedded default and fixed inputs for repeatable slides.

[ConsoleMonster](https://consolemonster.dev/) · [Recording and replay](https://consolemonster.dev/docs/recording/) ·
[Add-ons](https://consolemonster.dev/docs/add-ons/)
