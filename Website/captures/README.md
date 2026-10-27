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
- The operations-calendar fixture explicitly supplies `Su Mo Tu We Th Fr Sa`
  as its shortest English weekday names; ICU and Windows NLS defaults differ.
- Project demo builds map the shown `Program.cs` source to that portable PDB file
  name through an explicit directory `PathMap`, avoiding checkout paths and
  platform-dependent local-file links. Those temporary project builds disable
  automatic deterministic source-root mapping so it cannot override that pin.
- Both .NET cultures are en-US. The child locale is UTF-8 (`en_US.UTF-8` on
  macOS, `C.UTF-8` on Linux); `TZ=UTC` fixes Unix local date presentation.
  Cross-platform date fixtures also pass `Presentation.TimeZone=Utc`, since
  Windows local time-zone selection does not follow the Unix `TZ` variable.
- Emoji occupy two cells; ambiguous-width characters occupy one. The recorder
  answers Unix cursor-position queries from the same VT model used for replay.
- `CONSOLEMONSTER_NOW=2026-10-21T12:00:00Z` fixes relative-date presentation.
  Watch headings, logging-provider timestamps and the hosted dashboard's start
  label use the same presentation clock. Its explicit offset is preserved;
  logging's UTC option converts it to UTC. Explicit watch times retain their
  existing local-time presentation. This variable does not replace `DateTime.Now`,
  `Get-Date` or a stopwatch, and hosted service durations remain real measurements.
  Sample programs supply their own fixed timestamps and explicit invented data.
- PowerShell uses no logo or profile. The checkout's module link comes first on
  `PSModulePath`, and the runner verifies that the loaded binary came from that
  link. Capture recipes select the Release development configuration, including
  native help, so a pre-existing Debug build cannot change the producing binary.
  Native review commands
  can explicitly select their build configuration.
- CI markers, terminal branding, colour suppression and telemetry/update prompts
  are cleared or pinned. Builds disable MSBuild node and server reuse.
- ANSI captures retain recorded VT output through the shared, opt-in
  `VtOutputNormalizer`. It canonicalizes supported SGR changes at their point of
  use, removes title/cursor/focus/input-mode housekeeping, and omits
  undecorated default-background trailing blanks before CRLF only when the
  original cells are already blank with exactly the same full style (spaces that
  erase text or establish a combining mark’s base style are retained). Visible backgrounds,
  decorations, links, glyphs, cursor moves and synchronized frame boundaries stay
  intact; unsupported SGR or truncated controls fail instead of being hidden.
  This removes incidental ConPTY ordering without replacing real output.
  Casts use the same normalizer, UTF-8 without BOM and LF
  JSON-line endings. Print-once encoder lines use CRLF. Native VT line feeds
  retain their column semantics; adding CR would corrupt cursor-optimized ConPTY
  output. Redundant CR immediately before LF is reduced to one CR. Window titles and
  cursor queries are omitted from replay output because they contain no cells.
  Append-only hosted stream text explicitly converts its native text newlines to
  CRLF before VT recording; that conversion is never applied to ordinary VT.
- Cast packet boundaries and wall-clock arrival timestamps are replaced by a
  logical timeline: each complete synchronized repaint or printed line advances
  50 ms. Scripted input waits are retained as additional reading holds, and the
  comparison gate checks those holds separately from transport timing. Text,
  colour and cursor operations remain recorded output. This does
  not normalize application-level elapsed times or concurrent output ordering.
- Gallery frames also use the hosted-output sink: their state markers and full
  encoded output reach the recorder before ConPTY can move a repaint past its marker.
- Gallery action-log timestamps use a frozen `SimulatedClock.Read` through
  `InteractiveOptions.ActionClock`. This pins elapsed labels at zero without
  changing action execution, cancellation, or scripted input pacing. Normal
  applications keep their real action clock.
- Synthetic live pipeline and assessment programs use `RecordingTarget.SkipDelays`
  checkpoints with `CaptureViewport` enabled. They execute the shown program and
  render its logical states at the real terminal size, including compact layouts.
  The shared terminal encoder emits those frames inside the recorded native child.
  Its hosted `SessionRecorder.CaptureOutput` sink records the exact VT before ConPTY
  can coalesce rapid repaints; the parent still owns the native child and its timeout.
  The sink captures emitted output only, not console APIs or keyboard input. Replay checkpoints follow recorded
  time groups, and simulated pipelines advance their explicit clock. Their frame
  schedule does not depend on producer CPU speed. Measured and concurrent programs
  keep their normal native execution path.
- The fictional Fleet health app uses paced scripted keys, immediate inventory loading
  and a seeded `SimulatedClock`. `ScriptedInput.SkippedWait` records each pause
  through `ScreenCapture.Hold`; checkpoint workers pass those pauses through
  `SessionRecorder.AddMarker` so menu, form and review screens remain readable. Full-viewport recordings capture its quarter-second
  simulated steps and final hold, rather than sampling a render thread. String activity
  IDs use a stable hash for spinner phases. Its fictional report path uses `/` on all
  hosts because the example displays it without writing a real file.
