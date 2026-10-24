---
title: Read Markdown in the terminal
description: Read Markdown files with native tables, highlighted code, links and a pager that follows the terminal width.
slug: markdown
order: 17
group: apps
summary: "Headings, lists, quotes, code and tables rendered as native console widgets."
apis: [Show-ConsoleMarkdown, MarkdownConsole, MarkdownView, WidgetPager]
chips: [CommonMark, Tables, Pager]
card: markdown-120
---

`Show-ConsoleMarkdown` reads CommonMark with pipe tables and renders through ConsoleMonster's own widgets. Headings use accent rules, lists keep hanging indents, quotes get a vertical bar, and fenced code uses the registered syntax highlighter with its language label.

## Try it

{{< cm-try key="markdown" ids="markdown-120" project="MarkdownReader" packages="ConsoleMonster.Markdown" caption="One document with headings, nested lists, a quote, a native table, highlighted PowerShell and a link. The C# example uses the optional Markdown add-on; the PowerShell module includes it." >}}

## Files and source text

Use `Show-ConsoleMarkdown -Path .\README.md` to read a file. Paths are literal, resolve from PowerShell's current FileSystem location, and support FileSystem drives. `-Content` takes source text; pipeline strings are joined before parsing, so reference links and multi-line blocks retain their meaning.

`-Width` sets the layout width. `-NoPager` prints the whole document, and `-AsString` returns the complete rendering as one plain string for assignment or saving. A downstream pipeline command receives plain lines.

{{< cm-frame id="markdown-60" script="false" caption="At 60 columns the same source reflows. Table cells and quote content remain complete, and a terminal without OSC 8 shows the link's destination." >}}

## Reading a long document

When a document is taller than a rich interactive screen, the shared pager opens automatically. Use the arrows to move, Space or Page Down to advance, Page Up to go back, Home or End to jump, and q or Escape to close. Its header shows the visible line range and total. Resizing reflows the document and keeps the viewport within its new bounds.

Piped, redirected, transcribed, job, remoting, CI and non-interactive output never opens the pager. The static path prints the complete document; use `-AsString` when capturing from scripts on either PowerShell runtime.

## C# composition

Install the optional `ConsoleMonster.Markdown` package. `MarkdownConsole.Show(content)` writes with automatic interactive paging. `MarkdownConsole.Parse(content)` returns a `MarkdownView` that can be placed in a layout, written through `Terminal.Write`, exported, or passed to `WidgetPager.TryShow` with scripted input and screen capture options.

The parser lives in the add-on and uses OfficeIMO.Markdown. The ConsoleMonster core stays dependency-free. Links follow the terminal capability profile: OSC 8 when supported, a muted URL otherwise. Unsupported or unsafe link schemes remain visible as text. Raw HTML is literal, images become labelled links, and neither scripts nor remote content are executed or fetched.

## Related

See [PowerShell conventions](/docs/powershell-conventions/), [object exploration](/docs/object-explorer/) and [text comparison](/docs/text-diff/).
