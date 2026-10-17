using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// A small JSON reader and writer for presets and remembered choices: objects, arrays, strings, booleans, null and
    /// numbers (kept as strings). Zero dependencies; malformed input raises <see cref="FormatException"/>.
    /// </summary>
    internal static class MiniJson {
        public static string Write(object? value) {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, 0);
            return sb.ToString();
        }

        public static object? Read(string text) {
            int index = 0;
            object? value = ReadValue(text, ref index);
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
                    WriteString(sb, s);
                    return;
                case IDictionary<string, object?> map:
                    sb.Append('{');
                    bool first = true;
                    foreach (KeyValuePair<string, object?> pair in map) {
                        sb.Append(first ? "\n" : ",\n");
                        first = false;
                        sb.Append(' ', (indent + 1) * 2);
                        WriteString(sb, pair.Key);
                        sb.Append(": ");
                        WriteValue(sb, pair.Value, indent + 1);
                    }
                    if (!first) {
                        sb.Append('\n').Append(' ', indent * 2);
                    }
                    sb.Append('}');
                    return;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool firstItem = true;
                    foreach (object? item in list) {
                        if (!firstItem) {
                            sb.Append(", ");
                        }
                        firstItem = false;
                        WriteValue(sb, item, indent + 1);
                    }
                    sb.Append(']');
                    return;
                default:
                    WriteString(sb, CommandLine.Format(value));
                    return;
            }
        }

        private static void WriteString(StringBuilder sb, string s) {
            sb.Append('"');
            foreach (char c in s) {
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

        private static object? ReadValue(string text, ref int i) {
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
                    map[key] = ReadValue(text, ref i);
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
                    list.Add(ReadValue(text, ref i));
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
            int start = i;
            while (i < text.Length && (char.IsDigit(text[i]) || "+-.eE".IndexOf(text[i]) >= 0)) {
                i++;
            }
            if (i == start) {
                throw new FormatException("Unexpected character '" + c + "' at " + i.ToString(CultureInfo.InvariantCulture) + ".");
            }
            return text.Substring(start, i - start);
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
                    sb.Append(c);
                    continue;
                }
                if (i >= text.Length) {
                    break;
                }
                char e = text[i++];
                switch (e) {
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
                        sb.Append((char)int.Parse(text.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default:
                        sb.Append(e);
                        break;
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
            while (i < text.Length && char.IsWhiteSpace(text[i])) {
                i++;
            }
        }
    }
}
