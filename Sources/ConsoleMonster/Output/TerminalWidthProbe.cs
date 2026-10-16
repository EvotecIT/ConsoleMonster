using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Interop;
using ConsoleMonster.Text;

namespace ConsoleMonster.Output {
    /// <summary>
    /// Measures how wide the current interactive terminal draws emoji, ZWJ sequences, flags, CJK and East Asian Ambiguous
    /// characters, by writing a small sample set and reading the cursor column back. A live session runs it once per
    /// process on the alternate screen, before its first frame, so the samples are never visible in scrollback.
    /// </summary>
    public static class TerminalWidthProbe {
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

        // Each cursor report gets a short wait; a terminal that does not answer the first one is measured through the
        // screen buffer for the rest of the samples instead of exhausting the whole budget.
        private static readonly TimeSpan SampleWait = TimeSpan.FromMilliseconds(120);

        private static bool _attempted;
        private static bool _success;
        private static TerminalWidthProfile _cached = TerminalWidthProfile.Standard;
        private static string _decision = string.Empty;

        /// <summary>
        /// Probes once per process through <paramref name="writer"/> (the console writer the session draws with); later
        /// calls return the cached outcome. Call it only while the alternate screen is active: the probe writes on the
        /// current row and erases it afterwards.
        /// </summary>
        /// <param name="writer">The console writer; it must write UTF-16 to the console (not a code-page stream).</param>
        /// <param name="timeout">Overall time budget for the samples (about 300 ms is typical).</param>
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
            if (!CanProbe(Console.IsOutputRedirected, Console.IsInputRedirected, writer.IsConsole)) {
                decision = "width probe skipped: console is unavailable or redirected";
                return false;
            }
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                // Reading a cursor report portably would consume pending keystrokes; outside Windows the explicit
                // CONSOLEMONSTER_EMOJI_WIDTH / CONSOLEMONSTER_AMBIGUOUS_WIDTH settings or the defaults apply.
                decision = "width probe skipped: only supported on Windows consoles (set CONSOLEMONSTER_EMOJI_WIDTH / CONSOLEMONSTER_AMBIGUOUS_WIDTH)";
                return false;
            }
            bool windows = true;
            // Under Windows Terminal the cursor report comes from the terminal side of ConPTY; classic conhost answers
            // through its own screen buffer, which GetConsoleScreenBufferInfo (Console.CursorLeft) reads directly.
            bool dsr = Environment.GetEnvironmentVariable("WT_SESSION") != null;
            bool unanswered = false;
            int[] advances = new int[Samples.Length];
            Stopwatch budget = Stopwatch.StartNew();
            string method = dsr ? "cursor report" : "screen buffer";
            // Without VT input mode, conhost (also inside Windows Terminal's ConPTY) consumes cursor position reports
            // itself, so the reply never reaches the application.
            IntPtr input = Interop.WindowsConsole.StandardInput;
            bool restoreInput = false;
            uint inputMode = 0;
            if (dsr && windows && Interop.WindowsConsole.TryGetMode(input, out inputMode)) {
                restoreInput = Interop.WindowsConsole.TrySetMode(input, inputMode | Interop.WindowsConsole.EnableVirtualTerminalInput);
            }
            try {
                for (int i = 0; i < Samples.Length; i++) {
                    if (budget.Elapsed >= timeout) {
                        decision = "width probe timed out after " + i.ToString(CultureInfo.InvariantCulture) + " samples";
                        return false;
                    }
                    writer.Write("\r\u001b[2K");
                    writer.Flush();
                    if (dsr && !InputQueueIsEmpty()) {
                        // Pending input (even a key release) would sit in front of the reply; measure through the
                        // screen buffer instead of touching the queue.
                        dsr = false;
                        method = "screen buffer (input pending)";
                    }
                    if (dsr) {
                        writer.Write(Samples[i] + "\u001b[6n");
                        writer.Flush();
                        TimeSpan wait = timeout - budget.Elapsed;
                        if (wait > SampleWait) {
                            wait = SampleWait;
                        }
                        if (ReadCursorColumn(wait, out int column)) {
                            advances[i] = Math.Max(0, column - 1);
                        } else if (windows) {
                            dsr = false;
                            unanswered = true;
                            method = "screen buffer (no cursor report)";
                            advances[i] = Math.Max(0, Console.CursorLeft);
                        } else {
                            decision = "width probe: the terminal did not answer the cursor position request";
                            return false;
                        }
                    } else {
                        writer.Write(Samples[i]);
                        writer.Flush();
                        advances[i] = Math.Max(0, Console.CursorLeft);
                    }
                }
            } catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ArgumentException) {
                decision = "width probe failed: " + ex.GetType().Name;
                return false;
            } finally {
                if (unanswered && restoreInput) {
                    // A reply that arrives late must not reach the application as typed text.
                    ReadCursorColumn(TimeSpan.FromMilliseconds(100), out _);
                }
                if (restoreInput) {
                    Interop.WindowsConsole.TrySetMode(input, inputMode);
                }
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
            decision = "width probe (" + method + ", " + budget.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms): "
                + profile + "; advances emoji,vs16,text,family,flag,cjk,triangle,check = " + measured;
            return true;
        }

        // Removes a cursor position report ("ESC [ row ; col R") from the console input queue only when it is at the very
        // front: the records are inspected with PeekConsoleInput and exactly the reply's records are read. Unrelated input
        // is never read, reordered or written back. If anything else is in front of the reply, the probe gives up on cursor
        // reports for this run (the caller falls back to the screen buffer); in that rare race the reply stays queued
        // behind the user's keys and may reach the application as text, but no keystroke is lost or reordered.
        private static bool ReadCursorColumn(TimeSpan timeout, out int column) {
            column = 0;
            if (timeout <= TimeSpan.Zero) {
                return false;
            }
            IntPtr input = Interop.WindowsConsole.StandardInput;
            ConsoleInputRecord[] buffer = new ConsoleInputRecord[64];
            Stopwatch timer = Stopwatch.StartNew();
            while (timer.Elapsed < timeout) {
                int count = Interop.WindowsConsole.PeekInput(input, buffer);
                if (count > 0) {
                    int length = FrontReplyLength(buffer, count, out column);
                    if (length < 0) {
                        return false;
                    }
                    if (length > 0) {
                        ConsoleInputRecord[] reply = new ConsoleInputRecord[length];
                        return Interop.WindowsConsole.ReadInput(input, reply, length) == length;
                    }
                }
                Thread.Sleep(1);
            }
            return false;
        }

        private static bool InputQueueIsEmpty() {
            ConsoleInputRecord[] one = new ConsoleInputRecord[1];
            return Interop.WindowsConsole.PeekInput(Interop.WindowsConsole.StandardInput, one) == 0;
        }

        /// <summary>
        /// Measures a cursor position report at the front of the input queue. Returns the number of records that form
        /// the complete reply (its key presses and releases, starting at the first record), 0 when the front is still an
        /// incomplete reply, or -1 when the front record is not part of a reply (unrelated input that must not be read).
        /// </summary>
        internal static int FrontReplyLength(ConsoleInputRecord[] records, int count, out int column) {
            column = 0;
            if (count <= 0) {
                return 0;
            }
            if (!records[0].IsKeyDown || records[0].UnicodeChar != '\u001b') {
                return -1;
            }
            bool[] drop = new bool[count];
            if (!FindCursorReport(records, count, drop, out column)) {
                // Still arriving, or something unrelated is interleaved: wait only while every key press so far fits.
                StringBuilder prefix = new StringBuilder();
                for (int i = 0; i < count; i++) {
                    if (!records[i].IsKeyDown) {
                        continue;
                    }
                    char c = records[i].UnicodeChar;
                    bool fits = prefix.Length == 0 ? c == '\u001b' : prefix.Length == 1 ? c == '[' : char.IsDigit(c) || c == ';';
                    if (!fits) {
                        return -1;
                    }
                    prefix.Append(c);
                }
                return 0;
            }
            int length = 0;
            while (length < count && drop[length]) {
                length++;
            }
            for (int i = length; i < count; i++) {
                if (drop[i]) {
                    // Part of the reply sits behind an unrelated record: leave the queue alone.
                    column = 0;
                    return -1;
                }
            }
            return length;
        }

        /// <summary>
        /// Finds the first cursor position report among input records and marks the records that belong to it (the
        /// key-down characters and their key-up partners); every other record is left unmarked.
        /// </summary>
        internal static bool FindCursorReport(ConsoleInputRecord[] records, int count, bool[] drop, out int column) {
            column = 0;
            for (int start = 0; start < count; start++) {
                if (!records[start].IsKeyDown || records[start].UnicodeChar != '\u001b') {
                    continue;
                }
                StringBuilder reply = new StringBuilder();
                List<int> members = new List<int>();
                for (int i = start; i < count; i++) {
                    if (!records[i].IsKey) {
                        continue;
                    }
                    char c = records[i].UnicodeChar;
                    if (!records[i].IsKeyDown) {
                        if (reply.Length > 0 && reply.ToString().IndexOf(c) >= 0) {
                            members.Add(i);
                        }
                        continue;
                    }
                    bool expected = reply.Length == 0 ? c == '\u001b' : reply.Length == 1 ? c == '[' : char.IsDigit(c) || c == ';' || c == 'R';
                    if (!expected) {
                        break;
                    }
                    reply.Append(c);
                    members.Add(i);
                    if (c == 'R') {
                        if (!TryParseReply(reply.ToString(), out column)) {
                            break;
                        }
                        if (i + 1 < count && records[i + 1].IsKey && !records[i + 1].IsKeyDown && records[i + 1].UnicodeChar == 'R') {
                            members.Add(i + 1);
                        }
                        foreach (int member in members) {
                            drop[member] = true;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool TryParseReply(string reply, out int column) {
            column = 0;
            int bracket = reply.IndexOf('[');
            int separator = reply.IndexOf(';');
            int end = reply.LastIndexOf('R');
            if (bracket < 0 || separator <= bracket || end <= separator) {
                return false;
            }
            return int.TryParse(reply.Substring(separator + 1, end - separator - 1), NumberStyles.None, CultureInfo.InvariantCulture, out column) && column > 0;
        }
    }
}
