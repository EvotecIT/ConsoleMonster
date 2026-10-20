---
title: ConsoleMonster in PowerShell
description: Cmdlets for tables, trees, panels, charts, prompts and live dashboards, with their real output on Windows PowerShell 5.1 and PowerShell 7.
slug: powershell
layout: powershell
meta.seo_title: ConsoleMonster PowerShell module for 5.1 and 7
---

## Install

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
```

ConsoleMonster 2.0 is on the PowerShell Gallery and replaces the 0.0.x module. To make ordinary output readable for the whole session, add `Enable-ConsoleFormatting`; the [everyday output guide](/guides/everyday-powershell/) shows it before and after, and [PowerShell conventions](/docs/powershell-conventions/) explains how every widget command reads.

The scripts above run with the module from this repository's source build. Each was started in a Windows pseudo console of a fixed width, with the culture pinned to en-US for both ConsoleMonster and the built-in cmdlets. Live dashboards started with `Start-ConsoleDashboard` are on the [live dashboards page](/live/), and prompts, menus and forms on the [interactive page](/interactive/).