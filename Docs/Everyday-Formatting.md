# Everyday PowerShell formatting

`Enable-ConsoleFormatting` styles direct console output, including `Format-Table`, `Format-List` and `Format-Wide`. Use `-Exclude FormatCommands` to keep those commands native.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting

Get-Process | Format-Table Name, Id, CPU -AutoSize
Get-Process -Id $PID | Format-List Name, Id, Path
Get-ChildItem | Format-Wide Name -Column 3

Disable-ConsoleFormatting
```

Only output sent directly to a local console host uses ConsoleMonster. If another command follows in the pipeline, output is assigned to a variable, or output is redirected, the proxy delegates to the native formatter. Jobs, remoting, transcripts, CI and hosts without a local console also use PowerShell's output. An unrecognized output route or format shape uses the native renderer.

Wide output uses PowerShell's own column fitting on its evaluated records, preserving `-Column`, `-AutoSize`, grouping, spacing,
and the current runtime's ellipsis rules. ConsoleMonster's table padding and narrow-column hiding do not shorten Wide names.

```powershell
# These retain PowerShell's own formatting records and exact text output.
$records = Get-Process | Format-Table Name, Id
$text = Get-Process | Format-Table Name, Id | Out-String
Get-Process | Format-Table Name, Id | Out-File ./processes.txt
Get-Process | Format-Table Name, Id > ./processes.txt
```

The default enables Output, FormatCommands, Errors, Streams and ReadLine. Use `-Include Help` to add an opt-in, `-Exclude FormatCommands, Errors` to omit features, or `-Feature Streams, ReadLine` to select an exact minimal set. `Get-ConsoleFormatting` shows the current selection. Help, Prompt, Progress, GridView and compact errors remain explicit choices. `Disable-ConsoleFormatting` restores all enabled features; `Disable-ConsoleFormatting -Feature FormatCommands` restores just the format commands.

Custom functions are preserved unless `-Force` is supplied, then restored on disable or module removal. A custom alias named `Format-Table`, `Format-List` or `Format-Wide` prevents installation; remove it explicitly first. Aliases such as `ft`, `fl` and `fw` continue to resolve their normal command names.

## Compact host errors

The standard error view is enabled by default. Add `-CompactErrors` for the message-first layout; error-producing commands remain native.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting -CompactErrors

# At the prompt, errors use the compact view.
try { [int]::Parse('production') } catch { $_ | Show-Console -Compact -View Error }

# Include script and .NET stacks in subsequent host errors.
Enable-ConsoleFormatting -CompactErrors -Detailed
Disable-ConsoleFormatting -Feature Errors
```

The message comes first. Only its label uses the error tone; the invocation line uses the PowerShell highlighter and a
caret marks the failing expression. The script, line and column come from `InvocationInfo`. Category and target stay
visible; inner exceptions fold into a muted count. PowerShell 7 adds a muted `Get-Error for details` hint. `-Detailed`
includes stacks and causes. `Show-Console -View Error -Compact` provides the same view explicitly, including `-AsWidget`
and exports.

`$ErrorView` is never changed: disabling the feature returns display control to PowerShell;
`CategoryView` retains its native rendering even while Errors is enabled. `Get-Error` retains its native detailed view.
Records, `$Error`, `-ErrorVariable`, `2>`, `try`/`catch`, `$?` and `$ErrorActionPreference` keep PowerShell's behavior.
Pipelines, variables, files, jobs, remoting, CI, transcripts and unknown output routes retain native rendering.
An existing `Out-Default` function requires `-Force` and is restored exactly on disable or module removal.

