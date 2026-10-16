using System;
using ConsoleMonster.Capabilities;

namespace ConsoleMonster.Styling {
    /// <summary>
    /// Foreground, background and decorations for a run of text or a cell. Immutable.
    /// </summary>
    public readonly struct Style : IEquatable<Style> {
        /// <summary>Creates a style.</summary>
        /// <param name="foreground">Foreground colour.</param>
        /// <param name="background">Background colour.</param>
        /// <param name="decoration">Decorations.</param>
        public Style(Color foreground, Color background = default, TextDecoration decoration = TextDecoration.None) {
            Foreground = foreground;
            Background = background;
            Decoration = decoration;
        }

        /// <summary>The unstyled default (terminal colours, no decoration).</summary>
        public static Style Plain => default;

        /// <summary>Foreground colour.</summary>
        public Color Foreground { get; }

        /// <summary>Background colour.</summary>
        public Color Background { get; }

        /// <summary>Decorations.</summary>
        public TextDecoration Decoration { get; }

        /// <summary>Whether this is the plain style.</summary>
        public bool IsPlain => Foreground.IsDefault && Background.IsDefault && Decoration == TextDecoration.None;

        /// <summary>Returns a copy with a different foreground.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>A new style.</returns>
        public Style WithForeground(Color color) => new Style(color, Background, Decoration);

        /// <summary>Returns a copy with a different background.</summary>
        /// <param name="color">The colour.</param>
        /// <returns>A new style.</returns>
        public Style WithBackground(Color color) => new Style(Foreground, color, Decoration);

        /// <summary>Returns a copy with extra decorations added.</summary>
        /// <param name="decoration">Decorations to add.</param>
        /// <returns>A new style.</returns>
        public Style With(TextDecoration decoration) => new Style(Foreground, Background, Decoration | decoration);

        /// <summary>Returns a copy with decorations removed.</summary>
        /// <param name="decoration">Decorations to remove.</param>
        /// <returns>A new style.</returns>
        public Style Without(TextDecoration decoration) => new Style(Foreground, Background, Decoration & ~decoration);

        /// <summary>
        /// Layers <paramref name="overlay"/> on top of this style: non-default overlay colours win and decorations are combined.
        /// </summary>
        /// <param name="overlay">The style on top.</param>
        /// <returns>The combined style.</returns>
        public Style Combine(Style overlay) {
            return new Style(
                overlay.Foreground.IsDefault ? Foreground : overlay.Foreground,
                overlay.Background.IsDefault ? Background : overlay.Background,
                Decoration | overlay.Decoration);
        }

        /// <summary>Downsamples both colours to the given depth; decorations are dropped for <see cref="ColorDepth.None"/>.</summary>
        /// <param name="depth">The colour depth.</param>
        /// <returns>The downsampled style.</returns>
        public Style Downsample(ColorDepth depth) {
            if (depth == ColorDepth.None) {
                return Plain;
            }
            return new Style(Foreground.Downsample(depth), Background.Downsample(depth), Decoration);
        }

        /// <inheritdoc />
        public bool Equals(Style other) {
            return Foreground.Equals(other.Foreground) && Background.Equals(other.Background) && Decoration == other.Decoration;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Style other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() {
            unchecked {
                return (Foreground.GetHashCode() * 397) ^ (Background.GetHashCode() * 31) ^ (int)Decoration;
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
            string text = Foreground + " on " + Background;
            return Decoration == TextDecoration.None ? text : Decoration + " " + text;
        }
    }
}
