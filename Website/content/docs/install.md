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

The module runs on Windows PowerShell 5.1 and PowerShell 7. Coming from the 0.0.x module: its `New-SpectreImage`, `New-SpectreRule` and `New-Terminal*` functions are gone, and 0.0.4 stays on the PowerShell Gallery if a script still needs them.

## Three ways to use ConsoleMonster output

Choose the approach that fits what you are doing:

- **Automatic:** run `Enable-ConsoleFormatting`, then use your normal commands. Known and registered types get ConsoleMonster views.
- **Explicit:** pipe any objects to `Show-ConsoleTable` to choose columns, a title and grouping, or to `Show-ConsoleObject` to explore their members. These commands do not change session formatting. Use `Show-Console -View Tree` in a script when you do not want interactive input.
- **Native `Format-*` restyled:** `FormatCommands` renders `Format-Table`, `Format-List` and `Format-Wide` at the console while keeping PowerShell's column choices. It is included by default. Use `-Exclude FormatCommands` to leave them native. Redirected and captured output stays native.

You do not need both automatic and explicit formatting.

To try each approach separately:

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting
Get-ChildItem
Disable-ConsoleFormatting
```

```powershell
Import-Module ConsoleMonster
$servers = @(
    [pscustomobject]@{ Name = 'web-01'; Site = 'North'; State = 'Running' }
    [pscustomobject]@{ Name = 'db-01'; Site = 'South'; State = 'Stopped' }
)
$servers | Show-ConsoleTable -Property Name, Site, State -Title 'Servers' -GroupBy Site
$servers[0] | Show-Console -View Tree
```

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting
Get-Process | Select-Object -First 5 | Format-Table Name, Id
Disable-ConsoleFormatting -Feature FormatCommands
```

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
        .Key()
        .Overflow(ColumnOverflow.Never))
    .Column(a => a.Size, c => c
        .Presenter(CellPresenters.Bytes)
        .Aggregate(ColumnAggregate.Sum))
    .Totals()
    .Title("Artifacts"));
```

This uses the fluent style. The same table can be written with object initializers or PowerShell parameters; [Three ways to write it](/docs/csharp-styles/) shows the translation.

The package targets netstandard2.0, net472, net8.0 and net10.0 and has no package dependencies. Native AOT applications can use typed tables, rendering and export; to inspect objects in a trimmed build, set `InspectorOptions.UseReflection = false` and use dictionary, JSON or custom member sources.

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

Windows PowerShell 5.1 loads only one version of ConsoleMonster per session, so two modules that need different versions cannot both load there. PowerShell 7 keeps them apart. Keep the modules that use ConsoleMonster on the same, current version.

Every type and cmdlet is in the [.NET API reference](/api/) and the [PowerShell command reference](/powershell/commands/), and the [widget gallery](/widgets/) shows every widget with its source.

## Next

- [Add-ons](/docs/add-ons/): the optional Markdown, graphics, hosting and logging packages.
- [How it works](/docs/how-it-works/): what ConsoleMonster detects and how it draws.
- [Three ways to write it](/docs/csharp-styles/): PowerShell, C# initializers and C# fluent calls side by side.
- [Coming from another tool](/guides/coming-from/): familiar APIs and commands mapped to ConsoleMonster.
