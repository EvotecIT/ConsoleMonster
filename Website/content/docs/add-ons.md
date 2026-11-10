---
title: Add-ons
description: Markdown and graphics bridges stay outside the dependency-free terminal core.
slug: add-ons
order: 2
group: start
card: formatting
chips: [NuGet, Markdown, Graphics]
summary: "Choose the bridge for documents, images, hosting or logging."
---

The core never emits Markdown; the `ConsoleMonster.Markdown` add-on owns Markdown display and export. Markdown belongs to OfficeIMO, and image rendering belongs to ChartForgeX, so both bridges live outside the terminal core.

## Markdown

Add `ConsoleMonster.Markdown` to a C# application. `MarkdownConsole.Parse` and `Show` read and display documents; `MarkdownExporter.Export` writes widgets, frames and live snapshots; `ExportRecording` writes recordings. The add-on uses OfficeIMO.Markdown's document model for fenced blocks and recording documents. Semantic widget output retains its cell presenters, groups and totals. See [Markdown display and export](/docs/markdown/).

The PowerShell module includes the add-on and loads it when Markdown is requested. `$rows | Export-ConsoleFrame -Format Markdown` and `Export-ConsoleRecording -Format Markdown` keep the same command names.

## Graphics

`ConsoleMonster.Graphics` exports PNG, GIF and APNG through ChartForgeX 3.1.0. It includes frame and recording exports and slide presets. See [Talks and demos](/docs/talks-and-demos/). It also draws images inline with Kitty, iTerm2 or Sixel pixels, or half blocks; see [Inline images](/docs/inline-images/).

## Hosting and logging

`ConsoleMonster.Hosting` integrates hosted services with a dashboard. `ConsoleMonster.Extensions.Logging` sends structured log entries to a dashboard region. Their dependencies stay in those packages; the core has none. See [Hosting](/docs/hosting/), [Logging](/docs/logging/) and the [dependency list](/docs/install/#optional-add-ons-and-dependencies).

## PowerShell output from your own cmdlets

`ConsoleMonster.PowerShell.Display` is for binary PowerShell modules that use ConsoleMonster without the ConsoleMonster module. Its `HostOutput` class writes widgets and markup from a compiled cmdlet the way ConsoleMonster's own commands do: drawn directly on a local console, through the host for transcripts, the ISE, jobs, remoting and redirected output, and above a running live area instead of under it. It targets .NET Framework 4.7.2, .NET 8 and .NET 10, so one build works in Windows PowerShell 5.1 and PowerShell 7.

```terminal
dotnet add package ConsoleMonster.PowerShell.Display
```

With `using System.Management.Automation;`, `using ConsoleMonster.PowerShell.Display;` and `using ConsoleMonster.Widgets;`:

```csharp
[Cmdlet(VerbsLifecycle.Invoke, "Build")]
public sealed class InvokeBuildCommand : PSCmdlet {
    protected override void EndProcessing() {
        Table table = new Table().AddColumn("Step").AddColumn("Took");
        table.AddRow("Restore", "2.1s").AddRow("Build", "14s");
        HostOutput.Write(this, table);
        HostOutput.WriteMarkup(this, "[pass]Build finished[/] in [bold]16s[/]");
        HostOutput.Run(this, () => WriteWarning("Two analyzers were skipped."));
    }
}
```

- `HostOutput.Write(cmdlet, widget)` and `HostOutput.WriteMarkup(cmdlet, markup)` pick the route by the cmdlet's host. `NO_COLOR` and plain mode give plain lines everywhere.
- `HostOutput.Run(cmdlet, action)` runs the cmdlet's own host writes (`WriteWarning`, `WriteVerbose`, a `ShouldContinue` prompt) with a running live area out of the way, then draws the area again below them.
- `HostOutput.CanRenderLive(cmdlet)` tells you whether a live session can draw here; when it is false, write progress as lines.

The same package holds the optional default display adapter for product modules.