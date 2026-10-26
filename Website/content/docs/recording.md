---
title: Recording and replay
description: Record a whole terminal session, including native commands, to asciicast v2 and export an HTML player, animated SVG, text, Markdown or a final frame.
slug: recording
toc: false
order: 8
group: live
card: app
chips: [Terminal sessions, Chapters, Export]
summary: "Whole-session recording, dashboard replay and local exports."
---

Record a terminal session with native commands, colours and chapter markers, then share it as a local HTML player or asciicast v2 file.

## Try it

This example records a real Git command and coloured PowerShell output, then writes `command-review.cast` and a self-contained `command-review.html` player. Use a terminal with Git installed and an empty output folder. The C# version starts PowerShell 7, so `pwsh` must be on your PATH.

{{< cm-try key="session-command" ids="session-command" project="CommandRecording" >}}

Open `command-review.html` in a browser. The cast and its `.cast.json` metadata stay beside it; nothing is uploaded. A new run needs a fresh destination because the recorder never overwrites an existing cast.

## How it works

`Invoke-ConsoleRecording` starts an isolated PowerShell child inside a pseudo-console. Native commands and ConsoleMonster output reach the same terminal stream, including colour, cursor movement and progress redraws. Caller variables and functions are not inherited; keep required setup inside the script block.

These are the recording calls in the example:

```powershell
Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Inspect'
    git --version
    Add-ConsoleRecordingMarker 'Report'
    Write-Host 'Review complete' -ForegroundColor Green
} @recording
```

`-Destination` selects the export and saves the original cast alongside it. Markers become named chapters in the HTML player. Its chapter menu and on-track ticks seek to a chapter; hover or focus a tick to see its name. **?** opens keyboard help for Space, the arrow keys and number-key chapter shortcuts.

In C#, `SessionRecorder.Start` owns the child, `AddMarker` adds a chapter, and `WaitForExit` drains output and finalizes the cast. `SessionRecording.Read` loads it for the existing exporter. `SendInput` forwards input without storing it; `Resize` records terminal size changes. `Stop` or disposal stops that recorder's child.

Windows uses ConPTY and needs Windows 10 version 1809 or later. Linux and macOS use `openpty` with a controlling terminal. The library also targets .NET Standard 2.0 and .NET Framework 4.7.2; the example's recorder API is the same on those targets.

On macOS, each launch extracts a small embedded terminal helper into a private temporary directory and removes it after startup. Hosts that prohibit executing files from temporary directories reject recording startup with an operating-system permission error. Such hosts need a temporary directory where execution is permitted by their policy.

## Variations

### Watch a session with typed commands

This longer, real `Invoke-ConsoleRecording` capture inspects a synthetic Git repository, builds with live progress, then shows a ConsoleMonster table, a failing command and a redacted token. Its **Inspect**, **Build** and **Report** chapters jump to each part. Before playback, the poster shows the completed report.

{{< cm-session-recording >}}

The [recording script](/examples/recording/record-session.ps1) uses `Invoke-ConsolePresentation` with a [plain PowerShell demo](/examples/recording/demo.ps1). It types commands into one real child terminal and detects readiness through OSC 133 marks, so the visible prompt can change on any operating system. The synthetic repository lives under the output folder. A synthetic token becomes `API_TOKEN=[REDACTED] (redacted)` before the cast is written. See [Talks and demos](/docs/talks-and-demos/) for directives, presenter controls and slide exports.

Download the [HTML player](/recordings/session.html), [animated SVG](/recordings/session.svg), [asciicast v2](/recordings/session.cast), [text](/recordings/session.txt), [Markdown](/recordings/session.md) or [final frame](/recordings/session-final-dark.svg). To reproduce the typed session, save both source files together in an empty folder.

### Record an interactive terminal

Start a recording in your terminal, enter commands in the child, then stop it:

```powershell
Invoke-ConsoleRecording -Interactive -RecordingPath ./interactive.cast -Title 'Deployment'
git --version
Add-ConsoleRecordingMarker 'Deploy'
Stop-ConsoleRecording
```

