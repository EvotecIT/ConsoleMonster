using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Sessions {
    /// <summary>Opt-in canonical encoding of terminal output without changing its visible cells or repaint boundaries.
    /// Removes window titles, cursor visibility/focus/input-mode housekeeping and cursor queries, combines supported SGR
    /// changes, and re-encodes equivalent cursor movement and already-blank writes through the shared terminal model.
    /// Latent blank styles, real scrolling, holds, synchronized updates and OSC hyperlinks remain effective.
    /// Unknown SGR is rejected rather than silently discarded; other terminal controls are retained.</summary>
    public sealed partial class VtOutputNormalizer {
        private readonly VtScreen _styles;
        private readonly VtScreen _written;

        /// <summary>Creates a normalizer with the recorded terminal's fixed geometry.</summary>
        /// <param name="width">Terminal columns.</param>
        /// <param name="height">Terminal rows.</param>
        public VtOutputNormalizer(int width, int height) {
            if (width < 1 || width > 4096) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height < 1 || height > 4096 || (long)width * height > 4_194_304) { throw new ArgumentOutOfRangeException(nameof(height)); }
            _styles = new VtScreen(width, height, keepScrollback: false);
            _written = new VtScreen(width, height, keepScrollback: false);
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
            for (int i = 0; i < text.Length; i++) {
                VtScreen.CursorState before = _styles.Cursor;
                Style active = _styles.ActiveStyle;
                char c = text[i];
                if (c == '\u001b') {
                    CellBuffer? cells = MayErase(text, i) ? _styles.Snapshot() : null;
                    int start = i;
                    do { _styles.Feed(text[i].ToString()); i++; }
                    while (i < text.Length && _styles.HasIncompleteSequence);
                    if (_styles.HasIncompleteSequence) { throw new FormatException("Normalization requires complete VT sequences."); }
                    string control = text.Substring(start, i - start); i--;
                    if (control.StartsWith("\u001b[", StringComparison.Ordinal) && control.EndsWith("m", StringComparison.Ordinal)) {
                        if (!SupportedSgr(control)) { throw new FormatException("The recording contains SGR outside the supported cell model."); }
                        continue;
                    }
                    if (Housekeeping(control) || CursorTransport(control)) { continue; }
                    if (cells != null && SameVisibleCells(cells)) { continue; }
                    FlushCursor(result, before);
                    EmitStyle(result, active);
                    Emit(result, control);
                    continue;
                }
                string scalar = char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? text.Substring(i++, 2) : c.ToString();
                if (c is '\r' or '\b' or '\t') { _styles.Feed(scalar); continue; }
                if (c is '\n' or '\v' or '\f') {
                    if (c == '\n' && textLineEndings) { _styles.Feed("\r"); before = _styles.Cursor; }
                    if (before.Y != before.Bottom) { _styles.Feed(scalar); continue; }
                    // A bottom-margin line feed really scrolls and uses the active background.
                    FlushCursor(result, before);
                    if (_written.ActiveStyle.BackgroundColor != active.BackgroundColor) { EmitStyle(result, active); }
                    _styles.Feed(scalar);
                    Emit(result, scalar);
                    continue;
                }
                if (c == ' ' && !before.PendingWrap && active.BackgroundColor.IsDefault &&
                    active.Decoration == TextDecoration.None && active.Link == null &&
                    SameVisibleCell(_styles.CellAt(before.X, before.Y), new Cell(" ", active))) {
                    // Keep the original latent foreground in the source model. A later combining
                    // scalar can make it visible, even in a subsequent Normalize segment.
                    _styles.Feed(scalar);
                    continue;
                }
                if (_styles.TryJoiningCell(scalar, out int baseX, out Cell previous) && previous != _written.CellAt(baseX, before.Y)) {
                    FlushCursor(result, before);
                    WriteCell(result, baseX, before.Y, before.OriginTop, previous);
                }
                FlushCursor(result, before);
                if (!char.IsControl(c)) { EmitStyle(result, active); }
                _styles.Feed(scalar);
                Emit(result, scalar);
            }
            // Recording holds and synchronized updates are real boundaries, never flattened pages.
            FlushCursor(result, _styles.Cursor);
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
