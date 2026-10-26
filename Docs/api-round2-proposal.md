# Round 2: fewer commands, one operation model

**Approved, 25 October 2026.** [Round 1](api-consistency-proposal.md) establishes names, parameter vocabulary, accelerator ownership and the Markdown package boundary. This document proposes the next breaking pass. The examples and signatures below describe the proposed API, except where explicitly labelled current.

The approved #55 baseline remains **136 → 97**. With the feature PRs merged at master `dd5d7ce`, the expanded surface is **144 → 101** (46 retired names and three introduced). Flow, Calendar, Command and ScrollView each remain one distinct construction job. The integration record below identifies their contracts.

For the original #55 surface I recommended **136 → 97 exported cmdlets**, a reduction of 39 (28.7%). That retires 42 current names and introduces three: `Update-Console`, `New-ConsoleChart` and `New-ConsoleLayoutGrid`. The last is a clarifying rename with no effect on the total. There are no compatibility aliases, parameter aliases or obsolete forwarding methods. Show-ConsoleTable and Show-ConsoleList remain idiomatic shortcuts, sharing the Show-Console view implementation. Constructors and display remain separate operations.

Round 1's other rules stay: no compatibility shims, American public spelling, only the 13 fully qualified accelerators on import with no generic or automatically discovered additions, Width 0 resolved with a 100-cell fallback, distinct BarWidth/Weight, numeric PowerShell durations and non-default switches. Markdown display/export stays in ConsoleMonster.Markdown.

The count applies to the 136-command round-1 surface. Commands introduced by other feature PRs need their own disposition against these rules; they are not silently included in this number.

## 1. What the inventory actually contains

The baseline includes the long-recording and synchronized-frame fixes from #56; it adds no cmdlets. The starting inventory is the compiled `Get-Command -Module ConsoleMonster -CommandType Cmdlet` surface, including inherited parameters, checked against the manifest and C# implementations. It contains the ten update commands and all the families listed below.

One correction to the starting survey matters: widgets already have a generated fluent API. The round-1 assembly contains **61 public `*Fluent` extension classes and 429 declared public static methods, of which 365 are `With*` methods**, before counting handwritten instance methods such as `TerminalProfile.WithMode`. `ConsoleMonster.Generators/FluentGenerator.cs` generates widget and reachable option methods; `FluentCoverageTests` and `ThreeStylesTests` already exercise coverage and rendering. The current three-styles page also includes `Ui` factories. I would extend and standardize this existing implementation.

The other important correction is `Update-ConsoleRun`: it addresses a **LiveRun handle**, not a widget ID in a dashboard. Folding its name into `Update-Console` is reasonable; pretending its lifetime and address are identical is not.

## 2. Consolidation and exact arithmetic

Each row below removes names only once. “Existing destination” means the destination is counted in another row or in the unchanged remainder.

| Family | Before → after | Decision and reason | Before | After | Reduction |
| --- | --- | --- | ---: | ---: | ---: |
| Typed live updates | Activity, Cell, Lane, LiveTable, Phase, Progress, Run, Spinner, StatusItem and SummaryBar `Update-Console*` → `Update-Console` | Accept, with a separate Run-handle parameter set and validated widget capabilities. | 10 | 1 | 9 |
| Other live value/content edits | `Set-ConsoleValue`, `Add-ConsoleValue`, `Set-ConsoleRegion` → `Update-Console` | Accept: set, increment, append and replace-content are update operations on an addressed target. | 3 | 0, existing destination | 3 |
| Passive item constructors | BarItem, KeyValueItem, StatusItem, TreeNode, LogEntry, ChartSeries and Lane `New-Console*` → parent pipeline rows | Accept after preserving every current item field and conversion in the parent. | 7 | 0 | 7 |
| Preset constructors | `New-ConsoleFormPreset`, `New-ConsoleMenuPreset` → parent `-Preset` records | Accept; both parents already have `-Preset`, but currently require constructed preset types. | 2 | 0 | 2 |
| Single/multiple input pairs | `New-ConsoleMultiSelect`, `Read-ConsoleMultiSelect` → corresponding Select `-Multiple` | Accept; the question/control stays the same operation and changes selection cardinality. | 2 | 0, existing destinations | 2 |
| App text input pair | `New-ConsoleTextArea` → `New-ConsoleTextInput -Multiline` | Accept; multiline-specific sizing belongs in that parameter set. | 1 | 0, existing destination | 1 |
| Text/secret question pair | `Read-ConsoleSecret` → `Read-ConsoleText -AsSecret` | Accept, with secure output by default and explicit restrictions on history/defaults. | 1 | 0, existing destination | 1 |
| Numeric chart constructors | `New-ConsoleBarChart`, `New-ConsoleLineChart`, `New-ConsoleHistogram`, `New-ConsoleSparkline` → `New-ConsoleChart -Type` | Accept; Bar, Stacked, Line, Scatter, Histogram and Sparkline become kinds. | 4 | 1 | 3 |
| Progress construction | `New-ConsoleSpinner` → `New-ConsoleProgress -Indeterminate` | Accept: known-total and unknown-total activity indicators share a construction operation. | 1 | 0, existing destination | 1 |
| Native progress submission | `Write-ConsoleProgress` → native `Write-Progress` inside `Use-ConsoleProgress` or explicit Progress integration | Remove the redundant ConsoleMonster spelling. The mainstream command and its native `-Id` remain intact. | 1 | 0 | 1 |
| Run wait/finalize | `Wait-ConsoleRun` → `Complete-ConsoleRun -Wait` | Accept for the normal wait-then-finalize workflow; non-finalizing waits become an explicit C# capability, discussed below. | 1 | 0, existing destination | 1 |
| Mutable table construction | `New-ConsoleLiveTable` → `New-ConsoleTable -Live` | Accept; configuring columns and initial rows is shared, with live identity/sort options in the Live set. | 1 | 0, existing destination | 1 |
| Display representations | Tree, Chart, Code, Json, Error, Markdown and Rule `Show-Console*` → `Show-Console -View` | Accept; keep Show-ConsoleTable/List as idiomatic shortcuts to the same shared implementation. Rule remains one line. | 7 | 0, existing destination | 7 |
| Layout-grid name clash | `New-ConsoleGrid` → `New-ConsoleLayoutGrid` | Rename the layout constructor so it is distinct from the interactive row viewer, Show-ConsoleGrid. | 1 | 1 | 0 |
| All other commands | Unchanged names; parity changes still apply | Retain 94 current names in total, including the surviving destinations above. | 94 | 94 | 0 |
| **Total** | **42 current names retired, three introduced** | **One coherent implementation PR after this proposal.** | **136** | **97** | **39** |

The first fourteen rows account for the 42 retired names, not 42 distinct jobs removed. The last row accounts for all surviving current names, avoiding double-counting destinations.

### 2.1 `Update-Console`: binding and failure behavior

Proposed common call:

```powershell
Update-Console build -Value 5 -Total 20 -Label 'Compile' -Status Running
Update-Console checks -Key dns -Status Pass -Detail '2 resolvers'
Update-Console totals -Value 12 -Total 20 -Counter @{ Passed = 10; Failed = 2 }
Update-Console results -Row ([pscustomobject] @{ Key = 'web01'; Values = @('web01', 'Pass', 42) })
Update-Console results -RemoveRow -Key web02
Update-Console outcomes -Index 2, 5 -Status Fail
Update-Console chart -Append -Value 18 -Key latency
Update-Console main -Content (New-ConsoleText 'Finished')
Update-Console -Run $run -Index 3 -Status Pass -Detail 'Uploaded'
```

`-WidgetId` is mandatory and position 0 in the widget sets. There is no universal default such as `progress`: that would be wrong for almost every other target. `-Session` keeps round 1's explicit/session-variable/current-session resolution. `-Run` is position 0 in its own set and cannot be combined with WidgetId or Session.

Use a small set of structural parameter sets rather than one set per widget:

| Set | Discriminator and payload | Scope |
| --- | --- | --- |
| Fields, default | WidgetId; optional Key, Status, Label, Value, Detail, Total, Increment, Result, Counter, Restart, Complete, ParentKey and RemainingSeconds | Scalar and keyed updates, checked against the resolved widget. Value is the absolute value; Increment is an additive change. |
| Append | Mandatory `-Append`, Value, optional Key | Streaming chart samples; append is different from increasing a counter. |
| Row | Mandatory `-Row`, WidgetId | One keyed table row; a pipeline may stream these records. |
| RemoveRow | Mandatory `-RemoveRow` and Key, WidgetId | Remove a keyed table row. |
| ClearRows | Mandatory `-ClearRows`, WidgetId | Clear a live table. |
| Cells | Mandatory Index and Status, WidgetId | Set outcome cells. |
| AppendCells | Mandatory `-AppendCells`, optional Status and Count, WidgetId | Append outcome cells. |
| ResizeCells | Mandatory CellCount, WidgetId | Resize the outcome grid. |
| Content | Mandatory Content, WidgetId | Replace a region's widget. |
| Run | Mandatory Run, Index and Status; optional Label, Detail and DurationSeconds | Update a caller-managed run item. Label replaces the current Phase spelling; Detail replaces Message. |

The cmdlet has one CLR declaration per parameter name. In particular, Index is an int array for cell batches; the Run set checks that it contains exactly one element before mapping to the C# run-item Index. That scalar/collection conversion is explicit in the parity fixture. Parameter sets cannot give the same parameter different CLR types or different ValidateCount attributes.

This is ten sets, below PowerShell's 32-set limit. Parameters unique to one set are declared only in that set. Shared scalar parameters are deliberately not advertised on structural sets where they have no meaning.

Static binding rejects combinations such as `-Row ... -Index 2` or `-Run $run -WidgetId build`. It cannot determine a widget's type from the string `build`. The core resolves the target through the existing `LiveSession.TryGetTarget`, checks the **entire bound update** against that target, then constructs events. It does not post half of an invalid update.

Examples of terminating errors:

- Unknown WidgetId: `ConsoleMonsterUnknownWidget`, naming the session's available targets.
- `Update-Console spinner -Total 100`: `ConsoleMonsterUpdateParameterNotSupported`, naming Spinner, Total and the supported fields.
- `Update-Console checklist -Value 5`: the same error, rather than an ignored value.
- Value plus Increment, incompatible Result/Status modes, a missing required Key, a nonnumeric progress value, or an invalid cell index: specific argument errors before enqueueing.
- An unsupported custom target: direct the extension author to the raw event API, rather than guessing an update schema.

Existing updates can ignore unknown operations and the PowerShell base warns once for an unknown target. That behavior is unsuitable for a universal command; this proposal intentionally makes the high-level update API strict. Raw event replay keeps its protocol behavior.

