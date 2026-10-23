---
title: Recording and replay
description: Record a whole terminal session, including native commands, to asciicast v2 and export an HTML player, animated SVG, text, Markdown or a final frame.
slug: recording
order: 8
group: live
card: app
chips: [Terminal sessions, Chapters, Export]
summary: "Whole-session recording, dashboard replay and local exports."
---

Record a whole terminal session with `Start-ConsoleRecording`, or make a demo from a script with `Invoke-ConsoleRecording`. Native commands, colours, progress redraws and terminal resizes become a local asciicast v2 file. Export it to ConsoleMonster's HTML player, an animated SVG, text, Markdown or a static final-frame SVG.

{{< cm-session-recording >}}

This is a real run of the [complete recording script](/examples/recording/record-session.ps1), made with `Invoke-ConsoleRecording`. The machine name is redacted, and the sample API key becomes `[REDACTED]` before the cast is written. Download the [HTML player](/recordings/session.html), [animated SVG](/recordings/session.svg), [asciicast v2](/recordings/session.cast), [text](/recordings/session.txt), [Markdown](/recordings/session.md) or [final frame](/recordings/session-final-dark.svg). Nothing is uploaded by the recorder.

## Record a shell

Install ConsoleMonster, then run this in a terminal:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster

Start-ConsoleRecording -Path ./deployment.cast -Name 'Deployment'
# You are now in the recorded child shell.
git --version
Add-ConsoleRecordingMarker 'Deploy'
Write-Host 'Deployment finished' -ForegroundColor Green
Stop-ConsoleRecording
# The child closes and the original shell returns.

Export-ConsoleRecording -Path ./deployment.cast -Destination ./deployment.html
```

Exiting the child shell also finalizes the recording. `-Shell pwsh`, `powershell`, `cmd` or `/bin/bash` chooses the shell. On Windows the default is the current PowerShell executable; on Linux and macOS it is the current PowerShell executable when called through the cmdlet. Windows needs ConPTY (Windows 10 version 1809 or later). Linux and macOS use `openpty` and a child session with a controlling terminal.

For `cmd` and Unix shells, type `exit` to finish. PowerShell children load the recording commands automatically. The `● REC` indicator appears in the window title. It is not added to recorded output.

Without `-Path`, filenames use the date, time and a unique suffix under the user's Documents/ConsoleMonster/Recordings folder. Use `-Folder` to choose another location. Existing casts and sidecars are never overwritten.

## Record a script and export it

Use an empty destination folder for each new run. The script runs in an isolated PowerShell child; caller variables and functions are not inherited. Save setup and any required data inside the script, or load them there.

```powershell
Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Inspect'
    git --version
    dotnet --version
    Add-ConsoleRecordingMarker 'Build'
    Write-Host 'Build complete' -ForegroundColor Green
} -Destination ./build-demo.html -Width 100 -Height 24 -Name 'Build demo'

# build-demo.cast is the native recording; build-demo.cast.json holds metadata.
Export-ConsoleRecording -Path ./build-demo.cast -Destination ./build-demo.svg
Export-ConsoleRecording -Path ./build-demo.cast -Destination ./build-demo.txt
Export-ConsoleRecording -Path ./build-demo.cast -Destination ./build-demo.md
Export-ConsoleRecording -Path ./build-demo.cast -FinalFrame -Theme Light `
    -WindowChrome -Destination ./build-demo-final.svg
```

The HTML player has play/pause, speed, scrubbing, copy text and a chapter list. `-Format PlayerData` writes the same player's JSON for embedding. `-Theme Dark` or `Light` uses the existing exporter palette. SVG animation uses CSS keyframes; final-frame SVG is static. Text and Markdown include the main screen's scrollback. Session casts remain asciicast v2, including output, marker and resize events.

## Redaction and limits

