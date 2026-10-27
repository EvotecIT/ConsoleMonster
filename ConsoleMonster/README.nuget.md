# ConsoleMonster

**Everyday output in one line. The monster when you need it.**

ConsoleMonster makes the output of the commands you already run readable, and draws tables, trees, charts, live
runs, dashboards, menus and forms when you need more. It is one engine for Windows PowerShell 5.1, PowerShell 7 and
.NET, with no package dependencies. A setting has one name everywhere (`-Title` in PowerShell, `Title =` and
`.Title()` in C#), and when the output goes to a file, a log or CI, it writes plain lines instead of escape codes.

![A live assessment dashboard: progress per target, a results summary, charts, failures by category and the log](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/hero-light.png)

[Website](https://consolemonster.dev/) · [Documentation](https://consolemonster.dev/docs/) ·
[Demos](https://consolemonster.dev/demos/) · [Widgets](https://consolemonster.dev/widgets/) ·
[.NET API](https://consolemonster.dev/api/) · [PowerShell commands](https://consolemonster.dev/powershell/commands/)

## Start in PowerShell

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Get-Process | Sort-Object CPU -Descending | Select-Object -First 8 | Show-ConsoleTable
```

`Show-ConsoleDemo` lists the gallery's demos, grouped by what they are for, and runs the one you pick.
`Get-Command -Module ConsoleMonster` lists every command, and `Get-Help <command> -Examples` shows how to use it.

## Start in C#

```bash
dotnet new console -n FirstTable
cd FirstTable
dotnet add package ConsoleMonster
```

Replace `Program.cs`, then `dotnet run`:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

var files = new DirectoryInfo(".").GetFiles().Select(f => new { f.Name, f.Length });
Table.For(files).Title("Files").Show();
```

[Install](https://consolemonster.dev/docs/install/) ·
[Three ways to write it in C#](https://consolemonster.dev/docs/csharp-styles/) ·
[The `dotnet new` template](https://consolemonster.dev/docs/template/), which starts a project from any gallery demo

## Nicer default output in one line

Files, processes, services, hashtables and more read as tables and records; the objects, pipelines, files and
transcripts stay PowerShell's.

```powershell
Enable-ConsoleFormatting
```

![Get-ChildItem and a hashtable after Enable-ConsoleFormatting](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/formatting-light.png)

To keep it in every session, add the lines `-ProfileSnippet` prints to your profile:

```powershell
if (-not (Test-Path -Path $PROFILE)) {
    $null = New-Item -ItemType File -Path $PROFILE -Force
}
Enable-ConsoleFormatting -Feature All -ProfileSnippet | Add-Content -Path $PROFILE
```

[Everyday PowerShell output](https://consolemonster.dev/guides/everyday-powershell/)

## Tables, lists and trees

`Show-ConsoleTable`, `Show-ConsoleList` and `Show-Console -View Tree` take pipeline input and fit the window.
`Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless you turn on `-Feature FormatCommands`.

```powershell
Import-Module ConsoleMonster

# Invented package artifacts with fixed sizes and UTC timestamps.
$recordedAt = [datetimeoffset] '2026-10-21T12:00:00Z'
$files = @(
    [pscustomobject] @{
        Name = 'ConsoleMonster.dll'; Length = 1048576
        LastWriteTime = $recordedAt; Extension = '.dll'
    }
    [pscustomobject] @{
        Name = 'ConsoleMonster.deps.json'; Length = 4096
        LastWriteTime = $recordedAt; Extension = '.json'
    }
    [pscustomobject] @{
        Name = 'start.ps1'; Length = 512
        LastWriteTime = $recordedAt; Extension = '.ps1'
    }
)
$files | Show-ConsoleTable -Property Name, Length, LastWriteTime `
    -GroupBy Extension -Presentation @{ TimeZone = [TimeZoneInfo]::Utc }
```

![Sample package artifacts grouped by extension, with fixed sizes and dates](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/files-light.png)

[Coming from Format-Table](https://consolemonster.dev/guides/format-table/) · [Tables at six widths](https://consolemonster.dev/tables/)

## Widgets

Every widget is a `New-Console*` command: panels, trees, charts, logs, lanes and more. Containers take a script block
of children, and items share `-Status`, `-Detail` and `-Role`.

```powershell
Import-Module ConsoleMonster

New-ConsoleChart -Role Fail -InputObject ([ordered] @{
    'Domain controllers' = 14
    'Group Policy'       = 9
    'Kerberos'           = 6
    'DNS'                = 4
    'PKI'                = 2
}) -Type Bar | Show-Console
```

![A bar chart of failures by category](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/widgets-light.png)

[The widget gallery](https://consolemonster.dev/widgets/): every widget at every colour and glyph tier, in PowerShell and C#.

## Live progress for long-running work

```powershell
Import-Module ConsoleMonster

# Forty servers, eight at a time: three fail and one hangs past the four-second limit.
$servers = 1..40 | ForEach-Object { 'web{0:d2}' -f $_ }
$run = @{ Parallel = $true; ThrottleLimit = 8; TimeoutSeconds = 4; Title = 'Patching' }
$results = $servers | Invoke-ConsoleRun @run -ErrorAction SilentlyContinue {
    Start-Sleep -Milliseconds (500 + ([int] $_.Substring(3) * 137) % 900)
    if ($_ -in 'web13', 'web26', 'web39') { throw "$_ did not come back" }
    if ($_ -eq 'web22') { Start-Sleep -Seconds 10 }
    "$_ patched"
}
```

![A parallel run of forty servers: the summary bar, a cell per server and the failures](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/parallel-light.png)

[Live runs](https://consolemonster.dev/docs/live-runs/) · [Live dashboards](https://consolemonster.dev/live/)

## C# and the fluent API

The same engine as a .NET library. Every widget takes an object initializer or fluent calls named after its
properties (`.Title("Build")`), and `Ui.Panel()`, `Ui.Table()` and the other `Ui` factories start a fluent chain.
All three draw the same cells.

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new StatusList()
    .Add("Restore NuGet packages", StatusKind.Pass, "1.6 s")
    .Add("Compile ConsoleMonster", StatusKind.Pass, "4.7 s")
    .Add("Run Pester on PowerShell 7", StatusKind.Warn, "1 skipped")
    .Add("Sign module files", StatusKind.Skip, "skipped")
    .Add("Pack ConsoleMonster.2.0.0.nupkg", StatusKind.Running)
    .Add("Publish to PowerShell Gallery", StatusKind.Pending));
```

![A status list of build steps: passed, warning, skipped, running and pending](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/csharp-light.png)

[Three ways to write it in C#](https://consolemonster.dev/docs/csharp-styles/) · [Logging](https://consolemonster.dev/docs/logging/) ·
[Hosting dashboard](https://consolemonster.dev/docs/hosting/) · [dotnet new template](https://consolemonster.dev/docs/template/)

## Save, replay and test

Views export to Markdown, HTML, SVG and text, live runs record and replay, and `Assert-ConsoleOutput` checks output in
tests.

```powershell
Import-Module ConsoleMonster

# Invented package artifacts, exported as a Markdown table for a report.
$recordedAt = [datetimeoffset] '2026-10-21T12:00:00Z'
$files = @(
    [pscustomobject] @{
        Name = 'ConsoleMonster.dll'; Length = 1048576; LastWriteTime = $recordedAt
    }
    [pscustomobject] @{
        Name = 'ConsoleMonster.deps.json'; Length = 4096; LastWriteTime = $recordedAt
    }
    [pscustomobject] @{
        Name = 'start.ps1'; Length = 512; LastWriteTime = $recordedAt
    }
)
$files | Show-ConsoleTable -Property Name, Length, LastWriteTime `
    -Presentation @{ TimeZone = [TimeZoneInfo]::Utc } -AsWidget |
    Export-ConsoleFrame -Format Markdown
```

![The Markdown table the script printed](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/readme/markdown-light.png)

[Recording and replay](https://consolemonster.dev/docs/recording/) ·
[Testing your output](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Usage.md#testing-your-output)

## Thread safety

Any thread can update the screen and one thread draws it, so parallel work never tears the view.

```powershell
Import-Module ConsoleMonster

$dashboard = New-ConsoleDashboard -Title 'Workers' -Content {
    New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
}
1..8 | Invoke-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 8 {
    param($Number)
    Start-Sleep -Milliseconds (Get-Random -Maximum 800)
    Write-Host "worker $Number finished"
}
```

[Threads and concurrency](https://consolemonster.dev/docs/threads/): what is safe from which thread, and the stress test.

## Compatibility

- **PowerShell** 5.1 and 7. On 5.1 one ConsoleMonster version loads per session; restart it after an upgrade.
- **.NET** netstandard2.0, net472, net8.0 and net10.0, no package dependencies, AOT compatible on net8.0 and net10.0
  ([trimming notes](https://consolemonster.dev/docs/install/)).
- **Consoles** are [detected](https://consolemonster.dev/docs/capabilities/) (colour, glyphs, size, CI, redirection);
  `NO_COLOR` and [`CONSOLEMONSTER_*`](https://consolemonster.dev/docs/environment/) override it
  ([hosts and CI](https://consolemonster.dev/guides/hosts-and-ci/)).

[Benchmarks](https://consolemonster.dev/benchmarks/) ·
[Changelog](https://github.com/EvotecIT/ConsoleMonster/blob/master/CHANGELOG.MD) ·
[Contributing](https://github.com/EvotecIT/ConsoleMonster/blob/master/CONTRIBUTING.md) ·
[MIT license](https://github.com/EvotecIT/ConsoleMonster/blob/master/LICENSE) · Maintained by [Evotec](https://evotec.xyz/).
