---
title: Coming from another tool
description: Find familiar APIs, compare runnable examples, and check the behaviour that changes when moving to ConsoleMonster.
slug: coming-from
order: 1
group: from
toc: false
card: migration-report-100
---

Choose the tool you use today. Each guide maps familiar commands to ConsoleMonster, shows equivalent operations on sample admin data, and calls out features that need a different approach. You can move one output or interaction at a time; none of these guides requires changing your data collection code.

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Coming from | Start here | Main change |
| --- | --- | --- |
| Spectre.Console in C# | [Rendering, prompts and live output](/guides/spectre-console/) | Compose widgets and update live handles. |
| PwshSpectreConsole | [PowerShell command mappings](/guides/pwshspectreconsole/) | Use Show for display, New for definitions and Read for answers; also run on Windows PowerShell 5.1. |
| Write-Color from PSWriteColor | [Coloured messages and logs](/guides/write-color/) | Keep the pieces-and-colours call; colour by theme role and align in terminal cells. |
| Format-Table, Format-List and Format-Wide | [Tables and records](/guides/format-table/) | Keep typing them with `Enable-ConsoleFormatting`, or use `Show-ConsoleTable` for column options. |
| Out-GridView, GraphicalTools or ConsoleGuiTools | [Grids and object exploration](/guides/microsoft-grid-tools/) | Keep selection in the terminal and pass ordinary objects onward. |
| Show-Command | [Forms and command previews](/guides/show-command/) | Define the questions explicitly; decide separately whether to execute. |
| System.CommandLine.Rendering | [C# rendering migration](/guides/system-commandline-rendering/) | Replace the retired rendering package while retaining your command-line parser. |

</div>

The Microsoft guides distinguish Windows-only commands from deprecated packages and an archived repository. Each includes primary sources and a verification date. Spectre.Console and PwshSpectreConsole are separate, active projects; these guides make no claim that either has stopped development.

## Try the output first

This small inventory report uses the same object rows as the PowerShell comparisons. The output is rendered from the complete program below, in the site's normal capture pipeline.

{{< cm-try ids="migration-report-100" project="InventoryReport" >}}

## What ConsoleMonster adds

Most migrations start with tables and prompts. These are the parts people usually don't expect from a terminal library:

- **One model for C# and PowerShell.** Every widget has a C# object, a fluent form and a cmdlet that draw the same cells; the [widget gallery](/widgets/) shows all three for each one, on PowerShell 7 and Windows PowerShell 5.1.
- **Live runs, not just progress bars.** Per-item lanes, phase strips that fan out to their items, log tails with source tags and filters, and dashboards that keep a summary in the scrollback when they finish. See [live dashboards](/live/) and [large runs](/docs/large-runs/).
- **Full-screen apps.** Forms, grids, tabs, dialogs, menus, focus and mouse, with a command palette and a footer of key hints. See [interactive apps](/docs/interactive-apps/).
- **Testable and recordable.** The same app runs on a headless terminal for golden-file tests, records to asciicast, GIF, APNG or an HTML player, opens in a local browser page, and can be driven by an agent through a debug socket. See [test, record and drive apps](/docs/testing/) and [recording and replay](/docs/recording/).
- **Adaptive output.** Colour depth, glyph set, width, language and redirection are detected, so the same script prints a rich report in Windows Terminal, plain text in a CI log and readable output over SSH. See [capabilities](/docs/capabilities/).
- **No package dependencies** in the core library, which targets netstandard2.0, net472, net8.0 and net10.0.

## Next

- [Install ConsoleMonster](/docs/install/).
- [Everyday PowerShell output](/guides/everyday-powershell/): readable output from the commands you already type.
- [The widget gallery](/widgets/): every widget in PowerShell, C# and C# fluent.
