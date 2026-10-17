using System;
using System.Globalization;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// A terminal colour: the default colour, a 16- or 256-palette index, or 24-bit RGB.
    /// Colours are downsampled to the profile's <see cref="ColorDepth"/> at encoding time.
    /// </summary>
    public readonly struct Color : IEquatable<Color> {
        private Color(ColorKind kind, byte index, byte r, byte g, byte b) {
            Kind = kind;
            Index = index;
            R = r;
            G = g;
            B = b;
        }

        /// <summary>The terminal's default colour.</summary>
        public static Color Default => default;

        /// <summary>How the colour is expressed.</summary>
        public ColorKind Kind { get; }

        /// <summary>Palette index for <see cref="ColorKind.Ansi16"/> and <see cref="ColorKind.Ansi256"/>.</summary>
        public byte Index { get; }

        /// <summary>Red component for <see cref="ColorKind.Rgb"/>.</summary>
        public byte R { get; }

        /// <summary>Green component for <see cref="ColorKind.Rgb"/>.</summary>
        public byte G { get; }

        /// <summary>Blue component for <see cref="ColorKind.Rgb"/>.</summary>
        public byte B { get; }

        /// <summary>Whether this is the terminal default.</summary>
        public bool IsDefault => Kind == ColorKind.Default;

        /// <summary>Creates a 24-bit colour.</summary>
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        /// <returns>The colour.</returns>
        public static Color FromRgb(byte r, byte g, byte b) => new Color(ColorKind.Rgb, 0, r, g, b);

        /// <summary>Creates one of the 16 standard colours.</summary>
        /// <param name="index">Index 0-15 (0 black, 1 maroon, 2 green, 3 olive, 4 navy, 5 purple, 6 teal, 7 silver, 8 grey, 9 red, 10 lime, 11 yellow, 12 blue, 13 fuchsia, 14 aqua, 15 white).</param>
        /// <returns>The colour.</returns>
        public static Color FromAnsi16(int index) {
            if (index < 0 || index > 15) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new Color(ColorKind.Ansi16, (byte)index, 0, 0, 0);
        }

        /// <summary>Creates an xterm 256-palette colour.</summary>
        /// <param name="index">Index 0-255.</param>
        /// <returns>The colour.</returns>
        public static Color FromAnsi256(int index) {
            if (index < 0 || index > 255) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return new Color(ColorKind.Ansi256, (byte)index, 0, 0, 0);
        }

        /// <summary>Parses <c>#rgb</c> or <c>#rrggbb</c>.</summary>
        /// <param name="hex">The hex string, with or without the leading <c>#</c>.</param>
        /// <returns>The colour.</returns>
        public static Color FromHex(string hex) {
            if (!TryParseHex(hex, out Color color)) {
                throw new FormatException("Invalid hex colour '" + hex + "'.");
            }
            return color;
        }

        /// <summary>
        /// Parses a colour the way Spectre.Console markup does: one of the 256 xterm names (<c>red</c>, <c>aqua</c>,
        /// <c>orange1</c>, <c>cadetblue</c>, <c>grey50</c>, <c>deepskyblue4_1</c>), <c>default</c>, <c>#rrggbb</c>, <c>#rgb</c>,
        /// <c>rgb(r,g,b)</c>, or a 256-palette index (<c>208</c> or <c>color(208)</c>). Where an xterm name and a ConsoleColor
        /// name collide (<c>darkred</c>, <c>darkcyan</c>), the xterm colour wins here, as in Spectre markup; ConsoleColor names
        /// that are not xterm names (<c>gray</c>, <c>darkgray</c>, <c>darkyellow</c>, <c>magenta</c>, <c>cyan</c>) keep the
        /// PowerShell mapping. <see cref="RichColor.TryParse(string, out RichColor)"/> reads ConsoleColor names first.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="color">The parsed colour.</param>
        /// <returns>Whether parsing succeeded.</returns>
        public static bool TryParse(string? text, out Color color) {
            color = Default;
            if (text == null) {
                return false;
            }
            string value = text.Trim();
            if (value.Length == 0) {
                return false;
            }
            if (value[0] == '#') {
                return TryParseHex(value, out color);
            }
            string lower = value.ToLowerInvariant();
            if (lower == "default") {
                return true;
            }
            if (lower.StartsWith("rgb(", StringComparison.Ordinal) && lower.EndsWith(")", StringComparison.Ordinal)) {
                string[] parts = lower.Substring(4, lower.Length - 5).Split(',');
                if (parts.Length == 3
                    && byte.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r)
                    && byte.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g)
                    && byte.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b)) {
                    color = FromRgb(r, g, b);
                    return true;
                }
                return false;
            }
            if (lower.StartsWith("color(", StringComparison.Ordinal) && lower.EndsWith(")", StringComparison.Ordinal)) {
                lower = lower.Substring(6, lower.Length - 7).Trim();
            }
            if (int.TryParse(lower, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)) {
                if (index >= 0 && index <= 255) {
                    color = FromAnsi256(index);
                    return true;
                }
                return false;
            }
            return ColorNames.TryGet(lower, out color);
        }

        /// <summary>
        /// Converts the colour to the closest colour representable at <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The target colour depth.</param>
        /// <returns>The downsampled colour; <see cref="Default"/> for <see cref="ColorDepth.None"/>.</returns>
        public Color Downsample(ColorDepth depth) {
            if (Kind == ColorKind.Default || depth == ColorDepth.TrueColor) {
                return this;
            }
            if (depth == ColorDepth.None) {
                return Default;
            }
            if (Kind == ColorKind.Ansi16) {
                return this;
            }
            if (depth == ColorDepth.Ansi256) {
                if (Kind == ColorKind.Ansi256) {
                    return this;
                }
                return FromAnsi256(AnsiPalette.Nearest256(R, G, B));
            }
            ToRgb(out byte r, out byte g, out byte b);
            if (Kind == ColorKind.Ansi256 && Index < 16) {
                return FromAnsi16(Index);
            }
            return FromAnsi16(AnsiPalette.Nearest16(r, g, b));
        }

        /// <summary>Returns the approximate RGB value of the colour (palette colours use the Campbell scheme).</summary>
        /// <param name="r">Red.</param>
        /// <param name="g">Green.</param>
        /// <param name="b">Blue.</param>
        public void ToRgb(out byte r, out byte g, out byte b) {
            switch (Kind) {
                case ColorKind.Rgb:
                    r = R;
                    g = G;
                    b = B;
                    break;
                case ColorKind.Ansi16:
                    AnsiPalette.Get16(Index, out r, out g, out b);
                    break;
                case ColorKind.Ansi256:
                    AnsiPalette.Get256(Index, out r, out g, out b);
                    break;
                default:
                    r = 204;
                    g = 204;
                    b = 204;
                    break;
            }
        }

        /// <summary>Linearly blends towards another colour in RGB space (used for gradients).</summary>
        /// <param name="other">The target colour.</param>
        /// <param name="amount">0 returns this colour, 1 returns <paramref name="other"/>.</param>
        /// <returns>An RGB colour.</returns>
        public Color Blend(Color other, double amount) {
            if (amount <= 0) {
                return this;
            }
            if (amount >= 1) {
                return other;
            }
            ToRgb(out byte r1, out byte g1, out byte b1);
            other.ToRgb(out byte r2, out byte g2, out byte b2);
            return FromRgb(
                (byte)Math.Round(r1 + ((r2 - r1) * amount)),
                (byte)Math.Round(g1 + ((g2 - g1) * amount)),
                (byte)Math.Round(b1 + ((b2 - b1) * amount)));
        }

        /// <inheritdoc />
        public bool Equals(Color other) {
            return Kind == other.Kind && Index == other.Index && R == other.R && G == other.G && B == other.B;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Color other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            return ((int)Kind << 24) ^ (Index << 16) ^ (R << 11) ^ (G << 5) ^ B;
        }

        /// <summary>Equality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether equal.</returns>
        public static bool operator ==(Color left, Color right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether different.</returns>
        public static bool operator !=(Color left, Color right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() {
            switch (Kind) {
                case ColorKind.Rgb:
                    return "#" + R.ToString("x2", CultureInfo.InvariantCulture) + G.ToString("x2", CultureInfo.InvariantCulture) + B.ToString("x2", CultureInfo.InvariantCulture);
                case ColorKind.Ansi16:
                    return "ansi16(" + Index.ToString(CultureInfo.InvariantCulture) + ")";
                case ColorKind.Ansi256:
                    return "color(" + Index.ToString(CultureInfo.InvariantCulture) + ")";
                default:
                    return "default";
            }
        }

        private static bool TryParseHex(string? hex, out Color color) {
            color = Default;
            if (hex == null) {
                return false;
            }
            string value = hex.Trim();
            if (value.StartsWith("#", StringComparison.Ordinal)) {
                value = value.Substring(1);
            }
            if (value.Length == 3) {
                value = new string(new[] { value[0], value[0], value[1], value[1], value[2], value[2] });
            }
            if (value.Length != 6) {
                return false;
            }
            if (!int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) {
                return false;
            }
            color = FromRgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
            return true;
        }
    }
}
