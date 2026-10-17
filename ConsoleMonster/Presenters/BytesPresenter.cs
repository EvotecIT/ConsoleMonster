using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>A byte count in binary units with the culture's decimal separator: <c>512 B</c>, <c>1.4 MB</c>, <c>2.07 GB</c>.</summary>
    public sealed class BytesPresenter : ICellPresenter {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (!PresenterValues.TryGetNumber(value, out double bytes) || value is TimeSpan) {
                return new TextPresenter().Present(value, context);
            }
            double scaled = Math.Abs(bytes);
            int unit = 0;
            while (scaled >= 1024 && unit < Units.Length - 1) {
                scaled /= 1024;
                unit++;
            }
            if (bytes < 0) {
                scaled = -scaled;
            }
            string number = unit == 0 ? scaled.ToString("#,0", context.Culture) : scaled.ToString(Math.Abs(scaled) < 10 ? "0.##" : Math.Abs(scaled) < 100 ? "0.#" : "#,0", context.Culture);
            StyledLine line = new StyledLine(new[] {
                new TextSpan(number, context.Render.Style(ThemeToken.Text)),
                new TextSpan(" " + Units[unit], context.Render.Style(ThemeToken.Muted))
            });
            return new PresentedValue(line, HorizontalAlignment.Right) { Number = bytes };
        }
    }
}