Exiting the child also finalizes the cast and returns to the original terminal. PowerShell children load the recording commands automatically; for `cmd` or a Unix shell, type `exit`. `-Shell` chooses the executable. The PowerShell cmdlets default to the current PowerShell executable. The `● REC` indicator is in the window title and is not recorded.

Without `-Path`, filenames use the date, time and a unique suffix under Documents/ConsoleMonster/Recordings. Use `-Folder` to choose another location.

### Export the same cast in another format

```powershell
Export-ConsoleRecording -Path ./command-review.cast -Destination ./command-review.svg
Export-ConsoleRecording -Path ./command-review.cast -Destination ./command-review.txt
Export-ConsoleRecording -Path ./command-review.cast -Destination ./command-review.md
Export-ConsoleRecording -Path ./command-review.cast -Destination ./command-review-final.svg `
    -FinalFrame -Theme Light -ChromeStyle Windows
```

`-Format PlayerData` writes JSON for embedding the same player. `-Theme Dark` or `Light` uses the existing exporter palette. Animated SVG uses CSS keyframes; the final-frame SVG is static. Text and Markdown include the main screen's scrollback. Whole-session casts remain asciicast v2, including output, marker and resize events. In C#, `ExportFinalSvg` writes a static frame and `FinalFrame` returns a `CellBuffer` for the existing exporter.

### Redact values and bound a recording

Input bytes are forwarded but never stored. A password prompt that disables terminal echo therefore adds no password text to the cast. Built-in output patterns mask bearer tokens, common API-key prefixes and assignments such as `password=`, `api_key=` and `client_secret=`. Add regular expressions for your own values:

```powershell
Invoke-ConsoleRecording {
    Write-Host 'account=sample-account'
    Write-Host 'api_key=synthetic-demo-value'
} -Destination ./redacted.html -Redact 'sample-account' `
    -IdleLimitSeconds 2 -MaxBytes 10485760 -TimeoutSeconds 600
```

Resolving the default recording folder does not check Documents access. An explicit destination can record without touching that folder.

Redaction runs before writing the cast or chapter metadata, including secrets split across reads or colour codes. Long lines and cursor-positioned repaints stream without a 64 KiB record cutoff. The recorder keeps raw and visible overlap for built-in credentials and custom patterns whose maximum match span can be proved, up to 8,192 UTF-16 characters. Rules match the original output, so overlapping patterns cannot hide a secret by replacing part of it first. Chapter markers and resizes preserve their ordering without breaking redaction across them. Playback honours synchronized redraw boundaries, keeping completed frames within the export frame budget. When redraws arrive together, it spaces them at the requested frame rate and moves later frames and chapters with them; the final hold stays intact.

Patterns with unbounded quantifiers, anchors, lookarounds, backreferences or unsupported syntax use complete-record matching. A record ends at a newline, carriage return, stop control or end of capture. If uncommitted redaction or terminal context reaches 1,048,576 UTF-16 characters, the cast shows an explicit omission message and resumes after the next line break. A pattern timeout also fails closed with a visible message. The companion `.cast.json` counts these cases in `redactionFallbacks`. This buffer safeguard is separate from `-MaxBytes`, which caps the total cast bytes. Review a recording before sharing it because patterns cannot recognize every secret.

`-RedactionText` chooses a replacement such as seven bullets; the default is `[REDACTED]`. Match only the value when surrounding context should stay visible. Replacement text cannot contain control characters. In C#, set `SessionRecordingOptions.RedactionText`.

`-IdleLimitSeconds` compresses longer gaps; zero preserves timing. `-MaxBytes` caps the cast, including its header. At the cap, the cast stays valid and a warning appears; the child continues while further output is omitted. `-TimeoutSeconds` stops the child after the specified time.

### Use the limited in-process fallback

When a host cannot create a pseudo-console, explicitly select `-InProcess`:

```powershell
Invoke-ConsoleRecording {
    Add-ConsoleRecordingMarker 'Summary'
    Write-Host 'PowerShell output only'
} -InProcess -Destination ./summary.html
```

It captures PowerShell host and pipeline output in an in-process runspace. The cast header and metadata say **in-process PowerShell output only**, and the command prints a warning. Native terminal behaviour, cursor movement and resize handling need pseudo-console mode.

### Choose a window style

