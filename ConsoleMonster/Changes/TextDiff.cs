using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleMonster.Changes {
    /// <summary>Dependency-free text comparison shared by object previews, text widgets and unified output.</summary>
    public static class TextDiff {
        /// <summary>
        /// Compares lines ordinally using linear auxiliary memory and a bounded comparison budget. For very different
        /// large ranges the result remains exact as an edit script, but may contain more edits than a minimal diff.
        /// CRLF, LF and CR endings are equivalent; the presence of the final newline is significant.
        /// </summary>
        /// <param name="referenceText">The original text; null is empty.</param>
        /// <param name="differenceText">The new text; null is empty.</param>
        /// <param name="options">Comparison and presentation settings, copied into the result.</param>
        /// <returns>A reusable comparison, with original text and line numbers.</returns>
        public static TextDiffResult Compare(string? referenceText, string? differenceText, TextDiffOptions? options = null) {
            options ??= new TextDiffOptions();
            string a = NormalizeEndings(referenceText);
            string b = NormalizeEndings(differenceText);
            string[] before = Lines(a);
            string[] after = Lines(b);
            string[] keysA = Keys(before, a.EndsWith("\n", StringComparison.Ordinal), options.IgnoreWhitespace);
            string[] keysB = Keys(after, b.EndsWith("\n", StringComparison.Ordinal), options.IgnoreWhitespace);
            List<TextDiffLine> lines = new List<TextDiffLine>();
            int x = 0;
            int y = 0;
            foreach (ChangeKind kind in TextSequenceDiff.Compare(keysA, keysB)) {
                bool added = kind == ChangeKind.Added;
                int index = added ? y : x;
                string[] source = added ? after : before;
                bool ending = index < source.Length - 1 || (added ? b : a).EndsWith("\n", StringComparison.Ordinal);
                lines.Add(new TextDiffLine(kind, source[index], added ? (int?)null : x + 1,
                    kind == ChangeKind.Removed ? (int?)null : y + 1, ending));
                if (kind != ChangeKind.Added) { x++; }
                if (kind != ChangeKind.Removed) { y++; }
            }
            return new TextDiffResult(lines.AsReadOnly(), options);
        }

        private static string NormalizeEndings(string? text) => (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');

        private static string[] Lines(string text) {
            if (text.Length == 0) { return Array.Empty<string>(); }
            string[] lines = text.Split('\n');
            return text.EndsWith("\n", StringComparison.Ordinal) ? lines.Take(lines.Length - 1).ToArray() : lines;
        }

        private static string[] Keys(string[] lines, bool endsWithNewline, bool ignoreWhitespace) {
            string[] keys = new string[lines.Length];
            for (int i = 0; i < keys.Length; i++) {
                string text = lines[i];
                if (ignoreWhitespace) {
                    StringBuilder key = new StringBuilder(text.Length);
                    foreach (char c in text) {
                        if (!char.IsWhiteSpace(c)) { key.Append(c); }
                    }
                    text = key.ToString();
                }
                keys[i] = text + (i < keys.Length - 1 || endsWithNewline ? "\n" : string.Empty);
            }
            return keys;
        }
    }
}
