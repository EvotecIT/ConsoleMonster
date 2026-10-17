using System.Collections;
using System.Collections.Generic;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>A series as a one-line sparkline in the cell, followed by the last value: <c>▂▃▅▇▆▄  42</c>.</summary>
    public sealed class SparklinePresenter : ICellPresenter {
        /// <summary>Creates a sparkline presenter.</summary>
        /// <param name="width">Cells the sparkline would like (default 12).</param>
        public SparklinePresenter(int width = 12) {
            Width = width;
        }

        /// <summary>Cells the sparkline would like.</summary>
        public int Width { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            if (value is string || !(value is IEnumerable items)) {
                return new TextPresenter().Present(value, context);
            }
            List<double> series = new List<double>();
            foreach (object? item in items) {
                if (PresenterValues.TryGetNumber(item, out double number)) {
                    series.Add(number);
                }
            }
            if (series.Count == 0) {
                return new PresentedValue(StyledLine.Empty) { IsEmpty = true };
            }
            double last = series[series.Count - 1];
            StyledLine line = StyledLine.From(last.ToString("#,0.##", context.Culture), context.Render.Style(ThemeToken.Muted));
            return new PresentedValue(line) { Visual = CellVisualKind.Sparkline, Series = series, VisualWidth = Width, Number = last };
        }
    }
}
