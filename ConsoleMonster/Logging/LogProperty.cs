using System;
using System.Collections;
using System.Text;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Logging {
    internal sealed class LogProperty {
        internal LogProperty(string name, object? value, Func<string?, string>? render = null) {
            Name = name;
            Value = value;
            Render = render;
        }

        internal string Name { get; }
        internal object? Value { get; }
        internal Func<string?, string>? Render { get; }
        internal ThemeToken Role => Value is string || Value is char ? ThemeToken.Pass
            : Value is DateTime || Value is DateTimeOffset || Value is TimeSpan ? ThemeToken.Accent2
            : Value is byte || Value is sbyte || Value is short || Value is ushort || Value is int || Value is uint
                || Value is long || Value is ulong || Value is float || Value is double || Value is decimal ? ThemeToken.Highlight
            : ThemeToken.Accent;

        internal string Format(string? format, IFormatProvider provider) {
            if (Render != null) { return Render(format); }
            if (Value == null) { return "(null)"; }
            if (Value is string text) { return text; }
            if (Value is IFormattable formattable) {
                try { return formattable.ToString(format, provider) ?? string.Empty; }
                catch (FormatException) { return formattable.ToString(null, provider) ?? string.Empty; }
            }
            // This matches ILogger's enumerable template values without reflecting over arbitrary objects.
            if (Value is IEnumerable sequence) {
                StringBuilder result = new StringBuilder();
                foreach (object? item in sequence) {
                    if (result.Length > 0) { result.Append(", "); }
                    result.Append(Convert.ToString(item, provider) ?? "(null)");
                }
                return result.ToString();
            }
            return Convert.ToString(Value, provider) ?? string.Empty;
        }

        internal string Tail(IFormatProvider provider) => Format(null, provider).Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
