using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        // Context-sensitive or unbounded regexes require complete records; never guess their overlap.
        internal const int MaximumPatternWidth = 8192;
        internal bool RequiresCompleteRecord { get; private set; }
        private int _overlap = 32; // More than the longest built-in key/prefix, including its word boundary.

        // An unfinished built-in credential must remain private even when its value/whitespace is arbitrarily long.
        private static readonly Regex PendingCredential = new Regex(
            @"(?i)\b(?:bearer(?:\s+[^\s;""'\x00-\x1f]*)?|(?:password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret|token)(?:\s*(?:[=:]\s*(?:""[^""\x00-\x1f]*|'[^'\x00-\x1f]*|[^;""'\x00-\x1f]*))?)?|gh[pousr]_[A-Za-z0-9_]*|github_pat_[A-Za-z0-9_]*|sk-[A-Za-z0-9_-]*)\z",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        private void ConfigureStreaming(string pattern) {
            if (pattern == null) { throw new ArgumentNullException(nameof(pattern)); }
            int? length = PatternWidth.Read(pattern);
            if (!length.HasValue) { RequiresCompleteRecord = true; }
            else { _overlap = Math.Max(_overlap, length.Value + 1); }
        }

        /// <summary>Keeps visible and raw pattern lookahead, incomplete controls and unfinished built-in credentials.</summary>
        internal int PrefixLength(string text) {
            MatchCollection controls = Ansi.Matches(text);
            List<int> positions = new List<int>();
            StringBuilder visible = new StringBuilder();
            int controlIndex = 0;
            for (int i = 0; i < text.Length; i++) {
                if (controlIndex < controls.Count && i == controls[controlIndex].Index) {
                    i += controls[controlIndex++].Length - 1; continue;
                }
                if (IncompleteControl(text, i)) { break; }
                positions.Add(i); visible.Append(text[i]);
            }
            if (positions.Count <= _overlap) { return 0; }
            int boundary = Math.Min(positions[positions.Count - _overlap], text.Length - _overlap);
            foreach (Match control in controls) {
                if (control.Index < boundary && control.Index + control.Length > boundary) { boundary = control.Index; }
            }
            Match pending = PendingCredential.Match(visible.ToString());
            if (pending.Success) { boundary = Math.Min(boundary, positions[pending.Index]); }
            pending = PendingCredential.Match(text);
            if (pending.Success) { boundary = Math.Min(boundary, pending.Index); }
            if (boundary > 0 && char.IsHighSurrogate(text[boundary - 1]) && char.IsLowSurrogate(text[boundary])) { boundary--; }
            return boundary;
        }

        /// <summary>Retains a previous visible character and following controls for word-boundary context.</summary>
        internal static int ContextStart(string text, int consumed) {
            string prefix = text.Substring(0, consumed);
            MatchCollection controls = Ansi.Matches(prefix);
            int lastVisible = -1, lastControl = consumed, controlIndex = 0;
            for (int i = 0; i < prefix.Length; i++) {
                if (controlIndex < controls.Count && i == controls[controlIndex].Index) {
                    lastControl = i; i += controls[controlIndex++].Length - 1;
                } else { lastVisible = i; }
            }
            int start = lastVisible >= 0 ? lastVisible : lastControl;
            if (start > 0 && start < prefix.Length && char.IsLowSurrogate(prefix[start]) && char.IsHighSurrogate(prefix[start - 1])) { start--; }
            return start;
        }

        private static bool IncompleteControl(string text, int at) {
            if (text[at] != '\u001b') { return false; }
            if (at + 1 == text.Length || "]PX^_".IndexOf(text[at + 1]) >= 0) { return true; }
            bool csi = text[at + 1] == '[';
            if (!csi && (text[at + 1] < 0x20 || text[at + 1] > 0x2f)) { return false; }
            for (int i = at + (csi ? 2 : 1); i < text.Length; i++) {
                if (text[i] < 0x20 || text[i] > (csi ? 0x3f : 0x2f)) { return false; }
            }
            return true;
        }

        private static int CommitBoundary(int boundary, List<SourceRange> ranges) {
            foreach (SourceRange range in ranges) {
                if (range.Start < boundary && range.End > boundary) { return range.Start; }
            }
            return boundary;
        }

        private readonly struct SourceRange {
            internal SourceRange(int start, int end) { Start = start; End = end; }
            internal int Start { get; }
            internal int End { get; }
        }
    }
}
