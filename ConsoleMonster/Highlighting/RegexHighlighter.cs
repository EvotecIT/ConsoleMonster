using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// A highlighter built from regular-expression rules, tried in order at each position (the first rule that matches
    /// wins). Use it to add a language without writing a lexer:
    /// <c>Highlighters.Register(new RegexHighlighter("ini").Rule(@"^\s*;.*$", HighlightRole.Comment).Rule(...), "ini")</c>.
    /// Matching has a time limit (default 250 ms per text); text after a timeout stays plain.
    /// </summary>
    public sealed class RegexHighlighter : ITextHighlighter {
        private readonly List<KeyValuePair<string, HighlightRole>> _rules = new List<KeyValuePair<string, HighlightRole>>();
        private Regex? _combined;

        /// <summary>Creates an empty highlighter.</summary>
        /// <param name="name">The language name.</param>
        /// <param name="ignoreCase">Whether the rules ignore case.</param>
        public RegexHighlighter(string name, bool ignoreCase = false) {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IgnoreCase = ignoreCase;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>Whether the rules ignore case.</summary>
        public bool IgnoreCase { get; }

        /// <summary>The time limit for one text (default 250 ms).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>Adds a rule. Patterns run in multiline mode (^ and $ match at line breaks); avoid nested quantifiers.</summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="role">The role of what it matches.</param>
        /// <returns>This highlighter.</returns>
        public RegexHighlighter Rule(string pattern, HighlightRole role) {
            _rules.Add(new KeyValuePair<string, HighlightRole>(pattern ?? throw new ArgumentNullException(nameof(pattern)), role));
            _combined = null;
            return this;
        }

        /// <summary>Adds a keyword rule: whole words from a list.</summary>
        /// <param name="role">The role.</param>
        /// <param name="words">The words.</param>
        /// <returns>This highlighter.</returns>
        public RegexHighlighter Words(HighlightRole role, params string[] words) {
            StringBuilder sb = new StringBuilder(@"\b(?:");
            for (int i = 0; i < words.Length; i++) {
                if (i > 0) {
                    sb.Append('|');
                }
                sb.Append(Regex.Escape(words[i]));
            }
            return Rule(sb.Append(@")\b").ToString(), role);
        }

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            List<HighlightToken> tokens = new List<HighlightToken>();
            Regex regex = Combined();
            try {
                for (Match match = regex.Match(text); match.Success; match = match.NextMatch()) {
                    if (match.Length == 0) {
                        continue;
                    }
                    for (int g = 0; g < _rules.Count; g++) {
                        if (match.Groups["r" + g.ToString(System.Globalization.CultureInfo.InvariantCulture)].Success) {
                            tokens.Add(new HighlightToken(match.Index, match.Length, _rules[g].Value));
                            break;
                        }
                    }
                }
            } catch (RegexMatchTimeoutException) {
            }
            return tokens;
        }

        private Regex Combined() {
            Regex? combined = _combined;
            if (combined != null) {
                return combined;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _rules.Count; i++) {
                if (i > 0) {
                    sb.Append('|');
                }
                sb.Append("(?<r").Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('>').Append(_rules[i].Key).Append(')');
            }
            RegexOptions options = RegexOptions.Multiline | RegexOptions.CultureInvariant | (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            combined = new Regex(_rules.Count == 0 ? "(?!)" : sb.ToString(), options, Timeout);
            _combined = combined;
            return combined;
        }
    }

    /// <summary>
    /// Values in plain text: URLs and e-mail addresses (as links), GUIDs, distinguished names, dates and times, IPv4 and
    /// IPv6 addresses, Windows and Unix paths, quoted strings, booleans and null, and numbers with units.
    /// </summary>
    public sealed class SmartHighlighter : ITextHighlighter {
        private static readonly Regex Values = new Regex(
            @"(?<url>\b(?:https?|ftp)://[^\s<>""'`]*[^\s<>""'`.,;:!?)\]])" +
            @"|(?<mail>\b[\w.+-]+@[\w-]+(?:\.[\w-]+)+\b)" +
            @"|(?<guid>\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b)" +
            @"|(?<dn>\b(?:CN|OU|DC|O|L|ST|C)=[^,=\s]+(?: [^,=\s]+)*(?:,\s?(?:CN|OU|DC|O|L|ST|C)=[^,=\s]+(?: [^,=\s]+)*)*,\s?(?:CN|OU|DC|O|L|ST|C)=[^,=\s]+)" +
            @"|(?<date>\b\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:[.,]\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?\b|\b\d{1,2}:\d{2}:\d{2}(?:[.,]\d+)?\b)" +
            @"|(?<ip4>\b(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(?:/\d{1,2})?\b)" +
            @"|(?<ip6>(?<![\w:])(?:(?:[0-9a-fA-F]{1,4}:){1,7}(?::[0-9a-fA-F]{1,4}){1,7}|(?:[0-9a-fA-F]{1,4}:){3,7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|::1)(?![\w:]))" +
            @"|(?<path>(?<![\w/])(?:[A-Za-z]:\\|\\\\)[^\s""'<>|*?]+|(?<![\w/.~])(?:~|\.{1,2})?/(?:[\w.-]+/)+[\w.-]*)" +
            @"|(?<str>""[^""\r\n]*""|(?<!\w)'[^'\r\n]*')" +
            @"|(?<lit>(?<![\w$])\$?(?:true|false|null|True|False|TRUE|FALSE|NULL|None)\b)" +
            @"|(?<num>(?<![\w.])[-+]?\d+(?:[.,]\d+)*(?:[eE][-+]?\d+)?(?:%|ms|s|KB|MB|GB|TB|kB)?(?![\w.]))",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

        /// <inheritdoc />
        public string Name => "smart";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            List<HighlightToken> tokens = new List<HighlightToken>();
            try {
                for (Match match = Values.Match(text); match.Success; match = match.NextMatch()) {
                    if (match.Groups["url"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Link, match.Value));
                    } else if (match.Groups["mail"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Link, "mailto:" + match.Value));
                    } else if (match.Groups["date"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Timestamp));
                    } else if (match.Groups["str"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.String));
                    } else if (match.Groups["lit"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Literal));
                    } else if (match.Groups["num"].Success) {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Number));
                    } else {
                        tokens.Add(new HighlightToken(match.Index, match.Length, HighlightRole.Identifier));
                    }
                }
            } catch (RegexMatchTimeoutException) {
            }
            return tokens;
        }
    }

    /// <summary>The regular-expression languages: C#, SQL, INI/TOML and Markdown.</summary>
    internal static class BuiltInRules {
        public static RegexHighlighter CSharp() {
            return new RegexHighlighter("csharp")
                .Rule(@"//[^\n]*", HighlightRole.Comment)
                .Rule(@"/\*[\s\S]*?\*/", HighlightRole.Comment)
                .Rule(@"\$?@""(?:[^""]|"""")*""|\$?""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.){1,6}'", HighlightRole.String)
                .Rule(@"^\s*#\w+[^\n]*", HighlightRole.Keyword)
                .Words(HighlightRole.Keyword, "abstract", "as", "async", "await", "base", "break", "case", "catch", "checked", "class", "const", "continue", "default", "delegate", "do", "else", "enum", "event", "explicit", "extern", "finally", "fixed", "for", "foreach", "get", "goto", "if", "implicit", "in", "init", "interface", "internal", "is", "lock", "namespace", "new", "operator", "out", "override", "params", "partial", "private", "protected", "public", "readonly", "record", "ref", "return", "sealed", "set", "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw", "try", "typeof", "unchecked", "unsafe", "using", "value", "var", "virtual", "void", "volatile", "when", "where", "while", "yield")
                .Words(HighlightRole.Literal, "true", "false", "null")
                .Words(HighlightRole.Type, "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "nint", "nuint", "object", "sbyte", "short", "string", "uint", "ulong", "ushort")
                .Rule(@"\b[A-Z][A-Za-z0-9_]*(?=\s*(?:<[^>\n]*>)?\s+[a-z_]\w*\s*[=;,)])", HighlightRole.Type)
                .Rule(@"\b(?:0x[0-9a-fA-F_]+|\d[\d_]*(?:\.\d+)?(?:[eE][-+]?\d+)?)[uUlLfFdDmM]*\b", HighlightRole.Number)
                .Rule(@"[{}()\[\];,.]", HighlightRole.Punctuation)
                .Rule(@"=>|[=!<>]=?|&&|\|\||[+\-*/%?]", HighlightRole.Operator);
        }

        public static RegexHighlighter Sql() {
            return new RegexHighlighter("sql", true)
                .Rule(@"--[^\n]*", HighlightRole.Comment)
                .Rule(@"/\*[\s\S]*?\*/", HighlightRole.Comment)
                .Rule(@"N?'(?:[^']|'')*'", HighlightRole.String)
                .Rule(@"\[[^\]\n]+\]|""[^""\n]+""", HighlightRole.Type)
                .Rule(@"@@?\w+", HighlightRole.Variable)
                .Words(HighlightRole.Keyword, "select", "from", "where", "and", "or", "not", "insert", "into", "values", "update", "set", "delete", "create", "alter", "drop", "table", "view", "index", "procedure", "function", "trigger", "join", "inner", "left", "right", "full", "outer", "cross", "on", "as", "group", "by", "order", "having", "union", "all", "distinct", "top", "limit", "offset", "case", "when", "then", "else", "end", "begin", "commit", "rollback", "transaction", "declare", "exec", "execute", "return", "if", "exists", "in", "is", "like", "between", "primary", "key", "foreign", "references", "default", "with", "asc", "desc", "go", "use", "grant", "revoke", "merge", "output", "over", "partition")
                .Words(HighlightRole.Literal, "null", "true", "false")
                .Rule(@"\b\d+(?:\.\d+)?\b", HighlightRole.Number)
                .Rule(@"[(),;.]", HighlightRole.Punctuation)
                .Rule(@"<>|[=<>!]=?|[+\-*/%]", HighlightRole.Operator);
        }

        public static RegexHighlighter Ini() {
            return new RegexHighlighter("ini")
                .Rule(@"^[ \t]*[;#][^\n]*", HighlightRole.Comment)
                .Rule(@"^[ \t]*\[{1,2}[^\]\n]+\]{1,2}", HighlightRole.Header)
                .Rule(@"^[ \t]*[\w.\-""' ]+?(?=[ \t]*[=:])", HighlightRole.Key)
                .Rule(@"""(?:[^""\\\n]|\\.)*""|'[^'\n]*'", HighlightRole.String)
                .Rule(@"\b(?:true|false|yes|no|on|off)\b", HighlightRole.Literal)
                .Rule(@"(?<![\w.])[-+]?\d+(?:\.\d+)?(?![\w.])", HighlightRole.Number)
                .Rule(@"[=:\[\],{}]", HighlightRole.Punctuation);
        }

        public static RegexHighlighter Markdown() {
            return new RegexHighlighter("markdown")
                .Rule(@"^```[^\n]*", HighlightRole.Comment)
                .Rule(@"^#{1,6}[ \t][^\n]*", HighlightRole.Heading)
                .Rule(@"^[ \t]*>[^\n]*", HighlightRole.Comment)
                .Rule(@"`[^`\n]+`", HighlightRole.String)
                .Rule(@"\*\*[^*\n]+\*\*|__[^_\n]+__", HighlightRole.Strong)
                .Rule(@"(?<![*\w])\*[^*\n]+\*(?!\*)|(?<![_\w])_[^_\n]+_(?![_\w])", HighlightRole.Emphasis)
                .Rule(@"!?\[[^\]\n]+\]\([^)\s]+\)", HighlightRole.Link)
                .Rule(@"^[ \t]*(?:[-*+]|\d+\.)(?=[ \t])", HighlightRole.Punctuation);
        }
    }
}
