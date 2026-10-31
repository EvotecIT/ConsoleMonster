using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        // Context-sensitive or unbounded regexes require complete records; never guess their overlap.
        internal const int MaximumPatternWidth = 8192;
        internal bool RequiresCompleteRecord { get; private set; }
        private int _overlap = 32; // More than the longest built-in key/prefix, including its word boundary.

        private void ConfigureStreaming(string pattern) {
            if (pattern == null) { throw new ArgumentNullException(nameof(pattern)); }
            int? length = PatternWidth.Read(pattern);
            if (!length.HasValue) { RequiresCompleteRecord = true; }
            else { _overlap = Math.Max(_overlap, length.Value + 1); }
        }

        /// <summary>Keeps visible and raw pattern lookahead, incomplete controls and unfinished built-in credentials.</summary>
        internal int PrefixLength(string text) {
            int boundary = text.Length;
            foreach (TerminalInterpretation interpretation in Interpretations) {
                boundary = Math.Min(boundary, ProjectionPrefixLength(text, interpretation));
            }
            if (boundary == 0) { return 0; }
            int pending = Math.Min(PendingCredentialStart(text, PendingRunPatterns), PendingCredentialStart(text, PendingWordPatterns));
            return Math.Min(boundary, pending);
        }

        private int ProjectionPrefixLength(string text, TerminalInterpretation interpretation) {
            string visible = VisibleText(text, stopAtIncomplete: true, out List<int> positions, out List<SourceRange> controls, interpretation);
            if (positions.Count <= _overlap) { return 0; }
            int boundary = Math.Min(positions[positions.Count - _overlap], text.Length - _overlap);
            foreach (SourceRange control in controls) {
                if (control.Start < boundary && control.End > boundary) { boundary = control.Start; }
            }
            int pending = PendingCredentialStart(visible, PendingWordPatterns);
            if (pending < visible.Length) { boundary = Math.Min(boundary, positions[pending]); }
            // An unfinished key/value is pending only within the visible run that ends the record.
            string runs = VisibleText(text, stopAtIncomplete: true, out List<int> runPositions, out _, interpretation, runBreaks: true);
            pending = PendingCredentialStart(runs, PendingRunPatterns);
            if (pending < runs.Length) { boundary = Math.Min(boundary, runPositions[pending]); }
            if (boundary > 0 && char.IsHighSurrogate(text[boundary - 1]) && char.IsLowSurrogate(text[boundary])) { boundary--; }
            return boundary;
        }

        /// <summary>Finds the earliest unfinished credential (or the text length) while keeping each rule's bounded search intact.</summary>
        private static int PendingCredentialStart(string text, Regex[] patterns) {
            int start = text.Length;
            foreach (Regex pattern in patterns) {
                Match pending = pattern.Match(text);
                if (pending.Success) { start = Math.Min(start, pending.Index); }
                if (start == 0) { break; }
            }
            return start;
        }

        /// <summary>Retains a previous visible character and following controls for word-boundary context.</summary>
        internal static int ContextStart(string text, int consumed) {
            string prefix = text.Substring(0, consumed);
            int start = consumed;
            foreach (TerminalInterpretation interpretation in Interpretations) {
                start = Math.Min(start, ProjectionContextStart(prefix, interpretation));
            }
            return start;
        }

        private static int ProjectionContextStart(string prefix, TerminalInterpretation interpretation) {
            List<SourceRange> controls = TerminalControls(prefix, out _, interpretation);
            int lastVisible = -1, lastControl = prefix.Length, controlIndex = 0;
            for (int i = 0; i < prefix.Length; i++) {
                if (controlIndex < controls.Count && i == controls[controlIndex].Start) {
                    lastControl = i; i = controls[controlIndex++].End - 1;
                } else { lastVisible = i; }
            }
            int start = lastVisible >= 0 ? lastVisible : lastControl;
            if (start > 0 && start < prefix.Length && char.IsLowSurrogate(prefix[start]) && char.IsHighSurrogate(prefix[start - 1])) { start--; }
            return start;
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
