# A public reporter hook for Pester: proposal

Status: draft for a later upstream pull request. Nothing has been opened upstream.

`Invoke-ConsolePester` draws a live dashboard while Pester runs. Pester has no public way to receive test events, so it
uses a guarded adapter onto Pester's internal plugin steps. This document records what was found, the route taken and
why, and the smallest public hook that would let any reporter (ConsoleMonster, IDE integrations, custom CI formats)
drop such adapters.

## What Pester offers today (5.x to 6.2)

Researched from Pester's public documentation (pester.dev, the configuration reference, release notes up to 6.2.0) and
the layout of its source on GitHub. No Pester code is copied here.

- **Configuration.** `Output.Verbosity`, `Output.StackTraceVerbosity`, `Output.CIFormat`, `Output.CILogLevel`,
  `Output.CIDebugOutput`, `Output.RenderMode` and (6.1) `Output.ShowTags` shape Pester's own console output. Results can be
  written as NUnit or JUnit files after the run. None of them call user code while tests run.
- **Events and callbacks.** None are documented. `Debug.ShowStartMarkers` prints start markers; `Pester.BeforeContainer.ps1`
  (6.x) runs set-up code before a container, which is not a notification.
- **Internal plugin system.** Pester's own features (screen output, test results, code coverage, mocks, TestDrive,
  TestRegistry, skip-remaining-on-failure) are plugins: objects made by an internal `New-PluginObject` with script blocks
  for steps such as `DiscoveryEnd`, `ContainerDiscoveryEnd`, `ContainerRunStart`, `EachTestSetupStart`,
  `EachTestTeardownEnd`, `ContainerRunEnd` and `RunEnd`. `Invoke-Pester` builds the list itself; the screen plugin comes
  from an internal `Get-WriteScreenPlugin` when `Output.Verbosity` is not `None`. There is no supported way to add a plugin.
- **Behaviour that matters to a reporter.** Pester 6 discovers and runs container by container, so `DiscoveryEnd` can
  arrive after the first files have run (5.x discovers everything first). With `Run.Parallel` (experimental), files run
  in worker runspaces and only the run-level steps reach the main runspace's reporting plugins. Tests that never ran
  because their block's `BeforeAll` failed get no test steps; they appear as failed in the result object.

## Route taken by Invoke-ConsolePester

The preferred route was a public surface. There is none in Pester 6, so `Invoke-ConsolePester` uses the second route:
a guarded adapter onto the internal plugin steps.

1. **Feature detection** before every run: `New-PluginObject` and `Get-WriteScreenPlugin` exist inside the Pester
   module, `New-PluginObject` accepts the five steps used, `Get-WriteScreenPlugin` takes `-Verbosity`, and `Invoke-Pester`
   still calls it. Any miss means Pester runs unchanged with its own output and a warning naming the reason;
   `Run.Parallel` and `-Output None` also run Pester unchanged.
2. **The adapter** creates its plugin with Pester's own `New-PluginObject`, so the object has the shape Pester expects,
   and replaces `Get-WriteScreenPlugin` inside the module for one `Invoke-Pester` call only, restoring it in a `finally`
   block (also when the run is stopped with Ctrl+C). It uses only the configuration to run Pester (`-Configuration`), so
   filters, test results, coverage and everything else behave as with `Invoke-Pester`.
3. **Events become plain values** (strings, numbers, arrays of block names) before they reach ConsoleMonster, so the
   C# side depends on no Pester type. Tests that never ran under a failed block, and files that failed before running,
   are read from the container tree at `ContainerRunEnd` and from the result object.
4. **Pester's result object** is returned unchanged with `-PassThru` or `Run.PassThru`. `Run.Exit` (`-CI`) and `Run.Throw`
   are applied after the summary, as Pester would apply them.

Rejected alternatives: parsing Pester's console output (it changes between versions and loses structure); running
`Invoke-Pester` once per file (a public API, but no per-test progress, and set-up files and filters would behave
differently); and a custom `Pester.BeforeContainer.ps1` (runs once per container, gives no test events).

