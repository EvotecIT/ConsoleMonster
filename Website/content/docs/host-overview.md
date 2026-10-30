---
title: Host overview
description: Show a compact, read-only host summary with resource bars and optional sections for a terminal or SSH login.
slug: host-overview
order: 14
group: hosts
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

The default collection budget is 750 ms; `-BudgetMilliseconds` accepts 1–900 ms. A section still collecting at the deadline shows `…` (`...` in ASCII). Each probe receives a cancellation token with a timeout of at most two seconds. The waiting budget excludes OS scheduling and terminal rendering. Built-ins launch no child processes. Repeated calls share any in-flight provider, so a blocked native call cannot create another copy on each login.

Windows uses native CPU and memory counters, read-only service status for RPC, Event Log and Task Scheduler, cached Windows Update install metadata and reboot markers. Its pending-update count queries the installed Windows Update Agent with online search and automatic agent upgrades disabled, so the count reflects cached applicability data. The native call is registered for COM cancellation when the probe expires. Security events require existing permission to read the Security channel; the command never enables privileges or changes audit policy. Failed-logon counts cover the last 24 hours, capped at 1,024 events with a `+` marker; the last three interactive logon events are shown when readable.

Linux reads `/proc` CPU and memory counters. Where the operating system already provides libsystemd and a local system bus, the service panel queries loaded journaling, D-Bus, SSH and cron units through systemd's read-only `GetUnit` and `ActiveState` calls. Each local method has a 100 ms timeout; no service is loaded or started. Update information uses update-notifier's cached status, `/run/reboot-required` and the tail of the dpkg installation log where present. Security uses a bounded 64 KiB tail of readable `/var/log/auth.log`: failed SSH password logons and up to three accepted SSH logons in the last 24 hours. Its count is labeled as a sample lower bound. Journal-only authentication logs, other distributions' update systems and non-systemd service managers are not queried. Optional panels stay hidden when no supported source is readable.

macOS uses native CPU counters and estimates available memory from free and inactive pages. Optional service, update and security panels are hidden. Local fixed volumes and active interface addresses use the framework's platform facilities on all three systems; disconnected network and removable disks are skipped. Native platform calls cannot be forcibly aborted by managed cancellation, so an unexpectedly blocked call retains its single registry slot until it returns.

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

## Related

[Choose a theme](/docs/themes/), [pin a terminal profile](/docs/capabilities/), show it [at an SSH login](/docs/linux-and-ssh/), or browse the [PowerShell command reference](/powershell/commands/).
