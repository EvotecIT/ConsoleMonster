using System;
using System.Collections.Generic;

namespace ConsoleMonster.Highlighting {
    /// <summary>XML and HTML: tags, attribute names and values, comments, CDATA, processing instructions and entities.</summary>
    public sealed class XmlHighlighter : ITextHighlighter {
        /// <inheritdoc />
        public string Name => "xml";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int i = 0;
            int n = text.Length;
            while (i < n) {
                if (Starts(text, i, "<!--")) {
                    i = Block(tokens, text, i, "-->", HighlightRole.Comment);
                } else if (Starts(text, i, "<![CDATA[")) {
                    i = Block(tokens, text, i, "]]>", HighlightRole.String);
                } else if (Starts(text, i, "<?") || Starts(text, i, "<!")) {
                    i = Block(tokens, text, i, ">", HighlightRole.Keyword);
                } else if (text[i] == '<' && i + 1 < n && (char.IsLetter(text[i + 1]) || text[i + 1] == '/' || text[i + 1] == '_')) {
                    int start = i;
                    i += text[i + 1] == '/' ? 2 : 1;
                    tokens.Add(start, i - start, HighlightRole.Punctuation);
                    int name = i;
                    while (i < n && !char.IsWhiteSpace(text[i]) && text[i] != '>' && text[i] != '/') {
                        i++;
                    }
                    tokens.Add(name, i - name, HighlightRole.Type);
                    while (i < n && text[i] != '>') {
                        char c = text[i];
                        if (c == '"' || c == '\'') {
                            int end = text.IndexOf(c, i + 1);
                            int stop = end < 0 ? n : end + 1;
                            tokens.Add(i, stop - i, HighlightRole.String);
                            i = stop;
                        } else if (c == '=' || c == '/') {
                            tokens.Add(i, 1, HighlightRole.Punctuation);
                            i++;
                        } else if (!char.IsWhiteSpace(c)) {
                            int attribute = i;
                            while (i < n && !char.IsWhiteSpace(text[i]) && text[i] != '=' && text[i] != '>' && text[i] != '/') {
                                i++;
                            }
                            tokens.Add(attribute, i - attribute, HighlightRole.Key);
                        } else {
                            i++;
                        }
                    }
                    if (i < n) {
                        tokens.Add(i, 1, HighlightRole.Punctuation);
                        i++;
                    }
                } else if (text[i] == '&') {
                    int end = text.IndexOf(';', i);
                    if (end > i && end - i <= 10) {
                        tokens.Add(i, end - i + 1, HighlightRole.Literal);
                        i = end + 1;
                    } else {
                        i++;
                    }
                } else {
                    i++;
                }
            }
            return tokens.Tokens;
        }

        private static bool Starts(string text, int index, string value) => string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

        private static int Block(TokenList tokens, string text, int start, string close, HighlightRole role) {
            int end = text.IndexOf(close, start + 2, StringComparison.Ordinal);
            int stop = end < 0 ? text.Length : end + close.Length;
            tokens.Add(start, stop - start, role);
            return stop;
        }
    }

    /// <summary>YAML: comments, document markers, keys, list markers, quoted strings, numbers, booleans/null, anchors and aliases.</summary>
    public sealed class YamlHighlighter : ITextHighlighter {
        /// <inheritdoc />
        public string Name => "yaml";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int lineStart = 0;
            while (lineStart <= text.Length) {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) {
                    lineEnd = text.Length;
                }
                Line(tokens, text, lineStart, lineEnd);
                lineStart = lineEnd + 1;
            }
            return tokens.Tokens;
        }

        private static void Line(TokenList tokens, string text, int start, int end) {
            int i = start;
            while (i < end && (text[i] == ' ' || text[i] == '\t')) {
                i++;
            }
            if (i >= end) {
                return;
            }
            if (text[i] == '#') {
                tokens.Add(i, end - i, HighlightRole.Comment);
                return;
            }
            if ((string.CompareOrdinal(text, i, "---", 0, 3) == 0 || string.CompareOrdinal(text, i, "...", 0, 3) == 0) && i == start) {
                tokens.Add(i, 3, HighlightRole.Header);
                i += 3;
            }
            while (i + 1 < end && text[i] == '-' && (text[i + 1] == ' ' || text[i + 1] == '\t')) {
                tokens.Add(i, 1, HighlightRole.Punctuation);
                i += 2;
                while (i < end && text[i] == ' ') {
                    i++;
                }
            }
            int colon = KeyColon(text, i, end);
            if (colon > i) {
                tokens.Add(i, colon - i, HighlightRole.Key);
                tokens.Add(colon, 1, HighlightRole.Punctuation);
                i = colon + 1;
            }
            Value(tokens, text, i, end);
        }

        // The colon that ends a key ("name:" followed by a space or the line end), outside quotes; -1 if none.
        private static int KeyColon(string text, int start, int end) {
            char quote = '\0';
            for (int i = start; i < end; i++) {
                char c = text[i];
                if (quote != '\0') {
                    if (c == quote) {
                        quote = '\0';
                    }
                } else if (c == '"' || c == '\'') {
                    if (i != start) {
                        return -1;
                    }
                    quote = c;
                } else if (c == '#' && i > start && text[i - 1] == ' ') {
                    return -1;
                } else if (c == ':' && (i + 1 >= end || text[i + 1] == ' ' || text[i + 1] == '\t' || text[i + 1] == '\r')) {
                    return i;
                } else if (c == '{' || c == '[') {
                    return -1;
                }
            }
            return -1;
        }

        private static void Value(TokenList tokens, string text, int start, int end) {
            int i = start;
            while (i < end && (text[i] == ' ' || text[i] == '\t')) {
                i++;
            }
            int comment = -1;
            char quote = '\0';
            for (int k = i; k < end; k++) {
                if (quote != '\0') {
                    if (text[k] == quote) {
                        quote = '\0';
                    }
                } else if (text[k] == '"' || text[k] == '\'') {
                    quote = text[k];
                } else if (text[k] == '#' && k > i && text[k - 1] == ' ') {
                    comment = k;
                    break;
                }
            }
            int valueEnd = comment < 0 ? end : comment;
            while (valueEnd > i && (text[valueEnd - 1] == ' ' || text[valueEnd - 1] == '\r')) {
                valueEnd--;
            }
            if (valueEnd > i) {
                string value = text.Substring(i, valueEnd - i);
                HighlightRole role = HighlightRole.Text;
                if (value[0] == '"' || value[0] == '\'') {
                    role = HighlightRole.String;
                } else if (value[0] == '&' || value[0] == '*') {
                    role = HighlightRole.Variable;
                } else if (value == "|" || value == ">" || value == "|-" || value == ">-" || value == "|+" || value == ">+") {
                    role = HighlightRole.Punctuation;
                } else if (IsLiteral(value)) {
                    role = HighlightRole.Literal;
                } else if (double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) {
                    role = HighlightRole.Number;
                } else if (value[0] == '{' || value[0] == '[') {
                    role = HighlightRole.Punctuation;
                }
                tokens.Add(i, valueEnd - i, role);
            }
            if (comment >= 0) {
                tokens.Add(comment, end - comment, HighlightRole.Comment);
            }
        }

        private static bool IsLiteral(string value) {
            switch (value.ToLowerInvariant()) {
                case "true":
                case "false":
                case "yes":
                case "no":
                case "on":
                case "off":
                case "null":
                case "~":
                    return true;
                default:
                    return false;
            }
        }
    }
}
