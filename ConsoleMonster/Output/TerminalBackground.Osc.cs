using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {
    public static partial class TerminalBackground {
        private static bool _oscAttempted;
        private static TerminalBackgroundInfo? _oscBackground;

        /// <summary>Primes the background before a console source is opened; redirected and explicit sources never query.</summary>
        internal static void ProbeBeforeInput() {
            if (WindowsConsole.IsWindows) { return; }
            ConsoleInput.BeforeFirstRead(() => {
                if (!_oscAttempted) {
                    _oscAttempted = true;
                    try { _oscBackground = QueryBackground(); }
                    catch (Exception) { /* Optional detection must never prevent opening the keyboard. */ }
                    if (_oscBackground != null) { Terminal.RefreshBackground(); }
                }
            });
        }

        private static TerminalBackgroundInfo? QueryBackground() {
            string? term = Environment.GetEnvironmentVariable("TERM");
            if (WindowsConsole.IsWindows || Console.IsInputRedirected || Console.IsOutputRedirected || string.IsNullOrEmpty(term) || term == "dumb"
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"))) { return null; }
            IntPtr saved = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity);
            IntPtr raw = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity);
            bool changed = false;
            StringBuilder received = new StringBuilder();
            try {
                if (UnixInputNative.GetAttributes(0, saved) != 0) { return null; }
                byte[] mode = new byte[UnixInputNative.AttributesCapacity];
                Marshal.Copy(saved, mode, 0, mode.Length);
                Marshal.Copy(mode, 0, raw, mode.Length);
                UnixInputNative.MakeRaw(raw);
                if (UnixInputNative.SetAttributes(0, 0, raw) != 0) { return null; }
                changed = true;
                byte[] query = Encoding.ASCII.GetBytes("\u001b]11;?\u0007");
                using (System.IO.Stream output = Console.OpenStandardOutput()) {
                    output.Write(query, 0, query.Length);
                    output.Flush();
                }
                Stopwatch budget = Stopwatch.StartNew();
                byte[] one = new byte[1];
                while (budget.ElapsedMilliseconds < 100 && received.Length < 1024) {
                    if (!UnixInputNative.Ready(0)) { Thread.Sleep(1); continue; }
                    if (UnixInputNative.Read(0, one, new UIntPtr(1)).ToInt64() != 1) { break; }
                    received.Append((char)one[0]);
                    string text = received.ToString();
                    int start = text.IndexOf("\u001b]11;", StringComparison.Ordinal);
                    if (start < 0) { continue; }
                    int end = text.IndexOf('\u0007', start);
                    int terminator = 1;
                    if (end < 0) { end = text.IndexOf("\u001b\\", start, StringComparison.Ordinal); terminator = 2; }
                    if (end < 0) { continue; }
                    string reply = text.Substring(start, end + terminator - start);
                    received.Remove(start, reply.Length);
                    return TryParseOsc11(reply, out TerminalBackgroundInfo info) ? info : null;
                }
                return null;
            } finally {
                if (changed) { UnixInputNative.SetAttributes(0, 0, saved); }
                // Preserve bytes typed during the query, including a reply fragment for the VT decoder to discard.
                foreach (char value in received.ToString()) { UnixInputNative.PendingInput.Enqueue((byte)value); }
                Marshal.FreeHGlobal(raw);
                Marshal.FreeHGlobal(saved);
            }
        }

        /// <summary>Parses xterm OSC 11 RGB replies with BEL or ST and one to four hex digits per channel.</summary>
        internal static bool TryParseOsc11(string reply, out TerminalBackgroundInfo info) {
            info = null!;
            const string prefix = "\u001b]11;rgb:";
            if (!reply.StartsWith(prefix, StringComparison.Ordinal)) { return false; }
            int terminator = reply.EndsWith("\u0007", StringComparison.Ordinal) ? 1 : reply.EndsWith("\u001b\\", StringComparison.Ordinal) ? 2 : 0;
            if (terminator == 0) { return false; }
            string[] channels = reply.Substring(prefix.Length, reply.Length - prefix.Length - terminator).Split('/');
            if (channels.Length != 3) { return false; }
            byte[] rgb = new byte[3];
            for (int i = 0; i < 3; i++) {
                string channel = channels[i];
                if (channel.Length < 1 || channel.Length > 4 || !uint.TryParse(channel, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value)) { return false; }
                rgb[i] = (byte)(value * 255 / ((1u << (channel.Length * 4)) - 1));
            }
            double luminance = CalculateLuminance(rgb[0], rgb[1], rgb[2]);
            info = new TerminalBackgroundInfo(luminance > 0.5, true, rgb[0], rgb[1], rgb[2], luminance, "OSC 11", "background queried before console input");
            return true;
        }
    }
}