The capability table is explicit, not a reflection guess based on matching property names:

| Existing update target | Operations retained through the new command |
| --- | --- |
| ActivityList | Key (native activity ID), ParentKey, Label, Detail, Value as percent with Total 100, RemainingSeconds, Complete; native Write-Progress itself keeps its native names. |
| CellMap | Set indices/status, append Count cells/status, resize to CellCount. |
| Lanes | Key, Label, Total, Status, Detail, Result plus Increment for completed steps, Complete. |
| LiveTable | Keyed row upsert, remove and clear, preserving column presenters and typed values. |
| Phases | Key, Status and Detail. |
| ProgressBar | Value, Total, Increment, Label and Status. |
| LiveRun | Run handle, Index, Status, Label, Detail and DurationSeconds. |
| Spinner | Label, Detail, Status and Restart. |
| StatusList | Key, Label, Status and Detail. |
| SummaryBar | Value (done), Total and Counter. |
| KeyValueList / BarChart / StackedBar | Key and Value, using the target's actual value type and existing semantic updates. |
| Numeric sample charts | Append Value with an optional series Key, preserving order. |
| RegionWidget | Content replacement through the existing region operation. |

Implement validation and conversion once in the core, as `ConsoleUpdate.Apply(session, widgetId, WidgetUpdate)` and a Run overload. Both the cmdlet and normalized typed-handle methods call it. The request preserves field presence independently from its value: zero, false, empty text, explicit null and omission cannot collapse together. Property setters can maintain an internal presence mask, so C# initializers, fluent calls and PowerShell binding have the same semantics. No runtime property reflection or string-based C# setters.

Preserve existing live recording wire keys and operations. The new high-level update request translates to those events; it does not rename the recording protocol. Validation-before-posting does not promise transactional rendering across several queued events.

`Send-ConsoleEvent` remains an advanced protocol command for custom targets and recorded event work. It is not taught as another way to update built-in progress, tables or status lists. Its C# counterpart is `LiveSession.Post`; it does not bypass the new high-level validator by accident.

### 2.2 Parent records replace passive item commands

Parents take typed C# items, dictionaries where already meaningful, and `[pscustomobject]` rows with documented fields. A common PowerShell adapter reads properties and creates the existing core item types; it does not implement formatting or update rules. Conversion failures name the parent, row and property. Labels containing markup remain literal unless Markup or rich text is explicit.

For KeyValue, both reserved properties Key and Value identify an item descriptor, including an explicit null Value. An ordinary object or dictionary with only a Key property remains ordinary key/value data. Descriptor records reject unknown fields.

| Retired item command | Parent and proposed row schema | Behavior to retain |
| --- | --- | --- |
| `New-ConsoleBarItem` | Chart: Key, Label, Value, Role | Per-item roles, rich labels, stable keys and finite numeric validation. |
| `New-ConsoleChartSeries` | Line/Scatter chart: Key, Label, Values, Role | Several samples, explicit versus automatic role selection; normalize today's singular Value to Values on both surfaces. |
| `New-ConsoleKeyValueItem` | KeyValue: Key, Value, Status, Markup, Presenter, Unit | Typed values and presenters; a descriptor row must be distinguished from a dictionary of ordinary key/value data. |
| `New-ConsoleStatusItem` | StatusList/Phases: Key, Label, Status, Detail | The same keyed record works for either layout. |
| `New-ConsoleTreeNode` | Tree: Label, Children, Status, Detail, Value, Presenter, Unit | Recursive children, formatted node values and rich labels; Children replaces the item constructor's Content block. |
| `New-ConsoleLogEntry` | LogRegion: Message, Level, AtSeconds | Explicit timestamps and levels; plain strings still mean ordinary log lines. |
| `New-ConsoleLane` | Lanes: Key, Label, Total, Value, Status, Detail, Passed, Failed, Warned, Skipped, StartedSeconds, FinishedSeconds | Retain seeded result counters and clock values; Value replaces Done consistently with updates. |

These are proposed schemas, not a claim that every parent accepts them today. `PSItems` currently accepts some records but requires typed ChartSeries/LogEntry, and TreeNode has richer behavior than a bare string. All those paths must be completed before removing the constructors.

```powershell
@(
    [pscustomobject] @{ Key = 'dns'; Label = 'DNS'; Status = 'Pass'; Detail = '2 resolvers' }
    [pscustomobject] @{ Key = 'tls'; Label = 'TLS'; Status = 'Running'; Detail = 'Handshake' }
) | New-ConsoleStatusList -WidgetId checks | Show-Console
```

**Keep `New-ConsoleField` and `New-ConsoleMenuItem`.** They define validators, visibility/dependency rules, asynchronous choice loading, actions, form submission and navigation. Requiring a large object literal would hide executable behavior in an undocumented dictionary language. Improve their parameter sets instead: action, submenu, form, selection, page, review, separator and exit are mutually exclusive menu-item kinds. Keep `New-ConsoleMenuRunResult`, which carries typed figures and executable next actions, for the same reason.

### 2.3 Presets and input pairs

| Change | Proposed binding and output |
| --- | --- |
| Form/Menu preset constructors → `-Preset` | Parent accepts an array of records with Name, Description, Preview and Values/Form/Selections as applicable, or typed FormPreset/MenuPreset objects from C#. Preserve protected built-in names, previews, matching of choice values and multi-item menu selections. Read-ConsoleForm's existing `-Preset` continues to select a preset by name; documentation distinguishes definition from selection. |
| Select/MultiSelect → Select `-Multiple` | Single remains the default. Single uses a scalar Default/Value; Multiple accepts a collection and enables Minimum/Maximum. Reject multiple-only options without Multiple. Read-ConsoleSelect returns one answer or emits the selected answers individually in Multiple mode, matching today’s pipeline behavior; app controls keep that cardinality in their state. Normalize the multi-question Selected input to Default, matching the single-question input. |
| TextInput/TextArea → TextInput `-Multiline` | Height and multiline editing options belong only to the Multiline set. Key, Label, Value, Placeholder, MaxLength, focus and change callbacks are shared. |
| Text/Secret → Text `-AsSecret` | Plain text remains the default. The Secret set masks input and returns SecureString unless AsPlainText is explicit. Hidden, AllowEmpty and MinLength remain available; History, Default, PrefillDefault and Multiline are excluded from the Secret set. Validators must not leak the answer into history, summaries, diagnostics or recordings. |

The corresponding C# construction entry points take the same selection/text/progress options, including Multiple, Multiline and Indeterminate. Concrete Select/MultiSelect, TextInput/TextArea and ProgressBar/Spinner renderers may remain distinct implementation/result types; neither language needs a second public factory name to select them. Typed state/results must retain their scalar-versus-collection contract.

Keep Read-ConsoleNumber, Path and Confirm: they provide typed numeric validation, filesystem semantics and confirmation respectively. A universal Read command would require another nested type selector and a large unrelated option union. The retained minimum is six question entry points: Text (including secrets), Select (including multiple), Number, Path, Confirm and Form.

Question workflows acquire answers and return to a script; persistent application controls retain focus, events and shared mutable state until the screen closes. This distinction remains one sentence in both the prompt and app docs.

### 2.4 Charts: consolidate numeric kinds, keep the outcome grid

Use `New-ConsoleChart -Type Bar|Stacked|Line|Scatter|Histogram|Sparkline`. Type is mandatory, enum-completed and uses the same ChartType vocabulary in C#. Bins/Edges belong to histograms, series to line/scatter charts, thresholds to bars, and latest-value display to sparklines. Shared Role, Unit, WidgetId and column-format objects keep the same names and defaults.

A `-Type` enum value **does not select a static parameter set**. Use structural input sets (Samples, Series, KeyedItems), then reject type-incompatible bound options through the same core `Chart.Create(type, input, options)` validation used by C#. `Get-Help` and generated examples show a per-kind option table. Do not add dynamic parameters, dozens of kind/option combinations or silent fallbacks. Binder-level alternatives such as mandatory `-Bar`/`-Line` switches are viable, but I recommend the single Type vocabulary because C# and PowerShell should teach the same choice.

Keep `New-ConsoleCellMap`. Its values are outcome states, its layout is an outcome grid, and its update operation addresses cell indices. Calling it a histogram or numeric chart would erase a useful distinction just to reduce the count by one.

`Show-Console -View Chart -Type Bar` builds and displays the same chart model. The display selector is View; Type is the same chart-kind parameter as on the constructor and maps to Chart.Type. There is no second pipeline-chart implementation. Rename or collapse the current PipelineChartType enum where it duplicates ChartType; record any aggregation-only values separately.

### 2.5 Progress, runs, tables and display

The progress surface becomes **New-ConsoleProgress**, the shared **Update-Console**, and **Use-ConsoleProgress**. The first builds a known-total or indeterminate indicator; the second changes its state; the third scopes interception of native Write-Progress records. `Enable-ConsoleFormatting -Feature Progress` remains the explicitly installed session-wide version of that integration. Native Write-Progress keeps all native parameter names and behavior.

The run surface becomes **Invoke-ConsoleRun**, **Start-ConsoleRun**, **Complete-ConsoleRun**, plus shared Update-Console's Run set. Invoke owns execution and cleanup; Start returns a caller-managed handle without executing the caller's work; Complete finalizes and releases it. `Complete-ConsoleRun -Wait [-TimeoutSeconds n]` waits for all reported items before finalization. Timeout or cancellation must not silently finalize unfinished work. Complete without Wait retains explicit immediate finalization. The old non-finalizing Wait operation becomes a shared `LiveRun.Wait` operation in C# (new: today the PowerShell cmdlet polls Snapshot itself) for hosts coordinating several lifetimes; PowerShell can wait on its actual jobs/tasks when it needs a checkpoint without closing the run. If that checkpoint is a demonstrated scripting requirement, retain Wait-ConsoleRun and revise the target to 98 instead of disguising a behavior loss.

`New-ConsoleTable -Live` builds a keyed mutable table using the same column and row configuration as the ordinary table. It accepts explicit WidgetId, KeyProperty and live sorting/row-limit options. Avoid duplicated table options in two unrelated C# option objects: a live definition contains the same Table model and adds addressing/lifetime policy.

`Show-Console` owns automatic display and explicit View values **Auto, Table, List, Tree, Chart, Code, Json, Error, Markdown and Rule**. Structural sets distinguish ordinary data, supplied widgets, content/path input, error input and rules. View-specific options are validated before rendering, because a View enum value cannot do the binder's work. AsString and AsWidget remain mutually exclusive capture choices; export stays in Export-ConsoleFrame, with Markdown dispatched to the add-on. Keep Show-ConsoleTable and Show-ConsoleList because they mirror the Format-Table/Format-List habit. They set View Table/List on the same operation; Show-Console -View Table|List remains available. The parity map records these two intentional shortcuts explicitly.

