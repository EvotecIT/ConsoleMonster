using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        /// <summary>Projects visible characters while retaining their original source offsets.</summary>
        private static string VisibleText(string text, bool stopAtIncomplete, out List<int> positions, out List<SourceRange> controls) {
            controls = TerminalControls(text, out int incompleteAt);
            int end = stopAtIncomplete && incompleteAt >= 0 ? incompleteAt : text.Length;
            StringBuilder visible = new StringBuilder(end);
            positions = new List<int>(end);
            int controlIndex = 0;
            for (int i = 0; i < end; i++) {
                if (controlIndex < controls.Count && i == controls[controlIndex].Start) {
                    i = controls[controlIndex++].End - 1;
                } else {
                    visible.Append(text[i]); positions.Add(i);
                }
            }
            return visible.ToString();
        }

        /// <summary>
        /// Scans each control body once. An unfinished string owns the remaining input, so nested
        /// escapes cannot restart a regex search over the same body. The writer bounds retained input.
        /// </summary>
        private static List<SourceRange> TerminalControls(string text, out int incompleteAt) {
            List<SourceRange> controls = new List<SourceRange>();
            incompleteAt = -1;
            for (int at = 0; at < text.Length;) {
                if (text[at] != '\u001b') { at++; continue; }
                int end = TerminalControlEnd(text, at, out bool incomplete);
                if (incomplete) { incompleteAt = at; break; }
                if (end > at) {
                    controls.Add(new SourceRange(at, end)); at = end;
                } else { at++; }
            }
            return controls;
        }

        /// <summary>Recognizes CSI, OSC, DCS/SOS/PM/APC and ordinary ESC sequences without backtracking.</summary>
        private static int TerminalControlEnd(string text, int start, out bool incomplete) {
            incomplete = false;
            int at = start + 1;
            if (at == text.Length) { incomplete = true; return start; }
            char kind = text[at];
            if (kind == ']' || kind == 'P' || kind == 'X' || kind == '^' || kind == '_') {
                for (at++; at < text.Length; at++) {
                    if (kind == ']' && text[at] == '\a') { return at + 1; }
                    if (text[at] == '\u001b' && at + 1 < text.Length && text[at + 1] == '\\') { return at + 2; }
                }
                incomplete = true; return start;
            }
            if (kind == '[') {
                at++;
                while (at < text.Length && text[at] >= 0x30 && text[at] <= 0x3f) { at++; }
                while (at < text.Length && text[at] >= 0x20 && text[at] <= 0x2f) { at++; }
                if (at == text.Length) { incomplete = true; return start; }
                return text[at] >= 0x40 && text[at] <= 0x7e ? at + 1 : start;
            }
            while (at < text.Length && text[at] >= 0x20 && text[at] <= 0x2f) { at++; }
            if (at == text.Length) { incomplete = true; return start; }
            return text[at] >= 0x30 && text[at] <= 0x7e ? at + 1 : start;
        }
    }
}
