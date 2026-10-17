using System;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Text parameters that accept rich text (<c>New-ConsoleText</c>) as well as strings. Rich text keeps its colours;
    /// strings keep the parameter's documented meaning (markup for titles and labels that always took markup, literal for
    /// data such as lane names and log messages); other values are shown literally.
    /// </summary>
    internal static class PSText {
        internal static object? Unwrap(object? value) => value is PSObject wrapped ? wrapped.BaseObject : value;

        /// <summary>For parameters documented as markup: rich text becomes its markup, strings stay as written, other values are escaped.</summary>
        internal static string? Markup(object? value) {
            object? unwrapped = Unwrap(value);
            if (unwrapped == null) {
                return null;
            }
            if (unwrapped is RichText rich) {
                return rich.ToMarkup();
            }
            if (unwrapped is string text) {
                return text;
            }
            return Text.Markup.Escape(Convert.ToString(unwrapped, CultureInfo.InvariantCulture));
        }

        /// <summary>For literal parameters: markup only for rich text (escaped otherwise), so data is never interpreted.</summary>
        internal static string? LiteralMarkup(object? value) {
            object? unwrapped = Unwrap(value);
            if (unwrapped == null) {
                return null;
            }
            return unwrapped is RichText rich ? rich.ToMarkup() : Text.Markup.Escape(Convert.ToString(unwrapped, CultureInfo.InvariantCulture));
        }

        /// <summary>Text for a live event to a literal target: rich text travels as markup with the markup flag set.</summary>
        internal static string? EventText(object? value, out bool isMarkup) {
            object? unwrapped = Unwrap(value);
            isMarkup = unwrapped is RichText;
            if (unwrapped is RichText rich) {
                return rich.ToMarkup();
            }
            return unwrapped == null ? null : Convert.ToString(unwrapped, CultureInfo.InvariantCulture);
        }

        /// <summary>The value as rich text when it is rich text, otherwise null.</summary>
        internal static RichText? Rich(object? value) => Unwrap(value) as RichText;

        /// <summary>The plain text of a value (rich text without its styles).</summary>
        internal static string PlainText(object? value) {
            object? unwrapped = Unwrap(value);
            return unwrapped is RichText rich ? rich.PlainText : Convert.ToString(unwrapped, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
