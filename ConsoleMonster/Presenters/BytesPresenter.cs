using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>A byte count in binary units with the culture's decimal separator: <c>512 B</c>, <c>1.4 MB</c>, <c>2.07 GB</c>.</summary>
    public sealed class BytesPresenter : ICellPresenter {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

        /// <summary>Fixed decimal places for scaled values, or null for the ordinary compact formatting.</summary>
        public int? DecimalPlaces { get; set; }

        /// <summary>Optional fixed precision by binary unit name, such as TB. Other units retain <see cref="DecimalPlaces"/>.</summary>
        public IDictionary<string, int> UnitDecimalPlaces { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

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
            int? decimals = UnitDecimalPlaces.TryGetValue(Units[unit], out int unitDecimals) ? unitDecimals : DecimalPlaces;
            if (unit > 0 && decimals.HasValue) { number = scaled.ToString("N" + Math.Max(0, Math.Min(8, decimals.Value)), context.Culture); }
            StyledLine line = new StyledLine(new[] {
                new TextSpan(number, context.Render.Style(ThemeToken.Text)),
                new TextSpan(" " + Units[unit], context.Render.Style(ThemeToken.Muted))
            });
            return new PresentedValue(line, HorizontalAlignment.Right) { Number = bytes };
        }
    }
}
