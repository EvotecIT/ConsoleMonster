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
        private bool _collectOutput;
        private bool _commandSeen;
        internal void BeginInput() { _collectOutput = _commandSeen = false; }
        /// <summary>Number of complete prompt-start/prompt-end pairs observed.</summary>
        public long PromptCount { get; private set; }
        /// <summary>Last command status supplied by OSC 133 D, or null when it was omitted.</summary>
        public int? LastExitCode { get; private set; }
        /// <summary>Processes terminal output without matching any visible prompt text.</summary>
        public void Feed(string text) => Feed(text, null, null);
        // Input echo and the visible prompt are outside the semantic command-output region.
        internal void Feed(string text, Action<char>? output, Action? commandStarted) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            foreach (char c in text) {
                if (_osc) {
                    if (c == '\a' || (_escape && c == '\\')) {
                        string value = _sequence.ToString();
                        if (value == "133;A" || value.StartsWith("133;A;", StringComparison.Ordinal)) { _prompt = true; _collectOutput = false; }
                        else if ((value == "133;B" || value.StartsWith("133;B;", StringComparison.Ordinal)) && _prompt) { PromptCount++; _prompt = false; _collectOutput = _commandSeen; }
                        else if (value == "133;C") { _prompt = false; LastExitCode = null; _collectOutput = _commandSeen = true; commandStarted?.Invoke(); }
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
                else if (c == '\u001b') { _escape = true; }
                else {
                    if (_collectOutput) { if (_escape) { output?.Invoke('\u001b'); } output?.Invoke(c); }
                    _escape = false;
                }
            }
        }
    }
}
