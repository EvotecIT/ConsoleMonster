using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// Log lines: the time stamp at the start, the level (error, warning, information, debug and trace words in their
    /// usual spellings), a [logger] or [component] after it, and smart values in the message.
    /// </summary>
    public sealed class LogHighlighter : ITextHighlighter {
        private static readonly Regex Head = new Regex(
            @"^(?<time>\[?(?:\d{4}-\d{2}-\d{2}[T ])?\d{1,2}:\d{2}:\d{2}(?:[.,]\d+)?(?:Z|[+-]\d{2}:?\d{2})?\]?)?\s*(?<level>\[?(?:FATAL|CRITICAL|CRIT|ERROR|ERR|FAIL|FAILED|WARNING|WARN|WRN|INFO|INFORMATION|INF|NOTICE|DEBUG|DBG|TRACE|TRC|VERBOSE|VRB)\]?:?(?=\s|$))?\s*(?<logger>\[[^\]\s]{1,60}\])?",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

        /// <inheritdoc />
        public string Name => "log";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            List<HighlightToken> tokens = new List<HighlightToken>();
            int lineStart = 0;
            while (lineStart <= text.Length) {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) {
                    lineEnd = text.Length;
                }
                string line = text.Substring(lineStart, lineEnd - lineStart);
                int rest = 0;
                try {
                    Match match = Head.Match(line);
                    if (match.Success) {
                        Add(tokens, lineStart, match.Groups["time"], HighlightRole.Timestamp);
                        Add(tokens, lineStart, match.Groups["level"], LevelRole(match.Groups["level"].Value));
                        Add(tokens, lineStart, match.Groups["logger"], HighlightRole.Type);
                        rest = match.Length;
                    }
                } catch (RegexMatchTimeoutException) {
                }
                foreach (HighlightToken token in Highlighters.Smart.Tokenize(line.Substring(rest))) {
                    tokens.Add(new HighlightToken(lineStart + rest + token.Start, token.Length, token.Role, token.Link));
                }
                lineStart = lineEnd + 1;
            }
            return tokens;
        }

        private static void Add(List<HighlightToken> tokens, int offset, Group group, HighlightRole role) {
            if (group.Success && group.Length > 0) {
                tokens.Add(new HighlightToken(offset + group.Index, group.Length, role));
            }
        }

        private static HighlightRole LevelRole(string level) {
            string word = level.Trim('[', ']', ':').ToUpperInvariant();
            if (word.StartsWith("FAT", StringComparison.Ordinal) || word.StartsWith("CRIT", StringComparison.Ordinal) || word.StartsWith("ERR", StringComparison.Ordinal) || word.StartsWith("FAIL", StringComparison.Ordinal)) {
                return HighlightRole.Error;
            }
            if (word.StartsWith("W", StringComparison.Ordinal)) {
                return HighlightRole.Warning;
            }
            if (word.StartsWith("INF", StringComparison.Ordinal) || word == "NOTICE") {
                return HighlightRole.Info;
            }
            return HighlightRole.Comment;
        }
    }

    /// <summary>Diffs and patches: file headers, hunk headers, added and removed lines.</summary>
    public sealed class DiffHighlighter : ITextHighlighter {
        /// <inheritdoc />
        public string Name => "diff";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int lineStart = 0;
            while (lineStart < text.Length) {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) {
                    lineEnd = text.Length;
                }
                int length = lineEnd - lineStart;
                if (length > 0) {
                    HighlightRole role = Role(text, lineStart, length);
                    if (role == HighlightRole.Header && string.CompareOrdinal(text, lineStart, "@@", 0, 2) == 0) {
                        int close = text.IndexOf("@@", lineStart + 2, length - 2, StringComparison.Ordinal);
                        tokens.Add(lineStart, close < 0 ? length : close + 2 - lineStart, HighlightRole.Header);
                    } else {
                        tokens.Add(lineStart, length, role);
                    }
                }
                lineStart = lineEnd + 1;
            }
            return tokens.Tokens;
        }

        private static HighlightRole Role(string text, int start, int length) {
            string line = text.Substring(start, Math.Min(length, 12));
            if (line.StartsWith("diff ", StringComparison.Ordinal) || line.StartsWith("index ", StringComparison.Ordinal) || line.StartsWith("+++", StringComparison.Ordinal) || line.StartsWith("---", StringComparison.Ordinal) || line.StartsWith("@@", StringComparison.Ordinal)) {
                return HighlightRole.Header;
            }
            switch (line[0]) {
                case '+':
                    return HighlightRole.Added;
                case '-':
                    return HighlightRole.Removed;
                case '\\':
                    return HighlightRole.Comment;
                default:
                    return HighlightRole.Text;
            }
        }
    }

    /// <summary>CSV and TSV: each column takes its own colour (cycling through six), quoted fields keep theirs, separators are muted.</summary>
    public sealed class CsvHighlighter : ITextHighlighter {
        private static readonly HighlightRole[] Columns = { HighlightRole.Key, HighlightRole.String, HighlightRole.Number, HighlightRole.Type, HighlightRole.Variable, HighlightRole.Keyword };

        /// <inheritdoc />
        public string Name => "csv";

        /// <inheritdoc />
        public IReadOnlyList<HighlightToken> Tokenize(string text) {
            TokenList tokens = new TokenList();
            int firstLine = text.IndexOf('\n');
            string head = firstLine < 0 ? text : text.Substring(0, firstLine);
            char separator = head.IndexOf('\t') >= 0 ? '\t' : head.IndexOf(';') > head.IndexOf(',') ? ';' : ',';
            int column = 0;
            int fieldStart = 0;
            bool quoted = false;
            for (int i = 0; i <= text.Length; i++) {
                char c = i < text.Length ? text[i] : '\n';
                if (quoted) {
                    if (c == '"') {
                        if (i + 1 < text.Length && text[i + 1] == '"') {
                            i++;
                        } else {
                            quoted = false;
                        }
                    }
                    continue;
                }
                if (c == '"') {
                    quoted = true;
                } else if (c == separator || c == '\n' || c == '\r') {
                    tokens.Add(fieldStart, i - fieldStart, Columns[column % Columns.Length]);
                    if (c == separator) {
                        tokens.Add(i, 1, HighlightRole.Punctuation);
                        column++;
                    } else if (c == '\n') {
                        column = 0;
                    }
                    fieldStart = i + 1;
                }
            }
            return tokens.Tokens;
        }
    }
}
