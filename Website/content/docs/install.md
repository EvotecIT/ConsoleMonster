---
title: Install
description: Add the ConsoleMonster package to a .NET project or install the PowerShell module, then print a first table.
slug: install
order: 1
group: start
card: formatting
chips: [NuGet, PowerShell Gallery]
summary: "The NuGet package, the PowerShell module and a first program in each."
---

Install the PowerShell module for scripts, or add the .NET package to a C# application. Both use the same rendering engine, and each takes a minute to a first table.

## PowerShell

Install the module for your account, import it and print a first table:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Get-Process | Sort-Object CPU -Descending | Select-Object -First 8 | Show-ConsoleTable
```

Then look around. `Show-ConsoleDemo` lists the gallery's demos, grouped by what they are for, and runs the one you pick. `Enable-ConsoleFormatting` makes ordinary output readable for the rest of the session; [Everyday PowerShell output](/guides/everyday-powershell/) shows how to keep it in your profile.

```powershell
Show-ConsoleDemo
Enable-ConsoleFormatting
Get-ChildItem
```

The module runs on Windows PowerShell 5.1 and PowerShell 7. On PowerShell 7 it loads in its own assembly load context. Version 2 replaces the Spectre.Console and Terminal.Gui based 0.0.x module, whose functions (`New-SpectreImage`, `New-SpectreRule`, `New-Terminal*`) are removed; 0.0.4 stays available on the PowerShell Gallery.

## .NET

Make a console project and add the package:

```terminal
dotnet new console -n FirstTable
cd FirstTable
dotnet add package ConsoleMonster
```

Replace `Program.cs` with a first table, then start it with `dotnet run`:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var files = new DirectoryInfo(".").GetFiles().Select(f => new { f.Name, f.Length });
Table.For(files).Title("Files").Show();
```

Columns take presenters, totals and their own settings:

```csharp
using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

var artifacts = new[] {
    new { Artifact = "ConsoleMonster.2.0.0.nupkg", Size = 1_482_113L },
    new { Artifact = "ConsoleMonster.2.0.0.snupkg", Size = 312_904L }
};
Terminal.Write(Table.For(artifacts)
    .Column(a => a.Artifact, c => c
        .IsKey()
        .Overflow(ColumnOverflow.Never))
    .Column(a => a.Size, c => c
        .Presenter(CellPresenters.Bytes)
        .Aggregate(ColumnAggregate.Sum))
    .Totals()
    .Title("Artifacts"));
```

This uses the fluent style. The same table can be written with object initializers or PowerShell parameters; [Three ways to write it](/docs/csharp-styles/) shows the translation.

The package targets netstandard2.0, net472, net8.0 and net10.0 and has no package dependencies. The net8.0 and net10.0 targets are marked AOT-compatible; the object inspector's reflection member source is marked `RequiresUnreferencedCode`, and `InspectorOptions.UseReflection = false` keeps trimmed builds safe.

Native AOT applications can use typed tables and the rendering and export APIs. For inspection, use dictionary,
JSON or custom member sources with reflection disabled, or explicitly preserve the properties you inspect. The
general inspection API still reports a trimming warning at the call site; suppress it only after checking those
conditions. Inferred property columns use alphabetical order when native metadata tokens are unavailable.

This AOT support applies to applications consuming the .NET library. The PowerShell module runs inside Windows
PowerShell or PowerShell 7 and is not itself a standalone native executable.

## Optional add-ons and dependencies

| Package | Job | Runtime dependency |
| --- | --- | --- |
| `ConsoleMonster` | Terminal rendering and text/HTML/SVG/asciicast output | None |
| `ConsoleMonster.Markdown` | Markdown display and widget/frame/recording export | `OfficeIMO.Markdown` |
| `ConsoleMonster.Graphics` | PNG, GIF and APNG rendering | `ChartForgeX` 3.1.0 |
| `ConsoleMonster.Hosting` | Hosted-service dashboard | `Microsoft.Extensions.Hosting.Abstractions` |
| `ConsoleMonster.Extensions.Logging` | Dashboard logging provider | `Microsoft.Extensions.Logging.Abstractions` |

The PowerShell module includes the Markdown and Graphics bridges and loads them on first use. C# applications add the package for the output they need. [Add-ons](/docs/add-ons/) describes these boundaries.

## One version per session

Windows PowerShell 5.1 loads only one version of an assembly per session, so two modules that need different ConsoleMonster versions cannot both load there; PowerShell 7 isolates binary modules in their own load context. Products that use ConsoleMonster ship and require the same, current version.

## Next

- [Coming from another tool](/guides/coming-from/) maps familiar APIs and commands to ConsoleMonster.

- [Widgets](/widgets/) shows every widget with its PowerShell and C# source.
- [Three ways to write it](/docs/csharp-styles/) compares PowerShell, C# initializers and C# fluent calls.
- [Capability detection](/docs/capabilities/) explains what ConsoleMonster decides about your console.
- The [.NET API reference](/api/) and the [PowerShell command reference](/powershell/commands/) list every type and cmdlet.