The shared .NET view accepts structured invocation data from any host:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var error = new ExceptionInfo("FormatException", "The inventory number is invalid.") {
    SourceLine = "[int]::Parse('production')",
    ScriptPath = "deploy.ps1",
    LineNumber = 12,
    ColumnNumber = 1,
    ErrorLength = 26
};
Terminal.Write(new ExceptionView(error) { Compact = true, ShowStack = false });
```

| Guarantee | Proof |
| --- | --- |
| Captures and file bytes stay native | `ConsoleMonster.ErrorFormatting.Tests.ps1`: “keeps records, continuation, Out-String, Out-File and redirection byte-for-byte native”; the same snapshot runs in `Assert-ErrorFormattingHost.ps1` with an attached console. |
| Records, preferences and error accounting stay native | “keeps error records, ErrorVariable, preferences, catch and success status native”. |
| Jobs stay native | “leaves job output native when Errors is enabled inside the job”. |
| Explicit installation and exact restoration | “excludes Errors from All and prints its explicit Detailed profile snippet”; “restores the original Out-Default exactly on disable and module removal”. |
| Shared cells and transcript fallback at the actual prompt | `Assert-ErrorFormattingHost.ps1`, separate prompt pipelines on Windows PowerShell 5.1 and PowerShell 7. |
| Layout, source marker and tones | `CompactExceptionViewTests`: `CompactMessageInvocationAndFoldedCausesFitTheBudget`, `InvocationMarkerUsesTerminalCellsInsteadOfUtf16Offsets`, `CompactLabelUsesFailureToneAndProfileLanguage`. |
| Command identity, useful categories and fitted source | `CompactErrorContextTests`: `CommandHeader_UsesFailureToneAndHangsTheMessageUnderItsPrefix`, `CompactFacts_UsesMeaningfulCategoryOrTypePlusTarget`, `SourceWindow_DedentsAndElidesAroundTheFailingExtentWithAlignedCarets`. |

Compact errors preserve the failing expression from PowerShell's invocation extent. For a compiled command invoked
as `$x = @(& $command @arguments)`, the marker covers only `& $command @arguments`; the assignment and enclosing
parenthesis are not marked. Long messages wrap under the message text. A missing `InvocationInfo` omits the source
location and marker rather than adding blank position lines. The header identifies the failing command when
PowerShell supplies it; detached records and script-level throws use `Error:`. The facts line shows a meaningful
category and target, or the exception type when the category is `NotSpecified`. Source excerpts drop leading
indentation and use ellipses around the failing extent when the original line is wider than the console. File,
line and column coordinates still refer to the original source.

Repeated outer/inner messages appear once, with the inner exception's type in the folded count. When the cause has
a different message, a dim `cause:` line shows the innermost distinct reason. Runtime-shaped `at System.…()` lines
embedded in an exception's `Message` are counted and hidden in compact mode; other multiline prose is retained.
Detailed mode and the original error record retain the full text. `Get-Error` remains available on PowerShell 7;
on Windows PowerShell 5.1 inspect `$Error[0] | Format-List * -Force`.

`ConsoleMonster.ErrorShapes.Tests.ps1` covers six synthetic compiled-command, wrapped-cause, embedded-stack,
detached-record and mixed-output scenarios, with golden frames on both runtimes at 120/60 columns in light/dark.
The native comparison uses PowerShell 7's `ConciseView`. Windows PowerShell 5.1 has no `ConciseView`, so its comparison
uses the actual native `NormalView`. Non-terminating `WriteError` records remain in order with normal output.

Process aborts are outside this feature. Text such as `Unhandled exception. System.…`, followed by exit code 134
or `0xe0434352`, comes from the .NET runtime while the process dies. No in-process formatter can restyle that output.

To regenerate the native and compact review frames through the site's console runner:

```powershell
dotnet run --project Website/tools/SiteFrames -- review-error-shapes Website ./artifacts/error-shapes
```

The output includes a `review-index.md` pairing native and compact HTML exports, plus SVG, text and original ANSI
captures for each of the six shapes and eight runtime/width/theme combinations.

## Native stream colors

Use `Streams` to give PowerShell's own messages the active theme's warning, information, muted and failure tones. Choose the theme before enabling it, or re-enable `Streams` to apply a new theme.

```powershell
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Include Streams
Write-Warning 'Disk space is low'
Write-Verbose 'Checking backup' -Verbose
Write-Debug 'Using the local cache' -Debug

