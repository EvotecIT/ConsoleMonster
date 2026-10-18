---
title: Prompts, menus and forms
description: Questions, menus, wizards and a grid for the console on Windows PowerShell 5.1 and PowerShell 7. Every screen here was drawn by scripted keys.
slug: interactive
layout: interactive
meta.seo_title: Console prompts, menus and forms | ConsoleMonster
---

The site tool runs each example with a scripted key sequence and draws it into a `ScreenCapture`, the same in-memory surface ConsoleMonster's tests and exports use. Lists filter as you type, the footer lists the keys that work right now and `?` shows them all, and answers come back as typed values or as the chosen objects themselves.

When nobody can answer (CI, redirected input, `pwsh -NonInteractive`, `CONSOLEMONSTER_NONINTERACTIVE=1`), a prompt returns its default or fails with a message that names the parameter to pass. In remoting and the ISE, prompts ask through the host, and `CONSOLEMONSTER_ACCESSIBLE=1` switches to plain numbered questions.