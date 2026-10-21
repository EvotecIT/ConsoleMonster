# ConsoleMonster

**Everyday output in one line. The monster when you need it.** ConsoleMonster makes the output of the commands you
already run readable, and draws tables, trees, charts, live runs, dashboards, menus and forms when you need more. One
engine for PowerShell 5.1 and 7 and .NET, with no package dependencies and plain lines when the output is a log.

<a href="https://consolemonster.dev/"><picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/hero-dark.png">
  <img alt="A ConsoleMonster console app: a menu, and the results of a fleet health check with the failed checks" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/hero-light.png">
</picture></a>

[Website](https://consolemonster.dev/) · [Documentation](https://consolemonster.dev/docs/) ·
[Widgets](https://consolemonster.dev/widgets/) · [Prompts, menus and forms](https://consolemonster.dev/interactive/) ·
[.NET API](https://consolemonster.dev/api/) · [PowerShell commands](https://consolemonster.dev/powershell/commands/)

## Install

PowerShell (Windows PowerShell 5.1 and PowerShell 7):

<!-- example: Website/content/docs/install.md#1 -->
```powershell
Install-Module ConsoleMonster -Scope CurrentUser
```

.NET (netstandard2.0, net472, net8.0 and net10.0):

<!-- example: Website/content/docs/install.md#3 -->
```bash
dotnet new console -n FirstTable
cd FirstTable
dotnet add package ConsoleMonster
```

## Nicer default output in one line

Turn it on for the session, and files, processes, services, hashtables and more read as tables and records. The objects
stay the same: pipelines, `Out-String`, files, jobs and transcripts keep PowerShell's behaviour.

<!-- example: Website/content/guides/everyday-powershell.md#1 -->
```powershell
Enable-ConsoleFormatting
```

To keep it in every session, add the lines it prints to your profile:

<!-- example: Website/content/docs/help-and-defaults.md#2 -->
```powershell
if (-not (Test-Path -Path $PROFILE)) { $null = New-Item -ItemType File -Path $PROFILE -Force }
Enable-ConsoleFormatting -Feature All -ProfileSnippet | Add-Content -Path $PROFILE
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/formatting-dark.png">
  <img alt="Get-Process, Get-ChildItem and a hashtable after Enable-ConsoleFormatting, then services grouped by status" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/formatting-light.png">
</picture>

[Everyday PowerShell output](https://consolemonster.dev/guides/everyday-powershell/) shows it before and after, with
themed warning and verbose colours and views that modules ship themselves.

## Format-Console, and Format-Table when you ask for it

`Format-ConsoleTable` and `Format-ConsoleList` take pipeline input and `-Property` like the built-in commands, plus
column options: priorities, presenters, totals and groups, and `Format-ConsoleTree` shows an object's structure.
`-As Markdown` returns the same view as a document. `Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless you turn on
`Enable-ConsoleFormatting -Feature FormatCommands`.

<!-- example: Website/examples/powershell/format-consoletable.ps1 -->
```powershell
Import-Module ConsoleMonster

# Build artifacts as a module build reports them.
$artifacts = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.nupkg'; Kind = 'NuGet'; Length = 1482113; Time = [timespan]::FromSeconds(48.2); Signed = $true; Path = 'D:\Builds\ConsoleMonster\Packed\ConsoleMonster.2.0.0.nupkg' }
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.snupkg'; Kind = 'NuGet'; Length = 402331; Time = [timespan]::FromSeconds(3.1); Signed = $true; Path = 'D:\Builds\ConsoleMonster\Packed\ConsoleMonster.2.0.0.snupkg' }
    [pscustomobject] @{ Name = 'ConsoleMonster-2.0.0.zip'; Kind = 'Module'; Length = 2904576; Time = [timespan]::FromSeconds(11.7); Signed = $true; Path = 'D:\Builds\ConsoleMonster\Packed\ConsoleMonster-2.0.0.zip' }
    [pscustomobject] @{ Name = 'Docs.zip'; Kind = 'Module'; Length = 51302; Time = [timespan]::FromSeconds(1.2); Signed = $false; Path = 'D:\Builds\ConsoleMonster\Packed\Docs.zip' }
)

$artifacts | Format-ConsoleTable -Title '[bold]Build artifacts[/]' -GroupBy Kind -Total -Property @(
    @{ Name = 'Name'; Key = $true; Overflow = 'Never' }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Priority = 9; Aggregate = 'Sum' }
    @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration'; Priority = 6; Aggregate = 'Sum' }
    @{ Name = 'Signed'; Presenter = 'Boolean'; Priority = 5 }
    @{ Name = 'Path'; Overflow = 'TruncateMiddle'; Priority = 1; MinWidth = 20 }
)
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/format-consoletable-dark.png">
  <img alt="The build artifacts grouped by kind, with sizes, durations, a totals row and shortened paths" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/format-consoletable-light.png">
</picture>

[Coming from Format-Table](https://consolemonster.dev/guides/format-table/) · [Tables at six widths](https://consolemonster.dev/tables/)

## Widgets

Every widget is a `New-Console*` command. Containers take a script block of children, and items share `-Status`,
`-Detail` and `-Tone`.

<!-- example: Website/examples/widgets/tree.ps1 -->
```powershell
Import-Module ConsoleMonster

New-ConsoleTree '[bold]evotec.xyz[/]' {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'SPF' -Status Warn -Detail '9 of 10 DNS lookups' {
        'include:spf.protection.outlook.com'
        'include:_spf.salesforce.com'
    }
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
    New-ConsoleTreeNode 'DKIM' -Status Pass -Detail 'selector1, selector2'
} | Write-ConsoleWidget
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/tree-dark.png">
  <img alt="A tree of mail records with pass, warning and failure markers" width="532" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/tree-light.png">
</picture>

[The widget gallery](https://consolemonster.dev/widgets/) shows every widget at every colour and glyph tier, in
PowerShell and C#.

## Live progress for long-running work

`ForEach-ConsoleItem -Parallel` runs a script block for many items with a throttle and a timeout, and shows the run as
it happens: a summary bar, a cell per item, what is running and what failed.

<!-- example: Website/examples/demos/parallel.ps1 -->
```powershell
Import-Module ConsoleMonster

# Forty servers patched eight at a time. Three fail and one hangs past the four-second limit;
# the view lists the failures and -ErrorVariable keeps them for the rest of the script.
$servers = 1..40 | ForEach-Object { 'web{0:d2}' -f $_ }
$results = $servers | ForEach-ConsoleItem -Parallel -ThrottleLimit 8 -TimeoutSeconds 4 -Title 'Patching the web farm' -ErrorVariable failed -ErrorAction SilentlyContinue {
    $number = [int] $_.Substring(3)
    Start-Sleep -Milliseconds (500 + ($number * 137) % 900)
    if ($number % 13 -eq 0) { throw "$_ did not come back after the reboot" }
    if ($number -eq 22) { Start-Sleep -Seconds 10 }
    "$_ patched"
}
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/parallel-dark.png">
  <img alt="A parallel run of forty servers: the summary bar, a cell per server and the failures" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/parallel-light.png">
</picture>

[Live runs](https://consolemonster.dev/docs/live-runs/) · [Live dashboards](https://consolemonster.dev/live/)

## C# and the fluent API

The same engine as a .NET library: typed tables from your own records, with columns that fit the window.

<!-- example: Website/examples/home/controllers.cs -->
```csharp
using ConsoleMonster;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

DomainController[] controllers = {
    new("DC01.ad.adatum.com", "Warsaw", StatusKind.Pass, 12, TimeSpan.FromSeconds(42)),
    new("DC02.ad.adatum.com", "Warsaw", StatusKind.Warn, 140, TimeSpan.FromMinutes(42)),
    new("DC03.eu.ad.adatum.com", "Dublin", StatusKind.Pass, 31, TimeSpan.FromSeconds(64)),
    new("DC04.eu.ad.adatum.com", "Dublin", StatusKind.Fail, 0, TimeSpan.FromHours(4))
};

Terminal.Write(Table.For(controllers)
    .WithTitle("[bold]Domain controllers[/]")
    .Column(c => c.Name, c => c.WithIsKey())
    .Column(c => c.Site, c => c.WithPriority(2))
    .Column(c => c.Status, c => c.WithPresenter(CellPresenters.Status).WithPriority(9))
    .Column(c => c.Latency, c => c
        .WithPresenter(new NumberPresenter("ms"))
        .WithPriority(5)
        .AddToneRules(ToneRule.Above(100, ThemeToken.Warn)))
    .Column(c => c.LastSync, c => c
        .WithHeader("Last sync")
        .WithPresenter(CellPresenters.Duration)
        .WithPriority(4)));

record DomainController(string Name, string Site, StatusKind Status, int Latency, TimeSpan LastSync);
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/csharp-dark.png">
  <img alt="A table of domain controllers with status, latency and last sync" width="574" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/csharp-light.png">
</picture>

[Three ways to write it in C#](https://consolemonster.dev/docs/csharp-styles/) · `ConsoleMonster.Extensions.Logging`,
`ConsoleMonster.Hosting` and `dotnet new consolemonster` build on it: [logging](https://consolemonster.dev/docs/logging/),
[hosting dashboard](https://consolemonster.dev/docs/hosting/), [template](https://consolemonster.dev/docs/template/).

## Save, replay and test

Record a run, play it back into the same dashboard, and save it as a page with a player. Widgets and frames export to
HTML, SVG, Markdown and text, and `Assert-ConsoleOutput` checks output in tests
([testing your output](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Usage.md#testing-your-output)).

<!-- example: Website/examples/demos/record.ps1 -->
```powershell
Import-Module ConsoleMonster

# Record a run, replay it and save it as a page. The dashboard is defined once: the run and the replay
# both draw into its widgets by name.
$dashboard = New-ConsoleDashboard -Title '[bold]Deploy[/]' -Subtitle 'web farm · 4 servers' {
    New-ConsolePhases -Phase 'Stage', 'Swap', 'Verify'
    New-ConsolePanel -Title 'Servers' { New-ConsoleStatusList -Name servers -Item 'web01', 'web02', 'web03', 'web04' }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}
$recording = Join-Path ([IO.Path]::GetTempPath()) 'deploy.jsonl'

# 1. Run it, writing every update to the recording.
Start-ConsoleDashboard $dashboard -RecordPath $recording {
    foreach ($phase in 'Stage', 'Swap', 'Verify') {
        Update-ConsolePhase -Key $phase -Status Running
        foreach ($server in 'web01', 'web02', 'web03', 'web04') {
            Update-ConsoleStatus -Name servers -Key $server -Status Running -Detail $phase
            Start-Sleep -Milliseconds 250
            Update-ConsoleStatus -Name servers -Key $server -Status Pass -Detail $phase
        }
        Write-ConsoleLog "$phase done on 4 servers"
        Update-ConsolePhase -Key $phase -Status Pass
    }
}

# 2. Play it back at twice the speed, into the same dashboard.
Start-ConsoleReplay $dashboard $recording -Speed 2

# 3. Save it as a page with a player.
Export-ConsoleRecording $dashboard $recording -Format Html -Destination (Join-Path ([IO.Path]::GetTempPath()) 'deploy.html') -Width 100 -Height 16
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/record-dark.png">
  <img alt="The deploy dashboard with its phases, servers and log" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/record-light.png">
</picture>

[Recording and replay](https://consolemonster.dev/docs/recording/)

## Thread safety

Any thread can update the screen; one thread draws it. Runspaces and worker threads post updates by widget name, and
`Write-Host` from any of them goes to the log panel instead of tearing the screen. In CI the same script writes whole,
ordered lines.

<!-- example: Website/examples/demos/threads-dashboard.ps1 -->
```powershell
Import-Module ConsoleMonster

# The same twelve servers, each runspace updating its own lane; Write-Host from any of them goes to the log panel.
$servers = 1..12 | ForEach-Object { 'web{0:d2}' -f $_ }
$dashboard = New-ConsoleDashboard -Title 'Patching the web farm' {
    New-ConsolePanel -Title 'Servers' -Caption '12 at once' { New-ConsoleLanes -Name servers -Lane $servers -Total 4 }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}

$servers | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 12 {
    param($Server)
    foreach ($step in 'copying', 'stopping', 'swapping', 'starting') {
        Update-ConsoleLane -Name servers -Key $Server -Status Running -Detail $step
        Start-Sleep -Milliseconds (Get-Random -Minimum 40 -Maximum 240)
        Update-ConsoleLane -Name servers -Key $Server -Result Pass
    }
    Write-Host "$Server done"
    Update-ConsoleLane -Name servers -Key $Server -Complete
}
```

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/threads-dark.png">
  <img alt="Twelve lanes updated by parallel runspaces, with a log panel" src="https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/threads-light.png">
</picture>

[Threads and concurrency](https://consolemonster.dev/docs/threads/): the model, what is safe from which thread, and the
stress test.

## Compatibility

- **PowerShell:** Windows PowerShell 5.1 and PowerShell 7 (`Install-Module ConsoleMonster`). Windows PowerShell 5.1
  loads one version of an assembly per session, so modules that use ConsoleMonster there need the same version;
  restart the session after an upgrade.
- **.NET:** netstandard2.0, net472, net8.0 and net10.0, with no package dependencies (`dotnet add package
  ConsoleMonster`). The net8.0 and net10.0 targets are AOT compatible ([trimming notes](https://consolemonster.dev/docs/install/)).
- **Consoles:** colour depth, glyphs, size, CI and redirection are detected; `NO_COLOR` and `CONSOLEMONSTER_*`
  variables override them. See [capability detection](https://consolemonster.dev/docs/capabilities/),
  [environment overrides](https://consolemonster.dev/docs/environment/) and
  [hosts, CI and transcripts](https://consolemonster.dev/guides/hosts-and-ci/).
- **Upgrading from 0.0.x:** version 2 replaces the Spectre.Console and Terminal.Gui wrappers; the `New-Spectre*` and
  `New-Terminal*` commands are removed. See [the guides](https://consolemonster.dev/guides/).

## Links

[Website](https://consolemonster.dev/) · [.NET API reference](https://consolemonster.dev/api/) ·
[PowerShell commands](https://consolemonster.dev/powershell/commands/) · [Benchmarks](https://consolemonster.dev/benchmarks/) ·
[Changelog](https://github.com/EvotecIT/ConsoleMonster/blob/master/CHANGELOG.MD) ·
[Contributing](https://github.com/EvotecIT/ConsoleMonster/blob/master/CONTRIBUTING.md) ·
[MIT license](https://github.com/EvotecIT/ConsoleMonster/blob/master/LICENSE)

ConsoleMonster is maintained by [Evotec](https://evotec.xyz/).
