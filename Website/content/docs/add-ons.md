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

`ConsoleMonster.Graphics` exports PNG, GIF and APNG through ChartForgeX 3.1.0. It includes frame and recording exports and slide presets. See [Talks and demos](/docs/talks-and-demos/).

## Hosting and logging

`ConsoleMonster.Hosting` integrates hosted services with a dashboard. `ConsoleMonster.Extensions.Logging` sends structured log entries to a dashboard region. Their dependencies stay in those packages; the core has none. See [Hosting](/docs/hosting/), [Logging](/docs/logging/) and the [dependency list](/docs/install/#optional-add-ons-and-dependencies).
