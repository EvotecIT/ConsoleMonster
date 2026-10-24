using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    /// <summary>Redacts complete output records; never persists partial secret-bearing tokens.</summary>
    internal sealed partial class RecordingRedactor {
        private static readonly Regex Ansi = new Regex(@"\x1b(?:\[[0-?]*[ -/]*[@-~]|\][^\a]*?(?:\a|\x1b\\)|[PX^_][\s\S]*?\x1b\\|(?![\[\]PX^_])[ -/]*[0-~])", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private readonly List<Regex> _patterns = new List<Regex>();
        private readonly string _replacement;
        public RecordingRedactor(IEnumerable<string> patterns, string replacement = "[REDACTED]") {
            _replacement = replacement;
            Add(@"(?i)\bbearer\s+[^\s;""'\x00-\x1f]+");
            Add(@"(?i)\b(?:password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret|token)\s*[=:]\s*(?:""[^""\x00-\x1f]*(?:""|(?=[\x00-\x1f]|\z))|'[^'\x00-\x1f]*(?:'|(?=[\x00-\x1f]|\z))|[^;""'\x00-\x1f]+)");
            Add(@"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+|sk-[A-Za-z0-9_-]{8,}|AKIA[A-Z0-9]{16})\b");
            foreach (string pattern in patterns) { ConfigureStreaming(pattern); Add(pattern); }
        }
        private void Add(string pattern) { _patterns.Add(new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))); }
        public string Apply(string text) => ApplyWithOffsets(text).Text;
        public RedactedOutput ApplyWithOffsets(string text) => ApplyPrefix(text, text.Length);

        /// <summary>Redacts the complete lookahead, committing only a prefix that cannot split a replacement.</summary>
        internal RedactedOutput ApplyPrefix(string text, int sourceLength, int startAt = 0) {
            try {
                MatchCollection controls = Ansi.Matches(text);
                StringBuilder visible = new StringBuilder();
                List<int> positions = new List<int>();
                int controlIndex = 0;
                for (int i = 0; i < text.Length; i++) {
                    if (controlIndex < controls.Count && i == controls[controlIndex].Index) {
                        i += controls[controlIndex++].Length - 1; continue;
                    }
                    visible.Append(text[i]); positions.Add(i);
                }
                string plain = visible.ToString();
                int visibleStart = 0;
                while (visibleStart < positions.Count && positions[visibleStart] < startAt) { visibleStart++; }
                bool[] masked = new bool[text.Length];
                List<SourceRange> ranges = new List<SourceRange>();
                // Retained context informs word boundaries but is never a new match start.
                // Every rule sees the original output. Replacement labels must not shorten a later
                // rule's input or hide an overlapping secret from another configured rule.
                foreach (Regex pattern in _patterns) {
                    foreach (Match match in pattern.Matches(plain, visibleStart)) {
                        if (match.Length == 0) { continue; }
                        ranges.Add(new SourceRange(positions[match.Index], positions[match.Index + match.Length - 1] + 1));
                        for (int i = match.Index; i < match.Index + match.Length; i++) { masked[positions[i]] = true; }
                    }
                    // Also cover secrets inside non-printing controls, such as a window title.
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
