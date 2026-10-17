---
title: Install
description: Add the ConsoleMonster package to a .NET project or install the PowerShell module, then print a first table.
slug: install
order: 1
---

## .NET

```bash
dotnet add package ConsoleMonster
```

The package targets netstandard2.0, net472 and net8.0 and has no package dependencies. On net8.0 it is marked AOT-compatible; the object inspector's reflection member source is marked `RequiresUnreferencedCode`, and `InspectorOptions.UseReflection = false` keeps trimmed builds safe.

```csharp
using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

Table table = new Table { Title = "Artifacts", ShowTotals = true };
table.AddColumn(new TableColumn("Artifact") { IsKey = true, Overflow = ColumnOverflow.Never });
table.AddColumn(new TableColumn("Size") { Priority = 9, Presenter = CellPresenters.Bytes, Aggregate = ColumnAggregate.Sum });
table.AddValues("ConsoleMonster.2.0.0.nupkg", 1_482_113L);
Terminal.Write(table);
```

## PowerShell

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Get-Service | Select-Object -First 8 Name, Status, StartType | Write-ConsoleTable -Title 'Services' -StatusProperty Status
```

The module runs on Windows PowerShell 5.1 and PowerShell 7. On PowerShell 7 it loads in its own assembly load context. Version 2 replaces the Spectre.Console and Terminal.Gui based 0.0.x module, whose functions (`New-SpectreImage`, `New-SpectreRule`, `New-Terminal*`) are removed; 0.0.4 stays available on the PowerShell Gallery.

## One version per session

Windows PowerShell 5.1 loads only one version of an assembly per session, so two modules that need different ConsoleMonster versions cannot both load there; PowerShell 7 isolates binary modules in their own load context. Products that use ConsoleMonster ship and require the same, current version.

## Next

- [Widgets](/widgets/) shows every widget with its C# and PowerShell source.
- [Capability detection](/docs/capabilities/) explains what ConsoleMonster decides about your console.
- The [.NET API reference](/api/) and the [PowerShell command reference](/powershell/commands/) list every type and cmdlet.