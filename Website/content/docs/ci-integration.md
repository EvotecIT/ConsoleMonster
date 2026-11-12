---
title: CI integration
description: "Collapsible log groups, annotations and job summaries on GitHub Actions, Azure Pipelines and GitLab CI, with readable output in a terminal."
slug: ci-integration
order: 51
group: shells
card: threads-ci
apis: [Use-ConsoleGroup, Write-ConsoleAnnotation, Add-ConsoleJobSummary, ContinuousIntegration]
chips: [GitHub Actions, Azure Pipelines, GitLab CI, Pester]
summary: "Native log groups, annotations and job summaries in CI, with headings and status lines locally."
---

This page shows how a build or test script gets the CI features its provider offers: collapsible sections in the log, errors and warnings pinned to a file and line, and a Markdown summary on the job page. The same script runs unchanged in a terminal, where the groups become headings and the annotations become status lines.

```powershell
Import-Module ConsoleMonster

Use-ConsoleGroup 'Package' {
    Compress-Archive -Path ./src/* -DestinationPath ./package.zip -Force
}
Use-ConsoleGroup 'Check configuration' {
    Write-ConsoleAnnotation -Level Warning -Message 'Timeout is not set; using 30 s' -File ./build.ps1 -Line 12
}
$results = @(
    [pscustomobject] @{ Check = 'Package'; Result = 'Passed' }
    [pscustomobject] @{ Check = 'Configuration'; Result = 'Warning' }
)
$results | Add-ConsoleJobSummary
```

## What each command does

| Command | GitHub Actions | Azure Pipelines | GitLab CI | Terminal |
|---|---|---|---|---|
| `Use-ConsoleGroup` | `::group::` and `::endgroup::` | `##[group]` and `##[endgroup]` | A collapsed section | A muted heading |
| `Write-ConsoleAnnotation` | An error, warning or notice annotation on the file and line | An error or warning issue; a notice is an ordinary line | A status line | A status line, with the file linked where the terminal supports links |
| `Add-ConsoleJobSummary` | Appends to the step summary | Uploads a Markdown summary file | Returns the Markdown | Returns the Markdown |

- **Groups** nest, and they close even when the script block throws. The script block runs with the caller's variables and its output is passed through.
- **Annotations** take `-Level Error`, `Warning` or `Notice`, a `-Message`, and optionally `-Title`, `-File`, `-Line` and `-Column`. Text is escaped for the provider, so a message cannot inject a workflow command. A relative `-File` resolves from the current location; inside the checkout it becomes a checkout-relative path with forward slashes, so the annotation lands on the right line of the pull request. An annotation never changes the exit code or writes to PowerShell's error stream.
- **Job summaries** take widgets or ordinary objects. Objects use the automatic view and several widgets are stacked, then the whole is exported as Markdown with semantic tables, records and trees. On GitHub and Azure nothing is returned; elsewhere the Markdown comes back, so a script can save it.

## Choosing the provider

The provider is detected from `GITHUB_ACTIONS`, `TF_BUILD` or `GITLAB_CI`. `CONSOLEMONSTER_CI` overrides detection with `github`, `azure`, `gitlab` or `none`; `none` turns provider commands off, which keeps a log plain when a script runs inside another tool's CI step.

```yaml
# .github/workflows/build.yml (excerpt)
- name: Build and test
  shell: pwsh
  run: |
    Install-Module ConsoleMonster -Scope CurrentUser -Force
    ./build.ps1
```

## Without changing the script

Several commands already use the provider when it is detected:

- **`Invoke-ConsoleRun`** puts each sequential item's work and result in its own group. With `-Parallel`, each item's result is grouped as it completes.
- **`Invoke-ConsolePester`** annotates each failure with its source position and adds the results table and the failures to the GitHub or Azure job summary. `-NoJobSummary` keeps the annotations and skips the summary. GitLab has no annotation or summary API, so failures stay ordinary log lines.
- **`Enable-ConsoleFormatting`** annotates errors that have a script position, including terminating errors, through its Errors feature.
- **Live dashboards** write whole, ordered lines instead of redrawing, with `[mm:ss]` timestamps when CI is detected.

Automatic annotations stop at ten per run or output pipeline, followed by one line counting the rest, so a run with many failures does not flood the pull request. Annotations written with `Write-ConsoleAnnotation` have no limit.

{{< cm-demo id="threads-ci" heading="h2" >}}

## In C#

The static `ContinuousIntegration` class has the same three operations, with the same detection and fallbacks:

```csharp
using (ContinuousIntegration.Group("Package")) {
    Package();
}
ContinuousIntegration.Annotate(AnnotationLevel.Warning, "Timeout is not set; using 30 s", file: "build.ps1", line: 12);
ContinuousIntegration.AddSummary(resultsTable);
```

`AddSummary` takes Markdown text, or a widget that it exports with the Markdown provider from the `ConsoleMonster.Markdown` package. `ContinuousIntegration.Provider` reports the detected provider, and `using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.None)) { ... }` overrides it for the code inside, which is how a test checks the plain output.

## Next

- [Hosts and CI](/docs/hosts-and-ci/): what ConsoleMonster detects in each host and CI system, and how to pin it.
- [Live runs](/docs/live-runs/): `Invoke-ConsoleRun` and the per-item view that groups itself in CI.
- [Testing](/docs/testing/): check output in tests, including Pester runs.
