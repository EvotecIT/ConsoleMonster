using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Maps rendered cell colours to the 16 <see cref="ConsoleColor"/> values a console API or a PowerShell host can show (Console.ForegroundColor, Write-Host -ForegroundColor). Theme roles map by
    /// meaning (pass green, fail red, warn yellow, muted dark grey, accent cyan; darker variants on the light theme), plain
    /// text keeps the host's own colour, and anything else falls back to the nearest of the 16 colours.
    /// </summary>
    public sealed class ConsoleColorMap {
        // ANSI order (black, red, green, yellow, blue, magenta, cyan, white, then the bright set) to ConsoleColor.
        private static readonly ConsoleColor[] AnsiToConsole = {
            ConsoleColor.Black, ConsoleColor.DarkRed, ConsoleColor.DarkGreen, ConsoleColor.DarkYellow,
            ConsoleColor.DarkBlue, ConsoleColor.DarkMagenta, ConsoleColor.DarkCyan, ConsoleColor.Gray,
            ConsoleColor.DarkGray, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Yellow,
            ConsoleColor.Blue, ConsoleColor.Magenta, ConsoleColor.Cyan, ConsoleColor.White
        };

        private readonly Dictionary<Color, ConsoleColor?> _roles = new Dictionary<Color, ConsoleColor?>();

        /// <summary>Creates a map for a render context.</summary>
        /// <param name="context">The context the cells were rendered with.</param>
        /// <param name="enabled">False when colour is off (NO_COLOR, CONSOLEMONSTER_COLOR=none, plain mode).</param>
        public ConsoleColorMap(RenderContext context, bool enabled = true) {
            Enabled = enabled;
            if (!enabled) {
                return;
            }
            bool light = ReferenceEquals(context.Theme, Theme.Light);
            // Text-like roles keep the host's colour, so output reads well on any background.
            Add(context, ThemeToken.Text, null);
            Add(context, ThemeToken.Title, null);
            Add(context, ThemeToken.Header, null);
            Add(context, ThemeToken.Highlight, null);
            Add(context, ThemeToken.Pass, light ? ConsoleColor.DarkGreen : ConsoleColor.Green);
            Add(context, ThemeToken.Fail, light ? ConsoleColor.DarkRed : ConsoleColor.Red);
            Add(context, ThemeToken.Warn, light ? ConsoleColor.DarkYellow : ConsoleColor.Yellow);
            Add(context, ThemeToken.Info, light ? ConsoleColor.DarkCyan : ConsoleColor.Cyan);
            Add(context, ThemeToken.Accent, light ? ConsoleColor.DarkCyan : ConsoleColor.Cyan);
            Add(context, ThemeToken.Accent2, light ? ConsoleColor.DarkMagenta : ConsoleColor.Magenta);
            Add(context, ThemeToken.Running, light ? ConsoleColor.DarkCyan : ConsoleColor.Cyan);
            Add(context, ThemeToken.Muted, ConsoleColor.DarkGray);
            Add(context, ThemeToken.Subtle, ConsoleColor.DarkGray);
            Add(context, ThemeToken.Skip, ConsoleColor.DarkGray);
            Add(context, ThemeToken.Pending, ConsoleColor.DarkGray);
            Add(context, ThemeToken.Border, ConsoleColor.DarkGray);
            Add(context, ThemeToken.Series1, light ? ConsoleColor.DarkCyan : ConsoleColor.Cyan);
            Add(context, ThemeToken.Series2, light ? ConsoleColor.DarkMagenta : ConsoleColor.Magenta);
            Add(context, ThemeToken.Series3, light ? ConsoleColor.DarkYellow : ConsoleColor.Yellow);
            Add(context, ThemeToken.Series4, light ? ConsoleColor.DarkGreen : ConsoleColor.Green);
            Add(context, ThemeToken.Series5, light ? ConsoleColor.DarkBlue : ConsoleColor.Blue);
            Add(context, ThemeToken.Series6, light ? ConsoleColor.DarkRed : ConsoleColor.Red);
        }

        /// <summary>Whether colours are mapped at all.</summary>
        public bool Enabled { get; }

        /// <summary>The host colour for a cell foreground, or null for the host's default.</summary>
        /// <param name="color">The cell colour.</param>
        /// <returns>The console colour, or null.</returns>
        public ConsoleColor? Foreground(Color color) {
            if (!Enabled || color.IsDefault) {
                return null;
            }
            return _roles.TryGetValue(color, out ConsoleColor? role) ? role : Nearest(color);
        }

        /// <summary>The host colour for a cell background, or null for the host's default.</summary>
        /// <param name="color">The cell colour.</param>
        /// <returns>The console colour, or null.</returns>
        public ConsoleColor? Background(Color color) {
            if (!Enabled || color.IsDefault) {
                return null;
            }
            return Nearest(color);
        }

        /// <summary>Whether colour is off for host output: NO_COLOR (unless CONSOLEMONSTER_COLOR names a depth), CONSOLEMONSTER_COLOR=none, or plain mode.</summary>
        /// <param name="profile">The detected profile.</param>
        /// <returns>True when host output must stay uncoloured.</returns>
        public static bool ColorDisabled(TerminalProfile profile) {
            if (profile.Mode == OutputMode.Plain) {
                return true;
            }
            string setting = (Environment.GetEnvironmentVariable("CONSOLEMONSTER_COLOR") ?? string.Empty).Trim().ToLowerInvariant();
            if (setting == "none") {
                return true;
            }
            if (setting == "16" || setting == "256" || setting == "truecolor" || setting == "24bit") {
                return false;
            }
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        }

        private void Add(RenderContext context, ThemeToken token, ConsoleColor? color) {
            Color foreground = context.Style(token).Color;
            if (!foreground.IsDefault && !_roles.ContainsKey(foreground)) {
                _roles[foreground] = color;
            }
        }

        private static ConsoleColor Nearest(Color color) {
            Color ansi = color.Downsample(ColorDepth.Ansi16);
            return ansi.Kind == ColorKind.Ansi16 ? AnsiToConsole[ansi.Index & 15] : ConsoleColor.Gray;
        }
    }
}