# Prints the corresponding profile lines; does not change the profile or settings.
Enable-ConsoleFormatting -Include Streams -ProfileSnippet
Disable-ConsoleFormatting -Feature Streams
```

PowerShell 7.2 and later use `$PSStyle.Formatting.Warning`, `.Verbose`, `.Debug` and `.Error`. Windows PowerShell 5.1 uses the host's available `$Host.PrivateData` warning, verbose, debug and progress foreground/background colors. Theme tones with a transparent background use the host's current background. Hosts with missing or unwritable properties are left alone; this includes headless and remoting hosts. PowerShell controls when colors appear, including its own redirected-output and transcript rules.

`Write-Warning`, `Write-Verbose`, `Write-Debug`, `Write-Information` and `Write-Progress` remain native cmdlets with Streams. Their records, preferences, `3>`, `-WarningVariable`, compiled-cmdlet warnings and transcripts retain PowerShell's behavior. The `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes remain. Use `Show-ConsoleStatusLine` or `Write-ConsoleLogEntry` for rich messages in your own scripts.

The original values are saved once for each native object and property while any installation owns it. If runspaces share a native preference, the most recently applied installation supplies its color. Disabling or removing that owner restores the latest surviving installation's color; removing an older owner leaves the current one active. The last owner restores the exact original value, including after user edits. Independent host objects retain their own baselines. Other settings are untouched. Streams alone does not install an `Out-Default` function, and it is included in the default set.

## Command-line editor and native progress

`ReadLine` applies the active theme to the already loaded PSReadLine editor: commands, parameters, strings, variables, numbers, operators, comments, keywords, types, errors, selected text and available inline predictions. It uses the same syntax roles as ConsoleMonster's code highlighter, a contrasting selection background and muted prediction text. It is included in `All`. If PSReadLine is absent, the feature does nothing; it does not import the module or enable predictions. The terminal profile controls colour depth; explicit no-colour or plain-text settings keep the editor's colours unchanged. Closing or disposing an owning runspace releases its editor and native stream/progress colours, restoring the latest surviving owner or the original baseline.

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Include ReadLine, Streams

# Type commands and select text in the normal PowerShell editor.
Write-Progress -Id 1 -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42
Write-Progress -Id 1 -Activity 'Backup' -Completed

Enable-ConsoleFormatting -Include ReadLine, Streams -ProfileSnippet
Disable-ConsoleFormatting -Feature ReadLine, Streams
```

`Streams` also sets `$PSStyle.Progress.Style` on PowerShell 7.2 and later, keeping `View`, width and other progress preferences as you set them. On Windows PowerShell 5.1 it sets the host's available progress foreground/background colors. Unsupported or unwritable properties are skipped. `Write-Progress` keeps native records, preferences and completion behavior. The separate `Progress` feature installs the native-metadata `Write-Progress` function adapter; selecting only `ReadLine` and `Streams` installs neither that function adapter nor an `Out-Default` function.

Re-enable either feature after choosing a new theme. Reapplying makes that installation the current owner of its native colors. Shared PSReadLine options follow the same ownership order as native stream and progress colors, retaining the first baseline until the last owner disables or removes the module. Editing mode, key bindings, prediction source and prediction view stay yours. Features can be disabled independently. The profile snippet only prints configuration lines.

For a .NET host, theme colors come from the same shared theme and encoder. Installing native PowerShell stream preferences belongs to its PowerShell runspace.

```csharp
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

var warning = Terminal.Theme.Resolve(ThemeToken.Warn, ColorDepth.TrueColor);
string warningSgr = SgrEncoder.Encode(warning, ColorDepth.TrueColor);
```

## Out-GridView where the desktop grid is unavailable

`GridView` is an explicit opt-in:

```powershell
Enable-ConsoleFormatting -Include GridView
Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
Disable-ConsoleFormatting -Feature GridView
```

A working native `Out-GridView` stays in charge, including on Windows PowerShell 5.1. The feature checks command availability and explicit SSH, remoting and desktop-less Windows contexts; it does not assume every PowerShell 7 session lacks the desktop command. The Microsoft desktop cmdlet is unavailable on non-Windows hosts even when its stub appears in `Get-Command`; third-party implementations keep their own availability rules. When the command is unavailable, a session function delegates to the existing `Show-ConsoleGrid`. No dependency is installed.

The fallback maps `-Title`, `-PassThru` (multiple selection), and `-OutputMode None|Single|Multiple`. Without an output option it browses and returns nothing. A terminal grid runs synchronously; both ordinary browse mode and `-Wait` return when you close it. It cannot leave a separate window running after the command ends. `-Wait`, `-PassThru` and `-OutputMode` retain their mutually exclusive native parameter sets.

The grid keeps its normal keys: type to filter, arrows to move, Space to mark rows, F10 or Ctrl+Enter to accept, and Esc to cancel. Selection returns the original objects, so assignment, `-OutVariable` and a downstream command receive objects rather than screen text. A working native command's pipeline, file, job and transcript behavior is unchanged. Without interactivity the existing `Show-ConsoleGrid` fallback prints a table with a warning and returns no selections. Closing the terminal grid restores its caller's Windows output mode, including when capability detection enabled VT for the first time, the user cancels, or the grid fails.

Existing functions and aliases are kept unless you pass `-Force` while the native grid is unavailable. Constants, `AllScope` aliases and caller-local aliases that cannot be replaced safely are kept with a warning, even with `-Force`; remove or rename them in their defining scope first if you want the fallback. Installation verifies that callers can actually resolve the fallback before marking it enabled. Disable or module removal restores the saved function and alias, including their options and descriptions. A command you replace after enabling belongs to you and is kept. `-Force` never replaces a working native grid.

For a .NET application the same renderer is `ConsoleMonster.Interactive.Grid`:

```csharp
using ConsoleMonster.Interactive;

