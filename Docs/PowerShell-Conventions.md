# PowerShell conventions

How ConsoleMonster's cmdlets read, so a widget you have not used yet works the way the last one did. The rules apply to
every cmdlet that builds a widget; `ConsoleMonster.Examples.Tests.ps1` checks the examples and documentation against
them. The .NET API is unchanged by these rules: C# code keeps its constructors, fluent methods and `using static
ConsoleMonster.Ui`, and PowerShell can still call the same methods when that suits a script better (see
[Using the object directly](#using-the-object-directly)).

## The grammar

- **Containers take a script block of children.** `New-ConsoleRows`, `New-ConsoleColumns`, `New-ConsoleGrid`,
  `New-ConsolePanel`, `New-ConsoleStack`, `New-ConsoleAlign`, `New-ConsolePadding` and `New-ConsoleRegion` lay out the
  widgets their script block emits. Strings in the script block are markup text.
- **Widgets with items take a script block of item commands.** Each item is its own `New-Console*` command with the
  same parameter names as the rest of the module:

  | Widget | Items |
  |---|---|
  | `New-ConsoleTree` | `New-ConsoleTreeNode` (nested with its own script block), or strings for plain leaves |
  | `New-ConsoleKeyValue`, `Write-ConsoleKeyValue` | `New-ConsoleKeyValueItem` |
  | `New-ConsoleStatusList`, `New-ConsolePhases` | `New-ConsoleStatusItem` |
  | `New-ConsoleBarChart` (also `-Stacked`) | `New-ConsoleBarItem` |
  | `New-ConsoleLineChart` (also `-Scatter`) | `New-ConsoleChartSeries` |
  | `New-ConsoleLog` | `New-ConsoleLogEntry` |
  | `New-ConsoleLanes` | `New-ConsoleLane` |

  Status lists, phases, bar charts, line charts, logs, lanes and trees also take their items from the pipeline (as
  item objects, or hashtables and objects with the same property names), and simple data still works without item
  commands: a hashtable for `-Data` (labels to values or statuses), arrays of numbers for `-Value`, names for
  `-Phase`, `-Lane` and `-Item`. Strings in a tree's script block are leaves shown literally; `New-ConsoleTreeNode`
  takes markup.
- **Objects come in through the pipeline.** `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree` shape
  any object; `New-ConsoleTree` follows hierarchical objects with `-ChildrenProperty` or a `-Children` script block
  (`$_` is the parent) and keeps a large or cyclic hierarchy bounded with `-Depth`, `-MaxItems` and `-MaxNodes`.
- **A widget made earlier grows with `Add-ConsoleItem`.** Pipe the widget in and give the same item commands (or
  numbers for sparklines and histograms, status words for a cell map, objects for a live table). `-PassThru` writes the
  widget on.
- **A widget in a running dashboard changes through the live commands.** `Update-ConsoleStatus`,
  `Update-ConsolePhase`, `Update-ConsoleLane`, `Update-ConsoleProgress`, `Update-ConsoleSpinner`,
  `Update-ConsoleTable`, `Add-ConsoleValue`, `Set-ConsoleValue`, `Set-ConsoleRegion` and `Write-ConsoleLog` find the
  widget by its `-Name`.
- **No .NET construction is needed for anything a widget can show.** If a widget property has no parameter, that is a
  gap in the cmdlet, not a reason to write `[ConsoleMonster.Widgets.X]::new()` in an example.

## Shared parameter names

| Parameter | Meaning everywhere |
|---|---|
| `-Status` | The shared status vocabulary: `Pass`, `Fail`, `Warn`, `Skip`, `Info`, `Running`, `Pending`, plus PowerShell's words (`Success`, `Warning`, `Error`, `Failed`, `Skipped`...). Tab completion offers the words. |
| `-Level` | Log levels: `info`, `warn`, `err`, `verbose`, `debug`, `pass`, `skip`, or `Information`, `Warning`, `Error`, `Verbose`, `Debug`, `Success`, `Skipped`. |
| `-Detail` | A muted note after or beside the label: a duration, a count, a short reason. Always literal text. |
| `-Title` | The heading of a container or widget (markup allowed). |
| `-Label` | The text of an item (progress bars, spinners and tree nodes take markup; status items, bars and lanes are literal). Rich text from `New-ConsoleText` keeps its colours wherever a label is accepted. |
| `-Value` | The value an item shows: a number for bars and progress, an array for charts, any object for key/value pairs and tree nodes (with `-Presenter`). |
| `-Tone` | A colour as a theme role: `Accent`, `Accent2`, `Pass`, `Fail`, `Warn`, `Info`, `Muted`, `Series1` to `Series6`. `-Role` on the text commands is the same thing (and accepts `-Tone`). `-Color` stays for console and RGB colours on the text commands. |
| `-Markup` | Opt-in: treat a data value as markup. Values are literal unless `-Markup` is given; titles and labels documented as markup take it without the switch. |
| `-Presenter` | How a value is formatted: `Bytes`, `Duration`, `Date`, `Number` (with `-Unit`), `Boolean`, `Status`, `Progress`, `DataBar`, `Link`, `Markup`, `Text`. |
| `-At` | A time: a `TimeSpan`, seconds (`2`, `1.5`) or a number with a unit (`500ms`, `2s`, `1.5m`, `1h`). |
| `-Name` | The live name a dashboard command uses to find the widget. |
| `-Weight`, `-Height`, `-Fill` | The space a widget takes in rows, columns and dashboards. |
| `-NoLatest`, `-NoElapsed`, `-NoLegend`, `-NoTime` | Switches that hide one part of a widget. |

## Every widget, both ways

The cmdlet form is what the documentation and the website show first. The method form is the widget's .NET API, which
keeps working from PowerShell and is what C# code uses.

| Widget (.NET type) | Cmdlet form | Add items later | Live update | Method form |
|---|---|---|---|---|
| Tree (`Tree`, `TreeNode`) | `New-ConsoleTree 'root' { New-ConsoleTreeNode 'A' -Status Pass -Detail 'note' { ... } }`, pipeline objects with `-ChildrenProperty`/`-Children`, or `-Data` | `Add-ConsoleItem` | | `Tree.Add(label, status, note)` returns the node; `TreeNode.Add(...)` |
| Key/value list (`KeyValueList`) | `New-ConsoleKeyValue (-Data) { New-ConsoleKeyValueItem 'Key' 'value' -Status Warn }`, `Write-ConsoleKeyValue` | `Add-ConsoleItem` | `Set-ConsoleValue` | `Add(key, value, status)`, `AddValue(key, value, presenter)` |
| Status list (`StatusList`) | `New-ConsoleStatusList { New-ConsoleStatusItem 'Step' -Status Pass -Detail '1.6 s' }`, `-Data`, `-Item` | `Add-ConsoleItem` | `Update-ConsoleStatus` | `Add(label, status, detail)`, `Set(key, label, status, detail)` |
| Phases (`Phases`) | `New-ConsolePhases { New-ConsoleStatusItem 'Build' -Status Running }`, `-Phase` | `Add-ConsoleItem` | `Update-ConsolePhase` | `Set(key, status, label, detail)` |
| Bar chart (`BarChart`) | `New-ConsoleBarChart -Tone Fail (-Data) { New-ConsoleBarItem 'DNS' 4 }`, `-Maximum`, `-Warning`, `-Critical` | `Add-ConsoleItem` | `Add-ConsoleValue`, `Set-ConsoleValue` | `Add(label, value, token)`, `BarToken`, `Max` |
| Stacked bar (`StackedBar`) | `New-ConsoleBarChart -Stacked { New-ConsoleBarItem 'Passed' 412 -Tone Pass }` | `Add-ConsoleItem` | `Add-ConsoleValue`, `Set-ConsoleValue` | `Add(label, value, token)` |
| Line and scatter chart (`LineChart`) | `New-ConsoleLineChart { New-ConsoleChartSeries 'cpu' -Value $values -Tone Accent }`, `-Data`, `-Series` | `Add-ConsoleItem` | `Add-ConsoleValue -Key` | `AddSeries(name, token)`, `Add(series, value)` |
| Sparkline (`Sparkline`) | `New-ConsoleSparkline -Value $values -Tone Pass -NoLatest -Maximum 100`, pipeline numbers | `Add-ConsoleItem` | `Add-ConsoleValue` | `Add(value)`, `AddRange(values)`, `Token`, `ShowLatest` |
| Histogram (`Histogram`) | `New-ConsoleHistogram -Edge 0, 1, 2 -Value $values -Tone Accent` | `Add-ConsoleItem` | `Add-ConsoleValue` | `Add(value)`, `Token` |
| Log (`LogRegion`) | `New-ConsoleLog { New-ConsoleLogEntry 'text' -Level Warning -At 31s }` | `Add-ConsoleItem` | `Write-ConsoleLog` | `Write(text, level, time)` |
| Lanes (`Lanes`) | `New-ConsoleLanes { New-ConsoleLane 'DC01' -Total 40 -Done 22 -Status Running }`, `-Lane` | `Add-ConsoleItem` | `Update-ConsoleLane` | `Add(key, label, total)` returns the `Lane` |
| Progress bar (`ProgressBar`) | `New-ConsoleProgress 'Docs' -Value 100 -Total 100 -Status Pass` | | `Update-ConsoleProgress` | `Value`, `Total`, `Status` |
| Spinner (`Spinner`) | `New-ConsoleSpinner 'Schema' -Detail 'cached' -Status Pass` | | `Update-ConsoleSpinner` | `Detail`, `Status` |
| Live table (`LiveTable`) | `New-ConsoleLiveTable -Column Job, State` | `Add-ConsoleItem` (objects or hashtables) | `Update-ConsoleTable` | `Set(key, values)`, `Remove(key)`, `Clear()` |
| Cell map (`CellMap`) | `New-ConsoleCellMap -Status $states -Count 40` | `Add-ConsoleItem` (status words) | drawn by `Invoke-ConsoleRun` | `Add(state)`, `Set(index, state)`, `Resize(count)` |
| Summary bar (`SummaryBar`) | `New-ConsoleSummaryBar -Done 30 -Total 40 -Counter ([ordered] @{ passed = 27; failed = 2 })` | | drawn by `Invoke-ConsoleRun` | `SetProgress(done, total)`, `SetCounter(label, count, status)` |
| Region (`Slot`) | `New-ConsoleRegion -Name main { ... }` | | `Set-ConsoleRegion` | `session.Region("main").Set(widget)` |
| Padding (`Padding`) | `New-ConsolePadding -Padding 2, 1 { ... }` | | | `new Padding(child, left, top, right, bottom)` |
| Align (`Align`) | `New-ConsoleAlign -Horizontal Right { ... }` | | | `new Align(child, horizontal, vertical)` |
| Menu run result (`RunResult`) | `New-ConsoleRunResult 'Done' -Figure @{ ... } -Next @{ ... }` | | | `new RunResult(title).Figure(...).Next(...)` |

## Using the object directly

Every widget is an ordinary .NET object, so a script can keep it in a variable and call its methods. This is handy when
a widget is built up over a long script, or when the code is shared with C#. Write it the PowerShell way: methods that
return the new node chain, and `[void]` (or `$null =`) discards a result instead of `| Out-Null`.

The native form of a log, and the same log through its methods:

```powershell
New-ConsoleLog -Height 5 {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
} | Write-ConsoleWidget
```

```powershell
# Using the object directly
$log = New-ConsoleLog -Height 5
$log.Write('Discovered 4 domain controllers in 2 domains', 'info', [timespan]::FromSeconds(2))
$log.Write('DC02: USN rollback check failed (event 2095)', 'err', [timespan]::FromSeconds(44))
$log | Write-ConsoleWidget
```

A tree, where `Add` returns the new node so children chain from it:

```powershell
# Using the object directly
$tree = New-ConsoleTree '[bold]evotec.xyz[/]'
$mx = $tree.Add('MX', 'Pass', '2 records')
[void] $mx.Add('evotec-xyz.mail.protection.outlook.com', $null, 'priority 0')
[void] $tree.Add('DMARC', 'Fail', 'p=none')
$tree | Write-ConsoleWidget
```

The same tree grown later with `Add-ConsoleItem`, without methods:

```powershell
$tree = New-ConsoleTree '[bold]evotec.xyz[/]'
$tree | Add-ConsoleItem { New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' }
$tree | Add-ConsoleItem { New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none' }
$tree | Write-ConsoleWidget
```

## Writing examples

- Examples in `Website/examples`, `Module/Examples`, `README.md`, `Docs/` and `Module/Docs` use the cmdlet form. The
  test fails on `[ConsoleMonster.` type names, `::new(`, `| Out-Null` and widget method calls in them.
- A method-form alternative goes in its own block whose first line is `# Using the object directly` (in a script, a
  `#region Using the object directly` ... `#endregion` block), shown after the native form.
- A sample that genuinely needs .NET interop is listed in the test's allow-list with the reason.
- Data is invented and generic (Evotec demo domains, servers, builds); every script runs headless in the example test.