- Sequential recording and progress examples use `Wait-ConsoleDashboard` checkpoints
  instead of wall-clock sleeps. Their normal runs still wait; recording targets
  advance logical time, retain the actual updates and capture every step. Demo
  temporary folders set `TEMP`, `TMP` and `TMPDIR` and are removed after the run.
- The fixed-data watch example uses the same viewport checkpoints. A live watch
  honors `RecordingTarget.SkipDelays`, captures every result including the last,
  and keeps ordinary polling and caller cancellation outside that recording mode.
- Typed examples insert recorder chapters before each comment, prompt and command
  output. The site exports their actual `SessionRecording.ChapterFrame` cells with
  fixed reading holds of 600, 400 and 1200 ms. This avoids comparing half-written
  native repaints. Static examples export the recorder's complete `Transcript`,
  including scrolled history. Both use the declared dark terminal palette and the
  shared terminal encoder; visible data, styles, links and wide cells are retained.
- Gallery recipes can declare `capture.ready` (or a state-specific `ready`) to
  wait for text in the actual current frame before sending scripted keys. The
  shared `ScriptedInput.WaitForOutput` queues a bounded wait and yields to the
  application's repaint loop; it does not guess asynchronous completion from a
  fixed sleep. Toolbox waits for its loaded target count before navigation.
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
platform, category, hash, terminal geometry and, where explicitly pinned, the
site's dark/light terminal defaults. Keep a copy of the complete
capture directory for each run. Compare two independent runs with:

```sh
dotnet run --project Website/tools/SiteFrames -c Release -- \
  compare-captures /path/to/windows/captures /path/to/macos/captures comparison.json
```

The command fails if a produced capture is missing, a hash or geometry is wrong,
a neutral cell/style differs, or it finds replacement glyphs, invalid wide-cell
continuations or disconnected adjacent border glyphs. Same-platform runs must
be byte-identical unless explicitly declared `live-measured` below. Cross-platform transport encoding and packet/timing differences
are allowed only when the decoded visible cells and styles match. The foreground
of a one-cell space is invisible unless underlined, struck through or reversed,
and may differ between ConPTY and Unix output. Backgrounds, all decoration flags, links,
glyphs and widths remain strict; undeclared same-platform runs still require identical bytes.
When both receipts declare the same terminal defaults, the comparer resolves
default colours to that palette before comparing: dark uses foreground `#cccccc`
and background `#0c0c0c`; light uses `#24292f` and `#ffffff`, matching the site's
renderer. A missing declaration never grants this equivalence, and conflicting
declarations fail. Structural checks run before colour resolution.
The JSON report lists every failing capture and counts every changed row and cell. It retains the first 32 changed rows and eight changed cells per row as diagnostic samples, with explicit omitted counts, so animated differences cannot produce unbounded reports. A category label
alone never waives a difference; platform values need an explicit comparison
policy. The three tagged `docs/profile*` captures permit native font presence/value
and terminal kind; the detected profile also permits detected colour/glyph values,
their explanations and the built-in muted-label mapping for Ansi256/TrueColor.
The parser checks every complete frame against the current ordered 100x40 schema;
stable fields, dimensions, culture, width policy, links and all other styles stay
strict. Every permitted field change is listed with both values and the original
row/cell diagnostics. Unknown or duplicate rows fail. Undeclared measurements still fail
strict repeated-run comparison. The Windows-only ps5 production difference is listed separately;
it does not exempt those committed files from render freshness and parity.

### Real measurements and concurrency

`classes.json` is maintained capture metadata, keyed by logical capture path (without a platform tag).
Its ten `live-measured` entries give a reason and bounds for complete decoded frames and normalized playback
duration. They cover benchmarks, stress, hosted services, Pester and actual concurrent demonstrations,
including the intentionally unsynchronized threads example. No recorded number or concurrent result is replaced.
The producer copies each declaration into that file's `run.json` entry; the gate requires both declarations
to match. An unknown class, missing reason, missing source stamp or changed policy fails.

For this class, measured cells, order, frame counts and bytes can vary on the same platform and across platforms.
Each side still needs a valid producer hash, matching source fingerprint, geometry, terminal defaults and scripted
reading holds. Every complete frame is checked for broken borders, replacement glyphs and invalid wide cells;
incomplete VT sequences and synchronized repaints fail. Counts and duration must remain within the entry's bounds.
The report lists every exception with its reason, both hashes, frame counts and durations. Everything else retains
strict byte identity on repeated runs and strict visible-cell/style parity across platforms. Adding a metadata
entry is a deliberate reviewed change, never an automatic response to a failed comparison.

Refresh live measurements deliberately after source/backend changes or when a new measurement is intended.
Keep presentation clocks, title timestamps, locale, paths, terminal settings and transport timing pinned;
retain essential elapsed measurements, discovery timing and real concurrent scheduling. Ordinary site builds
render the committed runs and still reject stale source fingerprints.

The comparison is a structural gate. Rendered review is still required to check
the actual font, border paint, compact layout and replay interaction. Measured
benchmark/stress values and intentionally concurrent demonstrations are real
runtime observations, explicitly listed in `classes.json`. Do not claim byte determinism for that class.
