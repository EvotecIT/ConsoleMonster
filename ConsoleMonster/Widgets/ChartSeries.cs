using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A named series of values in a <see cref="LineChart"/>; values are plotted at evenly spaced x positions.</summary>
    public sealed class ChartSeries {
        private readonly List<double> _values = new List<double>();

        /// <summary>Creates a series.</summary>
        /// <param name="key">The name (legend and live key).</param>
        /// <param name="role">The colour role, or null to let the parent chart choose it.</param>
        public ChartSeries(string key, ThemeToken? role = null) {
            Key = key;
            Label = key;
            _role = role ?? ThemeToken.Series1;
            AutomaticRole = !role.HasValue;
        }

        /// <summary>The name.</summary>
        public string Key { get; }

        /// <summary>Visible legend text, independently of the series key.</summary>
        public string Label { get; set; }

        /// <summary>A multicolour legend name shown instead of <see cref="Key"/> (which stays the key), or null.</summary>
        public Text.RichText? LabelText { get; set; }

        /// <summary>The colour role.</summary>
        public ThemeToken Role { get => _role; set { _role = value; AutomaticRole = false; } }
        private ThemeToken _role;
        internal bool AutomaticRole { get; private set; }

        /// <summary>The values, oldest first.</summary>
        public IReadOnlyList<double> Values => _values;

        /// <summary>Appends a value, dropping the oldest beyond <paramref name="capacity"/>.</summary>
        /// <param name="value">The value.</param>
        /// <param name="capacity">How many values to keep.</param>
        public void Add(double value, int capacity) {
            if (!NumberFormat.IsFinite(value)) {
                // NaN and infinities (a rate over zero elapsed time) are gaps, not points.
                return;
            }
            _values.Add(value);
            if (capacity > 0 && _values.Count > capacity) {
                _values.RemoveRange(0, _values.Count - capacity);
            }
        }
    }
}