Verified with Pester 5.7.1 and 6.1.0 on PowerShell 7, 5.8.0 and 6.0.1 on Windows PowerShell 5.1, and the latest from
PowerShell Gallery in CI. Pester 4 and older are not supported (no plugin steps, different result object).

## Proposal: a minimal public reporter hook

The aim is a small, additive change that exposes what the screen plugin already receives, with a contract Pester can
keep stable.

### Surface

```powershell
$reporter = New-PesterReporter -Name 'ConsoleMonster' -OnEvent {
    param($Event)          # [Pester.ReporterEvent]
    switch ($Event.Kind) { 'TestEnd' { ... } }
}
$configuration = New-PesterConfiguration
$configuration.Output.Reporter = $reporter          # one or more reporters
$configuration.Output.Verbosity = 'None'            # or keep Pester's own output next to the reporter
Invoke-Pester -Configuration $configuration
```

- `New-PesterReporter` is the only new command. A reporter is a name and one script block (or a .NET object
  implementing `Pester.IReporter` with one `OnEvent` method, for C# hosts).
- `Output.Reporter` is a new configuration option taking reporters. Pester's screen output stays controlled by
  `Output.Verbosity`, so a reporter can add to it or replace it (`None`).

### Events

| Kind | When | Payload |
|---|---|---|
| `RunStart` | before discovery | configuration (read-only), Pester version |
| `ContainerDiscovered` | after a container is discovered | container, tests to run in it |
| `DiscoveryEnd` | after discovery of all containers | total tests to run |
| `ContainerStart` / `ContainerEnd` | around a container's run | container; at the end its result and counts |
| `TestStart` / `TestEnd` | around each test | test; at the end its result |
| `BlockFailed` | a setup or teardown outside a test fails | block, error records, tests marked failed because of it |
| `RunEnd` | after the run | the run result object Pester returns |

Every event carries `Kind`, a timestamp and these stable fields, documented as the contract (new fields are added,
never renamed or removed):

- **container:** `Path` (file path or null), `Type` (`File`, `ScriptBlock`), `Name`;
- **test:** `Name` (expanded), `Path` (block names, outermost first, expanded), `Tags`, `File`, `Line`;
- **result:** `Result` (`Passed`, `Failed`, `Skipped`, `Inconclusive`, `NotRun`), `Duration`, `ErrorRecord` (the
  records Pester shows, with `File` and `Line` of the failing line where known), and for assertions `Expected` and
  `Actual` when the assertion knows them.

### Guarantees

- Events arrive in order on the thread that called `Invoke-Pester`, also with `Run.Parallel`: workers forward their
  events (serialised, the same payload) and the main runspace raises them as each worker reports.
- A reporter that throws does not fail the run. Pester reports the first error per reporter once, as a warning, and
  keeps calling it.
- Reporters see tests skipped by `-Skip`, by `Set-ItResult` and by `Run.SkipRemainingOnFailure` with the right result,
  and tests that never ran under a failed block in `BlockFailed` and in `ContainerEnd`.

### Why this shape

- It maps one to one onto the existing plugin steps, so the implementation is small: `New-PesterReporter` builds a
  plugin object from the steps, and `Invoke-Pester` appends the configured reporters to its plugin list next to the
  screen plugin. No change to discovery or execution.
- One script block with an event kind keeps the public surface to one command and one option, and lets Pester add
  event kinds later without breaking reporters (unknown kinds are ignored).
- The payload is a documented projection, not Pester's internal test and block objects, so Pester can keep changing
  its internals.

### What Invoke-ConsolePester does once it exists

It feature-detects `New-PesterReporter`; when present it passes a reporter through `Output.Reporter` and drops the
adapter, including the replacement of `Get-WriteScreenPlugin`. The adapter stays for older Pester versions.
