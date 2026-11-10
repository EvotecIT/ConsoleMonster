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

        private static TerminalBackgroundInfo? QueryBackground() {
            TerminalBackgroundInfo? result = null;
            TerminalColorQuery.Read("\u001b]11;?\u0007", reply => {
                if (!TryParseOsc11(reply, out TerminalBackgroundInfo info)) { return false; }
                result = info; return true;
            }, () => result != null);
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
            info = new TerminalBackgroundInfo(luminance > 0.5, true, rgb[0], rgb[1], rgb[2], luminance, "OSC 11", "background queried before console input");
            return true;
        }
    }
}
