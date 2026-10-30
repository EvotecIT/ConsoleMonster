---
title: PowerShell conventions
description: "One grammar for every widget: containers take script blocks, items are records with shared property names, and .NET methods still work."
slug: powershell-conventions
order: 15
group: hosts
card: shaping
chips: [Cmdlets, Script blocks]
summary: "One grammar for every widget, and each widget's .NET methods."
---

I use the same vocabulary in PowerShell and C#, with small translations where the hosts differ. A command should tell me whether it creates a definition, shows it, asks a question or runs work.

## Commands by job

| Job | Primary command | Composition or lifecycle |
| --- | --- | --- |
| Show ordinary objects or a widget | `Show-Console` | Automatic view selection; accepts an `IWidget` directly. |
| Show a table, record or hierarchy | `Show-ConsoleTable`, `Show-Console -View List`, `Show-Console -View Tree` | `New-ConsoleTable`, `New-ConsoleKeyValue`, `New-ConsoleTree` build definitions. `Show-Console -View List` accepts a hashtable or ordered dictionary directly. |
| Inspect an object | `Show-ConsoleObject` | Rich interactive keyboard explorer. Use `Show-Console -View Tree` for a static inspector. |
| Show a separator | `New-ConsoleRule -Title 'Section' \| Show-Console` | `New-ConsoleRule` builds the same widget for a layout. |
| Show data as a chart | `New-ConsoleChart \| Show-Console` | `New-ConsoleChart -Type Bar\|Stacked\|Line\|Scatter\|Histogram\|Sparkline` builds a reusable numeric chart; `New-ConsoleCellMap` builds a categorical outcome grid. |
| Write text | `Write-ConsoleText` | `New-ConsoleText` creates rich text for another widget. |
| Show an error | `Show-Console -View Error` | Displays the supplied exception or error record; does not emit another PowerShell error record. |
| Show a one-line status | `Show-ConsoleStatusLine` | `-Complete` finishes it. A dashboard status-list item uses `Update-Console`. |
| Follow a log source | `Watch-ConsoleLog` | `New-ConsoleLogRegion` builds a dashboard destination; `Write-ConsoleLogEntry` sends a message to it. |
| Run a dashboard | `Invoke-ConsoleDashboard` | Creates the live session, waits for the script and restores the terminal. |
| Run items | `Invoke-ConsoleRun` | `Start-ConsoleRun` returns a handle; `Complete-ConsoleRun -Wait` waits, finalizes and releases it. Use `Complete-ConsoleRun` for immediate finalization. |
| Adapt native progress | `Use-ConsoleProgress` | Scopes existing `Write-Progress` calls. `New-ConsoleProgress` builds a dashboard bar; `Update-Console` addresses it. |
| Ask for an answer | `Read-ConsoleText [-AsSecret]`, `Read-ConsoleNumber`, `Read-ConsoleConfirm`, `Read-ConsoleSelect [-Multiple]`, `Read-ConsolePath` | `New-ConsoleForm` builds a form; `Read-ConsoleForm` collects its answers. |
| Run an action menu or app | `Show-ConsoleMenu`, `Show-ConsoleApp` | `New-ConsoleMenuItem` defines an action. `New-ConsoleSelect` and the other application controls persist in app state. |
| Record a child terminal | `Invoke-ConsoleRecording` | Supply a script and `-Path`, or use `-Interactive`. Stop and marker commands apply inside the recorded child. |
| Play dashboard events | `Show-ConsoleReplay` | `-PlaybackMode Interactive` is the player; `Automatic` plays through the same event engine without keys. |
| Export output | `Export-ConsoleFrame`, `Export-ConsoleRecording` | Frame export accepts ordinary objects as well as widgets and snapshots. Omit the output path to return text; pipe it to `Set-Clipboard` to copy it. |
| Browse embedded demos | `Get-ConsoleDemo`, `Show-ConsoleDemo` | `(Get-ConsoleDemo <slug>).Source \| Set-Content -Path ./demo.ps1` saves the standalone script. |
| Present a script | `Show-ConsolePresentation -ScriptPath talk.ps1` | Optional `-Path talk.cast` records execution unattended; omit it for keyboard-controlled presentation. C# retains `ConsolePresentation.Present` and `ConsolePresentation.Record`. |

`New` returns a definition. `Show` renders or runs a viewer. `Read` returns an answer. `Invoke` waits for work, while `Start` returns an active handle. `Write` sends text or an update to its destination. Native `Format-*` integration retains PowerShell's formatting-record contract. Use the formatting integration commands to configure it.

## Composition and output

Containers take `-Content` blocks. Item widgets take item objects from `-Content`, `-InputObject` or the pipeline. A dictionary maps visible labels to values or statuses; item specifications use the same `Key`, `Label`, `Value`, `Status`, `Detail` and `Role` names as the cmdlets. Emit each item inside `-Content { foreach (...) { ... } }` when building a collection. Tabs, menu-run results and numeric-column formats use ordinary records in their parent parameters.

```powershell
$rows = @(
    [pscustomobject] @{ Server = 'web-01'; Checks = 12 }
    [pscustomobject] @{ Server = 'web-02'; Checks = 9 }
)
$rows | Show-ConsoleTable
Show-Console -View List ([ordered] @{ Region = 'west'; Servers = 2 })
New-ConsoleRule -Title 'Summary' | Show-Console
$rows | Export-ConsoleFrame -Format Markdown
$rows | New-ConsoleTable | Export-ConsoleFrame -Format Svg -Path ./servers.svg
```

