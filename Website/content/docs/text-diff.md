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

`Compare-ConsoleText` shows what changed between two strings or files: removed lines with a minus marker in the failure tone, added lines with a plus marker in the success tone, and inside a replaced line only the words that changed are highlighted.

## Try it

{{< cm-try key="text-diff" ids="text-diff-120" project="TextComparison" caption="Two versions of a small configuration, compared with one line of context. At 120 columns the two sides stand next to each other." >}}

## How it works

`-ReferenceText` and `-DifferenceText` (or `TextDiff.Compare` in C#) take the two versions; the comparison is line by line, then word by word inside lines that were replaced. Text comparisons are headed `Before` and `After`.

- **Layout follows the width.** At 120 columns or more, the two sides appear side by side. Narrower terminals show the removed lines followed by the added lines, with both original line numbers in the gutter. The minus and plus markers stay visible without colour.
- **Context.** `-Context` sets how many unchanged lines are kept before and after each change. It defaults to three; zero shows only the changed lines. Longer unchanged runs collapse to a count with the reference and difference line ranges.
- **Long lines** wrap at the available width instead of being cut.

{{< cm-frame id="text-diff-60" script="false" caption="The same comparison at 60 columns: removed lines first, then added lines, with both line numbers." >}}

## Files

Compare files with normal PowerShell paths, including relative paths, filesystem drives and provider-qualified paths. Each path must resolve to exactly one readable file; wildcards that match several files, directories, missing files and other providers are errors. Files are read as UTF-8 with byte order mark detection.

```powershell
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf -IgnoreWhitespace
Compare-ConsoleText -ReferencePath .\before.conf -DifferencePath .\after.conf -ReferenceName 'Deployed' -DifferenceName 'Proposed'
```

File comparisons are headed with the resolved file names, such as `before.conf` and `after.conf`; `-ReferenceName` and `-DifferenceName` override either heading, for files or text. `-IgnoreWhitespace` ignores Unicode whitespace within each line. It does not join lines or ignore a missing final newline. CRLF, LF and CR line endings compare equally, and empty text is an empty document rather than a blank line.

## Unified output and capture

For a string you can save or process, use `-AsUnified`. To keep the rendered view at a fixed width, capture it with `Use-ConsoleTarget`:

```powershell
$diff = Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1 -AsUnified
$diff | Set-Content .\changes.diff

$report = Use-ConsoleTarget -Width 120 {
    Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1
}
```

The unified string has reference and difference headers, hunk ranges and `-`/`+` lines, with LF separators and a marker for a missing final newline. Equal inputs return an empty string, and no terminal styling or markup is added. Normal rendering writes to the host and returns no comparison objects.

## In a C# layout

`TextDiffView` is an ordinary widget, so a comparison composes with the rest of a layout. This complete program puts it in a panel with its own headings:

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
    .Title("Configuration changes");
Terminal.Write(view);
```

`TextDiffResult.Lines` exposes the original line text, change kind, source line numbers and final-newline state. Comparison uses linear auxiliary memory and a bounded amount of alignment work. If a large changed region exceeds that budget, it is emitted as removals and additions; the result still describes the change, but it may contain more edits than a minimal diff.

## Related

The [Compare-ConsoleText reference](/powershell/commands/compare-consoletext/) lists every parameter, [PowerShell conventions](/docs/powershell-conventions/) covers composing ConsoleMonster output, and `Compare-ConsoleObject` compares object collections property by property.
