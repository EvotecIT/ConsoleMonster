using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>Flows facts at separators without breaking a fact into unrelated words.</summary>
public sealed class FactFlow : IWidget {
    // Preserve the styles of complete entries while breaking only at their separators.
    internal static List<StyledLine> WrapSeparated(StyledLine line, int width, string separator) {
        List<StyledLine> result = new List<StyledLine>();
        string plain = line.PlainText;
        List<TextSpan> current = new List<TextSpan>();
        int position = 0;
        while (position <= plain.Length) {
            int next = plain.IndexOf(separator, position, StringComparison.Ordinal);
            int end = next < 0 ? plain.Length : next;
            StyledLine part = Slice(position, end);
            List<TextSpan> joinedSpans = new List<TextSpan>(current);
            if (current.Count > 0) joinedSpans.AddRange(Slice(position - separator.Length, position));
            joinedSpans.AddRange(part);
            StyledLine joined = new StyledLine(joinedSpans);
            if (current.Count > 0 && joined.Width > width) {
                result.Add(new StyledLine(current));
                current.Clear();
            } else if (current.Count > 0) {
                current.AddRange(Slice(position - separator.Length, position));
            }
            if (part.Width > width) { result.AddRange(TextWrapper.Wrap(part, Math.Max(1, width))); }
            else { current.AddRange(part); }
            if (next < 0) break;
            position = next + separator.Length;
        }
        if (current.Count > 0) result.Add(new StyledLine(current));
        return result;

        StyledLine Slice(int start, int end) {
            List<TextSpan> spans = new List<TextSpan>();
            int offset = 0;
            foreach (TextSpan span in line) {
                int from = Math.Max(start, offset), to = Math.Min(end, offset + span.Text.Length);
                if (to > from) spans.Add(new TextSpan(span.Text.Substring(from - offset, to - from), span.Style));
                offset += span.Text.Length;
            }
            return new StyledLine(spans);
        }
    }
    /// <summary>Creates a flow from markup parts.</summary>
    public FactFlow(params string[] parts) : this((IEnumerable<string>)parts) { }
    /// <summary>Creates a flow from markup parts; empty parts are omitted.</summary>
    public FactFlow(IEnumerable<string> parts) {
        if (parts == null) { throw new ArgumentNullException(nameof(parts)); }
        foreach (string part in parts) { if (!string.IsNullOrWhiteSpace(part)) { Parts.Add(part); } }
    }
    /// <summary>Markup facts in display order.</summary>
    public IList<string> Parts { get; } = new List<string>();
    /// <summary>An optional role beneath each fact.</summary>
    public ThemeToken? Role { get; set; }
    /// <summary>Optional literal separator; the default is the tier's middle dot.</summary>
    public string? Separator { get; set; }
    internal List<StyledLine> Lines(int width, RenderContext context) {
        List<StyledLine> lines = new List<StyledLine>();
        string current = string.Empty;
        string separator = Separator ?? " " + context.Glyphs.Get(Glyph.Dot) + " ";
        foreach (string part in Parts) {
            string next = current.Length == 0 ? part : current + "[muted]" + separator + "[/]" + part;
            if (current.Length > 0 && context.Markup(next).Lines[0].Width > width) { Add(current); current = part; }
            else { current = next; }
        }
        if (current.Length > 0) { Add(current); }
        return lines;
        void Add(string markup) {
            StyledLine line = context.Markup(markup).Lines[0];
            if (Role.HasValue) { line = line.WithBaseStyle(context.Style(Role.Value)); }
            lines.AddRange(TextWrapper.Wrap(line, Math.Max(1, width)));
        }
    }
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) {
        List<StyledLine> lines = Lines(constraints.MaxWidth, context);
        int width = 0;
        foreach (StyledLine line in lines) { width = Math.Max(width, line.Width); }
        return new Size(width, Math.Min(lines.Count, constraints.MaxHeight));
    }
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) {
        List<StyledLine> lines = Lines(region.Width, context);
        for (int y = 0; y < lines.Count && y < region.Height; y++) { region.Write(0, y, lines[y]); }
    }
}
