using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Frames;

/// <summary>A column of <see cref="GroupedRows{T}"/>.</summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class GroupedColumn<T> {
    /// <summary>Creates a column.</summary>
    /// <param name="header">The literal header, sentence case.</param><param name="cell">Builds a cell's markup; escape literal text with <see cref="Markup.Escape(string?)"/>.</param>
    public GroupedColumn(string header, Func<T, RenderContext, string> cell) {
        Header = header ?? string.Empty;
        Cell = cell ?? throw new ArgumentNullException(nameof(cell));
    }
    /// <summary>The literal header.</summary>
    public string Header { get; }
    /// <summary>The cell builder.</summary>
    public Func<T, RenderContext, string> Cell { get; }
    /// <summary>Right-aligns the header and cells (numbers, ages).</summary>
    public bool AlignRight { get; set; }
    /// <summary>A fixed width, for bars and axes; null sizes to the content.</summary>
    public int? Width { get; set; }
    /// <summary>The widest the column may become before cells truncate.</summary>
    public int? MaxWidth { get; set; }
    /// <summary>A literal line printed under the header rule in this column, for an axis's labels.</summary>
    public string? Axis { get; set; }
}

/// <summary>The heading of one group: its literal facts and its states.</summary>
public sealed class GroupHeading {
    /// <summary>Literal Muted facts after the group key (<c>6 DCs</c>).</summary>
    public IList<string> Facts { get; } = new List<string>();
    /// <summary>The group's states (<c>▴ 1 not current</c>).</summary>
    public IList<FrameState> States { get; } = new List<FrameState>();
}

