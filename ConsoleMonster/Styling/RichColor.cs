using System;
using System.Globalization;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// A colour for rich text: an explicit <see cref="Styling.Color"/> (hex, RGB, 256-colour index, palette name), a
    /// <see cref="ConsoleColor"/> exactly as PowerShell shows it, or a theme role (Pass, Fail, Warn, Info, Muted, Accent...)
    /// that is resolved when the text is rendered, so it follows the light or dark theme.
    /// </summary>
    /// <remarks>
    /// ConsoleColor names map the way Write-Host shows them, not by name: PowerShell's <c>DarkRed</c> is ANSI red (index 1)
    /// and <c>Red</c> is bright red (index 9); <c>Gray</c> is index 7 and <c>DarkGray</c> index 8. They are emitted as the
    /// 16-colour codes, so the terminal's own palette shows them exactly like Write-Host -ForegroundColor.
    /// </remarks>
    public readonly struct RichColor : IEquatable<RichColor> {
        // Markup names for the 16 ANSI indices (Spectre-compatible), so a ConsoleColor serialises without changing meaning.
        private static readonly string[] AnsiNames = { "black", "maroon", "green", "olive", "navy", "purple", "teal", "silver", "grey", "red", "lime", "yellow", "blue", "fuchsia", "aqua", "white" };

        private readonly int _role; // ThemeToken + 1, or 0 for none.

        private RichColor(Color color, ThemeToken? role) {
            Color = color;
            _role = role.HasValue ? (int)role.Value + 1 : 0;
        }

        /// <summary>No colour: the surrounding or terminal default.</summary>
        public static RichColor Default => default;

        /// <summary>The explicit colour (default when this is a role).</summary>
        public Color Color { get; }

        /// <summary>The theme role, or null for an explicit colour.</summary>
        public ThemeToken? Role => _role == 0 ? (ThemeToken?)null : (ThemeToken)(_role - 1);

        /// <summary>Whether no colour is set.</summary>
        public bool IsDefault => _role == 0 && Color.IsDefault;

        /// <summary>An explicit colour.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>The rich colour.</returns>
        public static RichColor From(Color color) => new RichColor(color, null);

        /// <summary>A theme role, resolved at render time.</summary>
        /// <param name="role">The role.</param>
        /// <returns>The rich colour.</returns>
        public static RichColor FromRole(ThemeToken role) => new RichColor(Color.Default, role);

        /// <summary>A ConsoleColor exactly as PowerShell shows it (see the remarks).</summary>
        /// <param name="color">The console colour.</param>
        /// <returns>The rich colour.</returns>
        public static RichColor FromConsoleColor(ConsoleColor color) => new RichColor(ColorNames.FromConsole(color), null);

        /// <summary>A 24-bit colour.</summary>
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        /// <returns>The rich colour.</returns>
        public static RichColor FromRgb(byte r, byte g, byte b) => new RichColor(Color.FromRgb(r, g, b), null);

        /// <summary>Converts a ConsoleColor (PowerShell mapping).</summary>
        /// <param name="color">The console colour.</param>
        public static implicit operator RichColor(ConsoleColor color) => FromConsoleColor(color);

        /// <summary>Converts an explicit colour.</summary>
        /// <param name="color">The colour.</param>
        public static implicit operator RichColor(Color color) => From(color);

        /// <summary>Converts a theme role.</summary>
        /// <param name="role">The role.</param>
        public static implicit operator RichColor(ThemeToken role) => FromRole(role);

        /// <summary>
        /// Parses a colour: a ConsoleColor name with the PowerShell mapping (<c>Red</c>, <c>DarkCyan</c>; <c>grey</c> and
        /// <c>darkgrey</c> spell Gray and DarkGray), a theme role (<c>Pass</c>, <c>Muted</c>), <c>#RRGGBB</c>,
        /// <c>rgb(r,g,b)</c>, <c>r,g,b</c>, a 256-colour index, or a markup palette name (<c>orange1</c>, <c>cadetblue</c>).
        /// <c>palette.darkred</c> asks for the palette colour where a bare name would be the ConsoleColor.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="color">The colour.</param>
        /// <returns>True when the text is a colour.</returns>
        public static bool TryParse(string? text, out RichColor color) {
            color = Default;
            string value = (text ?? string.Empty).Trim();
            if (value.Length == 0) {
                return false;
            }
            bool word = char.IsLetter(value[0]);
            if (word && ColorNames.TryGetConsole(value, out Color console)) {
                color = From(console);
                return true;
            }
            if (word && Enum.TryParse(value, true, out ThemeToken role) && Enum.IsDefined(typeof(ThemeToken), role)) {
                color = FromRole(role);
                return true;
            }
            string[] parts = value.Split(',');
            if (parts.Length == 3
                && byte.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r)
                && byte.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g)
                && byte.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b)) {
                color = FromRgb(r, g, b);
                return true;
            }
            if (Color.TryParse(value, out Color parsed) && !parsed.IsDefault) {
                color = From(parsed);
                return true;
            }
            return false;
        }

        /// <summary>Parses a colour (see <see cref="TryParse"/>).</summary>
        /// <param name="text">The text.</param>
        /// <returns>The colour.</returns>
        /// <exception cref="FormatException">The text is not a colour.</exception>
        public static RichColor Parse(string text) {
            if (TryParse(text, out RichColor color)) {
                return color;
            }
            throw new FormatException("'" + text + "' is not a colour. Use a ConsoleColor name, a theme role (Pass, Fail, Warn, Info, Muted, Accent), #RRGGBB, rgb(r,g,b) or r,g,b.");
        }

        /// <summary>The 24-bit value of the colour (roles resolved with <paramref name="theme"/>), for gradients and blending.</summary>
        /// <param name="theme">The theme for roles, or null for the dark theme.</param>
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        /// <returns>False for the default colour.</returns>
        public bool TryGetRgb(ITheme? theme, out byte r, out byte g, out byte b) {
            Color color = Color;
            if (Role.HasValue) {
                color = (theme ?? Theme.Dark).Resolve(Role.Value, Capabilities.ColorDepth.TrueColor).Foreground;
            }
            r = g = b = 0;
            switch (color.Kind) {
                case ColorKind.Rgb:
                    r = color.R;
                    g = color.G;
                    b = color.B;
                    return true;
                case ColorKind.Ansi16:
                    AnsiPalette.Get16(color.Index, out r, out g, out b);
                    return true;
                case ColorKind.Ansi256:
                    AnsiPalette.Get256(color.Index, out r, out g, out b);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>The colour as a markup word (<c>pass</c>, <c>lime</c>, <c>color(214)</c>, <c>#ff8800</c>), or empty for the default.</summary>
        /// <returns>The markup word.</returns>
        public string ToMarkup() {
            if (Role.HasValue) {
                return Role.Value.ToString().ToLowerInvariant();
            }
            switch (Color.Kind) {
                case ColorKind.Ansi16:
                    return AnsiNames[Color.Index & 15];
                case ColorKind.Ansi256:
                    return "color(" + Color.Index.ToString(CultureInfo.InvariantCulture) + ")";
                case ColorKind.Rgb:
                    return "#" + Color.R.ToString("x2", CultureInfo.InvariantCulture) + Color.G.ToString("x2", CultureInfo.InvariantCulture) + Color.B.ToString("x2", CultureInfo.InvariantCulture);
                default:
                    return string.Empty;
            }
        }

        /// <inheritdoc />
        public bool Equals(RichColor other) => _role == other._role && Color.Equals(other.Color);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is RichColor other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (Color.GetHashCode() * 397) ^ _role;

        /// <summary>Equality.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>True when equal.</returns>
        public static bool operator ==(RichColor left, RichColor right) => left.Equals(right);

        /// <summary>Inequality.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>True when different.</returns>
        public static bool operator !=(RichColor left, RichColor right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() => IsDefault ? "default" : ToMarkup();
    }
}
