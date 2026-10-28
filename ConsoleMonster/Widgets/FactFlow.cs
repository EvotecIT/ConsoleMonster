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
