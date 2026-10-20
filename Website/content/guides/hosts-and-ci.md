---
title: Hosts, CI and transcripts
description: What ConsoleMonster writes on a real console, in CI, in background jobs, remoting and SSH, the ISE, transcripts and redirected output, and with NO_COLOR.
slug: hosts-and-ci
order: 5
---

ConsoleMonster draws directly only on a real local interactive console. Everywhere else the cmdlets write through the PowerShell host, line by line as `Write-Host` does, so jobs complete, transcripts record and redirection captures whole lines.

| Where the script runs | What ConsoleMonster writes |
|---|---|
| A real local console | Drawn directly, with colour and live dashboards. |
| A real console with `Start-Transcript` running | Coloured text through the host UI; the transcript receives the same lines as plain text. |
| The ISE | Coloured text through the host UI. Prompts ask through the host. |
| Background jobs and remoting | `Write-Host` style information records (tag `PSHOST`) with foreground and background colours. |
| Redirected output, thread jobs and other hosts | One plain record per line, so files, transcripts and `6>&1` get whole lines of plain text. |
| `NO_COLOR`, `CONSOLEMONSTER_COLOR=none`, plain mode | No colour on any route. |

Colours on the host routes map to the 16 console colours by role: pass green, fail red, warn yellow, muted dark grey and accent cyan; the light theme uses the darker variants. A live dashboard in those hosts narrates as append-only lines.

## Background jobs

{{< cm-frame ids="job-ps5,job-ps7" caption="The same job received in Windows PowerShell 5.1 and PowerShell 7, captured by the site tool in a pseudo console." >}}

## CI

When a CI system is detected (`CI`, GitHub Actions, Azure Pipelines, Jenkins, GitLab or TeamCity) or output is redirected, live sessions switch to stream mode: one line per meaningful change, a heartbeat summary every ten seconds and `[mm:ss]` prefixes in CI, where log viewers often lack their own timestamps.

{{< cm-frame id="plain" caption="Format-ConsoleTable with CONSOLEMONSTER_MODE=plain: ASCII and no escape sequences, for logs that keep every byte." >}}

Text that reaches stream and plain output is sanitised: escape sequences and control characters in log messages, captured lines or data are removed, so plain mode never emits an escape sequence.

## Prompts when nobody can answer

| Situation | What prompts do |
|---|---|
| CI, redirected input, `pwsh -NonInteractive`, `CONSOLEMONSTER_NONINTERACTIVE=1` | Return the default, or fail with a message that names the parameter to pass. |
| Remoting, the ISE | Ask through the host: numbered choices and `PromptForChoice`. |
| `CONSOLEMONSTER_ACCESSIBLE=1` | Plain numbered questions with no redraws; every answer is confirmed as text. |

## Known limits

- Windows Terminal does not expose its colour scheme, so its light schemes need `CONSOLEMONSTER_THEME=light`.
- Output that was already printed cannot re-flow on a later resize. Use `-Width` or a fixed profile when output must fit a known width.
- Transcript detection, and transcript writing on 5.1, use guarded reflection into PowerShell internals; it does nothing if a future version renames those members.