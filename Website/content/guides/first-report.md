---
title: Your first report
description: "Turn a list of results into a report with a title, a facts line, a status table and bars, then save it as HTML, SVG or text."
slug: first-report
group: tasks
order: 10
card: first-report-80
---

This guide builds a script that takes a list of results and prints a report people can read at a glance: a title, a line of counts, a table with a status column and a bar per server, saved as a web page as well. The data is four invented servers, so the script runs anywhere; replace it with your own at the end.

{{< cm-frame ids="first-report-80" script="false" caption="The finished report at 80 columns. The full script is at the end of the page." >}}

## 1. Start from data

A report starts from objects. In PowerShell they are `[pscustomobject]` rows, the same shape a command such as `Get-Service` returns; in C# a record per row. Counting the statuses up front keeps the facts line honest when the data changes.

<div class="cm-lang" data-lang="powershell">

```powershell
Import-Module ConsoleMonster

$servers = @(
    [pscustomobject] @{ Name = 'fs01.contoso.example'; Role = 'File'; Disk = 62; Status = 'Pass' }
    [pscustomobject] @{ Name = 'fs02.contoso.example'; Role = 'File'; Disk = 81; Status = 'Warn' }
    [pscustomobject] @{ Name = 'sql01.contoso.example'; Role = 'Database'; Disk = 93; Status = 'Fail' }
    [pscustomobject] @{ Name = 'web01.contoso.example'; Role = 'Web'; Disk = 40; Status = 'Pass' }
)
$passed = @($servers | Where-Object Status -EQ 'Pass').Count
$warnings = @($servers | Where-Object Status -EQ 'Warn').Count
$failed = @($servers | Where-Object Status -EQ 'Fail').Count
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var servers = new Server[]
{
    new("fs01.contoso.example", "File", 62, "Pass"),
    new("fs02.contoso.example", "File", 81, "Warn"),
    new("sql01.contoso.example", "Database", 93, "Fail"),
    new("web01.contoso.example", "Web", 40, "Pass")
};
int passed = servers.Count(server => server.Status == "Pass");
int warnings = servers.Count(server => server.Status == "Warn");
int failed = servers.Count(server => server.Status == "Fail");

record Server(string Name, string Role, int Disk, string Status);
```

</div>

## 2. Show the rows as a table

`New-ConsoleTable` picks the columns and `-StatusProperty` turns the Status column into status words with their symbol and colour. Pipe it to `Show-Console` to see it straight away. In C#, `Table.For` takes the rows and one `Column` per property.

<div class="cm-lang" data-lang="powershell">

```powershell
$servers | New-ConsoleTable -Property Name, Role, Disk, Status -StatusProperty Status | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var table = Table.For(servers)
    .Column(row => row.Name)
    .Column(row => row.Role)
    .Column(row => row.Disk)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);
Terminal.Write(table);
```

</div>

Numbers align to the right and the table fits the window: on a narrow one, columns hide by priority and then rows turn into cards.

## 3. Add a title and a facts line

A rule with a title heads the report, and a facts line says what matters before anyone reads a row. Each fact has a value, a label and optionally a role, which colours the count and its word together.

<div class="cm-lang" data-lang="powershell">

```powershell
New-ConsoleRule -Title '[accent bold]Nightly server report[/] [muted]· 2026-11-09[/]'
New-ConsoleFacts -Content {
    @{ Value = $passed; Label = 'healthy'; Role = 'Pass' }
    @{ Value = $warnings; Label = 'warning'; Role = 'Warn' }
    @{ Value = $failed; Label = 'failed'; Role = 'Fail' }
    @{ Value = $servers.Count; Label = 'servers' }
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var title = new Rule("[accent bold]Nightly server report[/] [muted]· 2026-11-09[/]");
var facts = new ViewFacts().Add(passed, "healthy", ThemeToken.Pass)
    .Add(warnings, "warning", ThemeToken.Warn)
    .Add(failed, "failed", ThemeToken.Fail)
    .Add(servers.Length, "servers");
```

