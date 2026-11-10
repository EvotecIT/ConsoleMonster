---
title: Coming from Format-Table
description: "Keep typing Format-Table, Format-List and Format-Wide, or move to Show-ConsoleTable and Show-Console for column options, totals and exports."
slug: format-table
card: format-consoletable
order: 4
---

The Format commands keep working: with `Enable-ConsoleFormatting`, `Format-Table`, `Format-List` and `Format-Wide` draw ConsoleMonster views when their output goes straight to the console. When a table needs more than Format-Table can say (a presenter, a totals row, a column that hides first in a narrow window), `Show-ConsoleTable` takes the same pipeline input with column options.

{{< cm-frame ids="format-table,format-consoletable" caption="The same four objects in PowerShell 7, 100 columns wide: Format-Table -AutoSize, then Show-ConsoleTable with -GroupBy, -ShowTotals and column options." >}}

## Command map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Habit | ConsoleMonster | Difference |
| --- | --- | --- |
| `Format-Table`, `Format-List`, `Format-Wide` at the prompt | The same commands after `Enable-ConsoleFormatting` | Same parameters. Only output that goes straight to the console changes. |
| `Format-Table -Property` with calculated properties | `Show-ConsoleTable -Property` | Hashtables also take `Presenter`, `Priority`, `Aggregate`, `Overflow` and thresholds. |
| `Format-Table -GroupBy` | `Show-ConsoleTable -GroupBy` | Groups without sorting first; `-ShowTotals` adds subtotals. |
| `Format-Table -AutoSize`, `-Wrap` | The default, `-Wrap` | Columns are fitted to the window and hidden by priority; narrow windows get cards. |
| `Format-List` | `Show-Console -View List` | A titled record with the same presenters. |
| `Format-Custom` for nested objects | `Show-Console -View Tree` | Nested properties as a tree. |
| `Format-Table \| Out-String` | `New-ConsoleTable \| Export-ConsoleFrame -Format Text` | Plain text of the drawn table. |
| `Format-Table \| Out-File` | `Export-ConsoleFrame -Format Markdown -Path` | Markdown, HTML or fenced text for a document. |

</div>

## Shared sample data

Install with `Install-Module ConsoleMonster -Scope CurrentUser` and run `Import-Module ConsoleMonster`. Each example uses these rows.

```powershell
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
```

## 1. Keep typing Format-Table

<div class="cm-migration">
<div>

**PowerShell**

```powershell
$servers | Format-Table Name, Site, Pending, Status -AutoSize
$servers[1] | Format-List
```

</div>
<div>

**ConsoleMonster**

```powershell
Enable-ConsoleFormatting
$servers | Format-Table Name, Site, Pending, Status -AutoSize
$servers[1] | Format-List
```

</div>
</div>

The commands, parameters and aliases (`ft`, `fl`, `fw`) stay the same. `$x = ... | Format-Table`, `| Out-String`, `| Out-File`, `>`, jobs, remoting and transcripts keep PowerShell's formatting records and text. `Enable-ConsoleFormatting -Exclude FormatCommands` leaves the three commands native while the rest of everyday formatting stays on.

{{< cm-demo id="format-commands" heading="h3" >}}

## 2. Calculated columns and groups

<div class="cm-migration">
<div>

**PowerShell**

```powershell
$servers | Sort-Object Status | Format-Table -GroupBy Status -Property Name, Site, @{
    Name = 'Pending'; Expression = { $_.Pending }; Alignment = 'Right'
}
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | Show-ConsoleTable -GroupBy Status -ShowTotals -Property Name, Site, @{
    Name = 'Pending'; Presenter = 'DataBar'; Aggregate = 'Sum'
}
```

</div>
</div>

A column hashtable takes `Name` (or `Expression`), `Header`, `Key`, `Priority`, `MinWidth`, `MaxWidth`, `Overflow`, `Alignment`, `Presenter`, `Aggregate` and thresholds such as `WarnAbove`; the [Show-ConsoleTable reference](/powershell/commands/show-consoletable/) lists them all. Real pipeline data needs none of them: sizes and dates get their presenters automatically.

{{< cm-frame id="files" caption="Invented package artifacts with fixed sizes and dates, grouped by extension, in PowerShell 7." >}}

## 3. Text and documents

<div class="cm-migration">
<div>

**PowerShell**

```powershell
$text = $servers | Format-Table -AutoSize | Out-String
$servers | Format-Table -AutoSize | Out-File ./servers.txt
```

</div>
<div>

**ConsoleMonster**

```powershell
$text = $servers | New-ConsoleTable | Export-ConsoleFrame -Format Text
$servers | New-ConsoleTable | Export-ConsoleFrame -Format Markdown -Path ./servers.md
$servers | New-ConsoleTable | Export-ConsoleFrame -Format Markdown | Set-Clipboard
```

</div>
</div>

`Show-ConsoleTable` writes to the console the way `Write-Host` does, so `$x = ... | Show-ConsoleTable` captures nothing; `-PassThru` also passes the input objects on. `New-ConsoleTable` returns the table for an export or a layout. Markdown keeps table semantics, `FencedText` keeps the console layout in a text fence, and `Html` makes a page. All of them work in scripts and on Windows PowerShell 5.1.

{{< cm-frame id="export-markdown" caption="`export-markdown.ps1` writes its table as Markdown; this is the text it printed." >}}

## Differences to know

- Display commands are an end of the pipeline. Keep the objects in a variable when later commands need them.
- In a narrow console, columns hide by priority and a footer names them; below 60 cells, rows turn into cards titled with the key value. Format-Table cuts columns off at the edge instead.
- Collections in a cell are joined (`DNS, GC, PDC`) with a `+N` marker, where Format-Table shows `{DNS, GC, PDC}`.
- Without `-Property`, an object's default display property set is used, as in Format-Table.

{{< cm-frame id="narrow" script="false" caption="`format-consoletable.ps1`, the second script at the top of this page, in a 56-column console." >}}

## Next

- [Everyday PowerShell output](/guides/everyday-powershell/): turn the formatting on for every session.
- [Tables](/tables/): each table behaviour at six widths.
- [Everyday formatting reference](/docs/everyday-formatting/): every built-in view and how Format-* parameters map.
- [All migration guides](/guides/coming-from/).
