using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A conditional formatting rule: when a value matches, the cell takes a tone (the first matching rule wins). Rules
    /// compare the presented number when there is one (numbers, bytes, durations in seconds, dates in days from now), so
    /// <c>ToneRule.Below(30, ThemeToken.Warn)</c> on a date column flags certificates expiring within 30 days. Without
    /// colour, a status tone (pass, warn, fail) adds its symbol in front of the value, so the rule stays visible.
    /// </summary>
    public sealed class ToneRule {
        private readonly Func<object?, double?, bool> _matches;

        /// <summary>Creates a rule from a predicate over the raw value and the presented number (when there is one).</summary>
        /// <param name="matches">The predicate.</param>
        /// <param name="tone">The tone to apply.</param>
        public ToneRule(Func<object?, double?, bool> matches, ThemeToken tone) {
            _matches = matches ?? throw new ArgumentNullException(nameof(matches));
            Tone = tone;
        }

        /// <summary>The tone applied on a match.</summary>
        public ThemeToken Tone { get; }

        /// <summary>Whether the rule matches.</summary>
        /// <param name="value">The raw value.</param>
        /// <param name="number">The presented number, or null.</param>
        /// <returns>True on a match.</returns>
        public bool Matches(object? value, double? number) => _matches(value, number);

        /// <summary>Matches numbers greater than a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule Above(double threshold, ThemeToken tone) => new ToneRule((v, n) => Number(v, n) > threshold, tone);

        /// <summary>Matches numbers greater than or equal to a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule AtLeast(double threshold, ThemeToken tone) => new ToneRule((v, n) => Number(v, n) >= threshold, tone);

        /// <summary>Matches numbers less than a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule Below(double threshold, ThemeToken tone) => new ToneRule((v, n) => Number(v, n) < threshold, tone);

        /// <summary>Matches numbers less than or equal to a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule AtMost(double threshold, ThemeToken tone) => new ToneRule((v, n) => Number(v, n) <= threshold, tone);

        /// <summary>Matches values equal to <paramref name="expected"/> (text comparison, case-insensitive).</summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule Equal(object? expected, ThemeToken tone) {
            string text = Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return new ToneRule((v, n) => string.Equals(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, text, StringComparison.OrdinalIgnoreCase), tone);
        }

        /// <summary>Matches values for which <paramref name="predicate"/> returns true.</summary>
        /// <param name="predicate">The predicate over the raw value.</param>
        /// <param name="tone">The tone.</param>
        /// <returns>The rule.</returns>
        public static ToneRule When(Func<object?, bool> predicate, ThemeToken tone) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new ToneRule((v, n) => predicate(v), tone);
        }

        private static double Number(object? value, double? number) {
            if (number.HasValue) {
                return number.Value;
            }
            return PresenterValues.TryGetNumber(value, out double parsed) ? parsed : double.NaN;
        }
    }
}
