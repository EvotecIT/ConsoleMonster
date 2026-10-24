using System;
using System.Collections.Generic;

namespace ConsoleMonster.Demos {
    // Lexical statement grouping only. The real PowerShell parser in the child remains execution authority.
    internal sealed class PowerShellCommandBoundary {
        private readonly Stack<char> _brackets = new Stack<char>();
        private char _quote;
        private char _here;
        private bool _comment;
        private bool _continuation;
        internal bool Complete { get; private set; }
        // Includes literal string content, but excludes comments and whitespace outside strings.
        internal bool HasCode { get; private set; }
        internal bool Feed(string line) {
            HasCode = true;
            bool closedHereString = false;
            if (_here != '\0') {
                if (!line.StartsWith(_here + "@", StringComparison.Ordinal)) { return Complete = false; }
                _here = '\0'; closedHereString = true; line = line.Substring(2);
            }
            bool continuation = false;
            string significant = closedHereString || _quote != '\0' ? "text" : string.Empty;
            for (int i = 0; i < line.Length; i++) {
                char c = line[i];
                char next = i + 1 < line.Length ? line[i + 1] : '\0';
                if (_comment) { if (c == '#' && next == '>') { _comment = false; i++; } continue; }
                if (_quote != '\0') {
                    if (c == '`' && _quote == '"') { i++; continue; }
                    if (c == _quote) { if (next == c) { i++; } else { _quote = '\0'; } }
                    continue;
                }
                if (c == '<' && next == '#') { _comment = true; i++; continue; }
                if (c == '#') { break; }
                if (c == '`') { continuation = i == line.Length - 1; i++; continue; }
                if (c == '@' && (next == '\'' || next == '"') && line.Substring(i + 2).Trim().Length == 0) { _here = next; break; }
                if (c == '\'' || c == '"') { _quote = c; significant = "text"; continue; }
                if (c == '(' || c == '[' || c == '{') { _brackets.Push(c); }
                if (c == ')' || c == ']' || c == '}') {
                    if (_brackets.Count == 0 || _brackets.Pop() != (c == ')' ? '(' : c == ']' ? '[' : '{')) { throw new FormatException("Unbalanced PowerShell brackets."); }
                }
                if (!char.IsWhiteSpace(c)) { significant += c; }
            }
            string[] operators = { "|", ",", "+", "=", "&&", "||", "-and", "-or", "-f" };
            foreach (string op in operators) { if (significant.EndsWith(op, StringComparison.OrdinalIgnoreCase)) { continuation = true; } }
            // Blank and comment-only lines do not finish a pending pipeline or operator.
            if (significant.Length > 0 || continuation) { _continuation = continuation; }
            HasCode = significant.Length > 0 || continuation || _here != '\0';
            return Complete = _brackets.Count == 0 && _quote == '\0' && _here == '\0' && !_comment && !_continuation;
        }
    }
}
