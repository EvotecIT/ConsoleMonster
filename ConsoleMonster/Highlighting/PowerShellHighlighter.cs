using System;
using System.Collections.Generic;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// PowerShell: comments and comment blocks, strings and here-strings, variables, keywords, commands (Verb-Noun),
    /// parameters, operators (-eq, -match), type literals ([int]), numbers (with KB/MB suffixes) and punctuation.
    /// </summary>
    public sealed class PowerShellHighlighter : ITextHighlighter {
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "if", "else", "elseif", "foreach", "for", "while", "do", "until", "switch", "function", "filter", "param", "begin",
            "process", "end", "clean", "return", "break", "continue", "try", "catch", "finally", "throw", "trap", "class", "enum",
            "using", "in", "data", "dynamicparam", "exit", "workflow", "parallel", "sequence", "inlinescript", "configuration",
            "hidden", "static", "base"
        };

        private static readonly HashSet<string> Operators = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "eq", "ne", "gt", "ge", "lt", "le", "like", "notlike", "match", "notmatch", "contains", "notcontains", "in", "notin",
            "replace", "split", "join", "is", "isnot", "as", "and", "or", "not", "xor", "band", "bor", "bxor", "bnot", "shl", "shr",
            "f", "ceq", "cne", "clike", "cmatch", "creplace", "csplit", "ieq", "ilike", "imatch", "ireplace"
        };

        /// <inheritdoc />
        public string Name => "powershell";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int i = 0;
            int n = text.Length;
            while (i < n) {
                char c = text[i];
                if (c == '<' && i + 1 < n && text[i + 1] == '#') {
                    int end = text.IndexOf("#>", i + 2, StringComparison.Ordinal);
                    int stop = end < 0 ? n : end + 2;
                    tokens.Add(i, stop - i, HighlightRole.Comment);
                    i = stop;
                } else if (c == '#') {
                    int start = i;
                    while (i < n && text[i] != '\n') {
                        i++;
                    }
                    tokens.Add(start, i - start, HighlightRole.Comment);
                } else if (c == '@' && i + 1 < n && (text[i + 1] == '"' || text[i + 1] == '\'') && IsLineEnd(text, i + 2)) {
                    string close = "\n" + text[i + 1] + "@";
                    int end = text.IndexOf(close, i + 2, StringComparison.Ordinal);
                    int stop = end < 0 ? n : end + close.Length;
                    tokens.Add(i, stop - i, HighlightRole.String);
                    i = stop;
                } else if (c == '\'') {
                    int start = i++;
                    while (i < n) {
                        if (text[i] == '\'') {
                            if (i + 1 < n && text[i + 1] == '\'') {
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        i++;
                    }
                    tokens.Add(start, i - start, HighlightRole.String);
                } else if (c == '"') {
                    int start = i++;
                    while (i < n) {
                        if (text[i] == '`' && i + 1 < n) {
                            i += 2;
                            continue;
                        }
                        if (text[i] == '"') {
                            if (i + 1 < n && text[i + 1] == '"') {
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        i++;
                    }
                    tokens.Add(start, i - start, HighlightRole.String);
                } else if (c == '$' && i + 1 < n && text[i + 1] == '{') {
                    int end = text.IndexOf('}', i + 2);
                    int stop = end < 0 ? n : end + 1;
                    tokens.Add(i, stop - i, HighlightRole.Variable);
                    i = stop;
                } else if (c == '$' && i + 1 < n && (char.IsLetterOrDigit(text[i + 1]) || text[i + 1] == '_' || text[i + 1] == '?' || text[i + 1] == '$' || text[i + 1] == '^')) {
                    int start = i++;
                    if (text[i] == '?' || text[i] == '$' || text[i] == '^') {
                        i++;
                    } else {
                        while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || (text[i] == ':' && i + 1 < n && char.IsLetter(text[i + 1])))) {
                            i++;
                        }
                    }
                    string name = text.Substring(start + 1, i - start - 1);
                    bool literal = string.Equals(name, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "false", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "null", StringComparison.OrdinalIgnoreCase);
                    tokens.Add(start, i - start, literal ? HighlightRole.Literal : HighlightRole.Variable);
                } else if (c == '[' && i + 1 < n && char.IsLetter(text[i + 1]) && TypeLiteralEnd(text, i) is int typeEnd) {
                    tokens.Add(i, typeEnd - i, HighlightRole.Type);
                    i = typeEnd;
                } else if (c == '-' && i + 1 < n && char.IsLetter(text[i + 1]) && (i == 0 || !char.IsLetterOrDigit(text[i - 1]))) {
                    int start = i++;
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) {
                        i++;
                    }
                    string word = text.Substring(start + 1, i - start - 1);
                    tokens.Add(start, i - start, Operators.Contains(word) ? HighlightRole.Operator : HighlightRole.Key);
                } else if (char.IsDigit(c) && (i == 0 || !char.IsLetterOrDigit(text[i - 1]))) {
                    int start = i;
                    if (c == '0' && i + 1 < n && (text[i + 1] == 'x' || text[i + 1] == 'X')) {
                        i += 2;
                        while (i < n && Uri.IsHexDigit(text[i])) {
                            i++;
                        }
                    } else {
                        while (i < n && (char.IsDigit(text[i]) || (text[i] == '.' && i + 1 < n && char.IsDigit(text[i + 1])))) {
                            i++;
                        }
                        if (i + 1 < n && char.ToLowerInvariant(text[i + 1]) == 'b' && "kmgtp".IndexOf(char.ToLowerInvariant(text[i])) >= 0) {
                            i += 2;
                        }
                    }
                    if (i < n && (char.IsLetter(text[i]) || text[i] == '_')) {
                        while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '-' || text[i] == '_')) {
                            i++;
                        }
                        continue;
                    }
                    tokens.Add(start, i - start, HighlightRole.Number);
                } else if (char.IsLetter(c) || c == '_') {
                    int start = i;
                    while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.' || (text[i] == '-' && i + 1 < n && char.IsLetter(text[i + 1])))) {
                        i++;
                    }
                    string word = text.Substring(start, i - start);
                    if (Keywords.Contains(word)) {
                        tokens.Add(start, i - start, HighlightRole.Keyword);
                    } else if (word.IndexOf('-') > 0 && char.IsUpper(word[0])) {
                        tokens.Add(start, i - start, HighlightRole.Type);
                    }
                } else if (c == '|' || c == '=' || c == '!' || c == '+' || c == '*' || c == '%' || c == '>' || c == '<') {
                    tokens.Add(i, 1, HighlightRole.Operator);
                    i++;
                } else if ("(){}[];,.".IndexOf(c) >= 0) {
                    tokens.Add(i, 1, HighlightRole.Punctuation);
                    i++;
                } else {
                    i++;
                }
            }
            return tokens.Tokens;
        }

        private static bool IsLineEnd(string text, int index) {
            while (index < text.Length && (text[index] == ' ' || text[index] == '\t' || text[index] == '\r')) {
                index++;
            }
            return index >= text.Length || text[index] == '\n';
        }

        // [Type.Name], [string[]], [Collections.Generic.List[int]] on one line: the index after the closing bracket, or null.
        private static int? TypeLiteralEnd(string text, int start) {
            int depth = 0;
            for (int i = start; i < text.Length && i < start + 200; i++) {
                char c = text[i];
                if (c == '[') {
                    depth++;
                } else if (c == ']') {
                    depth--;
                    if (depth == 0) {
                        return i + 1;
                    }
                } else if (!(char.IsLetterOrDigit(c) || c == '.' || c == ',' || c == '`' || c == ' ' || c == '_')) {
                    return null;
                }
            }
            return null;
        }
    }
}
