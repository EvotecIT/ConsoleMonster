using ConsoleMonster.Live;

namespace ConsoleMonster.Widgets {
    /// <summary>Thread-safe live handle for a <see cref="Sparkline"/>, <see cref="LineChart"/> series or <see cref="Histogram"/>.</summary>
    public sealed class SparklineHandle : LiveHandle {
        /// <summary>Creates a handle.</summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="widgetId">The widget name.</param>
        public SparklineHandle(ILiveDispatcher dispatcher, string widgetId) : base(dispatcher, widgetId) {
        }

        /// <summary>Appends a value.</summary>
        /// <param name="value">The value.</param>
        /// <param name="series">The series key for multi-series charts, or <c>null</c>.</param>
        public void Add(double value, string? series = null) => Post("add", series, value: value);
    }
}
