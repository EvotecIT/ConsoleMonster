using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Frames;

/// <summary>
/// A static dashboard frame for summary output: title line, meters, <c>Needs attention</c>, overview sections packed
/// side by side, detail sections, legend and footer. It prints once and scrolls like any output. The frame applies the
/// width tiers, the length preference, the ordering and the footer hint, so every product frame follows the same rules.
/// </summary>
public sealed class DashboardFrame : IWidget {
    private const int SectionGap = 2;

    /// <summary>Creates a frame.</summary>
    /// <param name="title">The literal title, usually the view name.</param>
    public DashboardFrame(string title) { Header = new FrameHeader(title); }

    /// <summary>The title line.</summary>
    public FrameHeader Header { get; }
    /// <summary>Headline meters, in priority order.</summary>
    public IList<Meter> Meters { get; } = new List<Meter>();
    /// <summary>The attention list; it prints first in the summary rows whenever it has items.</summary>
    public AttentionList Attention { get; } = new AttentionList();
    /// <summary>Overview and detail sections, in the order they print.</summary>
    public IList<FrameSection> Sections { get; } = new List<FrameSection>();
    /// <summary>The legend.</summary>
    public FrameLegend Legend { get; } = new FrameLegend();
    /// <summary>The footer.</summary>
    public FrameFooter Footer { get; } = new FrameFooter();
    /// <summary>An explicit length; null uses the session preference or Standard.</summary>
    public ConsoleViewLength? Length { get; set; }
    /// <summary>An explicit size and length policy, for previews and tests; null reads the destination.</summary>
    internal FrameScale? Scale { get; set; }
    /// <summary>Below this terminal width the frame prints <see cref="Compact"/> and the meters as one counts line (default 80).</summary>
    internal int CompactBelow { get; set; } = 80;
    /// <summary>The reference time for the observation time on the title line; null uses the clock. For previews and tests.</summary>
    internal DateTimeOffset? Now { get; set; }
    /// <summary>
    /// The type's compact view for narrow terminals; null stacks the frame's own sections. The compact view replaces the
    /// title line, attention list and sections, so it must carry its own title line and list every problem; the frame adds
    /// the meter counts and the footer.
    /// </summary>
    public Func<FrameScale, IWidget>? Compact { get; set; }

    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        context = ConsoleViewStyle.Apply(context);
        (IWidget body, int width) = Compose(constraints.MaxWidth, context);
        Size size = body.Measure(new Constraints(width, constraints.MaxHeight), context);
        return new Size(Math.Min(constraints.MaxWidth, width + 2), size.Height);
    }

    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        context = ConsoleViewStyle.Apply(context);
        (IWidget body, int width) = Compose(region.Width, context);
        body.Render(region.Sub(1, 0, Math.Min(width, Math.Max(0, region.Width - 1)), region.Height), context);
        if (context.Glyphs.Tier == Capabilities.GlyphTier.Ascii) AsciiCells(region);
    }

    /// <summary>
    /// The ASCII tier's guarantee for everything inside the frame, including author widgets: text is folded before
    /// measuring, and any cell still outside printable ASCII is replaced in place without changing the layout.
    /// </summary>
    private static void AsciiCells(Region region) {
        for (int y = 0; y < region.Height; y++) {
            for (int x = 0; x < region.Width; x++) {
                Cell cell = region.Get(x, y);
                if (cell.IsContinuation || string.IsNullOrEmpty(cell.Glyph) || IsAscii(cell.Glyph)) continue;
                region.Put(x, y, Single(cell.Glyph[0]).ToString(), cell.Style, 1);
                for (int c = 1; c < cell.Width && x + c < region.Width; c++) region.Put(x + c, y, " ", cell.Style, 1);
            }
        }
    }

    private static bool IsAscii(string glyph) {
        foreach (char c in glyph) { if (c < ' ' || c > '~') return false; }
        return true;
    }

    private static char Single(char c) {
        switch (c) {
            case '·': case '—': case '–': case '−': case '─': return '-';
            case '←': return '<';
            case '→': return '>';
            case '…': return '.';
            case '\u00A0': case '\u202F': case '\u2009': return ' ';
            case '\u2018': case '\u2019': return '\'';
            default: return '?';
        }
    }

    private (IWidget Body, int Width) Compose(int available, RenderContext context) {
        FrameScale scale = Scale ?? FrameScale.For(context.Profile, Length);
        int content = Math.Max(1, available - 2);
        ViewHeader header = HeaderFor(context);
        if (available < CompactBelow && Compact != null) {
            Rows compact = new Rows(Compact(scale));
            if (Meters.Count > 0) {
                List<string> counts = new List<string>();
                foreach (Meter meter in Meters) counts.Add(FrameText.Fold(meter.CountsText(context), context));
                compact.Add(new FactFlow(counts) { Role = Styling.ThemeToken.Muted });
            }
            compact.Add(Footer.Build(context));
            return (compact, content);
        }

        List<IWidget> blocks = new List<IWidget>();
        List<FrameSection> summary = new List<FrameSection>();
        if (Attention.Count > 0) summary.Add(Attention.ToSection(scale, context));
        foreach (FrameSection section in Sections) { if (section.Placement == FramePlacement.Summary) summary.Add(section); }
        blocks.AddRange(Summary(summary, content, scale, context));
        if (scale.DetailCap != 0) {
            foreach (FrameSection section in Sections) {
                if (section.Placement == FramePlacement.Detail) blocks.Add(section.Build(content, scale, context));
            }
        }
        Rows tail = new Rows();
        if (!Legend.IsEmpty) tail.Add(Legend.Build(context));
        tail.Add(Footer.Build(context));
        blocks.Add(tail);

        MeterRow meters = new MeterRow(Meters, scale);
        int width = Math.Min(content, Math.Max(meters.Measure(new Constraints(content), context).Width, header.NaturalWidth(context)));
        foreach (IWidget block in blocks) width = Math.Max(width, Math.Min(content, block.Measure(new Constraints(content), context).Width));

        Rows top = new Rows(new Lane(new IWidget[] { header }, new[] { width }, 0));
        if (Meters.Count > 0) top.Add(meters);
        Rows body = new Rows(top) { Gap = 1 };
        foreach (IWidget block in blocks) body.Add(block);
        return (body, width);
    }

    private ViewHeader HeaderFor(RenderContext context) {
        bool anyState = Header.States.Count > 0;
        bool allEmpty = true;
        foreach (FrameState state in Header.States) { if (!state.IsEmpty) allEmpty = false; }
        ViewHeader header = Header.Build(context, Now);
        if (anyState && allEmpty && Attention.Count == 0) {
            FrameHeader healthy = new FrameHeader(Header.Title) { ObservedAt = Header.ObservedAt };
            foreach (string fact in Header.Facts) healthy.Facts.Add(fact);
            healthy.States.Add(FrameState.Healthy("all healthy"));
            header = healthy.Build(context, Now);
        }
        return header;
    }

    /// <summary>
    /// Lays out the summary sections. Consecutive sections pack into a row while their minimum widths and gaps fit, in
    /// order. Later sections then drop into the shortest column of that row they fit, so no section waits below its
    /// tallest neighbour; a section that fits no column, or takes the full width, starts the next row.
    /// </summary>
    private static List<IWidget> Summary(List<FrameSection> sections, int content, FrameScale scale, RenderContext context) {
        List<IWidget> rows = new List<IWidget>();
        int index = 0;
        while (index < sections.Count) {
            List<FrameSection> row = new List<FrameSection>();
            int used = 0;
            while (index < sections.Count) {
                FrameSection section = sections[index];
                int minimum = section.Minimum(content, scale, context);
                bool alone = scale.Stacked || section.FullWidth || (row.Count > 0 && row[0].FullWidth);
                if (row.Count > 0 && (alone || used + SectionGap + minimum > content)) break;
                used += (row.Count > 0 ? SectionGap : 0) + minimum;
                row.Add(section);
                index++;
            }
            int[] widths = Widths(row, content, scale, context);
            Rows[] columns = new Rows[row.Count];
            int[] heights = new int[row.Count];
            for (int i = 0; i < row.Count; i++) {
                IWidget block = row[i].Build(widths[i], scale, context);
                columns[i] = new Rows(block) { Gap = 1 };
                heights[i] = block.Measure(new Constraints(widths[i]), context).Height;
            }
            while (row.Count > 1 && index < sections.Count && !sections[index].FullWidth) {
                FrameSection section = sections[index];
                int minimum = section.Minimum(content, scale, context);
                int target = -1;
                for (int i = 0; i < row.Count; i++) {
                    if (minimum <= widths[i] && (target < 0 || heights[i] < heights[target])) target = i;
                }
                if (target < 0) break;
                // A stacked section keeps its own width unless it grows, so its title line ends where its content does.
                int width = section.Grow > 0 ? Math.Min(widths[target], section.MaxWidth ?? widths[target]) : Math.Max(minimum, section.Natural(widths[target], scale, context));
                IWidget block = section.Build(width, scale, context);
                columns[target].Add(new Lane(new[] { block }, new[] { width }, 0));
                heights[target] += 1 + block.Measure(new Constraints(width), context).Height;
                index++;
            }
            rows.Add(new Lane(columns, widths, SectionGap));
        }
        return rows;
    }

    /// <summary>Shares a row's width: minimums first, then natural widths for fixed sections, then the rest by grow weight.</summary>
    private static int[] Widths(List<FrameSection> row, int content, FrameScale scale, RenderContext context) {
        int available = content - SectionGap * (row.Count - 1);
        int[] minimum = new int[row.Count];
        int[] natural = new int[row.Count];
        int[] widths = new int[row.Count];
        for (int i = 0; i < row.Count; i++) {
            minimum[i] = row[i].Minimum(available, scale, context);
            natural[i] = Math.Max(minimum[i], row[i].Natural(available, scale, context));
        }
        int sum = 0;
        for (int i = 0; i < row.Count; i++) { widths[i] = minimum[i]; sum += minimum[i]; }
        // Fixed-size sections reach their natural width first; growing sections share what is left.
        int rest = Math.Max(0, available - sum);
        foreach (bool growing in new[] { false, true }) {
            for (int i = 0; i < row.Count && rest > 0; i++) {
                if ((row[i].Grow > 0) != growing) continue;
                int add = Math.Min(rest, natural[i] - widths[i]);
                widths[i] += add; rest -= add;
            }
        }
        if (row.Count == 1 && (row[0].FullWidth || row[0].Grow > 0)) { widths[0] = Math.Min(available, row[0].MaxWidth ?? available); rest = 0; }
        while (rest > 0) {
            double weights = 0;
            for (int i = 0; i < row.Count; i++) {
                if (widths[i] < (row[i].MaxWidth ?? available)) weights += Math.Max(0, row[i].Grow);
            }
            if (weights <= 0) break;
            int given = 0, last = -1;
            for (int i = 0; i < row.Count; i++) {
                if (row[i].Grow <= 0 || widths[i] >= (row[i].MaxWidth ?? available)) continue;
                int share = (int)Math.Floor(rest * row[i].Grow / weights);
                if (row[i].MaxWidth.HasValue) share = Math.Min(share, Math.Max(0, row[i].MaxWidth!.Value - widths[i]));
                widths[i] += share; given += share; last = i;
            }
            rest -= given;
            if (given == 0 && last >= 0) { widths[last]++; rest--; }
        }
        return widths;
    }
}
