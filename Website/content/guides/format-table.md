---
title: Coming from Format-Table
description: "Format-ConsoleTable, Format-ConsoleList and Format-ConsoleTree beside the built-in Format cmdlets: pipeline input, -Property, column options and Markdown, HTML or text exports."
slug: format-table
order: 4
---

The three Format cmdlets take pipeline input like the built-in `Format-Table`, `Format-List` and `Format-Custom`, which remain the right choice when output should go through PowerShell's formatting system. They write to the console like `Write-Host`, so `$x = ... | Format-ConsoleTable` captures nothing; `-PassThru` also writes the input objects to the pipeline, and `-AsWidget` returns the widget for `Write-ConsoleWidget` or `Export-ConsoleFrame`.

{{< cm-frame ids="format-table,format-consoletable" caption="The same four objects in PowerShell 7, 100 columns wide: Format-Table -AutoSize, then Format-ConsoleTable with -GroupBy, -Total and column options." >}}

## -Property

- Names, wildcards and calculated properties work as in Format-Table.
- A hashtable with column options configures that column: `Name` (or `Expression`), `Header`, `Key`, `Priority`, `MinWidth`, `MaxWidth`, `Width`, `Overflow` (`Wrap`, `Truncate`, `TruncateMiddle`, `Never`), `MaxLines`, `Alignment`, `Presenter`, `Unit`, `Aggregate`, `Highlight`, `Markup` and thresholds such as `WarnAbove` and `FailBelow`.
- Without `-Property`, an object's DefaultDisplayPropertySet is used.

```powershell
Import-Module ConsoleMonster

Get-ChildItem C:\Windows\Logs -Recurse -File | Format-ConsoleTable -GroupBy Extension -Total -Top 20 -Property @(
    @{ Name = 'Name'; Key = $true }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Priority = 9 }
    @{ Name = 'LastWriteTime'; Header = 'Changed'; Presenter = 'DateRelative'; Priority = 5 }
    @{ Name = 'FullName'; Header = 'Path'; Overflow = 'TruncateMiddle'; Priority = 1 }
)
```

## Other parameters

| Parameter | What it does |
|---|---|
| `-GroupBy` | Groups rows, with subtotals when `-Total` is set. |
| `-Total` | A totals row (sum, count, average, minimum or maximum per column). |
| `-Top N` | Only the first N rows. |
| `-Width` | Lays the table out for a given width instead of the console's. |
| `-Culture` | Numbers and dates in a given culture; by default the current culture, as Format-Table does. |
| `-Title`, `-Caption`, `-Border` | The table's frame. |

## Export as Markdown, HTML or text

`-As Markdown` returns the table as a GitHub-flavoured Markdown document instead of drawing it, with the same presenters (sizes, durations, status words) and totals as bold rows; groups become headings. `-As Html` returns a self-contained page and `-As Text` the console layout in a fenced block. It works inside scripts and on Windows PowerShell 5.1, and `Format-ConsoleList`, `Format-ConsoleTree`, `Format-ConsoleCode`, `Format-ConsoleJson` and `Format-ConsoleError` take it too.

{{< cm-frame id="export-markdown" caption="`export-markdown.ps1` writes its table as Markdown; this is the text it printed." >}}

`Copy-ConsoleOutput -As Markdown` puts the same document on the clipboard (Windows, macOS with `pbcopy`, Linux with `wl-copy` or `xclip`), and `Export-ConsoleFrame -Path report.md` writes it to a file:

```powershell
Import-Module ConsoleMonster

Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Copy-ConsoleOutput -As Markdown
Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path ./services.md
```

## Format-Table itself

`Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless you turn on `Enable-ConsoleFormatting -Feature FormatCommands`. Then the commands you already type draw ConsoleMonster views when their output goes straight to the console, and keep PowerShell's records everywhere else; [Everyday PowerShell output](/guides/everyday-powershell/#format-table-format-list-and-format-wide) shows it before and after.

## When the console is narrow

Columns hide by priority and a footer names them; below 60 cells, when the minimum widths still do not fit, rows turn into cards titled with the key value. A single object renders as a record view.

{{< cm-frame id="narrow" script="false" caption="`format-consoletable.ps1`, the second script at the top of this page, in a 56-column console." >}}

The [tables page](/tables/) shows each behaviour at six widths, and the [PowerShell page](/powershell/) compares Format-List with Format-ConsoleList.