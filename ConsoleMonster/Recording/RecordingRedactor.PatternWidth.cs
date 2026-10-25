using System;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        /// <summary>
        /// Conservatively bounds context-free regexes: literals, classes, groups, alternatives and finite quantifiers.
        /// Unsupported syntax takes complete-record matching rather than assuming a shorter secret length.
        /// </summary>
        private sealed class PatternWidth {
            private readonly string _pattern;
            private int _at;
            private int _depth;
            private bool _valid = true;
            private PatternWidth(string pattern) { _pattern = pattern; }
            internal static int? Read(string pattern) {
                PatternWidth reader = new PatternWidth(pattern);
                int length = reader.Expression();
                return reader._valid && reader._at == pattern.Length ? length : (int?)null;
            }

            private int Expression() {
                int maximum = 0, branch = 0;
                while (_valid && _at < _pattern.Length && _pattern[_at] != ')') {
                    if (_pattern[_at] == '|') { maximum = Math.Max(maximum, branch); branch = 0; _at++; continue; }
                    int width = Atom();
                    if (!_valid) { break; }
                    int multiplier = 1;
                    if (_at < _pattern.Length) {
                        char next = _pattern[_at];
                        if (next == '*' || next == '+') { _valid = false; break; }
                        if (next == '?') { _at++; }
                        else if (next == '{') {
                            _at++;
                            int minimum = Number();
                            multiplier = minimum;
                            if (_at < _pattern.Length && _pattern[_at] == ',') { _at++; multiplier = Number(); }
                            if (_at >= _pattern.Length || _pattern[_at++] != '}' || multiplier < minimum) { _valid = false; break; }
                        }
                        if (_at < _pattern.Length && _pattern[_at] == '?') { _at++; }
                    }
                    long sum = branch + (long)width * multiplier;
                    if (sum > MaximumPatternWidth) { _valid = false; break; }
                    branch = (int)sum;
                }
                return Math.Max(maximum, branch);
            }

            private int Atom() {
                char c = _pattern[_at++];
                if (c == '^' || c == '$') { _valid = false; return 0; }
                if (c == '\\') {
                    if (_at == _pattern.Length) { _valid = false; return 0; }
                    c = _pattern[_at++];
                    if (c == 'b' || c == 'B') { return 0; }
                    // Escaped punctuation, common character categories and single-character controls.
                    if (!char.IsLetterOrDigit(c) || "dDwWsSntfrvae".IndexOf(c) >= 0) { return 1; }
                    _valid = false; return 0;
                }
                if (c == '[') {
                    while (_at < _pattern.Length) {
                        c = _pattern[_at++];
                        if (c == '\\' && _at < _pattern.Length) { _at++; }
                        else if (c == '[') { _valid = false; return 0; }
                        else if (c == ']') { return 1; }
                    }
                    _valid = false; return 0;
                }
                if (c == '(') {
                    if (++_depth > 64) { _valid = false; return 0; }
                    if (_at < _pattern.Length && _pattern[_at] == '?') {
                        _at++;
                        if (_at < _pattern.Length && _pattern[_at] == ':') { _at++; }
                        else {
                            int flags = _at;
                            while (_at < _pattern.Length && "imsn-".IndexOf(_pattern[_at]) >= 0) { _at++; }
                            if (_at == flags || _at >= _pattern.Length) { _valid = false; return 0; }
                            if (_pattern[_at] == ')') { _at++; _depth--; return 0; }
                            if (_pattern[_at++] != ':') { _valid = false; return 0; }
                        }
                    }
                    int length = Expression();
                    if (_at >= _pattern.Length || _pattern[_at++] != ')') { _valid = false; }
                    _depth--;
                    return length;
                }
                if (c == '*' || c == '+' || c == '?' || c == '{' || c == '}') { _valid = false; return 0; }
                return 1;
            }

            private int Number() {
                int start = _at, result = 0;
                while (_at < _pattern.Length && _pattern[_at] >= '0' && _pattern[_at] <= '9') {
                    result = result * 10 + _pattern[_at++] - '0';
                    if (result > MaximumPatternWidth) { _valid = false; return 0; }
                }
                if (_at == start) { _valid = false; }
                return result;
            }
        }
    }
}
