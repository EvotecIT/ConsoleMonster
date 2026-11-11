using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets;

/// <summary>A supplied score with a gauge and labelled components. It never computes a score or severity band.</summary>
public sealed class ScoreGauge : IWidget {
    /// <summary>Creates a gauge using the caller's score and finding role.</summary>
    public ScoreGauge(double value, double maximum = 100, ThemeToken role = ThemeToken.Pass) {
        Value = value; Maximum = maximum; Role = role;
    }
    /// <summary>The engine's score.</summary>
    public double Value { get; set; }
    /// <summary>The maximum score.</summary>
    public double Maximum { get; set; }
    /// <summary>The worst finding's role, supplied by the caller.</summary>
    public ThemeToken Role { get; set; }
    /// <summary>A fixed gauge width, or zero for 24 cells at wide widths and 20 at narrow widths.</summary>
    public int Width { get; set; }
    /// <summary>Literal label/value components, in their supplied order.</summary>
    public IList<KeyValuePair<string, string>> Parts { get; } = new List<KeyValuePair<string, string>>();
    private FactFlow Build(int width, RenderContext context) {
        int cells = Math.Max(1, Width > 0 ? Width : width >= 78 ? 24 : 20);
        double ratio = Maximum > 0 && !double.IsNaN(Value) ? Math.Max(0, Math.Min(1, Value / Maximum)) : 0;
        string marker = StatusKinds.TryFromToken(Role, out StatusKind status) ? Markup.Escape(context.Glyphs.StatusSymbol(status)) + " " : string.Empty;
        string value = Value.ToString("0.##", context.Culture) + (Maximum == 100 ? "%" : "/" + Maximum.ToString("0.##", context.Culture));
        string bar = BarDrawer.Markup(cells, ratio, (Role == ThemeToken.Pass ? ThemeToken.Muted : Role).ToString().ToLowerInvariant(), context, true, BarShape.Line);
        string markup = bar + "  " + marker + "[strong]" + value + "[/]";
        List<string> details = new List<string>();
        foreach (KeyValuePair<string, string> part in Parts) {
            if (width < 78 && string.Equals(part.Key, "coverage", StringComparison.OrdinalIgnoreCase)) { continue; }
            details.Add("[muted]" + Markup.Escape(part.Key) + "[/] " + Markup.Escape(part.Value));
        }
        if (details.Count > 0) { markup += "  " + string.Join(" [muted]" + context.Glyphs.Get(Glyph.Dot) + "[/] ", details); }
        return new FactFlow(markup);
    }
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) => Build(constraints.MaxWidth, context).Measure(constraints, context);
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) => Build(region.Width, context).Render(region, context);
}
