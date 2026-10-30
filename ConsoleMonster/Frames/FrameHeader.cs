using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Frames;

/// <summary>The frame's title line: title, facts, a thin leader, then the worst states and the observation time.</summary>
public sealed class FrameHeader {
    internal FrameHeader(string title) { Title = title ?? string.Empty; }
    /// <summary>The literal title, usually the view name (<c>Replication</c>).</summary>
    public string Title { get; set; }
    /// <summary>Literal facts in order: scope first, then counts. Empty facts are skipped.</summary>
    public IList<string> Facts { get; } = new List<string>();
    /// <summary>States; the frame sorts them worst first and drops zero counts.</summary>
    public IList<FrameState> States { get; } = new List<FrameState>();
    /// <summary>When the data was observed; shown as local time, with the date when it is not today.</summary>
    public DateTimeOffset? ObservedAt { get; set; }

    internal ViewHeader Build(RenderContext context, DateTimeOffset? now) {
        string state = FrameState.Join(States, context);
        if (ObservedAt.HasValue) {
            string time = DisplayText.Time(ObservedAt.Value, now);
            state = state.Length == 0 ? "[muted]" + Markup.Escape(time) + "[/]" : state + " [muted]" + FrameText.Dot(context) + " " + Markup.Escape(time) + "[/]";
        }
        List<string> facts = new List<string>();
        foreach (string fact in Facts) { if (!string.IsNullOrWhiteSpace(fact)) facts.Add(Markup.Escape(FrameText.Fold(fact, context))); }
        return new ViewHeader(FrameText.Fold(Title, context), FrameText.Fold(state, context), facts.ToArray());
    }
}

/// <summary>A section's title line: bold name, Muted facts, a Subtle leader and the section's state or meta at its right edge.</summary>
internal sealed class SectionTitle : IWidget {
    private readonly string _title;
    private readonly IList<string> _facts;
    private readonly IList<FrameState> _states;
    private readonly string? _meta;
    internal SectionTitle(string title, IList<string> facts, IList<FrameState> states, string? meta) { _title = title; _facts = facts; _states = states; _meta = meta; }

    internal StyledLine Line(int width, bool leader, RenderContext context) {
        string title = "[strong bold]" + FrameText.Escape(_title) + "[/]";
        List<string> facts = new List<string>();
        foreach (string fact in _facts) { if (!string.IsNullOrWhiteSpace(fact)) facts.Add(FrameText.Escape(fact)); }
        string factText = facts.Count == 0 ? string.Empty : "  [muted]" + string.Join(" " + FrameText.Dot(context) + " ", facts) + "[/]";
        string right = FrameState.Join(_states, context);
        if (right.Length == 0 && !string.IsNullOrEmpty(_meta)) right = "[muted]" + FrameText.Escape(_meta) + "[/]";
        StyledLine left = FrameText.Line(title + factText, context);
        if (right.Length == 0 || !leader) return left.Width > width ? TextWrapper.Truncate(left, width, context.Ellipsis) : left;
        StyledLine state = FrameText.Line(right, context);
        int room = width - state.Width - 2;
        if (left.Width + 3 > room) left = TextWrapper.Truncate(left, Math.Max(1, room - 3), context.Ellipsis);
        int rule = Math.Max(1, width - left.Width - state.Width - 2);
        List<TextSpan> spans = new List<TextSpan>(left);
        spans.Add(new TextSpan(" "));
        foreach (TextSpan span in FrameText.Line("[subtle]" + new string(context.Glyphs.Get(Glyphs.Glyph.RuleLine)[0], rule) + "[/]", context)) spans.Add(span);
        spans.Add(new TextSpan(" "));
        spans.AddRange(state);
        StyledLine line = new StyledLine(spans);
        return line.Width > width ? TextWrapper.Truncate(line, width, context.Ellipsis) : line;
    }

    internal int NaturalWidth(RenderContext context) {
        int left = Line(int.MaxValue / 2, false, context).Width;
        string right = FrameState.Join(_states, context);
        if (right.Length == 0 && !string.IsNullOrEmpty(_meta)) right = "[muted]" + FrameText.Escape(_meta) + "[/]";
        return right.Length == 0 ? left : left + FrameText.Width(right, context) + 5;
    }

    public Size Measure(Constraints constraints, RenderContext context) =>
        new Size(Math.Min(constraints.MaxWidth, NaturalWidth(context)), Math.Min(1, constraints.MaxHeight));

    public void Render(Region region, RenderContext context) {
        if (region.Height > 0) region.Write(0, 0, Line(region.Width, true, context));
    }
}