```powershell
Show-Console 'Section' -View Rule
Show-Console ([ordered] @{ Computer = 'web01'; State = 'Ready' }) -View List
Get-Service | Show-ConsoleTable -Property Name, Status
Get-Content ./README.md -Raw | Show-Console -View Markdown
```

Rename New-ConsoleGrid to New-ConsoleLayoutGrid: its C# type is Layout.Grid, whereas Show-ConsoleGrid uses Interactive.Grid. This removes a genuine noun clash without reducing a real capability.

Keep Show-ConsoleObject (interactive object navigation), Show-ConsoleGrid (an ad hoc table workbench: filtering, sorting, hiding columns, inspecting rows and optional row selection, including browse-only OutputMode None), Show-ConsoleMenu/App (application loops), Show-ConsoleChanges (review/confirmation), Show-ConsoleHelp, Show-ConsoleOverview, Show-ConsoleReplay and the demo/presentation viewers. Their state, data acquisition or output contract is a different job from choosing a passive representation. Keep Show-ConsoleStatusLine for the common implicit in-place status line; it does not require constructing and owning a dashboard session.

The other retained families have different contracts:

| Family | Why it remains separate |
| --- | --- |
| Add-ConsoleItem versus parent pipelines | Add items to an already constructed, unowned definition versus construct a new definition. It is not the live update path. |
| Write-ConsoleLogEntry versus Watch-ConsoleLog | Append a structured entry to a dashboard versus follow and parse a file. |
| Watch-Console versus Watch-ConsoleLog | Re-evaluate a script's object view versus tail a file's new lines. |
| Compare-ConsoleObject / Compare-ConsoleText / Show-ConsoleChanges | Compute structured object changes, compute a line/text diff, or run interactive review and confirmation. |
| Invoke/Stop/Export-ConsoleRecording and Add-ConsoleRecordingMarker | Own a capture lifetime, request its stop, convert a saved recording, or mark its timeline. |
| Get/Show/Export-ConsoleDemo versus Show/Invoke-ConsolePresentation | Discover/run/extract embedded gallery programs versus present or record a typed talk script. |
| Format-ConsoleCommand / Out-ConsoleFormatCommand / Out-ConsoleDefault | Format a command invocation as text, consume native formatting records, or route the shell's output stream. |
| Set-ConsoleFormValue versus Update-Console | Set a form's keyed answer values versus enqueue changes to a live target or run. |
| Use-ConsoleTarget versus Invoke-ConsoleDashboard | Scope an output destination versus own a live dashboard and its work. |

I would not merge Rows/Columns/Stack into a generic New-ConsoleWidget command: weighted layout, fixed stacking and wrapping are real layout choices with different constraints. Likewise HeaderBar and FooterBar retain their distinct navigation/context and key-hint models. Count reduction is not a reason to hide every type behind one untyped factory.

## 3. PowerShell and C# parity

### Contract

1. Every exported cmdlet has one canonical C# type or operation family, recorded explicitly. Overloads for structural parameter sets are permitted; a second implementation of the behavior is not. Show-ConsoleTable and Show-ConsoleList are the two approved idiomatic shortcuts: their parity records identify ShortcutOf Show-Console with View Table or List and the same Job/entry point. They are public commands, not aliases or independent renderers.
2. Domain parameters use the C# property or argument name, comparing case-insensitively for PowerShell and normalizing only C#'s initial camel-case letter. Nested paths are explicit, such as Table.Caption or Chart.Type. Semantic renaming is a change to both surfaces.
3. Every public configurable property, collection input and relevant constructor/operation argument is reachable through PowerShell or has an individually recorded C#-only reason. Runtime state and rendering interfaces are classified, not mistaken for configuration.
4. Initializer and fluent configuration use those same properties. The fluent name is the property name; collections use the actual Add operation. Containers expose Content(widget) and Content(Action<ContentBuilder>) to mirror -Content script blocks. Show() and Export(path, …) terminate a chain through the same display/export operations as Show-Console and Export-ConsoleFrame, including add-on dispatch.
5. Preserve the previously agreed PowerShell usability boundaries: switches select non-default behavior, and numeric durations name their units. They require explicit mechanical mappings to positive C# booleans and TimeSpan; literal spelling identity cannot also hold for NoMouse/Mouse and TimeoutSeconds/Timeout.

I recommend that fifth rule as a narrow exception to literal name identity, rather than undoing the round-1 switch and duration decisions. A mapping must state its exact conversion; “PowerShell is different” is not an exception.

A public option can be reachable through a typed nested configuration parameter, such as Presentation or a column descriptor, without flattening every option onto every cmdlet. The nested property names and defaults must still match C#, the help must document construction, and the parity test must actually bind a representative value through that path. An untyped catch-all options bag does not establish reachability.

### Ownership of host integrations

Rendering, shaping, item conversion, chart construction and live-update validation stay in the core or the existing format add-ons. PowerShell-specific work stays in the PowerShell assembly: installing/restoring formatting shims, retrieving native help, running Pester and executing embedded PowerShell gallery programs require a runspace. Expose a small, documented C# host-service entry point over the existing implementation for those commands; do not make the core reference System.Management.Automation. A mapping to the cmdlet implementation class alone does not establish a reusable C# counterpart.

The parity map must name those concrete host services too, with their runspace requirements and available package/module assembly. Ordinary C# applications still use the dependency-free core and opt into Markdown/Graphics packages. This is an explicit host boundary, not an exception that allows unmatched cmdlets.

### Proposed mapping records

The parity map is a versioned build/test input, not a runtime registry or a new configuration engine. Each cmdlet/set record contains its canonical C# type/entry point and explicit parameter-to-member bindings. Separate records classify every public option that intentionally has no cmdlet parameter. The website consumes a validated, generated export of this same map for the demo translation notes in section 5; it does not load a test assembly or maintain another mapping table.

| PowerShell operation | Canonical C# owner/entry point | Representative exact bindings |
| --- | --- | --- |
| Update-Console, widget sets | Proposed ConsoleUpdate.Apply with WidgetUpdate | Session → session, WidgetId → widgetId; Status/Label/Value/Detail/Total → matching request properties. |
| Update-Console, Run | Proposed ConsoleUpdate.Apply with LiveRun and RunUpdate | Run → run, Index → Index, Status → Status, Detail → Detail; DurationSeconds → Duration is a named unit conversion. |
| New-ConsoleTable, including Live | Table construction and live definition | Title/Caption/Border → Table properties; WidgetId/KeyProperty → live definition; Property/Column binding is explicitly classified. |
| New-ConsoleChart | Proposed Chart.Create and shared chart options | Type → type; Role/Unit/Minimum/Maximum → properties with the same names. |
| New-ConsoleProgress | Proposed Progress.Create; ProgressBar or Spinner result | Indeterminate → kind selector; Label/Value/Total/Status → same-name configuration; NoElapsed → ShowElapsed inversion. |
| New-ConsolePanel | Panel | Content → renamed content constructor argument/property; Title/HeaderRight/Caption/Border → same-name properties. |
| New-ConsoleSelect / Read-ConsoleSelect | Shared selection definitions and prompt runner | Multiple → Multiple; Key/Label/Choices/Value or Default → same-name control/prompt options. |
| New-ConsoleTextInput | Shared text-input definition | Multiline → Multiline; Key/Label/Value/Placeholder/MaxLength/Height → same names. |
| Read-ConsoleText | Shared text/secret prompt definition and runner | AsSecret → AsSecret; Title/Description/Required/MinLength/MaxLength → matching properties. |
| New-ConsoleField / MenuItem | Field definitions / MenuItem | Key/Label/Required; Action/OnSubmit/OnChange and selector names align with the public C# delegates. |
| New-ConsoleForm / Menu | Form / Menu | Title/Description/Preset → same names; rows map to typed preset objects. |
| Show-ConsoleTable / Show-ConsoleList | Same view operation as Show-Console | ShortcutOf Show-Console; fixed View Table/List, same parameter bindings and one implementation. |
| Show-Console | Shared view construction and display operation family | InputObject, View, Title, Width, capture policy and nested view options; host-only routing is classified separately. |
| Export-ConsoleFrame / Recording | ConsoleExporter, MarkdownExporter or Graphics exporter according to Format | InputObject/Path/Destination and format-specific options have explicit operation/set bindings; the package choice is not a second serializer. |
| Remaining construction, lifecycle and integration commands | Their existing concrete core/add-on types or operation families | Expand this fixture across all 97 commands before enabling the gate; no unmatched remainder is allowed. |

A family mapping does not excuse arbitrary name differences. Show's Type addresses Chart.Type through an explicit nested path, preserving the same leaf name. Prefer exact shared names rather than inventing a second word for a nested option.

### Today's verified mismatches

These are present in round 1; several were intentionally left as idiomatic adapters. Round 2 must either align them or keep the precise conversion in the exception list.

