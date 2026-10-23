using System;
using System.Collections.Generic;

namespace ConsoleMonster.Widgets {
    public sealed partial class ExceptionView {
        // Runtime-shaped stack lines embedded in Message are separate from Exception.StackTrace.
        // Keep ordinary multiline prose; the explicit detailed view continues to show the original text.
        private static string CompactMessage(string message, bool foldStack, out int hidden) {
            hidden = 0;
            List<string> paragraphs = new List<string>();
            foreach (string line in message.Replace("\r\n", "\n").Split('\n')) {
                string trimmed = line.Trim();
                int arguments = trimmed.IndexOf('(');
                bool frame = line.Length > trimmed.Length && trimmed.StartsWith("at ", StringComparison.Ordinal)
                    && arguments > 3 && trimmed.IndexOf(')') > arguments
                    && trimmed.Substring(3, arguments - 3).IndexOfAny(new[] { ' ', '\t' }) < 0;
                bool boundary = trimmed.StartsWith("--- End of stack trace", StringComparison.Ordinal)
                    || trimmed.StartsWith("--- End of inner exception stack trace", StringComparison.Ordinal);
                if (foldStack && (frame || boundary)) {
                    if (frame) { hidden++; }
                } else { paragraphs.Add(line); }
            }
            return string.Join("\n", paragraphs).TrimEnd();
        }

        private ExceptionInfo? DistinctCause(string outerMessage) {
            ExceptionInfo? result = null;
            for (ExceptionInfo? inner = _info.Inner; inner != null; inner = inner.Inner) {
                string message = CompactMessage(inner.Message, true, out _).Trim();
                if (message.Length > 0 && !string.Equals(message, outerMessage.Trim(), StringComparison.OrdinalIgnoreCase)) {
                    result = inner;
                }
            }
            return result;
        }
    }
}
