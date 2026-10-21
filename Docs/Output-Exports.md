# Export output and copy it to the clipboard

ConsoleMonster exports the same widgets used in the terminal. Use `-As Markdown`
or `-As Html` to capture a formatted view as one string:

```powershell
Import-Module ConsoleMonster

Get-Process |
    Select-Object -First 10 Name, Id, WorkingSet64 |
    Format-ConsoleTable -As Markdown |
    Set-Content -Path .\processes.md -Encoding UTF8

$html = Get-Process -Id $PID | Format-ConsoleList -As Html
$html | Set-Content -Path .\process.html -Encoding UTF8
```

`-As` works on `Format-ConsoleTable`, `Format-ConsoleList`, `Format-ConsoleTree`,
`Format-ConsoleCode`, `Format-ConsoleJson` and `Format-ConsoleError`. It returns a
document without drawing to the console, including inside scripts and on
Windows PowerShell 5.1. `-Width` controls the rendered layout; exports default to
100 columns. A specified culture controls the same value presenters as normal
formatting.

Choose one output mode. `-As` cannot be combined with `-AsString`, `-AsWidget` or
`-PassThru`. `-AsString` retains the existing captured-text behavior. Where
supported, `-AsWidget` returns a widget for composition, and `-PassThru` displays
the view while returning the input objects.

## Registered views

Use `Out-ConsoleDefault -As` to export objects using their registered views,
without enabling automatic host formatting:

```powershell
Get-Process -Id $PID | Out-ConsoleDefault -As Markdown -Width 100
```

The same view discovery and registration apply as for normal output, including
module-provided views. Other objects use ConsoleMonster's object-to-table
export. `-As` cannot be combined with `-Transcript` or the internal `-Shim`.
The opt-in `Format-Table`, `Format-List` and `Format-Wide` proxies retain
PowerShell's native parameters. To export a `Format-Console*` view, call that
command with `-As` or pass its widget to `Export-ConsoleFrame`.

## Export widgets, rendered frames and files

```powershell
Get-Service |
    Select-Object -First 8 Name, Status |
    Format-ConsoleTable -AsWidget |
    Export-ConsoleFrame -Path .\services.md -Width 90

New-ConsolePanel -Title 'Status' { New-ConsoleText 'All checks complete' } |
    Export-ConsoleFrame -Format Html -Path .\status.html
```

`Export-ConsoleFrame` recognizes `.md` and `.markdown` as Markdown, alongside its
existing HTML, SVG and text formats. `-Format` overrides the file extension.
Without `-Path`, the document goes to the pipeline. A single `CellBuffer` or
live-session snapshot keeps its own dimensions. Mixed widgets, snapshots, text
and objects are stacked in input order.

Markdown uses a fenced `text` block containing the rendered frame. Borders,
group headings, list indentation, record cards, wrapping and Unicode characters
remain visible. It does not turn a visual table into a semantic Markdown table
or active Markdown links. The fence grows when the content contains backticks,
so source code cannot close the block accidentally. Terminal escape sequences
are not included. HTML uses the existing self-contained frame exporter, with
selectable text, safe web/mail links and light/dark rendering.

## Copy to the operating-system clipboard

```powershell
Get-Process |
    Select-Object -First 10 Name, Id |
    Format-ConsoleTable -AsWidget |
    Copy-ConsoleOutput -As Markdown

New-ConsoleText 'Deployment complete' | Copy-ConsoleOutput

Get-Service |
    Select-Object -First 8 Name, Status |
    Copy-ConsoleOutput -As Html -Width 90
```

`Copy-ConsoleOutput` accepts widgets, frames, live sessions, literal strings and
objects. `-As Text` is the default. All three formats copy the serialized source
as Unicode clipboard **text**; HTML is copied as HTML source, rather than a
native rich-HTML clipboard payload. Use a widget to preserve a customized view,
or pipeline objects directly to get a table. `-PassThru` returns the document
after a successful copy. `-WhatIf` previews the operation without accessing the
clipboard.

| Platform | Provider |
| --- | --- |
| Windows | Native Unicode clipboard; no external command or STA host required |
| macOS | `pbcopy` on `PATH` |
| Linux Wayland | `wl-copy` from wl-clipboard, with `WAYLAND_DISPLAY` present |
| Linux X11 | `xclip`, with `DISPLAY` present |

On a mixed Wayland/X11 desktop, X11 is used when `wl-copy` is missing. A provider
that starts and fails is reported as a failure. Clipboard providers receive
UTF-8 data through standard input, with a bounded wait. Content is never put in
command arguments.

SSH sessions (`SSH_CONNECTION`, `SSH_CLIENT` or `SSH_TTY`), Linux sessions
without a local display, missing commands, a busy Windows clipboard and failing
providers produce a non-terminating `ConsoleClipboardUnavailable` error. No
success output is returned. To handle the failure in a script:

```powershell
$widget = Get-Service | Select-Object -First 8 Name, Status | Format-ConsoleTable -AsWidget
try {
    $widget | Copy-ConsoleOutput -As Markdown -ErrorAction Stop
} catch {
    $widget | Export-ConsoleFrame -Path .\report.md
}
```

Copying over SSH does not target the connecting computer's clipboard. Export to
a string or file and copy it locally instead.

## .NET

The core library has no runtime package dependencies. Export and clipboard
handling are reusable independently of PowerShell:

```csharp
using ConsoleMonster.Clipboard;
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;

var widget = new TextBlock("Deployment complete") { IsMarkup = false };
var options = new ExportOptions { Width = 90 };
string markdown = ConsoleExporter.ToMarkdown(widget, options);
string html = ConsoleExporter.Export(widget, ExportFormat.Html, options);
ConsoleExporter.Save(widget, "status.md", options: options);

var clipboard = new ClipboardService();
if (!clipboard.TryCopy(markdown, out string? error)) {
    System.Console.Error.WriteLine(error);
}
```

`ToMarkdown` accepts an `IWidget`, `CellBuffer` or `LiveSession`. The existing
`Export` overloads accept `ExportFormat.Markdown`. `ClipboardService.TryCopy`
returns `false` with an actionable diagnostic when no usable clipboard exists;
it does not expose the copied content in error messages.
