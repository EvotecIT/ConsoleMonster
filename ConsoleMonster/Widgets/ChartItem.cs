using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A labelled value in a <see cref="BarChart"/> or <see cref="StackedBar"/>.</summary>
    public sealed class ChartItem {
        /// <summary>Creates an item.</summary>
        /// <param name="label">The label (plain text).</param>
        /// <param name="value">The value.</param>
        /// <param name="role">The colour role, or <c>null</c> for the chart default.</param>
        /// <param name="key">Stable identity, or null to use the label.</param>
        public ChartItem(string label, double value, ThemeToken? role = null, string? key = null) {
            Label = label;
            Key = key ?? label;
            Value = value;
            Role = role;
        }

        /// <summary>The stable key used by live updates; defaults to the label.</summary>
        public string Key { get; }

        /// <summary>The label.</summary>
        public string Label { get; }

        /// <summary>A multicolour label shown instead of <see cref="Label"/> (which stays the key), or null.</summary>
        public Text.RichText? LabelText { get; set; }

        /// <summary>The value.</summary>
        public double Value { get; set; }

        /// <summary>The colour role.</summary>
        public ThemeToken? Role { get; set; }

        /// <summary>A plain text replacement for a non-finite value in a bar chart; null uses the chart default.</summary>
        public string? MissingText { get; set; }

        /// <summary>An explicit status, shown as a glyph as well as a theme role in bar charts.</summary>
        public StatusKind? Status { get; set; }
    }
}
