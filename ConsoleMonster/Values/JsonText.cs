using System;

namespace ConsoleMonster.Values {
    /// <summary>Validates the JSON grammar before the display formatter removes insignificant whitespace.</summary>
    internal sealed class JsonText {
        private readonly string _text;
        private int _position;

        private JsonText(string text) => _text = text;

        internal static bool IsContainer(string text) {
            if (text.Length == 0 || (text[0] != '{' && text[0] != '[')) {
                return false;
            }
            JsonText reader = new JsonText(text);
            return reader.Value(0) && reader._position == text.Length;
        }

        private bool Value(int depth) {
            // The formatter is for display; reject extreme nesting without risking the process stack.
            if (depth > 256) {
                return false;
            }
            WhiteSpace();
            if (_position == _text.Length) {
                return false;
            }
            switch (_text[_position]) {
                case '{':
                    _position++;
                    WhiteSpace();
                    if (Take('}')) {
                        return true;
                    }
                    do {
                        WhiteSpace();
                        if (!String()) {
                            return false;
                        }
                        WhiteSpace();
                        if (!Take(':') || !Value(depth + 1)) {
                            return false;
                        }
                        WhiteSpace();
                        if (Take('}')) {
                            return true;
                        }
                    } while (Take(','));
                    return false;
                case '[':
                    _position++;
                    WhiteSpace();
                    if (Take(']')) {
                        return true;
                    }
                    do {
                        if (!Value(depth + 1)) {
                            return false;
                        }
                        WhiteSpace();
                        if (Take(']')) {
                            return true;
                        }
                    } while (Take(','));
                    return false;
                case '"':
                    return String();
                case 't':
                    return Literal("true");
                case 'f':
                    return Literal("false");
                case 'n':
                    return Literal("null");
                default:
                    return Number();
            }
        }

        private bool String() {
            if (!Take('"')) {
                return false;
            }
            while (_position < _text.Length) {
                char c = _text[_position++];
                if (c == '"') {
                    return true;
                }
                if (c < 0x20) {
                    return false;
                }
                if (c != '\\') {
                    continue;
                }
                if (_position == _text.Length) {
                    return false;
                }
                char escape = _text[_position++];
                if (escape == 'u') {
                    for (int i = 0; i < 4; i++) {
                        if (_position == _text.Length) {
                            return false;
                        }
                        char hex = _text[_position++];
                        if (!(hex >= '0' && hex <= '9') && !(hex >= 'a' && hex <= 'f') && !(hex >= 'A' && hex <= 'F')) {
                            return false;
                        }
                    }
                } else if ("\"\\/bfnrt".IndexOf(escape) < 0) {
                    return false;
                }
            }
            return false;
        }

        private bool Number() {
            Take('-');
            if (!Take('0') && !Digits()) {
                return false;
            }
            if (Take('.') && !Digits()) {
                return false;
            }
            if (Take('e') || Take('E')) {
                if (!Take('+')) {
                    Take('-');
                }
                if (!Digits()) {
                    return false;
                }
            }
            return true;
        }

        private bool Digits() {
            int start = _position;
            while (_position < _text.Length && _text[_position] >= '0' && _text[_position] <= '9') {
                _position++;
            }
            return _position > start;
        }

        private bool Literal(string value) {
            if (_position + value.Length > _text.Length || string.CompareOrdinal(_text, _position, value, 0, value.Length) != 0) {
                return false;
            }
            _position += value.Length;
            return true;
        }

        private bool Take(char value) {
            if (_position < _text.Length && _text[_position] == value) {
                _position++;
                return true;
            }
            return false;
        }

        private void WhiteSpace() {
            while (_position < _text.Length && " \t\r\n".IndexOf(_text[_position]) >= 0) {
                _position++;
            }
        }
    }
}
