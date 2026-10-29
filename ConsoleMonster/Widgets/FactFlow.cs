using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>Flows facts at separators without breaking a fact into unrelated words.</summary>
public sealed class FactFlow : IWidget {
    // Normalize only semantic fact separators, including those split across markup spans.
    internal static StyledLine TierSeparators(StyledLine line, RenderContext context) {
        string glyph = context.Glyphs.Get(Glyph.Dot);
        if (glyph == "·") return line;
        string plain = line.PlainText;
        HashSet<int> offsets = new HashSet<int>();
        for (int i = 0; i < plain.Length - 2; i++) {
            if (plain[i] == ' ' && plain[i + 1] == '·' && plain[i + 2] == ' ') offsets.Add(i + 1);
        }
        if (offsets.Count == 0) return line;
        List<TextSpan> spans = new List<TextSpan>();
        int position = 0;
        foreach (TextSpan span in line) {
            StringBuilder text = new StringBuilder();
            foreach (char ch in span.Text) { text.Append(offsets.Contains(position++) ? glyph : ch.ToString()); }
            spans.Add(new TextSpan(text.ToString(), span.Style));
        }
        return new StyledLine(spans);
    }
    // Preserve the styles of complete entries while breaking only at their separators.
    internal static List<StyledLine> WrapSeparated(StyledLine line, int width, string separator, bool preserveSeparator = false, Func<StyledLine, StyledLine>? convert = null) {
        List<StyledLine> result = new List<StyledLine>();
        string plain = line.PlainText;
        List<TextSpan> current = new List<TextSpan>();
        int position = 0;
        while (position <= plain.Length) {
            int next = plain.IndexOf(separator, position, StringComparison.Ordinal);
            int end = next < 0 ? plain.Length : next;
            List<TextSpan> token = new List<TextSpan>();
            if (preserveSeparator && position > 0) token.AddRange(Slice(position - separator.Length, position));
            token.AddRange(Slice(position, end));
            List<TextSpan> joined = new List<TextSpan>(current);
            if (!preserveSeparator && current.Count > 0) joined.AddRange(Slice(position - separator.Length, position));
            joined.AddRange(token);
            if (current.Count > 0 && Convert(new StyledLine(joined)).Width > width) {
                result.Add(Convert(new StyledLine(current)));
                current.Clear();
                joined = token;
            }
            StyledLine fitted = Convert(new StyledLine(joined));
            if (fitted.Width > width) { result.AddRange(TextWrapper.Wrap(fitted, Math.Max(1, width))); current.Clear(); }
            else { current = joined; }
            if (next < 0) break;
            position = next + separator.Length;
        }
        if (current.Count > 0) result.Add(Convert(new StyledLine(current)));
        return result;

        StyledLine Convert(StyledLine value) => convert == null ? value : convert(value);

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
