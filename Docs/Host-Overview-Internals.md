# Host overview internals

How `Show-ConsoleOverview` and `OverviewCollector` collect each section. The site's
[Host overview](https://consolemonster.dev/docs/host-overview/) page covers what to run; this file keeps the sources,
timeouts and limits behind it.

## Budget and cancellation

The default collection budget is 750 ms; `-BudgetSeconds` accepts 0.001 to 0.9 seconds (`OverviewOptions.CollectionBudget`
in C#). A section still collecting at the deadline shows `…` (`...` in ASCII). Each probe receives a cancellation token
with a timeout of at most two seconds. The waiting budget excludes OS scheduling and terminal rendering. Built-ins launch
no child processes. Repeated calls share any in-flight provider, so a blocked native call cannot create another copy on
each login. Native platform calls cannot be forcibly aborted by managed cancellation, so an unexpectedly blocked call
retains its single registry slot until it returns.

## Windows

Windows uses native CPU and memory counters, read-only service status for RPC, Event Log and Task Scheduler, cached
Windows Update install metadata and reboot markers. Its pending-update count queries the installed Windows Update Agent
with online search and automatic agent upgrades disabled, so the count reflects cached applicability data. The native
call is registered for COM cancellation when the probe expires. Security events require existing permission to read the
Security channel; the command never enables privileges or changes audit policy. Failed-logon counts cover the last 24
hours, capped at 1,024 events with a `+` marker; the last three interactive logon events are shown when readable.

## Linux

Linux reads `/proc` CPU and memory counters. Where the operating system already provides libsystemd and a local system
bus, the service panel queries loaded journaling, D-Bus, SSH and cron units through systemd's read-only `GetUnit` and
`ActiveState` calls. Each local method has a 100 ms timeout; no service is loaded or started. Update information uses
update-notifier's cached status, `/run/reboot-required` and the tail of the dpkg installation log where present.
Security uses a bounded 64 KiB tail of readable `/var/log/auth.log`: failed SSH password logons and up to three accepted
SSH logons in the last 24 hours. Its count is labelled as a sample lower bound. Journal-only authentication logs, other
distributions' update systems and non-systemd service managers are not queried. Optional panels stay hidden when no
supported source is readable.

## macOS and shared sources

macOS uses native CPU counters and estimates available memory from free and inactive pages. Optional service, update and
security panels are hidden. Local fixed volumes and active interface addresses use the framework's platform facilities
on all three systems; disconnected network and removable disks are skipped.
