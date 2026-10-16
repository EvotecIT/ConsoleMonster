using System;
using System.Globalization;

namespace ConsoleMonster.Widgets {
    /// <summary>Compact, invariant formatting for numbers, percentages and durations used across widgets.</summary>
    public static class NumberFormat {
        /// <summary>Formats a count compactly: 950, 1.2k, 12k, 1.4M.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The text.</returns>
        public static string Compact(double value) {
            double abs = Math.Abs(value);
            if (abs < 1000) {
                return Math.Abs(value - Math.Round(value)) < 0.05 ? Math.Round(value).ToString("0", CultureInfo.InvariantCulture) : value.ToString("0.#", CultureInfo.InvariantCulture);
            }
            if (abs < 10000) {
                return (value / 1000).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            }
            if (abs < 1000000) {
                return (value / 1000).ToString("0", CultureInfo.InvariantCulture) + "k";
            }
            if (abs < 10000000) {
                return (value / 1000000).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            }
            return (value / 1000000).ToString("0", CultureInfo.InvariantCulture) + "M";
        }

        /// <summary>Formats a number with thousands separators: 12,345.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The text.</returns>
        public static string Grouped(double value) => value.ToString("#,0", CultureInfo.InvariantCulture);

        /// <summary>Formats a fraction (0-1) as a whole percentage: 64%.</summary>
        /// <param name="fraction">The fraction.</param>
        /// <returns>The text.</returns>
        public static string Percent(double fraction) {
            double clamped = Math.Max(0, Math.Min(1, fraction));
            return Math.Floor(clamped * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>Formats a duration compactly: 850ms, 12.4s, 1m 12s, 1h 02m.</summary>
        /// <param name="duration">The duration.</param>
        /// <returns>The text.</returns>
        public static string Duration(TimeSpan duration) {
            if (duration < TimeSpan.Zero) {
                duration = TimeSpan.Zero;
            }
            if (duration.TotalSeconds < 1) {
                return Math.Round(duration.TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture) + "ms";
            }
            if (duration.TotalSeconds < 60) {
                return duration.TotalSeconds.ToString(duration.TotalSeconds < 10 ? "0.0" : "0", CultureInfo.InvariantCulture) + "s";
            }
            if (duration.TotalHours < 1) {
                return duration.Minutes.ToString(CultureInfo.InvariantCulture) + "m " + duration.Seconds.ToString("00", CultureInfo.InvariantCulture) + "s";
            }
            return ((int)duration.TotalHours).ToString(CultureInfo.InvariantCulture) + "h " + duration.Minutes.ToString("00", CultureInfo.InvariantCulture) + "m";
        }

        /// <summary>Formats a clock: 00:01:23 (or 01:23 under an hour when <paramref name="shortForm"/> is true).</summary>
        /// <param name="elapsed">The elapsed time.</param>
        /// <param name="shortForm">Whether to drop the hours when zero.</param>
        /// <returns>The text.</returns>
        public static string Clock(TimeSpan elapsed, bool shortForm = false) {
            if (elapsed < TimeSpan.Zero) {
                elapsed = TimeSpan.Zero;
            }
            if (shortForm && elapsed.TotalHours < 1) {
                return elapsed.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + elapsed.Seconds.ToString("00", CultureInfo.InvariantCulture);
            }
            return ((int)elapsed.TotalHours).ToString("00", CultureInfo.InvariantCulture) + ":" + elapsed.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" + elapsed.Seconds.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
