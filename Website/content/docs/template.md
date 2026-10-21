---
title: dotnet new template
description: "dotnet new consolemonster: a console app with a menu, a status report, logging, the hosting dashboard and native AOT."
slug: template
order: 13
group: apps
card: template
apis: [dotnet new]
chips: [Native AOT]
summary: "A console app with a menu, logging, the dashboard and native AOT."
---

```bash
dotnet new install ConsoleMonster.Templates
dotnet new consolemonster -o MyTool
cd MyTool
dotnet run                 # the menu: a status report and how to run the service
dotnet run -- serve        # the generic host with the live console dashboard (Ctrl+C stops it)
dotnet publish -c Release -r win-x64   # one native executable (native AOT)
```

{{< cm-demo id="template" heading="h2" >}}

The recording runs the template's own `Program.cs` against this repository's packages: the menu opens the status report and the service page, Exit leaves it, and `dotnet run -- serve` starts the host with its dashboard until Ctrl+C. When the output is redirected, the app prints the report instead of opening the menu.

| Option | Default | Meaning |
| --- | --- | --- |
| `--Framework` | `net8.0` | `net8.0` or `net10.0`. |
| `--ConsoleMonsterVersion` | `2.0.*` | The ConsoleMonster package version (the newest 2.0 release by default). |
| `--NoAot` | `false` | Leaves native AOT publishing off. |

The app uses a `Menu` with pages, a `Table` with status cells, [logging](/docs/logging/) through `ConsoleMonster.Extensions.Logging` and the [hosting dashboard](/docs/hosting/).