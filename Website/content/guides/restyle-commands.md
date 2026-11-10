---
title: Restyle any command's output
description: "Give another module's commands, or PowerShell's own, readable views: find the type name, pick columns and presenters, add a summary line and keep it."
slug: restyle-commands
card: formatting
order: 12
group: tasks
---

`Enable-ConsoleFormatting` already has views for the commands people type most: files, processes, services, events, history and more. Any other command keeps a generic table of every property until you tell ConsoleMonster what matters. That works for a module someone else wrote, a module of your own and PowerShell's built-in commands alike, and the objects themselves never change.

{{< cm-frame ids="restyle-commands" script="true" >}}

The script stands in for someone else's module with `Get-ContosoBackup`, a function that returns typed objects. Before the registration they show as the generic table of every property, with sizes in raw bytes; after it, the same command shows the chosen columns, sizes as a bar against the largest, and a line that sums them up.

## Start from a draft

Pipe a few results into `ConvertTo-ConsoleView` to get a working registration script:

```powershell
Get-ContosoBackup | ConvertTo-ConsoleView
```

For the backup objects above, the draft is ready to paste into your profile:

```powershell
# Contoso.Backup: keep these lines in your profile; ConvertTo-ConsoleView -AsDataFile makes the module form.
$state = @{ Name = 'State'; Expression = 'State'; Presenter = 'StatusWord' }
$size = @{ Name = 'Size'; Expression = 'Size'; Presenter = 'BytesBar' }
$took = @{ Name = 'Took'; Expression = 'Took'; Presenter = 'Duration' }

$summary = {
    param($items)
    @{ Value = @($items).Count; Label = 'backups' }
    $roles = @{
        failed = 'Fail'; error = 'Fail'; stopped = 'Fail'
        warning = 'Warn'; degraded = 'Warn'
        completed = 'Pass'; passed = 'Pass'; success = 'Pass'; ok = 'Pass'
    }
    $items | Group-Object -Property 'State' | ForEach-Object {
        $label = $_.Name.ToLowerInvariant()
        $fact = @{ Value = $_.Count; Label = $label }
        if ($roles.ContainsKey($label)) { $fact.Role = $roles[$label] }
        $fact
    }
}
# Drop -Summary $summary to leave the line out.
$view = @{
    TypeName = 'Contoso.Backup'
    Property = @('Server', $state, $size, $took)
    Summary = $summary
}
Register-ConsoleView @view
```

Plain columns stay names; presenters get short named variables. Short registrations stay
on one line, and longer ones use a splat. The summary is on by default: it counts backups
and each status, with lowercase labels and pass, warning or failure roles. Remove
`-Summary $summary` (or the `Summary` entry in a splat) to leave the line out.

The draft samples the first 50 objects and chooses up to six nonempty columns, with the
name or identity first, even when a default display set puts it last. For `Get-Process`,
that gives `Name`, `Id`, `CPU`, `Handles` and `SI`. Presenters reuse the default table's
inference, with status words, bars for widely varying sizes and relative dates for recent
values. Add `-Register` to install the columns and summary now; `Enable-ConsoleFormatting`
makes registered views show on the console.

To keep a data-only file instead:

```powershell
Get-ContosoBackup | ConvertTo-ConsoleView -AsDataFile |
    Set-Content ContosoBackup.consoleviews.psd1 -Encoding UTF8
Register-ConsoleView -Path ./ContosoBackup.consoleviews.psd1
```

Ship it as `ModuleName.consoleviews.psd1` beside that module's manifest for automatic
discovery. Plain columns are strings in its `Property` list; only presenter columns need
hashtables. The file contains no script blocks, so summaries stay in the profile script.
Sample one object shape at a time. Deserialized input warns and drafts for the base type;
that view does not apply to job or remote output. `Select-Object` input keeps its
`Selected.` type name and warns that the view targets that selected shape.

## 1. Find the type name

A view is registered for a type name, and every object carries a list of them. The first is the most specific and the one to use:

```powershell
(Get-ContosoBackup)[0].PSObject.TypeNames[0]   # Contoso.Backup
Get-ADUser someone | Get-Member                # TypeName: Microsoft.ActiveDirectory.Management.ADUser
```

`Get-Member` prints it on its first line. Objects a module builds with `[pscustomobject] @{ PSTypeName = '...' }` carry the name it chose; .NET objects carry their class name, such as `System.Diagnostics.Process`.

