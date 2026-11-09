using System;
using System.Globalization;
using System.Text;
using ConsoleMonster.Input;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Output {
    internal static class TerminalPalette {
        private static bool _attempted;
        private static volatile TerminalScheme? _scheme;
        internal static TerminalScheme? Current => _scheme;
        internal static TerminalScheme? Scheme { get { ProbeBeforeInput(); return _scheme; } }

        internal static void ProbeBeforeInput() {
            TerminalBackground.ProbeBeforeInput();
            ConsoleInput.BeforeFirstRead(() => {
                if (_attempted) { return; }
                _attempted = true;
                try {
                    Color?[] palette = new Color?[16];
                    TerminalBackgroundInfo? knownBackground = TerminalBackground.OscBackground;
                    Color? foreground = null, background = knownBackground == null ? (Color?)null : Color.FromRgb(knownBackground.Red, knownBackground.Green, knownBackground.Blue);
                    int count = 0;
                    StringBuilder query = new StringBuilder();
                    for (int i = 0; i < 16; i++) { query.Append("\u001b]4;").Append(i.ToString(CultureInfo.InvariantCulture)).Append(";?\u0007"); }
                    query.Append("\u001b]10;?\u0007");
                    TerminalColorQuery.Read(query.ToString(), reply => {
                        if (TryParseOsc4(reply, out int index, out Color color)) {
                            if (!palette[index].HasValue) { count++; }
                            palette[index] = color; return true;
                        }
                        if (reply.StartsWith("\u001b]10;", StringComparison.Ordinal) && TerminalBackground.TryParseOsc11("\u001b]11;" + reply.Substring(5), out TerminalBackgroundInfo fg)) {
                            foreground = Color.FromRgb(fg.Red, fg.Green, fg.Blue); return true;
                        }
                        if (TerminalBackground.TryParseOsc11(reply, out TerminalBackgroundInfo bg)) { background = Color.FromRgb(bg.Red, bg.Green, bg.Blue); return true; }
                        return false;
                    }, () => count == 16 && foreground.HasValue && background.HasValue);
                    if (count == 16 && foreground.HasValue && background.HasValue) {
                        Color[] colors = new Color[16];
                        for (int i = 0; i < 16; i++) { colors[i] = palette[i]!.Value; }
                        _scheme = new TerminalScheme("Terminal", colors, foreground.Value, background.Value);
                    }
                } catch (Exception) { /* Palette detection is optional and never prevents console input. */ }
            });
        }

        internal static bool TryParseOsc4(string reply, out int index, out Color color) {
            index = -1; color = Color.Default;
            const string prefix = "\u001b]4;";
            if (!reply.StartsWith(prefix, StringComparison.Ordinal)) { return false; }
            int semicolon = reply.IndexOf(';', prefix.Length);
            if (semicolon < 0 || !int.TryParse(reply.Substring(prefix.Length, semicolon - prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out index)
                || index < 0 || index > 15 || !TerminalBackground.TryParseOsc11("\u001b]11;" + reply.Substring(semicolon + 1), out TerminalBackgroundInfo rgb)) { return false; }
            color = Color.FromRgb(rgb.Red, rgb.Green, rgb.Blue); return true;
        }
    }
}
