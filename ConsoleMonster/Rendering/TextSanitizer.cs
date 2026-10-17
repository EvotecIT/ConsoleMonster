using System.Text;

namespace ConsoleMonster.Rendering {
    /// <summary>
    /// Makes untrusted text safe for the line path (stream and plain output, narration, captured lines, markup lines):
    /// whole escape sequences are removed (CSI <c>ESC [ ... final</c>, OSC <c>ESC ] ... BEL</c> or <c>ESC ]... ESC \</c>, and
    /// two-character escapes), then the remaining C0 controls, DEL and C1 controls (U+0080 to U+009F) are dropped and tabs
    /// become four spaces. Only the output layer adds escape sequences, so Plain mode never emits any.
    /// </summary>
    internal static class TextSanitizer {
        private const char Escape = (char)0x1B;
        private const char Bell = (char)0x07;

        /// <summary>Appends <paramref name="text"/> without control characters or escape sequences.</summary>
        /// <param name="sb">The target.</param>
        /// <param name="text">The untrusted text.</param>
        public static void Append(StringBuilder sb, string? text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            if (IsClean(text!)) {
                sb.Append(text);
                return;
            }
            int i = 0;
            while (i < text!.Length) {
                char c = text[i];
                if (c == Escape) {
                    i = SkipEscape(text, i);
                    continue;
                }
                if (c == '\t') {
                    sb.Append("    ");
                } else if (!IsControl(c)) {
                    sb.Append(c);
                }
                i++;
            }
        }

        /// <summary>Returns <paramref name="text"/> without control characters or escape sequences.</summary>
        /// <param name="text">The untrusted text.</param>
        /// <returns>The safe text.</returns>
        public static string Clean(string? text) {
            if (string.IsNullOrEmpty(text) || IsClean(text!)) {
                return text ?? string.Empty;
            }
            StringBuilder sb = new StringBuilder(text!.Length);
            Append(sb, text);
            return sb.ToString();
        }

        private static bool IsClean(string text) {
            foreach (char c in text) {
                if (IsControl(c) || c == '\t') {
                    return false;
                }
            }
            return true;
        }

        private static bool IsControl(char c) => c < 0x20 || c == 0x7F || (c >= 0x80 && c <= 0x9F);

        // Returns the index after the escape sequence that starts at start.
        private static int SkipEscape(string text, int start) {
            int i = start + 1;
            if (i >= text.Length) {
                return i;
            }
            char kind = text[i];
            if (kind == '[') {
                // CSI: parameter and intermediate bytes, then one final byte in 0x40..0x7E.
                i++;
                while (i < text.Length && (text[i] < 0x40 || text[i] > 0x7E)) {
                    i++;
                }
                return i + 1;
            }
            if (kind == ']' || kind == 'P' || kind == '_' || kind == '^') {
                // OSC, DCS, APC, PM: up to BEL or the string terminator ESC \.
                i++;
                while (i < text.Length) {
                    if (text[i] == Bell) {
                        return i + 1;
                    }
                    if (text[i] == Escape && i + 1 < text.Length && text[i + 1] == '\\') {
                        return i + 2;
                    }
                    i++;
                }
                return i;
            }
            // A two-character escape (ESC 7, ESC c, ...).
            return i + 1;
        }
    }
}
