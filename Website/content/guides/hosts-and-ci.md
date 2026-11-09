---
title: Hosts, CI and transcripts
description: What ConsoleMonster writes on a real console, in CI, in background jobs, remoting and SSH, the ISE, transcripts and redirected output, and with NO_COLOR.
slug: hosts-and-ci
card: threads-ci
order: 5
---

ConsoleMonster draws directly only on a real local interactive console. Everywhere else the cmdlets write through the PowerShell host, line by line as `Write-Host` does, so jobs complete, transcripts record and redirection captures whole lines.

| Where the script runs | What ConsoleMonster writes |
|---|---|
| A real local console | Drawn directly, with colour and live dashboards. |
| A real console with `Start-Transcript` running | Coloured text through the host UI; the transcript receives the same lines as plain text. |
| The ISE | Coloured text through the host UI. Prompts ask through the host. |
| Background jobs and remoting | `Write-Host` style information records (tag `PSHOST`) with foreground and background colours. |
| Redirected output, thread jobs and other hosts | One plain record per line, so files, transcripts and redirected streams get whole lines of plain text. |
| `NO_COLOR`, `CONSOLEMONSTER_COLOR=none`, plain mode | No colour on any route. |

Colours on the host routes map to the 16 console colours by role: pass green, fail red, warn yellow, muted dark grey and accent cyan; the light theme uses the darker variants. A live dashboard in those hosts narrates as append-only lines.

## Background jobs

{{< cm-frame ids="job-ps5,job-ps7" caption="The same job received in Windows PowerShell 5.1 and PowerShell 7, captured by the site tool in a pseudo console." >}}

## CI

When a CI system is detected (`CI`, GitHub Actions, Azure Pipelines, Jenkins, GitLab or TeamCity) or output is redirected, live sessions switch to stream mode: one line per meaningful change, a heartbeat summary every ten seconds and `[mm:ss]` prefixes in CI, where log viewers often lack their own timestamps.

To use it in a pipeline:

1. Install the module in the job. On GitHub Actions, with PowerShell 7:

   ```yaml
   - name: Checks
     shell: pwsh
     run: |
       Install-Module ConsoleMonster -Scope CurrentUser -Force
       Import-Module ConsoleMonster
       ./checks.ps1
   ```

2. Nothing else to configure: GitHub Actions sets `CI`, so live output streams as lines. Other systems are detected by their own variables, and `CI=true` works for any of them.

3. For logs with ASCII glyphs and no escape sequences, also set `CONSOLEMONSTER_MODE: plain` under the step's `env:`.

{{< cm-frame id="plain" caption="`Show-ConsoleTable` with `CONSOLEMONSTER_MODE=plain`: ASCII glyphs and no escape sequences, for logs that keep every byte." >}}

Text that reaches stream and plain output is sanitised: escape sequences and control characters in log messages, captured lines or data are removed, so plain mode never emits an escape sequence.

## Native CI logs and job summaries

Groups, annotations and job summaries use the provider's own protocol when `GITHUB_ACTIONS=true`, `TF_BUILD=true` or `GITLAB_CI=true`. Other CI systems keep ordinary output. `CONSOLEMONSTER_CI=github`, `azure` or `gitlab` forces that provider; `none` disables provider commands. This choice is independent of colour, glyphs and stream mode. GitLab's section markers deliberately contain the control sequences required by its log viewer, including in plain mode.

In a PowerShell terminal:

```terminal
Import-Module ConsoleMonster

Use-ConsoleGroup 'Build' {
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-ConsoleAnnotation -Level Error -Title 'Build' -Message 'The build failed'
    }
}

Write-ConsoleAnnotation -Level Warning -Message 'Configuration is missing a timeout' -File ./build.ps1 -Line 12 -Column 5

@(
    [pscustomobject]@{ Check = 'Build'; Result = 'Passed' }
    [pscustomobject]@{ Check = 'Tests'; Result = 'Passed' }
) | Add-ConsoleJobSummary
```

| Provider | Groups | Annotations | Summary |
| --- | --- | --- | --- |
| GitHub Actions | Collapsible log groups | Error, Warning and Notice, with optional title and source position | Appends Markdown to `GITHUB_STEP_SUMMARY` |
| Azure Pipelines | Collapsible log groups | Error and Warning with source position; Notice is an ordinary line | Uploads a UTF-8 Markdown file to the run's Extensions tab |
| GitLab CI | Sections collapsed by default | Styled log lines | Returns Markdown for the script to save |
| Terminal or other CI | Muted heading | Styled status line; source links where supported | Returns Markdown without writing a file |

