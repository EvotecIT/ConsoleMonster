# ConsoleMonster design system

This document is the single source of truth for how ConsoleMonster screens look. Each rule names the component that
implements it, and every surface draws through that component: dashboards, the Shell and Classic menu layouts,
full-screen and inline forms, prompts, runs started from a menu or a form, inline live areas and exports.
`ConsistencyTests` checks that these surfaces render the shared parts identically in each glyph tier.

## Colour roles

Colours are never written directly. Widgets ask the theme for a role (`ThemeToken`), and the theme maps it to the
terminal's colour depth: TrueColor uses the hex value, 256 colours the nearest cube index, 16 colours the listed ANSI
index, and no colour leaves the text plain.

| Role | Used for | Dark (hex / 16 / 256) |
|---|---|---|
| `Text` | ordinary text (terminal default) | default |
| `Muted` | labels, secondary text, footer labels, captions | #8b8f9a / 8 / 245 |
| `Subtle` | dimmed backdrop behind dialogs, empty bar track | #3a3d45 / 8 / 237 |
| `Accent` | keys in footers, dialog borders, focused panel border, filter pointer | #60a5fa / 14 / 75 |
| `Border` | panel borders | #4b5060 / 8 / 240 |
| `Title`, `Header`, `Highlight` | panel titles, table headers, emphasised values (bold) | default, bold |
| `Surface` / `SurfaceAccent` | the header bar band and its title | #e2e8f0 on #1f2a3c / #93c5fd on #1f2a3c |
| `Focus` | the focused row in lists, menus, forms and buttons | #f1f5f9 on #243b5e |
| `Pass` `Warn` `Fail` `Info` `Skip` `Running` `Pending` | status | see the status table |
| `Series1`–`Series6` | charts | blue, green, amber, red, purple, teal |

Light and high-contrast themes define the same roles (`Theme.BuiltIn.cs`).

## Spacing

- Content starts one column from the left edge (header, footer, log lines), or one column inside a panel border plus
  one column of padding.
- Panels inside an app frame have one blank row under the title border (`new Padding(inner, 1, 1, 1, 0)`).
- Dialogs have one blank row under the top border and one column either side (`DialogChrome.Frame`).
- Two spaces separate footer hints. Inline facts are separated by " · " (ASCII " | ").
- The breadcrumb separator is " › " (ASCII " > ").

## Header bar (`HeaderBar`)