Grid grid = new Grid("Choose nodes") { OutputMode = GridSelection.Single };
grid.AddColumn("Name").AddColumn("Region");
grid.AddRow("api-east", "api-east", "eu-west");
GridResult selected = grid.Show();
```

## Parameter mapping

The proxies use the parameter metadata of the current PowerShell runtime. On the direct-host route they retain the native property expressions' raw values while the native formatter supplies labels, grouping and layout records. Those values pass through the same shaping budgets and presenters as `Show-ConsoleTable` and `Show-ConsoleList`: status glyphs and tones, byte units, dates, durations and joined collections. Calculated expressions and getters are read once. Conditional table views keep their selected row expressions.

An explicit `FormatString` keeps the native cell text. Known status values still receive their status glyph and tone. Native views with custom controls or unsupported conditional list layouts keep native text and status tones rather than approximating their controls.

PowerShell's public view metadata can omit custom group headers. Named views with built-in grouping retain their native cell text and group renderer; an explicit `-Property` or `-GroupBy` selection uses the captured-value route. Views with scripted row selection also retain native cell text so PowerShell owns the selector's evaluation sequence. Type-based row selection uses captured values.

| Parameter | Console host rendering |
| --- | --- |
| `-Property` | Names, wildcards and calculated properties select columns, list fields or wide values. Labels, format strings, alignment and declared widths retain native semantics. Table/list cells use raw evaluated values through the shared presenters. |
| `-GroupBy` | PowerShell's group labels head each group, in native order. |
| `-AutoSize` | Native values and width hints feed ConsoleMonster's measured layout; wide output calculates how many values fit. |
| `-Wrap` | Table cells show all wrapped lines; without it they truncate to one line. |
| `-HideTableHeaders` | Suppresses the table's header row. |
| `-View` | Selects PowerShell's named table, list or wide view. Custom controls and page decorations retain the native rendering route. |
| `-Column` | Fixes the number of wide columns. |
| `-Expand`, `-Force`, `-ShowError`, `-DisplayError` | PowerShell evaluates these before rendering. |
| `-RepeatHeader` | Accepted and delegated to PowerShell; print-once ConsoleMonster tables show one header for each group. |
| Common parameters | Native binding and stream behavior are retained. `-OutVariable` and `-PipelineVariable` keep the native route. |

The direct-host routing check supports PowerShell 5.1 and 7. It reads the engine's downstream pipe and transcription state conservatively because the public invocation length alone cannot distinguish assignment from host output.

## Everyday built-in views

Member tables show `TypeName: ...` above each type. Within a type, full-width section rows separate methods, properties and events. Labels use the accent colour, followed by a subdued count, such as `Property · 1`. Header column lines stop above a section row without stubs and resume on the next data row, including at narrow widths.

Enable default formatting to use the registered views for members, commands, modules and history. Add `FormatCommands` when you also want ConsoleMonster to render explicit `Format-Table` and `Format-List` commands at the host.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting

'synthetic' | Get-Member -Name Length, Substring
Get-Command Get-Date, Get-Item
Get-Module
Get-History -Count 5

# Explicit selections still choose their own columns, labels and values.
Get-Command Get-Date | Format-Table Name, @{ Label = 'Origin'; Expression = { $_.Source } }
Disable-ConsoleFormatting
```

