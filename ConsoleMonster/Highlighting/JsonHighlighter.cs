using System.Collections.Generic;

namespace ConsoleMonster.Highlighting {
    /// <summary>JSON (and JSONC comments): keys, strings, numbers, true/false/null, punctuation; anything else is an error.</summary>
    public sealed class JsonHighlighter : ITextHighlighter {
        /// <inheritdoc />
        public string Name => "json";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int i = 0;
            int n = text.Length;
            while (i < n) {
                char c = text[i];
                if (char.IsWhiteSpace(c)) {
                    i++;
                } else if (c == '"') {
                    int start = i++;
                    while (i < n && text[i] != '"' && text[i] != '\n') {
                        i += text[i] == '\\' && i + 1 < n ? 2 : 1;
                    }
                    if (i < n && text[i] == '"') {
                        i++;
                    }
                    int look = i;
                    while (look < n && (text[look] == ' ' || text[look] == '\t')) {
                        look++;
                    }
                    tokens.Add(start, i - start, look < n && text[look] == ':' ? HighlightRole.Key : HighlightRole.String);
                } else if (c == '-' || char.IsDigit(c)) {
                    int start = i++;
                    while (i < n && (char.IsDigit(text[i]) || text[i] == '.' || text[i] == 'e' || text[i] == 'E' || text[i] == '+' || text[i] == '-')) {
                        i++;
                    }
                    tokens.Add(start, i - start, HighlightRole.Number);
                } else if (char.IsLetter(c)) {
                    int start = i;
                    while (i < n && char.IsLetterOrDigit(text[i])) {
                        i++;
                    }
                    string word = text.Substring(start, i - start);
                    tokens.Add(start, i - start, word == "true" || word == "false" || word == "null" ? HighlightRole.Literal : HighlightRole.Error);
                } else if (c == '/' && i + 1 < n && text[i + 1] == '/') {
                    int start = i;
                    while (i < n && text[i] != '\n') {
                        i++;
                    }
                    tokens.Add(start, i - start, HighlightRole.Comment);
                } else if (c == '/' && i + 1 < n && text[i + 1] == '*') {
                    int end = text.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                    int stop = end < 0 ? n : end + 2;
                    tokens.Add(i, stop - i, HighlightRole.Comment);
                    i = stop;
                } else if (c == '{' || c == '}' || c == '[' || c == ']' || c == ',' || c == ':') {
                    tokens.Add(i, 1, HighlightRole.Punctuation);
                    i++;
                } else {
                    tokens.Add(i, 1, HighlightRole.Error);
                    i++;
                }
            }
            return tokens.Tokens;
        }
    }
}
