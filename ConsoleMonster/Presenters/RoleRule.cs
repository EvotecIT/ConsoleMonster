using System;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// A conditional formatting rule: when a value matches, the cell takes a role (the first matching rule wins). Rules
    /// compare the presented number when there is one (numbers, bytes, durations in seconds, dates in days from now), so
    /// <c>RoleRule.Below(30, ThemeToken.Warn)</c> on a date column flags certificates expiring within 30 days. Without
    /// colour, a status role (pass, warn, fail) adds its symbol in front of the value, so the rule stays visible.
    /// </summary>
    public sealed class RoleRule {
        private readonly Func<object?, double?, bool> _matches;

        /// <summary>Creates a rule from a predicate over the raw value and the presented number (when there is one).</summary>
        /// <param name="matches">The predicate.</param>
        /// <param name="role">The role to apply.</param>
        public RoleRule(Func<object?, double?, bool> matches, ThemeToken role) {
            _matches = matches ?? throw new ArgumentNullException(nameof(matches));
            Role = role;
        }

        /// <summary>The role applied on a match.</summary>
        public ThemeToken Role { get; }

        /// <summary>Whether the rule matches.</summary>
        /// <param name="value">The raw value.</param>
        /// <param name="number">The presented number, or null.</param>
        /// <returns>True on a match.</returns>
        public bool Matches(object? value, double? number) => _matches(value, number);

        /// <summary>Matches numbers greater than a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule Above(double threshold, ThemeToken role) => new RoleRule((v, n) => Number(v, n) > threshold, role);

        /// <summary>Matches numbers greater than or equal to a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule AtLeast(double threshold, ThemeToken role) => new RoleRule((v, n) => Number(v, n) >= threshold, role);

        /// <summary>Matches numbers less than a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule Below(double threshold, ThemeToken role) => new RoleRule((v, n) => Number(v, n) < threshold, role);

        /// <summary>Matches numbers less than or equal to a threshold.</summary>
        /// <param name="threshold">The threshold.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule AtMost(double threshold, ThemeToken role) => new RoleRule((v, n) => Number(v, n) <= threshold, role);

        /// <summary>Matches values equal to <paramref name="expected"/> (text comparison, case-insensitive).</summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule Equal(object? expected, ThemeToken role) {
            string text = Convert.ToString(expected, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return new RoleRule((v, n) => string.Equals(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, text, StringComparison.OrdinalIgnoreCase), role);
        }

        /// <summary>Matches values for which <paramref name="predicate"/> returns true.</summary>
        /// <param name="predicate">The predicate over the raw value.</param>
        /// <param name="role">The role.</param>
        /// <returns>The rule.</returns>
        public static RoleRule When(Func<object?, bool> predicate, ThemeToken role) {
            if (predicate == null) {
                throw new ArgumentNullException(nameof(predicate));
            }
            return new RoleRule((v, n) => predicate(v), role);
        }

        private static double Number(object? value, double? number) {
            if (number.HasValue) {
                return number.Value;
            }
            return PresenterValues.TryGetNumber(value, out double parsed) ? parsed : double.NaN;
        }
    }
}
