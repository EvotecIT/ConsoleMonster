using System;
using System.Diagnostics;
using System.Globalization;

namespace ConsoleMonster.Input {
    internal sealed partial class VtInputDecoder {
        private readonly Stopwatch _backgroundReplyAge = new Stopwatch();
        private static readonly string[] StartupColourReplies = { "\u001b]4;", "\u001b]10;", "\u001b]11;" };

        private static bool IsStartupColourReply(string text) {
            foreach (string prefix in StartupColourReplies) {
                if (text.StartsWith(prefix, StringComparison.Ordinal) || (text.Length > 1 && prefix.StartsWith(text, StringComparison.Ordinal))) { return true; }
            }
            return false;
        }

        /// <summary>Abandons a truncated startup reply so later typing can never remain trapped behind it.</summary>
        private void ExpireBackgroundReply() {
            if (_backgroundReplyAge.IsRunning && _backgroundReplyAge.ElapsedMilliseconds >= 250) {
                _pending.Clear();
                _backgroundReplyAge.Reset();
            }
        }

        /// <summary>Consumes bounded OSC 4/10/11 RGB replies while retaining unrelated keys and controls.</summary>
        private bool ReadBackgroundReply(string text, int start, out int consumed) {
            string tail = text.Substring(start);
            string header = StartupColourReplies[0];
            foreach (string candidate in StartupColourReplies) {
                if (tail.StartsWith(candidate, StringComparison.Ordinal) || candidate.StartsWith(tail, StringComparison.Ordinal)) { header = candidate; break; }
            }
            int position = start;
            foreach (char expected in header) {
                if (position == text.Length) { goto Pending; }
                if (text[position] != expected) { goto Invalid; }
                position++;
            }
            if (header == StartupColourReplies[0]) {
                int begin = position;
                while (position < text.Length && text[position] >= '0' && text[position] <= '9' && position - begin < 2) { position++; }
                if (position == text.Length) { goto Pending; }
                if (position == begin || text[position] != ';' || !int.TryParse(text.Substring(begin, position - begin), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int index) || index > 15) { goto Invalid; }
                position++;
            }
            foreach (char expected in "rgb:") {
                if (position == text.Length) { goto Pending; }
                if (text[position] != expected) { goto Invalid; }
                position++;
            }
            for (int channel = 0; channel < 3; channel++) {
                int digits = 0;
                while (position < text.Length && IsHex(text[position]) && digits < 4) { position++; digits++; }
                if (position == text.Length) { goto Pending; }
                if (digits == 0) { goto Invalid; }
                if (channel < 2) {
                    if (text[position] != '/') { goto Invalid; }
                    position++;
                } else if (text[position] == '\u0007') {
                    consumed = position - start + 1; _backgroundReplyAge.Reset(); return true;
                } else if (text[position] == '\u001b') {
                    if (position + 1 == text.Length) { goto Pending; }
                    if (text[position + 1] != '\\') { goto Invalid; }
                    consumed = position - start + 2; _backgroundReplyAge.Reset(); return true;
                } else { goto Invalid; }
            }
        Invalid:
            consumed = position - start;
            _backgroundReplyAge.Reset();
            return true;
        Pending:
            consumed = position - start;
            if (!_backgroundReplyAge.IsRunning) { _backgroundReplyAge.Start(); }
            _pending.Append(text.Substring(start));
            return false;
        }

        private static bool IsHex(char value) => value >= '0' && value <= '9' || value >= 'a' && value <= 'f' || value >= 'A' && value <= 'F';
    }
}
