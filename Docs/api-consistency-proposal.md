# Public API consistency proposal

Status: final decisions for the API consistency pass, incorporating the usability review. The source catalogues record the pre-change surface at the inspected base.

I want one obvious entry point for each job, predictable parameter names, and a clear distinction between creating a view and running it. This is the point to make breaking changes: I am removing the old spellings and duplicate commands rather than keeping aliases or obsolete wrappers.

This inventory is against `ab25431fe4f79fc487643f8c0e1765da056f72f8`. The manifest and source both contain **137 cmdlets**: 52 New, 11 Show, 11 Write, 10 Update, 8 Read, 6 Format, 4 each Invoke/Set/Start, 3 each Add/Export/Out, 2 each Compare/Get/Use/Watch, and 1 each Assert/Clear/Complete/Copy/Disable/Enable/Register/Send/Stop/Wait. The tables below account for every existing command, including retained commands. Additions are listed separately.

I inspected cmdlet declarations and their inherited parameters, the core C# types and factories, Markdown/graphics/hosting/logging packages, the fluent generator, bootstrapper configuration, module manifest, help generation, website pipeline, examples and tests. The source catalogues below retain the original inventory. The decisions are approved for implementation; validation results belong in the pull request.

The source inventory corrects two survey details:

- The generated Core and Desktop accelerator registration uses `$Type.FullName`. It does **not** register generic short names such as `[Table]`, `[Color]` or `[Grid]`. Assembly-wide registration is still unnecessarily broad, and `ConsoleMonster.Layout.Grid` and `ConsoleMonster.Interactive.Grid` illustrate why short names would be ambiguous. The reported roughly 454 entries are not a fixed contract: registration enumerates the loaded assemblies. A fresh import audit must establish the actual count on each loader.
- `Widgets.StatColumn` is internal. Its public configuration object is `Layout.ColumnFormat`; exposing the implementation class would create the wrong API.

## The command model

The everyday route is **Show a view**, **New a reusable definition**, **Read an answer**, **Invoke work and wait**, or **Start a session and receive a handle**. A constructor and a renderer are separate jobs. A renderer that takes ordinary data and a renderer that takes an already-built widget share the same rendering engine.

| Verb | Rule to apply throughout the module |
| --- | --- |
| `New` | Return a definition, widget, item or options object. Do not start input, work, a child process or a live session. |
| `Show` | Render a view or run a user-facing viewer until it closes. No success output by default; explicit capture/selection options describe any returned value. |
| `Write` | Send text, a message or a progress record to its destination. It is not an alternative spelling for showing a table or tree. |
| `Format` | Reserved for PowerShell formatting records. Rename all six current ConsoleMonster display commands to `Show`; introduce no public ConsoleMonster `Format-*` replacements. |
| `Out` | A terminal sink/adapter for PowerShell output or native formatting records. Retain only the two integration adapters; move pipeline chart display to `Show`. |
| `Read` | Ask for input and return the answer. Keep the familiar `Read-Host` model; rendering a prompt is part of acquiring the value. |
| `Invoke` | Execute the supplied work synchronously and return its documented results. |
| `Start` / `Wait` / `Complete` / `Stop` | Start returns an active handle; Wait waits for it, Complete finalizes it, and Stop requests termination. A stop command may also signal its recorded child session explicitly. |
| `Get` / `Export` / `Copy` | Get retrieves objects; Export serializes data to a file or an explicitly selected interchange representation; Copy writes to the clipboard. Get must not export scripts as a side effect. |
| `Set` / `Update` / `Add` / `Send` | Set replaces a value/content; Update brings a running resource or keyed item up to date; Add appends to a collection; Send submits a low-level live event. |
| `Enable` / `Disable` / `Register` / `Unregister` | Change session integration or register/unregister a view. These pairs must be discoverable without an unrelated switch. |
| `Use` / `Watch` / `Compare` / `Assert` / `Clear` | Use scopes an output/progress context around a script; Watch repeatedly observes; Compare computes/displays differences; Assert verifies output; Clear clears the screen. |

