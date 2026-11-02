using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Serialization {
    /// <summary>
    /// A small JSON reader and writer for presets and remembered choices: objects, arrays, strings, booleans, null and
    /// numbers (kept as strings by default; typed numbers for schema validation). Zero dependencies;
    /// malformed input raises <see cref="FormatException"/>.
    /// </summary>
    internal static class MiniJson {
        public static string Write(object? value) {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        /// <summary>One line without indentation, for line-delimited protocols and event streams. <c>&lt;</c>, <c>&gt;</c>,
        /// <c>&amp;</c> and the Unicode line separators are escaped, so the text is also safe inside an HTML script element.</summary>
        public static string WriteCompact(object? value) {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, -1);
            return sb.ToString();
        }

        public static object? Read(string text, bool preserveNumberTypes = false) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            int index = 0;
            object? value = ReadValue(text, ref index, 0, preserveNumberTypes);
            SkipWhite(text, ref index);
            if (index != text.Length) {
                throw new FormatException("Unexpected text after the JSON value at " + index.ToString(CultureInfo.InvariantCulture) + ".");
            }
            return value;
        }

        private static void WriteValue(StringBuilder sb, object? value, int indent) {
            switch (value) {
                case null:
                    sb.Append("null");
                    return;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    return;
                case string s:
                    WriteString(sb, s, indent < 0);
                    return;
                case IDictionary<string, object?> map:
                    sb.Append('{');
                    bool first = true;
                    foreach (KeyValuePair<string, object?> pair in map) {
                        if (indent < 0) {
                            if (!first) { sb.Append(','); }
                            first = false;
                            WriteString(sb, pair.Key, true);
                            sb.Append(':');
                            WriteValue(sb, pair.Value, -1);
                            continue;
                        }
                        sb.Append(first ? "\n" : ",\n");
                        first = false;
                        sb.Append(' ', (indent + 1) * 2);
                        WriteString(sb, pair.Key);
                        sb.Append(": ");
                        WriteValue(sb, pair.Value, indent + 1);
                    }
                    if (!first && indent >= 0) {
                        sb.Append('\n').Append(' ', indent * 2);
                    }
                    sb.Append('}');
                    return;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool firstItem = true;
                    foreach (object? item in list) {
                        if (!firstItem) {
                            sb.Append(indent < 0 ? "," : ", ");
                        }
                        firstItem = false;
                        WriteValue(sb, item, indent < 0 ? -1 : indent + 1);
                    }
                    sb.Append(']');
                    return;
                default:
                    if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is decimal) {
                        sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    } else {
                        WriteString(sb, value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, indent < 0);
                    }
                    return;
            }
        }

        private static void WriteString(StringBuilder sb, string s, bool htmlSafe = false) {
            sb.Append('"');
            foreach (char c in s) {
                if (htmlSafe && (c == '<' || c == '>' || c == '&' || c == '\u2028' || c == '\u2029')) {
                    sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    continue;
                }
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
                        if (c < ' ') {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static object? ReadValue(string text, ref int i, int depth, bool preserveNumberTypes) {
            if (depth > 64) { throw new FormatException("JSON nesting exceeds 64 levels."); }
            SkipWhite(text, ref i);
            if (i >= text.Length) {
                throw new FormatException("Unexpected end of JSON.");
            }
            char c = text[i];
            if (c == '{') {
                i++;
                Dictionary<string, object?> map = new Dictionary<string, object?>(StringComparer.Ordinal);
                SkipWhite(text, ref i);
                if (i < text.Length && text[i] == '}') {
                    i++;
                    return map;
                }
                while (true) {
                    SkipWhite(text, ref i);
                    string key = ReadString(text, ref i);
                    SkipWhite(text, ref i);
                    Expect(text, ref i, ':');
                    if (map.ContainsKey(key)) { throw new FormatException("Duplicate JSON key '" + key + "'."); }
                    map.Add(key, ReadValue(text, ref i, depth + 1, preserveNumberTypes));
                    SkipWhite(text, ref i);
                    if (i < text.Length && text[i] == ',') {
                        i++;
                        continue;
                    }
                    Expect(text, ref i, '}');
                    return map;
                }
            }
            if (c == '[') {
                i++;
                List<object?> list = new List<object?>();
                SkipWhite(text, ref i);
                if (i < text.Length && text[i] == ']') {
                    i++;
                    return list;
                }
                while (true) {
                    list.Add(ReadValue(text, ref i, depth + 1, preserveNumberTypes));
                    SkipWhite(text, ref i);
                    if (i < text.Length && text[i] == ',') {
                        i++;
                        continue;
                    }
                    Expect(text, ref i, ']');
                    return list;
                }
            }
            if (c == '"') {
                return ReadString(text, ref i);
            }
            if (Starts(text, i, "true")) {
                i += 4;
                return true;
            }
            if (Starts(text, i, "false")) {
                i += 5;
                return false;
            }
            if (Starts(text, i, "null")) {
                i += 4;
                return null;
            }
            return ReadNumber(text, ref i, preserveNumberTypes);
        }

        private static object ReadNumber(string text, ref int i, bool preserveNumberTypes) {
            int start = i;
            if (i < text.Length && text[i] == '-') { i++; }
            if (i < text.Length && text[i] == '0') { i++; }
            else { Digits(text, ref i); }
            if (i < text.Length && text[i] == '.') {
                i++;
                Digits(text, ref i);
            }
            if (i < text.Length && (text[i] == 'e' || text[i] == 'E')) {
                i++;
                if (i < text.Length && (text[i] == '+' || text[i] == '-')) { i++; }
                Digits(text, ref i);
            }
            string literal = text.Substring(start, i - start);
            if (!preserveNumberTypes) { return literal; }
            if (long.TryParse(literal, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer)) { return integer; }
            if (decimal.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)) { return number; }
            throw new FormatException("JSON number is outside the supported numeric range.");
        }

        private static void Digits(string text, ref int i) {
            int start = i;
            while (i < text.Length && text[i] >= '0' && text[i] <= '9') { i++; }
            if (start == i) { throw new FormatException("Expected a JSON digit at " + i.ToString(CultureInfo.InvariantCulture) + "."); }
        }

        private static string ReadString(string text, ref int i) {
            Expect(text, ref i, '"');
            StringBuilder sb = new StringBuilder();
            while (i < text.Length) {
                char c = text[i++];
                if (c == '"') {
                    return sb.ToString();
                }
                if (c != '\\') {
                    if (c < ' ') { throw new FormatException("Unescaped control character in JSON string."); }
                    sb.Append(c);
                    continue;
                }
                if (i >= text.Length) {
                    break;
                }
                char e = text[i++];
                switch (e) {
                    case '"':
                    case '\\':
                    case '/':
                        sb.Append(e);
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'u':
                        if (i + 4 > text.Length) {
                            throw new FormatException("Bad unicode escape.");
                        }
                        if (!ushort.TryParse(text.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code)) {
                            throw new FormatException("Bad unicode escape.");
                        }
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:
                        throw new FormatException("Unknown JSON escape: \\" + e + ".");
                }
            }
            throw new FormatException("Unterminated string.");
        }

        private static bool Starts(string text, int i, string word) => string.CompareOrdinal(text, i, word, 0, word.Length) == 0;

        private static void Expect(string text, ref int i, char c) {
            if (i >= text.Length || text[i] != c) {
                throw new FormatException("Expected '" + c + "' at " + i.ToString(CultureInfo.InvariantCulture) + ".");
            }
            i++;
        }

        private static void SkipWhite(string text, ref int i) {
            while (i < text.Length && (text[i] == ' ' || text[i] == '\t' || text[i] == '\r' || text[i] == '\n')) {
                i++;
            }
        }
    }
}
