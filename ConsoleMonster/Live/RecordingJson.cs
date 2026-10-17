using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Live {
    // Deliberately limited to the flat JSON objects in the recording contract.
    internal sealed class RecordingJson {
        private readonly string _text;
        private int _position;

        internal RecordingJson(string text) { _text = text; }

        internal Dictionary<string, string?> ReadObject(out HashSet<string> strings) {
            Dictionary<string, string?> fields = new Dictionary<string, string?>(StringComparer.Ordinal);
            strings = new HashSet<string>(StringComparer.Ordinal);
            Require('{');
            if (!Take('}')) {
                do {
                    string name = ReadString();
                    Require(':');
                    SkipSpace();
                    string? value;
                    if (Peek == '"') { value = ReadString(); strings.Add(name); }
                    else { value = ReadPrimitive(); }
                    if (fields.ContainsKey(name)) { throw Error("Duplicate field."); }
                    fields.Add(name, value);
                } while (Take(','));
                Require('}');
            }
            SkipSpace();
            if (_position != _text.Length) { throw Error("Trailing content."); }
            return fields;
        }

        private char Peek => _position < _text.Length ? _text[_position] : '\0';

        private string ReadString() {
            Require('"');
            StringBuilder result = new StringBuilder();
            while (_position < _text.Length) {
                char c = _text[_position++];
                if (c == '"') { return result.ToString(); }
                if (c < ' ') { throw Error("Unescaped control character."); }
                if (c != '\\') { result.Append(c); continue; }
                if (_position >= _text.Length) { break; }
                c = _text[_position++];
                switch (c) {
                    case '"': case '\\': case '/': result.Append(c); break;
                    case 'b': result.Append('\b'); break;
                    case 'f': result.Append('\f'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break;
                    case 'u':
                        if (_position + 4 > _text.Length || !ushort.TryParse(_text.Substring(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code)) {
                            throw Error("Invalid Unicode escape.");
                        }
                        result.Append((char)code);
                        _position += 4;
                        break;
                    default: throw Error("Invalid string escape.");
                }
            }
            throw Error("Unterminated string.");
        }

        private string? ReadPrimitive() {
            int start = _position;
            while (Peek != ',' && Peek != '}' && Peek != '\0' && !char.IsWhiteSpace(Peek)) { _position++; }
            string value = _text.Substring(start, _position - start);
            if (value == "null") { return null; }
            if (value == "true" || value == "false") { return value; }
            // Validate JSON's number grammar before applying invariant numeric conversion.
            int index = 0;
            if (index < value.Length && value[index] == '-') { index++; }
            if (index >= value.Length) { throw Error("Expected JSON value."); }
            if (value[index] == '0') { index++; }
            else {
                if (value[index] < '1' || value[index] > '9') { throw Error("Invalid number."); }
                while (index < value.Length && Digit(value[index])) { index++; }
            }
            if (index < value.Length && value[index] == '.') {
                int fraction = ++index;
                while (index < value.Length && Digit(value[index])) { index++; }
                if (index == fraction) { throw Error("Invalid fraction."); }
            }
            if (index < value.Length && (value[index] == 'e' || value[index] == 'E')) {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-')) { index++; }
                int exponent = index;
                while (index < value.Length && Digit(value[index])) { index++; }
                if (index == exponent) { throw Error("Invalid exponent."); }
            }
            if (index != value.Length) { throw Error("Invalid number."); }
            return value;
        }

        private static bool Digit(char c) => c >= '0' && c <= '9';

        private void SkipSpace() {
            while (Peek == ' ' || Peek == '\t' || Peek == '\r' || Peek == '\n') { _position++; }
        }

        private bool Take(char c) {
            SkipSpace();
            if (Peek != c) { return false; }
            _position++;
            return true;
        }

        private void Require(char c) {
            if (!Take(c)) { throw Error("Expected '" + c + "'."); }
        }

        private FormatException Error(string message) => new FormatException(message + " Column " + (_position + 1).ToString(CultureInfo.InvariantCulture) + ".");
    }
}
