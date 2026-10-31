using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    /// <summary>Redacts complete output records; never persists partial secret-bearing tokens.</summary>
    internal sealed partial class RecordingRedactor {
        // Reuse callers' compiled rules without keeping session-specific literal secrets alive.
        private static readonly ConditionalWeakTable<string, CustomPatternSlot> CustomPatterns = new ConditionalWeakTable<string, CustomPatternSlot>();
        private readonly List<Regex> _patterns = new List<Regex>();
        private readonly string _replacement;
        public RecordingRedactor(IEnumerable<string> patterns, string replacement = "[REDACTED]") {
            _replacement = replacement;
            _patterns.AddRange(BuiltInPatterns);
            foreach (string pattern in patterns) { ConfigureStreaming(pattern); Add(pattern); }
        }
        private void Add(string pattern) { _patterns.Add(CustomPatterns.GetValue(pattern, CreateCustomPatternSlot).Get(pattern)); }
        private static CustomPatternSlot CreateCustomPatternSlot(string pattern) => new CustomPatternSlot();
        public string Apply(string text) => ApplyWithOffsets(text).Text;

        /// <summary>The ranges of plain visible text (no control sequences) that hold a secret, for masking screen cells.</summary>
        internal List<KeyValuePair<int, int>> MatchRanges(string plain) {
            List<KeyValuePair<int, int>> ranges = new List<KeyValuePair<int, int>>();
            try {
                foreach (Regex pattern in _patterns) {
                    foreach (Match match in pattern.Matches(plain)) {
                        if (match.Length > 0) { ranges.Add(new KeyValuePair<int, int>(match.Index, match.Length)); }
                    }
                }
            } catch (RegexMatchTimeoutException) {
                ranges.Clear();
                ranges.Add(new KeyValuePair<int, int>(0, plain.Length));
            }
            return ranges;
        }
        public RedactedOutput ApplyWithOffsets(string text) => ApplyPrefix(text, text.Length);

        /// <summary>Redacts the complete lookahead, committing only a prefix that cannot split a replacement.</summary>
        internal RedactedOutput ApplyPrefix(string text, int sourceLength, int startAt = 0, IReadOnlyList<bool>? rowChanges = null) {
            try {
                bool[] masked = new bool[text.Length];
                List<SourceRange> ranges = new List<SourceRange>();
                MaskProjections(text, startAt, runBreaks: false, _patterns, masked, ranges, rowChanges);
                MaskProjections(text, startAt, runBreaks: true, RunPatterns, masked, ranges, rowChanges);
                foreach (Regex pattern in AllPatterns()) {
                    foreach (Match match in pattern.Matches(text, startAt)) {
                        if (match.Length == 0) { continue; }
                        ranges.Add(new SourceRange(match.Index, match.Index + match.Length));
                        for (int i = match.Index; i < match.Index + match.Length; i++) { masked[i] = true; }
                    }
                }
                List<SourceRange> merged = MergeRanges(ranges);
                int boundary = CommitBoundary(sourceLength, merged);
                StringBuilder result = new StringBuilder(boundary);
                List<int> offsets = new List<int>(boundary);
                int rangeIndex = 0;
                for (int i = startAt; i < boundary; i++) {
                    while (rangeIndex < merged.Count && merged[rangeIndex].End <= i) { rangeIndex++; }
                    if (rangeIndex < merged.Count && merged[rangeIndex].Start == i) {
                        result.Append(_replacement);
                        for (int j = 0; j < _replacement.Length; j++) { offsets.Add(i); }
                    }
                    if (!masked[i]) { result.Append(text[i]); offsets.Add(i); }
                }
                return new RedactedOutput(result.ToString(), offsets, boundary);
            } catch (RegexMatchTimeoutException) {
                const string masked = "[REDACTED: pattern timeout]";
                return new RedactedOutput(masked, new List<int>(new int[masked.Length]), text.Length, timedOut: true);
            }
        }

        private IEnumerable<Regex> AllPatterns() {
            foreach (Regex pattern in RunPatterns) { yield return pattern; }
            foreach (Regex pattern in _patterns) { yield return pattern; }
        }

        private static void MaskProjections(string text, int startAt, bool runBreaks, IEnumerable<Regex> patterns, bool[] masked, List<SourceRange> ranges, IReadOnlyList<bool>? rowChanges) {
            List<List<int>> projections = new List<List<int>>();
            // Native terminal OSC/DCS handling and VtScreen replay differ for unexpected ESCs.
            // Protect credentials under both interpretations, as well as raw control payloads.
            // Every rule sees the original output, never earlier replacement labels.
            foreach (TerminalInterpretation interpretation in Interpretations) {
                string plain = VisibleText(text, stopAtIncomplete: false, out List<int> positions, out _, interpretation, runBreaks, rowChanges);
                // Equal text alone is insufficient: identical glyphs may have different source offsets.
                if (projections.Exists(previous => SamePositions(previous, positions))) { continue; }
                projections.Add(positions);
                int visibleStart = 0;
                while (visibleStart < positions.Count && positions[visibleStart] < startAt) { visibleStart++; }
                foreach (Regex pattern in patterns) {
                    foreach (Match match in pattern.Matches(plain, visibleStart)) {
                        int first = -1, last = -1;
                        for (int i = match.Index; i < match.Index + match.Length; i++) {
                            if (IsRunBreak(text, plain, positions, i)) { continue; }
                            masked[positions[i]] = true;
                            if (first < 0) { first = positions[i]; }
                            last = positions[i];
                        }
                        if (first >= 0) { ranges.Add(new SourceRange(first, last + 1)); }
                    }
                }
            }
        }

        private static bool SamePositions(List<int> first, List<int> second) {
            if (first.Count != second.Count) { return false; }
            for (int i = 0; i < first.Count; i++) { if (first[i] != second[i]) { return false; } }
            return true;
        }

        private static List<SourceRange> MergeRanges(List<SourceRange> ranges) {
            ranges.Sort((a, b) => a.Start.CompareTo(b.Start));
            List<SourceRange> merged = new List<SourceRange>();
            foreach (SourceRange range in ranges) {
                if (merged.Count == 0 || range.Start >= merged[merged.Count - 1].End) { merged.Add(range); }
                else {
                    SourceRange previous = merged[merged.Count - 1];
                    merged[merged.Count - 1] = new SourceRange(previous.Start, Math.Max(previous.End, range.End));
                }
            }
            return merged;
        }
    }

    /// <summary>Sanitized text with source offsets, so redaction does not discard output arrival times.</summary>
    internal sealed class RedactedOutput {
        public RedactedOutput(string text, List<int> offsets, int sourceLength, bool timedOut = false) {
            Text = text; Offsets = offsets; SourceLength = sourceLength; TimedOut = timedOut;
        }
        public string Text { get; }
        public List<int> Offsets { get; }
        public int SourceLength { get; }
        public bool TimedOut { get; }
    }
}
