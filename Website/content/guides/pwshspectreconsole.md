---
title: Coming from PwshSpectreConsole
description: Move PowerShell tables, prompts, progress and charts to ConsoleMonster, including Windows PowerShell 5.1.
slug: pwshspectreconsole
order: 3
group: from
toc: false
card: migration-report-100
---

Both modules draw tables, trees, panels, prompts, progress and charts from PowerShell, so most scripts move command by command. The differences are in naming and hosts. ConsoleMonster separates `New-*` definitions from `Show-*` display and `Read-*` answers, and runs on Windows PowerShell 5.1 as well as PowerShell 7. PwshSpectreConsole's [installation guide](https://pwshspectreconsole.com/guides/install/) lists PowerShell 7 and recommends a Nerd Font for its full set of symbols; ConsoleMonster picks its built-in symbols from the terminal's glyph tier, so it needs no particular font. Emoji in your own text depend on the font in either module.

## Command map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| PwshSpectreConsole | ConsoleMonster | Note |
| --- | --- | --- |
| `Format-SpectreTable` | `Show-ConsoleTable` | Direct display; construct with `New-ConsoleTable` and export text with `Export-ConsoleFrame`. |
| `Format-SpectreTree` | `Show-Console -View Tree`, `New-ConsoleTree` | Build explicit nodes or supply hierarchical data. |
| `Format-SpectrePanel` | `New-ConsolePanel` then `Show-Console` | Builder returns a widget; does not draw it. |
| `Write-SpectreHost` | `Write-ConsoleText -Markup` | Escape untrusted bracketed text; semantic roles are available. |
| `Write-SpectreRule` | `New-ConsoleRule \| Show-Console` | `New-ConsoleRule` constructs a reusable definition. |
| `Read-SpectreSelection`, `Read-SpectreSelectionGrouped` | `Read-ConsoleSelect -Display ... -GroupBy ...` | Returns the selected original object. |
| `Read-SpectreMultiSelection`, grouped variant | `Read-ConsoleSelect -Multiple` | Returns chosen objects; check cancellation behaviour. |
| `Read-SpectreText`, `Read-SpectreConfirm` | `Read-ConsoleText`, `Read-ConsoleConfirm` | Defaults and validation use different parameters. |
| `Invoke-SpectreCommandWithProgress` | `Invoke-ConsoleRun`, or `Use-ConsoleProgress` | Per-item work with a run view; or draw an existing script's `Write-Progress` calls. |
| `Invoke-SpectreCommandWithStatus` | `Show-ConsoleStatusLine -Spinner`, then `-Complete` | One line that updates in place until completed. |
| `Invoke-SpectreLive` | `Invoke-ConsoleDashboard` | Session handles update named widgets. |
| `Write-SpectreFigletText` | `New-ConsoleLogo` then `Show-Console` | Different lettering; no `.flf` font loading. |
| `New-SpectreChartItem`, `Format-SpectreBarChart` | `New-ConsoleChart -Type Bar \| Show-Console` | The constructor accepts ordinary records and returns a widget. |
| `Format-SpectreBreakdownChart` | `New-ConsoleChart -Type Stacked` | Proportions, with ConsoleMonster's own legend/layout. |
| `Format-SpectreGrid`, `Format-SpectreColumns`, `Format-SpectreRows` | `New-ConsoleLayoutGrid`, `New-ConsoleColumns`, `New-ConsoleRows` | Definitions; pipe the result to `Show-Console`. |
| `Format-SpectreJson`, `Format-SpectreException` | `Show-Console -View Json`, `Show-Console -View Error` | Display JSON or an error; do not emit new error records. |
| `Write-SpectreCalendar` | `New-ConsoleCalendar` then `Show-Console` | Month/year display with dated outcome rows; see the [operations calendar](/demos/operations-calendar/). |

</div>

## Shared sample data

Install with `Install-Module ConsoleMonster -Scope CurrentUser` and run `Import-Module ConsoleMonster`. For the left-hand examples import PwshSpectreConsole in PowerShell 7. Use these same rows for each example; none changes a real server.

```powershell
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
```

## 1. Show an inventory table

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
$servers | Format-SpectreTable
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | Show-ConsoleTable -Property Name, Site, Pending, Status -Title 'Patch inventory'
$text = $servers | New-ConsoleTable -Property Name, Pending | Export-ConsoleFrame -Format Text
```

</div>
</div>

`Show-ConsoleTable` displays directly, rather than producing native `Format-Table` records. Keep the rows if later commands need objects. `Export-ConsoleFrame -Format Text` captures plain text; use `Export-ConsoleFrame -Format Markdown` when you want a document.

{{< cm-try ids="migration-report-100,migration-report-48" project="InventoryReport" >}}

This full program produces the shown output, including explicit column order and a narrow-window layout.

## 2. Select a server and collect a ticket

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
$name = Read-SpectreSelection -Message 'Target server' -Choices $servers.Name
$ticket = Read-SpectreText -Message 'Change ticket' -DefaultAnswer 'CHG0042'
$review = Read-SpectreConfirm -Message "Review $name?"
```

</div>
<div>

**ConsoleMonster**

