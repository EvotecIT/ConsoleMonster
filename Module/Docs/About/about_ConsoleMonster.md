---
topic: about_ConsoleMonster
schema: 1.0.0
---
# about_ConsoleMonster

## Short Description

Readable terminal output and composable views for PowerShell objects.

## Long Description

Pipe ordinary objects to Show-Console for a fitted view. Use Show-ConsoleTable
when you want a table, or choose List, Tree, Code, Json, Error or Markdown
with Show-Console -View. Property selects the fields you need, including
calculated properties.

New-Console commands construct widgets. Nest their Content script blocks
to build rows, columns, panels and dashboards. Show-Console displays the
result; Export-ConsoleFrame returns a document or writes it to a path.

Read-Console commands collect answers. Read-ConsoleSelect labels objects
and filters them as you type. Show-ConsoleMenu runs actions from a menu.
Show-ConsoleGrid browses tabular data; Show-ConsoleObject explores members.

Enable-ConsoleFormatting styles ordinary prompt output for this session.
It is opt-in and does not edit your PowerShell profile. Include and Exclude
choose features relative to the defaults; Disable-ConsoleFormatting
restores the native behavior. Preview the change with WhatIf.

The module supports Windows PowerShell 5.1 and PowerShell 7. It includes
the Markdown and graphics capabilities, so no add-on modules are needed.
Redirected output stays readable without terminal control sequences.

## Examples


```powershell
Get-Process | Select-Object -First 5 |
Show-ConsoleTable -Property ProcessName, Id, WorkingSet64
```

```powershell
Get-Item . | Show-Console -View Tree -Depth 1
```

```powershell
Show-Console -Content {
New-ConsolePanel -Title 'Build summary' -Content {
New-ConsoleText 'All checks passed' -Role Pass
}
}
```

```powershell
New-ConsoleText 'Ready to review' | Export-ConsoleFrame -Format Text
```

```powershell
Enable-ConsoleFormatting -WhatIf
```

## See Also


```powershell
Get-Help Show-Console -Examples
Get-Help Show-ConsoleTable -Examples
Get-Help New-ConsoleTable -Full
Get-Help Read-ConsoleSelect -Full
Get-Help Enable-ConsoleFormatting -Full
```

https://consolemonster.dev/
