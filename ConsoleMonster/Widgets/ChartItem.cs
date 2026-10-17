using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A labelled value in a <see cref="BarChart"/> or <see cref="StackedBar"/>.</summary>
    public sealed class ChartItem {
        /// <summary>Creates an item.</summary>
        /// <param name="label">The label (plain text).</param>
        /// <param name="value">The value.</param>
        /// <param name="token">The colour token, or <c>null</c> for the chart default.</param>
        public ChartItem(string label, double value, ThemeToken? token = null) {
            Label = label;
            Value = value;
            Token = token;
        }

        /// <summary>The label.</summary>
        public string Label { get; }

        /// <summary>A multicolour label shown instead of <see cref="Label"/> (which stays the key), or null.</summary>
        public Text.RichText? LabelText { get; set; }

        /// <summary>The value.</summary>
        public double Value { get; set; }

        /// <summary>The colour token.</summary>
        public ThemeToken? Token { get; set; }
    }
}
