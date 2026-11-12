---
title: Your first live dashboard
description: "Go from an empty script to a dashboard that updates while work runs: a lane per server, a log panel, parallel workers, Ctrl+C and a recording to replay."
slug: first-dashboard
group: tasks
order: 11
card: session
---

This guide turns a script that deploys three servers into a dashboard that shows each server's progress and a log while the work runs, then records the run so it can be replayed or shared. The deploy is simulated with short pauses, so nothing is changed anywhere.

The steps build the program up one piece at a time; the finished program and a recording of it running are at the end.

## 1. Describe the screen

A dashboard is a layout like any other, defined before anything runs. Give each widget that will change a name (`-WidgetId`), because the work addresses its updates to that name. Here a panel of lanes shows one progress row per server and a log panel takes the rest of the height.

<div class="cm-lang" data-lang="powershell">

```powershell
Import-Module ConsoleMonster

$dashboard = New-ConsoleDashboard -Title 'Deploy' -Content {
    New-ConsolePanel -Title 'Servers' -HeaderRight '3 workers' -Content {
        New-ConsoleLanes -WidgetId servers -Content {
            foreach ($server in 'SRV1', 'SRV2', 'SRV3') { @{ Key = $server; Total = 4 } }
        }
    }
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Lanes lanes = new Lanes();
Dashboard dashboard = Ui.Dashboard().Title("Deploy")
    .Content(body => body
        .Panel(new Panel(lanes, "Servers") { HeaderRight = "3 workers" })
        .Add(new Panel(new LogRegion(), "Log").Fill()));
string[] steps = { "copying files", "stopping the site", "swapping folders", "starting the site" };
foreach (string server in new[] { "SRV1", "SRV2", "SRV3" })
{
    lanes.Add(server, server, steps.Length);
}
```

</div>

`Total = 4` is the number of steps each lane counts to. `-Fill` gives the log panel the height the other panels leave.

## 2. Run the work inside it

`Invoke-ConsoleDashboard` shows the dashboard and runs the script block. Inside it, `Update-Console` moves a lane on: `-Status Running -Detail` shows the current step, `-Result Pass` counts a finished step, and `-Complete` closes the lane. `Write-Host`, warnings, errors and verbose output land in the log panel instead of writing over the screen. In C#, `LiveSession.Run` does the same, the lanes' handle posts the updates and `Console.WriteLine` goes to the log.

<div class="cm-lang" data-lang="powershell">

```powershell
Invoke-ConsoleDashboard $dashboard {
    foreach ($server in 'SRV1', 'SRV2', 'SRV3') {
        foreach ($step in 'copying files', 'stopping the site', 'swapping folders', 'starting the site') {
            Update-Console -WidgetId servers -Key $server -Status Running -Detail $step
            Start-Sleep -Milliseconds 500
            Update-Console -WidgetId servers -Key $server -Result Pass
        }
        Write-Host "$server done"
        Update-Console -WidgetId servers -Key $server -Complete
    }
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
using (LiveSession session = new LiveSession(dashboard))
{
    session.Run(s =>
    {
        LanesHandle servers = lanes.Handle(s);
        foreach (string server in new[] { "SRV1", "SRV2", "SRV3" })
        {
            foreach (string step in steps)
            {
                servers.Start(server, step);
                Thread.Sleep(500);
                servers.Advance(server, StatusKind.Pass);
            }
            Console.WriteLine(server + " done");
            servers.Complete(server);
        }
    });
}
```

</div>

The script block runs in a runspace of its own, so variables from the rest of the script are not visible inside it. Read one with `$using:name`, as with `ForEach-Object -Parallel`, pass values with `-ArgumentList` and a `param()` block, or bring functions in with `-ImportFunction`. However many updates arrive, the screen is redrawn about 15 times a second and only the cells that changed are written.

## 3. Run the servers in parallel

Pipe the servers to `Invoke-ConsoleDashboard -Parallel`: the script block runs once per item, each in its own runspace, up to `-ThrottleLimit` at a time, and receives the item as its first argument. Each worker updates only its own lane, so no locking is needed. In C#, the lanes' handle is safe to use from any thread.

<div class="cm-lang" data-lang="powershell">

