---
title: Compare text and files
description: Compare configuration strings or files with line numbers, changed words and a plain unified export.
slug: text-diff
order: 16
group: apps
summary: "Numbered lines, word highlights, collapsed context and unified output from strings or files."
apis: [Compare-ConsoleText, TextDiff, TextDiffView]
chips: [Files, Unified diff]
card: text-diff-120
---

`Compare-ConsoleText` shows what changed between two strings or files. Removed lines use a minus marker and the failure tone; added lines use a plus marker and the success tone. Within a replacement line, the changed words are highlighted while shared words keep their normal appearance.

1. Install the module for your user account:

   ```powershell
   Install-Module ConsoleMonster -Scope CurrentUser
   ```

2. Import it and compare the two configuration strings:

```powershell
Import-Module ConsoleMonster
$before = @'
host=web-01
port=80
protocol=http
workers=4
'@
$after = @'
host=web-01
port=443
protocol=https
workers=4
'@
Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1
```

Text comparisons use `Before` and `After` headings. At 120 columns or more, they appear side by side. Narrower terminals show removed lines followed by added lines, with both original line numbers in the gutter. The minus and plus markers remain visible in no-colour output.

{{< cm-frame id="text-diff-120" script="true" >}}

`-Context` sets the number of unchanged lines retained before and after each change. It defaults to three; zero shows only changed lines. Longer unchanged regions collapse to a count with the reference and difference line ranges. Wrapped content stays visible at the available width.

{{< cm-frame id="text-diff-60" >}}

Use a script block with `Use-ConsoleTarget` to capture the rendered comparison at a fixed width:

```powershell
$report = Use-ConsoleTarget -Width 120 {
    Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1
}
```

Compare files using normal PowerShell paths, including relative paths, filesystem drives and provider-qualified paths:

```powershell
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf -IgnoreWhitespace
```

Each path must resolve to exactly one readable FileSystem file. Wildcards that match several files, directories, missing files and other providers produce errors. Files use UTF-8 with BOM detection.

File comparisons use the resolved filenames as headings, such as `before.conf` and `after.conf`. Use `-ReferenceName` and `-DifferenceName` to override either heading for files or text. The same names appear in unified output:

```powershell
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf -ReferenceName 'Deployed' -DifferenceName 'Proposed'
```

`-IgnoreWhitespace` ignores Unicode whitespace within each line. It does not join lines or ignore a missing final newline. CRLF, LF and CR line endings compare equally. Empty text is an empty document rather than a blank line.

For a string you can save or process, use `-AsUnified`:

```powershell
$diff = Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1 -AsUnified
$diff | Set-Content .\changes.diff
```

The string contains reference and difference headers, hunk ranges and `-`/`+` lines, with LF separators and a marker for a missing final newline. Equal inputs return an empty string. Normal rendering writes to the host and returns no comparison objects. No terminal styling or ConsoleMonster markup is added to unified output.

For C#, create a console project and add the package:

```powershell
dotnet new console -n TextComparison
Set-Location TextComparison
dotnet add package ConsoleMonster
```

The figure's C# tab contains a complete `Program.cs` that prints the same comparison. Paste the full program into `Program.cs`, then run `dotnet run`. To compose a comparison with the fluent panel layout instead, use this complete program:

```csharp
using ConsoleMonster;
using ConsoleMonster.Changes;
using ConsoleMonster.Layout;

string before = "host=web-01\nport=80\nprotocol=http\nworkers=4\n";
string after = "host=web-01\nport=443\nprotocol=https\nworkers=4\n";
TextDiffResult comparison = TextDiff.Compare(before, after, new TextDiffOptions {
    Context = 1,
    ReferenceLabel = "Deployed",
    DifferenceLabel = "Proposed"
});
var view = new Panel(new TextDiffView(comparison))
    .WithTitle("Configuration changes");
Terminal.Write(view);
```

`TextDiffResult.Lines` exposes the original line text, change kind, source line numbers and final-newline state. Comparison uses linear auxiliary memory and a bounded amount of alignment work. If a large changed region exceeds that budget, it is emitted as removals and additions; the result still describes the change, but it may contain more edits than a minimal diff.

Try [the PowerShell command reference](/powershell/commands/compare-consoletext/) for parameter details, or [PowerShell conventions](/docs/powershell-conventions/) for composing ConsoleMonster output. Use `Compare-ConsoleObject` for matching object collections and comparing properties.
