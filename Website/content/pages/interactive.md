---
title: Prompts, menus and forms
description: Questions, menus, wizards and a grid for the console on Windows PowerShell 5.1 and PowerShell 7. Every screen here was drawn by scripted keys.
slug: interactive
layout: interactive
meta.seo_title: Console prompts, menus and forms | ConsoleMonster
---

Every story below is a complete program, PowerShell first and C# second, and the screens beside it are what that program drew when the keys listed under it were pressed. Lists filter as you type, the footer lists the keys that work right now and `?` shows them all, and answers come back as typed values or as the chosen objects themselves.

To run one yourself:

1. **PowerShell 5.1 or 7:** `Install-Module ConsoleMonster -Scope CurrentUser`, save the script and run it in a console window.
2. **.NET 8 or later:** `dotnet new console`, then `dotnet add package ConsoleMonster`, replace `Program.cs` with the C# program and `dotnet run`.
When nobody can answer (CI, redirected input, `pwsh -NonInteractive`, `CONSOLEMONSTER_NONINTERACTIVE=1`), a prompt returns its default or fails with a message that names the parameter to pass. In remoting and the ISE, prompts ask through the host, and `CONSOLEMONSTER_ACCESSIBLE=1` switches to plain numbered questions.