/// <summary>
/// A detail table grouped by a key. Every group shares one column layout; group headings carry their own rollup.
/// Long single-column tables repeat their header; wide terminals flow groups into columns without splitting a group.
/// Healthy rows collapse into the group heading in large sections, and rows stop at the length's cap with <c>+N more</c>.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class GroupedRows<T> : IWidget {
    private const int FlowGap = 4;
    private readonly IReadOnlyList<T> _items;
    private readonly Func<T, string> _group;

    /// <summary>Creates grouped rows.</summary>
    /// <param name="items">The rows, already in display order within each group.</param><param name="group">The group key; groups appear in first-seen order.</param>
    /// <param name="scale">The frame's scale, from the section context.</param>
    public GroupedRows(IEnumerable<T> items, Func<T, string> group, FrameScale scale) {
        _items = (items ?? throw new ArgumentNullException(nameof(items))).ToList();
        _group = group ?? throw new ArgumentNullException(nameof(group));
        Scale = scale ?? throw new ArgumentNullException(nameof(scale));
    }

    /// <summary>The columns.</summary>
    public IList<GroupedColumn<T>> Columns { get; } = new List<GroupedColumn<T>>();
    /// <summary>The frame's scale.</summary>
    public FrameScale Scale { get; }
    /// <summary>Builds a group's heading; null shows the key only.</summary>
    public Func<string, IReadOnlyList<T>, GroupHeading>? Heading { get; set; }
    /// <summary>Whether a row is healthy, for collapsing large sections; null never collapses.</summary>
    public Func<T, bool>? IsHealthy { get; set; }
    /// <summary>The singular noun for rows, for <c>+N more</c> and collapsed counts.</summary>
    public string Noun { get; set; } = "row";
    /// <summary>The plural noun; null adds an <c>s</c>.</summary>
    public string? PluralNoun { get; set; }
    /// <summary>The word for collapsed healthy rows (default <c>healthy</c>).</summary>
    public string HealthyWord { get; set; } = "healthy";

    private string Count(long count) => Presenters.DisplayText.Count(count, Noun, PluralNoun);

    private sealed class Layout {
        internal int[] Widths = Array.Empty<int>();
        internal int Total;
        internal List<List<StyledLine>> Groups = new List<List<StyledLine>>();
        internal List<StyledLine> Notes = new List<StyledLine>();
        internal StyledLine Header = StyledLine.From(string.Empty);
        internal StyledLine Rule = StyledLine.From(string.Empty);
        internal StyledLine? Axis;
    }

    private Layout Build(int width, RenderContext context) {
        Layout layout = new Layout();
        bool collapse = IsHealthy != null && Scale.HealthyCollapseAbove.HasValue && _items.Count > Scale.HealthyCollapseAbove.Value;
        int cap = Scale.DetailCap ?? int.MaxValue;
        List<(string Key, List<T> Rows)> groups = new List<(string, List<T>)>();
        Dictionary<string, int> index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (T item in _items) {
            string key = _group(item) ?? string.Empty;
            if (!index.TryGetValue(key, out int at)) { at = groups.Count; index[key] = at; groups.Add((key, new List<T>())); }
            groups[at].Rows.Add(item);
        }

        // Widths come from every row that prints, so all groups and flowed columns share one layout.
        int columns = Columns.Count;
        int[] widths = new int[columns];
        string[][] cells = new string[_items.Count][];
        for (int c = 0; c < columns; c++) widths[c] = Columns[c].Width ?? FrameText.Width(FrameText.Escape(Columns[c].Header), context);
        int printed = 0;
        long collapsed = 0;
        List<(string Key, List<T> Rows, List<T> Shown)> plan = new List<(string, List<T>, List<T>)>();
        foreach ((string key, List<T> rows) in groups) {
            List<T> shown = new List<T>();
            foreach (T row in rows) {
                if (collapse && IsHealthy!(row)) { collapsed++; continue; }
                if (printed >= cap) continue;
                shown.Add(row); printed++;
            }
            plan.Add((key, rows, shown));
        }
        int r = 0;
        foreach ((string _, List<T> _, List<T> shown) in plan) {
            foreach (T row in shown) {
                string[] values = new string[columns];
                for (int c = 0; c < columns; c++) {
                    values[c] = Columns[c].Cell(row, context) ?? string.Empty;
                    if (!Columns[c].Width.HasValue) widths[c] = Math.Max(widths[c], FrameText.Width(values[c], context));
                }
                cells[r] = values; r++;
            }
        }
        for (int c = 0; c < columns; c++) { if (Columns[c].MaxWidth.HasValue) widths[c] = Math.Min(widths[c], Columns[c].MaxWidth!.Value); }
        int total = widths.Sum() + 2 * Math.Max(0, columns - 1);
        for (int c = columns - 1; c >= 0 && total > width; c--) {
            if (Columns[c].Width.HasValue) continue;
            int take = Math.Min(total - width, Math.Max(0, widths[c] - 4));
            widths[c] -= take; total -= take;
        }
        layout.Widths = widths;
        layout.Total = Math.Min(width, total);

        List<TextSpan> header = new List<TextSpan>();
        List<TextSpan> rule = new List<TextSpan>();
        string line = context.Glyphs.Get(Glyphs.Glyph.RuleLine);
        for (int c = 0; c < columns; c++) {
            if (c > 0) { header.Add(new TextSpan("  ")); rule.Add(new TextSpan("  ")); }
            header.AddRange(FrameText.Fit(FrameText.Line("[muted]" + FrameText.Escape(Columns[c].Header) + "[/]", context), widths[c], Columns[c].AlignRight, context));
            rule.AddRange(FrameText.Line("[subtle]" + new string(line[0], widths[c]) + "[/]", context));
        }
        layout.Header = new StyledLine(header);
        layout.Rule = new StyledLine(rule);
        if (Columns.Any(column => column.Axis != null)) {
            List<TextSpan> axis = new List<TextSpan>();
            for (int c = 0; c < columns; c++) {
                if (c > 0) axis.Add(new TextSpan("  "));
                axis.AddRange(FrameText.Fit(FrameText.Line("[muted]" + FrameText.Escape(Columns[c].Axis ?? string.Empty) + "[/]", context), widths[c], false, context));
            }
            layout.Axis = new StyledLine(axis);
        }

        r = 0;
        foreach ((string key, List<T> rows, List<T> shown) in plan) {
            if (shown.Count == 0 && !collapse) continue;
            List<StyledLine> lines = new List<StyledLine> { GroupLine(key, rows, layout.Total, context) };
            foreach (T _ in shown) {
                List<TextSpan> spans = new List<TextSpan>();
                for (int c = 0; c < columns; c++) {
                    if (c > 0) spans.Add(new TextSpan("  "));
                    spans.AddRange(FrameText.Fit(FrameText.Line(cells[r][c], context), widths[c], Columns[c].AlignRight, context));
                }
                lines.Add(new StyledLine(spans)); r++;
            }
            layout.Groups.Add(lines);
        }
        if (collapsed > 0) {
            layout.Notes.Add(FrameText.Line("[muted]" + FrameText.Number(collapsed, context) + " " + HealthyWord + " " + (collapsed == 1 ? Noun : PluralNoun ?? Noun + "s") + " not listed " + FrameText.Dot(context)
                + " $ConsoleViewLengthPreference = 'Full' lists every row[/]", context));
        }
        long hidden = _items.Count - collapsed - printed;
        if (hidden > 0) layout.Notes.Add(FrameText.Line("[muted]+" + FrameText.Number(hidden, context) + " more " + FrameText.Dot(context) + " pipe to Format-Table for every row[/]", context));
        return layout;
    }

    private StyledLine GroupLine(string key, IReadOnlyList<T> rows, int width, RenderContext context) {
        GroupHeading heading = Heading?.Invoke(key, rows) ?? new GroupHeading();
        List<string> facts = new List<string>();
        foreach (string fact in heading.Facts) { if (!string.IsNullOrWhiteSpace(fact)) facts.Add(FrameText.Escape(fact)); }
        string state = FrameState.Join(heading.States, context);
        string markup = "[strong bold]" + FrameText.Escape(key) + "[/]";
        if (facts.Count > 0) markup += "  [muted]" + string.Join(" " + FrameText.Dot(context) + " ", facts) + "[/]";
        if (state.Length > 0) markup += (facts.Count > 0 ? " [muted]" + FrameText.Dot(context) + "[/] " : "  ") + state;
        StyledLine result = FrameText.Line(markup, context);
        return result.Width > width ? TextWrapper.Truncate(result, width, context.Ellipsis) : result;
    }

    private List<List<StyledLine>> Flow(Layout layout, int width) {
        int flows = Math.Max(1, Math.Min(Scale.DetailColumns(layout.Total), (width + FlowGap) / (layout.Total + FlowGap)));
        List<List<StyledLine>> result = new List<List<StyledLine>>();
        if (flows == 1) {
            List<StyledLine> lines = new List<StyledLine> { layout.Header, layout.Rule };
            if (layout.Axis != null) lines.Add(layout.Axis);
            int since = 0;
            foreach (List<StyledLine> group in layout.Groups) {
                foreach (StyledLine line in group) {
                    if (since >= Scale.HeaderRepeat) { lines.Add(layout.Header); since = 0; }
                    lines.Add(line); since++;
                }
            }
            result.Add(lines);
            return result;
        }
        int totalLines = layout.Groups.Sum(group => group.Count);
        int target = (totalLines + flows - 1) / flows;
        List<StyledLine> current = new List<StyledLine>();
        int count = 0;
        foreach (List<StyledLine> group in layout.Groups) {
            if (count > 0 && count + group.Count > target && result.Count < flows - 1) { result.Add(current); current = new List<StyledLine>(); count = 0; }
            current.AddRange(group); count += group.Count;
        }
        result.Add(current);
        foreach (List<StyledLine> column in result) {
            column.Insert(0, layout.Rule);
            column.Insert(0, layout.Header);
            if (layout.Axis != null) column.Insert(2, layout.Axis);
        }
        return result;
    }

    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        Layout layout = Build(constraints.MaxWidth, context);
        List<List<StyledLine>> flows = Flow(layout, constraints.MaxWidth);
        int height = flows.Max(column => column.Count) + layout.Notes.Count;
        int width = flows.Count * layout.Total + (flows.Count - 1) * FlowGap;
        foreach (StyledLine note in layout.Notes) width = Math.Max(width, note.Width);
        return new Size(Math.Min(constraints.MaxWidth, width), Math.Min(constraints.MaxHeight, height));
    }

    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        Layout layout = Build(region.Width, context);
        List<List<StyledLine>> flows = Flow(layout, region.Width);
        int x = 0;
        foreach (List<StyledLine> column in flows) {
            for (int y = 0; y < column.Count && y < region.Height; y++) region.Write(x, y, column[y], layout.Total);
            x += layout.Total + FlowGap;
        }
        int top = flows.Max(column => column.Count);
        for (int i = 0; i < layout.Notes.Count && top + i < region.Height; i++) region.Write(0, top + i, layout.Notes[i], region.Width);
    }
}
