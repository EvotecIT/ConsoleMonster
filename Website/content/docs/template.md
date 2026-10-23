---
title: dotnet new template
description: "dotnet new consolemonster: a console app with a menu, a status report, logging, the hosting dashboard and native AOT."
slug: template
order: 14
group: apps
card: template
apis: [dotnet new]
chips: [Native AOT]
summary: "A console app with a menu, logging, the dashboard and native AOT."
---

`dotnet new consolemonster` creates a console app with a menu, a status report, logging, the hosting dashboard and native AOT publishing, ready to change into your own tool.

1. Install the template (it needs the .NET 8 SDK or later):

   ```terminal
   dotnet new install ConsoleMonster.Templates
   ```

2. Create an app and start it. `dotnet run` opens the menu; `dotnet run -- serve` starts the service with its dashboard, and Ctrl+C stops it.

   ```terminal
   dotnet new consolemonster -o MyTool
   cd MyTool
   dotnet run
   ```

3. Publish one native executable. Native AOT needs the C++ build tools: on Windows, Visual Studio's "Desktop development with C++" workload.

   ```terminal
   dotnet publish -c Release -r win-x64
   ```

{{< cm-demo id="template" heading="h2" >}}

The recording runs the template's own `Program.cs`, built against the ConsoleMonster projects of the same source as this site, with the keys pressed for it: the menu opens the status report and the service page, Exit leaves it, and `dotnet run -- serve` starts the host with its dashboard until Ctrl+C. When the output is redirected, the app prints the report instead of opening the menu.

| Option | Default | Meaning |
| --- | --- | --- |
| `--Framework` | `net8.0` | `net8.0` or `net10.0`. |
| `--ConsoleMonsterVersion` | `2.0.*` | The ConsoleMonster package version (the newest 2.0 release by default). |
| `--NoAot` | `false` | Leaves native AOT publishing off. |

The app uses a `Menu` with pages, a `Table` with status cells, [logging](/docs/logging/) through `ConsoleMonster.Extensions.Logging` and the [hosting dashboard](/docs/hosting/).

## Related

The app uses two packages: [logging](/docs/logging/) and the [hosting dashboard](/docs/hosting/). For what else a menu can open, see [menus, forms and apps](/interactive/).
