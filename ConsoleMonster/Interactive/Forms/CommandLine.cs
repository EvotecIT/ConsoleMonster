using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Builds the command line equivalent to a set of answers, from each field's parameter name, so every interactive run
    /// teaches the non-interactive form. Values are quoted for the chosen style; switches appear only when on.
    /// </summary>
    public static class CommandLine {
        /// <summary>Formats a parameter name for a style (<c>Domain</c> → <c>-Domain</c>, <c>--domain</c> or <c>/domain</c>).</summary>
        /// <param name="name">The name, with or without its prefix.</param>
        /// <param name="style">The style.</param>
        /// <returns>The parameter as written on the command line.</returns>
        public static string Parameter(string name, CommandStyle style) {
            if (string.IsNullOrEmpty(name)) {
                return string.Empty;
            }
            string bare = name.TrimStart('-', '/');
            switch (style) {
                case CommandStyle.Posix:
                    return name.StartsWith("-", StringComparison.Ordinal) ? name : (bare.Length == 1 ? "-" : "--") + bare;
                case CommandStyle.Windows:
                    return "/" + bare;
                default:
                    return "-" + bare;
            }
        }

        /// <summary>Quotes a value when the style needs it. Windows uses CommandLineToArgvW-compatible argument encoding.</summary>
        /// <param name="value">The value.</param>
        /// <param name="style">The style.</param>
        /// <returns>The value as written on the command line.</returns>
        public static string Quote(string value, CommandStyle style) {
            value ??= string.Empty;
            switch (style) {
                case CommandStyle.Posix:
                    if (value.Length > 0 && IsPlain(value, "-_./:=@%+,")) {
                        return value;
                    }
                    return "'" + value.Replace("'", "'\\''") + "'";
                case CommandStyle.Windows:
                    if (value.Length > 0 && IsPlain(value, "-_./:\\=@%+,")) {
                        return value;
                    }
                    return QuoteWindows(value);
                default:
                    if (value.Length > 0 && IsPlain(value, "-_./:\\=+%")) {
                        return value;
                    }
                    return "'" + value.Replace("'", "''") + "'";
            }
        }

        private static string QuoteWindows(string value) {
            StringBuilder quoted = new StringBuilder();
            quoted.Append('"');
            int backslashes = 0;
            foreach (char c in value) {
                if (c == '\\') {
                    backslashes++;
                    continue;
                }
                // A literal quote needs 2n+1 backslashes; other characters leave the run unchanged.
                quoted.Append('\\', c == '"' ? backslashes * 2 + 1 : backslashes);
                quoted.Append(c);
                backslashes = 0;
            }
            // Double trailing backslashes so the closing quote remains a delimiter.
            quoted.Append('\\', backslashes * 2);
            quoted.Append('"');
            return quoted.ToString();
        }

        /// <summary>Formats one argument: <c>-Name value</c>, a list, or a switch.</summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">A string, number, bool (switch) or list.</param>
        /// <param name="style">The style.</param>
        /// <returns>The argument, or an empty string for an off switch or an empty value.</returns>
        public static string Argument(string name, object? value, CommandStyle style) {
            string parameter = Parameter(name, style);
            if (value == null) {
                return string.Empty;
            }
            if (value is bool flag) {
                return flag ? parameter : string.Empty;
            }
            string text;
            if (value is string s) {
                if (s.Length == 0) {
                    return string.Empty;
                }
                text = Quote(s, style);
            } else if (value is System.Collections.IEnumerable list) {
                List<string> parts = new List<string>();
                foreach (object? item in list) {
                    if (item != null) {
                        parts.Add(Quote(Format(item), style));
                    }
                }
                if (parts.Count == 0) {
                    return string.Empty;
                }
                text = string.Join(",", parts.ToArray());
            } else {
                text = Quote(Format(value), style);
            }
            if (style == CommandStyle.Windows) {
                return parameter + ":" + text;
            }
            return parameter + " " + text;
        }

        /// <summary>Joins a command and its arguments, skipping empty ones.</summary>
        /// <param name="command">The command.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The command line.</returns>
        public static string Join(string command, IEnumerable<string> arguments) {
            StringBuilder sb = new StringBuilder(command ?? string.Empty);
            foreach (string argument in arguments) {
                if (string.IsNullOrEmpty(argument)) {
                    continue;
                }
                if (sb.Length > 0) {
                    sb.Append(' ');
                }
                sb.Append(argument);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Wraps a command line for display, breaking between arguments (a parameter stays with its value), then between
        /// the items of a list value, and only inside a token when it alone is wider than the line.
        /// </summary>
        /// <param name="command">The command line.</param>
        /// <param name="width">The line width.</param>
        /// <returns>The lines.</returns>
        public static IReadOnlyList<string> Wrap(string command, int width) => Wrap(command, width, 0);

        /// <summary>
        /// Wraps a command line for display with continuation lines indented. Breaks fall between arguments first (a
        /// parameter stays with its value: <c>-Name value</c>); an argument too wide for a line of its own breaks between
        /// the items of its list value (after each comma, never inside quotes), with those lines indented twice as far; a
        /// parameter and its value part only when they cannot share a line; and a token is cut only when it alone is wider
        /// than a line. Widths are terminal cells.
        /// </summary>
        /// <param name="command">The command line.</param>
        /// <param name="width">The line width (at least 8).</param>
        /// <param name="indent">The cells continuation lines start with (at most half the width).</param>
        /// <returns>The lines, continuation lines starting with the indent.</returns>
        public static IReadOnlyList<string> Wrap(string command, int width, int indent) {
            if (string.IsNullOrEmpty(command)) {
                return new List<string>();
            }
            width = Math.Max(8, width);
            indent = Math.Max(0, Math.Min(width / 2, indent));
            int listIndent = Math.Min(width / 2, indent * 2);
            WrapWriter writer = new WrapWriter(width, indent);
            List<string> tokens = Tokens(command);
            for (int i = 0; i < tokens.Count; i++) {
                string token = tokens[i];
                string? value = IsParameter(token) && i + 1 < tokens.Count && !IsParameter(tokens[i + 1]) ? tokens[++i] : null;
                string whole = value == null ? token : token + " " + value;
                if (writer.Fits(whole) || writer.FitsAlone(whole)) {
                    // The argument fits here or on a line of its own: never split it.
                    writer.Place(whole, indent);
                    continue;
                }
                // Too wide for any line: it starts a line of its own, the parameter first, then its value item by item; the
                // next argument starts a new line too, so it never reads as one more item of the list.
                List<string> items = ListItems(value ?? token);
                writer.StartLine(indent);
                if (value == null) {
                    writer.PlaceItems(items, true, listIndent);
                } else {
                    writer.Place(token, indent);
                    writer.PlaceItems(items, false, listIndent);
                }
                writer.BreakBeforeNext();
            }
            return writer.Finish();
        }

        // A parameter name: -Name, --name or /name (not a negative number).
        private static bool IsParameter(string token) => token.Length > 1 && (token[0] == '-' || token[0] == '/') && !char.IsDigit(token[1]);

        // The items of a list value, each but the last keeping its comma; commas inside quotes stay inside their item.
        private static List<string> ListItems(string value) {
            List<string> items = new List<string>();
            StringBuilder current = new StringBuilder();
            char quote = '\0';
            foreach (char c in value) {
                current.Append(c);
                if (quote != '\0') {
                    if (c == quote) {
                        quote = '\0';
                    }
                    continue;
                }
                if (c == '\'' || c == '"') {
                    quote = c;
                } else if (c == ',') {
                    items.Add(current.ToString());
                    current.Clear();
                }
            }
            if (current.Length > 0) {
                items.Add(current.ToString());
            }
            return items;
        }

        /// <summary>Builds wrapped lines: what is placed goes on the current line when it fits, else on a new indented one.</summary>
        private sealed class WrapWriter {
            private readonly List<string> _lines = new List<string>();
            private readonly StringBuilder _line = new StringBuilder();
            private readonly int _width;
            private readonly int _indent;
            private int _used;
            private bool _content;
            private bool _break;

            public WrapWriter(int width, int indent) {
                _width = width;
                _indent = indent;
            }

            // Whether text fits after what the current line holds (with a space between).
            public bool Fits(string text) => !_break && _used + (_content ? 1 : 0) + DisplayWidth.Measure(text) <= _width;

            /// <summary>Makes the next word start a new line.</summary>
            public void BreakBeforeNext() => _break = _content;

            /// <summary>Starts a new line indented by <paramref name="pad"/> unless the current one is still empty.</summary>
            public void StartLine(int pad) {
                if (_content) {
                    NewLine(pad);
                }
            }

            // Whether text fits a continuation line of its own.
            public bool FitsAlone(string text) => _indent + DisplayWidth.Measure(text) <= _width;

            /// <summary>Places a word: after a space when it fits, else on a new line indented by <paramref name="pad"/>.</summary>
            public void Place(string text, int pad) {
                if (_content && !Fits(text)) {
                    NewLine(pad);
                }
                if (_content) {
                    Append(" ");
                }
                AppendCut(text, pad);
            }

            /// <summary>Places list items: the first after a space (unless it starts the argument), the rest straight after the previous comma.</summary>
            public void PlaceItems(List<string> items, bool startsArgument, int pad) {
                for (int i = 0; i < items.Count; i++) {
                    string item = items[i];
                    bool spaced = i == 0 && _content;
                    int needed = (spaced ? 1 : 0) + DisplayWidth.Measure(item);
                    if (_content && (_break || _used + needed > _width)) {
                        NewLine(i == 0 && startsArgument ? _indent : pad);
                        spaced = false;
                    }
                    if (spaced) {
                        Append(" ");
                    }
                    AppendCut(item, pad);
                }
            }

            public List<string> Finish() {
                if (_content) {
                    _lines.Add(_line.ToString());
                }
                return _lines;
            }

            private void NewLine(int pad) {
                _lines.Add(_line.ToString());
                _line.Clear();
                _line.Append(' ', pad);
                _used = pad;
                _content = false;
                _break = false;
            }

            private void Append(string text) {
                _line.Append(text);
                _used += DisplayWidth.Measure(text);
                _content = true;
            }

            // Appends text, cutting it at the line end only when it is wider than the room left on a fresh line.
            private void AppendCut(string text, int pad) {
                if (_used + DisplayWidth.Measure(text) <= _width) {
                    Append(text);
                    return;
                }
                StringBuilder part = new StringBuilder();
                int partWidth = 0;
                foreach (TextCell cell in DisplayWidth.Cells(text)) {
                    if (_used + partWidth + cell.Width > _width && (partWidth > 0 || _content)) {
                        Append(part.ToString());
                        NewLine(pad);
                        part.Clear();
                        partWidth = 0;
                    }
                    part.Append(cell.Glyph);
                    partWidth += cell.Width;
                }
                if (part.Length > 0) {
                    Append(part.ToString());
                }
            }
        }

        private static List<string> Tokens(string command) {
            List<string> tokens = new List<string>();
            StringBuilder current = new StringBuilder();
            char quote = '\0';
            foreach (char c in command) {
                if (quote != '\0') {
                    current.Append(c);
                    if (c == quote) {
                        quote = '\0';
                    }
                    continue;
                }
                if (c == '\'' || c == '"') {
                    quote = c;
                    current.Append(c);
                    continue;
                }
                if (c == ' ') {
                    if (current.Length > 0) {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) {
                tokens.Add(current.ToString());
            }
            return tokens;
        }

        internal static string Format(object value) {
            switch (value) {
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }

        private static bool IsPlain(string value, string allowed) {
            foreach (char c in value) {
                if (!char.IsLetterOrDigit(c) && allowed.IndexOf(c) < 0) {
                    return false;
                }
            }
            return true;
        }
    }
}
