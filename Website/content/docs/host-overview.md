---
title: Host overview
description: Show a compact, read-only host summary with resource bars and optional sections for a terminal or SSH login.
slug: host-overview
order: 54
group: shells
card: host-overview-120
chips: [PowerShell, Host, Read-only]
apis: [Show-ConsoleOverview, OverviewCollector, OverviewWidget]
summary: A read-only login summary with resource bars, optional sections and a snapshot for scripts.
---

`Show-ConsoleOverview` prints a host summary with CPU and memory bars, local disks, network addresses and the optional information your account can read. It runs once, without elevating or changing the machine, so it suits a terminal profile or an SSH login.

## Try it

On your own machine it takes two lines:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Show-ConsoleOverview
```

The frame below comes from a C# program with fixed illustrative data (reserved example addresses and synthetic users), drawn by the same `OverviewWidget` the command uses, so it looks the same everywhere:

{{< cm-try key="overview" ids="host-overview-120" project="HostOverview" >}}

## How it reads

The title shows the host, OS and version, uptime and local time. Panels use two columns at 80 columns or more, fitting within 120 columns and 24 rows. Below 80 columns they stack at their natural height, so every volume bar and the three login rows stay visible and the terminal scrolls. Disk bars turn amber at 80% used and red at 90%; the words `high` and `critical` keep those states readable without colour. Large sections in the wide view show `+N more`, and clipped disk sections keep the fullest volumes first.

## Choose sections

Name the sections you want, in the order you want them. Fewer sections give each more room:

```powershell
Show-ConsoleOverview -Section Resources,Disks,Network
Show-ConsoleOverview -Section Services,Updates -Width 60
```

The built-in keys are `Resources`, `Disks`, `Network`, `Services`, `Updates` and `Security`. Unknown keys are an error. Sections that are unavailable on a host disappear instead of filling the screen with placeholders.

{{< cm-frame id="host-overview-60" script="false" caption="The same data below 80 columns: the panels stack at their natural height." >}}

## Use the data, or keep it out of the way

`-AsObject` returns the snapshot for another report on PowerShell 5.1 and 7. In a terminal profile, keep collection inside an interactive-session check:

```powershell
$snapshot = Show-ConsoleOverview -Section Resources,Disks -AsObject
$snapshot.Sections | ForEach-Object {
    $_.Items | Select-Object Label,Value,Percentage
}

# In $PROFILE: only for an interactive session whose output is not redirected.
if ([Environment]::UserInteractive -and -not [Console]::IsOutputRedirected) {
    Import-Module ConsoleMonster
    Show-ConsoleOverview -Section Resources,Disks,Network -BudgetSeconds 0.5
}
```

## What it reads, and how long it may take

The default collection budget is 750 ms; `-BudgetSeconds` accepts 0.001–0.9 seconds. A section still collecting at the deadline shows `…` (`...` in ASCII). Each probe receives a cancellation token with a timeout of at most two seconds. The waiting budget excludes OS scheduling and terminal rendering. Built-ins launch no child processes. Repeated calls share any in-flight provider, so a blocked native call cannot create another copy on each login.

| System | What it shows |
|---|---|
| Windows | CPU and memory, the RPC, Event Log and Task Scheduler services, pending updates and reboot from Windows Update's cached data, failed logons in the last 24 hours and the last three interactive logons when your account can read the Security log |
| Linux | CPU and memory from `/proc`; on systemd, the journal, D-Bus, SSH and cron services; cached update and reboot status on Debian and Ubuntu; failed and accepted SSH logons from a readable `/var/log/auth.log` |
| macOS | CPU and available memory; no service, update or security panels |

On every system it shows local fixed volumes and the addresses of active network interfaces. Panels with no readable source are hidden.

## In your own application

In the project from Try it, replace `Program.cs` with the collector to show the real host through the same core API:

```csharp
using ConsoleMonster;
using ConsoleMonster.Overview;

var snapshot = await new OverviewCollector().CollectAsync(new OverviewOptions {
    Sections = new[] { "Resources", "Disks", "Network" },
    CollectionBudget = TimeSpan.FromMilliseconds(500)
});
Terminal.Write(new OverviewWidget(snapshot));
```

Register sections of your own with the fluent registry API. A private registry collects only its providers; registering on `OverviewSectionRegistry.Default` also makes the section available to `Show-ConsoleOverview -Section` in that process:

```csharp
using ConsoleMonster;
using ConsoleMonster.Overview;

var registry = new OverviewSectionRegistry()
    .Register("Worker", "Background worker", token => {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<OverviewSection?>(new OverviewSection(
            "Worker", "Background worker",
            new[] { new OverviewItem("Queue", "3 waiting") }));
    });

var snapshot = await new OverviewCollector(registry).CollectAsync();
Terminal.Write(new OverviewWidget(snapshot));
```

Providers return `null` when unavailable, honour cancellation, release their resources and keep collection read-only. Text is displayed literally. Providers run on worker threads, so PowerShell script blocks that need the caller's runspace cannot be probe delegates.

## Next

- [Linux and SSH](/docs/linux-and-ssh/): show the overview at an SSH login.
- [Capability detection](/docs/capabilities/): what ConsoleMonster decides about each terminal.
