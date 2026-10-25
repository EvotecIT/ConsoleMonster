using System.Collections.Generic;
using System.Text;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        /// <summary>Projects visible characters while retaining their original source offsets.</summary>
        private static string VisibleText(string text, bool stopAtIncomplete, out List<int> positions, out List<SourceRange> controls, bool replay = false) {
            controls = TerminalControls(text, out int incompleteAt, replay);
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
        private static List<SourceRange> TerminalControls(string text, out int incompleteAt, bool replay = false) {
            List<SourceRange> controls = new List<SourceRange>();
            incompleteAt = -1;
            for (int at = 0; at < text.Length;) {
                if (IsIgnoredGroundControl(text[at])) { controls.Add(new SourceRange(at, at + 1)); at++; continue; }
                if (text[at] != '\u001b') { at++; continue; }
                int end = TerminalControlEnd(text, at, out bool incomplete, replay);
                if (incomplete) { incompleteAt = at; break; }
                if (end > at) {
                    controls.Add(new SourceRange(at, end)); at = end;
                } else { at++; }
            }
            return controls;
        }

        private static bool IsIgnoredGroundControl(char c) => c == 0x7f || (c < 0x20 && c != '\u001b'
            && c != '\r' && c != '\n' && c != '\t' && c != '\b' && c != '\v' && c != '\f');

        /// <summary>Recognizes CSI, OSC, DCS/SOS/PM/APC and ordinary ESC sequences without backtracking.</summary>
        private static int TerminalControlEnd(string text, int start, out bool incomplete, bool replay) {
            TerminalControlState state = default;
            for (int at = start; at < text.Length; at++) {
                ControlScanResult result = state.Append(text[at], replay);
                if (result == ControlScanResult.Complete || result == ControlScanResult.Restarted) {
                    incomplete = false;
                    return result == ControlScanResult.Restarted ? at : at + 1;
                }
            }
            incomplete = true; return start;
        }

        /// <summary>Tracks the same controls across arbitrary reads without rescanning a growing string body.</summary>
        internal struct TerminalControlState {
            private ControlState _state;
            private bool _osc;
            internal bool IsPending => _state != ControlState.Ground;

            internal ControlScanResult Append(char c, bool replay = false) {
                switch (_state) {
                    case ControlState.Ground:
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Pending; }
                        return ControlScanResult.Text;
                    case ControlState.Escape:
                        if (c == ']' || c == 'P' || c == 'X' || c == '^' || c == '_') {
                            _osc = c == ']'; _state = ControlState.String; return ControlScanResult.Pending;
                        }
                        if (c == '[') { _state = ControlState.Csi; return ControlScanResult.Pending; }
                        if (replay) {
                            // VtScreen accepts exactly one charset selector after these introducers;
                            // all other ordinary escapes consume only their immediate next character.
                            if (c == '(' || c == ')' || c == '*' || c == '+') {
                                _state = ControlState.Charset; return ControlScanResult.Pending;
                            }
                            _state = ControlState.Ground; return ControlScanResult.Complete;
                        }
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Restarted; }
                        if (c >= 0x20 && c <= 0x2f) { _state = ControlState.EscapeIntermediate; return ControlScanResult.Pending; }
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.EscapeIntermediate:
                        if (c >= 0x20 && c <= 0x2f) { return ControlScanResult.Pending; }
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.Charset:
                        _state = ControlState.Ground; return ControlScanResult.Complete;
                    case ControlState.Csi:
                        // Replay consumes C0 characters inside CSI without ending the control. They
                        // cannot flush a redaction record or split its visible credential spelling.
                        if (c == '\u001b') { _state = ControlState.Escape; return ControlScanResult.Restarted; }
                        if (c >= 0x40 && c <= 0x7e) { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        return ControlScanResult.Pending;
                    case ControlState.String:
                        if (_osc && c == '\a') { _state = ControlState.Ground; return ControlScanResult.Complete; }
                        if (c == '\u001b') { _state = ControlState.StringEscape; }
                        return ControlScanResult.Pending;
                    case ControlState.StringEscape:
                        if (c == '\\' || (_osc && c == '\a')) { _state = ControlState.Ground; return ControlScanResult.Complete; }
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

        internal enum ControlScanResult { Text, Pending, Complete, Restarted }
    }
}
