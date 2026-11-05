# Interactive navigation: pointer parity, folding and documents

Status: proposed, 4 November 2026.

Two conversations prompted this. jborean asked for examples of a terminal documentation viewer with collapsible
sections and navigation, for an ansible-doc style browser built on Textual. trackd's
[fullscreen](https://github.com/trackd/fullscreen) module (built on XenoAtom.Terminal.UI) shows how far a PowerShell
TUI can go with mouse support, overlays and reusable controls. We already have the app shell, screens, overlays and
scoped key bindings. What we lack is a document you can navigate, sections you can fold, and the mouse everywhere the
keyboard works.

## Where we stand

| Area | Today |
|---|---|
| Mouse input | SGR and Windows console decoding; click, drag, wheel. Reporting is on only in `ConsoleApp` and `GridView`. |
| Mouse in apps | Click focuses; drag stays with the pressed control; wheel scrolls the viewport; popups take pointer input first and close on an outside click. |
| Mouse-aware controls | Button, Select, MultiSelect, TreeSelect, DataGrid, DatePicker, LogViewport, Tabs headers, command and form modals. |
| No mouse | Standalone prompts, `MenuApp`, the palette, `WidgetPager`, `HelpPager`, Commander, topology `Run()`; in apps, Toggle, TextInput, TextArea, footer hints, breadcrumbs and scrollbars. No double-click or right-click handling anywhere. |
| Expand and collapse | Only `TreeSelect` and `ExplorerView`. No collapsible section or accordion. |
| Documents | `MarkdownView` renders headings, links and tables. `MarkdownConsole.Show` and `Show-ConsoleHelp` page through `WidgetPager`, which only scrolls. `HelpPager` pages screen by screen. |

## What fullscreen does that we should match

- The mouse works in every app: click, double-click to open, wheel, right-click for a context menu with a keyboard
  equivalent, drag to resize list and detail panes, click a column header to sort, and a close button.
- `Collapsible`, `Popup`, `SearchReplacePopup` and `CommandPalette` are ordinary controls a layout can use.
- An in-app diagnostic overlay (F12) shows frame timings and focus.
- A minimal mode hides the chrome (F4).

What we keep that it doesn't have: no runtime dependencies, Windows PowerShell 5.1 and netstandard2.0, ASCII and Basic
glyph tiers, safe output in CI and redirection, recording, replay and HTML/SVG export of every frame, and the headless
driver for tests. Every step below must keep those.

Not adopted: a visual layout designer and data-only layout files. `Show-ConsoleApp` script blocks already describe a
layout; revisit only if a product needs layouts edited outside code.

## Steps

Each step is one PR. Steps 1 and 2 touch different files and can run at the same time; step 3 needs both, and step 4
needs step 1.

### 1. The mouse wherever the keyboard works

- Every interactive surface opens mouse reporting through the same scope as `ConsoleApp`: standalone prompts, menus
  and the palette, `WidgetPager`, `HelpPager`, Commander, the object explorer, and topology `Run()` once it ships.
  `-NoMouse` and `ConsoleAppOptions.Mouse` turn it off everywhere.
- Chrome: a footer hint runs its binding when clicked, a breadcrumb goes back to its screen, scrollbars can be clicked
  and dragged.
- Controls: clicking a Toggle flips it; clicking text places the caret and dragging selects; double-click opens the
  row in grids, trees and lists; clicking a DataGrid column header sorts it.
- Context menus: right-click opens a menu on the overlay layer, built from the focused input's and the screen's
  bindings. The Menu key and Shift+F10 open the same menu from the keyboard.
- Done when each control has one pointer regression at its own boundary, driven through the headless driver, and every
  pointer action has a keyboard path shown in the footer or the context menu.

### 2. Folding, panes and find

- `Collapsible`: a header row with ▸/▾ (`+`/`-` in ASCII), a one-line summary when collapsed ("Examples (6)"),
  Enter, Space, ←/→ and click to toggle. `Accordion` keeps one child open; expand all and collapse all are bindings.
  PowerShell: `New-ConsoleCollapsible`, with `-Accordion` on a group.
- `Splitter`: two panes with a draggable divider and keyboard resize, remembering the ratio per screen.
- Find: `/` opens a find bar on the overlay layer for `LogViewport`, `TextArea` and the pager, with a match count,
  n/N (or Enter/Shift+Enter) and highlighting of every match.
- Done when each control has PowerShell, C# initializer and fluent examples in the gallery, works at ASCII, Basic and
  Full glyph tiers, and records and exports like any other app frame.

### 3. A document you can navigate

`WidgetPager` grows into a document viewer used by `MarkdownConsole.Show`, `Show-ConsoleHelp` and `HelpPager`:

- an outline pane built from headings (a `Splitter`), following the scroll position and jumping on Enter or click;
- the current section's heading pinned at the top, with the path to it;
- headings fold through `Collapsible`, and long sections such as Examples can start folded;
- find from step 2 opens folded sections that contain a match;
- links inside the document jump to their target, with back and forward history;
- the palette lists sections, and for help, parameters, so "go to -Path" is two keystrokes;
- tabs for the top-level parts of a document (for help: Overview, Parameters, Examples, Related), which needs `Tabs`
  brought up to date: an underline on the active tab and a rule under the strip instead of `[label]`, number keys
  for each tab, a strip that can take focus and move with ←/→, the keys shown in the footer, and labels that scroll
  instead of disappearing on narrow terminals. Tab stays for moving focus.

The showcase is PowerShell help browsed like ansible-doc: a header card with the synopsis and badges (module,
version, supports `-WhatIf`), then tabs for Overview with Syntax, Parameters (one row per parameter showing its type,
default and a one-line summary, which expands to its details), Examples (each folded under its title) and Related
links that open the linked command's help.

Done when `Show-ConsoleHelp` and Markdown documents use it, the website has a recording of it, and non-interactive
output is unchanged.

### 4. Charts you can explore

Today `LineChart`, `BarChart`, `Chart`, `Sparkline` and `Flow` take no input; only the topology diagram does. This step
replaces the backlog line on interactive terminal charts.

- A crosshair: ←/→ or a click moves a cursor along X and shows every series' value at that point, with the values in
  the footer for screen readers and the ASCII tier.
- Clicking a legend entry, or pressing its number, hides or shows that series.
- Zoom and pan: +/- and the wheel zoom, drag or Shift+←/→ pans, Home resets. Charts drawn through ChartForgeX use its
  prepared X zoom levels (ChartForgeX #426), so navigation doesn't re-run the source chart; native `LineChart` gets a
  simple X-range zoom.
- Linked to tables: selecting a `DataGrid` row highlights its point, and clicking a point selects its row, through
  shared app state rather than a direct reference between the two controls.
- Done when a chart works inside `Show-ConsoleApp` as an ordinary focusable control, static output and exports are
  unchanged, and the website has a recording of a chart linked to a table.

Needs step 1 for the pointer conventions; it is independent of steps 2 and 3.

### 5. Later

- An in-app diagnostic overlay showing frame time, focus and the layout reasons the debug socket already reports.
- A minimal chrome toggle for apps.
- A number box and masked input as app controls (forms have `NumberField`; the app shell has neither), and a slider.