</div>

## 4. Add a bar per server

A bar chart with `-Maximum 100` draws each value as a share of the whole, with the percentage on the right. `-Warning` and `-Critical` colour the bars that cross them, so the full disk stands out without a legend.

<div class="cm-lang" data-lang="powershell">

```powershell
New-ConsolePanel -Title 'Disk used' -HeaderRight '% of capacity' -Content {
    $servers | New-ConsoleChart -Property Disk -Label Name -Unit '%' -Maximum 100 -Warning 80 -Critical 90
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
BarChart disk = new BarChart { Unit = "%", Maximum = 100, Warning = 80, Critical = 90 };
foreach (var server in servers)
    disk.Add(server.Name, server.Disk);
var bars = new Panel(disk, "Disk used") { HeaderRight = "% of capacity" };
```

</div>

## 5. Put it together

`New-ConsoleRows` stacks the parts with a blank row between them. Nothing is drawn until the finished report is passed to `Show-Console` (`Terminal.Write` in C#), so the same `$report` can be printed, exported or placed inside a bigger layout. [How it works](/docs/how-it-works/) explains the model.

<div class="cm-lang" data-lang="powershell">

```powershell
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[accent bold]Nightly server report[/] [muted]· 2026-11-09[/]'
    New-ConsoleFacts -Content {
        @{ Value = $passed; Label = 'healthy'; Role = 'Pass' }
        @{ Value = $warnings; Label = 'warning'; Role = 'Warn' }
        @{ Value = $failed; Label = 'failed'; Role = 'Fail' }
        @{ Value = $servers.Count; Label = 'servers' }
    }
    New-ConsoleTable -InputObject $servers -Property Name, Role, Disk, Status -StatusProperty Status
    New-ConsolePanel -Title 'Disk used' -HeaderRight '% of capacity' -Content {
        $servers | New-ConsoleChart -Property Disk -Label Name -Unit '%' -Maximum 100 -Warning 80 -Critical 90
    }
}
$report | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Rows report = new Rows(title, facts, table, bars) { Gap = 1 };
Terminal.Write(report);
```

</div>

## 6. Save it as HTML, SVG or text

`Export-ConsoleFrame` writes the same report to a file, choosing the format from the extension. The HTML page is self-contained, with selectable text and a dark and a light rendering that follow the reader's setting; the SVG draws borders and bars as shapes, so it stays sharp in a slide or a wiki. `-Width` sets the width in cells (the terminal's width otherwise).

<div class="cm-lang" data-lang="powershell">

```powershell
$report | Export-ConsoleFrame -Path ./server-report.html -Width 80
$report | Export-ConsoleFrame -Path ./server-report.svg -Width 80 -Title 'Nightly server report'
$report | Export-ConsoleFrame -Path ./server-report.txt -Width 80
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var options = new ExportOptions { Width = 80 };
ConsoleExporter.Save(report, "server-report.html", options: options);
ConsoleExporter.Save(report, "server-report.svg", options: options);
ConsoleExporter.Save(report, "server-report.txt", options: options);
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
report.Export("server-report.html", options => options.Width = 80);
report.Export("server-report.svg", options => options.Width = 80);
report.Export("server-report.txt", options => options.Width = 80);
```

</div>

`-Format Markdown` writes the table as a Markdown table instead, ready for a pull request, a wiki or a [CI job summary](/docs/ci-integration/).

## The full script

{{< cm-try key="first-report" ids="first-report-80" project="ServerReport" caption="The full script and the report it prints at 80 columns. Saving it as HTML, SVG or text is step 6." >}}

To use your own data, replace the `$servers` list with objects from a command, for example `Get-Service` rows with a calculated Status, and keep the rest.

## Next

- [Your first live dashboard](/guides/first-dashboard/): the same parts kept on screen while work runs.
- [Layout](/docs/layout/): columns, panels and grids for larger reports.
- [Facts, bars and hierarchy](/docs/facts-and-bars/): when to use a count, a bar or a chart.