Groups nest and close when an exception unwinds the script. Sequential `Invoke-ConsoleRun` puts each item's work and result inside a group. Parallel runs and `Start-ConsoleRun` group each completed item's result atomically, so groups cannot cross when workers finish out of order. Output emitted independently by parallel workers keeps its existing routing; use `Use-ConsoleGroup` for a serial block whose entire log should fold together.

`Invoke-ConsolePester` automatically annotates failing tests, with the assertion's file and line, and adds its results-by-file table and failures to the GitHub or Azure summary. `-NoJobSummary` turns off that summary. Pester's result object, throw policy, XML result file and exit code keep their native behavior. Each invocation emits at most ten automatic annotations, followed by one line counting the remainder; all failures remain in the summary. With `Enable-ConsoleFormatting`, the Errors feature also annotates displayed script errors, including terminating errors with a script position, capped at ten per output pipeline. Explicit `Write-ConsoleAnnotation` calls have no ConsoleMonster limit. An annotation alone does not set an exit code.

GitHub summary writes append. Azure retains a separate `consolemonster-summary-*.md` file for each call in `SYSTEM_DEFAULTWORKINGDIRECTORY` (or the temporary directory when forced outside an agent). Keep those files available until the agent uploads them; the job workspace owns their cleanup. GitLab and terminal callers can save the returned Markdown themselves:

```powershell
$env:CONSOLEMONSTER_CI = 'none'
Get-Process | Select-Object -First 5 Name, Id | Add-ConsoleJobSummary | Set-Content ./summary.md
```

The C# API shares those transports. `AddSummary(string)` accepts Markdown; the widget overload uses the existing optional Markdown exporter, without adding a dependency to the core package:

```csharp
using ConsoleMonster;
using ConsoleMonster.Markdown;
using ConsoleMonster.Widgets;

using (ContinuousIntegration.Group("Build")) {
    ContinuousIntegration.Annotate(AnnotationLevel.Warning, "Review this setting", file: "build.cs", line: 12);
}
var table = new Table();
table.AddColumn("Check");
table.AddColumn("Result");
table.AddRow("Build", "Passed");
string markdown = ContinuousIntegration.AddSummary(table, provider: new MarkdownFrameExportProvider());

// A host can override detection for one async flow, then restore the previous choice.
using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.None)) {
    ContinuousIntegration.Annotate(AnnotationLevel.Notice, "Local run");
}
```

The wire formats follow [GitHub workflow commands](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands), [Azure logging commands](https://learn.microsoft.com/en-us/azure/devops/pipelines/scripts/logging-commands?view=azure-devops) and [GitLab custom collapsible sections](https://docs.gitlab.com/ci/jobs/job_logs/#create-custom-collapsible-sections). GitHub and Azure escape their own property and message delimiters. Azure command-shaped text in headings, notices and rendered log data is neutralized before writing. ConsoleMonster owns Pester's CI annotations even when its live adapter is disabled, so the provider override and annotation limit apply to fallback runs too.

## Prompts when nobody can answer

| Situation | What prompts do |
|---|---|
| CI, redirected input, `pwsh -NonInteractive`, `CONSOLEMONSTER_NONINTERACTIVE=1` | Return the default, or fail with a message that names the parameter to pass. |
| Remoting, the ISE | Ask through the host: numbered choices and `PromptForChoice`. |
| `CONSOLEMONSTER_ACCESSIBLE=1` | Plain numbered questions with no redraws; every answer is confirmed as text. |
| `CONSOLEMONSTER_NOMOUSE=1` | The usual prompts, with the mouse left to the terminal (selection, scrollback). `InteractiveOptions.Default.Mouse = false` does the same in C#. |

## Known limits

- Windows Terminal does not expose its colour scheme, so its light schemes need `CONSOLEMONSTER_THEME=light`.
- Output that was already printed cannot re-flow on a later resize. Use `-Width` or a fixed profile when output must fit a known width.
- Transcript detection, and transcript writing on 5.1, use guarded reflection into PowerShell internals; it does nothing if a future version renames those members.

## Related

To pin any of these decisions, use the [environment overrides](/docs/environment/); [capability detection](/docs/capabilities/) explains how each one is made.
