---
title: Documentation
description: Install ConsoleMonster, see how it detects the console, choose a theme, override detection, and run, record and replay live sessions.
slug: index
card: size-100x30
order: 0
layout: docsindex
---

ConsoleMonster is a .NET library (netstandard2.0, net472, net8.0 and net10.0) with no package dependencies, and a PowerShell module of thin cmdlets over it for Windows PowerShell 5.1 and PowerShell 7. Each layer can be used without the ones above it:

| Layer | What it does |
|---|---|
| Terminal | Capability detection, Unicode output through `WriteConsoleW` on Windows consoles, UTF-8 when redirected, Ctrl+C restore. |
| Text and style | Display widths (Unicode 17), Spectre-compatible markup, semantic themes, glyph sets. |
| Canvas | A cell buffer, braille and half-block pixel canvases, frame diffs. |
| Layout and widgets | Rows, columns, grids, panels and every widget, with cell presenters shared by tables, cards, key/value lists and trees. |
| Live | `LiveSession`: the update queue, about 15 frames a second, resize handling, console capture, record and replay. |
| Surfaces | The C# object model, a fluent builder and the PowerShell cmdlets. |

[Talks and demos](/docs/talks-and-demos/) covers scripted typing, presenter keys and image/video exports for slides.
