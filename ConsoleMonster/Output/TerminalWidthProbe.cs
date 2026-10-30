using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Output {
    /// <summary>
    /// Measures how wide the current interactive terminal draws emoji, ZWJ sequences, flags, CJK and East Asian Ambiguous
    /// characters, by writing a small sample set and reading the cursor column back from the console screen buffer. A live
    /// session runs it once per process on the alternate screen, before its first frame, so the samples are never visible
    /// in scrollback. It never reads console input: the cursor position comes from GetConsoleScreenBufferInfo, which inside
    /// Windows Terminal (ConPTY) reports the same advances as the terminal's own cursor report.
    /// </summary>
    internal static class TerminalWidthProbe {
        private static readonly object Sync = new object();
        private static readonly string[] Samples = {
            "\U0001F600",
            "\u2764\uFE0F",
            "\u263A",
            "\U0001F468\u200D\U0001F469\u200D\U0001F467",
            "\U0001F1F5\U0001F1F1",
            "\u4E2D",
            "\u25B2",
            "\u2713"
        };

        private static bool _attempted;
        private static bool _success;
        private static TerminalWidthProfile _cached = TerminalWidthProfile.Standard;
        private static string _decision = string.Empty;

        internal static bool HasCachedResult {
            get {
                lock (Sync) {
                    return _attempted;
                }
            }
        }

        /// <summary>Forgets the cached measurement so the next call to <see cref="TryProbe"/> measures again.</summary>
        public static void Invalidate() {
            lock (Sync) {
                _attempted = false;
                _success = false;
                _cached = TerminalWidthProfile.Standard;
                _decision = string.Empty;
            }
        }

        /// <summary>
        /// Probes once per process through <paramref name="writer"/> (the console writer the session draws with); later
        /// calls return the cached outcome. Call it only while the alternate screen is active: the probe writes on the
        /// current row and erases it afterwards.
        /// </summary>
        /// <param name="writer">The console writer; it must write UTF-16 to the console (not a code-page stream).</param>
        /// <param name="timeout">Overall time budget for the samples (about 300 ms is typical; measuring usually takes a few).</param>
        /// <param name="profile">The measured profile, or <see cref="TerminalWidthProfile.Standard"/> when probing failed.</param>
        /// <param name="decision">A human-readable result or skip reason.</param>
        /// <returns>True when the sample set was measured.</returns>
        public static bool TryProbe(ITerminalWriter writer, TimeSpan timeout, out TerminalWidthProfile profile, out string decision) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }
            lock (Sync) {
                if (!_attempted) {
                    _attempted = true;
                    _success = Probe(writer, timeout, out _cached, out _decision);
                }
                profile = _cached;
                decision = _decision;
                return _success;
            }
        }

        internal static TerminalWidthProfile Derive(int emoji, int vs16, int textEmoji, int family, int flag, int cjk, int triangle, int check) {
            int emojiWidth = emoji == 1 ? 1 : 2;
            TerminalWidthProfile result = TerminalWidthProfile.Standard.WithEmojiWidth(emojiWidth);
            if (family >= 2 * emojiWidth) {
                result = result.WithZwjSequences(ZwjRendering.Split);
            }
            result = result.WithFlagWidth(flag > 0 ? flag : 2);
            result = result.WithAmbiguousWidth(triangle == 2 ? 2 : 1);
            return result.WithSource("probe");
        }

        internal static bool CanProbe(bool outputRedirected, bool inputRedirected, bool hasConsole) => !outputRedirected && !inputRedirected && hasConsole;

        private static bool Probe(ITerminalWriter writer, TimeSpan timeout, out TerminalWidthProfile profile, out string decision) {
            profile = TerminalWidthProfile.Standard;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // Elsewhere the cursor position is only available as a terminal reply on stdin, which would compete with
                // the user's keystrokes; the explicit settings or the defaults apply instead.
                decision = "width probe skipped: only supported on Windows consoles (set CONSOLEMONSTER_EMOJI_WIDTH / CONSOLEMONSTER_AMBIGUOUS_WIDTH)";
                return false;
            }
            if (!CanProbe(Console.IsOutputRedirected, Console.IsInputRedirected, writer.IsConsole)) {
                decision = "width probe skipped: console is unavailable or redirected";
                return false;
            }
            int[] advances = new int[Samples.Length];
            Stopwatch budget = Stopwatch.StartNew();
            try {
                for (int i = 0; i < Samples.Length; i++) {
                    if (budget.Elapsed >= timeout) {
                        decision = "width probe timed out after " + i.ToString(CultureInfo.InvariantCulture) + " samples";
                        return false;
                    }
                    writer.Write("\r\u001b[2K" + Samples[i]);
                    writer.Flush();
                    advances[i] = Math.Max(0, Console.CursorLeft);
                }
            } catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ArgumentException) {
                decision = "width probe failed: " + ex.GetType().Name;
                return false;
            } finally {
                writer.Write("\r\u001b[2K\u001b[H");
                writer.Flush();
            }
            profile = Derive(advances[0], advances[1], advances[2], advances[3], advances[4], advances[5], advances[6], advances[7]);
            StringBuilder measured = new StringBuilder();
            for (int i = 0; i < advances.Length; i++) {
                if (i > 0) {
                    measured.Append(',');
                }
                measured.Append(advances[i].ToString(CultureInfo.InvariantCulture));
            }
            decision = "width probe (screen buffer, " + budget.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms): "
                + profile + "; advances emoji,vs16,text,family,flag,cjk,triangle,check = " + measured;
            return true;
        }
    }
}
