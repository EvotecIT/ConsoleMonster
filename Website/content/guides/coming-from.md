---
title: Coming from another tool
description: Find familiar APIs, compare runnable examples, and check the behavior that changes when moving to ConsoleMonster.
slug: coming-from
order: 1
toc: false
card: migration-report-100
---

Choose the tool you use today. Each guide maps familiar commands to ConsoleMonster, shows equivalent operations on sample admin data, and calls out features that need a different approach. You can move one output or interaction at a time; none of these guides requires changing your data collection code.

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Coming from | Start here | Main change |
| --- | --- | --- |
| ConsoleMonster 2.0.x | [Moving from 2.0](/docs/moving-from-2/) | Replace removed commands with their 2.1 equivalents. |
| Spectre.Console in C# | [Rendering, prompts and live output](/guides/spectre-console/) | Compose widgets and update live handles. |
| PwshSpectreConsole | [PowerShell command mappings](/guides/pwshspectreconsole/) | Use Show for display, New for definitions and Read for answers; also run on Windows PowerShell 5.1. |
| Out-GridView, GraphicalTools or ConsoleGuiTools | [Grids and object exploration](/guides/microsoft-grid-tools/) | Keep selection in the terminal and pass ordinary objects onward. |
| Show-Command | [Forms and command previews](/guides/show-command/) | Define the questions explicitly; decide separately whether to execute. |
| System.CommandLine.Rendering | [C# rendering migration](/guides/system-commandline-rendering/) | Replace the retired rendering package while retaining your command-line parser. |

</div>

The Microsoft guides distinguish Windows-only commands from deprecated packages and an archived repository. Each includes primary sources and a verification date. Spectre.Console and PwshSpectreConsole are separate, active projects; these guides make no claim that either has stopped development.

## Try the output first

This small inventory report uses the same object rows as the PowerShell comparisons. The output is rendered from the complete program below, in the site's normal capture pipeline.

{{< cm-try ids="migration-report-100" project="InventoryReport" >}}

## Next

[Install ConsoleMonster](/docs/install/), explore the [widget gallery](/widgets/), or compare [native Format-Table](/guides/format-table/) and [Write-Color](/guides/write-color/).
