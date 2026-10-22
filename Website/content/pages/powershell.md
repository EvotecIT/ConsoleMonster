---
title: ConsoleMonster in PowerShell
description: Cmdlets for tables, trees, panels, charts, prompts and live dashboards, with their real output on Windows PowerShell 5.1 and PowerShell 7.
slug: powershell
layout: powershell
meta.seo_title: ConsoleMonster PowerShell module for 5.1 and 7
---

## Install

1. Install the module for your user (Windows PowerShell 5.1 or PowerShell 7):

   ```powershell
   Install-Module ConsoleMonster -Scope CurrentUser
   ```

2. Import it in the session, or in your profile (`$PROFILE`) for every session:

   ```powershell
   Import-Module ConsoleMonster
   ```

ConsoleMonster 2.0 is on the PowerShell Gallery and replaces the 0.0.x module. To make ordinary output readable for the whole session, add `Enable-ConsoleFormatting`; the [everyday output guide](/guides/everyday-powershell/) shows it before and after, and [PowerShell conventions](/docs/powershell-conventions/) explains how every widget command reads.

Each script below is complete: save it and run it. For this page each one ran in a Windows pseudo console of a fixed width, on both hosts, with the module built from the same source as this site and the culture pinned to en-US for ConsoleMonster and the built-in cmdlets. Live dashboards started with `Start-ConsoleDashboard` are on the [live dashboards page](/live/), and prompts, menus and forms on the [interactive page](/interactive/).