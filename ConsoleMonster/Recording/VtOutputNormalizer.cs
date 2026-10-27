using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Sessions {
    /// <summary>Opt-in canonical encoding of terminal output without changing its visible cells or repaint boundaries.
    /// Removes window titles, cursor visibility/focus/input-mode housekeeping and cursor queries, combines supported SGR
    /// changes, and omits already-identical blank-cell writes immediately before a newline that returns to column one.
    /// Unknown SGR is rejected rather than silently discarded. Other terminal controls and OSC hyperlinks are retained.</summary>
    public sealed class VtOutputNormalizer {
        private readonly VtScreen _styles;
        private Style _emitted;

        /// <summary>Creates a normalizer with the recorded terminal's fixed geometry.</summary>
        /// <param name="width">Terminal columns.</param>
        /// <param name="height">Terminal rows.</param>
        public VtOutputNormalizer(int width, int height) {
            if (width < 1 || width > 4096) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height < 1 || height > 4096 || (long)width * height > 4_194_304) { throw new ArgumentOutOfRangeException(nameof(height)); }
            _styles = new VtScreen(width, height, keepScrollback: false);
        }

        /// <summary>Normalizes a complete VT segment. Successive calls retain the active SGR state.
        /// Segments must end outside escape sequences. Set <paramref name="textLineEndings"/> only for append-only
        /// text output whose LF means a new line; ordinary VT LF retains its column.</summary>
        /// <param name="text">Actual recorded output, including complete terminal controls.</param>
        /// <param name="textLineEndings">Whether bare LF is a text newline and should become CRLF.</param>
        /// <returns>Canonical VT, with real text, measurements, geometry and frame boundaries preserved.</returns>
        public string Normalize(string text, bool textLineEndings = false) {
            if (text == null) { throw new ArgumentNullException(nameof(text)); }
            var result = new StringBuilder(text.Length);
            var blanks = new List<Style>();
            void EmitStyle(Style style) {
                // Links are encoded by their original OSC controls, independently of SGR.
                style = style.WithLink(null);
                if (_emitted != style) { SgrEncoder.Append(result, style, ColorDepth.TrueColor); _emitted = style; }
            }
            void FlushBlanks() {
                foreach (Style style in blanks) { EmitStyle(style); result.Append(' '); }
                blanks.Clear();
            }
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (c == '\u001b') {
                    int start = i;
                    do { _styles.Feed(text[i].ToString()); i++; }
                    while (i < text.Length && _styles.HasIncompleteSequence);
                    if (_styles.HasIncompleteSequence) { throw new FormatException("Normalization requires complete VT sequences."); }
                    string control = text.Substring(start, i - start); i--;
                    if (control.StartsWith("\u001b[", StringComparison.Ordinal) && control.EndsWith("m", StringComparison.Ordinal)) {
                        if (!SupportedSgr(control)) { throw new FormatException("The recording contains SGR outside the supported cell model."); }
                        continue;
                    }
                    if (control.StartsWith("\u001b]0;", StringComparison.Ordinal) || control.StartsWith("\u001b]2;", StringComparison.Ordinal)
                        || control == "\u001b[6n" || control == "\u001b[?25h" || control == "\u001b[?25l"
                        || control == "\u001b[?9001h" || control == "\u001b[?9001l" || control == "\u001b[?1004h" || control == "\u001b[?1004l") { continue; }
                    FlushBlanks();
                    // Cursor moves and synchronization do not paint; erases and unknown controls may depend on SGR.
                    bool cursor = control.StartsWith("\u001b[", StringComparison.Ordinal)
                        && "HfABCDEFGd`".IndexOf(control[control.Length - 1]) >= 0;
                    bool synchronized = control == "\u001b[?2026h" || control == "\u001b[?2026l";
                    if (!cursor && !synchronized) { EmitStyle(_styles.ActiveStyle); }
                    result.Append(control);
                    if (control == "\u001bc") { _emitted = Style.Plain; }
                    continue;
                }
                Style active = _styles.ActiveStyle;
                bool blank = c == ' ' && _styles.CursorOnMatchingBlank && active.BackgroundColor.IsDefault
                    && active.Decoration == TextDecoration.None && active.Link == null;
                _styles.Feed(c.ToString());
                if (blank) { blanks.Add(active); continue; }
                bool crlf = c == '\r' && i + 1 < text.Length && text[i + 1] == '\n';
                if (crlf || (c == '\n' && textLineEndings)) {
                    blanks.Clear();
                    // Scrolling/erasure at LF uses the active background, even without printable text.
                    if (_emitted.BackgroundColor != active.BackgroundColor) { EmitStyle(active); }
                    result.Append("\r\n");
                    if (crlf) { _styles.Feed("\n"); i++; }
                    else {
                        // The input was append-only text: its LF also returns to column one.
                        _styles.Feed("\r");
                    }
                } else {
                    FlushBlanks();
                    if (!char.IsControl(c) || ((c is '\n' or '\v' or '\f') && _emitted.BackgroundColor != active.BackgroundColor)) { EmitStyle(active); }
                    result.Append(c);
                }
            }
            FlushBlanks();
            return result.ToString();
        }

        private static bool SupportedSgr(string control) {
            string[] parts = control.Substring(2, control.Length - 3).Split(';');
            for (int i = 0; i < parts.Length; i++) {
                int code = 0;
                if (parts[i].Length != 0 && !int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out code)) { return false; }
                if (code == 38 || code == 48) {
                    if (++i >= parts.Length || !int.TryParse(parts[i], out int kind)) { return false; }
                    int count = kind == 2 ? 3 : kind == 5 ? 1 : -1;
                    if (count < 0 || i + count >= parts.Length) { return false; }
                    for (int end = i + count; i < end;) {
                        if (!int.TryParse(parts[++i], NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value > 255) { return false; }
                    }
                } else if (!(code is 0 or 1 or 2 or 3 or 4 or 7 or 9 or 22 or 23 or 24 or 27 or 29 or 39 or 49
                    || code >= 30 && code <= 37 || code >= 40 && code <= 47 || code >= 90 && code <= 97 || code >= 100 && code <= 107)) { return false; }
            }
            return true;
        }
    }
}