Input bytes are forwarded to the child but never stored. A password prompt that disables terminal echo therefore contributes no password text to the recording. Output patterns mask bearer tokens, common API-key prefixes and assignments such as `password=`, `api_key=` and `client_secret=`. Add regular expressions for your own identifiers:

```powershell
Invoke-ConsoleRecording {
    Write-Host 'account=sample-account'
    Write-Host 'api_key=synthetic-demo-value'
} -Destination ./redacted.html -Redact 'sample-account' `
    -IdleLimit 2 -MaxBytes 10485760 -TimeoutSeconds 600
```

Redaction runs before writing the cast or its chapter metadata. Patterns apply to complete output records, including secrets split across reads or colour codes. A record ends at a newline, carriage return or recording control. Records longer than 64 KiB without a boundary are suppressed; a pattern timeout masks the record. Patterns cannot recognize every possible secret, so review a recording before sharing it.

`-IdleLimit` compresses longer gaps; zero keeps original timing. `-MaxBytes` caps the cast, including its header. At the cap the cast stays valid and a warning appears; the child keeps running while further output is omitted. `-TimeoutSeconds` stops the recorded child after the specified time. All files stay local.

## Limited in-process fallback

When a host cannot create a pseudo-console, explicitly select `-InProcess`. It runs PowerShell in an in-process runspace and captures its host and pipeline output. The cast header and metadata state **in-process PowerShell output only**, and the command prints a warning. Native terminal behavior, cursor movement and resize handling need the pseudo-console mode.

```powershell
Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Summary'
    Write-Host 'PowerShell output only'
    Get-Date -Format 'yyyy-MM-dd'
} -InProcess -Destination ./summary.html
```

## C# API

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;

var options = new SessionRecordingOptions {
    Path = "session.cast",
    Shell = OperatingSystem.IsWindows() ? "powershell.exe" : "/bin/bash",
    Width = 100,
    Height = 24
};
options.Arguments.Add(OperatingSystem.IsWindows() ? "-Command" : "-c");
options.Arguments.Add("echo 'Recorded child output'");
using (var recorder = SessionRecorder.Start(options, Console.Write)) {
    recorder.AddMarker("Start");
    recorder.WaitForExit();
}
var recording = SessionRecording.Read("session.cast");
File.WriteAllText("session.html", recording.Export(ExportFormat.Html));
File.WriteAllText("session-final.svg", recording.ExportFinalSvg());
```

`SendInput` forwards text without storing it; `Resize` records a new terminal size. `WaitForExit` drains output and finalizes the cast and sidecar. `Stop` or disposal stops this recorder's child. `FinalFrame` returns a `CellBuffer` for the existing exporter. The library also targets .NET Standard 2.0 and .NET Framework 4.7.2; use `RuntimeInformation.IsOSPlatform` instead of `OperatingSystem.IsWindows` on those targets.

## Dashboard recordings

A live dashboard can write every update it applies to a JSON-lines file: `Start-ConsoleDashboard -RecordPath` in PowerShell, `LiveSessionOptions.RecordPath` or `RecordWriter` in C#. These recordings hold widget updates and replay into the same dashboard definition: `Start-ConsoleReplay` or `Show-ConsoleReplay` in PowerShell, `LiveSession.Replay` in C#.

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
- **Dashboard recordings** export to an HTML player with scrubbing, an animated SVG using CSS keyframes only, or asciicast v3. Whole terminal sessions use asciicast v2.
- **Links** become links for http, https, mailto and ftp only, and nothing is fetched from the network.

The players on this site are ConsoleMonster's own player, the one `Export-ConsoleRecording -Format Html` writes.

## Choose a window style

Use `-ChromeStyle MacOS`, `Windows` or `Linux` on `Export-ConsoleFrame`, `Export-ConsoleRecording` and `Invoke-ConsoleRecording`. The same style applies to static HTML and SVG, the HTML player, animated SVG and a session's final frame. These are generic vector shapes; no operating-system assets or logos are used.

{{< cm-chrome-styles >}}

