using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        private static readonly TerminalInterpretation[] Interpretations = {
            TerminalInterpretation.OpaqueStrings, TerminalInterpretation.Standard, TerminalInterpretation.Replay
        };

        /// <summary>Projects visible characters while retaining their original source offsets.</summary>
        private static string VisibleText(string text, bool stopAtIncomplete, out List<int> positions, out List<SourceRange> controls, TerminalInterpretation interpretation) {
            controls = TerminalControls(text, out int incompleteAt, interpretation);
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
        private static List<SourceRange> TerminalControls(string text, out int incompleteAt, TerminalInterpretation interpretation) {
            List<SourceRange> controls = new List<SourceRange>();
            incompleteAt = -1;
            for (int at = 0; at < text.Length;) {
                // These controls do not create a visible gap in either the terminal or replay.
                if (IgnoredGroundControl(text[at])) { controls.Add(new SourceRange(at, ++at)); continue; }
                if (text[at] != '\u001b') { at++; continue; }
                int end = TerminalControlEnd(text, at, out bool incomplete, interpretation);
                if (incomplete) { incompleteAt = at; break; }
                if (end > at) {
                    controls.Add(new SourceRange(at, end)); at = end;
                } else { at++; }
            }
            return controls;
        }

        /// <summary>Recognizes CSI, OSC, DCS/SOS/PM/APC and ordinary ESC sequences without backtracking.</summary>
        private static int TerminalControlEnd(string text, int start, out bool incomplete, TerminalInterpretation interpretation) {
            TerminalControlState state = default;
            for (int at = start; at < text.Length; at++) {
                ControlScanResult result = state.Append(text[at], interpretation);
                if (result == ControlScanResult.Complete || result == ControlScanResult.Restarted) {
                    incomplete = false;
                    return result == ControlScanResult.Restarted ? at : at + 1;
                }
            }
            incomplete = true; return start;
        }

        private static bool IgnoredGroundControl(char c) =>
            c == '\u007f' || (c < 0x20 && c != '\u001b' && c != '\b' && c != '\t'
                && c != '\n' && c != '\r' && c != '\v' && c != '\f');

        /// <summary>Tracks the same controls across arbitrary reads without rescanning a growing string body.</summary>
        internal struct TerminalControlState {
            private ControlState _state;
            private bool _osc;
            internal bool IsPending => _state != ControlState.Ground;

            internal ControlScanResult Append(char c, TerminalInterpretation interpretation = TerminalInterpretation.OpaqueStrings) {
                bool replay = interpretation == TerminalInterpretation.Replay;
                // Preserve the opaque view as well as native CAN/SUB cancellation. Replay consumes
                // these characters inside strings; neither alternative may erase the opaque map.
                if (interpretation == TerminalInterpretation.Standard && IsPending && (c == '\u0018' || c == '\u001a')) {
                    _state = ControlState.Ground; return ControlScanResult.Complete;
                }
                switch (_state) {
                    case ControlState.Ground:
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Pending; }
                        return ControlScanResult.Text;
                    case ControlState.Escape:
                        if (c == ']' || c == 'P' || c == 'X' || c == '^' || c == '_') {
                            _osc = c == ']'; _state = ControlState.String; return ControlScanResult.Pending;
                        }
                        if (c == '[') { _state = ControlState.Csi; return ControlScanResult.Pending; }
                        if (replay && (c == '(' || c == ')' || c == '*' || c == '+')) {
                            _state = ControlState.Charset; return ControlScanResult.Pending;
                        }
                        if (replay) { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        if (c == '\u001b') { return ControlScanResult.Restarted; }
                        if (c < 0x20 || c == 0x7f) { return ControlScanResult.Pending; }
                        if (c >= 0x20 && c <= 0x2f) { _state = ControlState.EscapeIntermediate; return ControlScanResult.Pending; }
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.EscapeIntermediate:
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Restarted; }
                        if (c < 0x20 || c == 0x7f) { return ControlScanResult.Pending; }
                        if (c >= 0x20 && c <= 0x2f) { return ControlScanResult.Pending; }
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.Charset:
                        // VtScreen consumes exactly one character after a charset designation.
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.Csi:
                        // Replay consumes C0 characters inside CSI without ending the control. They
                        // cannot flush a redaction record or split its visible credential spelling.
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Restarted; }
                        if (c >= 0x40 && c <= 0x7e) { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        return ControlScanResult.Pending;
                    case ControlState.String:
                        if (_osc && c == '\a') { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        if (c == '\u001b' && interpretation == TerminalInterpretation.Standard) {
                            _state = ControlState.Escape; return ControlScanResult.Restarted;
                        }
                        if (c == '\u001b') { _state = ControlState.StringEscape; }
                        return ControlScanResult.Pending;
                    case ControlState.StringEscape:
                        if (_osc && c == '\a') { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        if (c == '\\') { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        if (_osc && replay) {
                            // OSC replay ends at an unexpected ESC and processes this character again.
                            _state = c == '\u001b' ? ControlState.Escape : ControlState.Ground;
                            return ControlScanResult.Restarted;
                        }
                        // DCS/SOS/PM/APC consume the character after ESC, including another ESC.
                        _state = !replay && c == '\u001b' ? ControlState.StringEscape : ControlState.String;
                        return ControlScanResult.Pending;
                    default:
                        return ControlScanResult.Text;
                }
            }

            private enum ControlState { Ground, Escape, EscapeIntermediate, Charset, Csi, String, StringEscape }
        }

        // Preserve the established opaque-string interpretation and also protect native cancellation
        // and VtScreen's existing behavior. These differ for malformed/nested escapes.
        internal enum TerminalInterpretation { OpaqueStrings, Standard, Replay }
        internal enum ControlScanResult { Text, Pending, Complete, Restarted }
    }
}
