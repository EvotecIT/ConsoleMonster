using System;
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A duration in the most readable unit: <c>380 ms</c>, <c>4.2 s</c>, <c>1 m 05 s</c>, <c>2 h 14 m</c>, <c>3 d 4 h</c>.
    /// Accepts <see cref="TimeSpan"/> values, or numbers in <see cref="NumberUnit"/> (seconds by default).
    /// </summary>
    public sealed class DurationPresenter : ICellPresenter {
        /// <summary>Creates a duration presenter.</summary>
        /// <param name="numberUnit">The unit of plain numbers (default seconds).</param>
        public DurationPresenter(TimeSpan? numberUnit = null) {
            NumberUnit = numberUnit ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>The unit of plain numbers.</summary>
        public TimeSpan NumberUnit { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            TimeSpan span;
            if (value is TimeSpan direct) {
                span = direct;
            } else if (PresenterValues.TryGetNumber(value, out double number)) {
                span = TimeSpan.FromTicks((long)(number * NumberUnit.Ticks));
            } else {
                return new TextPresenter().Present(value, context);
            }
            string text = Format(span, context.Culture);
            return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text)), HorizontalAlignment.Right) { Number = span.TotalSeconds };
        }

        /// <summary>Formats a duration with the culture's decimal separator.</summary>
        /// <param name="span">The duration.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The text.</returns>
        public static string Format(TimeSpan span, CultureInfo culture) {
            string sign = span < TimeSpan.Zero ? "-" : string.Empty;
            TimeSpan d = span.Duration();
            if (d.TotalSeconds < 1) {
                return sign + Math.Round(d.TotalMilliseconds).ToString("0", culture) + " ms";
            }
            if (d.TotalSeconds < 60) {
                return sign + d.TotalSeconds.ToString(d.TotalSeconds < 10 ? "0.0" : "0", culture) + " s";
            }
            if (d.TotalHours < 1) {
                return sign + d.Minutes.ToString(culture) + " m " + d.Seconds.ToString("00", culture) + " s";
            }
            if (d.TotalDays < 1) {
                return sign + d.Hours.ToString(culture) + " h " + d.Minutes.ToString("00", culture) + " m";
            }
            return sign + ((int)d.TotalDays).ToString(culture) + " d " + d.Hours.ToString(culture) + " h";
        }
    }
}
