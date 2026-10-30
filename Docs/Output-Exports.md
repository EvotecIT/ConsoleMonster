# Export output and copy it to the clipboard

`Export-ConsoleFrame` accepts ordinary objects, widgets and snapshots. Choose `-Format Markdown` for supported table/tree semantics, `-Format FencedText` for a layout-preserving Markdown fence, `-Format Text` for plain text, or `-Format Html` for a self-contained frame page.

```powershell
Import-Module ConsoleMonster

Get-Process |
    Select-Object -First 10 Name, Id, WorkingSet64 |
    New-ConsoleTable | Export-ConsoleFrame -Format Markdown |
    Set-Content -Path .\processes.md -Encoding UTF8

$html = Get-Process -Id $PID | Show-Console -View List -AsWidget | Export-ConsoleFrame -Format Html
$html | Set-Content -Path .\process.html -Encoding UTF8
```

Construct a customized table with `New-ConsoleTable | Export-ConsoleFrame`. `Show-Console -AsWidget` returns other composable views without drawing. Where supported, `-PassThru` displays and returns original input. For the automatic view, pipe objects straight to the exporter.

Width 0 resolves the current target/terminal, falling back to 100 columns. Positive widths are explicit. A specified culture controls the same presenters as normal display.

## Registered views

Use `Export-ConsoleFrame -Format` to export objects using their registered views,
without enabling automatic host formatting:

```powershell
Get-Process -Id $PID | Export-ConsoleFrame -Format Markdown -Width 100
```

The same view discovery and registration apply as for normal output, including
module-provided views. Other objects use ConsoleMonster's object-to-table
export. The internal host-output adapter routes normal display; document export uses this command.
The opt-in `Format-Table`, `Format-List` and `Format-Wide` proxies retain
PowerShell's native parameters. Use a table constructor or `Show-Console -AsWidget` to export a selected view.

## Export widgets, rendered frames and files

```powershell
Get-Process |
    Select-Object -First 8 ProcessName, Id |
    New-ConsoleTable |
    Export-ConsoleFrame -Path .\services.md -Width 90

New-ConsolePanel -Title 'Status' -Content { New-ConsoleText 'All checks complete' } |
    Export-ConsoleFrame -Format Html -Path .\status.html
```

`Export-ConsoleFrame` recognizes `.md` and `.markdown` as Markdown, alongside its
existing HTML, SVG and text formats. `-Format` overrides the file extension.
Without `-Path`, the document goes to the pipeline. A single `CellBuffer` or
live-session snapshot keeps its own dimensions. Mixed widgets, snapshots, text
and objects are stacked in input order.

Markdown tables use GFM pipe syntax, with `:---`, `---:` or `:---:` alignment
markers. Cells retain their value presenters, including status glyphs, byte units,
dates, durations and joined collections. Pipes are escaped and line breaks become
`<br>`. Automatic alignment follows the console presenters: numbers, bytes and
durations align right; status words and text align left. Explicit column alignment
is retained. Groups become headings, with bold subtotals inside each group and a
separate table for the bold grand total after all groups.
Record lists become two-column property/value tables, and trees become nested
bullet lists with the same member presenters. Scalar collection items appear as
direct bullets; complex items retain their indexes. Markdown includes every
column without console-width truncation.

Use `-Format FencedText` to preserve borders and the exact console layout in a fenced `text`
block. The fence grows around backticks in the content. `Export-ConsoleFrame -Format Text` returns plain text. Widgets without
a semantic Markdown representation, and already-rendered frames or live snapshots,
use a fenced fallback because their source fields are unavailable. Terminal
escape sequences are not included. HTML uses the existing self-contained frame
exporter, with selectable text, safe web/mail links and light/dark rendering.

```powershell
Get-Process | Select-Object -First 5 Name, Id | New-ConsoleTable | Export-ConsoleFrame -Format Markdown
Get-Process | Select-Object -First 5 Name, Id | New-ConsoleTable | Export-ConsoleFrame -Format FencedText
```

## Copy to the operating-system clipboard

Export the document, then use PowerShell's normal clipboard command:

```powershell
Get-Process |
    Select-Object -First 10 ProcessName, Id |
    New-ConsoleTable |
    Export-ConsoleFrame -Format Markdown |
    Set-Clipboard

New-ConsoleText 'Deployment complete' |
    Export-ConsoleFrame -Format Text |
    Set-Clipboard
```

The exported document is ordinary text. HTML output copies the HTML source;
it is not a native rich-HTML clipboard payload. Clipboard availability and
remote-session behavior follow `Set-Clipboard` on the current platform. Export
to a file when the destination has no usable clipboard.

## .NET

The core library has no runtime package dependencies. Export and clipboard
handling are reusable independently of PowerShell:

```csharp
using ConsoleMonster.Clipboard;
using ConsoleMonster.Export;
using ConsoleMonster.Markdown;
using ConsoleMonster.Widgets;

var widget = new TextBlock("Deployment complete") { IsMarkup = false };
var options = new ExportOptions { Width = 90 };
string markdown = MarkdownExporter.Export(widget, options);
string html = ConsoleExporter.Export(widget, ExportFormat.Html, options);
System.IO.File.WriteAllText("status.md", markdown);

var clipboard = new ClipboardService();
if (!clipboard.TryCopy(markdown, out string? error)) {
    System.Console.Error.WriteLine(error);
}
```

`MarkdownExporter.Export` in the `ConsoleMonster.Markdown` package accepts an `IWidget`, `CellBuffer` or `LiveSession`; `ExportRecording` accepts terminal recordings or a dashboard definition and recorded events. Set `fenced: true` for a widget's exact terminal layout. The PowerShell module includes this add-on and loads it on first Markdown use. Core exports support Text, Html, Svg and recording Asciicast; `.md` requests made through core name the required add-on. `ClipboardService.TryCopy`
returns `false` with an actionable diagnostic when no usable clipboard exists;
it does not expose the copied content in error messages.
