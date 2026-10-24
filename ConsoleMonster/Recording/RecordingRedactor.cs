using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    /// <summary>Redacts complete output records; never persists partial secret-bearing tokens.</summary>
    internal sealed class RecordingRedactor {
        private static readonly Regex Ansi = new Regex(@"\x1b(?:\[[0-?]*[ -/]*[@-~]|\][^\a]*(?:\a|\x1b\\))", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private readonly List<Regex> _patterns = new List<Regex>();
        private readonly string _replacement;
        public RecordingRedactor(IEnumerable<string> patterns, string replacement = "[REDACTED]") {
            _replacement = replacement;
            Add(@"(?i)\bbearer\s+[^\s;""'\x00-\x1f]+");
            Add(@"(?i)\b(?:password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret|token)\s*[=:]\s*(?:""[^""\x00-\x1f]*""|'[^'\x00-\x1f]*'|[^;""'\x00-\x1f]+)");
            Add(@"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+|sk-[A-Za-z0-9_-]{8,}|AKIA[A-Z0-9]{16})\b");
            foreach (string pattern in patterns) { Add(pattern); }
        }
        private void Add(string pattern) { _patterns.Add(new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))); }
        public string Apply(string text) => ApplyWithOffsets(text).Text;
        public RedactedOutput ApplyWithOffsets(string text) {
            List<int> offsets = new List<int>(text.Length);
            for (int i = 0; i < text.Length; i++) { offsets.Add(i); }
            try {
                foreach (Regex pattern in _patterns) {
                    text = RedactVisible(text, pattern, offsets);
                    // Also cover secrets inside non-printing controls, such as a window title.
                    MatchCollection raw = pattern.Matches(text);
                    for (int i = raw.Count - 1; i >= 0; i--) {
                        if (raw[i].Length > 0) { text = Replace(text, raw[i].Index, raw[i].Length, offsets, false); }
                    }
                }
                return new RedactedOutput(text, offsets);
            } catch (RegexMatchTimeoutException) {
                const string masked = "[REDACTED: pattern timeout]";
                return new RedactedOutput(masked, new List<int>(new int[masked.Length]));
            }
        }
        private string RedactVisible(string text, Regex pattern, List<int> offsets) {
            MatchCollection controls = Ansi.Matches(text);
            StringBuilder visible = new StringBuilder();
            List<int> positions = new List<int>();
            int controlIndex = 0;
            for (int i = 0; i < text.Length; i++) {
                if (controlIndex < controls.Count && i == controls[controlIndex].Index) { i += controls[controlIndex++].Length - 1; continue; }
                visible.Append(text[i]); positions.Add(i);
            }
            MatchCollection matches = pattern.Matches(visible.ToString());
            for (int i = matches.Count - 1; i >= 0; i--) {
                Match match = matches[i];
                if (match.Length == 0) { continue; }
                int start = positions[match.Index], end = positions[match.Index + match.Length - 1] + 1;
                text = Replace(text, start, end - start, offsets, true);
            }
            return text;
        }
        private string Replace(string text, int start, int length, List<int> offsets, bool keepControls) {
            StringBuilder replacement = new StringBuilder(_replacement);
            List<int> replacementOffsets = new List<int>();
            for (int i = 0; i < replacement.Length; i++) { replacementOffsets.Add(offsets[start]); }
            if (keepControls) {
                foreach (Match control in Ansi.Matches(text.Substring(start, length))) {
                    replacement.Append(control.Value);
                    for (int i = 0; i < control.Length; i++) { replacementOffsets.Add(offsets[start + control.Index + i]); }
                }
            }
            offsets.RemoveRange(start, length);
            offsets.InsertRange(start, replacementOffsets);
            return text.Substring(0, start) + replacement + text.Substring(start + length);
        }
    }

    /// <summary>Sanitized text with source offsets, so redaction does not discard output arrival times.</summary>
    internal sealed class RedactedOutput {
        public RedactedOutput(string text, List<int> offsets) { Text = text; Offsets = offsets; }
        public string Text { get; }
        public List<int> Offsets { get; }
    }
}
