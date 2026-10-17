using System.Collections.Generic;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Widgets {
    /// <summary>A named series of values in a <see cref="LineChart"/>; values are plotted at evenly spaced x positions.</summary>
    public sealed class ChartSeries {
        private readonly List<double> _values = new List<double>();

        /// <summary>Creates a series.</summary>
        /// <param name="name">The name (legend and live key).</param>
        /// <param name="token">The colour token.</param>
        public ChartSeries(string name, ThemeToken token) {
            Name = name;
            Token = token;
        }

        /// <summary>The name.</summary>
        public string Name { get; }

        /// <summary>A multicolour legend name shown instead of <see cref="Name"/> (which stays the key), or null.</summary>
        public Text.RichText? NameText { get; set; }

        /// <summary>The colour token.</summary>
        public ThemeToken Token { get; set; }

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
