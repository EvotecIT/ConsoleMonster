# Proposal: a design pass for data, hierarchy and layout

Status: implemented; visual review pending. Sections 1–7 are implemented in the library and gallery. Their current
rules live in [Design-System.md](../Design-System.md), under Hierarchy, Bars, Charts and Layout. This proposal records
the agreed scope. The contact sheet and running gallery await Przemek's review before merge.

## Principles

1. **One meaning, one signal.** A colour, a bar style or a marker means the same thing on every screen.
2. **One emphasis per block.** Each block has one thing that stands out (a figure, a failure, a title); everything
   else steps back.
3. **A bar says what it measures against.** A part of a known whole shows the whole; a comparison does not.

## 1. Hierarchy

Four levels, from the top:

| Level | Style | Where |
|---|---|---|
| Screen title | Accent, bold | the first rule or header bar of a screen (`DashboardFrame` already does this) |
| Section title | Title role (bold) | panel and rule titles, dashboard section headings |
| Label | Muted | meter, key-value and fact labels, captions |
| Value | Text; the block's key figure Strong (bold) | values; one figure per block may be Strong |

Changes:

- Meter labels in `DashboardFrame` and keys in `KeyValueList` are Muted; their values are Text, and a meter's main
  figure is Strong. Today labels and values have nearly the same weight ("Servers healthy" reads like "44/48").
- Facts lines (counts at the top of a report: "3 passed · 1 warning · 1 failed · 5 checks") use one widget with one
  format: the count bold in its status colour, the word in the same colour, " · " between facts (ASCII " | "), and
  neutral facts Muted. The internal `ViewFacts` that draws the summary line under default views (PR #130) becomes the
  public widget for this (`New-ConsoleFacts`, `Ui.Facts()`), so demos stop hand-spacing counts with three blanks.

## 2. One bar language

Three kinds of bar, chosen by what the value is measured against:

| Kind | Examples | Shape | Track | Value |
|---|---|---|---|---|
| Part of a whole | progress, quota and disk use, a score out of 100, a meter | `Line` (`━`) | Subtle `─` to full width | right: `72%`, or `72/100` when the whole is not 100 |
| Comparison | bar charts, data bars in tables, histograms | `Block` (`▆`) | none, scaled to the largest | right, in a column |
| Composition | healthy / warning / failing of a total | stacked segments | the remainder | right: the total |

Changes:

- `BarChart` (and `New-ConsoleChart -Type Bar`) given a `Maximum` is a part of a whole, so it draws the `Line` shape
  with a track. Without `Maximum` it stays a comparison.
- `ProgressBar` shows only the percentage when the total is 100 ("72%", not "72% 72/100").
- Composition bars colour only the segments that need attention; the healthy part stays neutral, as the dashboard
  frame's meters already do. Pass green appears where something is newly passing, not as a sea of green.

## 3. Status colour means the status

- A count of errors is in Fail, of warnings in Warn, and critical in the Critical fill. The event log demo shows
  "239 errors" in warning yellow beside a red errors chart.
- The warning symbol becomes `▲` (Full and Basic; it is in WGL4) instead of `▴`, which is too small to read next to
  `✓` and `✗` at normal terminal sizes. ASCII keeps `!`.

## 4. Layout

- **Printed reports keep their natural height.** They are written once into scrollback; padding them would be noise.
- **Live full-screen dashboards fill the screen:** the last section, or one marked to fill, takes the remaining
  rows, so the footer sits at the bottom and nothing floats in the middle.
- **Sections side by side start on the same row,** each with its own heading at the same height. In the server estate
  demo, "Sites" is appended to the end of the "Needs attention" heading instead of heading its own column.
- **A legend sits with what it explains,** as one Muted facts line under that section ("✓ healthy · ▲ warning ·
  ✗ failing · 48 servers"), not stranded at the bottom of the screen.

## 5. Charts that read on their own

- A sparkline or histogram given point labels shows an axis row with the first, middle and last label (00:00, 12:00,
  23:00) and its peak value; without labels it stays as it is.
- Inside a titled panel, the chart drops its own repeated name ("errors" under "Errors per hour").

## 6. Tables

- A column whose cells are all numbers right-aligns by default, also when the cells were added as text, so
  hand-built tables line up like `Show-ConsoleTable` output. An explicit alignment wins.

## 7. Demos

Every gallery demo is reread against these rules and fixed where it breaks them: the readiness report's scores
become parts of a whole, the event log summary's counts take their status colours, the server estate's sections and
legend move, and facts lines use the new widget. Demo text and data do not change.

## Verification

- Reference renders of every static demo in Dark, Light and ColorBlindSafe, compared as cells and styles in CI, so a
  later change that breaks a rule shows up as a diff, plus a contact sheet of PNGs for review.
- Existing goldens change where the defaults above change, and every changed golden is reviewed visually before it
  is accepted.
- `ConsistencyTests` gains the bar-kind and hierarchy rules for the components that implement them.

## Out of scope

Colour scales and gradients, per-side borders and title alignment, elevated cards and big numerals are the next
step, built on these rules once they are settled.