One row on the Surface band. On the left: the title (SurfaceAccent), then breadcrumb steps or a subtitle. On the
right: a status with its marker, free text (an app's subtitle or a step count), and the elapsed clock. Without colour
the band is left out and the same text is drawn plain. These surfaces use it:

- dashboards: title · subtitle, status, clock;
- the Shell app: app title › step › step, with the app subtitle on the right;
- runs from a menu: app title › … › Run (the content panel carries the clock);
- full-screen forms: form title › current step.

The Classic layout is a centred card, so its breadcrumb goes in the card title instead of a header bar.

## Footer (`FooterBar`, via `KeyFooter` for interactive views)

One row, no band, starting at column 1 (column 2 inline). Keys use Accent and labels use Muted, written in lower case
(`ctrl+c stop`, `↑↓ move`, `? keys`). Hints that don't fit are dropped from the end; the help hint (`?`/`f1 keys`) is
kept when there is room for it. A toast replaces the hints while it shows. Text on the right is Muted. Keys change
with the state: during a run only `ctrl+c stop` is shown, because no other key applies.

## Panel chrome (`Panel`)

Rounded borders by default (ASCII `+-|`). The title sits on the top border at column 2. Title-right text (the
caption) sits on the top border at the right: counts ("3 of 11 selected"), the step ("2/5"), the clock ("00:00:04"),
or problems ("1 to fix"). In the Shell, the focused panel's border is Accent and other borders are Border. Classic
cards and dialogs always use Accent.

## Focus, selection, disabled

- **Focused row:** a left bar `▌` in Accent, then the row on the Focus background with bold text. Lists, menus, form
  fields and the palette all use it. An inactive list keeps the bar Muted, with no background.
- **Selection:** `[✓]` / `[ ]` / `[–]` (ASCII `[x]` / `[ ]` / `[-]`) in Accent when selected and Subtle when not;
  groups show "n/m" in Accent when partly selected.
- **Buttons:** the focused button sits on the Focus background, bold. A blocked button (Run with problems) is drawn
  Muted, also when focused.
- **Disabled:** Muted text; the reason shows on the info line as "Not available: …".

## Status vocabulary

| Status | Symbol (Full/Basic) | Symbol (ASCII) | Marker (ASCII) | Role |
|---|---|---|---|---|
| pass | ✓ | + | [OK] | Pass |
| warn | ▴ | ! | [WARN] | Warn |
| fail | ✗ | x | [FAIL] | Fail |
| skip | – | - | [SKIP] | Skip |
| running | spinner (●) | spinner (*) | [BUSY] | Running |
| queued | ○ | . | [WAIT] | Pending |
| info | ◆ | i | [INFO] | Info |

`IGlyphSet.StatusSymbol` gives the one-cell symbol (lane markers, activity rows). `IGlyphSet.StatusMarker` gives the
marker for status columns (status lists, phases, the dashboard header). `RenderContext.Spinner()` is the running
symbol for live widgets, so lanes, phases, status lists, activities and spinners turn together. Destructive questions
use the warn symbol in the Warn colour.

For an emoji or application symbol, `SymbolFallback.GetGlyph(symbol, tier)` returns one complete cell on Basic/ASCII.
`GetPrefix` reserves that cell and a separate space (three cells for Full emoji). These APIs never truncate a prose
token such as `[doc]` or a face such as `:(`. ASCII success uses `+` for a cell and `[OK]` for a readable status badge.

`SymbolFallback.Apply` is for prose: it keeps badges, replaces whole Unicode graphemes (including ZWJ sequences),
and pads a narrower emoji replacement before adjacent text. Existing spaces are retained without doubling them.
Basic/ASCII have a generic fallback for unknown emoji; ASCII decorative fallbacks are printable 7-bit text.
Ordinary multilingual content is preserved. Full keeps the original symbols. Hint (`*`) and search (`?`) remain
distinct from warning (`!`). Styled text follows the same rules across span boundaries.

## Icons (`GlyphIcon`)

Menu items take a semantic icon that follows the glyph tier, or free text when an app needs its own mark. Every icon is
one cell wide in every tier.

| Icon | Full | Basic | ASCII |
|---|---|---|---|
| Item | • | • | * |
| Targets | ● | ● | o |
| Tasks | ■ | ■ | # |
| Settings | ◆ | ♦ | * |
| Favorite | ★ | ☼ | * |
| Run | ► | ► | > |
| Back | ◄ | ◄ | < |
| List | ≡ | ≡ | = |
| Help | ? | ? | ? |
| Warning | ▴ | ! | ! |
| Done | ✓ | √ | + |
| Exit | × | × | x |
## Empty, loading and error states

- **Empty:** one Muted line inside the panel ("No presets saved yet", "waiting for work…"). A list with no choices
  shows "No matches" on the info line.
- **Loading:** the spinner and "Loading…" in Muted italic, where the value will appear; Esc cancels.
- **Error:** "✗ message" in Fail, directly under the field or above the buttons. Results with an error show the error
  under the title.

## Toasts and dialogs (`Toast`, `DialogChrome`)

- **Toast:** the status symbol and the text, in the footer row, for about three seconds.
- **Dialog:** `DialogChrome.Frame`, placed by `DialogChrome.Place` (centred, a third of the way down; palettes near
  the top), over a backdrop dimmed to Subtle. Menu dialogs (confirm, name, output, key map, palette), prompts asked
  over a menu (`BoxedSurface`), prompts asked over a paused run (`LivePromptSurface`) and full-screen form dialogs all
  draw through it.

## Numbers and time

- Numbers, sizes and dates use the profile culture (`NumberFormat`, presenters).
- Elapsed time in captions and headers is a clock: `00:00:04`, or `1:02:03` from an hour (`NumberFormat.Clock`). A
  results caption shows the same clock the run showed a moment before.
- Short durations inside content (lanes, phases, activities) are compact: `2.3s`, `1m 05s`, `~42s left`.

## Transitions

- **Menu to run:** the run draws the same app frame: the same header (the breadcrumb gains "Run"), sidebar, content
  panel and footer. Only the panel's content changes. The screen is never cleared between steps; frames continue as
  diffs on the same output (`RunHost`, `BorrowedOutput`).
- **Form to run:** `FormOptions.AfterSubmit` gives the submitted form a `MenuContext`. `context.Run(dashboard, work)`
  runs in the form's frame and ends on the same results pane as menus.
- **Inline:** inline areas draw the same widgets as full screen (lanes, progress, status, activities). Output and
  log lines scroll above the area, with Write-Host colours kept.

## Test runs (`TestRunView`, `TestRunSummary`, `TestRunText`)

- **Live view:** the dashboard header bar (title, totals as facts, status, clock) and footer (`ctrl+c stop`). Panels:
  Files (the test files as lanes, with the skipped count in the third column and "n of m done" as the caption), the
  running test as the blocks in Muted and the test in Text joined by the breadcrumb separator, and on the full screen
  Failures, Slowest and Durations. The compact (inline) layout keeps the files, the running test and the first two
  failures.
- **A failure** reads the same in the live view, the summary and the export: the fail symbol and the path, then
  `expected` and `actual` (labels Muted, values in Pass and Fail, the differing characters underlined) or up to three
  message lines, then the file and line as a link in Muted.
- **Summary:** a table of results by file whose status cell is the row's one colour signal, a totals row, the failures,
  and a closing line with the outcome in its status colour and the totals in Muted.
- **Stream output:** one line per failure (`✗ Block › test: expected 4, actual 3 (File.Tests.ps1:12)`) and one summary
  line, with the separators of the tier.
