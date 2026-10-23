using System;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared lossless text and culture-aware validation for decimal editors.</summary>
    internal static class DecimalInput {
        /// <summary>Formats editable text without rounding; integral whole numbers omit a retained decimal scale.</summary>
        internal static string Format(decimal value, bool wholeNumber, CultureInfo culture) =>
            value.ToString(wholeNumber && decimal.Truncate(value) == value ? "0" : null, culture);

        /// <summary>Parses non-empty text without reinterpreting a rejected local decimal as invariant integer grouping.</summary>
        internal static bool TryParse(string text, bool wholeNumber, CultureInfo culture, out decimal value) {
            NumberStyles styles = wholeNumber ? NumberStyles.Integer | NumberStyles.AllowThousands : NumberStyles.Number;
            if (decimal.TryParse(text, styles, culture, out value)) return true;
            if (wholeNumber && text.IndexOf(culture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal) >= 0) return false;
            return decimal.TryParse(text, styles, CultureInfo.InvariantCulture, out value);
        }
    }
}
