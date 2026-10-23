using System;
using System.Text;

namespace ConsoleMonster.Output {
    /// <summary>Shares log-file BOM detection between appenders and incrementally decoded tails.</summary>
    internal static class LogEncodingPreamble {
        private static readonly Encoding[] Encodings = {
            new UTF32Encoding(false, true), new UTF32Encoding(true, true), new UTF8Encoding(true),
            new UnicodeEncoding(false, true), new UnicodeEncoding(true, true)
        };

        // A complete UTF-16 LE BOM is also a prefix of UTF-32 LE. Live readers wait for enough bytes to distinguish them;
        // static-file probes can decide from their completed snapshot. True also means a decided absence of any BOM.
        internal static bool TrySelect(byte[] bytes, int count, bool final, out Encoding? encoding, out int length) {
            encoding = null;
            length = 0;
            foreach (Encoding candidate in Encodings) {
                byte[] preamble = candidate.GetPreamble();
                bool matches = true;
                for (int i = 0; i < Math.Min(count, preamble.Length); i++) {
                    if (bytes[i] != preamble[i]) { matches = false; break; }
                }
                if (!matches) { continue; }
                if (count < preamble.Length) { if (!final) { return false; } continue; }
                encoding = (Encoding)candidate.Clone();
                length = preamble.Length;
                return true;
            }
            return true;
        }
    }
}