Display commands return no success output by default. `New-ConsoleTable` and the other constructors return composable widgets without drawing. `Export-ConsoleFrame -Format Text` returns plain text when no path is supplied. Where supported, `-PassThru` draws and returns the original input. Interchange formats belong to exporters: `Text` is plain text, `FencedText` is a Markdown text fence, and `Markdown` preserves supported table/tree semantics. Interactive viewers document their selection or result separately.

## Parameter vocabulary

| Idea | Name and meaning |
| --- | --- |
| Live widget address | `WidgetId`, position 0 on live updates, setters, adders and addressed builders. |
| Stored identity | `Key`: field answer, application state, chart series or keyed item. |
| Visible item text | `Label`; defaults to the key where appropriate. |
| View heading | `Title`; `Subtitle` qualifies it below the heading. |
| Below-content explanation | `Caption`; panels use `HeaderRight` for top-right border text. |
| Semantic outcome | `Status`; free text explaining it is `Detail`. |
| Theme or colors | `Theme` selects a theme, `Role` selects a theme role, and `Color`/`BackgroundColor` are literal colors. `Decoration` adds text decorations. |
| Wall-clock formatting | `TimestampFormat`; `ShowTimestamp` opts into display timestamps, while the Boolean `LogTimestamp` controls log-file inclusion (default true). |
| Durations in PowerShell | Numeric `TimeoutSeconds`, `StepTimeoutSeconds`, `PollIntervalSeconds`, `IdleLimitSeconds`, `ProgressIntervalSeconds`, `ElapsedSeconds`, `DurationSeconds`, `AtSeconds`, `StartedSeconds`, `FinishedSeconds`, or `BudgetMilliseconds`. C# keeps `TimeSpan` properties such as `Timeout` and `PollInterval`. |
| Rendering cadence | `FramesPerSecond`; distinct from the source polling delay. |
| Overall width | `Width = 0` resolves the current target/terminal; unknown width falls back to 100 cells. Positive widths are explicit. Recorded frames retain their geometry. |
| Component sizing | `BarWidth` is a bar's cells; `Weight` is a share of remaining layout space. |

A switch selects non-default behavior. Default-on behavior opts out through `NoMouse`, `NoReview`, `NoPager`, `NoLegend`, `NoElapsed` or `NoLive`. Default-off behavior opts in through `ShowElapsed` or `ShowTimestamp`. Switches never default to true. C# uses positive Boolean properties such as `Mouse`, `Review` and `Live`; the cmdlet maps an opt-out to `false`.

```powershell
$dashboard = New-ConsoleDashboard -Title 'Build' -Content {
    New-ConsoleProgress build -Label 'Packages' -Total 10
}
Invoke-ConsoleDashboard $dashboard {
    Update-Console -WidgetId build -Value 5
    Update-Console -WidgetId build -Status Pass -Value 10
}
Watch-ConsoleLog -Path ./service.log -Count 1 -PollIntervalSeconds 0.25 -NoLive
```

The native progress adapter keeps `Activity`, `Status`, `Id`, `ParentId`, `PercentComplete`, `SecondsRemaining`, `CurrentOperation`, `Completed` and `SourceId` exactly as PowerShell defines them. Its `Id` is never a `WidgetId`, and its native descriptive `Status` is the explicit exception to semantic outcomes. Native `Write-Host`/`Format-*` integration likewise preserves native binding. `NoNewLine` follows the familiar PowerShell spelling.

##region Using the object directly

Builders return ordinary typed objects, so their methods remain available. C# uses American API spelling, including `Color`, `Center`, `PromptCanceledException` and `IsCanceled`. `RegionWidget` names a replaceable region, `MenuRunResult` names a menu result, and numeric ranges use `Minimum`/`Maximum`.

The module registers 15 fully qualified type accelerators: `ConsoleMonster.Terminal`, `ConsoleMonster.Layout.IWidget`, `ConsoleMonster.Styling.Color`, `RichColor`, `Style`, `RichStyle`, `Theme`, `ThemeToken`, `StatusKind`, `ConsoleMonster.Text.RichText`, `ConsoleMonster.Interactive.InteractionMode`, `ConsoleMonster.Interactive.CommandLine`, `ConsoleMonster.Live.SimulatedClock`, `ConsoleMonster.Capabilities.GlyphTier` and `ColorDepth` (the unqualified entries in this sentence share their preceding namespace). It registers no generic `[Table]`, `[Color]` or `[Grid]` names and leaves foreign registrations alone.

Advanced interop can look up another core type from the loaded assembly without changing global accelerator state:

```powershell
#region Using the object directly
$profileType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile', $true)
$profile = $profileType::Fixed(100, 24)
#endregion
```

Prefer cmdlets for ordinary scripts. Configure headings and footers on `New-ConsoleDashboard` or `Show-ConsoleApp`; pass records to numeric-column format parameters. `New-ConsoleRegion -Horizontal Center -Padding 1` aligns and pads content. `New-ConsoleStatusList -Layout Strip` builds a phase strip. Canvas remains a C# capability. Recording JSONL keeps its existing `target` and `state` wire keys while the public API uses `WidgetId` and `Status`.

Try [live sessions](/docs/live-sessions/) next.