```powershell
'SRV1', 'SRV2', 'SRV3' | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 3 {
    param($Server)
    foreach ($step in 'copying files', 'stopping the site', 'swapping folders', 'starting the site') {
        Update-Console -WidgetId servers -Key $Server -Status Running -Detail $step
        Start-Sleep -Milliseconds (400 + (Get-Random -Minimum 0 -Maximum 500))
        Update-Console -WidgetId servers -Key $Server -Result Pass
    }
    Write-Host "$Server done"
    Update-Console -WidgetId servers -Key $Server -Complete
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
session.Run(s =>
{
    LanesHandle servers = lanes.Handle(s);
    Parallel.ForEach(new[] { "SRV1", "SRV2", "SRV3" }, server =>
    {
        foreach (string step in steps)
        {
            servers.Start(server, step);
            Thread.Sleep(400 + Random.Shared.Next(500));
            servers.Advance(server, StatusKind.Pass);
        }
        Console.WriteLine(server + " done");
        servers.Complete(server);
    });
});
```

</div>

[Threads and concurrency](/docs/threads/) explains what is safe from which thread.

## 4. Ctrl+C and the end of the run

The dashboard owns the screen only while the work runs:

- **When the work ends**, the screen is restored and the dashboard's final state is printed to the scrollback, so the result stays readable after the run. `-Summary` prints a widget of your own instead, and `-PrintFinalState:$false` leaves nothing behind.
- **Ctrl+C** stops the script and always restores the terminal, then prints the final state as above. In C#, the first Ctrl+C cancels `session.Cancellation`; check it in long loops (`s.Cancellation.ThrowIfCancellationRequested()`) so the work stops promptly.
- **Errors** in the script restore the terminal too, and problems such as an update for a name the dashboard does not have are reported once the screen is back.
- **`-Inline`** draws the dashboard below the cursor instead of on its own screen, with output scrolling above it.

```powershell
$summary = New-ConsoleText -Markup '[pass]Deploy finished[/] on 3 servers'
'SRV1', 'SRV2', 'SRV3' | Invoke-ConsoleDashboard $dashboard -Parallel -Summary $summary {
    param($Server)
    Update-Console -WidgetId servers -Key $Server -Complete
}
```

## 5. Record the run and replay it

`-RecordingPath` writes every update to a JSON-lines file as it happens. `Show-ConsoleReplay` plays it back into the same dashboard, and `Export-ConsoleRecording` saves it as a web page with a player, so a run can be reviewed or shared after the fact. In C#, set `RecordPath` in `LiveSessionOptions`.

<div class="cm-lang" data-lang="powershell">

```powershell
$recording = Join-Path ([IO.Path]::GetTempPath()) 'deploy.jsonl'
'SRV1', 'SRV2', 'SRV3' | Invoke-ConsoleDashboard $dashboard -Parallel -RecordingPath $recording {
    param($Server)
    Update-Console -WidgetId servers -Key $Server -Complete
}
Show-ConsoleReplay -PlaybackMode Automatic $dashboard $recording -Speed 2
$page = Join-Path ([IO.Path]::GetTempPath()) 'deploy.html'
Export-ConsoleRecording $dashboard $recording -Format Html -Path $page -Width 100 -Height 16
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var options = new LiveSessionOptions { RecordPath = Path.Combine(Path.GetTempPath(), "deploy.jsonl") };
using (LiveSession session = new LiveSession(dashboard, options))
{
    session.Run(s => { /* the work from step 3 */ });
}
```

</div>

`-AutoRecord 10` keeps the last ten runs in a local folder without naming a file. [Recording and replay](/docs/recording/) covers replay speeds, chapters and the other export formats.

## 6. Without a console

The same script works in CI, in a scheduled task and with redirected output. There, the dashboard does not redraw: it writes a whole line for each lane start and finish and each log entry, in order, with `[mm:ss]` timestamps when a CI system is detected, then prints its final state. `-NoLive` asks for these lines in a terminal too.

## The full program

Steps 1 and 3 together, recorded as they run; the program seeds its random pauses so every run takes the same time. The PowerShell and C# sources are under the recording, with the commands to run them.

{{< cm-demo id="session" heading="h3" autoplay="true" >}}

## Next

- [Live sessions](/docs/live-sessions/): progress columns, replaceable regions, live tables and small windows.
- [Dashboard frames](/docs/dashboard-frames/): a ready-made summary layout for the final state.
- [Live runs](/docs/live-runs/): `Invoke-ConsoleRun` when each item only needs a cell and a result.
