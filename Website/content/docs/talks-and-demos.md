---
title: Talks and demos
description: Write a typed PowerShell demo, present it live, and export PNG slides, animated GIF, APNG or MP4.
slug: talks-and-demos
toc: true
order: 9
group: live
card: app
chips: [Typed demos, Presenter mode, Slides]
summary: "Repeatable typed demos and exports for your next talk."
---

I keep demo commands in a plain PowerShell file, so I can rehearse, record and present the same sequence. ConsoleMonster types them into a real PowerShell shell, waits for it to finish, and records the actual terminal output.

## Write a demo

Typed demos use PowerShell 7. Save this as `demo.ps1`:

```powershell
# demo: setup
function prompt { 'demo> ' }
# demo: wpm 140
# demo: chapter Calculate
Write-Host ('The answer is ' + (6 * 7)) -ForegroundColor Cyan
# demo: pause 1
# demo: chapter Report
'Compile: passed', 'Tests: 24 passed', 'Package: ready'
# demo: wait Package: ready
# demo: pause 2
```

Directives are comment lines beginning with `# demo:`. Ordinary PowerShell remains ordinary PowerShell; multiline commands, functions, pipelines and here-strings stay together.

| Directive | Meaning |
|---|---|
| `chapter Inspect` | Add a chapter before the next command. |
| `pause 1.5` | Pause for seconds, using a decimal point. |
| `setup` | Hide the next complete command and its output. Wrap several setup statements in `. { ... }`. |
| `type-only` | Type the next command without running it. The following command replaces it. In presenter mode Enter can run it. |
| `narrate What happens next` | Write a narration line into the child terminal and recording. |
| `wpm 140` | Set subsequent typing speed; one word is five characters. |
| `wait Package: ready` | Wait for a regular expression in the preceding command's output, including output arriving asynchronously. |

`-Seed` fixes the typing jitter. Keep fixtures, culture, timestamps and command results fixed too: a live network, random data or the current time still produces different output. Real command execution and operating-system scheduling determine output timing. Setup hides its own output; it is not a security boundary for commands that later print the same data. Redaction protects the saved recording; live terminal output remains unredacted.

The driver wraps the shell's actual prompt in OSC 133 semantic marks. It never searches for `PS C:\>` or a directory name, so a custom prompt, another language or a different OS does not change readiness detection. Each command executes in the same child PowerShell scope. Commands should finish unattended; interactive password prompts and full-screen applications need an ordinary [session recording](/docs/recording/).

## Record and export

```powershell
Import-Module ConsoleMonster
Invoke-ConsoleDemo -Path ./demo.ps1 -Destination ./talk.cast -Seed 42
Export-ConsoleRecording -Path ./talk.cast -Format Gif -Preset Slide16x9 -WindowChrome -Destination ./talk.gif
Export-ConsoleRecording -Path ./talk.cast -Format Mp4 -Preset Slide16x9 -WindowChrome -Destination ./talk.mp4
```

PNG, GIF and APNG run without an external executable. MP4 pipes RGBA frames to `ffmpeg` on PATH and writes H.264 with `yuv420p` pixels and fast-start metadata; it has no audio track. If ffmpeg is missing, use GIF or install ffmpeg separately. It is never a package dependency.

`-FramesPerSecond` is the maximum sample rate (10 by default). Identical frames are merged; GIF uses a shared quantised palette. `-IdleLimit 2` caps unchanged-frame delays while the final frame has its own hold. Use zero to keep original timing. `-LoopCount 0` loops indefinitely; one plays once. MP4 is a single pass; choose repetition in your slide tool. APNG preserves full-colour pixels for software that supports it.

| Preset | Canvas |
|---|---|
| `Slide16x9` | 1920 × 1080, padded and fitted to the complete grid. |
| `Slide4x3` | 1440 × 1080. |
| `Social` | 1200 × 630. |
| `Docs` | Natural size. |

Use `-Theme Dark`, `Light` or `HighContrast`, and `-ChromeStyle MacOS`, `Windows`, `Linux` or `None`. The embedded JetBrains Mono font makes default text consistent across operating systems. `-FontFamily 'Consolas'` chooses an installed font; C# also accepts additional fallback families. Emoji and CJK coverage depends on installed fonts. Box drawing, blocks and braille use geometric cell shapes, so they join without font seams.

## Export still slides