```powershell
$server = $servers | Read-ConsoleSelect 'Target server' -Display Name -GroupBy Site
$ticket = Read-ConsoleText 'Change ticket' -Default 'CHG0042' -Required
$review = Read-ConsoleConfirm "Review $($server.Name)?" -Default $false
```

</div>
</div>

The right-hand selection preserves `Site`, `Pending` and `Status` on the chosen object. Use `Read-ConsoleSelect -Multiple` for several rows. Handle the terminating `PromptCanceled` error on cancellation; do not silently treat cancellation as approval. In unattended runs provide values yourself, or use explicit defaults and `-Mode None` where appropriate.

## 3. Report progress while collecting data

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
Invoke-SpectreCommandWithProgress -ScriptBlock {
    param($context)
    $task = $context.AddTask('Inventory')
    foreach ($server in $servers) {
        Start-Sleep -Milliseconds 100
        $task.Increment(100 / $servers.Count)
    }
}
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | Invoke-ConsoleRun -Title 'Inventory' -Label Name {
    Start-Sleep -Milliseconds 100
}
```

</div>
</div>

The sleep stands in for the collection work; `$_` is the server. The run view counts done of total, shows a cell for each server and the ones still running, and lists the items that failed when it ends. Add `-Parallel -ThrottleLimit 8` to collect several at once. A script that already calls `Write-Progress` needs no rewrite: wrap it in `Use-ConsoleProgress { ... }` to draw its bars. See [live runs](/docs/live-runs/) for retries, time limits and the output contract.

## 4. A status line while work runs

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
$names = Invoke-SpectreCommandWithStatus -Title 'Reading inventory' -ScriptBlock {
    $servers.Name
}
```

</div>
<div>

**ConsoleMonster**

```powershell
Show-ConsoleStatusLine 'Reading inventory' -Spinner
$names = $servers.Name
Show-ConsoleStatusLine "Read $($names.Count) servers" -Status Pass -Complete
```

</div>
</div>

Each call replaces the line; `-Complete` writes the final version and moves on. If the work throws, the spinner stops by itself as soon as the error moves the cursor, so it never draws over the message. Outside a real console each call writes a plain line. For a checklist of several steps use `New-ConsoleStatusList`, and for a full live layout `Invoke-ConsoleDashboard`.

## 5. Heading and message

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
Write-SpectreFigletText -Text 'Ops'
Write-SpectreRule -Title 'Patch inventory'
Write-SpectreHost '[yellow]SQL01 needs review[/]'
```

</div>
<div>

**ConsoleMonster**

```powershell
New-ConsoleLogo 'Ops' | Show-Console
New-ConsoleRule -Title 'Patch inventory' | Show-Console
Write-ConsoleText -Markup '[warn]SQL01 needs review[/]'
```

</div>
</div>

The built-in logo lettering is different. Literal text is the default for `Write-ConsoleText`; `-Markup` opts into parsing. Use `New-ConsoleText` to compose styled cells and labels.

## 6. Chart pending patches

<div class="cm-migration">
<div>

**PwshSpectreConsole**

```powershell
$items = foreach ($server in $servers) {
    New-SpectreChartItem -Label $server.Name -Value $server.Pending -Color Yellow
}
Format-SpectreBarChart -Data $items -Label 'Pending patches'
```

</div>
<div>

**ConsoleMonster**

```powershell
$servers | New-ConsoleChart -Label Name -Property Pending -Type Bar | Show-Console
# For composition, build the chart and pass the widget to a panel/dashboard.
```

</div>
</div>

## Differences to know

- `New-*` creates definitions, `Show-*` renders or opens a viewer, `Read-*` returns answers, `Invoke-*` executes and waits, and `Start-ConsoleRun` returns a running handle. The names describe lifetimes rather than mirror Spectre's method names.
- Constructors return widgets without drawing. Export commands return text or write files. The native PowerShell pipeline remains objects until a deliberate display/export boundary.
- PowerShell 5.1 uses the .NET Framework build; PowerShell 7 isolates the binary module in its own assembly load context. Test scripts on the hosts you support; running in PowerShell 7 does not establish native 5.1 proof.
- A Nerd Font is unnecessary for built-in rendering. ASCII/plain fallback is available; custom glyphs and emoji still need a font that contains them.
- No automatic conversion exists for Spectre renderables or Figlet fonts. Calendar output uses `New-ConsoleCalendar` with a month or year view. Check the [roadmap](https://github.com/EvotecIT/ConsoleMonster/blob/master/ROADMAP.md) before replacing those paths.

## Sources

Checked 25 October 2026: [installation and font guidance](https://pwshspectreconsole.com/guides/install/), [PwshSpectreConsole command reference](https://pwshspectreconsole.com/reference/formatting/format-spectretable/), and the [maintained source and generated command examples](https://github.com/ShaunLawrie/PwshSpectreConsole). This guide does not describe PwshSpectreConsole as discontinued.

## Next

- [PowerShell conventions](/docs/powershell-conventions/): what `New`, `Show`, `Read` and `Invoke` mean.
- [Interactive screens](/interactive/): prompts and forms with recorded input.
- [ConsoleMonster command reference](/powershell/commands/).
- [All migration guides](/guides/coming-from/).
