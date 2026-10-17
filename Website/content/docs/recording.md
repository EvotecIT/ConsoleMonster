---
title: Recording and replay
description: Record a live session as JSON lines, replay it into the same dashboard, and export widgets, frames and recordings to HTML, SVG, text and asciicast.
slug: recording
order: 6
---

A live session can write every applied event to a JSON-lines file: `LiveSessionOptions.RecordPath` or `RecordWriter` in C#, `Start-ConsoleDashboard -RecordPath` in PowerShell. A recording holds updates, not pictures, so it is played back into the widget tree it was recorded with.

```json
{"consolemonster":"recording","version":1}
{"t":1,"target":"dashboard","op":"status","text":"discovering forest","state":"running"}
{"t":1,"target":"phases","op":"state","key":"Discover","state":"running"}
```

These are the first lines of the recording behind the [live dashboards page](/live/), which you can [download](/recordings/testimox-run.jsonl). `LiveRecorder` writes the format and `LiveRecordingReader` reads it, with no dependencies.

## Replay

```csharp
using (StreamReader reader = File.OpenText("run.jsonl")) {
    LiveSession.Replay(dashboard, reader, new LiveSessionOptions(), speed: 1.5);
}
```

`Start-ConsoleReplay` does the same in PowerShell. The replayed clock follows the recorded timeline, so header clocks and spinners move as they did.

## Export

Any widget, rendered frame or live-session snapshot can be saved as a self-contained file:

```csharp
string html = ConsoleExporter.ToHtml(widget, new ExportOptions { Width = 120, Title = "Nightly run", WindowChrome = true });
string svg = ConsoleExporter.ToSvg(session);
string text = ConsoleExporter.ToText(frame);
string player = Recording.ExportHtml(dashboard, File.OpenText("run.jsonl"));
```

```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path .\services.html
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Destination .\run.svg
```

- **HTML** pages carry a dark and a light rendering and show the one the viewer prefers. Glyphs a fallback font could draw at another width are boxed to their cell, and block elements and braille are painted as cell backgrounds.
- **SVG** draws box drawing, block and braille characters as shapes; other glyphs stay selectable text.
- **Recordings** export to an HTML player with scrubbing, an animated SVG using CSS keyframes only, or asciicast v3.
- **Links** become links for http, https, mailto and ftp only, and nothing is fetched from the network.

This site does not use the exporters: its own site tool converts frames and VT streams with a small converter of its own, so the pages share one stylesheet and follow the site theme. The [site tool's notes](https://github.com/EvotecIT/ConsoleMonster/blob/master/Website/AGENTS.md) describe how.

## Testing output

The helpers ConsoleMonster's own tests use are public: `TestTerminal.RenderText(widget, width: 80, glyphs: GlyphTier.Ascii)` renders at a fixed size, tier and culture, `GoldenFile.Match` compares with a golden file and shows the first differing line, and `ScreenCapture` with scripted keys captures prompts, menus and forms. In Pester, `Assert-ConsoleOutput -GoldenPath` does the same.