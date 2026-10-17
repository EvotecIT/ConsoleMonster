using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

        /// <summary>Quotes a value when the style needs it.</summary>
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
                    return "\"" + value.Replace("\"", "\\\"") + "\"";
                default:
                    if (value.Length > 0 && IsPlain(value, "-_./:\\=+%")) {
                        return value;
                    }
                    return "'" + value.Replace("'", "''") + "'";
            }
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
        /// Wraps a command line for display, breaking between arguments (a parameter stays with its value) and only inside
        /// an argument when it alone is wider than the line.
        /// </summary>
        /// <param name="command">The command line.</param>
        /// <param name="width">The line width.</param>
        /// <returns>The lines.</returns>
        public static IReadOnlyList<string> Wrap(string command, int width) {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(command)) {
                return lines;
            }
            width = Math.Max(8, width);
            List<string> tokens = Tokens(command);
            List<string> groups = new List<string>();
            for (int i = 0; i < tokens.Count; i++) {
                string token = tokens[i];
                bool parameter = token.Length > 1 && (token[0] == '-' || token[0] == '/') && !char.IsDigit(token[1]);
                if (parameter && i + 1 < tokens.Count && !(tokens[i + 1].Length > 1 && (tokens[i + 1][0] == '-' || tokens[i + 1][0] == '/') && !char.IsDigit(tokens[i + 1][1]))) {
                    groups.Add(token + " " + tokens[i + 1]);
                    i++;
                } else {
                    groups.Add(token);
                }
            }
            StringBuilder line = new StringBuilder();
            foreach (string group in groups) {
                int needed = (line.Length > 0 ? 1 : 0) + group.Length;
                if (line.Length > 0 && line.Length + needed > width) {
                    lines.Add(line.ToString());
                    line.Clear();
                }
                string rest = group;
                while (rest.Length > width) {
                    if (line.Length > 0) {
                        lines.Add(line.ToString());
                        line.Clear();
                    }
                    lines.Add(rest.Substring(0, width));
                    rest = rest.Substring(width);
                }
                if (line.Length > 0) {
                    line.Append(' ');
                }
                line.Append(rest);
            }
            if (line.Length > 0) {
                lines.Add(line.ToString());
            }
            return lines;
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
