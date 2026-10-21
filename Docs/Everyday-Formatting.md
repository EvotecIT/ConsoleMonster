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

```powershell
# These retain PowerShell's own formatting records and exact text output.
$records = Get-Process | Format-Table Name, Id
$text = Get-Process | Format-Table Name, Id | Out-String
Get-Process | Format-Table Name, Id | Out-File ./processes.txt
Get-Process | Format-Table Name, Id > ./processes.txt
```

`-Feature All` enables default object views, errors and progress. It deliberately excludes `FormatCommands`: overriding commands used in scripts requires a separate opt-in. Features add up, so use `Enable-ConsoleFormatting -Feature All, FormatCommands` when you want both. `Disable-ConsoleFormatting` removes all installed features. `Disable-ConsoleFormatting -Feature FormatCommands` restores only the three formatter functions; explicit `-Feature All` retains the separate `FormatCommands` opt-in.

Custom functions are preserved unless `-Force` is supplied, then restored on disable or module removal. A custom alias named `Format-Table`, `Format-List` or `Format-Wide` prevents installation; remove it explicitly first. Aliases such as `ft`, `fl` and `fw` continue to resolve their normal command names.

## Parameter mapping

The proxies use the parameter metadata of the current PowerShell runtime and execute its native formatter. PowerShell resolves expressions, native `.format.ps1xml` views and formatted values; ConsoleMonster arranges the evaluated values in its existing widgets without evaluating expressions again.

| Parameter | Console host rendering |
| --- | --- |
| `-Property` | Names, wildcards and calculated properties become the native formatter's selected columns, list fields or wide values. Labels, format strings, alignment and declared widths come from its evaluated records. |
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
