using ConsoleMonster.Layout;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A number with a data bar in the cell, scaled to <see cref="Maximum"/> or the column maximum:
    /// <c>▆▆▆▆▆▆▆     412</c>. The number stays visible, so the cell reads the same without colour.
    /// </summary>
    public sealed class DataBarPresenter : ICellPresenter {
        /// <summary>Creates a data bar presenter.</summary>
        /// <param name="maximum">The value of a full bar, or null for the column maximum.</param>
        /// <param name="number">How the number is formatted (default <see cref="NumberPresenter"/>).</param>
        /// <param name="barWidth">Cells the bar would like (default 10).</param>
        public DataBarPresenter(double? maximum = null, ICellPresenter? number = null, int barWidth = 10) {
            Maximum = maximum;
            NumberFormat = number ?? new NumberPresenter();
            BarWidth = barWidth;
        }

        /// <summary>The value of a full bar, or null for the column maximum.</summary>
        public double? Maximum { get; }

        /// <summary>How the number is formatted.</summary>
        public ICellPresenter NumberFormat { get; }

        /// <summary>Cells the bar would like.</summary>
        public int BarWidth { get; }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            PresentedValue text = NumberFormat.Present(value, context);
            if (!text.Number.HasValue) {
                return text;
            }
            double max = Maximum ?? context.ColumnMaximum ?? text.Number.Value;
            return new PresentedValue(text.Line, HorizontalAlignment.Right) {
                Number = text.Number,
                Visual = CellVisualKind.Bar,
                Fraction = max <= 0 ? 0 : text.Number.Value / max,
                VisualWidth = BarWidth,
                Role = text.Role
            };
        }
    }
}
