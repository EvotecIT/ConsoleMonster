using System;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Escaping and link checks shared by the HTML, SVG and asciicast writers. Every piece of cell text and every attribute
    /// goes through here, so text from widgets can never become markup, and only plain web and mail links survive.
    /// </summary>
    internal static class ExportText {
        /// <summary>Appends text escaped for HTML/SVG content or a double-quoted attribute.</summary>
        public static void AppendEscaped(StringBuilder sb, string? text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            for (int i = 0; i < text!.Length; i++) {
                char c = text[i];
                switch (c) {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    case '\'':
                        sb.Append("&#39;");
                        break;
                    default:
                        if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) {
                            sb.Append(c).Append(text[i + 1]);
                            i++;
                        } else if (char.IsSurrogate(c) || (c < 0x20 && c != '\t') || c == 0x7F || (c >= 0x80 && c <= 0x9F) || c == '￾' || c == '￿') {
                            sb.Append('�');
                        } else if (c == '\t') {
                            sb.Append(' ');
                        } else {
                            sb.Append(c);
                        }
                        break;
                }
            }
        }

        public static string Escape(string? text) {
            StringBuilder sb = new StringBuilder(text?.Length ?? 0);
            AppendEscaped(sb, text);
            return sb.ToString();
        }

        /// <summary>
        /// Whether an OSC 8 target may become a link: absolute http, https, mailto or ftp only. Anything else (javascript:,
        /// data:, vbscript:, file:, relative paths, or schemes hidden behind control characters) renders as plain text.
        /// </summary>
        public static bool IsSafeLink(string? link) {
            if (string.IsNullOrWhiteSpace(link) || link!.Length > 2048) {
                return false;
            }
            foreach (char c in link) {
                if (c <= 0x20 || c == 0x7F || c == '"' || c == '<' || c == '>' || c == '\\' || c == '`') {
                    return false;
                }
            }
            if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)) {
                return false;
            }
            string scheme = uri.Scheme.ToLowerInvariant();
            return scheme == "http" || scheme == "https" || scheme == "mailto" || scheme == "ftp";
        }

        /// <summary>
        /// Appends an SVG paint (fill or stroke) for a colour. CSS variables are not allowed in presentation attributes, so a
        /// variable colour goes into a style attribute instead.
        /// </summary>
        public static void AppendPaint(StringBuilder sb, string attribute, string color) {
            if (color.StartsWith("var(", StringComparison.Ordinal)) {
                sb.Append(" style=\"").Append(attribute).Append(':').Append(color).Append('"');
            } else {
                sb.Append(' ').Append(attribute).Append("=\"").Append(color).Append('"');
            }
        }

        /// <summary>A number for CSS and SVG attributes (invariant, at most two decimals).</summary>
        public static string Num(double value) {
            return Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>Whether a font stack is safe to place in CSS (names, quotes, commas, spaces, dots and dashes only).</summary>
        public static bool IsSafeFontFamily(string? family) {
            if (string.IsNullOrWhiteSpace(family) || family!.Length > 400) {
                return false;
            }
            foreach (char c in family) {
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == ',' || c == '\'' || c == '"' || c == '-' || c == '_' || c == '.')) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Appends a string as a JSON string literal (asciicast lines). Lone surrogates are escaped.</summary>
        public static void AppendJson(StringBuilder sb, string? value) {
            sb.Append('"');
            if (value != null) {
                for (int i = 0; i < value.Length; i++) {
                    char c = value[i];
                    switch (c) {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        default:
                            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) {
                                sb.Append(c).Append(value[i + 1]);
                                i++;
                            } else if (c < 0x20 || c == 0x7F || char.IsSurrogate(c)) {
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            } else {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }
    }
}
