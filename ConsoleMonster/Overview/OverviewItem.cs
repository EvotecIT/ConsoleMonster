using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    /// <summary>A plain-text observation, optionally drawn as a percentage bar.</summary>
    public sealed class OverviewItem {
        /// <summary>Creates an observation without interpreting its text as markup.</summary>
        /// <param name="label">The metric, volume, interface or event label.</param>
        /// <param name="value">A readable value or description.</param>
        /// <param name="percentage">An optional used percentage, from zero to 100.</param>
        /// <param name="tone">The semantic tone for a non-bar observation.</param>
        public OverviewItem(string label, string value, double? percentage = null, ThemeToken tone = ThemeToken.Text) {
            if (percentage.HasValue && (double.IsNaN(percentage.Value) || double.IsInfinity(percentage.Value) || percentage < 0 || percentage > 100)) {
                throw new ArgumentOutOfRangeException(nameof(percentage));
            }
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Percentage = percentage;
            Tone = tone;
        }

        /// <summary>The plain-text metric or observation label.</summary>
        public string Label { get; }
        /// <summary>The plain-text formatted value.</summary>
        public string Value { get; }
        /// <summary>The used percentage, or null for text observations.</summary>
        public double? Percentage { get; }
        /// <summary>The semantic text tone.</summary>
        public ThemeToken Tone { get; }
    }
}