```powershell
# Final frame, or a moment in source-recording time.
Export-ConsoleRecording ./talk.cast -Format Png -Preset Slide16x9 -Destination ./final.png
Export-ConsoleRecording ./talk.cast -Format Png -At '00:00:04' -Preset Slide16x9 -Destination ./moment.png

# A chapter's completed view, or numbered slides for every chapter.
Export-ConsoleRecording ./talk.cast -Format Png -Chapter Report -Preset Slide16x9 -Destination ./report.png
Export-ConsoleRecording ./talk.cast -Format Png -PerChapter -Preset Slide16x9 -Destination ./slides

# Export any widget or rendered frame too.
New-ConsoleText 'Ready for the talk' | Export-ConsoleFrame -Format Png -Preset Social -Path ./ready.png
```

`-Chapter` selects the last view before the next chapter; the last chapter uses the final frame. `-PerChapter` writes `slide-001.png`, `slide-002.png` and so on into a directory. Source-time selection happens before playback speed or idle compression. Choose one of `-At`, `-Chapter`, `-PerChapter` or `-FinalFrame`.

## Present live

```powershell
Start-ConsoleDemo -Path ./demo.ps1
Start-ConsoleDemo -Path ./demo.ps1 -Record ./on-stage.cast
```

The terminal title shows the current step. Commands run in a dedicated real PowerShell shell; closing the demo stops that child and restores the title. Without `-Record`, temporary recording files are removed on exit.

| Key | Action |
|---|---|
| Enter | Type and run; if already typed, run it. |
| Space | Type only. Repeated presses do not duplicate input. |
| b | Discard pending input and move back. Executed side effects stay in the shell. |
| s | Discard pending input and skip the command. |
| q | Quit and finalize any recording. |

Use a terminal such as Terminal.app, iTerm2 or Windows Terminal. Keep the window at the intended recording size during a take. Rehearse the script with the same shell, data and module versions you will use on stage.

## Put it in slides

| Slide tool | Use |
|---|---|
| PowerPoint desktop | GIF through Insert → Pictures, or MP4 through Insert → Video. Preview GIF animation in Slide Show. |
| Keynote | GIF as an image, or MOV/MP4 as a movie. Set playback in the media controls. |
| Google Slides | GIF through Insert → Image. Video uses its separate YouTube/Drive insertion flow. |

PNG works for stills in all three. Animated SVG commonly appears as a still in slide tools; use GIF or video when the typing needs to move. APNG support varies, so GIF is the portable animated-image choice. PowerPoint for the web has different GIF playback limitations from the desktop app.

See [Microsoft's GIF guide](https://support.microsoft.com/en-us/powerpoint/add-an-animated-gif-to-a-slide), [PowerPoint video formats](https://support.microsoft.com/en-us/powerpoint/video-and-audio-file-formats-supported-in-powerpoint), [Keynote media](https://support.apple.com/guide/keynote/add-video-and-audio-tan63d61519a/mac) and [Google Slides image and video insertion](https://support.google.com/docs/answer/97447).

## C#

Add the `ConsoleMonster` and `ConsoleMonster.Graphics` packages. The core package has no runtime package dependencies; the graphics add-on brings ChartForgeX and ChartForgeX.Stories. The PowerShell module includes the add-on and loads graphics assemblies on first use.

```csharp
using ConsoleMonster.Demos;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;

var demo = DemoScript.Read("demo.ps1");
var options = new DemoOptions {
    Seed = 42,
    Recording = new SessionRecordingOptions {
        Path = "talk.cast", Shell = "pwsh", Width = 100, Height = 28
    },
    Output = Console.Write
};
ConsoleDemo.Record(demo, options);
var recording = SessionRecording.Read("talk.cast");
var slides = new GraphicsExportOptions { Preset = GraphicsPreset.Slide16x9 };
recording.SaveGraphics("talk.gif", GraphicsFormat.Gif, slides);
recording.SaveChapterPngs("slides", slides);
```

For live presentation use `ConsoleDemo.Present(demo, options)`. Pass `stepChanged` to display a custom step counter and a cancellation token to stop the owned child.

The repository includes a complete [PowerShell recording example](https://github.com/EvotecIT/ConsoleMonster/tree/master/Website/examples/recording) and a [runnable C# example](https://github.com/EvotecIT/ConsoleMonster/tree/master/Website/examples/presenter-kit). Both use synthetic data.
