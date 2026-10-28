using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets;

/// <summary>One consistent footer for omitted rows and hidden columns.</summary>
public sealed class ViewHint : IWidget {
    /// <summary>The number of additional rows.</summary>
    public long MoreRows { get; set; }
    /// <summary>Literal names of hidden columns.</summary>
    public IList<string> Columns { get; } = new List<string>();
    private FactFlow Build() {
        List<string> parts = new List<string>();
        if (MoreRows > 0) parts.Add("+" + MoreRows + " more");
        List<string> names = new List<string>();
        foreach (string column in Columns) if (!string.IsNullOrWhiteSpace(column)) names.Add(column);
        if (names.Count > 0) parts.Add("+" + names.Count + (names.Count == 1 ? " column (" : " columns (") + Text.Markup.Escape(string.Join(", ", names)) + ")");
        if (parts.Count > 0) parts.Add("pipe to Format-Table for " + (MoreRows > 0 ? names.Count > 0 ? "every row and column" : "every row" : "all columns"));
        return new FactFlow(parts) { Role = ThemeToken.Muted };
    }
    internal List<Text.StyledLine> Lines(int width, RenderContext context) => Build().Lines(width, context);
    /// <inheritdoc />
    public Size Measure(Constraints constraints, RenderContext context) => Build().Measure(constraints, context);
    /// <inheritdoc />
    public void Render(Region region, RenderContext context) => Build().Render(region, context);
}
