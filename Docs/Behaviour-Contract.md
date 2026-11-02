# Behaviour contract

I use these rules when adding a widget, renderer or command. The shared contract suite is
`ConsoleMonster.Tests/Rendering/BehaviourContractTests.cs`; a new widget belongs in its scene table,
with a useful state, rather than in another copy of the same tests.

## Interaction and static output

- Redirected output, CI and non-interactive sessions do not acquire console input. An explicit
  `InteractiveOptions.NonInteractive` also wins over a requested rich mode. Scripted input and explicit
  line consoles are real input devices for capture and embedding; they do not require a physical TTY.
- `ConsoleApp.Run`/`RunAsync`, `Menu.Run`/`RunAsync`, `Grid.Show`, `ObjectExplorer.Show` and their
  PowerShell commands print one initialized static view when input is unavailable. Menus do not invoke
  item actions or build pages. Grids apply their initial filter, sort and hidden columns and return no
  selected rows. Apps return their initialized state; `Show-ConsoleApp -PassThru` still controls pipeline output.
- Questions and forms need answers. They use valid explicit defaults, or throw one
  `PromptUnavailableException` naming the question and the parameter/default needed. Cmdlets report the
  corresponding terminating `PromptUnavailable` error. A static view never invents an answer.
- An unbounded watch or log tail takes one snapshot without interactive input. An explicit finite
  `Count` is honoured. Cancellation still stops readers and restores terminal state.
  `RecordingTarget.CaptureViewport` explicitly records live checkpoints for synthetic programs;
  the producer process's `-NonInteractive` flag does not disable that declared recording destination.
- Static output uses the supplied target, theme, culture and capture. PowerShell uses its ordinary
  host/information output channel; displayed rows do not become selected pipeline objects.

## Matching and ordering

| Feature | Case rule |
| --- | --- |
| Menu item names; grid substring filters and prefix search; log tags/text; tree search; path completion | Ordinal, case-insensitive |
| Saved preset/form names | Ordinal, case-sensitive for lookup and listing; differently cased saved names remain distinct |
| Path completion order | Folders first, then files; ordinal, case-insensitive within each group |
| Grid and live-table string sorting | Ordinal, case-insensitive; equal values retain input/insertion order |
| Numeric/date sorting | Typed value order; formatting culture does not change string comparison |
| Selection/menu/command-palette fuzzy search | Invariant smart case: lowercase terms ignore case; uppercase terms require exact case; exact case also affects ranking |
| Glob patterns | Case-insensitive by default; `GlobPattern.CaseSensitive` selects exact case |
| Object comparison | Case-insensitive by default; `DiffOptions.CaseSensitive` applies to keys, properties and values; case-only differences remain visible |
| State keys, widget IDs, tree value-column IDs, literal dictionary member identities | Ordinal, case-sensitive |

Culture controls presentation of numbers, dates and labels. It does not silently choose a different
matching rule. A PowerShell property expression retains PowerShell's own member-binding semantics;
structural dictionary keys remain data identities.

## Values and glyphs

- Raw data distinguishes an absent member (`(missing)`), a present null/`DBNull` (`(null)`) and an empty
  string or collection (`(empty)`). The missing and empty labels use the existing translation catalog.
  `System.Reflection.Missing.Value` represents an absent value when a caller supplies cells directly.
- Tables, cards, record sheets, key/value lists, trees, inspectors and object diffs use those markers.
  Omitted table cells and missing selected properties are absent values. A deliberately blank markup
  span or layout cell is spacing, not raw data. Numeric NaN/infinity retains the numeric unavailable marker.
- Record sheets show null and empty fields by default. `RecordView.HideEmpty = true` explicitly hides
  them; `ConsoleViewOptions.ShowEmpty` controls inclusion in the list renderer. Hidden fields are not
  converted into missing values.
- ASCII output contains printable ASCII, including caller text. Basic output stays within WGL4.
  Known symbols use their tier alternatives; unsupported graphemes become `?`. Full retains caller text.
  Reduced-tier conversion is a display operation: source values and search inputs
  are not rewritten. Plain log-tail output keeps literal lines and applies only the repertoire restriction.

## Layout, live state and options

- Every widget measures and renders at widths down to one column. It clips, wraps or uses its existing
  compact representation without throwing or repeatedly trying the same layout. Missing-value markers
  may be clipped at those widths; they must not expand the destination.
- `Terminal.Write`/`Show-Console` writes a live widget's current state once outside a live session.
  Rendering does not start a worker, consume input or synthesize updates. Handles update state through
  their dispatcher; owning a handle does not start a session.
- Options apply where their renderer has that capability. Table column presenters, tone rules and
  culture also apply to cards and record fallback. Table borders, separators and column width negotiation
  belong to grid layout; cards use labelled fields. Tree guides/value columns, code line numbers,
  exception stack/source options, chart axes and export format settings belong to their named renderer.
  Interactive actions, mouse reporting and keyboard hints do not execute in static fallback.
- Use one public parameter or member for one choice. Sync/async endings and typed versus object input
  overloads share semantics. A convenience builder delegates to the same implementation. New aliases or
  overlapping selectors need a distinct contract or should be removed before release.

## Terminal handoff and prompt engines

I leave a clean terminal for the next shell prompt, including Oh My Posh with Nerd Fonts,
multiline prompts and a right prompt, on PowerShell 7 and Windows PowerShell 5.1.

- Normal return, Esc, Ctrl+C, host cancellation and exceptions follow the same cleanup path.
  The outermost live session, full-screen app or menu leaves the alternate screen, makes the
  cursor visible, finishes the current line and returns at column zero on an empty line.
  Inline prompts and dashboards erase their owned region; a prompt's answer or cancellation
  summary ends with a line break. Cleanup also works before the first frame is drawn.
- Reset colours and decorations after returning to the normal screen: leaving the alternate
  screen can restore its saved attributes. An explicit profile colour baseline still applies.
  Synchronized output ends, mouse reporting owned by the app is disabled, input is released,
  and saved native console modes are restored. Emergency cleanup restores inner owners first and
  the original entry mode last. Cleanup is idempotent and borrowed writers stay open.
- `Run`, `RunAsync` and disposal wait for final output and restoration. `Stop` requests shutdown
  with a bounded dispatcher join; if a user callback outlasts `StopTimeout`, disposal still waits
  for that callback to return. Callbacks must cooperate with cancellation; they cannot be forcibly
  terminated safely. Calling Stop from the dispatcher requests its loop to finish.
- A prompt or dashboard borrowing an app's screen returns ownership to that app. It does not
  leave the outer alternate screen or release the outer input lease. The outer owner performs
  the final shell handoff.
- Redirected/static output uses complete lines and does not emit interactive terminal modes.
  A writer or terminal that has disconnected cannot receive cleanup; the original failure
  remains observable rather than being replaced by an invented successful handoff.

The shared suite decodes actual output with the headless terminal model and checks final screen,
cursor, attributes and modes. Native console tests check signal handling and mode restoration.
I also check a real terminal with Oh My Posh: run each exit route in both PowerShell versions,
then type and edit a command at the multiline/right-aligned prompt, resize the window and run
another command. Headless checks cannot establish font rendering or the prompt engine's redraw.

Run the solution build, all supported core test targets, extension tests, both PowerShell module gates,
and the site-frame checks after a shared rendering change. Review real before/after output before
accepting changed fallbacks, value markers or visual baselines.
