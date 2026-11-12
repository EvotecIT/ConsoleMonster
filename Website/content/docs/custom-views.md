---
title: Custom views
description: "Register how a type is shown, draft a view from sample objects, and ship views with a module as a data file with no ConsoleMonster dependency."
slug: custom-views
order: 52
group: shells
card: module-views
apis: [Register-ConsoleView, ConvertTo-ConsoleView, Unregister-ConsoleView]
chips: [Views, Modules, Presenters]
summary: "Register views for any type, draft them from samples and ship them with a module as plain data."
---

This page covers the view registry behind `Enable-ConsoleFormatting`: how to tell ConsoleMonster which columns, presenters and grouping a type gets, how to draft that from objects you already have, and how a module ships its views without depending on ConsoleMonster. [Restyle any command's output](/guides/restyle-commands/) walks through the same steps for one command.

{{< cm-demo id="module-views" heading="h2" >}}

## Register a view

`Register-ConsoleView` names a type and the columns to show for it. Once formatting is on, every object whose type names include it is shown that way when it reaches the console:

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting

$health = @{ Name = 'Health'; Expression = { $_.State }; Presenter = 'StatusWord' }
Register-ConsoleView -TypeName Contoso.Server -Property Name, $health, Uptime -GroupBy Site
```

| Parameter | What it sets |
|---|---|
| `-TypeName` | The type names the view applies to, as in `$object.PSObject.TypeNames`. |
| `-View` | `Table` (the default), `Cards`, `Tree`, `Inspector` or `Chart`. |
| `-Property` | The columns, as `Show-ConsoleTable` takes them: names, or hashtables with `Name`, `Expression`, `Presenter` and the other column options. |
| `-GroupBy` | A property name or script block that puts rows under headings. |
| `-Title` | A script block giving each object's title: a shared title heads the view, different titles group the rows. |
| `-Group` | A group name: consecutive objects of types in the same group are shown together, as files and folders are. |
| `-Shaper` | A script block that turns each object into what is shown instead, such as an ordered dictionary of the fields worth seeing. It applies in every ConsoleMonster command. |
| `-Summary` | A script block that sums up the objects shown together, under the view on the console. It returns facts: strings, or hashtables with `Value`, `Label`, `Caption`, `Presenter` and `Role`. |

Registrations belong to the current session (runspace), and a registration takes precedence over the view ConsoleMonster ships for the same type. `Unregister-ConsoleView -TypeName Contoso.Server` removes it, and `Enable-ConsoleFormatting -NoBuiltIn` keeps only the views you registered.

## Draft a view from samples

`ConvertTo-ConsoleView` looks at the objects piped to it and returns a ready-to-paste `Register-ConsoleView` script:

```powershell
Get-Process | ConvertTo-ConsoleView
```

The draft examines the first 50 objects and chooses up to six non-empty columns, with the name or identity first. Presenters come from the same inference as the default table: status words, byte bars for sizes that vary more than tenfold, and relative dates for the last 30 days. A count of the objects, and of each status, becomes the summary; drop `-Summary $summary` to leave it out.

- `-Register` installs the drafted view now, and still returns the script for later editing.
- `-AsDataFile` returns a data file instead of a script, for a module to ship.

Sample one shape of object at a time. Deserialized input (from jobs and remoting) drafts for the base type with a warning, and `Select-Object` output keeps its `Selected.` type name, also with a warning.

## Ship views with a module

A module can ship its views as a data file beside its manifest, named `<ModuleName>.consoleviews.psd1`. The module needs no dependency on ConsoleMonster: it keeps returning ordinary typed objects, and its users get the view when they import ConsoleMonster and turn formatting on.

```powershell
# Contoso.Servers.consoleviews.psd1
@{
    SchemaVersion = 1
    Views = @(
        @{
            TypeName = 'Contoso.Server'
            View = 'Table'
            Property = @(
                'Name'
                @{ Name = 'Health'; Expression = 'State'; Presenter = 'StatusWord' }
                'Uptime'
            )
            GroupBy = 'Site'
            Group = 'ContosoServers'
        }
    )
}
```

- **Format.** A PowerShell data file, read with `Import-PowerShellDataFile`. `SchemaVersion` is `1`, and each entry in `Views` has a `TypeName` and optionally `View`, `Property`, `GroupBy` (a property name) and `Group`. Plain columns are strings; only columns with options need hashtables, and their `Expression` is a property name.
- **No code.** The file holds no script blocks or commands. Calculated expressions, shapers and summaries belong in an explicit `Register-ConsoleView` in the session.
- **Discovery.** ConsoleMonster finds the file for modules already loaded when formatting is enabled or a view is built, including modules imported after ConsoleMonster. Each module is inspected once per session, and discovery never imports other modules or changes their objects.
- **Validation.** The whole file is checked before anything is registered. A discovered file that is not valid produces a warning; an explicit import reports the error.
- **Precedence.** An explicit `Register-ConsoleView` wins over a discovered file, and discovery keeps existing registrations.

The module's users do the rest:

```powershell
Import-Module Contoso.Servers
Import-Module ConsoleMonster
Enable-ConsoleFormatting
Get-ContosoServer

# Explicit formatting and export use the same registered columns.
Get-ContosoServer | New-ConsoleTable | Export-ConsoleFrame -Format Markdown
```

To load a file from another location, or reload one while working on it, import it explicitly:

```powershell
Register-ConsoleView -Path ./views/Contoso.Servers.consoleviews.psd1
```

PowerShell's own `Format-Table -View` definitions stay separate and still come from `.format.ps1xml` files.

## In C#

The data file is a PowerShell module convention. In .NET code, declare the same columns on the table:

```csharp
var table = new Table { RecordViewRows = 0 };
table.AddColumn("Name");
table.AddColumn(new TableColumn("Health") { Presenter = CellPresenters.StatusWord });
table.AddValues("alpha", "Pass");
Terminal.Write(table);
```

## Next

- [Restyle any command's output](/guides/restyle-commands/): a view for another module's command, from type name to profile.
- [Everyday PowerShell output](/guides/everyday-powershell/): the views that ship with ConsoleMonster.
- [PowerShell conventions](/docs/powershell-conventions/): the column options and presenters `-Property` accepts.
