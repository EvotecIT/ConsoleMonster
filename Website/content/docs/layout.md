---
title: Layout
description: "Arrange widgets with rows, columns, panels and grids; size them with weights, fixed widths and heights; align, pad, layer and scroll them."
slug: layout
order: 20
group: build
card: layout-80
apis: [New-ConsoleRows, New-ConsoleColumns, New-ConsolePanel, New-ConsoleLayoutGrid, New-ConsoleStack, New-ConsoleScrollView, Rows, Columns, LayoutGrid]
chips: [Rows, Columns, Grid, Sizing]
summary: "Rows, columns, panels and grids, with weights, fixed sizes, alignment, padding, layers and scrolling."
---

This page shows how to arrange widgets on the screen: one below another, side by side, in a bordered panel or in an even grid, and how each container shares out the width it is given. The example puts two panels side by side, the first twice as wide, above a grid of six hosts.

## Try it

{{< cm-try key="layout" ids="layout-80" project="LayoutExample" caption="Columns with a 2:1 weight above a three-column LayoutGrid, at 80 columns." >}}

## The containers

| PowerShell | C# | Arranges its children |
|---|---|---|
| `New-ConsoleRows` | `Rows` | One below another. `-Gap` (`Gap`) adds blank rows between them. |
| `New-ConsoleColumns` | `Columns` | Side by side, sharing the width. `-Gap` sets the blank columns between them (one by default). |
| `New-ConsolePanel` | `Panel` | Inside a border with a title; `-HeaderRight` and `-Caption` add text to the top and bottom edges. |
| `New-ConsoleLayoutGrid` | `LayoutGrid` | In equal columns, filled row by row. |
| `New-ConsoleStack` | `Stack` | On top of each other in the same space, later layers painting over earlier ones. |
| `New-ConsoleScrollView` | `ScrollView` | In a clipped viewport that scrolls, inside an interactive app. |

In PowerShell every container takes its children as a script block in `-Content`; whatever the block outputs becomes a child, and a plain string becomes a line of text with markup. In C# the children go to the constructor or to `Add`, and in fluent code to `Content(c => ...)`.

## Sharing the width

Children of `New-ConsoleColumns` share the width equally until one asks for more:

- **Weight.** `-Weight 2` on a child (`.Fill(2)` in C#) gives it twice the share of a child without one. The example's Results panel uses it.
- **Fixed widths.** `-FixedWidth 0, 0, 17` gives the third child exactly 17 cells and lets the first two share the rest. In C#, `columns.Add(child, LayoutSize.Fixed(17))` does the same.
- **Narrow windows.** When the children no longer fit their minimum widths, the columns stack one below another instead of squeezing the content. `-CollapseBelow 90` (`CollapseBelow = 90`) makes them stack below 90 cells.

<div class="cm-lang" data-lang="powershell">

```powershell
New-ConsoleColumns -FixedWidth 0, 0, 17 -CollapseBelow 90 -Content {
    New-ConsolePanel -Title 'Failures' -Weight 1.5 -Content { 'No failures' }
    New-ConsolePanel -Title 'Slowest' -Content { 'DC02 · 4.1 s' }
    New-ConsolePanel -Title 'Owner' -Content { 'Platform' }
} | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Columns columns = new Columns { CollapseBelow = 90 };
columns.Add(new Panel(new TextBlock("No failures"), "Failures").Fill(1.5));
columns.Add(new Panel(new TextBlock("DC02 · 4.1 s"), "Slowest"));
columns.Add(new Panel(new TextBlock("Platform"), "Owner"), LayoutSize.Fixed(17));
Terminal.Write(columns);
```

</div>

Rows take their natural height in printed output. In a live dashboard, rows share the screen height instead: `-Fill` (`.Fill()`) gives a child the remaining height, `-Height 8` (`.Fixed(8)`) fixes it, and `-Weight` shares what is left in proportion.

## Grids

`New-ConsoleLayoutGrid -Columns 3` (`new LayoutGrid(3, ...)`) lays its children out in equal columns, row by row. When the columns would become narrower than `-MinColumnWidth` (24 cells by default), the grid uses fewer of them, so the same grid shows three columns on a wide terminal and one on a phone-width one. `-ColumnGap` and `-RowGap` set the space between cells, and `-NoFillHeight` (`FillHeight = false`) lets each row keep its natural height instead of matching the tallest cell.

## Alignment and padding

- **Panels.** A panel's content has one cell of padding on the left and right. `-PaddingLeft`, `-PaddingRight`, `-PaddingTop` and `-PaddingBottom` change it, and `-NoExpand` (`Expand = false`) makes the panel only as wide as its content.
- **Regions.** `New-ConsoleRegion -Horizontal Center -Padding 1` places content inside the space it is given: `-Horizontal` is `Left`, `Center` or `Right`, `-Vertical` is `Top`, `Middle` or `Bottom`, and `-Padding` takes one value for all sides, two for horizontal and vertical, or four for left, top, right and bottom. In C# the same are `new Align(content, HorizontalAlignment.Center)` and `new Padding(content, left, top, right, bottom)`.
- **Text.** `New-ConsoleText -Alignment Center` and `Write-ConsoleText -Alignment Center -Width 60` align a line of text.

## Layers

`New-ConsoleStack` (`Stack`) draws its children in the same space, bottom first. Combined with an aligned region it puts a badge in the corner of a chart:

<div class="cm-lang" data-lang="powershell">

```powershell
$load = 12, 18, 26, 31, 40, 47, 55, 64, 73, 79
New-ConsolePanel -Title 'Load' -Content {
    New-ConsoleStack -Content {
        New-ConsoleChart -Height 3 -NoLatest -Type Sparkline -Values $load
        New-ConsoleRegion -Horizontal Right -Content { '[pass] LIVE [/]' }
    }
} | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Sparkline load = new Sparkline { Height = 3, ShowLatest = false }
    .AddRange(new double[] { 12, 18, 26, 31, 40, 47, 55, 64, 73, 79 });
Align badge = new Align(TextBlock.FromMarkup("[pass] LIVE [/]"), HorizontalAlignment.Right);
Terminal.Write(new Panel(new Stack(load, badge), "Load"));
```

</div>

To place widgets one below another, use rows; a stack only overlays.

## Scrolling inside an app

`New-ConsoleScrollView` (`ScrollView`) clips a small widget tree to a viewport inside an [interactive app](/docs/interactive-apps/). The mouse wheel scrolls it and Tab scrolls a focused input into view. `-Height` fixes the viewport, `-Fill` gives it the remaining height and `-ScrollOffset` sets the first visible row. Long lists, grids and logs keep their own scrolling and do not need one.

```powershell
Show-ConsoleApp -Title 'Details' -Content {
    New-ConsoleScrollView -Height 6 -Content {
        1..20 | ForEach-Object { New-ConsoleText "Detail line $_" }
    }
}
```

## Next

- [Facts, bars and hierarchy](/docs/facts-and-bars/): what to put inside the layout.
- [Dashboard frames](/docs/dashboard-frames/): a ready-made summary layout with meters and sections.
- [Live sessions](/docs/live-sessions/): the same layouts kept on screen while work runs.
