using System;
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A number, right-aligned, with the culture's thousand and decimal separators and an optional unit:
    /// <c>12,480</c>, <c>8.4 rules/s</c>, <c>97.5 %</c>. Non-numeric values are shown as text.
    /// </summary>
    public sealed class NumberPresenter : ICellPresenter {
        /// <summary>Creates a number presenter.</summary>
        /// <param name="unit">A unit appended after a space (muted), or null.</param>
        /// <param name="decimals">Fixed decimals, or null for automatic: in a table column the most decimals any of its numbers needs (at most two), elsewhere up to two (none for whole numbers).</param>
        public NumberPresenter(string? unit = null, int? decimals = null) {
            Unit = unit;
            Decimals = decimals;
        }

        /// <summary>The unit, or null.</summary>
        public string? Unit { get; }

        /// <summary>Fixed decimals, or null for automatic.</summary>
        public int? Decimals { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (!PresenterValues.TryGetNumber(value, out double number) || value is TimeSpan) {
                return new TextPresenter().Present(value, context);
            }
            // In a column every number shares the decimals the column needs, so the separators line up: 7,0 / 11,7 / 344,4.
            string format = Decimals.HasValue
                ? "N" + Math.Max(0, Decimals.Value).ToString(CultureInfo.InvariantCulture)
                : context.ColumnDecimals.HasValue
                    ? ColumnFormat(Math.Max(context.ColumnDecimals.Value, PresenterContext.DecimalsNeeded(value)))
                    : "#,0.##";
            string text = number.ToString(format, context.Culture);
            StyledLine line = Unit == null
                ? StyledLine.From(text, context.Render.Style(ThemeToken.Text))
                : new StyledLine(new[] { new TextSpan(text, context.Render.Style(ThemeToken.Text)), new TextSpan(" " + Unit, context.Render.Style(ThemeToken.Muted)) });
            return new PresentedValue(line, HorizontalAlignment.Right) { Number = number };
        }

        // Fixed decimals for a column (never fewer than the value itself needs, so a value is never rounded away).
        private static string ColumnFormat(int decimals) => decimals > 0 ? "#,0." + new string('0', Math.Min(2, decimals)) : "#,0";
    }
}
