using System;
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Progress as a small bar and a fraction: <c>━━━━━──── 18/30</c>. Accepts a fraction (0-1), a percentage string
    /// (<c>64%</c>) or a <c>done/total</c> string (<c>18/30</c>).
    /// </summary>
    public sealed class ProgressPresenter : ICellPresenter {
        /// <summary>Creates a progress presenter.</summary>
        /// <param name="barWidth">Cells the bar would like (default 8).</param>
        public ProgressPresenter(int barWidth = 8) {
            BarWidth = barWidth;
        }

        /// <summary>Cells the bar would like.</summary>
        public int BarWidth { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            double fraction;
            string label;
            string text = PresenterValues.ToText(value, CultureInfo.InvariantCulture).Trim();
            int slash = text.IndexOf('/');
            if (slash > 0
                && double.TryParse(text.Substring(0, slash), NumberStyles.Float, CultureInfo.InvariantCulture, out double done)
                && double.TryParse(text.Substring(slash + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double total)) {
                fraction = total <= 0 ? 0 : done / total;
                label = done.ToString("#,0.##", context.Culture) + "/" + total.ToString("#,0.##", context.Culture);
            } else if (text.EndsWith("%", StringComparison.Ordinal)
                && double.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double percent)) {
                fraction = percent / 100;
                label = Widgets.NumberFormat.Percent(fraction);
            } else if (PresenterValues.TryGetNumber(value, out double number) && !(value is TimeSpan)) {
                fraction = number;
                label = Widgets.NumberFormat.Percent(Math.Max(0, Math.Min(1, number)));
            } else {
                return new TextPresenter().Present(value, context);
            }
            fraction = Math.Max(0, Math.Min(1, fraction));
            ThemeToken tone = fraction >= 1 ? ThemeToken.Pass : ThemeToken.Accent;
            return new PresentedValue(StyledLine.From(label, context.Render.Style(ThemeToken.Muted)), HorizontalAlignment.Right) {
                Visual = CellVisualKind.Progress,
                Fraction = fraction,
                VisualWidth = BarWidth,
                Number = fraction,
                Tone = tone
            };
        }
    }
}
