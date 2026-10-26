using System;
using System.Collections.Generic;

namespace ConsoleMonster.Text {
    /// <summary>
    /// A shell-style name pattern such as <c>*.cs</c>, <c>report-??.csv</c> or <c>[a-c]*</c>, for marking rows by
    /// pattern. Several patterns are separated with <c>;</c> or <c>,</c> and match when any of them matches;
    /// <c>!</c> before a pattern excludes what it matches (<c>*.log;!debug*</c>).
    /// </summary>
    /// <remarks>
    /// <c>*</c> matches any run of characters, <c>?</c> one character, <c>[abc]</c> or <c>[a-z]</c> one character from
    /// the set and <c>[!abc]</c> one character outside it. A backslash escapes the next character only where it is not a
    /// path separator, so Windows paths are never patterns. Matching is linear in the text and never compiles a regular
    /// expression.
    /// </remarks>
    public sealed class GlobPattern {
        private readonly string[] _include;
        private readonly string[] _exclude;

        /// <summary>Creates a pattern.</summary>
        /// <param name="pattern">One or more patterns separated by <c>;</c> or <c>,</c>.</param>
        /// <param name="caseSensitive">Whether letters must match case exactly; names are compared case-insensitively by default.</param>
        public GlobPattern(string pattern, bool caseSensitive = false) {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            CaseSensitive = caseSensitive;
            List<string> include = new List<string>();
            List<string> exclude = new List<string>();
            foreach (string part in pattern.Split(';', ',')) {
                string trimmed = part.Trim();
                if (trimmed.Length == 0) { continue; }
                if (trimmed[0] == '!' && trimmed.Length > 1) { exclude.Add(trimmed.Substring(1)); } else { include.Add(trimmed); }
            }
            // Only exclusions (!*.tmp) mean "everything except".
            if (include.Count == 0 && exclude.Count != 0) { include.Add("*"); }
            _include = include.ToArray();
            _exclude = exclude.ToArray();
        }

        /// <summary>The pattern text as given.</summary>
        public string Pattern { get; }

        /// <summary>Whether letters must match case exactly.</summary>
        public bool CaseSensitive { get; }

        /// <summary>Whether a name matches.</summary>
        /// <param name="text">The name, usually a file name without its folder.</param>
        /// <returns>True when an included pattern matches and no excluded one does.</returns>
        public bool IsMatch(string? text) {
            if (text == null) { return false; }
            bool matched = false;
            foreach (string pattern in _include) {
                if (Match(pattern, text, CaseSensitive)) { matched = true; break; }
            }
            if (!matched) { return false; }
            foreach (string pattern in _exclude) {
                if (Match(pattern, text, CaseSensitive)) { return false; }
            }
            return true;
        }

        /// <summary>Whether a name matches a pattern; see <see cref="GlobPattern"/>.</summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="text">The name.</param>
        /// <param name="caseSensitive">Whether letters must match case exactly.</param>
        /// <returns>True on a match.</returns>
        public static bool IsMatch(string pattern, string? text, bool caseSensitive = false) => new GlobPattern(pattern, caseSensitive).IsMatch(text);

        /// <inheritdoc />
        public override string ToString() => Pattern;

        // Iterative matcher with single-star backtracking: O(pattern × text) worst case, no recursion.
        private static bool Match(string pattern, string text, bool caseSensitive) {
            int p = 0, t = 0, starP = -1, starT = 0;
            while (t < text.Length) {
                if (p < pattern.Length) {
                    char c = pattern[p];
                    if (c == '*') {
                        while (p < pattern.Length && pattern[p] == '*') { p++; }
                        if (p == pattern.Length) { return true; }
                        starP = p; starT = t;
                        continue;
                    }
                    if (c == '?') { p++; t++; continue; }
                    if (c == '[' && TryClass(pattern, p, text[t], caseSensitive, out int next, out bool inClass)) {
                        if (inClass) { p = next; t++; continue; }
                    } else {
                        if (c == '\\' && p + 1 < pattern.Length && !IsSeparator(pattern[p + 1])) { p++; c = pattern[p]; }
                        if (Same(c, text[t], caseSensitive)) { p++; t++; continue; }
                    }
                }
                if (starP < 0) { return false; }
                p = starP; t = ++starT;
            }
            while (p < pattern.Length && pattern[p] == '*') { p++; }
            return p == pattern.Length;
        }

        private static bool IsSeparator(char c) => c == '\\' || c == '/';

        // A class is literal text when it has no closing bracket, as in shells.
        private static bool TryClass(string pattern, int start, char value, bool caseSensitive, out int next, out bool matched) {
            next = start; matched = false;
            int i = start + 1;
            bool negate = i < pattern.Length && (pattern[i] == '!' || pattern[i] == '^');
            if (negate) { i++; }
            int first = i;
            bool any = false;
            while (i < pattern.Length && (pattern[i] != ']' || i == first)) {
                char low = pattern[i];
                if (i + 2 < pattern.Length && pattern[i + 1] == '-' && pattern[i + 2] != ']') {
                    char high = pattern[i + 2];
                    if (InRange(value, low, high) || !caseSensitive && (InRange(char.ToLowerInvariant(value), low, high) || InRange(char.ToUpperInvariant(value), low, high))) { any = true; }
                    i += 3;
                } else {
                    if (Same(low, value, caseSensitive)) { any = true; }
                    i++;
                }
            }
            if (i >= pattern.Length) { return false; }
            next = i + 1;
            matched = any != negate;
            return true;
        }

        private static bool InRange(char value, char low, char high) => value >= low && value <= high;

        private static bool Same(char left, char right, bool caseSensitive) => left == right || !caseSensitive && char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
    }
}
