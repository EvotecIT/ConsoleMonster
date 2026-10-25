# Full-screen apps

Status: approved on 25 October 2026 with the dated decisions below. Phase 2 preview is in review; visual corrections and native Windows interaction remain before release.

I want ConsoleMonster to build full-screen administration tools: browse services,
inspect objects, filter a large result set, choose a maintenance window, run an
action and watch its output without leaving the app. The first useful result is
a coherent application layer over the rendering and interaction code I already
have.

I started this inventory at master `83b8fbe` and rebased the implementation onto
merged [#55](https://github.com/EvotecIT/ConsoleMonster/pull/55), master `3846758`.
API names follow its
[API proposal](https://github.com/EvotecIT/ConsoleMonster/blob/3846758/Docs/api-consistency-proposal.md).
The public action contract and phase 2 examples are implemented on
`feature/fullscreen-apps`; later controls remain proposals.

## Decisions — 25 October 2026

I am proceeding with this design and four amendments:

1. In-app actions ship in phase 2. A command defines `Key`, `Label`, `Shortcut`,
   an optional `ConfirmPrompt` and its action. Buttons, the palette and grid
   actions use that same definition. Confirmation is modal; work runs with
   cancellation, streams into the embedded log, and leaves the app open.
   PowerShell work uses isolated runspaces with explicit arguments and modules;
   C# work uses asynchronous actions with a cancellation token.
2. `Show-ConsoleGrid -Action` ships in phase 2. The convenience hashtable form
   maps action keys to scripts; command objects add labels, shortcuts and
   confirmation. Actions receive the selected row or a captured set of marked
   rows, with details and output visible in the same app.
3. The standalone Calendar and New-ConsoleCalendar ship first in a separate
   small PR from master on `feature/console-calendar`. Month/year display,
   marks, precedence, legend, ISO weeks, culture/first-day override, shared date
   contracts and an operations-calendar gallery twin do not depend on the host.
4. The phase 2 preview is a service browser with list, detail, in-app actions,
   confirmation and streaming log. I will record keyboard/mouse interaction,
   export a paced GIF and HTML, capture macOS Terminal and SSH, and identify
   remaining native Windows conhost/Windows Terminal proof. Visual review covers
   the calendar, browser and preview GIF before merge.

The calendar PR opens first. Phase 2 stays in one PR on
`feature/fullscreen-apps`, with logical commits and one complete push. Neither
PR merges as part of this work. API vocabulary follows merged round 1 in #55.
Round 2 lands after the open feature PRs and rebases onto them; I keep its
proposed consolidation and names out of this implementation.

### Public action contract

`New-ConsoleCommand -Key -Label -Shortcut -ConfirmPrompt -ScriptBlock
-ArgumentList -ImportModule` creates a passive `AppCommand` definition. `Key`
identifies the command, not its live output destination. The host captures row
arguments at invocation, asks once before the batch, executes rows in order and
streams each operation's output to its log. An omitted confirmation executes
without an extra question; a supplied prompt defaults to cancellation.

C# `AppCommand` receives `AppActionContext` and `CancellationToken` and returns
`Task`. The context exposes the captured rows and an output sink. App-owned
state and controls are updated only by posted completion messages. PowerShell
scripts are serialized as source with explicit data arguments; captured closure
variables are not shared with worker runspaces. A failure stops the current
batch and remains visible; cancellation leaves the viewer open.

```powershell
$restart = New-ConsoleCommand -Key Restart -Label 'Restart service' -Shortcut Ctrl+R `
    -ConfirmPrompt 'Restart the selected services?' -ScriptBlock {
        param($Row)
        Restart-Service -Name $Row.Name -ErrorAction Stop
        Get-Service -Name $Row.Name
    }
Get-Service | Show-ConsoleGrid -Action $restart
# Convenience form supplies a default confirmation; use a command to customize it:
Get-Service | Show-ConsoleGrid -Action @{ Inspect = { param($Row) Get-Service $Row.Name } }
```

## Principles

- Keep the core zero-dependency, including layout, controls, input and rendering.
  Terminal widgets belong in ConsoleMonster; ChartForgeX supplies none of them.
- Keep `New` for definitions, `Show` for viewers, `Read` for answers, and
  `Start` for operations that actually return an active handle.
- Use `Key` for app values, `WidgetId` for live addresses, `Label` for item text,
  `Title` for headings, `Status` for outcomes and `Role` for theme styling.
- Make every action reachable by keyboard. Mouse and colour improve the
  experience but carry no exclusive functionality.
- Share controls between prompts, forms, viewers and apps. Keep data access,
  service management and application policy in the caller.
- Build the admin workflows first. A CSS engine, desktop window manager and
  large animation system are outside this proposal.

## What I already have

Paths below are relative to the repository root. This is a source inventory,
not a claim of fresh native terminal qualification.

| Existing area | What works well | Full-screen limit and disposition |
| --- | --- | --- |
| `Interactive/App`: `ConsoleApp`, `AppState`, `AppBinding`, `AppController` | Stable input controls inside ordinary layouts; state-driven display bindings; synchronous and async C# entry points; PowerShell callbacks on the input thread; initialized snapshots | **Keep and extend** `Show-ConsoleApp`/`ConsoleApp.Run`. Bindings reject input controls, and discovery assumes a stable tree. Introduce explicit mounted subtrees for tabs/dialogs; do not recreate inputs on every binding refresh. |
| `Interactive/App/Controls` and `Interactive/Controls` | Select, MultiSelect, Toggle, Button, TextInput and TextArea; shared editing/selection internals | **Keep** models and editors. Add routed pointer events and shared focus/validation contracts. A mouse press currently focuses a control; it does not activate a button or select the clicked row. |
| `FocusManager`, `AppViewport` | Declaration order plus `TabIndex`, disabled-control skipping, focus requests, whole-app scrolling to reveal focus, narrow-column reflow | **Extend** to active focus scopes, visibility and nested scroll views. The current viewport renders a content-height buffer; it is not virtualization for large tables/logs. |
| `Interactive/Forms` and prompts | Typed values, validation, defaults, presets, review, wizard pages, rich/line/accessible modes | **Keep** answer and validation models. Reuse their editors as persistent app controls. Retain line prompts; a full-screen app cannot become an unattended answer source. |
| `Interactive/Menus` | Navigation, action execution, command palette, toasts, review and output panes; queued calls to the menu thread | **Merge infrastructure** into the app host: command routing, overlay stack, action output and dispatch. Keep `Show-ConsoleMenu` as a convenient composition. |
| `Interactive/Grid` | Filter, sort, paging, marking, column selection and detail view; returns original objects; uses table presenters | **Extract** an embeddable data-grid control. Keep `Show-ConsoleGrid` as the viewer and preserve its selection contract. `New-ConsoleGrid` remains a layout grid. |
| `Interactive/Explorer` | Lazy member expansion, paths, search and inspection budgets | **Extract** an interactive tree/detail composition. Keep `Show-ConsoleObject`, lazy evaluation, cycle handling and budgets. Do not replace inspection with eager reflection. |
| `Interactive/Views`, `Surfaces`, `InteractionLoop`, `WidgetPager` | Modal chrome, overlays, caret handling, pager, screen/inline/recording surfaces; burst input processing and animation ticks | **Merge** loop and overlay mechanics with the app host, then retire duplicate paths after migrations. Keep inline surfaces for prompts and pagers. The older loop routes keys/paste but does not dispatch mouse events. |
| `Layout`, `Canvas`, `Rendering` | Rows, Columns, Grid, Stack, Panel, constraints; Unicode-aware cells; `FrameDiff`/`DiffRun`; VT alternate screen and synchronized frame writes | **Keep** this rendering foundation. Diffing already reduces terminal writes; it still scans complete frames. Add layout metadata, clipping and damage tracking without another buffer/encoder. |
| `Live`, `Widgets`, `Input`, `Testing`, `Recording` | Live handles and dispatcher, tables/logs/charts, input lease, VT/native Windows input, `ScriptedInput`, captures/goldens, child-terminal recorder | **Reuse** them. Extend input beyond button transitions and give embedded live content the app's single screen lifetime. Keep terminal recordings distinct from dashboard event recordings. |

The current app is already full-screen in the literal sense: it uses a
`LiveSession`, rejects inline mode and reaches `VtOutput`'s alternate screen.
What is missing is composability across the complete interaction stack.

## Expectations from other frameworks

I use these projects as design references, not dependencies. Terminal.Gui v2
offers a retained view hierarchy with layout, navigation and scrolling;
Textual combines widgets, layouts, screens and asynchronous work. Ratatui
concentrates on immediate-mode rendering and leaves event orchestration to the
application/backend. Bubble Tea contributes a message/update/effect model;
its ecosystem supplies higher-level components. These are different starting
points, not interchangeable widget catalogues.
[Terminal.Gui v2](https://tui-cs.github.io/Terminal.Gui/docs/migratingfromv1),
[Textual layouts](https://textual.textualize.io/guide/layout/),
[Ratatui rendering](https://ratatui.rs/concepts/rendering/),
[Ratatui events](https://ratatui.rs/concepts/event-handling/),
[Bubble Tea v2](https://github.com/charmbracelet/bubbletea/blob/main/UPGRADE_GUIDE_V2.md).

| Expectation | Terminal.Gui v2 | Textual | Ratatui / Bubble Tea | ConsoleMonster decision |
| --- | --- | --- | --- | --- |
| Layout containers | View geometry, computed sizing, adornments | Vertical/horizontal/grid layouts and docking | Constraint rectangles / composed component views | Keep Rows/Columns/Grid/Stack; add clipped viewports and pinned shell regions. Essential. |
| Focus and tab order | Nested navigation and focusable views | Focusable widgets, traversal and bindings | Usually application/component state | Shared scopes, stable order, restore focus after dialogs. Essential. |
| Scroll views | View content and viewport scrolling | Overflow and scroll containers | Stateful scrolling widgets/components | Nested scrolling, keep-selected-row visible, visible-row rendering. Essential. |
| Dialogs | Runnable/modal views | Modal screens with results | Overlay composition/application state | One overlay stack with typed results and focus trapping. Essential. |
| Menus and palette | Menu controls and command infrastructure | Command palette and actions | Application or ecosystem components | Reuse menu palette/provider code; one command definition feeds buttons, menus and shortcuts. Palette first, elaborate menu bars later. |
| Status and shortcuts | Status controls, commands/key bindings | Footer bindings and actions | Widgets/components plus application bindings | Reuse HeaderBar/FooterBar/KeyHint; show busy/error state and active shortcuts. Essential. |
| Resize | Recompute view layout | Relayout containers/screens | Application handles size and redraw | Relayout and invalidate hit regions; preserve values, selection and focus. Essential. |
| Alternate screen | Driver/application lifetime | Application screen lifetime | Backend setup / declarative View option in Bubble Tea v2 | One terminal lease; restore the shell on all normal and exceptional exits. Essential. |
| Theming | Configurable view schemes | Themes and CSS styling | Styles/component styling | Extend existing semantic theme roles; light/dark/ANSI16/monochrome/ASCII. No stylesheet language. |
| Mouse | Routed mouse input | Pointer events and capture | Backend events / mouse messages | Click and wheel first, drag for ranges and scrolling, optional hover. Never required. |
| Background work | Main-loop dispatch | Workers, messages, UI-thread calls | Application tasks / command results as messages | Bounded work queue, cancellation, immutable results applied on the app thread. Essential for remote admin tasks. |

The comparisons above concern documented models, not tested equivalence.
Further references:
[Terminal.Gui layout](https://tui-cs.github.io/Terminal.Gui/docs/layout),
[Terminal.Gui views](https://tui-cs.github.io/Terminal.Gui/docs/View),
[Textual screens](https://textual.textualize.io/guide/screens/),
[Textual palette](https://textual.textualize.io/guide/command_palette/),
[Textual workers](https://textual.textualize.io/guide/workers/),
[Ratatui layout](https://ratatui.rs/concepts/layout/).

For PowerShell, object identity, usable defaults, runspace safety and bounded
remote actions matter more than custom drawing effects. For .NET CLIs, I also
need cancellation, async completion and straightforward integration into an
existing host. Neither audience should implement focus traversal or terminal
restoration for each tool.

## Architecture

### Layout and controls

Keep `IWidget.Measure`/`Render`, `Constraints`, `LayoutSize`, `Region` and
`CellBuffer`. Add an internal arrangement pass for interactive nodes: measured
size, allocated rectangle, clipped viewport, content offset and z-order.
Existing display-only widgets remain valid children without implementing input.
Hit testing uses this same arranged geometry rather than separately calculating
where a control ought to be.

Rows and Columns retain fixed/auto/fraction sizing and responsive collapse.
An app shell allocates header and footer first, then the remaining body. A
scroll view owns offsets and clips its child. Tables, trees and logs render
only visible rows; a general scroll view may measure ordinary small content,
but must not allocate a buffer proportional to an unbounded log. At tiny sizes,
show a compact navigation/resize message rather than negative rectangles or
silently inaccessible actions.

Retain stable control instances. Mount/unmount is explicit for tab pages and
dialogs; it updates layout, focus and live registrations together. Hidden tab
pages retain their values but cannot take input. Keep bindings display-only.
Value controls use `Key`; live regions use `WidgetId`; reject duplicates in
their respective app-wide namespaces before entering terminal modes.

Extend `FocusManager` with a stack of scopes. Tab/Shift+Tab visit visible,
enabled controls in `TabIndex`, then declaration order. A dialog traps focus,
remembers its invoker and restores the nearest surviving control on close.
The nearest scroll ancestor reveals the focused item. A scrollbar is operable
without introducing an extra tab stop for every decoration.

### Events, commands and work

Consolidate `ConsoleApp` and `InteractionLoop` behind one host. Its cycle is:
read bounded input/messages, apply state changes and completed work, update
layout if needed, render invalidated content, present one frame, then wait for
input or the next timer deadline. Drain bursts with a budget so a busy log or
mouse stream cannot starve keys. Retain a frame-rate ceiling; idle apps do not
continuously redraw.

Route keys through the active modal scope, focused control and its ancestors,
then app commands. Reserved cancellation is handled by the host. Enter stays
inside editors before invoking a dialog's default button. Escape first cancels
an edit/filter, then closes an overlay; app exit is the final fallback. Do not
intercept ordinary text as a global letter shortcut while an editor has focus.
Reuse `KeyChord`/`KeyBinding`; a shared command has a stable `Key`, `Label`,
gesture, enabled reason and action. Footer hints and palette entries derive
from that registry. Pointer routing uses the same active scope and command.

One app thread owns controls, state transitions, focus, layout and user
callbacks. C# `Run` blocks on that thread. `RunAsync` may run that same host on
a worker thread, as the existing API does; it does not promise callbacks on
the caller's synchronization context. Async actions run outside the loop and
post results back through a bounded dispatcher. Do not run an async callback
as `async void` or let its continuation mutate the control tree. Retain the
existing live dispatcher, but publish complete cell frames to any output
worker; it must not traverse mutable controls or execute callbacks.

PowerShell `Show-ConsoleApp` remains synchronous on the calling pipeline thread.
`-OnChange`, `-OnClick`, display expressions and bindings execute there, in their
creating runspace, as #55 already arranges. A short synchronous callback is
allowed and blocks interaction while it runs. Long work needs an explicit
isolated runspace operation with `ArgumentList` values and declared module
imports, not `Task.Run` around a captured script block. Results and PowerShell
streams return as messages; UI completion handlers run on the original thread.
Do not share `PSCmdlet`, `PSVariable`, control instances or mutable `PSObject`
graphs across those runspaces. Snapshot arguments and identify rows by stable
data keys; an action completes against its original target even if focus moves.

Use primitives available on .NET Framework 4.7.2/.NET Standard 2.0: queues,
wait handles, `Task` and `CancellationToken`. Windows PowerShell 5.1 is a first
release target; no `ForEach-Object -Parallel`, `DateOnly`, required Channels
package or PowerShell 7 syntax in its path. `StopProcessing` cancels queued and
active work. Bound worker shutdown; reject late completions after app closure.
Surface action failures in a log/dialog without corrupting the screen. Native
children that require terminal ownership need a deliberate suspend/restore
handoff, not simultaneous console access.

### Rendering and lifetime

Start with complete composition and the existing `FrameDiff.Compute` and
`VtOutput`. That already emits only changed cell runs and preserves wide
grapheme boundaries. Add separate layout and paint invalidation before
optimizing partial rendering. Damage is a union of clipped rectangles, expanded
for borders, wide-cell neighbours and both old/new bounds when content moves.
Closing a dialog damages its former footprint and redraws the layers beneath.
Damaged areas are cleared/recomposed in z-order; untouched cells remain in the
back buffer. Restrict diff scans to damage only after equivalence tests prove
the result matches full composition. Resize, theme or width-profile changes
invalidate the complete frame. No claim of faster rendering precedes measurement.

One host owns the input lease, alternate screen (`1049`), cursor, paste/focus
reporting and mouse modes. Extend existing guards/surfaces instead of opening
another screen inside each dialog or dashboard. Restore modes in reverse order
on success, cancellation, callback failure and partial startup; abrupt process
termination remains outside a managed cleanup guarantee. Keep the encoder's
bottom-right-cell scrolling protection. Synchronized output is an optional
terminal optimization, never a correctness requirement.

Noninteractive hosts get an explicit `-StaticSnapshot` or a clear unavailable
error for a running app. Never enter terminal modes on redirected output.
`Read-*` retains its line/accessible/default policy; `Show-ConsoleApp -PassThru`
returns one final state hashtable, and ordinary runs emit no success objects.

### Embedded live widgets and dashboards

Any existing `IWidget`, including a calendar or dashboard, can occupy an app
region. Register its live descendants once with the app's dispatcher; existing
`Update-ConsoleProgress`, `Update-ConsoleLiveTable`, `Write-ConsoleLogEntry` and
other typed updates still address `WidgetId`. A replaceable region uses
`New-ConsoleRegion`/`Set-ConsoleRegion` vocabulary from #55.

Embedded dashboards borrow the host's frame clock, terminal output and screen
lifetime. They do not start a second input loop or independent alternate screen.
Extend the existing borrowed-output/prompt-host approach into scoped app
services. Stream messages append to a bounded `LogRegion`; selection and focus
stay stable while values update. Deactivating a tab pauses its rendering, not
necessarily its data collection; disposing a region unregisters its live targets
and cancels work scoped to it. Missing live addresses fail visibly rather than
silently updating a detached widget.

## Mouse input

The existing VT decoder accepts SGR button press/release and modifiers; it
deliberately discards motion, wheel and extra-button reports. Native Windows
translation likewise retains button transitions only. `MouseReporting` currently
enables `1000` and `1006`. These are useful foundations, not full pointer support.

SGR reporting uses `ESC [ < button ; column ; row M` for presses/motion/wheel and
`m` for releases, with one-based cell coordinates. Decode to zero-based cells;
separate button, modifier, motion and wheel bits. `1006` selects the encoding;
`1000` reports buttons, `1002` adds motion with a button held, and `1003` adds
unpressed motion. Wheel reports have no matching release. This follows the
[xterm mouse protocol](https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Mouse-Tracking).

| Input | Decoder/host addition | Control behaviour |
| --- | --- | --- |
| Click | Down/Up action, changed button, held-button set, modifiers and position; synthesize Click in the host | Focus on press; activate on matching release inside the target. A button fires once, not on both transitions. |
| Wheel | Signed vertical/horizontal delta distinct from button identity | Nearest scrollable ancestor under the pointer consumes it; parent receives any unconsumed movement. Calendar wheel changes month. |
| Drag | Move with held buttons; pointer capture until release/cancel | Calendar ranges, text selection and scrollbar dragging; continue tracking outside the original bounds. |
| Hover | Move with no held button; synthesized enter/leave | Optional hint/highlight only. Coalesce motion, never drop button transitions. |

Extend `InputEvent` with explicit pointer action/deltas instead of assigning
wheel numbers to `MouseButton`. Extend both VT and Windows decoders and
`ScriptedInput(IEnumerable<InputEvent>)`. Preserve fragmented-sequence parsing,
bounded numeric fields, Escape disambiguation, bracketed paste and the rule that
unsupported reports never leak into typed text. Hit testing accounts for scroll
offsets, clipping, overlays and current resize generation. Clear capture on focus
loss, cancellation, removed targets or resize; discard queued pointer events
whose coordinates belong to an obsolete arrangement. Keyboard input survives.

| Environment | Intended route | Degradation and qualification |
| --- | --- | --- |
| Windows Terminal | Use the existing selected Windows-record or VT input backend; extend both, never read both concurrently | Verify click/wheel/drag with the actual backend. Hover remains optional; `-NoMouse` leaves all keyboard paths. |
| Windows conhost | Native `ReadConsoleInput` mouse records, preserving original modes; convert buffer coordinates using the current window origin | Handle move/wheel flags and signed wheel high word. Scope Quick Edit changes to the lease and restore them. Older/non-VT output can refuse a rich app while prompts retain line fallback. |
| SSH with a PTY | VT/SGR end to end; capabilities depend on client terminal and intervening hops | Keyboard-first over latency; no timeout-based assumption that silence proves mouse support. Without a PTY, use snapshots/line prompts or fail clearly. |
| tmux, including SSH inside tmux | Request normal terminal mouse modes inside the pane; respect tmux routing | User bindings/copy mode can consume events. Document the tested `mouse` configuration; do not change it. Qualify mouse on/off and keyboard-only operation. |

Windows record semantics are documented in
[MOUSE_EVENT_RECORD](https://learn.microsoft.com/en-us/windows/console/mouse-event-record-str);
output support is separate from input support, as described in
[Windows VT sequences](https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences).
tmux's own mouse option and bindings participate in routing:
[tmux mouse usage](https://github.com/tmux/tmux/wiki/Getting-Started#using-the-mouse).
The table is a support plan; native runs must establish the actual matrix.

Default to button/wheel/drag reporting when supported, with hover opt-in.
Retain `-NoMouse`; introduce a proposed `-MouseMode Buttons|Drag|Hover` override
only if the single Boolean cannot express needed policy. Never enable reporting
for snapshots, captures without a terminal, CI or redirected output. Keep native
terminal text-selection behaviour available through documented terminal gestures.

## Controls to ship

All names marked **new** below are proposed additions. Existing constructors
remain definitions; constructing an interactive control does not start input.

| First complete release | Reuse and API direction | Required behaviour |
| --- | --- | --- |
| List | `New-ConsoleSelect`, `New-ConsoleMultiSelect`; `Select`, `MultiSelect` | Original object values, filtering, disabled choices, visible-row navigation, keyboard and pointer selection. |
| Table/grid | **new** `New-ConsoleDataGrid -Key`; C# `Interactive.DataGrid` extracted from GridView | Filter/sort, stable row identity, marking, scrolling, column sizing and row detail. Preserve `Show-ConsoleGrid` output semantics. |
| Tree | **new** `New-ConsoleTreeSelect -Key`; C# `TreeSelect` | Expand/collapse, lazy children, bounded traversal/search, selection and scrolling. Static `New-ConsoleTree` stays display-only. |
| Text input | `New-ConsoleTextInput`, `New-ConsoleTextArea` and shared editors | Caret, selection, paste, validation and clipped scrolling. Keep secret handling in its existing prompt/form boundary. |
| Buttons | `New-ConsoleButton` | Enter/Space/click, disabled reason, command invocation and busy state. |
| Checkbox and radio | `New-ConsoleToggle` already renders a checkbox; extend `New-ConsoleSelect` with **new** `-Presentation Radio` | One Boolean/choice model, labelled hit areas, Space/arrows, disabled choices. No duplicate answer types or checkbox aliases. |
| Tabs | **new** `New-ConsoleTabs`/`New-ConsoleTab`; C# `Tabs`/`Tab` | Active-page scope, keyboard/click switching, retained values and predictable focus restoration. |
| Dialog | **new** `New-ConsoleDialog`; shared existing modal chrome and prompt models | Result/cancel, focus trap, default/cancel actions, constrained size and scrollable body. A composed dialog is hosted by the running app. |
| Log/output pane | #55's `New-ConsoleLogRegion`/`LogRegion`, **new** scroll-view adapter | Bounded capacity, severity, follow-tail pause/resume, copy and horizontal/vertical navigation. Capture action streams without writing over the frame. |
| Calendar/date picker | **new** `New-ConsoleCalendar`, `New-ConsoleDatePicker`, `Read-ConsoleDate` | Shared date rendering/model, marked days, keyboard/mouse navigation, ISO weeks and inclusive range selection. |

The shell also needs a shared scroll view, focus scopes, command palette,
header/status/footer and shortcut registry. Those are first-release plumbing,
not a later polish phase. Reuse existing menu commands and bars before adding
more shell cmdlets.

Later candidates: dedicated menu bar/context menus, file/directory picker,
splitter handles, numeric/path/date form editors, richer validation summaries,
autocomplete, multi-column trees, editable tables and richer text editing.
Existing numeric/path/secret prompts and form fields remain supported while
persistent app variants wait. Floating movable windows, docking workspaces,
stylesheet parsing, browser hosting and pixel graphics are not first-release work.

## Calendar as display, control and prompt

I want a useful calendar even when no app is running. The proposed
`New-ConsoleCalendar` returns an `IWidget`; `Show-Console` displays it and
`Export-ConsoleFrame` exports it. A month resembles `cal`; a year arranges twelve
months according to available width. It composes inside panels/dashboards
without reading input.

```powershell
# Proposed calendar APIs; Show-Console and Export-ConsoleFrame follow #55.
$days = @(
    [pscustomobject]@{ Date = [datetime]'2026-10-02'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject]@{ Date = [datetime]'2026-10-08'; Status = 'Info'; Label = 'Change window' }
    [pscustomobject]@{ Date = [datetime]'2026-10-23'; Status = 'Warn'; Label = 'Certificate expires' }
)
$days | New-ConsoleCalendar -Year 2026 -Month 10 |
    Show-Console
$days | New-ConsoleCalendar -Year 2026 -View Year |
    Export-ConsoleFrame -Path ./operations-calendar.svg -Format Svg
$window = Read-ConsoleDate -Title 'Maintenance window' -Range `
    -Default ([datetime]'2026-10-08') -InputObject $days
```

Proposed data parameters are `InputObject`, `DateProperty` (default `Date`),
`StatusProperty` (`Status`) and `LabelProperty` (`Label`). Keep original rows
available for day details. A reusable day model groups multiple rows, retains
every label and derives one displayed status with documented precedence:
Fail, Warn, Running, Pending, Pass, Info, Skip. An unmarked day is neutral,
not Pending. No implicit holiday or remote data provider is included.

Use `StatusKind`, the existing theme roles and glyph fallbacks. Marked days
have a visible symbol/legend as well as status colours; focused/selected/today
decorations remain distinguishable from the status. Conflicting labels appear
in a detail pane/dialog, rather than being silently overwritten.

Share date arithmetic and month geometry between display `Calendar`, persistent
`DatePicker` and `DatePrompt`/`Prompt.Date`. Use a date-only value represented
by midnight `DateTime` with `Kind.Unspecified` across all target frameworks.
Do not use `DateOnly` in the common API. Interpret supplied dates by their
calendar components; callers convert expiry timestamps to their chosen business
time zone before binding. Do not convert them through the machine's local zone.
First release uses Gregorian arithmetic with culture-aware month/day names,
first day from `CultureInfo.DateTimeFormat.FirstDayOfWeek`, and an explicit
`FirstDayOfWeek` override. Other calendar systems need a separate decision.

Show ISO week numbers in a gutter, including the ISO week-year at year
boundaries. For a Sunday-first row, label the ISO week of the Monday contained
in that row; its Sunday can belong to the previous ISO week. State that rule
in help rather than calling culture-specific week numbering ISO. Implement the
same ISO rule on net472; modern `ISOWeek` availability cannot become a dependency.

| Picker interaction | Contract |
| --- | --- |
| Arrows; Home/End | Move a day/week or to the first/last day of the displayed week. Keep focused date separate from committed selection. |
| PageUp/PageDown; Ctrl+PageUp/PageDown | Previous/next month or year; clamp a missing day, including 29 February. |
| Enter; Escape | Commit or cancel. In a prompt, cancellation follows #55's `PromptCanceledException`; it never returns today's date as a successful answer. |
| Shift+arrows; Space then navigation | Extend a range from an anchor; the anchor workflow works when a terminal cannot transmit a modified key. |
| Click; wheel; drag | Focus a day, change month, select an anchored range. During drag, edge navigation is rate-limited; no hover dependency. |
| Minimum/Maximum and unavailable days | Reject invalid commits visibly. For ranges, reject a range containing an unavailable day rather than silently cutting or splitting it. |

`New-ConsoleDatePicker -Key Window -Range` uses persistent `Value`;
`Read-ConsoleDate` uses prompt `Default` and returns one date, or a **new**
`DateRange` with inclusive `Start`/`End`. With `-Range`, a scalar default means
`Start = End`; the full range type is also accepted. A `DateRange` is one result,
not an enumerated list of every day. Add `FieldKind.Date` only with the later
form integration, sharing this same value/validator model.

Line/accessible prompts accept an explicit ISO date, plus unambiguous
culture-aware entry; display the expected format and validate bounds. Range
entry asks for start and end. Noninteractive calls need a valid explicit default
or fail under the existing prompt policy. Inject today's date/clock for tests.

A GitHub-style year heat strip is a later `Calendar -View Heatmap` presentation:
seven weekday rows by 53–54 week columns, clipped/padded at year boundaries,
with month labels and date-aware details. `CellMap` already owns generic indexed
status cells, row labels and a legend; it does not know dates, missing days,
week alignment or leap years. Factor/reuse its cell painting and legend where
useful. Keep calendar date-to-cell mapping in the calendar model, rather than
adding a second generic CellMap. Quantitative intensity would need an explicit
value/scale contract; status colours alone do not imply a contribution count.

## API sketches

These are the phase 2 APIs I use. I keep one command definition for a
button, the palette and grid actions. The PowerShell service example targets
Windows; the portable gallery twin uses fictional service snapshots.

```powershell
$restart = New-ConsoleCommand -Key Restart -Label 'Restart service' -Shortcut F5 `
    -ConfirmPrompt 'Restart the selected services?' `
    -ImportModule Microsoft.PowerShell.Management -ScriptBlock {
        param($Row)
        "Restarting $($Row.Name)"
        Restart-Service -Name $Row.Name -ErrorAction Stop
        "Restarted $($Row.Name)"
    }
Get-Service | Show-ConsoleGrid -Title Services -Property Name, Status, StartType -Action $restart

# The short form confirms by default and is available through the palette/buttons.
Get-Service | Show-ConsoleGrid -Action @{
    Restart = { param($Row) Restart-Service -Name $Row.Name -ErrorAction Stop }
}
```

Space marks rows; otherwise the focused row is the action input. F5, a button or
Ctrl+K opens the same confirmation. No is the default. I freeze the row set
before confirmation, then serialize PowerShell rows and explicit `-ArgumentList`
values into an isolated runspace with `-ImportModule` imports. Caller variables,
functions and live object methods do not cross that boundary. Streams go into
the embedded log; F6 requests cancellation; Escape leaves the app. One action
runs at a time, with marked rows processed in display order. A PowerShell error
stops the remaining batch. Service operations are not rolled back.

```csharp
var services = new ConsoleMonster.Interactive.Grid("Services");
services.AddColumn("Name").AddColumn("Status").AddColumn("StartType");
services.AddRow("Spooler", "Spooler", "Running", "Automatic");
services.AddRow("WinRM", "WinRM", "Running", "Automatic");
var restart = new AppCommand("Restart", "Restart service", async (context, cancellation) => {
    foreach (var row in context.Rows) {
        context.Write($"Restarting {row}");
        await serviceClient.RestartAsync((string)row!, cancellation);
        context.Write($"Restarted {row}");
    }
}) {
    Shortcut = new KeyChord(ConsoleKey.F5),
    ConfirmPrompt = "Restart the selected services?"
};
var options = new GridOptions();
options.Commands.Add(restart);
services.Show(options, cancellationToken);
```

`serviceClient` is the CLI's backend. The core owns no service-management
provider. In a composed app, `new Button(restart)` binds the same definition;
`ConsoleAppOptions.Commands` and `Selection` supply palette commands and row
snapshots. `Show-ConsoleApp -Command $restart -SelectionKey Service` does the
same for a named PowerShell input. Display widgets remain ordinary layout
children; `ScrollView` / `New-ConsoleScrollView` supplies a clipped viewport.
A per-run `AppDispatcher` accepts bounded C# UI updates and closes when the host
exits. PowerShell input callbacks stay on their owning pipeline thread.

The full service-browser gallery program has PowerShell and C# twins. Its
fictional restart takes the same confirm/work/log path without changing the
machine. I use that program for the phase 2 preview and native captures.

## Proof before calling it an app framework

| Proof | What I need to observe |
| --- | --- |
| Layout goldens | Extend `TestTerminal`, `GoldenFile`, `ScreenCapture` and `RecordingSurface`: 120×40, 80×24, 40×12 and a tiny terminal; narrow collapse, clipped scrolling, tabs, dialogs and long labels. Fix culture, clock, theme and dimensions. Cover light/dark, ASCII and monochrome with a representative matrix. |
| State and scripted input | Use existing `ScriptedInput` events for keys/paste/resize/button input; add normalized move/wheel/drag events. Assert returned objects, focus, scroll offsets, command counts and cancellation, not just pictures. Confirm disabled/hidden controls never activate. |
| Decoder/transport | Feed SGR at every split boundary, including wheel/modifiers, drag-release outside bounds, malformed/overlong reports followed by valid keys. Test Windows records, origin offsets and wheel signs. Never turn a wheel into a click. |
| Rendering correctness | Compare damage-based frames to full composition for scroll, moved/closed overlays and wide graphemes. Verify unchanged frames emit no cell updates and resize forces an appropriate complete redraw. |
| Calendar contracts | Leap days, 28/29/30/31-day months, ISO week-year boundaries, Sunday/Monday starts, culture labels, duplicate dates, status precedence, unavailable dates, reversed/ranged selection, min/max and no time-zone drift. |
| Work/lifetime | PowerShell 5.1 and 7 callback affinity; async C# completion/cancellation; worker failure, late results and bounded log backlog. Verify terminal modes and input lease after startup failure, action error and Ctrl+C. |
| Native interaction | Windows Terminal, conhost, macOS/Linux terminal, SSH PTY and tmux. Exercise resize, keyboard-only fallback, pointer range selection and restoration. Unit tests and recordings do not substitute for these runs. |

Record the service browser and calendar with the existing whole-session
recorder in a child PTY/ConPTY, including resize and a failure/cancel path:

```powershell
# #55 vocabulary. In the child shell, run ./browser.ps1, then exit.
Invoke-ConsoleRecording -Interactive -Destination ./fullscreen.cast -Width 120 -Height 40
Export-ConsoleRecording -Path ./fullscreen.cast -Destination ./fullscreen.html -Format Html
```

Use `SessionRecorder.SendInput`/`Resize` from the external capture driver for
repeatable keyboard and SGR interactions. Script recording does not itself
forward physical input; use the interactive route above for a human session.
The current Windows `TerminalInputRelay.ReadAvailable` discards non-key console
records. Physical Windows mouse recording therefore needs a bounded relay
extension and native proof; synthetic SGR injection cannot establish it. Keep
capture controls out of copied app code. Verify alternate-screen/cursor
rendering in the cast/export player, not only the final frame.
The recorder forwards raw input without storing it;
keep deterministic input scripts as separate synthetic test fixtures, never
record real secrets to reproduce a session. Dashboard JSONL records live events
and cannot by itself reconstruct focus, tab, modal and pointer behaviour.

### Phase 2 evidence — 25 October 2026

I rebuilt the grid's action flow on the shared host and retained the existing
app, menu, grid and explorer contracts. The service-browser gallery has matching
PowerShell/C# states for browsing, confirmation and completed output. Its
fictional services make the demo safe to run on every supported platform.

The source recording is `Website/captures/fullscreen/service-browser.cast`.
The website generator exports its paced GIF and HTML player under
`/recordings/fullscreen/`. I export the GIF with
`dotnet run --project Website/tools/SiteFrames -c Release -- capture-fullscreen-preview Website`
and commit it under `Website/captures/fullscreen/`. Render checks its capture
stamp against the source recording, export policy and GIF bytes, then copies
the reviewed raster asset. This avoids platform rasterization changing the
preview during CI; HTML and player data still regenerate from the recording.
I captured the same app in macOS Terminal and over an
SSH PTY. Physical Mac clicks selected a service, opened confirmation and ran the
action; keyboard navigation, the palette and resize kept the app usable. The
published Mac capture uses scripted keys and SGR reports through the recorder,
at 120×30. The separate SSH capture includes two resizes; it ran from in-memory
assemblies without installing files or changing the remote host.

I refresh acted-on rows through `Show-ConsoleGrid -Refresh` or
`GridOptions.RefreshRow` after successful work. The callback runs on the app's
input thread and preserves row positions and selection. A failed refresh is
logged without skipping the remaining rows. Captured row indices identify the
selection through confirmation, action and refresh, including filtered marks
and rows with shared or null source objects.

Local builds cover net472, net8.0 and net10.0 with zero warnings on master
`c1f761d`, including round 1. The net8.0/net10.0 core suites each pass 2,946 tests
with eight platform skips. The full en-US Pester suite passes 1,449 tests with
19 platform skips; the final four action/refresh integration checks pass again
after the last grid fix. All 112 gallery twin states match. Five preview
contracts verify the staged output, refreshed details, transport events and
capture/export freshness. PowerShell scripts parse without PowerShell 7-only
syntax; native Windows PowerShell 5.1 behavior remains a separate proof gate.

The fresh website render has no asset drift. The full website pipeline remains
held by PowerForge's template output budget: the larger gallery index is
truncated, leaving its last demo unlinked. I need the shared renderer fix and
a passing full pipeline before calling the website validation complete.

I corrected the requested visual findings: output arrives over several seconds,
confirmation holds on Yes, the ending shows the refreshed PID, and the compact
grid has a row-position cue with a readable DependsOn label. The calendar now
names Today, separates upcoming item statuses, marks future backups as pending
and shows an operational cadence across the year. Fresh visual acceptance is
still required before either PR merges.

Before release I still need native conhost and Windows Terminal click, wheel,
drag, window-origin handling and input-mode restoration, including Quick Edit.
Physical Windows mouse recording also needs the relay work described above.
tmux mouse routing and older conhost degradation remain unobserved.

## Phases and size

This is a substantial extension, not a new switch on `Show-ConsoleApp`.
Estimates are focused engineering weeks for one developer, including tests and
docs; they are planning ranges, not delivery promises. The stages below are
implementation checkpoints, not a requirement for a separate PR per stage.

| Phase | Scope | Exit evidence | Rough size |
| --- | --- | --- | --- |
| 1 — this proposal | Inventory, vocabulary, design and decisions | Reviewed design; no product code | Proposal only |
| Standalone calendar — first PR | Month/year Calendar, shared date model, marks/legend/ISO weeks and gallery twins | Date contract tests, captures, twins and website checks | Independent of the app host |
| 2 — host and admin preview | Shared event loop, arrangement/clipping, focus scopes, scroll view, terminal ownership, pointer decoder, bounded dispatcher, public commands/actions and Show-ConsoleGrid -Action | Existing app/menu/grid/explorer tests retained; service browser with in-app confirmation/log, mouse/keyboard, paced GIF/HTML, macOS Terminal/SSH and explicit Windows proof gaps | Re-estimate after the host prototype |
| 3 — first complete controls | Extract DataGrid and TreeSelect; tabs, log viewport, checkbox/radio presentations; month Calendar, DatePicker and Read-ConsoleDate including ranges | Service browser plus operations calendar; keyboard/mouse/date/culture proof; bounded large table/tree/log workloads | 4–6 weeks |
| 4 — integration and first release | Rebuild Show-ConsoleGrid and Show-ConsoleObject on the new controls; migrate menu/form/pager infrastructure; year calendar display; recorder/export qualification; remove superseded loops | Original-object selection and explorer budgets preserved; no duplicate screen/input ownership; complete first-control set and native matrix | 2–4 weeks |
| 5 — follow-on controls | Heatmap calendar, richer menu bar, file picker, splitters and advanced editors as real callers need them | Each has a concrete consumer and bounded proof | Estimate separately |

I would budget **9–15 focused weeks after the proposal** for the first complete
release. The largest uncertainties are extraction without regressions, native
mouse differences and PowerShell work cancellation. Ship a preview after the
host foundation if useful, but do not call it the complete controls release.
Damage-region optimization follows a measured bottleneck; whole-frame
composition with cell diffing is an acceptable initial renderer.

Performance proof should include a 100,000-row grid, a budgeted lazy object
tree and a capped streaming log while navigating/resizing. Record memory, input
latency, frame time and bytes written at normal and narrow sizes. Compare the
stable candidate with today's viewers before choosing release thresholds; do
not promise arbitrary latency or zero allocation from a design document.

## Decisions I need to settle

| Decision | My recommendation |
| --- | --- |
| Extend ConsoleMonster or wrap another toolkit? | Extend the existing core. Keep zero runtime dependencies and one rendering/input stack. The other frameworks remain references. |
| Preserve the app API or introduce an Application DSL? | Keep Show-ConsoleApp, ConsoleApp.Run/RunAsync and ordinary layout builders. Add focused controls/services; avoid a competing app syntax. |
| Retained controls or immediate-mode reconstruction? | Retain controls and state; allow bindings to rebuild display content. Explicitly mount tab/dialog subtrees. This fits PowerShell callback lifetime and existing models. |
| Data-grid/tree naming? | New-ConsoleDataGrid and New-ConsoleTreeSelect. Keep New-ConsoleGrid as layout and New-ConsoleTree as static hierarchy. Retain Select/MultiSelect vocabulary. |
| Checkbox/radio constructors? | Keep Toggle's existing checkbox and add a radio presentation to Select, with no second Boolean/choice state model. Add separate names only if real usage proves the distinction worthwhile. |
| Mouse defaults? | Click/wheel/drag when supported; optional hover and NoMouse. Preserve terminal selection gestures and keyboard equivalence. |
| Calendar date/range contract? | Gregorian, culture-aware labels/week start, date-only DateTime across all targets, inclusive DateRange, status aggregation as specified above. Add non-Gregorian calendars only for a demonstrated need. |
| Year heat strip now? | Month picker and month/year display first. Heatmap follows later, sharing CellMap painting rather than duplicating its general-purpose model. |
| Slow PowerShell actions? | Decided above: phase 2 includes the thin command adapter over isolated runspaces, explicit arguments/modules, streamed output and app-thread completion. |
| What establishes the first release? | The complete first-control set, Grid/explorer rebuilt on it, native PS5.1/7/C# interaction and a verified full-screen recording. A gallery screenshot alone is insufficient. |
| Breaking changes and packaging? | Follow #55's clean vocabulary, migrate real callers and remove superseded implementations once parity is proved. Keep the new controls in the zero-dependency core and cmdlets thin; do not add a second compatibility layer or toolkit package. |
