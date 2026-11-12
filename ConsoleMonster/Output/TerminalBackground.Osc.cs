using System;
using System.Globalization;
using ConsoleMonster.Input;

namespace ConsoleMonster.Output {
    public static partial class TerminalBackground {
        private static bool _oscAttempted;
        private static TerminalBackgroundInfo? _oscBackground;
        internal static TerminalBackgroundInfo? OscBackground => _oscBackground;

        /// <summary>Primes the background before a console source is opened; redirected and explicit sources never query.</summary>
        internal static void ProbeBeforeInput() {
            if (ConsoleMonster.Interop.WindowsConsole.IsWindows) { return; }
            ConsoleInput.BeforeFirstRead(() => {
                if (!_oscAttempted) {
                    _oscAttempted = true;
                    try { _oscBackground = QueryBackground(); }
                    catch (Exception) { /* Optional detection must never prevent opening the keyboard. */ }
                    if (_oscBackground != null) { Terminal.RefreshBackground(); }
                }
            });
        }

        /// <summary>
        /// Queries the background once for output that never reads keys (tables, lists, charts) when the automatic theme
        /// needs it and no other signal answered. Only on an interactive Unix terminal whose input nobody is reading and
        /// that can take typed keys back; the exchange is fenced and bounded, so an unanswered query costs at most one short
        /// wait per process.
        /// </summary>
        /// <returns>Whether this call learned the background.</returns>
        internal static bool ProbeForOutput() {
            if (ConsoleMonster.Interop.WindowsConsole.IsWindows || _oscAttempted || TryParseColorFgbg(Environment.GetEnvironmentVariable("COLORFGBG"), out _)) { return false; }
            // Keys typed during the exchange must reach the shell. Where the terminal can't take them back, only the key
            // reader asks (it owns the input), and later output reuses its answer.
            if (!ConsoleMonster.Interop.UnixInputNative.CanReturnInput()) { return false; }
            bool learned = false;
            ConsoleInput.BeforeFirstRead(() => {
                if (_oscAttempted) { return; }
                _oscAttempted = true;
                try { _oscBackground = QueryBackground(fence: true); }
                catch (Exception) { /* Optional detection must never prevent output. */ }
                learned = _oscBackground != null;
            });
            return learned;
        }

        private static TerminalBackgroundInfo? QueryBackground(bool fence = false) {
            TerminalBackgroundInfo? result = null;
            TerminalColorQuery.Read("\u001b]11;?\u0007", reply => {
                if (!TryParseOsc11(reply, out TerminalBackgroundInfo info)) { return false; }
                result = info; return true;
            }, () => result != null, fence);
            return result;
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
            info = new TerminalBackgroundInfo(luminance > 0.5, true, rgb[0], rgb[1], rgb[2], luminance, "OSC 11", "background queried from the terminal");
            return true;
        }
    }
}
