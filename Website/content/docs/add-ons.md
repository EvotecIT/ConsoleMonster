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

Optional packages add Markdown, image export, hosting and logging. The `ConsoleMonster` core has no dependencies, so each add-on brings its own: Markdown comes from OfficeIMO and image rendering from ChartForgeX. The PowerShell module includes the Markdown and Graphics add-ons.

## Markdown

Add `ConsoleMonster.Markdown` to a C# application. `MarkdownConsole.Parse` and `Show` read and display documents; `MarkdownExporter.Export` writes widgets, frames and live snapshots; `ExportRecording` writes recordings. The add-on uses OfficeIMO.Markdown's document model for fenced blocks and recording documents. Tables, records and trees keep their cell formatting, groups and totals. See [Markdown display and export](/docs/markdown/).

The PowerShell module includes the add-on and loads it when Markdown is requested. `$rows | Export-ConsoleFrame -Format Markdown` and `Export-ConsoleRecording -Format Markdown` keep the same command names.

## Graphics

`ConsoleMonster.Graphics` exports PNG, GIF and APNG through ChartForgeX 3.1.0. It includes frame and recording exports and slide presets. See [Talks and demos](/docs/talks-and-demos/). It also draws images inline with Kitty, iTerm2 or Sixel pixels, or half blocks; see [Inline images](/docs/inline-images/).

## Hosting and logging

`ConsoleMonster.Hosting` integrates hosted services with a dashboard. `ConsoleMonster.Extensions.Logging` sends structured log entries to a dashboard region. Their dependencies stay in those packages; the core has none. See [Hosting](/docs/hosting/), [Logging](/docs/logging/) and the [dependency list](/docs/install/#optional-add-ons-and-dependencies).

## Next

- [How it works](/docs/how-it-works/): what ConsoleMonster detects and how it draws.
- [Three ways to write it](/docs/csharp-styles/): the same widgets as cmdlets, C# initializers and fluent calls.
