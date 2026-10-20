using System;
using System.Globalization;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// Accepts a time as a <see cref="TimeSpan"/> (New-TimeSpan, [timespan]), a number of seconds (2, 1.5), or a number with a
    /// unit: 500ms, 2s, 1.5m, 1h, 1d. Strings PowerShell already converts ("00:01:30") work too.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class TimeSpanTransformationAttribute : ArgumentTransformationAttribute {
        /// <inheritdoc />
        public override object? Transform(EngineIntrinsics engineIntrinsics, object? inputData) {
            object? value = inputData is PSObject wrapped ? wrapped.BaseObject : inputData;
            switch (value) {
                case null:
                    return null;
                case TimeSpan _:
                    return value;
                case string text:
                    if (TryParse(text, out TimeSpan parsed)) {
                        return parsed;
                    }
                    throw new ArgumentTransformationMetadataException("'" + text + "' is not a time. Use a TimeSpan, seconds (2 or 1.5) or a number with a unit: 500ms, 2s, 1.5m, 1h.");
                case IConvertible _:
                    return TimeSpan.FromSeconds(LanguagePrimitives.ConvertTo<double>(value));
                default:
                    return value;
            }
        }

        /// <summary>Parses seconds, a number with a unit (ms, s, m, h, d) or a TimeSpan string.</summary>
        internal static bool TryParse(string text, out TimeSpan value) {
            value = TimeSpan.Zero;
            string trimmed = (text ?? string.Empty).Trim();
            if (trimmed.Length == 0) {
                return false;
            }
            string[] units = { "ms", "s", "m", "h", "d" };
            foreach (string unit in units) {
                if (trimmed.EndsWith(unit, StringComparison.OrdinalIgnoreCase)
                    && double.TryParse(trimmed.Substring(0, trimmed.Length - unit.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)) {
                    switch (unit) {
                        case "ms":
                            value = TimeSpan.FromMilliseconds(amount);
                            break;
                        case "s":
                            value = TimeSpan.FromSeconds(amount);
                            break;
                        case "m":
                            value = TimeSpan.FromMinutes(amount);
                            break;
                        case "h":
                            value = TimeSpan.FromHours(amount);
                            break;
                        default:
                            value = TimeSpan.FromDays(amount);
                            break;
                    }
                    return true;
                }
            }
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)) {
                value = TimeSpan.FromSeconds(seconds);
                return true;
            }
            return TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out value);
        }
    }
}
