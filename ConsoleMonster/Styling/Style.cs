using System;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Foreground, background and decorations for a run of text or a cell. Immutable.
    /// </summary>
    public readonly struct Style : IEquatable<Style> {
        /// <summary>Creates a style.</summary>
        /// <param name="color">Foreground colour.</param>
        /// <param name="backgroundColor">Background colour.</param>
        /// <param name="decoration">Decorations.</param>
        /// <param name="link">A hyperlink target (OSC 8), or null.</param>
        public Style(Color color, Color backgroundColor = default, TextDecoration decoration = TextDecoration.None, string? link = null) {
            Color = color;
            BackgroundColor = backgroundColor;
            Decoration = decoration;
            Link = string.IsNullOrEmpty(link) ? null : link;
        }

        /// <summary>The unstyled default (terminal colours, no decoration).</summary>
        public static Style Plain => default;

        /// <summary>Foreground colour.</summary>
        public Color Color { get; }

        /// <summary>Background colour.</summary>
        public Color BackgroundColor { get; }

        /// <summary>Decorations.</summary>
        public TextDecoration Decoration { get; }

        /// <summary>The hyperlink target emitted as OSC 8 where the terminal supports it, or null.</summary>
        public string? Link { get; }

        /// <summary>Whether this is the plain style.</summary>
        public bool IsPlain => Color.IsDefault && BackgroundColor.IsDefault && Decoration == TextDecoration.None && Link == null;

        /// <summary>Returns a copy with a different foreground.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>A new style.</returns>
        public Style WithColor(Color color) => new Style(color, BackgroundColor, Decoration, Link);

        /// <summary>Returns a copy with a different background.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>A new style.</returns>
        public Style WithBackgroundColor(Color color) => new Style(Color, color, Decoration, Link);

        /// <summary>Returns a copy with extra decorations added.</summary>
        /// <param name="decoration">Decorations to add.</param>
        /// <returns>A new style.</returns>
        public Style With(TextDecoration decoration) => new Style(Color, BackgroundColor, Decoration | decoration, Link);

        /// <summary>Returns a copy with decorations removed.</summary>
        /// <param name="decoration">Decorations to remove.</param>
        /// <returns>A new style.</returns>
        public Style Without(TextDecoration decoration) => new Style(Color, BackgroundColor, Decoration & ~decoration, Link);

        /// <summary>Returns a copy with a hyperlink target (null removes it).</summary>
        /// <param name="link">The target URL.</param>
        /// <returns>The new style.</returns>
        public Style WithLink(string? link) => new Style(Color, BackgroundColor, Decoration, link);

        /// <summary>
        /// Layers <paramref name="overlay"/> on top of this style: non-default overlay colours win and decorations are combined.
        /// </summary>
        /// <param name="overlay">The style on top.</param>
        /// <returns>The combined style.</returns>
        public Style Combine(Style overlay) {
            return new Style(
                overlay.Color.IsDefault ? Color : overlay.Color,
                overlay.BackgroundColor.IsDefault ? BackgroundColor : overlay.BackgroundColor,
                Decoration | overlay.Decoration,
                overlay.Link ?? Link);
        }

        /// <summary>Downsamples both colours to the given depth; decorations are dropped for <see cref="ColorDepth.None"/>.</summary>
        /// <param name="depth">The colour depth.</param>
        /// <returns>The downsampled style.</returns>
        public Style Downsample(ColorDepth depth) {
            if (depth == ColorDepth.None) {
                return Plain;
            }
            return new Style(Color.Downsample(depth), BackgroundColor.Downsample(depth), Decoration, Link);
        }

        /// <inheritdoc />
        public bool Equals(Style other) {
            return Color.Equals(other.Color) && BackgroundColor.Equals(other.BackgroundColor) && Decoration == other.Decoration && string.Equals(Link, other.Link, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Style other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return (Color.GetHashCode() * 397) ^ (BackgroundColor.GetHashCode() * 31) ^ (int)Decoration ^ (Link == null ? 0 : StringComparer.Ordinal.GetHashCode(Link));
            }
        }

        /// <summary>Equality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether equal.</returns>
        public static bool operator ==(Style left, Style right) => left.Equals(right);

        /// <summary>Inequality operator.</summary>
        /// <param name="left">Left.</param>
        /// <param name="right">Right.</param>
        /// <returns>Whether different.</returns>
        public static bool operator !=(Style left, Style right) => !left.Equals(right);

        /// <inheritdoc />
        public override string ToString() {
            string text = Color + " on " + BackgroundColor;
            return Decoration == TextDecoration.None ? text : Decoration + " " + text;
        }
    }
}