## 2. Register the view

`Register-ConsoleView` takes the type name and the columns, written the way `Select-Object` and `Show-ConsoleTable` take them: property names, or hashtables with a `Name`, an `Expression` and a `Presenter` that decides how the value looks.

```powershell
$state = @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }
$size = @{ Name = 'Size'; Expression = { $_.Size }; Presenter = 'BytesBar' }
Register-ConsoleView -TypeName Contoso.Backup -Property Server, $state, $size, Took
```

The presenters most views need:

| Presenter | Shows |
|---|---|
| `Bytes`, `BytesBar` | `52 GB`, with a bar against the largest in the list |
| `Duration`, `DurationBar` | `41 m 00 s`, with a bar |
| `Date`, `DateRelative` | `2026-11-09 14:30`, or `3 h ago` |
| `StatusWord`, `Status` | Pass, fail and warning words (`Completed`, `Failed`, `Running`) with their marker and colour |
| `Progress`, `DataBar` | A fraction as a bar, or a number against the largest |
| `Boolean` | Yes and No |
| `Link` | A clickable URL or path where the terminal supports it |

`-GroupBy` puts rows under headings, `-View` picks a different shape (`Cards`, `Tree`, `Inspector` or `Chart`), and types registered with the same `-Group` are shown together, as files and folders are.

## 3. Sum it up

`-Summary` adds a line under the view on the console. The script gets the objects shown together and returns facts: plain strings, or hashtables with a `Value` and optionally a `Label`, a `Caption`, a `Presenter`, a `Role` (`Pass`, `Warn`, `Fail`) and `NewLine`.

```powershell
Register-ConsoleView -TypeName Contoso.Backup -Property Server, $state, $size, Took -Summary {
    param($backups)
    @{ Value = $backups.Count; Label = 'backups' }
    $failed = @($backups | Where-Object State -eq 'Failed').Count
    if ($failed) { @{ Value = $failed; Label = 'failed'; Role = 'Fail' } }
}
```

The line appears only on the console. `Show-ConsoleTable`, exports and captured output keep the view without it, and a summary that fails leaves the view as it was.

## 4. Built-in commands

The same works for PowerShell's own commands, and a registration takes precedence over the view ConsoleMonster ships for the same type:

```powershell
Register-ConsoleView -TypeName System.Diagnostics.Process -Property ProcessName, Id, @{
    Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'BytesBar' }
Get-Process
```

`Unregister-ConsoleView -TypeName System.Diagnostics.Process` brings the shipped view back, and `Enable-ConsoleFormatting -NoBuiltIn` keeps only the views you registered.

## 5. Keep it

Registrations last for the session. To have them in every session, put the lines in your profile after `Enable-ConsoleFormatting`, or keep them in a views file and load it with `Register-ConsoleView -Path ./my.consoleviews.psd1`.

If you maintain the module, ship the view with it instead: a `<ModuleName>.consoleviews.psd1` file beside the manifest names the columns, presenters and grouping as plain data. ConsoleMonster picks it up for loaded modules, and the module needs no dependency on ConsoleMonster: without it, users see PowerShell's formatting. [Module-owned views](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Module-Views.md) describe the file. Script blocks such as `-Summary` and calculated `Expression`s belong in an explicit `Register-ConsoleView`, since the data file runs no code.

## When the view does not apply

- **`Select-Object` made new objects.** `Get-ContosoBackup | Select-Object Server, Size` returns objects named `Selected.Contoso.Backup`, which have no view of their own. Leave the selection to the view, or register the selected type name too.
- **They came from a job or a remote session.** `Receive-Job` and `Invoke-Command` return copies named `Deserialized.<type>`, which keep PowerShell's formatting. Pipe them to `Show-ConsoleTable -Property ...` to choose the columns there.
- **You asked for a format.** `Format-Table`, `Format-List`, `Out-String`, redirected output and transcripts stay PowerShell's, by design.
- **Formatting is off.** `Get-ConsoleFormatting` lists what is on; views need the `Output` feature, which `Enable-ConsoleFormatting` turns on by default.

## Related

- [Everyday PowerShell output](/guides/everyday-powershell/): the views that ship with ConsoleMonster.
- [`Register-ConsoleView`](/powershell/commands/register-consoleview/) and [`Show-ConsoleTable`](/powershell/commands/show-consoletable/): every column option.
