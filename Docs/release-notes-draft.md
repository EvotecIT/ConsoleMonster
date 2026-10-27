# ConsoleMonster 2.1.0 release notes

ConsoleMonster 2.1.0 is the first version I consider complete: one consistent API for PowerShell and C#, everyday output that reads well by default, full-screen apps, and recordings you can put on a slide.

## Highlights

- **A consistent API.** One command per job: `Show-Console -View` for passive views, `Update-Console` for every live update, `New-ConsoleChart -Type` for numeric charts, and `[pscustomobject]` rows instead of item cmdlets. PowerShell parameters and C# properties share their names, and the C# fluent API uses them directly: `.Title("Build")`, starting from `Ui.Panel()`. (#55, #69, #71)
- **Everyday output that reads well by default.** `Enable-ConsoleFormatting` now covers JSON and REST records, CSV imports, network replies, commands, modules and history, alongside files, processes and hashtables. (#22, #41, #54)
- **Full-screen apps.** A shared host with focus, scrolling, mouse input and terminal ownership. Compose grids, forms, tabs, trees and log viewports, or hand the terminal to a child program and return to the app. `New-ConsoleCommand` defines an action once for buttons, the command palette and grid rows, so `Show-ConsoleGrid -Command` can restart a service and stream its output without leaving the grid. (#41, #60, #79)
- **Recordings and slides without external programs.** Record whole terminal sessions, then export them to an HTML player, SVG, GIF, APNG or PNG slides. GIF is the slide format, with reading-speed presets and chapter holds, and no longer needs ffmpeg. (#46, #49, #57)
- **36 copyable demos.** Every demo is a complete PowerShell script plus C# initializer and C# fluent programs, with the output they produce. The gallery includes container status, event-log summaries, license usage, scheduled tasks, stale accounts and update compliance. (#50, #53, #69, #75)

## New

- `Show-Console -View Auto|Table|List|Tree|Chart|Code|Json|Error|Markdown|Rule`, with `Show-ConsoleTable` and `Show-ConsoleList` as shortcuts. (#55, #69)
- `Update-Console` for progress, spinners, status lists, phases, lanes, activities, cells, summary bars, live tables, charts, regions, flows and runs; C# `ConsoleUpdate.Apply` with `WidgetUpdate` and `RunUpdate`. (#69)
- C# `Ui` entry point, `.Content(c => …)` builders on every container, and `.Show()` / `.Export(path)` endings. (#69)
- Flow strips: `New-ConsoleFlow` and C# `Flow`, with the TLS verdict showcase. (#58)
- Month and year calendars: `New-ConsoleCalendar` and C# `Calendar`, with dated outcome rows, legend, ISO week numbers and culture-aware week starts. (#59)
- Full-screen host, `New-ConsoleCommand` / C# `AppCommand`, `New-ConsoleScrollView`, `Show-ConsoleGrid -Command -RefreshRow` and `Show-ConsoleApp -Command -SelectionKey`. (#60)
- Retained data grids with independent focus, stable row identities, marks, prefix search and lazy cells for visible rows; typed forms, date/range controls, tabs, trees and log viewports. The same controls power grid and object-explorer views. (#79)
- Terminal handoff with screen, input-mode and focus restoration after exit, startup failure, timeout or cancellation, plus a bounded last-output view. (#79)
- Interactive apps and controls: `Show-ConsoleApp`, inputs, focus, mouse and key bindings. (#41)
- Large runs: `Start-ConsoleRun`, `Invoke-ConsoleRun` and `Complete-ConsoleRun`, keeping finished items in scrollback while a bounded view shows active work and problems. (#18, #43)
- `Show-ConsoleObject` for exploring nested objects, `Compare-ConsoleText` for text and file diffs, and `Show-ConsoleOverview` for a host snapshot. (#27, #30, #41)
- Terminal session recording to asciicast with redaction and chapter markers: `Invoke-ConsoleRecording`, `Stop-ConsoleRecording`, `Add-ConsoleRecordingMarker`, `Export-ConsoleRecording`. (#46)
- Presentations from a plain PowerShell script: `Invoke-ConsolePresentation` and `Show-ConsolePresentation`. (#49)
- Graphics exports to PNG, GIF and APNG through the optional ConsoleMonster.Graphics package, with Slow, Normal and Fast pacing, chapter holds and per-chapter speeds. (#49, #57)
- Markdown reading through `Show-Console -View Markdown` and the optional ConsoleMonster.Markdown package. (#47)
- Portable JSON theme files: `Set-ConsoleTheme -Path` and `Export-ConsoleTheme`. (#39)
- Opt-in formatting for native help, compact errors, ReadLine and native progress colors, an Out-GridView fallback, an optional prompt and live log tails. (#41)
- CLI help presentation for C# command-line tools. (#42)
- Logging provider, generic-host dashboard and `dotnet new` template packages. (#18)
- Coming-from guides for Spectre.Console, PwshSpectreConsole, Microsoft grid tools, Show-Command and System.CommandLine.Rendering. (#62)
- The Demos catalogue with 36 programs in three styles each. (#50, #69, #75)

## Improved

- Everyday views follow the host on macOS and Linux: Unix permissions and owners, platform-specific overview data and Mac key bindings. (#51)
- Concurrent producers write complete blocks, and parallel runs route host messages, warnings and progress into the live view. (#23)
- Slide exports share window chrome across Mac, Windows Terminal and Linux styles, and render presets at their native pixel size. (#57)
- Demo pages show PowerShell, C# and C# fluent tabs with a remembered choice, generated "How it maps" notes and one Try it block per program. (#45, #52, #69)
- Examples use readable source layouts with matching native captures, and parity tables keep command names together on narrow screens. (#71, #73)

## Fixed

- macOS recording sessions now have a controlling terminal, so Ctrl+C, `/dev/tty` prompts and resize signals reach the recorded program. (#48)
- Long recordings no longer replace large full-screen redraws with a redaction placeholder; replay keeps complete synchronized redraws. (#56)
- Streaming redaction preserves long recording output while keeping built-in and custom sensitive values redacted across chunks. (#77)
- Recording player exports retain a final chapter at a fractional timestamp, including Windows PowerShell 5.1 recordings stopped immediately after the marker. (#81)
- App command completion includes final output and bookkeeping, so callers can observe the complete action before continuing. (#81)
- Built-in calendar languages use the same day and month labels on every operating system; other cultures use localized names with distinct weekday labels. (#80)
- The native recorder probe waits for readiness and the observed resize instead of fixed delays. (#65)
- Multiline rich log messages keep their markup across lines. (#25)
- Site code highlighting no longer colors `filter` or `process` inside hyphenated names. (#63)
- Demo cards stay inside their card on narrow and scaled screens. (#61)

## Dependencies

- ConsoleMonster.Graphics depends only on ChartForgeX 3.1.0. ChartForgeX.Stories and the external ffmpeg/MP4 writer are no longer used. (#57)
- ConsoleMonster.Markdown uses OfficeIMO.Markdown 3.5.2. (#47)
- ConsoleMonster.Hosting uses Microsoft.Extensions.Hosting.Abstractions 8.0.1, and ConsoleMonster.Extensions.Logging uses Microsoft.Extensions.Logging.Abstractions 8.0.3. (#18)
- The core ConsoleMonster package has no dependencies and targets netstandard2.0, net472, net8.0 and net10.0. The PowerShell module supports Windows PowerShell 5.1 and PowerShell 7.