The default member view groups by `MemberType`. Signatures keep their complete text, with types muted and method names highlighted. Commands show a bracketed command-type chip and alias targets, and larger results group by module. Modules show their name, version, module type, exported command count and shortened path. Multiple versions share a name in the group header. Paths retain the user or system location, with `~` for home. History shows the id, start time, elapsed duration, execution status and highlighted, truncated PowerShell command text. The objects and their properties remain unchanged.

With `FormatCommands`, PowerShell continues to choose its native columns and labels. For example, a native module table keeps `ExportedCommands` rather than acquiring the default ConsoleMonster view's `ModuleType`, `Commands` and `Path` columns. The built-in presentation decorates those native-selected fields and supplies member/module grouping when no explicit properties or named view were requested. Explicit `-Property`, `-GroupBy`, calculated labels and `FormatString` values remain authoritative. A wildcard matching no properties keeps PowerShell's native empty shape. Wide output keeps its existing native layout.

`Register-ConsoleView` can override any built-in registration. `-NoBuiltIn` excludes built-ins from default output. Disabling formatting restores native display; piped, assigned, redirected, recorded formatting records, jobs and transcripts retain native output while formatting is enabled.

For .NET programs, use the same shared presenters on ordinary widgets:

```csharp
using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

var commands = new Table { RecordViewRows = 0 };
commands.AddColumn(new TableColumn("CommandType") { Presenter = CellPresenters.Chip });
commands.AddColumn("Name");
commands.AddValues("Function", "Get-DemoItem");
Terminal.Write(commands);

var members = new Table { RecordViewRows = 0 };
members.AddColumn("Name");
members.AddColumn(new TableColumn("Definition") { Presenter = CellPresenters.MemberSignature });
members.AddValues("Substring", "string Substring(int startIndex)");
Terminal.Write(members);
```

## Export a native view explicitly

Use the same adapter independently of session formatting when you need a widget for export or composition.
Already-created native format records contain strings, so this explicit record-only route retains their cell text and status tones. Use `$rows | Export-ConsoleFrame -Format Markdown` for automatic display, or `$rows | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Markdown` for an explicit table with its full value presenters.

```powershell
Get-Process |
    Microsoft.PowerShell.Utility\Format-Table Name, Id |
    Out-ConsoleFormatCommand -AsWidget |
    Export-ConsoleFrame -Path ./processes.html
```

For .NET callers, the reusable widgets and frame exporter are the normal entry points; PowerShell command interception is specific to a PowerShell runspace.

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;

var table = new Table { RecordViewRows = 0 };
table.AddColumn("Name").AddColumn("Id");
table.AddValues("pwsh", 1234);
ConsoleExporter.Save(table, "processes.html");
```

See [module views](Module-Views.md), [output exports and clipboard](Output-Exports.md), and [terminal links](Hyperlinks.md).


## Optional shell prompt

Custom prompts with dynamic parameters, a process block or pipeline input keep their native function and binding, including every input record and begin/end behaviour. Enabling the feature reports that the existing prompt was kept.

`Enable-ConsoleFormatting -Include Prompt` separately opts into the shortened path, cached git branch and dirty mark, last-command status and completed-history duration above two seconds. It is not enabled by default. `-ProfileSnippet` prints the opt-in profile lines. oh-my-posh and Starship require `-Force`. Disable or module removal restores the original prompt, including its closure scope; later user replacements are kept. Captures, transcripts, recordings, remoting and nested prompts use the original prompt. PSReadLine handlers stay untouched. See [the guide](https://consolemonster.dev/docs/help-and-defaults/#minimal-shell-prompt) for cache timing, examples and renderer frames.

## JSON, CSV and discovery output

`Enable-ConsoleFormatting` also displays JSON/REST and CSV `PSCustomObject` records, command metadata, modules, history and network replies. The [everyday PowerShell guide](../Website/content/guides/everyday-powershell.md) covers the columns, before/after examples, truncation limits and platform availability. JSON/CSV display limits do not truncate captured objects or exported files. Windows networking views register only on Windows; PowerShell 7 ping and .NET DNS/address views work across platforms.
