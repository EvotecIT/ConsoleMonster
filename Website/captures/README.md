# Website capture producers

`SiteFrames capture` records PowerShell 7 and compiled C# programs through
`ConsoleMonster.Sessions.SessionRecorder`. The recorder uses ConPTY on Windows
and openpty on macOS and Linux. Scripted readiness waits, input, Ctrl+C, timeout,
output draining and terminal-query replies belong to the recorder. SiteFrames
supplies the examples, profiles and capture recipes.

| Set | Windows | macOS / Linux |
| --- | --- | --- |
| PowerShell 7, gallery, app, live C# and demos | Produce | Produce |
| Windows PowerShell 5.1 (`ps5`) | Produce | Skip production; validate committed output during render |
| Terminal-profile examples and measurements | Produce platform-tagged files | Produce platform-tagged files |

Run from the repository root, with .NET 8 and PowerShell 7 installed:

```sh
dotnet build ConsoleMonster.PowerShell -c Release
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website
dotnet run --project Website/tools/SiteFrames -c Release -- render Website
pwsh Website/build.ps1 -CI
```

The same commands work in PowerShell on Windows. Windows also builds the net472
module used by PowerShell 5.1. An optional area follows `Website`, for example
`capture Website gallery`. Existing `SITEFRAMES_GALLERY`, `SITEFRAMES_LIVE` and
`SITEFRAMES_DEMOS` selections remain available for focused work.

## Pins and normalization

- Terminal width and height come from the capture recipe. Colour, glyph tier,
  theme, interaction mode and language come from `PowerShellRunner.Environment`.
- Both .NET cultures are en-US. The child locale is UTF-8 (`en_US.UTF-8` on
  macOS, `C.UTF-8` on Linux); `TZ=UTC` fixes Unix local date presentation.
  Cross-platform date fixtures also pass `Presentation.TimeZone=Utc`, since
  Windows local time-zone selection does not follow the Unix `TZ` variable.
- Emoji occupy two cells; ambiguous-width characters occupy one. The recorder
  answers Unix cursor-position queries from the same VT model used for replay.
- `CONSOLEMONSTER_NOW=2026-10-21T12:00:00Z` fixes relative-date presentation.
  This variable does not replace `DateTime.Now`, `Get-Date` or a stopwatch.
  Sample programs supply their own fixed timestamps and explicit invented data.
- PowerShell uses no logo or profile. The checkout's module link comes first on
  `PSModulePath`, and the runner verifies that the loaded binary came from that
  link. Release is the default development configuration. Native review commands
  can explicitly select their build configuration.
- CI markers, terminal branding, colour suppression and telemetry/update prompts
  are cleared or pinned. Builds disable MSBuild node and server reuse.
- ANSI captures retain recorded VT output. Casts use UTF-8 without BOM and LF
  JSON-line endings. Visible terminal line endings are CRLF. Window titles and
  cursor queries are omitted from replay output because they contain no cells.
- Cast packet boundaries and wall-clock arrival timestamps are replaced by a
  logical timeline: each complete synchronized repaint or printed line advances
  50 ms. Text, colour and cursor operations remain recorded output. This does
  not normalize application-level elapsed times or concurrent output ordering.
- Gallery action-log timestamps use a frozen `SimulatedClock.Read` through
  `InteractiveOptions.ActionClock`. This pins elapsed labels at zero without
  changing action execution, cancellation, or scripted input pacing. Normal
  applications keep their real action clock.
- Synthetic live pipeline and assessment programs use `RecordingTarget.SkipDelays`
  checkpoints with `CaptureViewport` enabled. They execute the shown program and
  render its logical states at the real terminal size, including compact layouts.
  The shared terminal encoder emits those frames inside the recorded native child;
  SessionRecorder records that terminal output. Replay checkpoints follow recorded
  time groups, and simulated pipelines advance their explicit clock. Their frame
  schedule does not depend on producer CPU speed. Measured and concurrent programs
  keep their normal native execution path.
- The capture policy participates in source fingerprints. Do not edit stamps
  to make an old capture appear fresh. Regenerate each affected producing set.

## Platform ownership and comparison

Neutral fixtures keep their existing paths. Host-dependent terminal profiles,
stress and benchmark recordings add `.windows`, `.macos` or `.linux` before the
extension. Historical unqualified host-dependent files are Windows captures.
Rendering chooses its default explicitly rather than using the build host's OS.
Demo media can expose named platform variants, and the `cm-demo` shortcode accepts
`platform="linux"`, `platform="macos"` or `platform="windows"`. The `cm-live` shortcode uses the same platform selector for stress recordings. The live and benchmark pages expose each available measured platform separately.

`captures/run.json` describes the files actually produced by a capture invocation:
platform, category, hash and terminal geometry. Keep a copy of the complete
capture directory for each run. Compare two independent runs with:

```sh
dotnet run --project Website/tools/SiteFrames -c Release -- \
  compare-captures /path/to/windows/captures /path/to/macos/captures comparison.json
```

The command fails if a produced capture is missing, a hash or geometry is wrong,
a neutral cell/style differs, or it finds replacement glyphs, invalid wide-cell
continuations or disconnected adjacent border glyphs. Same-platform runs must
be byte-identical. Cross-platform transport encoding and packet/timing differences
are allowed only when the decoded cells and styles match. The JSON report lists every failing capture and counts every changed row and cell. It retains the first 32 changed rows and eight changed cells per row as diagnostic samples, with explicit omitted counts, so animated differences cannot produce unbounded reports. A category label
alone never waives a difference; platform values need an explicit comparison
policy, and real measurements still fail strict repeated-run comparison. The Windows-only ps5 production difference is listed separately;
it does not exempt those committed files from render freshness and parity.

The comparison is a structural gate. Rendered review is still required to check
the actual font, border paint, compact layout and replay interaction. Measured
benchmark/stress values and intentionally concurrent demonstrations are real
runtime observations; strict repeated-run comparison exposes their variability.
Do not claim determinism or silently mask those differences.
