using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>A title and facts joined by a thin leader to a right-aligned state.</summary>
public sealed class ViewHeader : IWidget {
    /// <summary>Creates a header. The title is literal text; facts and state accept markup.</summary>
    public ViewHeader(string title, string? state = null, params string[] facts) {
        Title = title ?? string.Empty; State = state ?? string.Empty;
        if (facts != null) { foreach (string fact in facts) { if (!string.IsNullOrWhiteSpace(fact)) { Facts.Add(fact); } } }
    }
    /// <summary>The literal title.</summary>
    public string Title { get; set; }
    /// <summary>State markup, including an optional observation time.</summary>
    public string State { get; set; }
    /// <summary>Fact markup, joined only at separators.</summary>
    public IList<string> Facts { get; } = new List<string>();
    /// <summary>Whether the header describes an open stream, whose verdict is not known yet.</summary>
    public bool Streaming { get; set; }
    /// <summary>The natural width before applying the compact title layout.</summary>
    public int NaturalWidth(RenderContext context) => context.Markup(Left(context)).Lines[0].Width
        + (Streaming ? context.Glyphs.Get(Glyph.Dot).Length + 2 + "streaming".Length : State.Length == 0 ? 0 : context.Markup(State).Lines[0].Width + 5);
    /// <summary>Natural compact width when the full title/facts/state line cannot fit.</summary>
    public int CompactWidth(int available, RenderContext context) {
        int title = context.Markup("[accent bold]" + Markup.Escape(Title) + "[/]").Lines[0].Width;
        int state = context.Markup(State).Lines[0].Width;
        int first = title + state + 5 <= available ? title + state + 5 : Math.Max(title, state);
        int facts = Facts.Count == 0 ? 0 : new FactFlow(Facts).Measure(new Constraints(available), context).Width;
        return Math.Min(available, Math.Max(first, facts));
    }
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        List<StyledLine> lines = Build(constraints.MaxWidth, context);
        int width = 0;
        foreach (StyledLine line in lines) { width = Math.Max(width, line.Width); }
        return new Size(Math.Min(width, constraints.MaxWidth), Math.Min(lines.Count, constraints.MaxHeight));
    }
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        List<StyledLine> lines = Build(region.Width, context);
        for (int y = 0; y < lines.Count && y < region.Height; y++) { region.Write(0, y, lines[y]); }
    }
    private string Left(RenderContext context) => "[accent bold]" + Markup.Escape(Title) + "[/]"
        + (Facts.Count == 0 ? string.Empty : "[muted] " + context.Glyphs.Get(Glyph.Dot) + " " + string.Join(" " + context.Glyphs.Get(Glyph.Dot) + " ", Facts) + "[/]");
    private List<StyledLine> Build(int width, RenderContext context) {
        string title = "[accent bold]" + Markup.Escape(Title) + "[/]";
        if (Streaming || State.Length == 0) {
            List<string> parts = new List<string> { title };
            foreach (string fact in Facts) parts.Add("[muted]" + fact + "[/]");
            if (Streaming) parts.Add("[muted]streaming[/]");
            return new FactFlow(parts).Lines(width, context);
        }
        StyledLine state = context.Markup(State).Lines[0];
        int leftWidth = context.Markup(Left(context)).Lines[0].Width;
        List<StyledLine> result = new List<StyledLine>();
        if (leftWidth + state.Width + 5 <= width) {
            result.Add(Leader(Left(context), State, width, context));
        } else if (context.Markup(title).Lines[0].Width + state.Width + 5 <= width) {
            result.Add(Leader(title, State, width, context));
            if (Facts.Count > 0) { result.AddRange(new FactFlow(Facts) { Role = Styling.ThemeToken.Muted }.Lines(width, context)); }
        } else {
            result.Add(TextWrapper.Truncate(context.Markup(title).Lines[0], width, context.Ellipsis));
            result.AddRange(new FactFlow(State).Lines(width, context));
            if (Facts.Count > 0) { result.AddRange(new FactFlow(Facts) { Role = Styling.ThemeToken.Muted }.Lines(width, context)); }
        }
        return result;
    }
    private static StyledLine Leader(string left, string state, int width, RenderContext context) {
        int count = Math.Max(3, width - context.Markup(left).Lines[0].Width - context.Markup(state).Lines[0].Width - 2);
        return context.Markup(left + " [subtle]" + new string(context.Glyphs.Get(Glyph.RuleLine)[0], count) + "[/] " + state).Lines[0];
    }
}
