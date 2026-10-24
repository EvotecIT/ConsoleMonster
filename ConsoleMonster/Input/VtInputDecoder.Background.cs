using System.Diagnostics;

namespace ConsoleMonster.Input {
    public sealed partial class VtInputDecoder {
        private readonly Stopwatch _backgroundReplyAge = new Stopwatch();

        /// <summary>Abandons a truncated startup reply so later typing can never remain trapped behind it.</summary>
        private void ExpireBackgroundReply() {
            if (_backgroundReplyAge.IsRunning && _backgroundReplyAge.ElapsedMilliseconds >= 250) {
                _pending.Clear();
                _backgroundReplyAge.Reset();
            }
        }

        /// <summary>Consumes only a bounded OSC 11 RGB prefix, leaving unrelated keys for the normal decoder.</summary>
        private bool ReadBackgroundReply(string text, int start, out int consumed) {
            const string prefix = "\u001b]11;rgb:";
            int position = start;
            int channel = 0;
            int digits = 0;
            for (; position < text.Length; position++) {
                char value = text[position];
                int offset = position - start;
                if (offset < prefix.Length) {
                    if (value != prefix[offset]) { break; }
                    continue;
                }
                if ((value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F')) {
                    if (++digits > 4) { break; }
                } else if (value == '/' && digits > 0 && channel < 2) {
                    channel++;
                    digits = 0;
                } else if (channel == 2 && digits > 0 && value == '\u0007') {
                    consumed = position - start + 1;
                    _backgroundReplyAge.Reset();
                    return true;
                } else if (channel == 2 && digits > 0 && value == '\u001b') {
                    if (position + 1 == text.Length) { position++; break; }
                    if (text[position + 1] != '\\') { break; }
                    consumed = position - start + 2;
                    _backgroundReplyAge.Reset();
                    return true;
                } else {
                    break;
                }
            }
            consumed = position - start;
            if (position == text.Length) {
                if (!_backgroundReplyAge.IsRunning) { _backgroundReplyAge.Start(); }
                _pending.Append(text.Substring(start));
                return false;
            }
            _backgroundReplyAge.Reset();
            return true;
        }
    }
}
