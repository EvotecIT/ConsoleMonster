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
| Menu item/preset names; grid substring filters and prefix search; log tags/text; tree search | Ordinal, case-insensitive |
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

Run the solution build, all supported core test targets, extension tests, both PowerShell module gates,
and the site-frame checks after a shared rendering change. Review real before/after output before
accepting changed fallbacks, value markers or visual baselines.