These choices follow [PowerShell's approved verb guidance](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/approved-verbs-for-windows-powershell-commands). Native `Format-Table` produces formatting records consumed by outputters; the module's current direct-host formatters are not that pipeline contract. See [PowerShell's formatting/output model](https://devblogs.microsoft.com/powershell/how-powershell-formatting-and-outputting-really-works/).

### Output and pipeline contracts

For noninteractive `Show-*` views, retain `-AsString` for one plain-text string and standardize `-AsWidget` for a composable `IWidget`. These modes render nothing to the host. Retain `-PassThru` only where it returns the original input after display; reject combinations with capture modes. Interactive viewers keep their explicitly documented selection/result contract. Static snapshots print initialized state without reading input or returning an answer.

Remove `-As Text|Markdown|Html` from display commands. Use the existing exporter instead: `$rows | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Markdown`. `-AsString` is plain text; `FencedText` means Markdown containing a layout-preserving text fence. There must be no context-sensitive change from plain text to a document. A direct display retains the existing PowerShell prompt/redirection adapter where needed, but explicit capture must work consistently inside scripts, functions, jobs and PowerShell 5.1/7. Do not replace ordinary objects with formatting records upstream of a display boundary.

Keep `Out-ConsoleDefault` and `Out-ConsoleFormatCommand` as documented advanced integration commands. Their native-format-record inputs, host/capture handling and fallback paths are distinct from the everyday views. `Enable-ConsoleFormatting -Feature FormatCommands` remains an explicit adapter for native `Format-*`; it must preserve native capture and pipeline behavior. This is an integration exception, not a second table API.

## Job-by-job command inventory and decisions

Blast-radius codes: **P** = cmdlet declarations/parameters and generated manifest/bootstrapper; **C** = public C# API and fluent factories; **D** = XML help, generated command reference, maintained guides and website copy/navigation; **E** = module/C#/website examples, gallery scripts and their twins; **T** = xUnit, Pester, golden/capture and site contracts. `Keep` means keep the command name; the parameter vocabulary below still applies. Removed commands disappear from exports and help, with all callers migrated. No compatibility aliases are retained.

### Tables, records, trees and automatic display

Primary commands: `Show-ConsoleTable` for tabular data, `Show-ConsoleList` for records, `Show-ConsoleTree` for hierarchy, `Show-ConsoleObject` for object inspection, and `Show-Console` when the caller wants automatic selection or already has a widget.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Format-ConsoleTable` → `Show-ConsoleTable` | Rename: display data as a table, including grouping, totals, adaptive narrow-window cards and capture. Remove explicit Record/Cards view synonyms; use Show-ConsoleList for deliberate record display. | P,D,E,T |
| `Write-ConsoleTable` → `Show-ConsoleTable` | Remove wrapper; move its useful construction/status options into the shared table construction path. | P,D,E,T |
| `New-ConsoleTable` → same | Keep builder, including `-Bind`; return a table for composition without drawing it. Share construction with the display command. | P,D,E,T |
| `Format-ConsoleList` → `Show-ConsoleList` | Rename: labelled record display, not native format records. | P,D,E,T |
| `New-ConsoleKeyValue` → same | Keep builder for explicit ordered pairs and dashboard values; distinguish it from inspecting a record's properties. | P,D,E,T |
| `Write-ConsoleKeyValue` → `Show-ConsoleList` | Remove the duplicate display wrapper; Show-ConsoleList accepts dictionaries directly, and New-ConsoleKeyValue remains the composition builder. | P,D,E,T |
| `New-ConsoleKeyValueItem` → same | Keep explicit key/value item construction. | D,E,T |
| `Format-ConsoleTree` → `Show-ConsoleTree` | Rename static hierarchy display; remove `-AsInspector` into the object-inspection job. | P,D,E,T |
| `Write-ConsoleTree` → `Show-ConsoleTree` | Merge support for explicit nodes, hierarchical input and hashtables into the one tree display command. | P,D,E,T |
| `New-ConsoleTree` → same | Keep hierarchy construction for layouts. | P,D,E,T |
| `New-ConsoleTreeNode` → same | Keep explicit node construction. | D,E,T |
| `Show-ConsoleObject` → same | Keep keyboard object explorer; add explicit static snapshot/string/widget modes for the old inspector presentation. | P,C,D,E,T |
| `Show-Console` → same | Keep automatic display and add direct widget passthrough to replace Write-ConsoleWidget. Remove explicit `-View`, `-Chart` and `-ChildProperty` routes in favor of named job commands. | P,C,D,E,T |
| `Write-ConsoleWidget` → `Show-Console` | Remove generic display duplicate; render supplied IWidget objects directly. | P,D,E,T |
| `New-ConsoleLiveTable` → same | Keep definition of a keyed, mutable live table. | P,D,E,T |
| `Update-ConsoleTable` → `Update-ConsoleLiveTable` | Rename so the update command names the actual resource and pairs with its builder. | P,D,E,T |
| `Show-ConsoleOverview` → same | Keep local-host overview and structured facts mode; apply the shared width policy. | P,C,D,E,T |
| `Add-ConsoleItem` → same | Keep append-to-an-existing-widget job; it must not create a hidden session. | P,D,E,T |

`New-ConsoleTable` and `Show-ConsoleTable -AsWidget` have different input intent: author a reusable table, or obtain the computed presentation of pipeline data. Both must call the same builder. `Show-Console` is the automatic dispatcher, not a second parameter-rich implementation of every named view. The existing `Write-ConsoleTable` actually derives from the builder; the formatter is a separate path today. Consolidation must preserve both paths' useful options rather than just deleting the wrapper.

### Text, code, errors, Markdown and help

Primary commands: `Write-ConsoleText` for messages; named `Show-*` commands for structured content.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Write-ConsoleText` → same | Keep rich text emission and its explicit log-file destination; normalize its parameters. | P,C,D,E,T |
| `Write-ConsoleMarkup` → `Write-ConsoleText -Markup` | Remove duplicate text writer; preserve markup and newline behavior as parameters. | P,D,E,T |
| `New-ConsoleText` → same | Keep RichText construction for cells, labels and messages; it does not draw. | P,C,D,E,T |
| `Format-ConsoleCode` → `Show-ConsoleCode` | Rename syntax-highlighted source display. | P,D,E,T |
| `Format-ConsoleJson` → `Show-ConsoleJson` | Rename structured JSON display; share code rendering but retain useful object serialization/depth semantics. | P,D,E,T |
| `Format-ConsoleError` → `Show-ConsoleError` | Rename one display command accepting ErrorRecord, Exception and message input. | P,D,E,T |
| `Write-ConsoleException` → `Show-ConsoleError` | Remove duplicate over the same ConsoleErrorCmdlet base. This displays errors; it does not emit new PowerShell error records. | P,D,E,T |
| `Show-ConsoleMarkdown` → same | Keep document reading/paging through the Markdown add-on. | P,D,E,T |
| `Show-ConsoleHelp` → same | Keep command help viewer; `-Name` identifies a command, not a widget. | P,D,E,T |
| `New-ConsoleRule` → same | Keep rule definition for composition; Show-ConsoleRule displays a separator directly. | D,E,T |
| `Write-ConsoleRule` → `Show-ConsoleRule` | Rename the convenient one-line separator; keep its construction in New-ConsoleRule. | P,D,E,T |
| `New-ConsoleLogo` → same | Keep banner construction, with explicit gradient color names. | P,C,D,E,T |
| `Clear-ConsoleScreen` → same | Keep explicit screen operation. | D,T |

### Progress, status and live collections

Primary routes: `Use-ConsoleProgress { ... Write-Progress ... }` for existing scripts, `New-ConsoleProgress` plus `Update-ConsoleProgress` inside a dashboard, and `Show-ConsoleStatusLine` for one in-place status line. These are different lifecycles, not competing progress constructors.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Write-ConsoleProgress` → same | Keep advanced native Write-Progress adapter, with its native parameter vocabulary. | P,D,E,T |
| `Use-ConsoleProgress` → same | Keep scoped interception of a script's native progress, returning the script's ordinary output. | P,D,E,T |
| `New-ConsoleProgress` → same | Keep passive progress-bar definition. | P,C,D,E,T |
| `Update-ConsoleProgress` → same | Keep live numeric update to that bar. | P,D,E,T |
| `New-ConsoleSpinner` → same | Keep unknown-total progress definition. | P,D,E,T |
| `Update-ConsoleSpinner` → same | Keep live spinner update. | P,D,E,T |
| `Write-ConsoleStatus` → `Show-ConsoleStatusLine` | Rename in-place single line to match C# StatusLine and distinguish checklist items. | P,C,D,E,T |
| `New-ConsoleStatusList` → same | Keep checklist definition. | P,D,E,T |
| `New-ConsoleStatusItem` → same | Keep keyed checklist-item definition. | P,D,E,T |
| `Update-ConsoleStatus` → `Update-ConsoleStatusItem` | Rename keyed checklist update; never confuse it with a status line. | P,D,E,T |
| `New-ConsolePhases` → same | Keep phase-strip definition. | P,D,E,T |
| `Update-ConsolePhase` → same | Keep phase-item update. | P,D,E,T |
| `New-ConsoleLanes` → same | Keep workload-lane collection definition. | P,D,E,T |
| `New-ConsoleLane` → same | Keep one keyed lane definition. | P,D,E,T |
| `Update-ConsoleLane` → same | Keep keyed lane update. | P,D,E,T |
| `New-ConsoleActivityList` → same | Keep list of active native-style progress activities. | P,D,E,T |
| `Update-ConsoleActivity` → same | Keep native activity-id update; align percentage, description and completion vocabulary. | P,C,D,E,T |
| `New-ConsoleCellMap` → same | Keep compact outcome-grid definition. | P,D,E,T |
| `Update-ConsoleCell` → same | Keep addressed cell update/resize/append operations; document Index versus item Key. | P,D,E,T |
| `New-ConsoleSummaryBar` → same | Keep progress/counters definition. | P,D,E,T |
| `Update-ConsoleSummaryBar` → same | Keep live progress/counter update. | P,D,E,T |

The only command-name alias I would retain is the **opt-in session alias `Write-Progress` → `Write-ConsoleProgress`** installed by `Enable-ConsoleFormatting -Feature Progress`. Existing scripts use the native name and its exact binding contract; this adapter deliberately intercepts them. It must restore the previous alias on disable. It is excluded from `All`. There is no permanent exported Write-Progress alias and no alias for a renamed ConsoleMonster command.

### Charts

Primary command: `Show-ConsoleChart` for charting pipeline objects. Builders remain for composing explicit charts into layouts; chart kind is a parameter where the existing model already shares it.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Out-ConsoleChart` → `Show-ConsoleChart` | Rename pipeline chart display; use `-Type` to select Bar, Histogram or Sparkline as today. | P,D,E,T |
| `New-ConsoleBarChart` → same | Keep explicit bar/stacked-bar builder; keep `-Stacked` rather than inventing a second constructor. | P,C,D,E,T |
| `New-ConsoleBarItem` → same | Keep labelled bar/segment value. | P,C,D,E,T |
| `New-ConsoleLineChart` → same | Keep explicit line/scatter builder; retain `-Scatter` on the existing shared model. | P,C,D,E,T |
| `New-ConsoleChartSeries` → same | Keep keyed series definition; separate its key and visible label. | P,C,D,E,T |
| `New-ConsoleSparkline` → same | Keep explicit trend-widget builder. | P,C,D,E,T |
| `New-ConsoleHistogram` → same | Keep explicit bucket/edge builder; distinct from deriving bins from pipeline data. | P,C,D,E,T |
| `Add-ConsoleValue` → same | Keep append of a live sample/value; address a widget and optional item key. | P,C,D,E,T |
| `Set-ConsoleValue` → same | Keep replacement of a keyed live value. | P,C,D,E,T |

### Layout, dashboards and work execution

Primary routes: `Invoke-ConsoleRun` executes work; `Start-ConsoleRun` observes work scheduled elsewhere; `Invoke-ConsoleDashboard` owns a supplied script's dashboard lifetime.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `New-ConsoleRows` → same | Keep vertical-layout definition. | D,E,T |
| `New-ConsoleColumns` → same | Keep horizontal-layout definition. | D,E,T |
| `New-ConsoleGrid` → same | Keep equal-width layout grid; explicitly distinguish it from interactive Show-ConsoleGrid. | D,E,T |
| `New-ConsoleStack` → same | Keep layered-layout definition. | D,E,T |
| `New-ConsolePanel` → same | Keep framed-content definition; align caption/header-right vocabulary. | P,C,D,E,T |
| `New-ConsolePadding` → same | Keep padding definition. | D,E,T |
| `New-ConsoleAlign` → same | Keep alignment definition. | D,E,T |
| `New-ConsoleDashboard` → same | Keep passive dashboard composition and metadata. | P,C,D,E,T |
| `Start-ConsoleDashboard` → `Invoke-ConsoleDashboard` | Rename: this executes supplied work and blocks until its session/work cleanup completes; it does not hand back an independent running handle. | P,D,E,T |
| `New-ConsoleRegion` → same | Keep replaceable region definition; align C# Slot names with region terminology. | P,C,D,E,T |
| `Set-ConsoleRegion` → same | Keep replacement of a live region. | P,D,E,T |
| `New-ConsoleClock` → `New-ConsoleSimulatedClock` | Rename a controllable simulated clock, not a wall-clock widget. | P,D,E,T |
| `Invoke-ConsoleRun` → same | Keep synchronous per-item execution and ordinary script output. | P,C,D,E,T |
| `New-ConsoleRun` → `Start-ConsoleRun` | Rename: construction currently starts a LiveRun/session and returns its handle. | P,C,D,E,T |
| `Update-ConsoleRun` → same | Keep item progress/outcome reporting through the returned Run handle. | P,C,D,E,T |
| `Wait-ConsoleRun` → same | Keep waiting/pumping for an observed run. | D,E,T |
| `Complete-ConsoleRun` → same | Keep finalization/summary; preserve finally/disposal examples. | D,E,T |
| `Invoke-ConsolePester` → same | Keep Pester execution and its test-result contract; normalize live-view option. | P,D,E,T |
| `Watch-Console` → same | Keep periodic script observation; the polling interval is not a frame rate. | P,C,D,E,T |
| `Send-ConsoleEvent` → same | Keep advanced event submission below the typed update commands. | P,C,D,E,T |
| `Use-ConsoleTarget` → same | Keep scoped output redirection; a target object is not a widget identifier. | P,C,D,E,T |
| `Get-ConsoleCapability` → same | Keep typed terminal-profile retrieval, with explicit refresh. | D,T |

### Log destinations

Primary routes: `Write-ConsoleLogEntry` sends a message into a running log region; `Watch-ConsoleLog` reads a file/event log.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `New-ConsoleLog` → `New-ConsoleLogRegion` | Rename builder to its C# resource and separate it from opening/following log files. | P,D,E,T |
| `New-ConsoleLogEntry` → same | Keep passive message/level/time item construction. | P,D,E,T |
| `Write-ConsoleLog` → `Write-ConsoleLogEntry` | Rename destination-oriented send, paired with LogEntry and addressed by WidgetId. | P,D,E,T |
| `Watch-ConsoleLog` → same | Keep file/Windows-event-log following; no dashboard-region construction implied. | P,C,D,E,T |

### Questions, forms, menus and embedded application controls

Primary routes: `Read-Console*` asks one question; `Read-ConsoleForm` collects a validated form/wizard; `Show-ConsoleMenu` runs an action menu; `Show-ConsoleApp` runs a persistent, composed application. Keep these models: a field describes validation, review, conditional pages and saved answers; an input control owns focus, events and mutable app state. Folding them together would lose behavior and require a new framework.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Read-ConsoleText` → same | Keep single/multiline question; multiline remains an option. | P,C,D,E,T |
| `Read-ConsoleNumber` → same | Keep numeric question. | P,C,D,E,T |
| `Read-ConsolePath` → same | Keep file/folder question. | P,D,E,T |
| `Read-ConsoleSecret` → same | Keep SecureString-by-default PowerShell contract. | P,D,E,T |
| `Read-ConsoleConfirm` → same | Keep ordinary/destructive confirmation question. | P,D,E,T |
| `Read-ConsoleSelection` → `Read-ConsoleSelect` | Rename to the same Select vocabulary as Prompt.Select and the application control. | P,D,E,T |
| `Read-ConsoleMultiSelection` → `Read-ConsoleMultiSelect` | Rename to the same MultiSelect vocabulary throughout. | P,D,E,T |
| `New-ConsoleField` → same | Keep typed field definition; use Key for stored answers, Label for displayed text. | P,C,D,E,T |
| `New-ConsoleForm` → same | Keep form/wizard definition. | P,D,E,T |
| `Read-ConsoleForm` → same | Keep answer acquisition; preserve form-specific review/preset behavior. | P,D,E,T |
| `Set-ConsoleFormValue` → same | Keep applying field-keyed values; change collection parameter to Values. | P,D,E,T |
| `New-ConsoleFormPreset` → same | Keep named answer preset definition. | D,E,T |
| `New-ConsoleMenu` → same | Keep action-menu definition. | D,E,T |
| `New-ConsoleMenuItem` → same | Keep action/submenu/form/selection/page item definition. | P,C,D,E,T |
| `Show-ConsoleMenu` → same | Keep action loop; it does more than collect one selection. | P,D,E,T |
| `New-ConsoleMenuPreset` → same | Keep named menu preset definition. | D,E,T |
| `New-ConsoleRunResult` → `New-ConsoleMenuRunResult` | Rename menu action's summary/result card; distinguish it from per-item execution results. | P,C,D,E,T |
| `New-ConsoleTextInput` → same | Keep persistent single-line application input. | P,C,D,E,T |
| `New-ConsoleTextArea` → same | Keep persistent multiline application input. | P,C,D,E,T |
| `New-ConsoleSelect` → same | Keep persistent single-choice application input. | P,C,D,E,T |
| `New-ConsoleMultiSelect` → same | Keep persistent multiple-choice application input. | P,C,D,E,T |
| `New-ConsoleToggle` → same | Keep persistent Boolean application input. | P,C,D,E,T |
| `New-ConsoleButton` → same | Keep application action control. | P,C,D,E,T |
| `New-ConsoleBinding` → same | Keep dependent display builder; its optional address is WidgetId, not a state Key. | P,C,D,E,T |
| `Show-ConsoleApp` → same | Keep application input/event lifetime and final state. | P,C,D,E,T |
| `Show-ConsoleGrid` → same | Keep interactive selection/filter/sort job, rather than adding selection to Show-ConsoleTable. | P,D,E,T |

Keep `Select`/`MultiSelect` in the C# facade, prompt classes, form builder and controls. No C# rename is needed to justify the PowerShell rename. `Read-ConsoleSecret` returning SecureString by default and C# SecretPrompt returning a string is a deliberate host-boundary difference; document it rather than silently changing secret handling.

### Differences and change confirmation

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Compare-ConsoleObject` → same | Keep computing/displaying object differences, with explicit PassThru for the ChangeSet. | P,C,D,E,T |
| `Compare-ConsoleText` → same | Keep text/file diff and explicit unified-text output. | P,C,D,E,T |
| `Show-ConsoleChanges` → same | Keep a proposed-change preview and confirmation; it is not a second pure comparison command. | P,C,D,E,T |

### Recording, replay and export

Primary routes: `Invoke-ConsoleRecording` records a script or an interactive child shell; `Show-ConsoleReplay` plays a dashboard recording; `Export-ConsoleRecording` converts an existing recording. Recording raw terminal bytes and recording named dashboard events remain explicit parameter sets/formats.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Invoke-ConsoleRecording` → same | Keep synchronous script recording; add an Interactive parameter set for the child-shell job. | P,D,E,T |
| `Start-ConsoleRecording` → `Invoke-ConsoleRecording -Interactive` | Merge: the current command waits for the child rather than returning an asynchronous recorder handle. | P,D,E,T |
| `Stop-ConsoleRecording` → same | Keep signalling recording stop from inside that child. Help must state its scope and that it is not a handle-based parent stop. | D,E,T |
| `Add-ConsoleRecordingMarker` → same | Keep adding a named chapter in the active recording context. | D,E,T |
| `Export-ConsoleRecording` → same | Keep conversion of either a session cast or dashboard JSONL, with explicit source/destination names. | P,C,D,E,T |
| `Start-ConsoleReplay` → `Show-ConsoleReplay -PlaybackMode Automatic` | Merge synchronous, uninteractive dashboard playback into the player command; keep the existing automatic LiveSession replay engine. | P,C,D,E,T |
| `Show-ConsoleReplay` → same | Keep Interactive playback default, add Automatic mode without keyboard controls. Dashboard JSONL still requires its matching widget definition. | P,C,D,E,T |
| `Export-ConsoleFrame` → same | Accept ordinary objects through the automatic view path, plus widgets/frames/snapshots. `$rows | Export-ConsoleFrame -Format Markdown` replaces display-command document export switches. | P,C,D,E,T |
| `Copy-ConsoleOutput` → same | Keep clipboard destination; standardize Format with the exporters. | P,D,E,T |
| `Assert-ConsoleOutput` → same | Keep output assertion; deterministic test calls specify dimensions explicitly. | P,D,E,T |

Do not imply that Show-ConsoleReplay currently plays a whole-session `.cast`: the existing interactive player consumes dashboard events. Whole-session player output already exists through `Export-ConsoleRecording -Format Html`. Adding an in-terminal whole-session player is a separate capability, not required for this naming cleanup.

### Gallery demos and scripted presentations

Primary routes: `Get-ConsoleDemo` retrieves catalogue entries, `Show-ConsoleDemo` runs one embedded demo, `Show-ConsolePresentation` presents a script, and `Invoke-ConsolePresentation` types/runs/records one unattended.

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Get-ConsoleDemo` → same | Keep gallery metadata retrieval; move its -Path file-export behavior to Export-ConsoleDemo. | P,D,E,T |
| `Show-ConsoleDemo` → same | Keep user-facing execution of embedded gallery code in the current runspace. | P,D,E,T |
| `Invoke-ConsoleDemo` → `Invoke-ConsolePresentation` | Rename unattended typing/recording of an external script in a child terminal. | P,D,E,T |
| `Start-ConsoleDemo` → `Show-ConsolePresentation` | Rename the blocking, keyboard-driven presenter; it does not return a background handle. | P,D,E,T |

Gallery `-Slug` remains a slug, while a demo script uses `-Path`. Never pass a slug where a script file is expected. Add-ons, input/runtime requirements and static snapshots remain gallery metadata. This distinction matters because an embedded gallery command runs code in the current runspace while the presenter/recorder owns a child shell.

### Session formatting, view registration and themes

| Before → after | Decision and reason | Blast radius |
| --- | --- | --- |
| `Enable-ConsoleFormatting` → same | Keep explicit feature installation, including shell Prompt. Preserve native engine boundaries. | P,D,E,T |
| `Disable-ConsoleFormatting` → same | Keep exact session restoration. | P,D,E,T |
| `Register-ConsoleView` → same | Keep type-to-view registration; remove -Remove into the matching unregister command. | P,D,E,T |
| `Out-ConsoleDefault` → same | Keep advanced default-output sink; do not advertise it as the primary table command. | P,D,E,T |
| `Out-ConsoleFormatCommand` → same | Keep native-format-record adapter for explicit integration and capture. | P,D,E,T |
| `Set-ConsoleTheme` → same | Keep selecting/importing the active theme, with Theme for a built-in choice and Path for a file. | P,D,E,T |
| `Export-ConsoleTheme` → same | Keep serializing the active theme to a team/user file. | D,E,T |

Remove exported aliases `fct`, `fcl` and `ForEach-ConsoleItem`, plus redundant parameter aliases, from the source metadata and regenerated manifest. `ForEach` is a reserved PowerShell naming form, and `Invoke-ConsoleRun` already describes the supported execution command. Standard parameter abbreviations still work through PowerShell itself; they are not extra exported aliases. Keep proxy parameters that must match native PowerShell exactly inside the integration adapters.

## Shared parameter and C# vocabulary

The rule is one name per **meaning**, not one overloaded word for different things. The following rows apply to every command using that meaning, including inherited parameters. C# constructor argument names, properties, fluent methods, builders and XML examples must move together. A retained protocol/schema field is called out explicitly; it is not a public alias.

### Addresses, keys, labels and metadata

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| Live builder/updater `-Name`; Send-ConsoleEvent `-Target` → `-WidgetId` | One identifier for addressing a live widget. Applies to charts, dashboard/regions, progress/spinners, lanes/phases/status/activity/cell/summary/live-table/log widgets, binding and typed updates. | P,C,D,E,T |
| `LiveWidget.Name`, `LiveHandle` address arguments, `LiveEvent.Target`, matching widget constructors → `WidgetId` / `widgetId` | Same address concept in C#; update handles, dispatchers, registration, builders and generated fluent code. | C,P,D,E,T |
| Invoke/Start run item selector `-Name` → `-Label` | Human-readable item text, supplied by property or script block; not an address. C# RunItem.Name → Label; RunOptions.Label already has the right name. | P,C,D,E,T |
| Start run `-Target` / `-Group` → same | These select workload target metadata and grouping, not a widget address; retain C# RunItem.Target/Group. | D,E,T |
| Field and application input `-Name` → `-Key` | Answer/app-state dictionary key; C# Field.Name and InputControl.Name and their constructor arguments become Key/key. | P,C,D,E,T |
| ChartSeries `-Name`, C# ChartSeries.Name → `-Key`, `Key` | Series identity used by live updates. Add explicit Label; C# NameText → LabelText, default label to key. | P,C,D,E,T |
| Bar/segment label used as identity → explicit optional Key plus Label | Keep Label visible; default Key to Label, and add a distinct key only where live lookup currently relies on the label. Apply to ChartItem and bar creation/update. | P,C,D,E,T |
| `Set-ConsoleTheme -Name` → `-Theme` | A selected theme, matching frame/recording export and application theme vocabulary. Keep ThemeFile.Name for the file's own name. | P,D,E,T |
| Recording `-Name`, SessionRecordingOptions.Name → `-Title`, `Title` | This is the title shown in the terminal/cast, not a live identifier. | P,C,D,E,T |
| Form/menu preset `-Name`; Show-ConsoleHelp `-Name`; TypeName/LogName → same | These genuinely name a preset, command, type or event-log resource. Do not rename every Name mechanically. | D,E,T |
| `Set-ConsoleFormValue -Value` → `-Values` | A field-keyed dictionary, matching presets and FormValues. A scalar widget Value stays singular. | P,D,E,T |
| MenuItem/Choice Title and New-ConsoleMenuItem `-Title` → Label / `-Label` | Item text is a label; reserve Title for the menu, form, page or other view heading. Update selection callbacks and menu examples together. | P,C,D,E,T |
| Prompt `ParameterName`, field `Parameter` → `ParameterName` in both | Name of the caller's noninteractive script parameter; remove the redundant Parameter alias. | P,C,D,E,T |
| `Show-ConsoleChanges -Before/-After` → `-ReferenceObject/-DifferenceObject` | Same object-diff inputs as Compare-ConsoleObject; keep ChangePreviewOptions as presentation/confirmation settings rather than adding duplicate object inputs to it. | P,C,D,E,T |
| Object explorer `-Output` → `-SelectionOutput` | Select Value versus Path; reserve OutputMode for genuine output/selection modes, not arbitrary output strings. | P,D,E,T |

Preserve the current JSONL wire keys such as `target` and `state` in the existing recording reader/writer unless a separately versioned format change is needed. Public C# WidgetId/Status can map to those protocol fields. The mapping belongs in the existing serializer, not a second old/new public object model. Update any serialized model that actually derives keys from property names deliberately, with round-trip proof. Do not silently make recordings unreadable as a side effect of a source rename.

### Color and style

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| `-Color` → same; C# Style/RichStyle Foreground → Color, constructor foreground → color | Literal foreground color; same named idea on both surfaces. Keep low-level terminal APIs' native field names where they mirror a platform contract. | P,C,D,E,T |
| `-BackGroundColor` → `-BackgroundColor`; Style/RichStyle Background → BackgroundColor | One American spelling/casing and one background-color name. PowerShell binding is case-insensitive, so the cmdlet change is metadata/help casing rather than a new runtime alias. | P,C,D,E,T |
| Text `-Style` → `-Decoration` | It currently accepts decorations such as Bold/Italic, not a complete Style object. Match C# TextDecoration/Style.Decoration. | P,D,E,T |
| Chart `-Tone` → `-Role`; C# Token/BarToken/Tone → Role | One semantic theme-role name, distinct from a literal Color. Sweep ChartItem, ChartSeries, BarChart, Histogram, Sparkline, TextBlock, PresentedValue, OverviewItem, FooterSegment and RecordView heading arguments. | P,C,D,E,T |
| C# `ToneRule`, `TypeTone`, `LineToken` → `RoleRule`, `TypeRole`, `LineRole` | Apply the semantic role name to rules and qualified role properties too. Keep ThemeToken as the existing enum identifying a theme token. | C,P,D,E,T |
| Style/RichStyle WithForeground/WithBackground → WithColor/WithBackgroundColor | Follow renamed properties; update fluent and object-initializer examples together. | C,D,E,T |
| Logo `-From/-To` → `-StartColor/-EndColor`; matching Logo properties/arguments | These are gradient endpoints, not pipeline source/destination. | P,C,D,E,T |
| Use-ConsoleTarget `-Colors` → `-ColorDepth` | Terminal color capability (None/Ansi16/Ansi256/TrueColor), not a foreground palette; match ExportOptions.ColorDepth. | P,D,E,T |
| Export graphics `-Background`, `-BackgroundColor`, `-BackgroundEndColor` → same | Background is a rendering mode; its color endpoints are separate values. Keep C# GraphicsExportOptions parity. | D,E,T |

For rich text, retain explicit Role plus optional literal Color/BackgroundColor and Decoration. State one precedence rule: start with the Role's theme style, apply supplied literal colors, then add decorations. Reject a single contradictory input mode rather than letting command-specific binding order decide. Charts continue to use semantic roles and thresholds; this proposal does not add arbitrary-color support to every chart.

### Titles, captions and descriptions

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| `Title`, `Caption`, `Subtitle` → retain as distinct concepts | Title heads the view; Subtitle qualifies the title immediately below it; Caption explains the content below it. They are not interchangeable aliases. | P,C,D,E,T |
| New-ConsolePanel `-Caption/-Footer` → `-HeaderRight/-Caption`; C# Panel.Caption/Footer → HeaderRight/Caption | Current Caption is the top-right border text and Footer is bottom-right text; keep both positions while reserving Caption for below-content explanation. | P,C,D,E,T |
| Dashboard `-Footer` → same; add `-FooterHints` for the existing typed hint list | Footer is custom footer text; FooterHints is a separate keyboard-hint list used when Footer is unset. Do not merge them. | P,D,E,T |
| Read prompt `-Title` → same; remove Prompt/Question aliases | Keep Prompt.Title and form/menu Title aligned; an ordinary question is the prompt's title. | P,D,E,T |
| HeaderBar and Dashboard Status/StatusLevel → Detail/Status | Detail is visible status text; Status is the semantic outcome used for styling. This also keeps the proposed header builder consistent. | C,P,D,E,T |
| Description versus Label/Detail → retain semantic separation | Description explains a whole prompt/form/menu; Label names an item; Detail describes its current state. | D,E,T |

### Time, cadence, state and switches

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| Status `-ClockFormat`, text `-DateTimeFormat`, C# ConsoleTextLayout.TimeFormat/StatusLine clockFormat → `TimestampFormat` / `timestampFormat` | Format a wall-clock timestamp; keep HH:mm:ss for a clock-only default and a full date/time default for text logs. Same name does not require identical presentation defaults. | P,C,D,E,T |
| DatePresenter.Format / format constructor argument → TimestampFormat / timestampFormat | Name the timestamp-format option specifically; keep native calculated-property FormatString (which can also format numbers) distinct. | P,C,D,E,T |
| LogTailOptions/LogTailView/LogRegion TimestampFormat → same | Already names the wall-clock timestamp correctly; share it across the log constructor and tail adapter. | P,C,D,E,T |
| PowerShell Elapsed/Duration/At/Started/Finished → ElapsedSeconds/DurationSeconds/AtSeconds/StartedSeconds/FinishedSeconds; C# typed names stay | Numeric PowerShell values carry units and accept fractions. C# keeps TimeSpan; elapsed time, measured duration and source position remain distinct. | P,C,D,E,T |
| `-NoLiveView` → `-NoLive`; C# LiveRunOptions.NoLive / WatchOptions.Interactive / RunOptions.Interactive → Live | Keep a default-false opt-out switch; map it to the positive C# Live property. Default remains automatic host-safe rendering. | P,C,D,E,T |
| `-NoElapsed` → retain on default-on widgets; New-ConsoleLog -NoTime → -NoElapsed; C# LogRegion.ShowTime → ShowElapsed | A switch names the non-default behavior. Default-off status lines use ShowElapsed; default-on builders use NoElapsed. | P,C,D,E,T |
| Text `-ShowTime`, `-LogTime` → `-ShowTimestamp`, `-LogTimestamp`; `-ShowClock` → `-ShowTimestamp` | ShowTimestamp opts into wall-clock timestamps; LogTimestamp remains a Boolean destination setting, enabled by default. | P,C,D,E,T |
| Watch `-Interval`, log `-Interval`/C# PollInterval → `-PollIntervalSeconds`, `PollInterval` | Time between observations. Use numeric seconds in PowerShell and TimeSpan in C#; numeric PowerShell inputs must never bind as ticks. | P,C,D,E,T |
| HeaderBar.ShowClock / Dashboard.ShowClock and HeaderBar.Clock → ShowElapsed / Elapsed | These clocks hold a TimeSpan since start, unlike the wall-clock Show-ConsoleStatusLine timestamp. | C,P,D,E,T |
| `-FramesPerSecond` → same | Renderer/recording sampling rate, never a polling delay. Preserve int redraw versus double export sampling where required. | D,E,T |
| Recording/run `-TimeoutSeconds`, demo `-StepTimeoutSeconds`, overview `-BudgetMilliseconds` → `-TimeoutSeconds`, `-StepTimeoutSeconds`, `-BudgetMilliseconds` | Keep numeric units in PowerShell, with double where fractional values make sense; C# keeps typed TimeSpan properties. | P,C,D,E,T |
| Recording/demo `-IdleLimit` → `-IdleLimitSeconds` | Numeric seconds in PowerShell map to TimeSpan IdleLimit in C#. | P,C,D,E,T |
| Run `-ProgressSeconds` → `-ProgressIntervalSeconds` | Numeric seconds mapped to TimeSpan, matching LiveRunOptions.ProgressInterval. Keep ProgressEvery as the separate completion-count trigger. | P,D,E,T |
| Update-ConsoleActivity `-Percent` → `-PercentComplete` | Match native Write-Progress. Keep raw Value/Total distinct from a percentage. | P,C,D,E,T |
| Update-ConsoleActivity free-text `-Status` → `-Detail`; run `-State` → `-Status` | Reserve Status for semantic outcomes; Detail is descriptive text. Align RunItemSnapshot.State and matching run-update C# arguments to Status. | P,C,D,E,T |
| Menu item `-State`, MenuItem.StateSource → `-Detail`, DetailSource | This is dynamic descriptive text such as “24 selected”, not app state or a semantic status value. | P,C,D,E,T |
| Send-ConsoleEvent `-State` / LiveEvent.State → `-Status` / `Status` | Low-level event outcome uses the same word, while wire `state` can stay fixed. | P,C,D,E,T |
| Update-ConsoleActivity `-Complete` → `-Completed` | Native progress completion flag spelling; Show-ConsoleStatusLine -Complete remains an explicit finalization operation, not a progress-record field. | P,D,E,T |
| Existing default-on opt-outs NoHeaderSeparator, NoLegend, NoLatest, NoStack, NoTitle, NoExamples, NoPager, NoReview, NoRemember, NoCommandPalette, NoMouse, NoBuiltIn, NoSort, NoConsoleOutput → retain | A switch selects non-default behavior. Keep NoNewLine idiomatic too; never declare a default-true switch. C# uses positive bool properties. | P,C,D,E,T |
| Read-ConsoleNumber/field `-Integer`, C# WholeNumber → `Integer` | One Boolean option for integral numeric input. Prompt.Integer remains the typed convenience factory. | P,C,D,E,T |
| Read text `-Prefill`, C# PrefillDefault → `PrefillDefault` | State that the default value is copied into the editor. | P,C,D,E,T |
| Prompt/field `-Validate`, C# Validator → `Validator` | One name for a validation callback across public prompt/field surfaces. | P,C,D,E,T |

Keep `-NoNewLine` on Write-ConsoleText: this follows the mainstream Write-Host spelling. It follows the same non-default-behavior switch rule, not a migration alias. Remove Write-Color compatibility claims from help where renamed parameters intentionally break that older module's signature.

Native progress adapter parameters `Activity`, `Status`, `Id`, `ParentId`, `PercentComplete`, `SecondsRemaining`, `CurrentOperation`, `Completed` and `SourceId` remain exactly native. Its Status is native descriptive text: this is the explicit mainstream PowerShell exception. The adapter translates to canonical Label/Detail/Status internally; it must not force existing Write-Progress scripts to use ConsoleMonster's semantic outcome enum. Apply the same exact-binding rule to native Write-Host/Format-* proxies, without turning their spellings into public vocabulary aliases.

### Width and sizing

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| Implicit terminal/80/100/capped-120 `-Width` → default `0` = resolve available width; fallback **100 cells** when unknown | One rule for display, text, target capture, exports, assertions and child recordings. Resolve a known current host/target width first; zero is a request, never an allocated zero-cell buffer. | P,C,D,E,T |
| C# ExportOptions.Width / RecordingExportOptions.Width / SessionRecordingOptions.Width defaults → `0`, resolved at rendering/session boundary | Match PowerShell options. Preserve explicit positive widths and existing dimension/allocation limits; validate the resolved dimensions. | C,P,D,E,T |
| Overview's internal 120-cell cap → ordinary available-width constraints | Remove hidden width clamping; responsive layout may choose columns without ignoring explicit Width. | C,D,E,T |
| Recorded frames/casts/snapshots with dimensions → retain those source dimensions | Existing pictures have geometry; Width only lays out widgets or initializes a child terminal. Do not reflow a recording accidentally. | P,C,D,E,T |
| Golden tests, gallery metadata and fixed captures → explicit dimensions | Avoid inheriting the review machine's terminal; migrate old 80/100 choices explicitly where they were intentional. | D,E,T |
| `BarWidth` → same | Width of a bar component in cells, not overall output width. | D,E,T |
| `Weight` → same | Proportional share of remaining layout space, not cells. Keep FixedWidth/MinWidth/MaxWidth and height/size constraints distinct. | D,E,T |

Resolve automatic width once per print operation or child-session initialization, and again on supported live resize. Honor the explicit target/profile and existing environment overrides. Low-level `Rect`, `CellBuffer`, `Size`, `Constraints`, `Terminal.Render(..., profile)` and pixel dimensions continue to require actual dimensions; this proposal does not give geometry types ambient terminal lookups. API-specific safety bounds still apply. Make explicitly requested unsupported sizes fail clearly rather than silently cap them at a presentation width.

### Files, hierarchy and output representation

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| New tree `-ChildrenProperty`, Show-Console `-ChildProperty`, C# ConsoleViewOptions.ChildProperty → `ChildrenProperty` at hierarchy entry points | One name for the member containing child objects; Show-Console no longer exposes the explicit hierarchy selector. | P,C,D,E,T |
| Tree `-NameProperty` → `-LabelProperty`; matching hierarchy-adapter label selection → LabelProperty | It selects visible node text, not an identifier. | P,C,D,E,T |
| Display `-As` → exporter `-Format`; Copy-ConsoleOutput `-As` → `-Format` | One serialization-format selector on destination commands; display capture uses AsString/AsWidget only. | P,D,E,T |
| Recording exporter `-Path/-Destination` → `-Path/-Destination` | Source path and destination file are genuinely different. Retain frame export Path because it has no source-file parameter. Remove redundant OutFile alias. | P,D,E,T |
| Invoke recording/scripted demo output `-Path/-Destination/-Record` → `-RecordingPath/-Destination/-RecordingPath` as applicable | RecordingPath saves the raw cast; Destination saves a derived export. A demo script's Path remains the source .ps1. Map recorder options Path explicitly rather than overloading script input. | P,C,D,E,T |
| Show-ConsoleOverview `-AsObject` → same | Explicit structured host facts, not rendered text or a display widget; retain the distinct meaning. | D,E,T |

### Construction and callback conventions

| Before → after | Reason and exact meaning | Blast radius |
| --- | --- | --- |
| Builder `-Data` plus pipeline `-InputObject` → `-InputObject` where both supply ordinary source data | Merge duplicate binding routes on bar/line/key-value/tree builders; preserve dictionary and pipeline collection behavior. Content remains the script block that emits explicit children/items. | P,D,E,T |
| Builder `-Content`, `-Widget`, typed `-Item/-Field/-Series/-Lane` → retain distinct forms | A construction callback, an existing child widget and typed item collections are different inputs. Use clear parameter sets and reject contradictory combinations. | P,D,E,T |
| Prompt `Default` versus app input `Value` → retain | A noninteractive/default answer and a persistent control's current value are different states. Field defaults and applied form Values remain distinct too. | D,E,T |
| Calculated-property Name/Label/Expression/FormatString → preserve native PowerShell keys | These mirror Format-Table/Select-Object conventions. They are explicit mainstream binding exceptions, not aliases for old ConsoleMonster names. | P,D,E,T |
| InputControl.Enabled versus app `-Disabled` → retain the idiomatic host mapping | Disabled is the non-default switch; map it to Enabled = false in C#. Keep a disabled-reason callback separate. | P,C,D,E,T |
| Selection/menu `-Disabled`, field/menu `-ChoiceDisabled` reason callbacks → `-DisabledReason`, `-ChoiceDisabledReason` | These return explanatory text rather than Boolean availability. Keep Choice.DisabledReason and the corresponding C# callbacks aligned (MenuItem.DisabledSource → DisabledReasonSource, CurrentState → CurrentDetail, State(...) → Detail(...), Disabled(...) → DisabledReason(...)). | P,C,D,E,T |

## C# parity, capability coverage and additions

The reusable behavior stays in ConsoleMonster. Cmdlets translate PowerShell values and host behavior into those types. Constructors, `Ui` factories and generated fluent methods are supported ways of building the same model; they are not three independent renderers to collapse. The fluent generator must regenerate changed property names at build time; generated C# is not checked in.

### Concrete C# changes and deliberate exceptions

| Before → after | Reason | Blast radius |
| --- | --- | --- |
| PromptCancelledException → PromptCanceledException | Match .NET OperationCanceledException and Task.IsCanceled spelling; update constructors, catches, XML references and PowerShell error id PromptCancelled → PromptCanceled. | C,P,D,E,T |
| InteractiveView.IsCancelled → IsCanceled; PromptLabels.Cancelled → Canceled | Public identifiers use American spelling; localized visible text may still say cancelled. | C,P,D,E,T |
| Widgets.Slot / SlotHandle → RegionWidget / RegionHandle | PowerShell already says ConsoleRegion and LiveSession.Region; Canvas.Region remains the drawing rectangle, so RegionWidget avoids a namespace collision. | C,P,D,E,T |
| Interactive.RunResult → MenuRunResult | Match New-ConsoleMenuRunResult; keep generic Live.RunResult<TItem,TResult> as execution results. | C,P,D,E,T |
| Ui.Status → Ui.StatusList; Ui.Progress → Ui.ProgressBar | Factories name the constructed C# widget, avoiding confusion with in-place status and native progress records. | C,D,E,T |
| C# LineChart.Min/Max, Sparkline.Max, BarChart.Max → Minimum/Maximum | Match numeric prompt/chart option names; regenerate fluent setters. | C,P,D,E,T |
| Terminal.Status/StatusAsync → retain | Scoped synchronous/asynchronous status operations; not constructors or PowerShell cmdlet names. Document the shared StatusLine implementation. | C,D,E,T |
| LiveSession.Start / RunAsync / Replay, SessionRecorder.Start, ConsoleDemo.Record/Present → retain | The C# methods already distinguish handle startup, awaiting work, replay and presentation. PowerShell blocking wrappers get the corresponding verbs. | C,D,E,T |
| LogRegion and KeyValueList → retain C# types | These accurately name resources; PowerShell New-ConsoleLogRegion now matches, and New-ConsoleKeyValue is the concise domain noun. | C,D,E,T |
| Interactive.Grid versus Layout.Grid → retain namespaces; qualify examples | Selection grid and layout grid are different types/jobs. Do not introduce a bare Grid accelerator. | D,E,T |
| Prompt<T>.Show/ShowAsync versus Read-Console* → retain | .NET object operation versus PowerShell answer-acquisition verb; renaming all Show methods adds no clarity. | D,E,T |
| Typed C# cancellation tokens, TimeSpan and options versus native script blocks/common parameters → retain | Host-specific binding differences are meaningful; match concepts and property names rather than forcing identical syntax. | C,P,D,E,T |

The C# vocabulary rows above also cover generated With*/Add* methods, public constructor/named arguments, DashboardBuilder overloads, chart/presenter rules, public cmdlet parameter properties and package examples. Keep overrides and interface contracts coherent across all target frameworks. `Canceled` identifiers must be swept across the whole public surface, while localized/help prose is left in its intended language.

### Additional commands worth exposing in this pass

These additions are included, except for the explicitly deferred canvas command. They use existing types and introduce no new production dependency.

| Before → after | Reason and final shape | Blast radius |
| --- | --- | --- |
| Get-ConsoleDemo -Path → `Export-ConsoleDemo -Slug -Path [-Force] [-PassThru]` | Separate saving a copyable embedded script from metadata retrieval. | P,D,E,T |
| Register-ConsoleView -Remove → `Unregister-ConsoleView -TypeName` or `-Path` | Complete the existing registration pair using the same registry. | P,D,E,T |
| HeaderBar only constructible through C# → `New-ConsoleHeaderBar` | Existing IWidget: Title, Subtitle, breadcrumbs, Status/semantic role, right content and elapsed-clock visibility (ShowElapsed); usable independently of Dashboard. | P,C,D,E,T |
| FooterBar only constructible through C# → `New-ConsoleFooterBar` | Existing IWidget: key hints, message/right content and segments. Accept data/hashtables rather than add several tiny item cmdlets. | P,C,D,E,T |
| StatColumn internal / ColumnFormat public → `New-ConsoleColumnFormat` and builder configuration parameters | Expose Alignment, MinWidth, ReserveDigits, NeverShrink; pass to count/percent/value/detail column settings on the relevant widgets. Keep StatColumn internal. | P,C,D,E,T |
| BrailleCanvas/HalfBlockCanvas/CharacterCanvas → retain C# drawing APIs; defer a canvas cmdlet | PixelCanvas is not IWidget. A future PowerShell drawing API needs an explicit coordinate/color contract and Region/RenderContext adapter. | D |

I include the header/footer/column-format exposure because they close everyday composition gaps without changing the rendering model. The **canvas cmdlet is deferred**: a useful PowerShell drawing contract needs a small adapter and a clear coordinate/color API, and there is no existing cmdlet consumer to constrain it. Keep the canvas classes documented as C# capabilities in this pass. This deferral is part of the approved scope.

The nine removed exports are Write-ConsoleTable, Write-ConsoleKeyValue, Write-ConsoleTree, Write-ConsoleWidget, Write-ConsoleMarkup, Write-ConsoleException, Write-ConsoleRule, Start-ConsoleRecording and Start-ConsoleReplay.

At the original inventory revision, these final decisions yield **134 exported cmdlets**: 137 existing, minus nine old exports, plus six additions (one-line rule, gallery export, view unregister, header, footer, column format). Renames do not change that count. Canvas is deferred and adds no command in this PR. The new count is a design consequence, not a target to preserve by adding commands.

### The shell prompt feature

`Enable-ConsoleFormatting -Feature Prompt` is not an input prompt. It installs a native PowerShell `prompt` function wrapper, and Disable restores it. `Widgets.ShellPrompt` renders provider path/home abbreviation, cached Git branch/dirty state, prior success/failure and a duration above two seconds. The installation supplies those values using the current provider/history and a bounded Git cache while preserving `$?`, `$LASTEXITCODE`, existing prompt signatures and native fallback. It refuses protected external prompt implementations unless Force is explicit.

| Before → after | Reason | Blast radius |
| --- | --- | --- |
| ShellPrompt via Feature Prompt only → retain installation through that feature | Installing/restoring a shell integration is not reading user input; do not create Read-ConsolePrompt or a second installer family. | D,E,T |
| No independent ShellPrompt builder → document C# ShellPrompt as the reusable renderer | Host-supplied prompt facts are distinct from automatic PowerShell integration. Defer a standalone constructor cmdlet unless a composition example needs it. | D,E,T |
| Prompt feature discoverability buried in formatting help → dedicated shell-prompt explanation linked from formatting and interactive docs | Explain provider path, Git, status, duration, protection/fallback and restoration; distinguish them from Read-ConsoleText/Show-ConsoleApp. | D,E,T |

### C# capability coverage map

The accompanying source type catalogue at the end inventories the public type declarations by namespace. This map records the disposition of the complete capability families, including those that should remain C# extension points rather than gain a cmdlet each.

| Capability family | Public entry points / disposition |
| --- | --- |
| Terminal output and targeting | Terminal.Write/Render/RenderText/RenderEncoded, ITerminalTarget/ITerminalWriter, ConsoleTarget/WriterTarget/RecordingTarget; retain, apply width/style vocabulary at the boundary. |
| Layout and building | IWidget/IContainer, Panel/Rows/Columns/Grid/Stack/Align/Padding/Sized, Ui and DashboardBuilder; retain construction choices and generated fluent parity. |
| Widgets and presenters | Table/Table<T>, RecordView, Tree/InspectorView, charts, progress, lanes, log/status/summary widgets, HeaderBar/FooterBar, ColumnFormat and ICellPresenter; normalize named concepts and add only the listed thin builders. |
| Live execution, capture and observation | LiveSession, LiveHandle/LiveEvent, ConsoleRun, LiveRun, RunMonitor/Tracker, ConsoleWatch and tails; keep lifetime differences explicit. |
| Input, prompts, forms, menus and applications | InputEvent/IInputSource, Prompt facade/classes, Field/Form, Menu/RunContext, ConsoleApp/AppState/InputControl, Grid/ObjectExplorer/WidgetPager; retain host abstractions and distinguish stored keys from labels. |
| Data shaping and changes | IMemberSource/ObjectInspector, ValueShape/ValueShaper, ConsoleView, ObjectDiff/TextDiff/ChangePreview; retain reusable engines, normalize hierarchy and diff-input terms. |
| Styling, glyphs, text and highlighting | Color/RichColor, Style/RichStyle, Theme/ThemeFile/ThemeToken, RichText/StyledLine/DisplayWidth/Markup, glyph/highlighter interfaces; keep terminal semantics, apply Role/Decoration vocabulary. |
| Canvas and rendering | PixelCanvas implementations, Cell/CellBuffer, Canvas.Region/Rect/RenderContext, FrameComposer and terminal encoders/outputs; retain C# rendering extension points. |
| Export, replay, sessions and demos | ConsoleExporter/Recording, SessionRecorder, ReplayPlayer, ConsoleDemo/DemoScript; retain shared engines, explicit recording format and output destinations. |
| Help, overview, localization, testing and test runs | CommandLineHelp/HelpTopic, OverviewCollector/registry, WidgetLabels/PromptLabels, TestTerminal/GoldenFile, TestRun/TestRunView; retain typed extension points and fixtures. |
| Markdown add-on | MarkdownConsole and MarkdownView with the existing OfficeIMO boundary; no parsing moved into cmdlets/core. |
| Graphics add-on | ConsoleGraphicsExtensions and GraphicsExportOptions, existing ChartForgeX/ffmpeg boundary; no new renderer/tool dependency. |
| Hosting and logging add-ons | IConsoleDashboard/options, DI/logging extensions/providers; retain C# host integrations, apply shared options vocabulary where exposed. |

## Module type exposure

I use a **small, fully qualified allow-list**. The default list is:

`ConsoleMonster.Terminal`, `ConsoleMonster.Layout.IWidget`, `ConsoleMonster.Styling.Color`, `ConsoleMonster.Styling.RichColor`, `ConsoleMonster.Styling.Style`, `ConsoleMonster.Styling.RichStyle`, `ConsoleMonster.Styling.Theme`, `ConsoleMonster.Styling.ThemeToken`, `ConsoleMonster.Styling.StatusKind`, `ConsoleMonster.Text.RichText`, `ConsoleMonster.Interactive.InteractionMode`, `ConsoleMonster.Capabilities.GlyphTier`, `ConsoleMonster.Capabilities.ColorDepth`.

Most scripts should construct widgets with cmdlets and receive typed objects normally. Advanced C# interop that names another ALC-loaded type must be qualified through a documented explicit loaded-type reference until a real consumer justifies adding it to the allow-list. Fully qualified accelerator names are already prefixed; adding CMTable/CMGrid would create another naming system and Grid would still need disambiguation. This is the scoped-prefix option, not an opt-in switch that silently keeps assembly-wide registration enabled by default.

| Before → after | Reason | Blast radius |
| --- | --- | --- |
| NETAssemblyTypeAcceleratorMode = Assembly → AllowList with the 13 names above | Explicit public scripting surface; prevent growth every time a C# helper becomes public. | P,D,E,T |
| Generated module bootstrapper edited by hand → source Build-Module configuration plus regeneration | PowerForge already provides AllowList; use its existing registration/collision/cleanup implementation. | P,D,T |
| Assumption that [Table]/[Panel]/[Color]/[Style]/[Terminal]/[Theme]/[Tree]/[Grid]/[Menu]/[Form]/[Rule]/[Size]/[Rect] are registered → corrected docs and clean-process import evidence | The current generator uses full names; do not invent removal of aliases the source never creates. | D,E,T |
| Broad reflection/short-name examples → cmdlets first, qualified retained interop where needed | Preserve the module's native PowerShell examples; migrate actual type literals removed from default exposure. | D,E,T |

Phase 2 must prove this in fresh PowerShell processes on the development and packaged loader paths: only listed registrations are added, a pre-existing foreign accelerator is never overwritten, removal touches only registrations owned by this module, re-import works, and representative parameter binding/returned-object use still works without assembly-wide exposure. A type being public in C# does not require an automatic PowerShell accelerator. If a real consumer cannot resolve a necessary type, revise the explicit list based on that case rather than automatically exposing the assembly again.

## Documentation contradictions and stale tasks

| Before → after | Evidence and reason | Blast radius |
| --- | --- | --- |
| ROADMAP Ownership says the core never emits semantic HTML or Markdown → describe the implemented output boundary | ExportFormat.Markdown, Table.Export and the existing Output-Exports guide implement semantic Markdown; HTML frame export and Markdown reading/add-on ownership need separate explanations. | D |
| ROADMAP backlog lists a Markdown viewer → describe current Show-ConsoleMarkdown and keep only remaining gaps | ConsoleMonster.Markdown/MarkdownConsole and the cmdlet exist. | D,E |
| ROADMAP mouse support as an unqualified future task → describe current input/app mouse support and any remaining widget-specific work | InputEvent.Mouse, native/VT input paths and ConsoleAppOptions.Mouse exist. | D |
| ROADMAP raster export backlog → distinguish current graphics export from unimplemented chart/image bridges | ConsoleMonster.Graphics exports frames/recordings; it does not imply every ChartForgeX terminal integration exists. | D |
| TODO “before first 2.0.0” and signing configuration described as empty → current release/readiness wording with verified build configuration | Manifest is 2.0.1; Build-Module has a configured signer. Do not change signing settings during API cleanup. | D |
| TODO Windows CI missing → remove the completed CI task | .github/workflows/test.yml already contains Windows and PowerShell test lanes. | D |
| TODO light-background detection requires an environment override → current automatic detection and explicit override limits | Terminal.ResolveTheme and TerminalBackground.TryDetect implement automatic background detection. Retain platform qualification work where unproved. | D |
| TODO hyperlinks, logging provider, Markdown and prompts/apps/mouse as wholly future → replace with current links and specific remaining work | Hyperlinks guide/core links, Extensions.Logging, Markdown add-on and Interactive/App input exist. | D,E |
| Old review labels / partially implemented “table v2” and other tasks → verify each item against current source/tests before removing it | LiveTable has sorting; interactive Grid has filtering, which does not prove filtering/scrolling in every live table. Presence of a class is not proof an entire task shipped. | D,T |
| README/guides/reference/examples use removed names → regenerate/update from the appropriate source | Update XML cmdlet help and examples before generating MAML, command pages and exports; keep maintained prose separate from generated pages. | P,C,D,E,T |

Do not mark the historical memory, concurrency, allocation, platform or terminal-restoration tasks complete based on this naming review. Their resolution needs current code/test evidence; this pass should prune proven stale entries and preserve specific unverified work.

## Implementation and verification

Implement these final decisions as one outcome in one PR, with logical commits for core vocabulary, command consolidation, and generated docs/examples/tests. Leave the PR open for review.

- [ ] Apply agreed C# and cmdlet changes together; remove old exports/aliases/shims and update all callers.
- [ ] Keep behavior in the current core/add-on component and translate only at the PowerShell boundary. Introduce no production dependencies.
- [ ] Regenerate fluent APIs through the existing generator; update public XML documentation and all supported target-framework source branches.
- [ ] Generate module exports/bootstrapper/help/Docs command pages through Module/Build/Build-Module.ps1's documentation mode with signing/install disabled. The new proposal is maintained prose, not generated command help.
- [ ] Regenerate the website command reference from Module/en-US/ConsoleMonster-help.xml using Website/pipeline.json; refresh API docs from the assemblies/XML and current public names.
- [ ] Update README, conventions/usage/output/interactive guides, maintained changelog, website links/navigation and all module/C#/website examples. Keep source code visible in the examples.
- [ ] Rebase whichever overlapping examples change lands second, preserving the demo code-quality changes; update both languages, gallery metadata/resources, captures, snippets and twins from the final sources.
- [ ] Add a vocabulary test over Get-Command metadata including inherited parameters, aliases, types and defaults. Assert that semantic slots use the agreed names, removed names/commands are absent, and only documented native adapters are exempt. Do not write a string test that merely mirrors this prose.
- [ ] Add focused behavioral proof for consolidated construction/display/capture, live addressing/state keys, native progress binding/restoration, prompt cancellation and export round trips. Keep recording protocol fields intentional.
- [ ] Validate core tests on net8.0 and net10.0 on this Mac; run Pester with both CurrentCulture and CurrentUICulture en-US. Use unsigned validation and no private-key access for these checks.
- [ ] Run gallery/native twin checks through Website/tools/SiteFrames and the existing Gallery Pester tests, then the complete Website CI pipeline. Specify dimensions for deterministic capture; inspect final representative 60/120-column, light/dark/plain and interactive/static states.
- [ ] Verify the resulting module's command count/export/help consistency, type-exposure lifecycle and generated-output drift. Native Windows PowerShell 5.1 remains a CI/Windows proof tier, not something Mac Pester establishes.
- [ ] Complete one stable candidate, push/open one PR with a concise before/after description, address current-head CI/review feedback and leave it unmerged.

Final usability decisions: keep Show-ConsoleDemo/Get-ConsoleDemo and add Export-ConsoleDemo; use Presentation for typed scripts; retain ConsoleDemo.Present/Record in C#. Show-ConsoleRule stays a one-liner. Show-ConsoleList accepts dictionaries directly, and Export-ConsoleFrame accepts ordinary objects through the automatic view path as well as widgets. WidgetId is position 0 on live updates/setters/adders and on builders where it is the natural first argument. Numeric duration names carry Seconds or Milliseconds; switches select the non-default behavior.

## Source parameter catalogue

This is the **before** inventory, extracted from public cmdlet parameter declarations and their base classes. Common PowerShell parameters supplied by the engine are omitted. Names are not endorsements: the vocabulary tables specify the proposed changes. It includes inherited prompt/app/recording/demo/error parameters, not just properties declared in each cmdlet file. Binding attributes, output contracts and meaningful defaults are discussed in the decisions above; Phase 2 verifies compiled metadata.

| Existing command | Declared and inherited parameters |
| --- | --- |
| [Add-ConsoleItem](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletAddConsoleItem.cs) | `InputObject`, `Item`, `KeyProperty`, `PassThru` |
| [Add-ConsoleRecordingMarker](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Recording/CmdletAddConsoleRecordingMarker.cs) | `Label` |
| [Add-ConsoleValue](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletAddConsoleValue.cs) | `Session`, `Name`, `Value`, `Key` |
| [Assert-ConsoleOutput](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletAssertConsoleOutput.cs) | `InputObject`, `GoldenPath`, `Width`, `Height`, `Glyphs`, `Culture`, `PassThru` |
| [Clear-ConsoleScreen](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletClearConsoleScreen.cs) | None beyond common parameters |
| [Compare-ConsoleObject](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Changes/CmdletCompareConsoleObject.cs) | `ReferenceObject`, `DifferenceObject`, `Property`, `CompareProperty`, `Ignore`, `Depth`, `IncludeEqual`, `View`, `Title`, `PassThru` |
| [Compare-ConsoleText](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Changes/CmdletCompareConsoleText.cs) | `ReferenceText`, `DifferenceText`, `ReferencePath`, `DifferencePath`, `ReferenceName`, `DifferenceName`, `Context`, `IgnoreWhitespace`, `AsUnified` |
| [Complete-ConsoleRun](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Runs/CmdletCompleteConsoleRun.cs) | `Run`, `PassThru` |
| [Copy-ConsoleOutput](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletCopyConsoleOutput.cs) | `InputObject`, `As`, `Width`, `Height`, `Culture`, `PassThru` |
| [Disable-ConsoleFormatting](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Formatting/CmdletEnableConsoleFormatting.cs) | `Feature` |
| [Enable-ConsoleFormatting](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Formatting/CmdletEnableConsoleFormatting.cs) | `Feature`, `Force`, `NoBuiltIn`, `ProfileSnippet`, `Detailed` |
| [Export-ConsoleFrame](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletExportConsoleFrame.cs) | `InputObject`, `Format`, `Path`, `Width`, `Height`, `Theme`, `Title`, `WindowChrome`, `ChromeStyle`, `FontSize`, `PassThru`, `Preset`, `Fit`, `Background`, `BackgroundColor`, `BackgroundEndColor`, `FontFamily` |
| [Export-ConsoleRecording](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletExportConsoleRecording.cs) | `Dashboard`, `Path`, `Format`, `Destination`, `Width`, `Height`, `FramesPerSecond`, `Speed`, `Theme`, `Title`, `WindowChrome`, `ChromeStyle`, `PassThru`, `FinalFrame`, `Preset`, `Fit`, `Background`, `BackgroundColor`, `BackgroundEndColor`, `FontFamily`, `At`, `Chapter`, `PerChapter`, `IdleLimit`, `LoopCount` |
| [Export-ConsoleTheme](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletExportConsoleTheme.cs) | `Path`, `PassThru` |
| [Format-ConsoleCode](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleCode.cs) | `InputObject`, `Path`, `Language`, `LineNumbers`, `Highlight`, `FirstLine`, `Wrap`, `MaxLines`, `Title`, `NoTitle`, `AsString`, `Width`, `AsWidget`, `As` |
| [Format-ConsoleError](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleError.cs) | `InputObject`, `ShowFrameworkFrames`, `MaxFrames`, `NoStack`, `Compact`, `AsString`, `As`, `Width`, `AsWidget` |
| [Format-ConsoleJson](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleCode.cs) | `InputObject`, `Depth`, `LineNumbers`, `MaxLines`, `Title`, `AsString`, `Width`, `AsWidget`, `As` |
| [Format-ConsoleList](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleList.cs) | `InputObject`, `Property`, `GroupBy`, `Title`, `ShowEmpty`, `Depth`, `Width`, `Culture`, `AsString`, `PassThru`, `As` |
| [Format-ConsoleTable](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleTable.cs) | `InputObject`, `Property`, `GroupBy`, `Total`, `Top`, `Title`, `Caption`, `Border`, `CardsBelow`, `Width`, `Culture`, `NoHeaderSeparator`, `AsString`, `PassThru`, `AsWidget`, `As`, `ShowGroupColumn`, `NoSort`, `View`, `Wrap` |
| [Format-ConsoleTree](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleTree.cs) | `InputObject`, `Depth`, `MaxItems`, `MaxNodes`, `Include`, `Exclude`, `Title`, `AsInspector`, `Width`, `Culture`, `AsString`, `PassThru`, `As` |
| [Get-ConsoleCapability](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletGetConsoleCapability.cs) | `Refresh` |
| [Get-ConsoleDemo](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Gallery/CmdletGetConsoleDemo.cs) | `Slug`, `Path`, `Force` |
| [Invoke-ConsoleDemo](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Demos/CmdletInvokeConsoleDemo.cs) | `Path`, `Width`, `Height`, `Seed`, `StepTimeoutSeconds`, `IdleLimit`, `Redact`, `Shell`, `Destination` |
| [Invoke-ConsolePester](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Pester/CmdletInvokeConsolePester.cs) | `Path`, `ExcludePath`, `TagFilter`, `ExcludeTagFilter`, `FullNameFilter`, `Container`, `Output`, `CI`, `PassThru`, `Configuration`, `FullScreen`, `ExportHtml`, `Title`, `NoLiveView` |
| [Invoke-ConsoleRecording](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Recording/CmdletInvokeConsoleRecording.cs) | `Path`, `Folder`, `Name`, `Shell`, `Width`, `Height`, `Redact`, `RedactionText`, `MaxBytes`, `IdleLimit`, `InProcess`, `TimeoutSeconds`, `ScriptBlock`, `Destination`, `Theme`, `WindowChrome`, `ChromeStyle`, `FinalFrame` |
| [Invoke-ConsoleRun](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletInvokeConsoleRun.cs) | `InputObject`, `ScriptBlock`, `Parallel`, `ThrottleLimit`, `TimeoutSeconds`, `Unordered`, `Name`, `Title` |
| [New-ConsoleActivityList](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleActivityList.cs) | `Name`, `MaxVisible`, `ShowIds`, `NoElapsed` |
| [New-ConsoleAlign](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleAlign.cs) | `Content`, `Widget`, `Horizontal`, `Vertical`, `Weight`, `Height`, `Fill` |
| [New-ConsoleBarChart](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleBarChart.cs) | `Data`, `Content`, `InputObject`, `Stacked`, `Unit`, `Name`, `Shape`, `Tone`, `Maximum`, `Warning`, `Critical` |
| [New-ConsoleBarItem](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleBarItem.cs) | `Label`, `Value`, `Tone` |
| [New-ConsoleBinding](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleBinding.cs) | `Content`, `Name` |
| [New-ConsoleButton](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleButton.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `OnClick` |
| [New-ConsoleCellMap](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleCellMap.cs) | `Status`, `Count`, `MaxRows`, `NoLegend`, `Columns`, `RowLabels`, `LegendLines`, `Name` |
| [New-ConsoleChartSeries](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleChartSeries.cs) | `Name`, `Value`, `Tone` |
| [New-ConsoleClock](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleClock.cs) | `Speed` |
| [New-ConsoleColumns](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleColumns.cs) | `Content`, `Gap`, `FixedWidth`, `CollapseBelow`, `Height`, `Fill` |
| [New-ConsoleDashboard](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleDashboard.cs) | `Content`, `Title`, `Subtitle`, `Footer`, `Name`, `Gap` |
| [New-ConsoleField](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleField.cs) | `Name`, `Label`, `Type`, `Page`, `Help`, `Parameter`, `Required`, `Default`, `Placeholder`, `Choices`, `Display`, `GroupBy`, `Describe`, `Badge`, `Disabled`, `Selected`, `CommandValue`, `Preview`, `LoadChoices`, `ArgumentList`, `DependsOn`, `VisibleWhen`, `Validate`, `Minimum`, `Maximum`, `Step`, `Integer`, `Kind`, `MustExist`, `Extension`, `OnText`, `OffText`, `Pattern`, `PatternMessage`, `MinLength`, `MaxLength`, `MaxLines`, `Height`, `CommandPlaceholder`, `NoRemember` |
| [New-ConsoleForm](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleForm.cs) | `Title`, `Content`, `Field`, `Description`, `Command`, `CommandStyle`, `NoReview`, `SubmitText`, `StorageKey`, `Preset` |
| [New-ConsoleFormPreset](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleFormPreset.cs) | `Name`, `Values`, `Description`, `Preview` |
| [New-ConsoleGrid](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleGrid.cs) | `Content`, `Columns` |
| [New-ConsoleHistogram](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleHistogram.cs) | `Edge`, `Value`, `Height`, `Unit`, `Tone`, `Name` |
| [New-ConsoleKeyValue](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleKeyValue.cs) | `Data`, `Content`, `Markup`, `Name` |
| [New-ConsoleKeyValueItem](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleKeyValueItem.cs) | `Key`, `Value`, `Status`, `Markup`, `Presenter`, `Unit` |
| [New-ConsoleLane](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleLane.cs) | `Key`, `Label`, `Total`, `Done`, `Status`, `Detail`, `Passed`, `Failed`, `Warned`, `Skipped`, `Started`, `Finished` |
| [New-ConsoleLanes](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleLanes.cs) | `Name`, `Content`, `InputObject`, `Lane`, `Total`, `Shape`, `Column`, `HideCompleted`, `CollapseWhenFinished`, `Transient` |
| [New-ConsoleLineChart](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleLineChart.cs) | `Data`, `Content`, `InputObject`, `Series`, `Height`, `Scatter`, `Unit`, `Minimum`, `Maximum`, `Name` |
| [New-ConsoleLiveTable](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleLiveTable.cs) | `Name`, `Column`, `Table`, `Title`, `Border`, `MaxRows`, `SortBy`, `Descending` |
| [New-ConsoleLog](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleLog.cs) | `Name`, `Content`, `InputObject`, `Height`, `Capacity`, `NoTime`, `Wrap` |
| [New-ConsoleLogEntry](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleLogEntry.cs) | `Message`, `Level`, `At` |
| [New-ConsoleLogo](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleLogo.cs) | `Text`, `Tagline`, `From`, `To`, `Alignment` |
| [New-ConsoleMenu](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleMenu.cs) | `Title`, `Content`, `Item`, `Preset`, `Description`, `Subtitle`, `Layout` |
| [New-ConsoleMenuItem](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleMenuItem.cs) | `Title`, `Action`, `Hotkey`, `Description`, `Icon`, `IconName`, `Badge`, `State`, `Disabled`, `Submenu`, `Form`, `OnSubmit`, `Select`, `MultiSelect`, `LoadChoices`, `ArgumentList`, `Single`, `Display`, `GroupBy`, `Describe`, `ChoiceBadge`, `ChoiceDisabled`, `Preview`, `Selected`, `Minimum`, `Maximum`, `OnChange`, `Page`, `Review`, `Presets`, `Separator`, `Exit` |
| [New-ConsoleMenuPreset](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleMenuPreset.cs) | `Name`, `Description`, `Preview`, `Form`, `Values`, `Item`, `Selection`, `Selections` |
| [New-ConsoleMultiSelect](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleMultiSelect.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `Choices`, `Display`, `Value` |
| [New-ConsolePadding](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsolePadding.cs) | `Content`, `Widget`, `Padding`, `Weight`, `Height`, `Fill` |
| [New-ConsolePanel](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsolePanel.cs) | `Content`, `Widget`, `Title`, `Caption`, `Footer`, `Border`, `Weight`, `Height`, `Fill` |
| [New-ConsolePhases](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsolePhases.cs) | `Phase`, `Content`, `InputObject`, `Name` |
| [New-ConsoleProgress](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleProgress.cs) | `Label`, `Value`, `Total`, `Status`, `Name`, `Shape`, `Column` |
| [New-ConsoleRegion](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleRegion.cs) | `Name`, `Content`, `Widget`, `Weight`, `Height`, `Fill` |
| [New-ConsoleRows](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleRows.cs) | `Content`, `Gap`, `Weight`, `Fill` |
| [New-ConsoleRule](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleRule.cs) | `Title`, `Alignment` |
| [New-ConsoleRun](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Runs/CmdletNewConsoleRun.cs) | `Items`, `Title`, `Name`, `Target`, `Group`, `NoLive`, `InteractiveKeys`, `Trend`, `HeightPercent`, `ProgressEvery`, `ProgressSeconds`, `ReportPath` |
| [New-ConsoleRunResult](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletNewConsoleRunResult.cs) | `Title`, `Status`, `Summary`, `Figure`, `Next`, `FigureStatus`, `Elapsed`, `Details` |
| [New-ConsoleSelect](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleSelect.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `Choices`, `Display`, `Value` |
| [New-ConsoleSparkline](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleSparkline.cs) | `Name`, `Value`, `Label`, `Height`, `Unit`, `InputObject`, `Tone`, `Maximum`, `NoLatest` |
| [New-ConsoleSpinner](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleSpinner.cs) | `Label`, `Detail`, `Status`, `Name`, `NoElapsed` |
| [New-ConsoleStack](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleStack.cs) | `Content`, `Widget`, `Weight`, `Height`, `Fill` |
| [New-ConsoleStatusItem](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleStatusItem.cs) | `Label`, `Status`, `Detail`, `Key` |
| [New-ConsoleStatusList](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleStatusList.cs) | `Data`, `Content`, `InputObject`, `Item`, `Name` |
| [New-ConsoleSummaryBar](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleSummaryBar.cs) | `Done`, `Total`, `Counter`, `Elapsed`, `BarWidth`, `Name` |
| [New-ConsoleTable](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleTable.cs) | `InputObject`, `Bind`, `Property`, `StatusProperty`, `Title`, `Caption`, `Border`, `NoHeaderSeparator`, `Expand` |
| [New-ConsoleText](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleText.cs) | `Text`, `Color`, `BackgroundColor`, `Markup`, `Style`, `Link`, `Role`, `Gradient`, `Alignment`, `Plain` |
| [New-ConsoleTextArea](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleTextArea.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `Value`, `Placeholder`, `MaxLength`, `Height` |
| [New-ConsoleTextInput](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleTextInput.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `Value`, `Placeholder`, `MaxLength` |
| [New-ConsoleToggle](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletNewConsoleToggle.cs) | `Name`, `Label`, `Disabled`, `TabIndex`, `OnChange`, `Value` |
| [New-ConsoleTree](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletNewConsoleTree.cs) | `Title`, `Content`, `Data`, `InputObject`, `ChildrenProperty`, `Children`, `NameProperty`, `StatusProperty`, `DetailProperty`, `Depth`, `MaxItems`, `MaxNodes` |
| [New-ConsoleTreeNode](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Items/CmdletNewConsoleTreeNode.cs) | `Label`, `Content`, `Status`, `Detail`, `Value`, `Presenter`, `Unit` |
| [Out-ConsoleChart](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletOutConsoleChart.cs) | `InputObject`, `Property`, `Label`, `Type`, `Top`, `Sort`, `Bins`, `Unit`, `Width`, `AsString`, `AsWidget` |
| [Out-ConsoleDefault](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Formatting/CmdletOutConsoleDefault.cs) | `As`, `Width`, `InputObject`, `Transcript`, `Force`, `Shim`, `TopLevel`, `HelpSink` |
| [Out-ConsoleFormatCommand](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Formatting/CmdletOutConsoleFormatCommand.cs) | `InputObject`, `AsWidget`, `Capture` |
| [Read-ConsoleConfirm](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleConfirm.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Default`, `Destructive`, `TypedConfirmation`, `Warning`, `YesText`, `NoText` |
| [Read-ConsoleForm](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleForm.cs) | `Form`, `AppName`, `RememberLastChoices`, `StorageRoot`, `Inline`, `Preset`, `PassThru`, `AsPlainText`, `Mode` |
| [Read-ConsoleMultiSelection](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleMultiSelection.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Choices`, `Display`, `GroupBy`, `Describe`, `Badge`, `Hotkey`, `Disabled`, `Preview`, `PageSize`, `Selected`, `Minimum`, `Maximum`, `Collapsed` |
| [Read-ConsoleNumber](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleNumber.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Default`, `Minimum`, `Maximum`, `Step`, `Integer`, `Validate` |
| [Read-ConsolePath](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsolePath.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Kind`, `MustExist`, `Default`, `Extension`, `BasePath` |
| [Read-ConsoleSecret](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleSecret.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `AsPlainText`, `Hidden`, `AllowEmpty`, `MinLength`, `Validate` |
| [Read-ConsoleSelection](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleSelection.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Choices`, `Display`, `GroupBy`, `Describe`, `Badge`, `Hotkey`, `Disabled`, `Preview`, `PageSize`, `Default` |
| [Read-ConsoleText](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletReadConsoleText.cs) | `Title`, `Description`, `ParameterName`, `Mode`, `StaticSnapshot`, `Default`, `Placeholder`, `Required`, `MinLength`, `MaxLength`, `Pattern`, `PatternMessage`, `Validate`, `History`, `Prefill`, `Multiline`, `MaxLines`, `Height` |
| [Register-ConsoleView](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Formatting/CmdletEnableConsoleFormatting.cs) | `TypeName`, `Path`, `View`, `Property`, `GroupBy`, `Title`, `Group`, `Shaper`, `Remove` |
| [Send-ConsoleEvent](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletSendConsoleEvent.cs) | `Session`, `Target`, `Operation`, `Key`, `Text`, `State`, `Value`, `Total` |
| [Set-ConsoleFormValue](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletSetConsoleFormValue.cs) | `Form`, `Value` |
| [Set-ConsoleRegion](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletSetConsoleRegion.cs) | `Session`, `Name`, `Content`, `Widget`, `Clear` |
| [Set-ConsoleTheme](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletSetConsoleTheme.cs) | `Name`, `Path` |
| [Set-ConsoleValue](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletSetConsoleValue.cs) | `Session`, `Key`, `Value`, `Name` |
| [Show-Console](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletShowConsole.cs) | `InputObject`, `View`, `Property`, `ChildProperty`, `Chart`, `Title`, `Width`, `Depth`, `AsString` |
| [Show-ConsoleApp](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/App/CmdletShowConsoleApp.cs) | `Content`, `PassThru`, `StaticSnapshot`, `NoMouse`, `Mode`, `InputSource`, `Capture`, `Profile`, `Theme`, `Live` |
| [Show-ConsoleChanges](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Changes/CmdletCompareConsoleObject.cs) | `Before`, `After`, `Property`, `Ignore`, `View`, `Title`, `Question`, `TypedConfirmation`, `Force` |
| [Show-ConsoleDemo](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Gallery/CmdletShowConsoleDemo.cs) | `Slug`, `StaticSnapshot`, `Step` |
| [Show-ConsoleGrid](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletShowConsoleGrid.cs) | `InputObject`, `Property`, `Title`, `OutputMode`, `Filter`, `SortBy`, `Descending`, `Hide`, `Culture`, `Mode`, `UnavailableFallback` |
| [Show-ConsoleHelp](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Help/CmdletShowConsoleHelp.cs) | `Name`, `Width`, `AsString`, `NoExamples` |
| [Show-ConsoleMarkdown](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Markdown/CmdletShowConsoleMarkdown.cs) | `Content`, `Path`, `Width`, `NoPager`, `AsString` |
| [Show-ConsoleMenu](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletShowConsoleMenu.cs) | `Menu`, `Layout`, `AppName`, `RememberLastChoices`, `StorageRoot`, `NoCommandPalette`, `Mode` |
| [Show-ConsoleObject](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletShowConsoleObject.cs) | `InputObject`, `PassThru`, `Output`, `RootPath`, `Title`, `Depth`, `MaxItems`, `MaxNodes`, `Include`, `Exclude`, `Mode`, `Width` |
| [Show-ConsoleOverview](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletShowConsoleOverview.cs) | `Section`, `AsObject`, `Width`, `AsString`, `BudgetMilliseconds` |
| [Show-ConsoleReplay](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Interactive/CmdletShowConsoleReplay.cs) | `Dashboard`, `Path`, `Title`, `Speed`, `Inline`, `Paused`, `Height` |
| [Start-ConsoleDashboard](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletStartConsoleDashboard.cs) | `Dashboard`, `ScriptBlock`, `ArgumentList`, `InputObject`, `Parallel`, `ThrottleLimit`, `ImportModule`, `RecordPath`, `AutoRecord`, `Summary`, `Inline`, `Transient`, `Clock`, `FramesPerSecond` |
| [Start-ConsoleDemo](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Demos/CmdletStartConsoleDemo.cs) | `Path`, `Width`, `Height`, `Seed`, `StepTimeoutSeconds`, `IdleLimit`, `Redact`, `Shell`, `Record` |
| [Start-ConsoleRecording](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Recording/CmdletStartConsoleRecording.cs) | `Path`, `Folder`, `Name`, `Shell`, `Width`, `Height`, `Redact`, `RedactionText`, `MaxBytes`, `IdleLimit`, `InProcess`, `TimeoutSeconds` |
| [Start-ConsoleReplay](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletStartConsoleReplay.cs) | `Dashboard`, `Path`, `Speed` |
| [Stop-ConsoleRecording](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Recording/CmdletStopConsoleRecording.cs) | None beyond common parameters |
| [Update-ConsoleActivity](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleActivity.cs) | `Session`, `Id`, `Activity`, `Status`, `Percent`, `SecondsRemaining`, `ParentId`, `Complete`, `Name` |
| [Update-ConsoleCell](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleCell.cs) | `Session`, `Index`, `Status`, `Add`, `Times`, `Count`, `Name` |
| [Update-ConsoleLane](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleLane.cs) | `Session`, `Key`, `Name`, `Result`, `Steps`, `Status`, `Detail`, `Label`, `Total`, `Complete` |
| [Update-ConsolePhase](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsolePhase.cs) | `Session`, `Key`, `Status`, `Detail`, `Name` |
| [Update-ConsoleProgress](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleProgress.cs) | `Session`, `Name`, `Value`, `Total`, `Increment`, `Label`, `Status` |
| [Update-ConsoleRun](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Runs/CmdletUpdateConsoleRun.cs) | `Run`, `Index`, `State`, `Phase`, `Message`, `Duration` |
| [Update-ConsoleSpinner](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleSpinner.cs) | `Session`, `Name`, `Label`, `Detail`, `Restart`, `Status` |
| [Update-ConsoleStatus](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleStatus.cs) | `Session`, `Key`, `Status`, `Label`, `Detail`, `Name` |
| [Update-ConsoleSummaryBar](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleSummaryBar.cs) | `Session`, `Done`, `Total`, `Counter`, `Name` |
| [Update-ConsoleTable](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUpdateConsoleTable.cs) | `Session`, `Name`, `Key`, `Value`, `Remove`, `Clear` |
| [Use-ConsoleProgress](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUseConsoleProgress.cs) | `ScriptBlock`, `Keep`, `FramesPerSecond` |
| [Use-ConsoleTarget](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletUseConsoleTarget.cs) | `ScriptBlock`, `Path`, `Append`, `Writer`, `Target`, `Width`, `Glyphs`, `Colors` |
| [Wait-ConsoleRun](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Runs/CmdletWaitConsoleRun.cs) | `Run` |
| [Watch-Console](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWatchConsole.cs) | `ScriptBlock`, `Interval`, `Count`, `Differences`, `Title` |
| [Watch-ConsoleLog](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/Tails/CmdletWatchConsoleLog.cs) | `Path`, `LogName`, `Encoding`, `Tail`, `Count`, `Interval`, `Filter`, `Highlight`, `TimestampFormat`, `Capacity`, `Plain` |
| [Write-ConsoleException](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletFormatConsoleError.cs) | `InputObject`, `ShowFrameworkFrames`, `MaxFrames`, `NoStack`, `Compact`, `AsString`, `As`, `Width` |
| [Write-ConsoleKeyValue](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleKeyValue.cs) | `Data`, `Content`, `Markup`, `Name`, `InputObject` |
| [Write-ConsoleLog](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleLog.cs) | `Session`, `Message`, `Level`, `Name`, `Highlight` |
| [Write-ConsoleMarkup](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleMarkup.cs) | `Text`, `NoNewLine` |
| [Write-ConsoleProgress](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleProgress.cs) | `Activity`, `Status`, `Id`, `PercentComplete`, `SecondsRemaining`, `CurrentOperation`, `ParentId`, `Completed`, `SourceId` |
| [Write-ConsoleRule](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleRule.cs) | `Title`, `Width` |
| [Write-ConsoleStatus](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleStatus.cs) | `Text`, `Color`, `BackgroundColor`, `Markup`, `Style`, `Role`, `Status`, `Spinner`, `ShowClock`, `ShowElapsed`, `ClockFormat`, `Complete` |
| [Write-ConsoleTable](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleTable.cs) | `InputObject`, `Bind`, `Property`, `StatusProperty`, `Title`, `Caption`, `Border`, `NoHeaderSeparator`, `Expand` |
| [Write-ConsoleText](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleText.cs) | `Text`, `Color`, `BackGroundColor`, `StartTab`, `LinesBefore`, `LinesAfter`, `StartSpaces`, `LogFile`, `DateTimeFormat`, `LogTime`, `LogRetry`, `Encoding`, `ShowTime`, `NoNewLine`, `HorizontalCenter`, `NoConsoleOutput`, `Markup`, `Style`, `Link`, `Role`, `Gradient`, `PadLeft`, `PadRight`, `PadCenter`, `PadCharacter`, `Wrap`, `Width`, `SplitLines`, `LogFormat`, `LogSeparator`, `Level`, `Category` |
| [Write-ConsoleTree](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleTree.cs) | `Title`, `Content`, `Data`, `InputObject`, `ChildrenProperty`, `Children`, `NameProperty`, `StatusProperty`, `DetailProperty`, `Depth`, `MaxItems`, `MaxNodes` |
| [Write-ConsoleWidget](https://github.com/EvotecIT/ConsoleMonster/blob/ab25431fe4f79fc487643f8c0e1765da056f72f8/ConsoleMonster.PowerShell/CmdletWriteConsoleWidget.cs) | `Widget`, `Width` |

## C# source type catalogue

This catalogue covers public type declarations in the shipped core and four add-on libraries. It folds partial declarations and includes generic arity, excluding public members inside inaccessible implementation types. Conditional declarations are source coverage, not a compiled API compatibility baseline. Methods/properties inherit the keep-or-normalize decisions of their family above; only explicit rows propose renaming types or adding cmdlets. Cmdlet classes are covered by the command catalogue, and the build-time generator is a tooling surface rather than a shipped runtime API.

| Namespace | Public type declarations at the inspected source |
| --- | --- |
| `ConsoleMonster` | `Terminal`, `Ui` |
| `ConsoleMonster.Building` | `ColumnsBuilder`, `DashboardBuilder`, `RowsBuilder`, `RowsBuilderBase<TSelf>` |
| `ConsoleMonster.Canvas` | `BrailleCanvas`, `Cell`, `CellBuffer`, `CharacterCanvas`, `DiffRun`, `FrameDiff`, `HalfBlockCanvas`, `PixelCanvas`, `Rect`, `Region`, `RenderContext` |
| `ConsoleMonster.Capabilities` | `CapabilityDetector`, `ColorDepth`, `GlyphTier`, `ICapabilityProvider`, `OutputMode`, `TerminalEnvironment`, `TerminalKind`, `TerminalProfile`, `ThemePreference` |
| `ConsoleMonster.Changes` | `ChangeKind`, `ChangePreview`, `ChangePreviewOptions`, `ChangeSet`, `ChangeView`, `ChangeViewMode`, `DiffLine`, `DiffOptions`, `ObjectChange`, `ObjectDiff`, `PropertyChange`, `TextDiff`, `TextDiffLine`, `TextDiffOptions`, `TextDiffResult`, `TextDiffView` |
| `ConsoleMonster.Clipboard` | `ClipboardService` |
| `ConsoleMonster.Data` | `ColumnInference`, `IMemberSource`, `InspectCollection`, `InspectCycle`, `InspectError`, `InspectLimit`, `InspectMember`, `InspectNode`, `InspectNodeKind`, `InspectObject`, `InspectScalar`, `InspectorOptions`, `ObjectInspector` |
| `ConsoleMonster.Demos` | `ConsoleDemo`, `DemoOptions`, `DemoPresenter`, `DemoPresenterAction`, `DemoScript`, `DemoStep`, `DemoStepKind`, `DemoTypingClock`, `SemanticPromptDetector` |
| `ConsoleMonster.Export` | `ConsoleExporter`, `ExportFormat`, `ExportFrameSize`, `ExportOptions`, `ExportTheme`, `RecordedFrame`, `Recording`, `RecordingExportOptions`, `RecordingMarker`, `WindowChromeStyle` |
| `ConsoleMonster.Extensions.Logging` | `ConsoleMonsterLoggerOptions`, `ConsoleMonsterLoggerProvider`, `ConsoleMonsterLoggingExtensions`, `LogCategoryFormat` |
| `ConsoleMonster.Glyphs` | `BoxGlyphs`, `BoxStyle`, `Glyph`, `GlyphIcon`, `GlyphSet`, `GlyphSetExtensions`, `IGlyphSet`, `Shortcodes`, `SymbolFallback` |
| `ConsoleMonster.Graphics` | `ConsoleGraphicsExtensions`, `GraphicsBackground`, `GraphicsExportOptions`, `GraphicsFit`, `GraphicsFormat`, `GraphicsPreset` |
| `ConsoleMonster.Help` | `CommandLineHelp`, `HelpCommand`, `HelpExample`, `HelpLink`, `HelpParameter`, `HelpSyntax`, `HelpSyntaxParameter`, `HelpTopic`, `HelpValue` |
| `ConsoleMonster.Highlighting` | `CsvHighlighter`, `DiffHighlighter`, `HighlightRole`, `HighlightStyles`, `HighlightToken`, `Highlighters`, `ITextHighlighter`, `JsonHighlighter`, `LogHighlighter`, `MemberSignatureHighlighter`, `PowerShellHighlighter`, `RegexHighlighter`, `SmartHighlighter`, `XmlHighlighter`, `YamlHighlighter` |
| `ConsoleMonster.Hosting` | `ConsoleDashboardMode`, `ConsoleDashboardOptions`, `ConsoleDashboardServiceCollectionExtensions`, `IConsoleDashboard`, `ServiceState`, `ServiceStatus`, `ServicesView` |
| `ConsoleMonster.Input` | `ConsoleInput`, `ConsoleKeyInput`, `IInputSource`, `InputClosedException`, `InputEvent`, `InputEventKind`, `InputLease`, `KeyChord`, `KeyModifiers`, `ScriptedInput`, `VtInputDecoder`, `WindowsConsoleInput` |
| `ConsoleMonster.Interactive` | `AppBinding`, `AppState`, `AppValueChangedEventArgs`, `Button`, `Choice`, `ChoiceField`, `ChoiceStore`, `CommandLine`, `CommandProvider`, `CommandStyle`, `ConfirmPrompt`, `ConsoleApp`, `ConsoleAppOptions`, `ConsoleLineConsole`, `ExplorerNode`, `Field`, `FieldKind`, `FocusManager`, `Form`, `FormOptions`, `FormPage`, `FormPreset`, `FormResult`, `FormValues`, `Grid`, `GridOptions`, `GridResult`, `GridRow`, `GridSelection`, `ICommandProvider`, `ILineConsole`, `InputControl`, `InteractionMode`, `InteractiveOptions`, `KeyBinding`, `Menu`, `MenuContext`, `MenuItem`, `MenuItemKind`, `MenuLayout`, `MenuOptions`, `MenuPreset`, `MultiSelect`, `MultiSelectPrompt<T>`, `NumberField`, `NumberPrompt`, `ObjectExplorer`, `PaletteCommand`, `PathField`, `PathKind`, `PathPrompt`, `Prompt`, `Prompt<T>`, `PromptCancelledException`, `PromptLabels`, `PromptUnavailableException`, `ReviewModel`, `RunContext`, `RunResult`, `ScreenCapture`, `SecretDisplay`, `SecretField`, `SecretPrompt`, `Select`, `SelectPrompt<T>`, `SelectionInputBase`, `TextArea`, `TextAreaField`, `TextAreaPrompt`, `TextField`, `TextInput`, `TextPrompt`, `Toast`, `Toggle`, `ToggleField`, `TypedConfirmPrompt`, `WidgetPager` |
| `ConsoleMonster.Layout` | `Align`, `ColumnFormat`, `Columns`, `Constraints`, `CultureScope`, `Grid`, `HorizontalAlignment`, `ICompactWidget`, `IContainer`, `IWidget`, `LayoutSize`, `LayoutSizeKind`, `Padding`, `Panel`, `Prioritized`, `Rows`, `Size`, `Sized`, `Stack`, `VerticalAlignment` |
| `ConsoleMonster.Live` | `ConsoleRun`, `ConsoleWatch`, `ILiveDispatcher`, `ILiveSummary`, `ILiveTarget`, `ILogTarget`, `ImmediateDispatcher`, `InlineArea`, `LiveEvent`, `LiveHandle`, `LiveMode`, `LivePause`, `LiveRecorder`, `LiveRecordingReader`, `LiveRun`, `LiveRunOptions`, `LiveSession`, `LiveSessionCounters`, `LiveSessionOptions`, `RunActivity`, `RunFailure`, `RunItem`, `RunItemSnapshot`, `RunMonitor`, `RunOptions`, `RunResult<TItem, TResult>`, `RunSnapshot`, `RunTracker`, `SimulatedClock`, `TerminalScope`, `WatchFrame`, `WatchOptions` |
| `ConsoleMonster.Live.Tails` | `ConsoleLogTail`, `FileLogTail`, `LogLineParser`, `LogTailOptions`, `LogTailView`, `TailedLogEntry` |
| `ConsoleMonster.Localization` | `LabelCatalog`, `LabelLanguage`, `WidgetLabels` |
| `ConsoleMonster.Markdown` | `MarkdownConsole` |
| `ConsoleMonster.Output` | `ConsoleFontWatcher`, `ConsoleHandleWriter`, `ConsoleLogFile`, `ConsoleLogFormat`, `ConsoleLogRecord`, `ConsoleTarget`, `ITaskbarProgressSource`, `ITerminalTarget`, `ITerminalWriter`, `NullTarget`, `RecordingTarget`, `StatusContext`, `StatusLine`, `StreamTerminalWriter`, `StringTerminalWriter`, `TaskbarProgress`, `TaskbarState`, `TerminalBackground`, `TerminalBackgroundInfo`, `TerminalGuard`, `TerminalSize`, `TerminalTargetExtensions`, `TerminalWidthProbe`, `TerminalWriterFactory`, `TextTerminalWriter`, `WriterTarget` |
| `ConsoleMonster.Overview` | `OverviewCollector`, `OverviewItem`, `OverviewOptions`, `OverviewSection`, `OverviewSectionRegistry`, `OverviewSnapshot`, `OverviewWidget` |
| `ConsoleMonster.Presenters` | `AutoPresenter`, `BooleanPresenter`, `BytesPresenter`, `CellPresenters`, `CellVisualKind`, `ChipPresenter`, `DataBarPresenter`, `DateDisplay`, `DatePresenter`, `DurationPresenter`, `EmojiPresenter`, `HighlightPresenter`, `ICellPresenter`, `IdentifierPresenter`, `LinkPresenter`, `MarkupPresenter`, `NumberPresenter`, `PresentationOptions`, `PresentedValue`, `PresenterContext`, `PresenterValues`, `ProgressPresenter`, `SparklinePresenter`, `StatusEnumPresenter`, `StatusPresenter`, `TextPresenter`, `ToneRule` |
| `ConsoleMonster.Rendering` | `FrameComposer`, `ITerminalOutput`, `InlineOutput`, `LineOutput`, `SgrEncoder`, `VtOutput` |
| `ConsoleMonster.Replay` | `ReplayMarker`, `ReplayMarkerKind`, `ReplayOptions`, `ReplayPlayer` |
| `ConsoleMonster.Sessions` | `SessionRecorder`, `SessionRecording`, `SessionRecordingOptions` |
| `ConsoleMonster.Styling` | `Color`, `ColorKind`, `ConsoleColorMap`, `ITheme`, `LogLevels`, `RichColor`, `RichStyle`, `StatusKind`, `StatusKinds`, `Style`, `TextDecoration`, `Theme`, `ThemeColor`, `ThemeFile`, `ThemeStyle`, `ThemeToken` |
| `ConsoleMonster.TestRuns` | `TestContainerState`, `TestRecord`, `TestRun`, `TestRunSnapshot`, `TestRunSummary`, `TestRunText`, `TestRunView` |
| `ConsoleMonster.Testing` | `GoldenFile`, `GoldenFileMismatchException`, `TestTerminal` |
| `ConsoleMonster.Text` | `ConsoleTextLayout`, `DisplayPath`, `DisplayWidth`, `FuzzyMatch`, `FuzzyMatcher`, `FuzzyResult<T>`, `Markup`, `MarkupExtensions`, `RichSegment`, `RichText`, `RichTextBuilder`, `StyledLine`, `StyledText`, `TerminalWidthProfile`, `TextCell`, `TextSpan`, `TextWrapper`, `ZwjRendering` |
| `ConsoleMonster.Values` | `BudgetExceeded`, `CollectionDisplay`, `FieldError`, `ShapeBudget`, `ShapeOptions`, `ValueJson`, `ValueShape`, `ValueShaper` |
| `ConsoleMonster.Views` | `ConsoleView`, `ConsoleViewKind`, `ConsoleViewOptions` |
| `ConsoleMonster.Widgets` | `ActivityList`, `BarChart`, `BarShape`, `CellMap`, `CellWindow`, `ChartItem`, `ChartSeries`, `CodeView`, `ColumnAggregate`, `ColumnOverflow`, `Dashboard`, `ExceptionFrame`, `ExceptionInfo`, `ExceptionView`, `FooterBar`, `FooterSegment`, `HeaderBar`, `HelpView`, `Histogram`, `InspectorTree`, `InspectorView`, `KeyHint`, `KeyValueHandle`, `KeyValueItem`, `KeyValueList`, `Lane`, `Lanes`, `LanesHandle`, `LineChart`, `LiveTable`, `LiveTableHandle`, `LiveWidget`, `LogEntry`, `LogHandle`, `LogRegion`, `LogTags`, `Logo`, `MarkdownView`, `NumberFormat`, `Phases`, `PhasesHandle`, `PipelineChart`, `PipelineChartSort`, `PipelineChartType`, `ProgressBar`, `ProgressColumn`, `ProgressColumns`, `ProgressHandle`, `ProgressRow`, `RecordItem`, `RecordItemKind`, `RecordView`, `Rule`, `RunCompletion`, `RunDashboard`, `RunProgress`, `RunSummary`, `RunView`, `ShellPrompt`, `Slot`, `SlotHandle`, `Sparkline`, `SparklineHandle`, `Spinner`, `SpinnerHandle`, `StackedBar`, `StatusItem`, `StatusList`, `StatusListHandle`, `SummaryBar`, `SummaryBarHandle`, `Table`, `Table<T>`, `TableBorder`, `TableCell`, `TableColumn`, `TableFluent`, `TextBlock`, `TextOverflow`, `Tree`, `TreeHierarchy`, `TreeHierarchyOptions`, `TreeNode`, `WatchView` |

## Merged gallery additions

I rebased this change onto the demo source cleanup in #53. That adds two commands after the original inventory, bringing the exported surface to **136 cmdlets**.

| Before → after | Reason | Blast radius |
| --- | --- | --- |
| `Format-ConsoleCommand` → same | Returns a copyable command string without executing or displaying it, following the Format rule. | P,D,E,T |
| `Wait-ConsoleDashboard -Milliseconds` → same | Pauses the active dashboard cancellably; the numeric parameter states its unit, and the capture driver owns checkpoints. | P,D,E,T |

Gallery programs keep explicit object rows and ordinary execution. Scripted keys, snapshots and checkpoints stay in the capture driver; copied scripts have no `-StaticSnapshot` or `-Step` parameters. PowerShell scripts use UTF-8 with a BOM.

The acceptance contract is one entry point for each operation, with definitions and lifecycle steps treated as different jobs: Show displays, New builds, and Update changes an addressed live widget. Question workflows acquire answers or run a selected action and return to the script; application controls retain focus, events and mutable state in a composed screen. Native progress submission (`Write-ConsoleProgress`) and scoped interception (`Use-ConsoleProgress`) are distinct from constructing and updating a dashboard bar. The automatic viewer chooses a view; the named viewers select a particular representation. Replay has one viewer and an explicit playback mode.

The module exports no command aliases or compatibility parameter aliases. `Enable-ConsoleFormatting -Feature Progress` remains the explicit native `Write-Progress` integration: it preserves PowerShell's command name and parameter vocabulary, rather than reviving a retired ConsoleMonster name. The generated loader's generic alias re-export bridge receives no aliases from this module. Tests cover all 29 retired or renamed cmdlet names, exported aliases, declared parameter aliases and fresh-process accelerator registration. The only default accelerators are the 13 fully qualified names listed above; no unprefixed accelerator is added.

## Principles

1. Give each job one primary command. Keep constructors, viewers and running-session updates distinct; share their implementation.
2. Use approved verbs for observable behavior. New creates definitions; Show displays; Invoke waits for work; Start returns an active handle.
3. Use WidgetId for a live address, Key for stored identity, Label for visible item text and Title for a view heading.
4. Use Color/BackgroundColor for literal colors, Role for semantic theme styling and Decoration for text decoration. Public identifiers use American spelling and .NET cancellation conventions.
5. Give each timing and sizing concept its own name and units. PowerShell durations are numbers with units in their names; C# durations are TimeSpan. Switches select non-default behavior. Width 0 resolves available width, with a 100-cell fallback; BarWidth and Weight retain their separate meanings.
6. Preserve ordinary pipeline objects until an explicit display/export boundary. Make string/widget capture deterministic and keep native PowerShell adapters narrowly documented.
7. Keep PowerShell, C#, fluent builders, help, module exports and examples in step. Expose only deliberate module types and useful thin cmdlet adapters.
8. Remove superseded names and aliases before release, update generated sources and every real caller, and keep documentation aligned with implemented behavior.