Use `-ChromeStyle MacOS`, `Windows` or `Linux` on `Export-ConsoleFrame`, `Export-ConsoleRecording` and `Invoke-ConsoleRecording`. The same style applies to static HTML and SVG, the HTML player, animated SVG and a session's final frame. These are generic vector shapes; no operating-system assets or logos are used.

{{< cm-chrome-styles >}}

Each command below runs on its own after installing ConsoleMonster. It creates the panel shown above and saves an SVG in the current directory. Change `-Theme Dark` to `Light` for the light sample, or change `.svg` to `.html` for selectable HTML.

#### MacOS

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' -Content { New-ConsoleText '8 of 8 checks passed' } |
    Export-ConsoleFrame -Path ./build-macos.svg -Width 36 -Theme Dark -Title 'Build report' -ChromeStyle MacOS
```

#### Windows

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' -Content { New-ConsoleText '8 of 8 checks passed' } |
    Export-ConsoleFrame -Path ./build-windows.svg -Width 36 -Theme Dark -Title 'Build report' -ChromeStyle Windows
```

#### Linux

```powershell
Import-Module ConsoleMonster
New-ConsolePanel -Title 'Build' -Content { New-ConsoleText '8 of 8 checks passed' } |
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


## Make it readable

Pacing is applied when exporting, so the same cast can produce a quick preview and a readable presentation without another recording. `-Pace Normal` is the default. New visible text is counted across a burst of output, then the settled screen gets a bounded reading budget:

| Pace | Screen min–max | Per character | Per line |
|---|---:|---:|---:|
| Slow | 2–16 s | 35.71 ms | 140 ms |
| Normal | 1.2–8 s | 23.81 ms | 80 ms |
| Fast | 0.55–4 s | 13.89 ms | 40 ms |

The budget is the new visible character count times its per-character time, plus the allowance for each changed line, bounded by the minimum and maximum. Slow gives about 1.5–2× Normal's reading time; Fast uses about half. The text rates (28 / 42 / 72 characters per second), line allowances and minimum holds follow ChartForgeX's terminal playback presets. The larger whole-screen maximum keeps Slow distinct on dense output.

Source time already spent on the settled frame counts towards its budget; export adds only the missing reading time. Speed and per-chapter speed scale this budget. Explicit marker, chapter and final holds are additional and keep their durations. Colour-only changes add no reading time. These ratios describe reading budgets; a recording's total duration also includes its source timing and explicit holds.

For 225 new characters on three lines, the reading budgets are 8.456 / 5.597 / 3.245 seconds for Slow / Normal / Fast, matching ChartForgeX's terminal typing and line timing for the same text. `Source` adds no reading time; combine it with `-IdleLimitSeconds 0` to keep all source gaps.

In the pipeline recording used to check these presets, the same cast exports to a 1920×1080 GIF lasting 15.51 seconds at Source, 19.51 at Fast, 23.51 at Normal and 31.51 at Slow. These exports disable idle capping and include a two-second final hold and the playable cadence for retained instantaneous redraws. The preset changes the delays, without dropping screens or rerecording the demo.

```powershell
Export-ConsoleRecording ./run.cast -Destination ./run.gif -Preset Slide16x9 -Pace Normal
Export-ConsoleRecording ./run.cast -Destination ./run-slow.gif -Preset Slide16x9 -Pace Slow
# Keep the cast's source delays, with no reading holds or idle compression.
Export-ConsoleRecording ./run.cast -Destination ./run-source.html -Pace Source -IdleLimitSeconds 0
```

`-IdleLimitSeconds 2` caps source delays on an unchanged screen. It never shortens reading holds, explicit marker holds or the final hold (`-HoldLastFrameSeconds 2`). Source timestamps in new casts are preserved; the cast's `idle_time_limit` header is only a hint to other players. An older cast whose timestamps were already compressed cannot recover the lost pause.

Use `-ChapterHoldSeconds 3` to freeze the screen at every chapter marker. Within any recorded script, add a hold without sleeping:

```powershell
Write-Host 'Read these results'
Add-ConsoleRecordingMarker -HoldSeconds 3
# A labelled hold also starts a chapter.
Add-ConsoleRecordingMarker 'Results' -HoldSeconds 3
```

A hold freezes the screen as it was when the marker arrived, including when later output has the same timestamp. It adds time during export and does not wait during recording. The cast stores these instructions as `cm` metadata events alongside ordinary `m` chapter events; reading and writing a cast preserves both, with no sidecar required. Other asciicast players may ignore the extra metadata.

Trim with source positions, or with unique chapter labels. The start is included and the end is excluded. `-TrimEndChapter Cleanup` stops before that chapter and its hold. Earlier output is still replayed to reconstruct the screen at the new start.

```powershell
Export-ConsoleRecording ./run.cast -Destination ./results.gif -Preset Slide16x9 `
    -TrimStartChapter Install -TrimEndChapter Cleanup `
    -ChapterSpeeds @{ Install = 4; Results = 1 } -ChapterHoldSeconds 2
