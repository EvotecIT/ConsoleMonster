---
title: Three ways to write it
description: "Translate the same widgets between PowerShell parameters, C# initializers and C# fluent calls."
slug: csharp-styles
order: 2
group: start
card: three-fluent
chips: [PowerShell, C#, C# fluent]
apis: [Panel, Table.For, ContentBuilder, WidgetPipeline]
summary: "The same widgets as PowerShell, C# initializers and C# fluent calls."
---

Every demo has three tabs: **PowerShell | C# | C# fluent**. They use the same widgets and operations. Choose the spelling that reads best, then use the other tabs to translate it. The choice follows you between pages; PowerShell is the default.

## Try it

{{< cm-try key="styles" ids="three-fluent" project="Servers" caption="The same servers, summary and CPU history in three source styles." >}}

## Parameters become properties and methods

A parameter keeps its domain name: `-Title 'Build'` becomes `Title = "Build"` or `.Title("Build")`. Panel's `-Border Rounded` maps to `Border = BoxStyle.Rounded` and `.Border(BoxStyle.Rounded)`; Table uses its own `TableBorder` enum with the same property name. Each demo's generated **How it maps** note names its actual owning members.

Callback properties use `.Configure(item => item.Action = callback)`. C# treats `.Action(...)` as invoking the delegate property, so a property-named extension cannot assign it. Configure returns the same object and runs the assignments in order; it does not invoke the callback. The generated mapping notes show this exception for each affected parameter.

```powershell
New-ConsolePanel -Title 'Build' -Border Rounded -Content {
    New-ConsoleText 'All checks passed'
} | Show-Console
```

```csharp
var panel = new Panel {
    Title = "Build",
    Border = BoxStyle.Rounded,
    Content = new TextBlock("All checks passed")
};
Terminal.Write(panel);
```

```csharp
new Panel()
    .Title("Build")
    .Border(BoxStyle.Rounded)
    .Content(c => c.Text("All checks passed"))
    .Show();
```

The C# snippets use `ConsoleMonster`, `ConsoleMonster.Layout`, `ConsoleMonster.Glyphs` and `ConsoleMonster.Widgets`. `.Content(widget)` attaches an existing widget; `.Content(c => …)` builds nested content, like PowerShell's `-Content { … }`. Rows, Columns, Stack, LayoutGrid, Panel and dashboard regions use the same grammar.

A pipeline ending becomes a chain ending: `| Show-Console` maps to `.Show()`, and `| Export-ConsoleFrame -Path ./build.svg -Format Svg` maps to `.Export("./build.svg", "Svg")`. Export dispatches through the same registered format providers. Add `ConsoleMonster.Markdown` for Markdown display/export and `ConsoleMonster.Graphics` for raster formats; the core does not implement those formats.

PowerShell switches name the non-default behavior: `-NoMouse` maps to `Mouse = false` and `.Mouse(false)`. Numeric `-TimeoutSeconds 0.5` maps to `Timeout = TimeSpan.FromSeconds(0.5)` and `.Timeout(TimeSpan.FromSeconds(0.5))`. Constructor arguments, collection rows and host callbacks have explicit entries in the [API parity map](/docs/api-parity/).

## Choosing a style

Use initializers for several settings, named variables and rows filled in a loop. Use fluent calls when one expression makes the layout easier to follow. Both mutate the same object and return the same widget types. Configure a widget before starting a live session; once the session owns it, use its update API.

Fluent methods are generated at build time from public properties. They use no runtime reflection and retain the receiver's derived type. Immutable values such as TerminalProfile keep their existing copy semantics and `With*` methods. The `Ui` factories remain available for short C# compositions; the demo tabs use explicit types so their parameter mappings remain visible.

The [gallery](/demos/) checks all three maintained programs across every recorded state. The [API reference](/api/) shows property and fluent configuration forms together; the [parity contract](/docs/api-parity/) lists host and C#-only exceptions.
