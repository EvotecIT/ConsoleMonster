using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Input {
    /// <summary>Decodes VT and xterm terminal input into normalized input events.</summary>
    public sealed partial class VtInputDecoder {
        private const char Escape = '\u001b';
        private const int MaximumCsiLength = 64;
        private readonly StringBuilder _pending = new StringBuilder();
        private readonly StringBuilder _paste = new StringBuilder();
        private bool _inPaste;
        private bool _droppingCsi;

        /// <summary>Unix startup queries may leave a late OSC 11 reply; ordinary decoders keep Alt+] as a key.</summary>
        internal bool IgnoreBackgroundReplies { get; set; }

        /// <summary>Creates an empty VT input decoder.</summary>
        public VtInputDecoder() { }

        /// <summary>Whether a partial escape sequence, paste, or UTF-16 surrogate is buffered.</summary>
        public bool HasPending => _pending.Length != 0 || _inPaste || _droppingCsi;

        /// <summary>Decodes available terminal input and appends complete events to <paramref name="output"/>.</summary>
        /// <param name="text">The characters read from the terminal.</param>
        /// <param name="output">The destination event list.</param>
        public void Feed(string text, List<InputEvent> output) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (output == null) {
                throw new ArgumentNullException(nameof(output));
            }
            if (text.Length == 0) {
                return;
            }
            ExpireBackgroundReply();
            if (_inPaste) {
                _pending.Append(text);
                ProcessPaste(output);
                return;
            }

            int index = 0;
            if (_pending.Length > 0 && _pending[_pending.Length - 1] >= '\ud800' && _pending[_pending.Length - 1] <= '\udbff' && char.IsLowSurrogate(text[0])) {
                output.Add(InputEvent.Paste(new string(new[] { _pending[_pending.Length - 1], text[0] })));
                _pending.Length--;
                index = 1;
            }
            string combined = _pending.ToString() + text.Substring(index);
            _pending.Clear();
            bool previousWasCr = false;
            for (int i = 0; i < combined.Length;) {
                char c = combined[i];
                if (_droppingCsi) {
                    if (c >= 0x40 && c <= 0x7e) _droppingCsi = false;
                    i++;
                    continue;
                }
                if (c == Escape) {
                    if (i + 1 >= combined.Length) {
                        _pending.Append(c);
                        break;
                    }
                    char next = combined[i + 1];
                    // A delayed startup colour reply is protocol data, never editable text.
                    if (IgnoreBackgroundReplies && next == ']' && (combined.Substring(i).StartsWith("\u001b]11;", StringComparison.Ordinal)
                        || "\u001b]11;".StartsWith(combined.Substring(i), StringComparison.Ordinal))) {
                        if (!ReadBackgroundReply(combined, i, out int consumed)) { break; }
                        i += consumed;
                        continue;
                    }
                    if (next == Escape) {
                        output.Add(InputEvent.Press(ConsoleKey.Escape));
                        i++;
                        previousWasCr = false;
                        continue;
                    }
                    if (next == '[' || next == 'O') {
                        if (next == '[') {
                            int consumed;
                            bool complete;
                            bool overlong;
                            string body;
                            if (TryReadCsi(combined, i, out consumed, out complete, out overlong, out body)) {
                                if (!overlong) DecodeCsi(body, output);
                                else _droppingCsi = !complete;
                                i += consumed;
                                if (_inPaste) {
                                    if (i < combined.Length) _pending.Append(combined.Substring(i));
                                    ProcessPaste(output);
                                    break;
                                }
                                previousWasCr = false;
                                continue;
                            }
                            _pending.Append(combined.Substring(i));
                            break;
                        }
                        if (i + 2 >= combined.Length) {
                            _pending.Append(combined.Substring(i));
                            break;
                        }
                        DecodeSs3(combined[i + 2], output);
                        i += 3;
                        previousWasCr = false;
                        continue;
                    }
                    if (next == '\u007f' || next == '\b') {
                        output.Add(InputEvent.Press(ConsoleKey.Backspace, KeyModifiers.Alt));
                        i += 2;
                        previousWasCr = false;
                        continue;
                    }
                    if (next >= 1 && next <= 0x1a) {
                        DecodeControl(next, KeyModifiers.Alt, output);
                        i += 2;
                        previousWasCr = false;
                        continue;
                    }
                    if (IsPrintable(next)) {
                        output.Add(InputEvent.Character(next, KeyModifiers.Alt));
                    }
                    i += 2;
                    previousWasCr = false;
                    continue;
                }

                if (c == '\r') {
                    output.Add(InputEvent.Press(ConsoleKey.Enter));
                    previousWasCr = true;
                    i++;
                    continue;
                }
                if (c == '\n') {
                    if (!previousWasCr) {
                        output.Add(InputEvent.Press(ConsoleKey.Enter));
                    }
                    previousWasCr = false;
                    i++;
                    continue;
                }
                previousWasCr = false;
                if (c == '\t') {
                    output.Add(InputEvent.Press(ConsoleKey.Tab));
                    i++;
                } else if (c == '\b' || c == '\u007f') {
                    output.Add(InputEvent.Press(ConsoleKey.Backspace));
                    i++;
                } else if (c == '\0') {
                    output.Add(InputEvent.Press(ConsoleKey.Spacebar, KeyModifiers.Control));
                    i++;
                } else if (c >= 1 && c <= 0x1a) {
                    DecodeControl(c, KeyModifiers.None, output);
                    i++;
                } else if (c == 0x1c) {
                    // FS is what terminals send for Ctrl+\ when signal keys are off.
                    output.Add(InputEvent.Press(ConsoleKey.Oem5, KeyModifiers.Control));
                    i++;
                } else if (c >= 0x1d && c <= 0x1f) {
                    i++;
                } else if (char.IsHighSurrogate(c)) {
                    if (i + 1 < combined.Length && char.IsLowSurrogate(combined[i + 1])) {
                        output.Add(InputEvent.Paste(new string(new[] { c, combined[i + 1] })));
                        i += 2;
                    } else if (i + 1 == combined.Length) {
                        _pending.Append(c);
                        break;
                    } else {
                        i++;
                    }
                } else if (char.IsLowSurrogate(c)) {
                    i++;
                } else if (IsPrintable(c)) {
                    output.Add(InputEvent.Character(c));
                    i++;
                } else {
                    i++;
                }
            }
        }

        /// <summary>Resolves an incomplete escape after a quiet period; an unfinished paste remains buffered.</summary>
        /// <param name="output">The destination event list.</param>
        public void Flush(List<InputEvent> output) {
            if (output == null) {
                throw new ArgumentNullException(nameof(output));
            }
            if (_inPaste || _pending.Length == 0) {
                return;
            }
            string pending = _pending.ToString();
            if (IgnoreBackgroundReplies && (pending.StartsWith("\u001b]11;", StringComparison.Ordinal)
                || (pending.Length > 1 && "\u001b]11;".StartsWith(pending, StringComparison.Ordinal)))) {
                ExpireBackgroundReply();
                return;
            }
            _pending.Clear();
            if (pending == Escape.ToString()) {
                output.Add(InputEvent.Press(ConsoleKey.Escape));
            } else if (pending.StartsWith("\u001b[<", StringComparison.Ordinal)) {
                // An unfinished mouse report must not become Alt+[ or leak its tail as typing.
                _droppingCsi = true;
            } else if (pending.Length >= 2 && pending[0] == Escape && (pending[1] == '[' || pending[1] == 'O')) {
                output.Add(InputEvent.Character(pending[1], KeyModifiers.Alt));
            }
        }

        private static bool TryReadCsi(string text, int start, out int consumed, out bool complete, out bool overlong, out string body) {
            consumed = 0;
            complete = false;
            overlong = false;
            body = string.Empty;
            for (int i = start + 2; i < text.Length; i++) {
                char c = text[i];
                if (c >= 0x40 && c <= 0x7e) {
                    consumed = i - start + 1;
                    complete = true;
                    overlong = i - start - 1 > MaximumCsiLength;
                    if (!overlong) body = text.Substring(start + 2, i - start - 2) + c;
                    return true;
                }
                // Keep scanning malformed bytes to the CSI final so no tail leaks as typed text.
                if (i - start - 1 > MaximumCsiLength) {
                    overlong = true;
                    consumed = i - start + 1;
                    return true;
                }
            }
            return false;
        }

        private void DecodeCsi(string sequence, List<InputEvent> output) {
            if (sequence.Length == 0) {
                return;
            }
            char final = sequence[sequence.Length - 1];
            string parameters = sequence.Substring(0, sequence.Length - 1);
            if (parameters.Length > 0 && parameters[0] == '<') {
                DecodeMouse(parameters.Substring(1), final, output);
                return;
            }
            int[] values;
            if (!TryParameters(parameters, out values)) {
                return;
            }
            if (final == 'I' && parameters.Length == 0) {
                output.Add(InputEvent.Focus(true));
                return;
            }
            if (final == 'O' && parameters.Length == 0) {
                output.Add(InputEvent.Focus(false));
                return;
            }
            if (final == '_' && values.Length == 6) {
                DecodeWin32(values, output);
                return;
            }
            if (final == 'u' && values.Length >= 1) {
                DecodeKitty(values, output);
                return;
            }
            if (final == '~' && values.Length >= 1) {
                if (values[0] == 200) {
                    _inPaste = true;
                    _paste.Length = 0;
                    return;
                }
                if (values[0] == 201) {
                    return;
                }
                ConsoleKey key = TildeKey(values[0]);
                if (key != 0) {
                    output.Add(InputEvent.Press(key, values.Length > 1 ? ModifierParameter(values[1]) : KeyModifiers.None));
                }
                return;
            }
            if (final == 't' && values.Length == 3 && values[0] == 8) {
                output.Add(InputEvent.Resized(values[2], values[1]));
                return;
            }
            if (final == 'R' && values.Length == 2) {
                return;
            }
            if (values.Length > 0 && values[0] == 200 && final == '~') {
                return;
            }
            ConsoleKey navigation = final == 'A' ? ConsoleKey.UpArrow : final == 'B' ? ConsoleKey.DownArrow : final == 'C' ? ConsoleKey.RightArrow : final == 'D' ? ConsoleKey.LeftArrow : final == 'H' ? ConsoleKey.Home : final == 'F' ? ConsoleKey.End : final == 'Z' ? ConsoleKey.Tab : (ConsoleKey)0;
            if (navigation != 0) {
                output.Add(InputEvent.Press(navigation, final == 'Z' ? KeyModifiers.Shift : values.Length > 1 ? ModifierParameter(values[1]) : KeyModifiers.None));
                return;
            }
            ConsoleKey function = FunctionKey(final);
            if (function != 0) {
                output.Add(InputEvent.Press(function, values.Length > 1 ? ModifierParameter(values[1]) : KeyModifiers.None));
            }
        }

        private void ProcessPaste(List<InputEvent> output) {
            const string terminator = "\u001b[201~";
            string text = _pending.ToString();
            int end = text.IndexOf(terminator, StringComparison.Ordinal);
            if (end >= 0) {
                _paste.Append(text.Substring(0, end));
                output.Add(InputEvent.Paste(_paste.ToString()));
                _paste.Length = 0;
                _inPaste = false;
                _pending.Clear();
                string rest = text.Substring(end + terminator.Length);
                if (rest.Length > 0) {
                    Feed(rest, output);
                }
                return;
            }
            int keep = 0;
            for (int length = 1; length < terminator.Length && length <= text.Length; length++) {
                if (text.EndsWith(terminator.Substring(0, length), StringComparison.Ordinal)) {
                    keep = length;
                }
            }
            if (text.Length > keep) {
                _paste.Append(text.Substring(0, text.Length - keep));
            }
            _pending.Length = 0;
            if (keep > 0) {
                _pending.Append(text.Substring(text.Length - keep));
            }
        }

        private static void DecodeSs3(char final, List<InputEvent> output) {
            ConsoleKey key = final == 'M' ? ConsoleKey.Enter : final == 'A' ? ConsoleKey.UpArrow : final == 'B' ? ConsoleKey.DownArrow : final == 'C' ? ConsoleKey.RightArrow : final == 'D' ? ConsoleKey.LeftArrow : final == 'H' ? ConsoleKey.Home : final == 'F' ? ConsoleKey.End : FunctionKey(final);
            if (key != 0) {
                output.Add(InputEvent.Press(key));
            }
        }

        private static ConsoleKey FunctionKey(char final) {
            switch (final) {
                case 'P': return ConsoleKey.F1;
                case 'Q': return ConsoleKey.F2;
                case 'R': return ConsoleKey.F3;
                case 'S': return ConsoleKey.F4;
                default: return 0;
            }
        }

        private static ConsoleKey TildeKey(int value) {
            switch (value) {
                case 1: case 7: return ConsoleKey.Home;
                case 2: return ConsoleKey.Insert;
                case 3: return ConsoleKey.Delete;
                case 4: case 8: return ConsoleKey.End;
                case 5: return ConsoleKey.PageUp;
                case 6: return ConsoleKey.PageDown;
                case 11: return ConsoleKey.F1;
                case 12: return ConsoleKey.F2;
                case 13: return ConsoleKey.F3;
                case 14: return ConsoleKey.F4;
                case 15: return ConsoleKey.F5;
                case 17: return ConsoleKey.F6;
                case 18: return ConsoleKey.F7;
                case 19: return ConsoleKey.F8;
                case 20: return ConsoleKey.F9;
                case 21: return ConsoleKey.F10;
                case 23: return ConsoleKey.F11;
                case 24: return ConsoleKey.F12;
                default: return 0;
            }
        }

        private static void DecodeControl(char value, KeyModifiers extra, List<InputEvent> output) {
            if (value == 0) {
                output.Add(InputEvent.Press(ConsoleKey.Spacebar, KeyModifiers.Control | extra));
            } else {
                output.Add(InputEvent.Press((ConsoleKey)((int)ConsoleKey.A + value - 1), KeyModifiers.Control | extra));
            }
        }

        private static void DecodeKitty(int[] values, List<InputEvent> output) {
            int code = values[0];
            KeyModifiers modifiers = values.Length > 1 ? ModifierParameter(values[1]) : KeyModifiers.None;
            if (code == 13) {
                output.Add(InputEvent.Press(ConsoleKey.Enter, modifiers));
            } else if (code == 9) {
                output.Add(InputEvent.Press(ConsoleKey.Tab, modifiers));
            } else if (code == 27) {
                output.Add(InputEvent.Press(ConsoleKey.Escape, modifiers));
            } else if (code == 127) {
                output.Add(InputEvent.Press(ConsoleKey.Backspace, modifiers));
            } else if (code >= 0x20 && code <= 0x10ffff && !(code >= 0xd800 && code <= 0xdfff)) {
                string value = char.ConvertFromUtf32(code);
                if ((modifiers & KeyModifiers.Control) != 0 && value.Length == 1 && char.IsLetter(value[0])) {
                    output.Add(InputEvent.Press(InputEvent.KeyFor(value[0]), modifiers));
                } else if (code == '\\' && (modifiers & KeyModifiers.Control) != 0) {
                    output.Add(InputEvent.Press(ConsoleKey.Oem5, modifiers));
                } else if (value.Length == 1) {
                    output.Add(InputEvent.Character(value[0], modifiers));
                } else {
                    output.Add(InputEvent.Paste(value));
                }
            }
        }

        private static void DecodeWin32(int[] values, List<InputEvent> output) {
            int virtualKey = values[0];
            int unit = values[2];
            int keyDown = values[3];
            if (keyDown != 1 || virtualKey == 16 || virtualKey == 17 || virtualKey == 18 || virtualKey == 91 || virtualKey == 92 || virtualKey == 20 || virtualKey == 144 || virtualKey == 145) {
                return;
            }
            KeyModifiers modifiers = KeyModifiers.None;
            if ((values[4] & 0x10) != 0) modifiers |= KeyModifiers.Shift;
            if ((values[4] & 0x0c) != 0) modifiers |= KeyModifiers.Control;
            if ((values[4] & 0x03) != 0) modifiers |= KeyModifiers.Alt;
            char character = unit >= 0 && unit <= 0xffff ? (char)unit : '\0';
            int repeat = values[5] <= 0 ? 1 : values[5];
            if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == (KeyModifiers.Control | KeyModifiers.Alt) && IsPrintable(character)) {
                modifiers &= ~(KeyModifiers.Control | KeyModifiers.Alt);
                for (int i = 0; i < repeat; i++) output.Add(InputEvent.Character(character));
                return;
            }
            if (IsPrintable(character) && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0) {
                for (int i = 0; i < repeat; i++) output.Add(InputEvent.Character(character));
                return;
            }
            for (int i = 0; i < repeat; i++) output.Add(InputEvent.Press((ConsoleKey)virtualKey, modifiers));
        }

        private static KeyModifiers ModifierParameter(int parameter) {
            int mask = parameter - 1;
            KeyModifiers result = KeyModifiers.None;
            if ((mask & 1) != 0) result |= KeyModifiers.Shift;
            if ((mask & 2) != 0) result |= KeyModifiers.Alt;
            if ((mask & 4) != 0) result |= KeyModifiers.Control;
            return result;
        }

        private static bool TryParameters(string parameters, out int[] values) {
            values = new int[0];
            if (parameters.Length == 0) return true;
            string[] parts = parameters.Split(';');
            values = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++) {
                if (parts[i].Length == 0 || !int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i])) return false;
            }
            return true;
        }

        private static bool IsPrintable(char c) => c >= 0x20 && c != 0x7f && !char.IsSurrogate(c);
    }
}