| Current PowerShell | Current C# | Proposed disposition |
| --- | --- | --- |
| New-ConsolePanel Content/Widget | Panel.Child and constructor child | Content/content on both surfaces; pipeline binding feeds Content, removing the redundant Widget spelling. |
| New-ConsoleMenuItem Hotkey | MenuItem.HotkeyChar | Hotkey on both surfaces. |
| New-ConsoleMenuItem IconName | MenuItem.IconGlyph | IconName on both; keep Icon as arbitrary text. |
| New-ConsoleGrid / Show-ConsoleGrid | Layout.Grid / Interactive.Grid | Rename only the layout command to New-ConsoleLayoutGrid; bind both to fully qualified C# types in the map. |
| Menu item Action / OnSubmit / OnChange | Handler / FormSubmitted / SelectionChanged | Action / OnSubmit / OnChange on both; async callbacks are separately classified. |
| Menu item Submenu / Form / Page / Review | SubmenuDefinition / FormDefinition / PageFactory / ReviewFactory | Align the option names; keep callback types idiomatic to each host. |
| Menu item Badge / Detail / DisabledReason | BadgeSource / DetailSource / DisabledReasonSource | Same concept names; delegate or constant adaptation is explicit. |
| Menu item LoadChoices | ChoicesLoader | LoadChoices in both APIs. |
| Choices GroupBy / Describe / DisabledReason | Choice.Group / Description / DisabledReason and selector adapters | Align selector option names to GroupBy/Describe/DisabledReason; distinguish stored Choice.Description from the selector that computes it. |
| New-ConsoleChartSeries Value | ChartSeries.Values | Values in the record schema and C# collection. |
| New-ConsoleLane Done; Update-SummaryBar Done | Lane/SummaryBar done counters and different event fields | Public construction/update Value; preserve internal result accounting and wire fields. |
| Update-ConsoleRun Phase / Message | LiveRun.Started phase / Finished message | RunUpdate.Label / Detail are canonical for the unified update. |
| Start-ConsoleRun Trend | LiveRunOptions.ShowTrend | ShowTrend on both. |
| Start-ConsoleRun HeightPercent | LiveRunOptions.HeightFraction | Prefer HeightPercent as the public option in both; convert to a fraction only inside layout code. |
| Progress/Lanes Column | ProgressBar.Columns / Lanes.Columns | Columns in both APIs; ColumnFormat remains a different object. |
| Show-ConsoleTable Total; table builders NoHeaderSeparator | Table.ShowTotals / HeaderSeparator | ShowTotals; NoHeaderSeparator remains an explicit bool inversion. |
| App inputs Disabled | InputControl.Enabled | Disabled on both public surfaces, retaining false as the default; map to enabled state internally. |
| New-ConsoleColumnFormat AllowShrink | ColumnFormat.NeverShrink | Public AllowShrink on both surfaces; preserve never-shrink as internal state if useful. |
| NoLive, NoMouse, NoReview, NoPager, NoLegend, NoElapsed, NoLatest | Positive bool or Show* properties | Keep PowerShell opt-out switches and explicit inversion bindings; no default-true switches. |
| TimeoutSeconds, PollIntervalSeconds, ProgressIntervalSeconds, DurationSeconds, AtSeconds, StartedSeconds, FinishedSeconds | TimeSpan options/arguments without the unit suffix | Keep numeric PowerShell values and exact TimeSpan conversions, including fractional and zero values. |
| Show-ConsoleGrid OutputMode | Grid.Selection | OutputMode in C# too, retaining the native Out-GridView vocabulary in PowerShell. The None value is a browse-only workbench, not an answer prompt. |
| Show/Invoke-ConsolePresentation | ConsoleDemo.Present/Record and DemoOptions | Rename the presenter C# facade to ConsolePresentation; gallery samples retain the separate Demo noun. |
| Show-ConsoleReplay PlaybackMode / Paused | Selection between session replay and ReplayPlayer.Play; ReplayOptions.AutoPlay | Put PlaybackMode selection in one shared replay entry point; retain Paused as an explicit inversion of AutoPlay. |
| PowerShell ScriptBlock selectors/validators/actions | Typed Func/Action delegates and cancellation-aware async delegates | Same domain names; host conversion, scheduling and cancellation remain explicit. |
| Pipeline InputObject, Content blocks and Property selectors | Typed item sequences, constructor content and expression columns | Map host adaptation explicitly; normalize avoidable content/column name drift rather than allow every difference. |
| Show-Console/Show-ConsoleChart object shaping | ValueShaper, PipelineChart and view-specific builders | One shared representation factory; remove the duplicate display-only implementation path. |
| PowerShell preset constructors and parent Preset | Form.Presets / Menu.Presets and binding methods | Preset input maps to typed Presets collection; record singular/plural mapping explicitly or normalize the public input option. |
| StaticSnapshot on prompts/object inspection | Snapshot rendering through the C# prompt/view APIs | Classify as explicit capture policy, not a setter on every widget; retain only where the public snapshot workflow needs it. |
| AsString / AsWidget / PassThru | Rendering, ToText and ordinary C# return values | Explicit shell-output-policy exceptions; do not add these switches to every widget class. |

This list is not a claim that spelling alone proves parity. The reflected root inventory and the test below must expose missing options, different defaults and unsupported parameter combinations too.

### Configurable C# capabilities needing exposure or classification

| Current C# capability | Round-2 treatment |
| --- | --- |
| ProgressBar.ShowCount, LabelWidth, RateWindow | Expose NoCount, LabelWidth and RateWindowSeconds; record bool/time conversions. |
| Panel.BorderRole, padding sides and Expand | Expose the same names, with NoExpand for default-on Expand. |
| Table.TitleIndent, WrapHeaders, KeepAllColumns, GroupHeadersAsRows, RecordViewRows, ShowKeyField, ShowHiddenColumns, StableWidths and Presentation | Expose through table configuration and typed nested Presentation options; use opt-out switches where defaults are true. Do not silently claim -Property reaches these. |
| TableColumn.RoleRules, Aggregate, overflow and width policy | Expose through the existing column descriptor/configuration path, with exact documented field names. |
| LiveRunOptions.ShowGroups and Session | Expose NoGroups; allow an explicit typed Session configuration where meaningful instead of keeping a private PowerShell option subset. |
| Spinner.Started/Finished and Lane initial timing/counters | Preserve timed recording/fixture input; mark runtime-updated values separately from initial state. |
| ReplayOptions.FramesPerSecond, MaxFrames and Interactive | Expose sampling limits and ordinary interactive options through replay configuration, with host hooks individually classified. |
| DemoOptions.Jitter and nested Recording options | Expose Jitter; align presenter/recording options and document host Output callbacks separately. |
| AsyncHandler, native async validators/loaders and CancellationToken | C#-only where a PowerShell runspace cannot execute the callback safely; ordinary ScriptBlock equivalents must remain reachable. Document each actual member, not all delegates as one exception. |
| LiveSessionOptions.Output/Target, terminal targets and host output implementations | C#-only host integration where injecting a raw writer/terminal owner would conflict with the PowerShell host contract. Expose ordinary presentation/session options separately. |
| TerminalProfile policies, clocks, member sources and AOT selector expressions | C#-only host/strongly typed integration where appropriate; list exact properties and alternatives such as Get-ConsoleCapability, native selectors or New-ConsoleSimulatedClock. |
| Canvas primitives and custom IWidget/ILiveTarget implementations | C#-only extension interfaces; the deferred canvas cmdlet remains deferred. |
| Widget Measure/Render/Apply, render contexts, read-only snapshots and computed results | Runtime contracts, not public configuration; classify them explicitly and exclude them from option completeness. |
| Markdown/Graphics configurable export options | Include the actual bundled add-on types in parity scans; do not stop at the dependency-free core or move serialization back into it. |

### Reflection-based gate

Add a contract fixture such as `ApiParityMap` plus `ApiParityExceptions` in tests. Each binding records Command, Job, ParameterSet, Parameter, CSharpType, member/argument path, conversion, direction, expected default and nullability. Exceptions record the exact member or parameter, reason and alternative; no wildcard namespace exemptions or generic “advanced” bucket.

The test should:

1. Import the built module in a fresh process; read every exported command and every inherited parameter/set through Get-Command. Fail on missing/extra commands, aliases, unmapped parameters and duplicate declared Job identifiers except the two explicit ShortcutOf records sharing their parent operation. Reflection cannot infer semantic duplication; the job inventory and behavioral cases supply that contract. Assert the agreed target count and all 42 retired names absent.
2. Reflect the built core and relevant add-on assemblies. Resolve each member/overload/constructor in the map; fail on stale paths, ambiguous overloads, incorrect names/types, nullability/default changes and missing setters or collection inputs. Common PowerShell engine parameters are excluded by their declaring type, not by a broad name list.
3. Walk every public configurable option from explicit root types: widgets, prompt/form/menu/app definitions, view/export/session/run options, and add-on options. Follow nested public option types and collection items with a visited set, without the current generator's arbitrary three-level depth. Each configurable member must be reachable or individually classified C#-only. NoFluent excludes only a fluent setter; it does not exempt a public configuration member from PowerShell parity. Fail on orphaned or unused exceptions as well as missing coverage.
4. Check the small approved conversions: bool inversion, numeric unit → TimeSpan, enum discriminator, the single-index collection conversion, pipeline/ScriptBlock host adaptation, nested configuration and shell output policy. Exercise zero, fractions, omission, explicit null, empty collections and false where those alter behavior. Do not infer conversions from suffixes alone.
5. For every fluent-covered setter, compile and invoke the property-named extension, prove it changes the same property and returns the same receiver, and check collection additions preserve ordering/validation. Check derived receiver types and collisions with existing instance methods. Immutable value/profile copy methods have their own explicit expectations.
6. Compare initializer, fluent and PowerShell construction for representative widgets using real frames and semantic state; compare update event sequences/state for every supported target and rejected field class. Test both success and mismatched-parameter failures, including no partial enqueue.
7. Run metadata/binding proof under Windows PowerShell 5.1 and PowerShell 7; run core/fluent tests on supported frameworks and a NativeAOT consumer. Reflection belongs in the test runner only.

The map is not a permission slip to keep different names indefinitely. Renamed members must update the map and all consumers; exceptions are reviewed API decisions, checked for continued use.

## 4. C# fluent style

### Naming and mutation

Choose **property-named extension methods**: `.Title("Build")`, `.Border(BoxStyle.Rounded)`, `.HeaderRight("2 jobs")`; collections use their actual operations such as `.AddColumn(...)`, `.AddRow(...)` or `.Add(...)`. Remove generated With* configuration spellings in this breaking pass. Keep operations such as WithColor on immutable value objects only where they deliberately return a modified copy; document that boundary instead of pretending value objects mutate.

The spelling must translate mechanically: `-Title` ↔ `Title =` ↔ `.Title(...)`. For border configuration I recommend retaining the existing **Border** name in all three surfaces: `-Border Rounded` ↔ `Border = BoxStyle.Rounded` ↔ `.Border(BoxStyle.Rounded)` on Panel. This gives the requested one-to-one translation without another name change. Table uses the same property/method name with `TableBorder.Rounded`; Panel uses `BoxStyle.Rounded`. There is no existing universal BorderStyle enum, and the table's border model has capabilities beyond a panel's box glyphs. The mapping note must show the actual enum type for that widget. BorderStyle in only one of the three surfaces would violate the naming rule.

Constructor inputs and collection operations need equally explicit translations. Every container exposes `.Content(widget)` and `.Content(c => …)` alongside initializer configuration; pipeline rows become AddRow calls. The only non-literal parameter/property mappings are the classified host, unit, switch and structural conversions in section 3. For example, `-NoMouse` means `Mouse = false` and `.Mouse(false)`, and `-TimeoutSeconds 0.5` means `Timeout = TimeSpan.FromSeconds(0.5)` and `.Timeout(TimeSpan.FromSeconds(0.5))`. Show those conversions in the translation note rather than inventing NoMouse or TimeoutSeconds C# properties.

