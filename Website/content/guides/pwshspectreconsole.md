---
title: Coming from PwshSpectreConsole
description: Move PowerShell tables, prompts, progress and charts to ConsoleMonster, including Windows PowerShell 5.1.
slug: pwshspectreconsole
order: 4
toc: false
card: migration-report-100
---

ConsoleMonster lets the same rendering script run on Windows PowerShell 5.1 and PowerShell 7, with object-oriented input and ordinary pipeline results. PwshSpectreConsole's [installation guide](https://pwshspectreconsole.com/guides/install/) requires PowerShell 7+ and recommends a Nerd Font for its full visual feature set. ConsoleMonster does not require a Nerd Font: it adapts its built-in symbols to the terminal's glyph tier. Neither statement means arbitrary emoji will work in every font.

## Command map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| PwshSpectreConsole | ConsoleMonster | Note |
| --- | --- | --- |
| `Format-SpectreTable` | `Show-ConsoleTable` | Direct display; `-AsWidget` for composition, `-AsString` for plain text. |
| `Format-SpectreTree` | `Show-Console -View Tree`, `New-ConsoleTree` | Build explicit nodes or supply hierarchical data. |
| `Format-SpectrePanel` | `New-ConsolePanel` then `Show-Console` | Builder returns a widget; does not draw it. |
| `Write-SpectreHost` | `Write-ConsoleText -Markup` | Escape untrusted bracketed text; semantic roles are available. |
| `Write-SpectreRule` | `Show-Console -View Rule` | `New-ConsoleRule` constructs a reusable definition. |
| `Read-SpectreSelection`, `Read-SpectreSelectionGrouped` | `Read-ConsoleSelect -Display ... -GroupBy ...` | Returns the selected original object. |
| `Read-SpectreMultiSelection`, grouped variant | `Read-ConsoleSelect -Multiple` | Returns chosen objects; check cancellation behavior. |
| `Read-SpectreText`, `Read-SpectreConfirm` | `Read-ConsoleText`, `Read-ConsoleConfirm` | Defaults and validation use different parameters. |
| `Invoke-SpectreCommandWithProgress` | `Use-ConsoleProgress` or `Invoke-ConsoleRun` | Wrap native progress, or execute per-item work with a run view. |
| `Invoke-SpectreCommandWithStatus` | `Show-ConsoleStatusLine` with `try/finally` | Explicitly complete the status line. |
| `Invoke-SpectreLive` | `Invoke-ConsoleDashboard` | Session handles update named widgets. |
| `Write-SpectreFigletText` | `New-ConsoleLogo` then `Show-Console` | Different lettering; no `.flf` font loading. |
| `New-SpectreChartItem`, `Format-SpectreBarChart` | `Show-Console -View Chart` or record rows piped to `New-ConsoleChart -Type Bar` | Ordinary object rows work with the display command. |
| `Format-SpectreBreakdownChart` | `New-ConsoleChart -Type Stacked` | Proportions, with ConsoleMonster's own legend/layout. |
| `Format-SpectreGrid`, `Format-SpectreColumns`, `Format-SpectreRows` | `New-ConsoleLayoutGrid`, `New-ConsoleColumns`, `New-ConsoleRows` | Definitions; pipe the result to `Show-Console`. |
| `Format-SpectreJson`, `Format-SpectreException` | `Show-Console -View Json`, `Show-Console -View Error` | Display JSON or an error; do not emit new error records. |
| `Write-SpectreCalendar` | No direct equivalent | See the [roadmap](https://github.com/EvotecIT/ConsoleMonster/blob/master/ROADMAP.md). |

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
$text = $servers | Show-ConsoleTable -Property Name, Pending -AsString
```

</div>
</div>

`Show-ConsoleTable` displays directly, rather than producing native `Format-Table` records. Keep the rows if later commands need objects. `-AsString` is plain text; use `Export-ConsoleFrame -Format Markdown` when you want a document.

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
Use-ConsoleProgress {
    for ($i = 0; $i -lt $servers.Count; $i++) {
        Write-Progress -Activity 'Inventory' -Status $servers[$i].Name `
            -PercentComplete ($i * 100 / $servers.Count)
        Start-Sleep -Milliseconds 100
    }
    Write-Progress -Activity 'Inventory' -Completed
}
```

</div>
</div>

The sleep represents the collection operation. ConsoleMonster can draw existing `Write-Progress` calls, so native progress scripts do not need Spectre task objects. For per-server results and concurrency, use [live runs](/docs/live-runs/); their execution and error contracts differ from a single progress wrapper.

## 4. A scoped status line

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
try {
    Show-ConsoleStatusLine 'Reading inventory' -Spinner
    $names = $servers.Name
} finally {
    Show-ConsoleStatusLine -Complete
}
```

</div>
</div>

Complete the line even when the work throws. This operation is separate from a `New-ConsoleStatusList` checklist or an `Invoke-ConsoleDashboard` live layout.

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
Show-Console -View Rule 'Patch inventory'
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
$servers | Show-Console -View Chart -Label Name -Property Pending -Type Bar
# For composition, build the chart and pass the widget to a panel/dashboard.
```

</div>
</div>

## Differences to know

- `New-*` creates definitions, `Show-*` renders or opens a viewer, `Read-*` returns answers, `Invoke-*` executes and waits, and `Start-ConsoleRun` returns a running handle. The names describe lifetimes rather than mirror Spectre's method names.
- `Show-* -AsString` and `-AsWidget` suppress host display. Export commands own file formats. The native PowerShell pipeline remains objects until a deliberate display/export boundary.
- PowerShell 5.1 uses the .NET Framework build; PowerShell 7 isolates the binary module in its own assembly load context. Test scripts on the hosts you support; running in PowerShell 7 does not establish native 5.1 proof.
- A Nerd Font is unnecessary for built-in rendering. ASCII/plain fallback is available; custom glyphs and emoji still need a font that contains them.
- No automatic conversion exists for Spectre renderables, Figlet fonts or calendar output. Check the [roadmap](https://github.com/EvotecIT/ConsoleMonster/blob/master/ROADMAP.md) before replacing those paths.

## Sources and next steps

Checked 25 October 2026: [installation and font guidance](https://pwshspectreconsole.com/guides/install/), [PwshSpectreConsole command reference](https://pwshspectreconsole.com/reference/formatting/format-spectretable/), and the [maintained source and generated command examples](https://github.com/ShaunLawrie/PwshSpectreConsole). This guide does not describe PwshSpectreConsole as discontinued.

Next: [PowerShell conventions](/docs/powershell-conventions/), [interactive screens](/interactive/), [ConsoleMonster command reference](/powershell/commands/) or [all migration guides](/guides/coming-from/).
