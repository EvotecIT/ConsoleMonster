---
title: Recording and replay
description: Record a live session as JSON lines, replay it into the same dashboard, and export widgets, frames and recordings to HTML, SVG, text and asciicast.
slug: recording
order: 8
group: live
card: app
chips: [JSON lines, Replay, Export]
summary: "JSON-lines recordings, replays, exports and test helpers."
---

A live session can write every update it applies to a JSON-lines file: `Start-ConsoleDashboard -RecordPath` in PowerShell, `LiveSessionOptions.RecordPath` or `RecordWriter` in C#. A recording holds updates, not pictures, so it is played back into the dashboard it was recorded with: `Start-ConsoleReplay` (or `Show-ConsoleReplay`, which lets you rewind) in PowerShell, `LiveSession.Replay` in C#.

{{< cm-demo id="record" heading="h2" >}}

## The format

```json
{"consolemonster":"recording","version":1}
{"t":1,"target":"dashboard","op":"status","text":"discovering forest","state":"running"}
{"t":1,"target":"phases","op":"state","key":"Discover","state":"running"}
```

These are the first lines of the recording behind the [live dashboards page](/live/), which you can [download](/recordings/assessment-run.jsonl) and replay with the program shown there. `LiveRecorder` writes the format and `LiveRecordingReader` reads it, with no dependencies.

## Export

Any widget, frame or recording can be saved as a self-contained file. In PowerShell:

```powershell
Import-Module ConsoleMonster

Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path ./services.html
```

`Export-ConsoleRecording` takes a dashboard and a recording, as in the demo above, with `-Format Html`, `Svg` or `Asciicast`. In C#, `ConsoleExporter.ToHtml(widget, options)`, `ToSvg` and `ToText` export a widget or a frame, and `Recording.ExportHtml(dashboard, reader)`, `ExportSvg` and `ExportAsciicast` export a recording:

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;

Table table = new Table().WithTitle("Nightly run").AddColumn("Check").AddColumn("Result");
table.AddValues("SPF", "pass");
table.AddValues("DMARC", "fail");
File.WriteAllText("nightly.html", ConsoleExporter.ToHtml(table, new ExportOptions { Width = 60, WindowChrome = true }));
```

- **HTML** pages carry a dark and a light rendering and show the one the viewer prefers. Glyphs a fallback font could draw at another width are boxed to their cell, and block elements and braille are painted as cell backgrounds.
- **SVG** draws box drawing, block and braille characters as shapes; other glyphs stay selectable text.
- **Recordings** export to an HTML player with scrubbing, an animated SVG using CSS keyframes only, or asciicast v3.
- **Links** become links for http, https, mailto and ftp only, and nothing is fetched from the network.

The players on this site are ConsoleMonster's own player, the one `Export-ConsoleRecording -Format Html` writes.

## Targets

Every write, live session, prompt and status helper goes to an `ITerminalTarget`: a profile, a theme, a writer and an input source. `ConsoleTarget.Instance` is the process console; `TestTerminal.Target(...)` returns a `RecordingTarget` for tests, `WriterTarget` writes append-only lines to a `TextWriter`, and `NullTarget.Instance` discards output. `using (Terminal.Use(target)) { ... }` sends everything in the block to a target, and the scope flows with `async` calls. In PowerShell, `Use-ConsoleTarget` runs a script block with ConsoleMonster output going to a file (`-Path`), a writer or a target, or returned as a string.

## Testing output

The helpers ConsoleMonster's own tests use are public: `TestTerminal.RenderText(widget, width: 80, glyphs: GlyphTier.Ascii)` renders at a fixed size, tier and culture, `GoldenFile.Match` compares with a golden file and shows the first differing line, and `ScreenCapture` with scripted keys captures prompts, menus and forms. In Pester, `Assert-ConsoleOutput -GoldenPath` does the same.
