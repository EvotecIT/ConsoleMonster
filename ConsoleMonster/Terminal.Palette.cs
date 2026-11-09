using ConsoleMonster.Styling;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    public static partial class Terminal {
        /// <summary>The terminal's RGB palette and defaults, queried once before input starts; null if redirected, unsupported or timed out.</summary>
        /// <remarks>The guarded startup exchange uses OSC 4 for sixteen colours, OSC 10 for foreground and OSC 11 for background, sharing a 100 ms deadline.</remarks>
        public static TerminalScheme? Palette => TerminalPalette.Scheme;

        internal static bool HasAssignedTerminalTheme {
            get { lock (Gate) { return _theme is Styling.Theme theme && theme.FollowsTerminal; } }
        }

        internal static string PaletteDecision => TerminalPalette.Current == null
            ? "terminal palette: unavailable; exports use the background variant defaults (OSC 4/10/11 was guarded, incomplete or timed out)"
            : "terminal palette: OSC 4/10/11; exports use the terminal's sixteen colours and defaults";
    }
}
