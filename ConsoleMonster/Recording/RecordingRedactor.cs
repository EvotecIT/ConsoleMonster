using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    /// <summary>Redacts complete output records; never persists partial secret-bearing tokens.</summary>
    internal sealed class RecordingRedactor {
        private static readonly Regex Ansi = new Regex(@"\x1b(?:\[[0-?]*[ -/]*[@-~]|\][^\a]*(?:\a|\x1b\\))", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private readonly List<Regex> _patterns = new List<Regex>();
        public RecordingRedactor(IEnumerable<string> patterns) {
            Add(@"(?i)\b(?:bearer\s+|(?:password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret|token)\s*[=:]\s*)(?:""[^""\x00-\x1f]*""|'[^'\x00-\x1f]*'|[^\s;""'\x00-\x1f]+)");
            Add(@"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+|sk-[A-Za-z0-9_-]{8,}|AKIA[A-Z0-9]{16})\b");
            foreach (string pattern in patterns) { Add(pattern); }
        }
        private void Add(string pattern) { _patterns.Add(new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))); }
        public string Apply(string text) {
            try {
                foreach (Regex pattern in _patterns) {
                    text = RedactVisible(text, pattern);
                    // Also cover secrets inside non-printing controls, such as a window title.
                    text = pattern.Replace(text, "[REDACTED]");
                }
                return text;
            } catch (RegexMatchTimeoutException) {
                return "[REDACTED: pattern timeout]";
            }
        }
        private static string RedactVisible(string text, Regex pattern) {
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
                StringBuilder keepControls = new StringBuilder();
                foreach (Match control in Ansi.Matches(text.Substring(start, end - start))) { keepControls.Append(control.Value); }
                text = text.Substring(0, start) + "[REDACTED]" + keepControls + text.Substring(end);
            }
            return text;
        }
    }
}