This matches the familiar method shape of [Spectre.Console's table API](https://spectreconsole.net/console/reference/table-border-reference/), including `.Border(TableBorder.Rounded)` and AddColumn/AddRow chains. ConsoleMonster keeps its own types, layout, ownership and rendering contracts. It does not acquire a Spectre dependency or copy a second builder model.

Public properties remain available for initializers and inspection. Property-named extensions can coexist with them: a compile probe confirms `new Widget().Title("Build").Title` resolves the extension call and property read. Still add compile coverage for inherited members, instance-method collisions, lambdas, null/default arguments and generic receivers.

**Language constraint recorded 2026-10-25:** delegate-valued properties use `.Configure(item => item.Action = callback)`. C# binds `.Action(...)` to invoking the property before considering an extension setter. Configure assigns callbacks without invoking them, returns the same receiver and preserves assignment order. The existing generator emits it once per relevant inheritance hierarchy; no wrapper object or compatibility setter is added. The parity map records this single fluent naming exception and generates the corresponding translation note.

Widgets and mutable option objects use **mutate-and-return-this**. That is the current model; immutable widget copies would duplicate collections, disturb live identity and make initializers and fluent calls mean different things. Configuration is for construction time. Once a live session owns a widget, use the update API instead of mutating its properties concurrently. Do not claim setters become thread-safe because they are fluent.

TerminalProfile and immutable style/value types retain copy semantics. Their fluent configuration must return the new value and leave the original unchanged. A future profile-specific naming change can be mechanical, but it must not change immutability just to match mutable widgets.

### Generation

Extend `ConsoleMonster.Generators`, not a new runtime builder package. Generate property setters and collection methods as ordinary compiled extension methods in the model's namespace, with inherited XML documentation. Reuse handwritten methods only for actual operations or multi-property invariants. The existing WithTotals helper, for example, sets ShowTotals and AutoTotals; replace it with a clearly documented Totals operation or expose the two canonical properties, rather than misrepresenting it as one setter.

Improve discovery and diagnostics: cover declared option roots and intended add-on models, remove the three-level traversal cutoff, preserve derived receiver types, and detect collisions by full signature. Keep NoFluent for members managed through invariant-preserving operations, with a reason checked by coverage tests. Do not emit duplicate methods for inherited properties or wrappers that merely forward old names.

The generator remains build-time only. Generated code does not discover properties through reflection, emit IL, compile expressions or require a generator in the consumer's application. Typed table expressions retain existing AOT constraints; this work must not turn expression-based convenience into a NativeAOT requirement. Publish a small consumer proving initializer/fluent construction and updates under trimming/AOT.

### Nesting and pipeline endings

Generate `.Content(IWidget)` and `.Content(Action<ContentBuilder>)` consistently for Panel, Rows, Columns, Stack, LayoutGrid and dashboard regions. Tabs use the same grammar when they land. The small typed ContentBuilder collects widgets; `c.Text("Ready")` adds a TextBlock, and nested containers use the same builder/content operations. Single-content containers accept one widget or compose several as Rows in source order; multi-child containers add the collected widgets in source order. An empty callback has a defined empty-content result. These are construction operations; an owned live region still changes through Update-Console. Keep validation and composition in core builders; the generator supplies typed methods, not duplicated behavior.

Parameterless Panel construction supports the approved chain. Object initializers and constructors remain available. Fluent method collisions with an existing Content property are compile-tested alongside inherited and generic container receivers. Do not generate both Action and Func lambda overloads with an ambiguous expression-lambda call.

`.Show()` terminates a widget chain through the canonical display operation, just as `| Show-Console` terminates a PowerShell pipeline. `.Export(path, …)` uses the same export operation and format dispatch as `| Export-ConsoleFrame`, including Markdown/Graphics add-ons and the clear missing-package errors. Keep these entry points thin, preserve explicit output/format options and return/error semantics, and test them through real output targets and exported files. No second serializer or renderer belongs in a fluent extension.

### The same panel in three styles

These are proposed signatures; the Content rename is part of this proposal. All three produce the same panel and visible text.

**PowerShell**

```powershell
New-ConsolePanel -Title 'Build' -HeaderRight '2 jobs' -Caption 'Ready' -Border Rounded -Content {
    New-ConsoleText 'All checks passed'
} | Show-Console
```

**C# object initializer**

```csharp
using ConsoleMonster;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

var panel = new Panel(content: new TextBlock("All checks passed")) {
    Title = "Build",
    HeaderRight = "2 jobs",
    Caption = "Ready",
    Border = BoxStyle.Rounded
};
Terminal.Write(panel);
```

**C# fluent**

```csharp
using ConsoleMonster;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

new Panel()
    .Title("Build")
    .HeaderRight("2 jobs")
    .Caption("Ready")
    .Border(BoxStyle.Rounded)
    .Content(c => c.Text("All checks passed"))
    .Show();
```

The same rule applies to tables: initializer properties Title/Border map to fluent Title/Border and PowerShell Title/Border; row additions use the shared table data model. Do not introduce a second FluentTable that must later convert back to Table.

## 5. Demos and documentation

**Every demo shows PowerShell | C# | C# fluent, in that order.** C# means constructors and object initializers; C# fluent uses the property-named methods above. All three are complete runnable programs showing the same data, widget structure and behavior. Seeing the same operation three ways is the translation guide: the reader can follow a cmdlet parameter into an initializer assignment and then into a call chain. Tabs keep the page compact without removing that comparison. This replaces the earlier recommendation to publish only one C# style per demo.

### Site switch and initial HTML

Extend the existing `data-cm-lang-switch` contract and `cm-lang` preference to three values: `powershell`, `csharp` and `csharp-fluent`. Existing csharp preferences select the initializer tab. Default to PowerShell when the value is missing, unknown or unreadable. Guard **both** localStorage reads and writes with try/catch, including the early head script which sets the choice before first paint. Storage failure leaves a working page-local choice. One selection synchronizes the switches on the page and is remembered on subsequent pages; it does not reload the page or reset the demo's playback state.

Render **all three source panes, their full programs and download links into the initial HTML**. No source tab is fetched or constructed from a network request after selection. The build-time renderer supplies the content for indexing; inactive panes are hidden visually/accessibly through the existing language mechanism. Without JavaScript, leave all three labelled sources readable. Extend the existing radio-group switch with three visibly tabbed options, retaining keyboard navigation, selected-state announcements and focus behavior. Ordinary ARIA tablists elsewhere on the site remain tablists; do not mix tab and radio roles within one control. Copy and Run it yourself act on the selected style and name the selected source file.

Apply this to gallery pages, embedded demos and the source-bearing demo layouts, not just the gallery index. The existing two-language docs wrapper must recognize complete triplets without mistaking an adjacent unrelated code block for the third style. General installation snippets that have no language-specific fluent operation can still show their relevant setup; every actual demo has all three sources. Site contract checks fail if a registered demo omits one, even if another page happens to show its missing style.

### Source files and Run it yourself

Each gallery folder contains `<slug>.ps1`, **Program.cs** (initializer) and **Program.Fluent.cs** (fluent), with one shared metadata/capture recipe. Keep both C# programs standalone: no repository helpers or hidden shared source that a reader must discover. Initializer style can use constructors, loops and behavioral Add methods; it avoids fluent configuration chains. Fluent style changes configuration syntax, not data, callbacks or lifetimes. Ui is the fluent entry point; every fluent tab starts widgets with Ui.<Noun>() and uses the same nouns inside Content builders. Initializer tabs retain constructors.

`CSharpRunner` already compiles a selected source file in isolation. Extend the gallery model to carry both C# paths and compile/run each independently with the same references, warning policy and inputs. A downloadable SDK project needs an explicit selection too: the normal Compile glob would otherwise compile two top-level programs. Supply a small gallery project with default compile items disabled, a default `DemoStyle=Initializer`, and exactly one Compile item selected by `DemoStyle=Initializer|Fluent`. Reject other values. The generated steps use `dotnet run` for the initializer and `dotnet run --property:DemoStyle=Fluent` for fluent, using the documented [MSBuild property option](https://learn.microsoft.com/dotnet/core/tools/dotnet-run#options); test those exact steps in fresh consumer projects, including switching back to the initializer after a fluent build. Package references use the ordinary published packages, including declared add-ons, with no reference to the gallery runner.

For readers starting from `dotnet new console`, the fluent tab's steps download Program.Fluent.cs **as Program.cs**, replacing the template program; the initializer tab downloads Program.cs as Program.cs. Those steps never leave both entry points in a project with default compile inclusion. The page preserves the downloadable file's actual name while explaining the destination. Preserve `[pscustomobject]` rows and UTF-8 BOM in PowerShell scripts. Scripted keys, checkpoints, frozen clocks and capture controls stay in the harness; no StaticSnapshot/Step parameters enter the copyable demos.

### Triplet checks and capture freshness

Replace the gallery's twin contract with a **triplet contract for every declared state**. Use identical fictional data, en-US culture, terminal size, theme, color/glyph tier, clock, delays, scripted keys and checkpoints across all three programs. Require each to reach its expected state; equal empty or prematurely terminated output must not pass. Run the declared width/theme variants as well, and retain real input, result and lifetime assertions where a frame cannot establish behavior.

The current gallery compares **rendered cells**, not raw process-output bytes. Preserve that comparison for text, colors, styles and geometry, and add byte equality of the deterministic emitted frame: use the existing export/render owner to serialize each selected state with the same UTF-8 encoding, newline policy and render profile. Compare PowerShell against initializer and PowerShell against fluent; these two comparisons establish three-way equality. Keep raw capture freshness and terminal-probe checks separately: ConPTY transcripts may contain host control sequences or timing which are not widget output. Do not remove meaningful cell differences or rewrite goldens to make unlike output compare equal. The acceptance artifact is a byte-identical deterministic terminal frame per state, with an explicit cell-level diagnostic when it fails.

Fingerprint both C# sources as well as the PowerShell source, metadata and harness/settings. A changed source or setting makes the corresponding generated assets stale, even when output looks unchanged. Regenerate through the site's capture/render commands and keep the freshness check mandatory. Store one verified presentation frame/replay per state after the three outputs agree; visitors do not need three copies of the same media. This adds one executable program per demo, not a separate hand-edited set of pictures.

### Generated how it maps note

Below the tabs, show a compact **How it maps** table for the widgets used by that demo, normally three to six useful rows. Generate it from the section 3 parity map and resolved C# symbols; the demo metadata may select member IDs and order, but may not supply alternate names or hand-written conversion strings. A simple panel's rows look like this:

| PowerShell | C# | C# fluent |
| --- | --- | --- |
| -Title 'Build' | Title = "Build" | .Title("Build") |
| -HeaderRight '2 jobs' | HeaderRight = "2 jobs" | .HeaderRight("2 jobs") |
| -Caption 'Ready' | Caption = "Ready" | .Caption("Ready") |
| -Border Rounded | Border = BoxStyle.Rounded | .Border(BoxStyle.Rounded) |

For defaults, NoX inversions, TimeSpan units, constructor arguments, pipeline row conversion and ScriptBlock/delegate callbacks, emit the actual mapping and a short conversion note from the classified conversion kind. Do not claim callbacks are interchangeable strings or show a scalar assignment for collection construction. Include the owning type where two widgets use the same parameter with different enum types. Values come from the demo's resolved assignments where supported; otherwise show a typed placeholder, never guess by matching parameter text.

The build validates the map against Get-Command and compiled public C#/fluent symbols before exporting a small documentation catalog. SiteFrames then combines that catalog with per-demo member selections to emit the note into initial HTML. Unknown IDs, stale symbols, unsupported conversions, a referenced member absent from the example, or a missing style fail the build. Reflection and Roslyn analysis stay in build/test tools. The core and shipped module do not depend on a website catalog. Tests must still construct and render the three real programs: generating both a table and an assertion from the same mapping cannot prove that the implementation honors it.

### Maintenance and estimate

The current gallery has **27 demos, 107 states and 2,039 lines across its 27 Program.cs files**: nine static demos and eighteen live demos. This is a lower bound for the merged implementation baseline; demos added by other PRs get the same three-source requirement. Adding the third style means at least **27 new C# files**, with both existing C# and PowerShell sources also receiving the API migration. Existing Program.cs files mix initializers and chains, so classify each before retaining it as initializer or moving its configuration into the fluent version.

| Work added by the third style | Estimate and maintenance rule |
| --- | --- |
| Initial authoring and review | Roughly 0.5–1 hour per static demo and 1–3 hours per live demo: **23–63 hours** for the existing 27, in addition to consolidation and the shared tooling. These are planning estimates, not measured conversion times. Recheck after two static and two live pilots. |
| Shared tooling | Allow roughly **3–5 working days** for source selection, triplet capture/diagnostics, three-choice site behavior, generated translation notes and consumer/run-instruction checks. This is one implementation cost, not a per-demo cost. |
| Runtime proof | 107 states × three styles = **321 style-state executions**, versus 214 today: **107 extra executions, a 50% increase in that portion of the work**. Two comparisons per state give 214 comparison edges. This does not predict a 50% increase in total website CI time; compilation caching, host lanes and other site work are separate. |
| Ongoing edits | A changed demo must keep all three runnable sources and its state recipe in agreement. A configuration rename is generated/mechanically fixable; a changed interaction or callback needs human review in both C# programs and PowerShell. New demos cannot land as a temporary pair. |

Use the existing Roslyn toolchain to generate the repetitive C# property-assignment ↔ property-call transformation where symbols resolve unambiguously. A bounded authoring tool can preserve data/loops and rewrite supported object construction, nested options and collection additions, then compile both outputs. Mark generated files and make regeneration deterministic where that transformation covers the whole example. Keep initializer source as the authoring input in that subset. Do not build a general C#-to-PowerShell transpiler: mutation after construction, branch-dependent configuration, callback closures, async lifetimes and invariant operations can require explicit authored code. Unsupported constructs produce a diagnostic and an authored pair, rather than a plausible-looking silent rewrite. Both forms remain complete downloadable programs.

The parity map generates naming and conversion notes and checks API reachability; it cannot infer that two loops, input handlers or run lifetimes behave alike. Triplet executions supply that proof. This limits routine repetition without reducing the three visible styles to unexecuted illustrative snippets.

Keep one **Three ways to write it** guide as a worked explanation of the translation, including the exact-name cases and the narrow conversions. Rewrite the current page's comparison of three C# styles accordingly. API reference pages show initializer and fluent forms side by side and link to the corresponding PowerShell operation. Regenerate gallery cards, download links, embedded demos, external help, manifest exports, command references and source-bound captures together.

## 6. Migration size and execution phases

This is a substantial API migration, not 39 deletions. A tracked-file scan of round 1 finds retired-name or With* call references in **24 gallery files, 75 other example files, 88 test files, 95 cmdlet files, 26 authored documentation files and 80 generated-help files**. Those are mechanical candidate files, not a promise to edit every match: historical before-inventories and immutable-style With* calls need classification. Another 138 files include core/add-on code, reference inputs and metadata. Generated API/site output is additional.

The work includes 42 retired cmdlet names, three new names, 365 compiled With* extension overloads to classify/rename, parent-record adaptation, a shared update validator, chart/view construction reuse, and a complete parity map. Section 5 adds at least 27 C# files and the triplet/site tooling; those costs are additional to the earlier matching-file scan. Menu/field callbacks, run lifetimes, secure input and live recording/replay are the higher-risk paths. No new production dependency is required.

Use one round-2 PR with logical commits, in this order:

1. **Contract inventory:** freeze the merged baseline, complete the mapping/exception records, confirm the 97-command disposition, add meaningful failing binding/parity cases and generator collision fixtures.
2. **Core operations:** implement shared update validation and chart/view factories using existing renderers, item models and live targets; align domain names in both languages. Keep recording wire keys stable.
3. **PowerShell consolidation:** implement static structural parameter sets, complete record/preset conversion, delete the 42 names, and remove the corresponding implementations rather than leaving wrappers. Preserve 5.1 binding behavior.
4. **Fluent normalization:** update the existing generator, handwritten invariant operations, core/add-on callers and consumer compile tests. Preserve object initializers and immutable value semantics.
5. **Examples and generated surfaces:** pilot two static and two live triplets, then migrate every demo to PowerShell, C# initializer and C# fluent. Extend the remembered three-choice switch and source downloads, generate How it maps notes from the validated parity map, and regenerate help, manifest, website command/API references, embedded demos and source-bound captures. Update the migration guide and the round-1 proposal with a link to this decision record, not a rewritten historical inventory.
6. **Stable-candidate proof:** core/add-on tests on net8/net10, native/framework lanes, en-US Pester on 5.1/7, secure-input and run-lifetime cases, exhaustive update-target validation, parity/generator/AOT consumers, every triplet state and full website CI. Check all three panes in initial HTML, keyboard/Copy/download behavior, cross-page persistence and blocked/corrupt storage fallback; run both downloaded C# variants in fresh projects. Review the consequential contracts once, fix the complete finding classes, then publish the consolidated candidate.

The release stays breaking: no compatibility aliases, obsolete methods or silent acceptance of old record fields. An incorrect row schema should produce an actionable error naming the replacement field.

## 7. Decisions — approved 2026-10-25

Przemysław approved this scope on **2026-10-25**, including the three amendments below. These replace the earlier proposal choices; implementation proceeds on feature/api-round2 in one PR, with no merge before the three-tab pages have been reviewed.

| Decision | Approved decision | Consequence |
| --- | --- | --- |
| Consolidation target | Adopt the 97-command plan above; judge actual jobs, not a round-number quota. | 39 fewer cmdlets, with an auditable disposition for every current name. |
| Run addressing | Allow `Update-Console -Run` as a structural exception to WidgetId. | Avoid inventing a fake global widget ID for caller-managed run lifetimes. |
| Non-finalizing run waits | Remove the cmdlet for the common wait/finalize path; add the shared C# Wait operation and retain ordinary PowerShell job waits. Keep Wait-ConsoleRun only if a concrete intermediate-checkpoint script needs it. | Target 97; retaining that distinct job would change it to 98. |
| Literal name identity versus idiomatic switches/durations | Keep the round-1 rules and the narrow explicit conversion list. | No default-true switches and no numeric values binding as TimeSpan ticks. |
| Fluent naming | Property-named extensions; mutable widgets return this, immutable values return copies. | Rename the existing generated With* surface and callers; do not add another fluent API. |
| Charts | One numeric chart command with Type and runtime validation; keep CellMap separate. | Honest type-specific errors and shared C# semantics without misleading static parameter-set claims. |
| Passive display commands — amendment 2, 2026-10-25 | Show-Console with View and structural input sets, plus Show-ConsoleTable/List as idiomatic shortcuts to the same implementation. | Seven fewer commands; Table/List preserve the common PowerShell typing habit through the same implementation. |
| Fluent nesting/endings — amendment 3, 2026-10-25 | Generate Content(widget) and Content(c => …) consistently for containers; expose Show() and Export(path, …) as shared pipeline endings. | Content builders cover Panel, Rows, Columns, Stack, LayoutGrid and dashboard regions; future Tabs follow the same grammar. Export uses the same core/add-on format dispatch. |
| Behavioral item commands | Keep Field, MenuItem and MenuRunResult; remove the seven passive constructors after record conversion is complete. | Preserve discoverable actions/validation instead of creating a callback dictionary language. |
| Demo styles — amendment 1, 2026-10-25 | PowerShell, C# initializer and C# fluent on every demo; three tabs with a remembered preference and PowerShell default. | At least 27 additional C# programs; mandatory triplet output checks and generated How it maps notes. This decision is settled; the estimates remain to be checked by the pilots. |

### Demo teaching decisions — 2026-10-26

The three tabs teach each style rather than repeat constructor syntax. Wherever the PowerShell composes widgets with `-Content { … }`, the fluent program uses `.Content(c => { … })`, including ordered `c.Rule`, `c.Text`, `c.Add` and nested containers. Data/content records are separate from widget composition; a plain text greeting and an interactive grid need no artificial layout block.

Both C# programs use the same named records, one literal item per line. A PowerShell `New-ConsoleTable -InputObject $rows -Property …` maps to `Table.For(rows)` in the initializer tab and `Ui.Table(rows)` in the fluent tab, plus the same typed column selectors, preserving presenters and original row values. The initializer tab keeps property assignment; the fluent tab uses generated setters. Boolean setters expose both a parameterless overload for true (`.Expand()`) and an explicit-value overload (`.Expand(false)`). The reflection fixture and generated translation notes record this grammar.

### Fluent entry point — decision, 2026-10-26

Przemysław made `Ui` the fluent entry point. It is no longer optional sugar. I keep constructors in the initializer tab and start fluent widgets with `Ui.<Noun>()`; nested content uses the same nouns on `ContentBuilder`. The 101-command count does not change.

| PowerShell | C# fluent | C# initializer |
| --- | --- | --- |
| New-Console<Noun> | Ui.<Noun>() | new <Noun>() |
| -Param value | .Param(value) | Param = value |
| -Expand | .Expand() | Expand = true |
| -Content { … } | .Content(c => …) | constructor children or Content assignment |
| Nested New-Console<Noun> | c.<Noun>(configured => …) or c.<Noun>(widget) | new <Noun>(…) |
| Show-Console / Export-ConsoleFrame -Path x | .Show() / .Export("x") | Terminal.Write(…) / exporter |

Factories start empty. `Ui.Panel(title, content)` and `Ui.Rows(children)` are removed; configuration belongs in `.Title(...)` and `.Content(...)`. Numeric charts use `Ui.Chart().Type(...)`, and progress uses `Ui.Progress().Indeterminate(...)`, keeping the same kind selectors as PowerShell. Selection and text controls likewise use Multiple and Multiline. Their existing typed options materialize through `Build()` and the shared factories; `Build<T>()` verifies a required concrete live handle. This preserves immutable control/command identities rather than making keys writable after registration. Callback-valued properties retain the recorded Configure exception.

The optional primary-input exceptions are explicit: `Ui.Text(text)` takes literal text, `Ui.Rule(title)` takes its title, `Ui.Table(rows)` takes one row collection, and factories for addressed live widgets accept their single WidgetId. Each corresponds to the same positional PowerShell input. Text markup is explicit (`Ui.Text().Content(markup).IsMarkup()`), so brackets in literal text stay literal. No factory accepts positional titles, child lists, layout sizes, colors, chart edges or other configuration. The parity map lists each exception and its native metadata position.

`ContentBuilder` covers every widget factory noun with a matching configuration lambda or configured instance; `c.Add(widget)` remains available for an existing handle. Definitions such as Chart, Progress, Select, TextInput, Button, Toggle and Binding are materialized once by the builder after configuration. Form/menu/action definitions, typed Read-* question renderers, generic option construction and the C# dashboard composition helper are recorded separately from display widgets. Field construction stays on the form/page builder; SimulatedClock is an operational clock, not a widget. C#'s interactive Grid factory maps to Show-ConsoleGrid's viewer job, since that cmdlet combines construction and execution.

Every fluent demo uses Ui and builders for composition, controls, options and session lifetimes. Ordinary C# data remains ordinary C#: record literals, collections, dates, style values, event records and exceptions are not widget factories. The source contract resolves constructions semantically and rejects direct widget/control/configuration construction rather than banning the `new` keyword inside data. The generated How it maps rows show the root factory and the matching nested builder noun, as well as Content and Boolean switch grammar. All three styles remain in the initial DOM and the triplet comparison remains byte-for-byte.

### Parallel PR ownership and added-command disposition

The 97-command target is the merged #55 baseline. I inspected the feature APIs without changing their branches. Their merged PRs own the feature behavior; this PR incorporates their shared round-2 command, fluent, parity and triplet contracts.

| Feature owner | Added surface → round-2 disposition | Follow-up owner |
| --- | --- | --- |
| #54 everyday views, merged | No added cmdlets; reuse Show-Console views and the same Table/List shortcuts. | Integrated here: this PR preserves the merged formatting adapters, registered views and gallery while applying the round-2 contracts. |
| #57 GIF slide exports, merged | No added cmdlets; GIF stays an Export-ConsoleFrame/Recording format, with the same fluent Export ending and add-on dispatch. | Integrated here: #57 owns GIF/pacing behavior; this PR maps the merged playback controls, numeric durations, typed Options and ChapterSpeeds into the parity catalog and generated fluent/reference surface. |
| #58 flow/TLS, merged | New-ConsoleFlow → keep the distinct flow constructor; Update-ConsoleFlow → Update-Console node/edge structural sets; New-ConsoleFlowNode/Edge/Branch → documented Nodes/Edges/Branches records on the parent. The three item commands only constructed passive records. | Integrated here: this PR preserves Flow validation/rendering and the TLS demo, and supplies the shared update validator, record binding, fluent names and three source styles. |
| #59 calendars, merged | New-ConsoleCalendar → keep; its existing View selects month/year, so no separate command per view. | #59 owns calendar behavior. This PR adds the shared object-adapter options, parity routes, generated fluent style and operations-calendar triplet. |
| #60 full-screen phase 2, merged | New-ConsoleCommand stays as a behavior-bearing asynchronous app action; New-ConsoleScrollView stays as a distinct scroll container, sharing ContainerCmdlet and Content(widget)/Content(builder). Its current separate Widget parameter folds into pipeline Content. Show-ConsoleApp -Command/-SelectionKey, New-ConsoleButton -Command and Show-ConsoleGrid -Command/-RefreshRow join the existing owners and parity map. Future Tabs use the same Content grammar. | #60 owns the interactions. This PR incorporates both constructors, shared command collections, ScrollView content grammar, row refresh and service-browser triplet. No edits are made to that branch. |

With all named feature PRs integrated, this branch is **144 → 101**: the original 97 plus Flow, Calendar, Command and ScrollView. The smaller surface comes from retiring 46 current names and introducing the three consolidated names. The new features do not retain separate passive item or update commands.

### Implementation clarification — 2026-10-25

I keep document export in `Export-ConsoleFrame`, including ordinary objects and registered views. The existing `Out-ConsoleDefault -Format` export branch duplicates that job, so I remove its `Format` and export-only `Width` parameters. `Out-ConsoleDefault` remains the shell output router. This changes no command count and adds no compatibility shim.

## 8. Complete command disposition

The following inventory is the count check for this proposal. A kept name can still receive parity changes described above. Retired names have no alias or obsolete replacement.

| Current command | Proposed disposition |
| --- | --- |
| `Add-ConsoleItem` | Keep. |
| `Add-ConsoleRecordingMarker` | Keep. |
| `Add-ConsoleValue` | `Update-Console`; retire this name. |
| `Assert-ConsoleOutput` | Keep. |
| `Clear-ConsoleScreen` | Keep. |
| `Compare-ConsoleObject` | Keep. |
| `Compare-ConsoleText` | Keep. |
| `Complete-ConsoleRun` | Keep. |
| `Copy-ConsoleOutput` | Keep. |
| `Disable-ConsoleFormatting` | Keep. |
| `Enable-ConsoleFormatting` | Keep. |
| `Export-ConsoleDemo` | Keep. |
| `Export-ConsoleFrame` | Keep. |
| `Export-ConsoleRecording` | Keep. |
| `Export-ConsoleTheme` | Keep. |
| `Format-ConsoleCommand` | Keep. |
| `Get-ConsoleCapability` | Keep. |
| `Get-ConsoleDemo` | Keep. |
| `Invoke-ConsoleDashboard` | Keep. |
| `Invoke-ConsolePester` | Keep. |
| `Invoke-ConsolePresentation` | Keep. |
| `Invoke-ConsoleRecording` | Keep. |
| `Invoke-ConsoleRun` | Keep. |
| `New-ConsoleActivityList` | Keep. |
| `New-ConsoleAlign` | Keep. |
| `New-ConsoleBarChart` | `New-ConsoleChart -Type`; retire this name. |
| `New-ConsoleBarItem` | `New-ConsoleChart pipeline records`; retire this name. |
| `New-ConsoleBinding` | Keep. |
| `New-ConsoleButton` | Keep. |
| `New-ConsoleCellMap` | Keep. |
| `New-ConsoleChartSeries` | `New-ConsoleChart pipeline records`; retire this name. |
| `New-ConsoleColumnFormat` | Keep. |
| `New-ConsoleColumns` | Keep. |
| `New-ConsoleDashboard` | Keep. |
| `New-ConsoleField` | Keep. |
| `New-ConsoleFooterBar` | Keep. |
| `New-ConsoleForm` | Keep. |
| `New-ConsoleFormPreset` | `New-ConsoleForm -Preset`; retire this name. |
| `New-ConsoleGrid` | `New-ConsoleLayoutGrid`; rename the layout constructor. |
| `New-ConsoleHeaderBar` | Keep. |
| `New-ConsoleHistogram` | `New-ConsoleChart -Type`; retire this name. |
| `New-ConsoleKeyValue` | Keep. |
| `New-ConsoleKeyValueItem` | `New-ConsoleKeyValue pipeline records`; retire this name. |
| `New-ConsoleLane` | `New-ConsoleLanes pipeline records`; retire this name. |
| `New-ConsoleLanes` | Keep. |
| `New-ConsoleLineChart` | `New-ConsoleChart -Type`; retire this name. |
| `New-ConsoleLiveTable` | `New-ConsoleTable -Live`; retire this name. |
| `New-ConsoleLogEntry` | `New-ConsoleLogRegion pipeline records`; retire this name. |
| `New-ConsoleLogRegion` | Keep. |
| `New-ConsoleLogo` | Keep. |
| `New-ConsoleMenu` | Keep. |
| `New-ConsoleMenuItem` | Keep. |
| `New-ConsoleMenuPreset` | `New-ConsoleMenu -Preset`; retire this name. |
| `New-ConsoleMenuRunResult` | Keep. |
| `New-ConsoleMultiSelect` | `New-ConsoleSelect -Multiple`; retire this name. |
| `New-ConsolePadding` | Keep. |
| `New-ConsolePanel` | Keep. |
| `New-ConsolePhases` | Keep. |
| `New-ConsoleProgress` | Keep. |
| `New-ConsoleRegion` | Keep. |
| `New-ConsoleRows` | Keep. |
| `New-ConsoleRule` | Keep. |
| `New-ConsoleSelect` | Keep. |
| `New-ConsoleSimulatedClock` | Keep. |
| `New-ConsoleSparkline` | `New-ConsoleChart -Type`; retire this name. |
| `New-ConsoleSpinner` | `New-ConsoleProgress -Indeterminate`; retire this name. |
| `New-ConsoleStack` | Keep. |
| `New-ConsoleStatusItem` | `New-ConsoleStatusList / New-ConsolePhases pipeline records`; retire this name. |
| `New-ConsoleStatusList` | Keep. |
| `New-ConsoleSummaryBar` | Keep. |
| `New-ConsoleTable` | Keep. |
| `New-ConsoleText` | Keep. |
| `New-ConsoleTextArea` | `New-ConsoleTextInput -Multiline`; retire this name. |
| `New-ConsoleTextInput` | Keep. |
| `New-ConsoleToggle` | Keep. |
| `New-ConsoleTree` | Keep. |
| `New-ConsoleTreeNode` | `New-ConsoleTree hierarchical records`; retire this name. |
| `Out-ConsoleDefault` | Keep. |
| `Out-ConsoleFormatCommand` | Keep. |
| `Read-ConsoleConfirm` | Keep. |
| `Read-ConsoleForm` | Keep. |
| `Read-ConsoleMultiSelect` | `Read-ConsoleSelect -Multiple`; retire this name. |
| `Read-ConsoleNumber` | Keep. |
| `Read-ConsolePath` | Keep. |
| `Read-ConsoleSecret` | `Read-ConsoleText -AsSecret`; retire this name. |
| `Read-ConsoleSelect` | Keep. |
| `Read-ConsoleText` | Keep. |
| `Register-ConsoleView` | Keep. |
| `Send-ConsoleEvent` | Keep. |
| `Set-ConsoleFormValue` | Keep. |
| `Set-ConsoleRegion` | `Update-Console`; retire this name. |
| `Set-ConsoleTheme` | Keep. |
| `Set-ConsoleValue` | `Update-Console`; retire this name. |
| `Show-Console` | Keep. |
| `Show-ConsoleApp` | Keep. |
| `Show-ConsoleChanges` | Keep. |
| `Show-ConsoleChart` | `Show-Console -View Chart`; retire this name. |
| `Show-ConsoleCode` | `Show-Console -View Code`; retire this name. |
| `Show-ConsoleDemo` | Keep. |
| `Show-ConsoleError` | `Show-Console -View Error`; retire this name. |
| `Show-ConsoleGrid` | Keep. |
| `Show-ConsoleHelp` | Keep. |
| `Show-ConsoleJson` | `Show-Console -View Json`; retire this name. |
| `Show-ConsoleList` | Keep as the approved idiomatic shortcut to the shared Show-Console view implementation. |
| `Show-ConsoleMarkdown` | `Show-Console -View Markdown`; retire this name. |
| `Show-ConsoleMenu` | Keep. |
| `Show-ConsoleObject` | Keep. |
| `Show-ConsoleOverview` | Keep. |
| `Show-ConsolePresentation` | Keep. |
| `Show-ConsoleReplay` | Keep. |
| `Show-ConsoleRule` | `Show-Console -View Rule`; retire this name. |
| `Show-ConsoleStatusLine` | Keep. |
| `Show-ConsoleTable` | Keep as the approved idiomatic shortcut to the shared Show-Console view implementation. |
| `Show-ConsoleTree` | `Show-Console -View Tree`; retire this name. |
| `Start-ConsoleRun` | Keep. |
| `Stop-ConsoleRecording` | Keep. |
| `Unregister-ConsoleView` | Keep. |
| `Update-ConsoleActivity` | `Update-Console`; retire this name. |
| `Update-ConsoleCell` | `Update-Console`; retire this name. |
| `Update-ConsoleLane` | `Update-Console`; retire this name. |
| `Update-ConsoleLiveTable` | `Update-Console`; retire this name. |
| `Update-ConsolePhase` | `Update-Console`; retire this name. |
| `Update-ConsoleProgress` | `Update-Console`; retire this name. |
| `Update-ConsoleRun` | `Update-Console`; retire this name. |
| `Update-ConsoleSpinner` | `Update-Console`; retire this name. |
| `Update-ConsoleStatusItem` | `Update-Console`; retire this name. |
| `Update-ConsoleSummaryBar` | `Update-Console`; retire this name. |
| `Use-ConsoleProgress` | Keep. |
| `Use-ConsoleTarget` | Keep. |
| `Wait-ConsoleDashboard` | Keep. |
| `Wait-ConsoleRun` | `Complete-ConsoleRun -Wait`; retire this name. |
| `Watch-Console` | Keep. |
| `Watch-ConsoleLog` | Keep. |
| `Write-ConsoleLogEntry` | Keep. |
| `Write-ConsoleProgress` | `Native Write-Progress through scoped/explicit integration`; retire this name. |
| `Write-ConsoleText` | Keep. |

**New names:** `Update-Console`, `New-ConsoleChart`, `New-ConsoleLayoutGrid`. **Kept current names:** 94. **Retired current names:** 42. **Original #55 baseline total:** 97; the merged feature integration totals 101.

## Sources and design constraints

- Current code: `ConsoleMonster.PowerShell`, `ConsoleMonster/Live/LiveSession.cs`, `ConsoleMonster/Interactive`, `ConsoleMonster/Widgets`, `ConsoleMonster.Generators/FluentGenerator.cs`, and `ConsoleMonster.Tests/Building`.
- [PowerShell parameter sets](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_parameter_sets): sets are distinguished by parameter combinations and are limited to 32. A Type/View enum's value does not choose a static set.
- [Spectre.Console table border API](https://spectreconsole.net/console/reference/table-border-reference/) and [table construction](https://spectreconsole.net/console/how-to/displaying-tabular-data/): familiar Border/AddColumn/AddRow method shapes, used here as an API comparison rather than a dependency or a rendering specification.

### Implementation record — 2026-10-25

The parity fixture is `Tests/Api/round2-parity.json`. It records each parameter’s member or argument, conversion, default and parameter-set metadata. The reverse scan follows nested configuration and item types without a depth limit. [The generated member decisions](Api-Parity.md) distinguish native host hooks, runtime models and equivalent representations; a namespace is never an exemption.

Export, form, menu and replay operations accept their shared C# configuration through `Options`. A PowerShell hashtable binds to that typed object. Mixing Options with individual settings is rejected, and the supplied request is not modified. Ordinary presentation controls remain named parameters, including progress-column records and numeric start/finish times for indeterminate progress.

LiveEvent remains a recording-protocol DTO: its integer timestamp argument is already milliseconds. `Send-ConsoleEvent -TimeMilliseconds` therefore maps directly to that wire value, with an explicit exception rather than a TimeSpan conversion. The recording keys stay unchanged. The unused New-ConsoleText Plain switch is removed; literal Text and explicit Markup already cover the two input jobs.

### Merged feature integration — 2026-10-25

I rebased onto master at `01da6e3f`, including #54 and #58. The expanded starting surface is **141**, and the round-2 result is **98**: the original 97 plus the distinct Flow constructor. This PR owns the round-2 migration of those merged features. I keep #54’s structured-data batching, row limit and native typed-view precedence in the shared C# output service.

Flow’s three passive item constructors become `Kind = Node | Edge | Branch` records accepted by New-ConsoleFlow and Add-ConsoleItem. The single branch uses the C# Branch property and generated `.Branch(…)` method. Update-ConsoleFlow becomes Update-Console’s FlowNode/FlowEdge sets, with NodeKey or EdgeKey, Status, and optional Cut. This preserves separate node/edge key spaces and rejects mismatched fields or unknown keys before enqueueing. Omitted Cut preserves connectivity; explicit false reconnects. Recording operation names stay unchanged.

The directly constructible C# prompt types are also parity roots, alongside question options. Their public configuration receives explicit parameter routes or individual C# host decisions; generic owner IDs use stable definitions rather than runtime assembly-qualified names.

### Host and consumer proof clarification — 2026-10-25

The generic-host and ILogger integrations keep their .NET lifetimes. Their PowerShell examples launch the same project; the parity record lists every integration option individually. Native formatting, help, progress and Pester examples use compiled PSCmdlet hosts and the same public services as the cmdlets, rather than a console program that starts another PowerShell implementation.

Custom RecordView sections and a fixed-scale data-bar presenter use the labelled direct-object form in PowerShell. Ordinary table examples use presenter names and pipeline records. This keeps the advanced C# composition available without adding another item or display cmdlet, or registering more type accelerators.

The packaged-template check compiles both gallery C# files against actual locally packed libraries in an isolated NuGet consumer cache. Generated reader projects keep their package references. The check covers all28 gallery demos, including the merged TLS verdict, and source-folder installation; it does not replace a reader's package references with project references. Native AOT proof covers nested Content and the Show/Export endings. Project-launcher tests pass an ordinary finite .NET project through the documented Project parameter, while the site's real captures exercise the generic host's service lifetime.


### Final merged-feature disposition — 2026-10-26

I rebased onto `dd5d7ce`, including #54, #57, #58, #59, #60, #62, #63 and #65. The current master exports **144** commands; this pass exports **101**, with no compatibility aliases or obsolete forwarding methods. The extra four jobs beyond the original 97 are explicit in this table.

| Family / before | After | Job and parity decision | Blast radius |
| --- | --- | --- | --- |
| Flow: New-ConsoleFlowNode / Edge / Branch, Update-ConsoleFlow | New-ConsoleFlow with ordinary node/edge/branch records; Update-Console for live changes | One flow builder and the common update operation; item keys and widget handles retain distinct names. | Core, cmdlets, TLS demo triplet, metadata and help |
| New-ConsoleCalendar | Keep; View Month / Year | A passive marked-date display. The host adapter shares Year, Month, member selectors and display options with C#; portable C# uses CalendarMark and Calendar directly. | Core/host parity, operations-calendar triplet, coming-from guides, tests/help |
| Read-ConsoleDate | No shipped command at this master; present only in the full-screen phase 3 plan | Date selection is a separate interactive job, not a calendar display alias. It does not contribute to either shipped count. | Proposal cross-reference; no compatibility shim or fabricated migration equivalent |
| New-ConsoleCommand | Keep | Defines one isolated asynchronous action, executed by an app, button or grid. ConsoleHostCommands.Create shares the PowerShell operation definition; plain C# uses AppCommand with a typed action. | Core/host parity, public host options, tests/help |
| Show-ConsoleGrid -Action / -Refresh; C# options.Commands | Show-ConsoleGrid -Command / -RefreshRow; C# GridOptions.Command / RefreshRow | The same action collection and row-refresh names across grids and apps. Script-block shorthand is a host adapter for the same AppCommand definitions. | Core, cmdlets, service-browser triplet, docs, tests/help |
| Show-ConsoleApp -Command / -SelectionKey; C# Commands / Selection | Shared Command collection and SelectionKey; native Selection delegate remains an explicit C# host choice | SelectionKey captures one row or a collection from state. Native dispatchers and custom selection delegates are documented C# host policies. | Core, cmdlets, metadata, callbacks and selection tests |
| New-ConsoleButton -Command | Keep, routed through AppInput.Button | A persistent control bound to the same action definition; its command label survives unless Label overrides it. | Core factory, cmdlet, regression tests/help |
| New-ConsoleScrollView -Content / -Widget | One Content parameter accepting pipeline values or a script block | One scroll container, collected once. C# uses Content(widget) or Content(builder), with the same Show/Export endings. | Core, cmdlet, AOT, content tests/help |
| Steps and key/value layouts | Keep on New-ConsolePhases / New-ConsoleKeyValue and New-ConsoleColumnFormat | Layout, number columns, spacing and value formatting configure their existing owners; they add no item constructors or display commands. | Parity map, samples, help and pipeline tests |
| Coming-from guides' calendar gap and old names | Calendar / New-ConsoleCalendar equivalents and the consolidated names | Describe the actual shipped display capability; date picking, Figlet fonts and command-line parsing retain their separate status. | Guide sources, executable examples and website |

The final parity catalog has **101 commands, 1,067 parameters and 293 reachable public types**. Read-only command collections translate as `-Command` ↔ `Command = { … }` ↔ `.AddCommand(…)`; the metadata test resolves the named fluent method. Both newly merged demos ship PowerShell, initializer and fluent sources, initial-DOM tabs, generated translation notes and triplet output checks.
