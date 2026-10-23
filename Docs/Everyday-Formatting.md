# Everyday PowerShell formatting

`Format-Table`, `Format-List` and `Format-Wide` use PowerShell's formatting unless you explicitly enable `FormatCommands`.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting -Feature FormatCommands

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

`-Feature All` enables default object views, errors, progress and native stream colors. It deliberately excludes `FormatCommands`: overriding commands used in scripts requires a separate opt-in. Features add up, so use `Enable-ConsoleFormatting -Feature All, FormatCommands` when you want both. `Disable-ConsoleFormatting` removes all installed features. `Disable-ConsoleFormatting -Feature FormatCommands` restores only the three formatter functions; explicit `-Feature All` retains the separate `FormatCommands` opt-in.

Custom functions are preserved unless `-Force` is supplied, then restored on disable or module removal. A custom alias named `Format-Table`, `Format-List` or `Format-Wide` prevents installation; remove it explicitly first. Aliases such as `ft`, `fl` and `fw` continue to resolve their normal command names.

## Compact host errors

`Errors` is a separate opt-in for a message-first error view. It is excluded from `All` because changing an everyday
error display should be an explicit session choice. Error-producing commands remain native.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting -Feature Errors

# At the prompt, errors use the compact view.
try { [int]::Parse('production') } catch { $_ | Format-ConsoleError -Compact }

# Include script and .NET stacks in subsequent host errors.
Enable-ConsoleFormatting -Feature Errors -Detailed
Disable-ConsoleFormatting -Feature Errors
```

The message comes first. Only its label uses the error tone; the invocation line uses the PowerShell highlighter and a
caret marks the failing expression. The script, line and column come from `InvocationInfo`. Category and target stay
visible; inner exceptions fold into a muted count. PowerShell 7 adds a muted `Get-Error for details` hint. `-Detailed`
includes stacks and causes. `Format-ConsoleError -Compact` provides the same view explicitly, including `-AsWidget`
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
| Layout, source marker and tones | `CompactExceptionViewTests`: `CompactMessageInvocationAndFoldedCausesFitTheBudget`, `InvocationMarkerUsesTerminalCellsInsteadOfUtf16Offsets`, `CompactLabelAloneUsesFailureToneAndProfileLanguage`. |

Compact errors preserve the failing expression from PowerShell's invocation extent. For a compiled command invoked
as `$x = @(& $command @arguments)`, the marker covers only `& $command @arguments`; the assignment and enclosing
parenthesis are not marked. Long messages wrap under the message text. A missing `InvocationInfo` omits the source
location and marker rather than adding blank position lines.

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
Set-ConsoleTheme -Name Light
Enable-ConsoleFormatting -Feature Streams
Write-Warning 'Disk space is low'
Write-Verbose 'Checking backup' -Verbose
Write-Debug 'Using the local cache' -Debug

# Prints the corresponding profile lines; does not change the profile or settings.
Enable-ConsoleFormatting -Feature Streams -ProfileSnippet
Disable-ConsoleFormatting -Feature Streams
```

PowerShell 7.2 and later use `$PSStyle.Formatting.Warning`, `.Verbose`, `.Debug` and `.Error`. Windows PowerShell 5.1 uses the host's available `$Host.PrivateData` warning, verbose and debug foreground/background colors. Theme tones with a transparent background use the host's current background. Hosts with missing or unwritable properties are left alone; this includes headless and remoting hosts. PowerShell controls when colors appear, including its own redirected-output and transcript rules.

`Write-Warning`, `Write-Verbose`, `Write-Debug` and `Write-Information` remain native cmdlets. Their records, preferences, `3>`, `-WarningVariable`, compiled-cmdlet warnings and transcripts retain PowerShell's behavior. The `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes remain. Use `Write-ConsoleStatus` or `Write-ConsoleLog` for rich messages in your own scripts.

The original values are saved once per enabled interval. Disable or module removal restores exactly the properties ConsoleMonster successfully set, even if you edited those properties while Streams was enabled. Other settings are untouched. Streams alone does not install an `Out-Default` function, and it is included in `-Feature All`.

For a .NET host, theme colors come from the same shared theme and encoder. Installing native PowerShell stream preferences belongs to its PowerShell runspace.

```csharp
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

var warning = Terminal.Theme.Resolve(ThemeToken.Warn, ColorDepth.TrueColor);
string warningSgr = SgrEncoder.Encode(warning, ColorDepth.TrueColor);
```

## Parameter mapping

The proxies use the parameter metadata of the current PowerShell runtime. On the direct-host route they retain the native property expressions' raw values while the native formatter supplies labels, grouping and layout records. Those values pass through the same shaping budgets and presenters as `Format-ConsoleTable` and `Format-ConsoleList`: status glyphs and tones, byte units, dates, durations and joined collections. Calculated expressions and getters are read once. Conditional table views keep their selected row expressions.

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

## Export a native view explicitly

Use the same adapter independently of session formatting when you need a widget for export or composition.
Already-created native format records contain strings, so this explicit record-only route retains their cell text and status tones. Use `Format-ConsoleTable -As Markdown` or `-AsWidget` when exporting raw objects with their full value presenters.

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
