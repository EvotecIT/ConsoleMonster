using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters;

/// <summary>Shows a literal host identity with its DNS suffix muted.</summary>
public sealed class DnsNamePresenter : ICellPresenter {
    /// <summary>Omit the suffix when the view's scope already identifies the domain.</summary>
    public bool StripSuffix { get; set; }
    /// <inheritdoc />
    public PresentedValue Present(object? value, PresenterContext context) {
        string name = PresenterValues.ToText(value, context.Culture);
        int dot = name.IndexOf('.');
        if (dot < 0) return new PresentedValue(StyledLine.From(name, context.Render.Style(ThemeToken.Text)));
        return new PresentedValue(new StyledLine(new[] {
            new TextSpan(name.Substring(0, dot), context.Render.Style(ThemeToken.Text)),
            new TextSpan(StripSuffix ? string.Empty : name.Substring(dot), context.Render.Style(ThemeToken.Muted))
        }));
    }
}