# Time boundaries use positions in the original cast, before speed and holds.
Export-ConsoleRecording ./run.cast -Destination ./excerpt.svg -TrimStartSeconds 4 -TrimEndSeconds 18
```

Chapter speed multiplies `-Speed` for source and reading delays; explicit holds keep their requested duration. GIF, APNG, HTML/player data and animated SVG share this timeline. Raw session asciicast, text/transcripts and still PNG/SVG exports retain their source content and positions.

In C#, use `RecordingExportOptions.Pace`, `IdleLimit`, `ChapterHold`, `TrimStart`, `TrimEnd`, `TrimStartChapter`, `TrimEndChapter` and `ChapterSpeeds`. Durations are `TimeSpan` values. A running `SessionRecorder` accepts `AddMarker(TimeSpan.FromSeconds(3))` or `AddMarker(TimeSpan.FromSeconds(3), "Results")`; `SetPace(RecordingPace.Slow)` records a pace instruction. From a recorded child, write `SessionRecorder.MarkerSequence(TimeSpan.FromSeconds(3))` to its terminal output.

### Record dashboard updates

A live dashboard can record the widget updates it applies instead of terminal bytes. Use `Invoke-ConsoleDashboard -RecordPath` in PowerShell, or `LiveSessionOptions.RecordPath` / `RecordWriter` in C#. Replay into the same dashboard with `Show-ConsoleReplay -PlaybackMode Automatic`, `Show-ConsoleReplay` or `LiveSession.Replay`. Dashboard recordings use JSON lines and export to asciicast v3; whole terminal sessions use asciicast v2.

{{< cm-demo id="record" heading="h4" >}}

### Automate a terminal capture in C#

`SessionRecorder` can wait for child output before sending input. This example needs PowerShell 7 on `PATH`:

```csharp
using ConsoleMonster.Sessions;

var options = new SessionRecordingOptions {
    Shell = "pwsh",
    Arguments = { "-NoLogo", "-NoProfile", "-Command", "[Console]::WriteLine('READY'); [Console]::WriteLine([Console]::ReadLine())" },
    Path = "scripted.cast",
    Width = 80,
    Height = 20,
    Timeout = TimeSpan.FromSeconds(30),
    SessionTimeout = TimeSpan.FromSeconds(30),
    RespondToCursorQueries = true
};
using SessionRecorder recorder = SessionRecorder.Start(options);
recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
recorder.SendInputAfter(TimeSpan.FromMilliseconds(50), "hello\r");
recorder.WaitForExit();
```

`Timeout` bounds the exit wait. `SessionTimeout` also bounds readiness and scripted-input work from startup. Cancellation and deadlines stop the owned child and drain its recording, including when the child stops reading input. `WaitForExitWithInterrupt` sends Ctrl+C at a chosen time from startup for a long-running child.

Enable `RespondToCursorQueries` for unattended Unix captures: the recorder answers cursor-position requests from its VT screen. Leave it off when forwarding output to a real terminal that supplies its own replies. Windows uses ConPTY's native console handling.

## Related

[Live sessions](/docs/live-sessions/) explain dashboard updates and targets. The [live dashboards page](/live/) shows a longer dashboard recording. See the [recording cmdlet reference](/powershell/commands/invoke-consolerecording/) and [export reference](/powershell/commands/export-consolerecording/) for all parameters.
