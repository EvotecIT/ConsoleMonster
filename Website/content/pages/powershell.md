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

That is the 2.0 install once 2.0 is published. It is not on the PowerShell Gallery yet; the Gallery's current release is the 0.0.x module it replaces.

The scripts above run with the module from this repository's source build. Each was started in a Windows pseudo console of a fixed width, with the culture pinned to en-US for both ConsoleMonster and the built-in cmdlets. Live dashboards started with `Start-ConsoleDashboard` are on the [live dashboards page](/live/), and prompts, menus and forms on the [interactive page](/interactive/).