using System;
using System.Text;
using System.Globalization;

namespace ConsoleMonster.Demos {
    /// <summary>Incremental OSC 133 decoder. Accepts BEL and ST terminators across arbitrary output chunks.</summary>
    public sealed class SemanticPromptDetector {
        private readonly StringBuilder _sequence = new StringBuilder();
        private bool _escape;
        private bool _osc;
        private bool _prompt;
        /// <summary>Number of complete prompt-start/prompt-end pairs observed.</summary>
        public long PromptCount { get; private set; }
        /// <summary>Last command status supplied by OSC 133 D, or null when it was omitted.</summary>
        public int? LastExitCode { get; private set; }
        /// <summary>Processes terminal output without matching any visible prompt text.</summary>
        public void Feed(string text) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            foreach (char c in text) {
                if (_osc) {
                    if (c == '\a' || (_escape && c == '\\')) {
                        string value = _sequence.ToString();
                        if (value == "133;A" || value.StartsWith("133;A;", StringComparison.Ordinal)) { _prompt = true; }
                        else if ((value == "133;B" || value.StartsWith("133;B;", StringComparison.Ordinal)) && _prompt) { PromptCount++; _prompt = false; }
                        else if (value == "133;C") { _prompt = false; LastExitCode = null; }
                        else if (value == "133;D" || value.StartsWith("133;D;", StringComparison.Ordinal)) {
                            _prompt = false;
                            string[] fields = value.Split(';');
                            LastExitCode = fields.Length > 2 && int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code) ? (int?)code : null;
                        }
                        _osc = _escape = false; _sequence.Clear();
                    } else if (c == '\u001b') { _escape = true; }
                    else {
                        if (_escape) { _osc = false; _sequence.Clear(); _escape = false; }
                        else if (_sequence.Length < 4096) { _sequence.Append(c); }
                        else { _osc = false; _sequence.Clear(); }
                    }
                } else if (_escape && c == ']') { _osc = true; _escape = false; _sequence.Clear(); }
                else { _escape = c == '\u001b'; }
            }
        }
    }
}
