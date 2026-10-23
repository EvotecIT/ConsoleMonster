### Unreleased - the everyday path
#### What's new
- `Show-ConsoleObject` explores nested members with lazy expansion, breadcrumbs, search and keyboard selection. `-PassThru` returns the selected value, and `-PassThru Path` returns its PowerShell expression. Redirected and non-interactive sessions print the static inspector.
- HTML and SVG frame titles reserve space after the window dots, truncate with an ellipsis and retain the full title as a tooltip. Very narrow frames keep all three dots visible and hide titles that cannot fit.
- `Out-ConsoleChart` draws horizontal bars, histograms and grouped sparklines from pipeline values or selected properties. Stable value sorting and Top limits, signed bars, whole-number histogram edges and numerical text fallback share the core `PipelineChart` API.
- `Compare-ConsoleText` compares strings or files with line numbers, word highlights and collapsed unchanged ranges. It uses side-by-side output from 120 columns, inline output below that, `-Context`, `-IgnoreWhitespace` and plain unified text with `-AsUnified`. The shared text engine uses linear auxiliary memory and bounded alignment work.
- The interactive apps guide includes complete PowerShell and C# inventory filters, with initial and filtered frames at 120 and 60 columns generated from the shown program and checked against native PowerShell output.