Each command below runs on its own after installing ConsoleMonster. It creates the panel shown above and saves an SVG in the current directory. Change `-Theme Dark` to `Light` for the light sample, or change `.svg` to `.html` for selectable HTML.

### MacOS

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' { New-ConsoleText '8 of 8 checks passed' } |
    Export-ConsoleFrame -Path ./build-macos.svg -Width 36 -Theme Dark -Title 'Build report' -ChromeStyle MacOS
```

### Windows

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' { New-ConsoleText '8 of 8 checks passed' } |
    Export-ConsoleFrame -Path ./build-windows.svg -Width 36 -Theme Dark -Title 'Build report' -ChromeStyle Windows
```

### Linux

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' { New-ConsoleText '8 of 8 checks passed' } |
    Export-ConsoleFrame -Path ./build-linux.svg -Width 36 -Theme Dark -Title 'Build report' -ChromeStyle Linux
```

MacOS has a 28px bar and three coloured dots. Windows has a 32px tab bar, prompt glyph, plus sign and line controls. Linux has a 36px header, a bold title and a round close control. Titles truncate within the space reserved for them. The bar uses the theme's `Surface` background and the control glyphs use `Muted`; only the macOS dots keep their traffic-light colours.

`-WindowChrome` keeps the MacOS default. `-ChromeStyle` by itself enables the chosen style. `-WindowChrome:$false` or `-ChromeStyle None` hides the frame. Frame and recording exports still omit chrome when neither parameter is supplied; `Invoke-ConsoleRecording` includes it by default. Text, Markdown and asciicast contain terminal content, so chrome does not add characters to those formats.

Record and export a Windows-style player, then a Linux-style final frame from the same cast:

```powershell
Import-Module ConsoleMonster
Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Build'
    Write-Host '8 of 8 checks passed' -ForegroundColor Green
} -Destination ./chrome-demo.html -ChromeStyle Windows -Theme Light

Export-ConsoleRecording -Path ./chrome-demo.cast -Destination ./chrome-demo-final.svg `
    -FinalFrame -ChromeStyle Linux -Theme Light -Title 'Build report'
```

In C#, both option types keep `WindowChrome` as the on/off control. `ChromeStyle` defaults to `MacOS`; setting it alone does not enable the frame. `WindowChrome = false` and `ChromeStyle = None` each hide it:

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;

var options = new ExportOptions {
    Width = 36, Theme = ExportTheme.Light, Title = "Build report",
    WindowChrome = true, ChromeStyle = WindowChromeStyle.Windows
};
File.WriteAllText("build.svg", ConsoleExporter.ToSvg(TextBlock.Plain("8 of 8 checks passed"), options));
```

## Targets

Every write, live session, prompt and status helper goes to an `ITerminalTarget`: a profile, a theme, a writer and an input source. `ConsoleTarget.Instance` is the process console; `TestTerminal.Target(...)` returns a `RecordingTarget` for tests, `WriterTarget` writes append-only lines to a `TextWriter`, and `NullTarget.Instance` discards output. `using (Terminal.Use(target)) { ... }` sends everything in the block to a target, and the scope flows with `async` calls. In PowerShell, `Use-ConsoleTarget` runs a script block with ConsoleMonster output going to a file (`-Path`), a writer or a target, or returned as a string.

## Testing output

The helpers ConsoleMonster's own tests use are public: `TestTerminal.RenderText(widget, width: 80, glyphs: GlyphTier.Ascii)` renders at a fixed size, tier and culture, `GoldenFile.Match` compares with a golden file and shows the first differing line, and `ScreenCapture` with scripted keys captures prompts, menus and forms. In Pester, `Assert-ConsoleOutput -GoldenPath` does the same.

## Related

To see a long recording played back, open the [live dashboards page](/live/). [Live sessions](/docs/live-sessions/) explain what a recording captures; the [Export-ConsoleRecording reference](/powershell/commands/export-consolerecording/) lists the formats.
