using System;
using System.Globalization;

namespace ConsoleMonster.Presenters {
    /// <summary>Value conversions shared by presenters, tone rules and aggregates.</summary>
    public static class PresenterValues {
        /// <summary>
        /// Converts a value to a double when it is a finite number (numbers, numeric strings in invariant format, TimeSpan
        /// seconds). NaN and infinities are not numbers here, so totals, tone rules and scales skip them.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="number">The number.</param>
        /// <returns>True when the value is numeric.</returns>
        public static bool TryGetNumber(object? value, out double number) {
            number = 0;
            switch (value) {
                case null:
                    return false;
                case double d:
                    number = d;
                    return !double.IsNaN(d) && !double.IsInfinity(d);
                case float f:
                    number = f;
                    return !float.IsNaN(f) && !float.IsInfinity(f);
                case decimal m:
                    number = (double)m;
                    return true;
                case int i:
                    number = i;
                    return true;
                case long l:
                    number = l;
                    return true;
                case short s:
                    number = s;
                    return true;
                case byte b:
                    number = b;
                    return true;
                case uint ui:
                    number = ui;
                    return true;
                case ulong ul:
                    number = ul;
                    return true;
                case ushort us:
                    number = us;
                    return true;
                case sbyte sb:
                    number = sb;
                    return true;
                case TimeSpan span:
                    number = span.TotalSeconds;
                    return true;
                case bool _:
                    return false;
                case string text:
                    return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out number) && !double.IsNaN(number) && !double.IsInfinity(number);
                default:
                    return false;
            }
        }

        /// <summary>The placeholder for a value that has no meaningful display (an unbounded duration, a non-finite number): an em dash, or "-" in the ASCII tier.</summary>
        /// <param name="context">The presenter context.</param>
        /// <returns>The dash.</returns>
        public static string Dash(PresenterContext context) {
            return context.Render.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? "-" : "—";
        }

        /// <summary>Whether a value is a NaN or infinite <see cref="double"/> or <see cref="float"/> (shown as a dash).</summary>
        /// <param name="value">The value.</param>
        /// <returns>True for NaN and infinities.</returns>
        public static bool IsNonFinite(object? value) {
            switch (value) {
                case double d:
                    return double.IsNaN(d) || double.IsInfinity(d);
                case float f:
                    return float.IsNaN(f) || float.IsInfinity(f);
                default:
                    return false;
            }
        }

        /// <summary>Whether a value counts as empty (null, DBNull or an empty string).</summary>
        /// <param name="value">The value.</param>
        /// <returns>True when empty.</returns>
        public static bool IsEmpty(object? value) {
            return value == null || value is DBNull || (value is string text && text.Length == 0);
        }

        /// <summary>Formats any value as plain text with the culture (dates and numbers included).</summary>
        /// <param name="value">The value.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>The text.</returns>
        public static string ToText(object? value, CultureInfo culture) {
            if (value == null || value is DBNull) {
                return string.Empty;
            }
            if (value is IFormattable formattable) {
                return formattable.ToString(null, culture);
            }
            return value.ToString() ?? string.Empty;
        }
    }
}
